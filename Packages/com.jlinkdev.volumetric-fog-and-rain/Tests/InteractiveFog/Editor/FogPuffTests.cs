#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace jlinkdev.UnityUtilities.InteractiveFog.Tests
{
    public sealed class FogPuffTests
    {
        GameObject root;
        FogStyleProfile profile;
        FogVolume Create(FogSimulationBackend backend)
        {
            if(!SystemInfo.supportsComputeShaders) Assert.Ignore("Compute GPU required.");
            ShaderUtil.allowAsyncCompilation=false;
            root=new GameObject("Dry Ice lifecycle test"); var fog=root.AddComponent<FogVolume>();
            fog.automaticSimulation=false; fog.simulationBackend=backend;
            fog.size=new Vector3(4,2,4); fog.resolution=new Vector3Int(32,24,32); fog.pressureIterations=8;
            fog.simulation=AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.jlinkdev.volumetric-fog-and-rain/Runtime/InteractiveFog/Shaders/FogFluid.compute");
            profile=ScriptableObject.CreateInstance<FogStyleProfile>(); profile.settings=FogStyleSettings.DryIce;
            profile.settings.restRecovery=0; profile.settings.wind=Vector3.zero; profile.settings.turbulence=0;
            profile.settings.puffs.spreadSpeed=0; profile.settings.puffs.mistAmount=2;
            fog.style=profile; fog.Initialize(); return fog;
        }
        static float Sum(RenderTexture texture)
        {
            var r=AsyncGPUReadback.Request(texture,0,TextureFormat.RFloat);r.WaitForCompletion();Assert.That(r.hasError,Is.False);
            double sum=0;
            for(int layer=0;layer<r.layerCount;layer++) foreach(float value in r.GetData<float>(layer))
            { Assert.That(float.IsFinite(value),Is.True);Assert.That(value,Is.GreaterThanOrEqualTo(-.00001f));sum+=value; }
            return (float)sum;
        }
        static void Steps(FogVolume fog,int n) { for(int i=0;i<n;i++) fog.Advance(1f/30); }
        static float Mass(FogVolume fog) => Sum(fog.Density)*fog.size.x/fog.resolution.x*fog.size.z/fog.resolution.z*(fog.simulationBackend==FogSimulationBackend.Volume3D?fog.size.y/fog.resolution.y:1);
        [TestCase(FogSimulationBackend.Volume3D)] [TestCase(FogSimulationBackend.Planar2D)]
        public void LiveRadiusEditsChangeProceduralBodyWithoutRestart(FogSimulationBackend backend)
        {
            var fog=Create(backend);profile.settings.puffs.coverage=1;profile.settings.puffs.spacing=1.5f;
            profile.settings.puffs.radiusRange=new Vector2(.2f,.2f);fog.ResetSimulation();
            float small=Sum(fog.PuffDensity);Steps(fog,1);int tick=fog.StepCount;
            profile.settings.puffs.radiusRange=new Vector2(.6f,.6f);Steps(fog,1);
            Assert.That(fog.StepCount,Is.EqualTo(tick+1));
            Assert.That(Sum(fog.PuffDensity),Is.GreaterThan(small*4),"Live radius edits must reach the field without changing profiles or resetting.");
        }
        [TearDown] public void Cleanup()
        {
            if(root) Object.DestroyImmediate(root);
            if(profile) Object.DestroyImmediate(profile);
        }
        [TestCase(FogSimulationBackend.Volume3D)] [TestCase(FogSimulationBackend.Planar2D)]
        public void NewGenericProfileCanSwitchToDryIceWithVisibleBillows(FogSimulationBackend backend)
        {
            var fog=Create(backend);profile.settings=FogStyleSettings.Default;
            profile.settings.densityShape=FogDensityShape.DryIce;fog.ResetSimulation();
            Assert.That(Sum(fog.PuffDensity),Is.GreaterThan(0),"New profiles must not start with zero coverage and zero billow density.");
        }
        [TestCase(FogSimulationBackend.Volume3D)] [TestCase(FogSimulationBackend.Planar2D)]
        public void ProceduralFieldRunsWithoutEmittersAndReplaysAfterReset(FogSimulationBackend backend)
        {
            var fog=Create(backend);

            profile.settings.puffs.spacing=.5f;profile.settings.puffs.coverage=1;
            profile.settings.puffs.radiusRange=new Vector2(.2f,.25f);
            profile.settings.puffs.growthRange=new Vector2(.4f,.7f);profile.settings.puffs.settlingRange=new Vector2(.7f,1.2f);
            profile.settings.puffs.quietRange=new Vector2(.1f,.3f);
            fog.ResetSimulation();Assert.That(Sum(fog.PuffDensity),Is.GreaterThan(0));
            Steps(fog,60);float first=Sum(fog.PuffDensity);
            Assert.That(first,Is.GreaterThan(0));
            fog.ResetSimulation();for(int i=0;i<120;i++)fog.Advance(1f/60);
            Assert.That(Sum(fog.PuffDensity),Is.EqualTo(first).Within(.001));
        }
        [TestCase(FogSimulationBackend.Volume3D)] [TestCase(FogSimulationBackend.Planar2D)]
        public void ProceduralCoverageAndReleaseAffectTheActualFluid(FogSimulationBackend backend)
        {
            var fog=Create(backend);
            profile.settings.puffs.spacing=1;profile.settings.puffs.coverage=1;profile.settings.puffs.radiusRange=new Vector2(.4f,.5f);
            profile.settings.puffs.growthRange=new Vector2(.4f,.7f);profile.settings.puffs.settlingRange=new Vector2(.7f,1.2f);
            profile.settings.puffs.quietRange=new Vector2(.1f,.3f);
            fog.ResetSimulation();float before=Mass(fog);Steps(fog,60);
            Assert.That(Mass(fog),Is.GreaterThan(before+.05f));
            profile.settings.puffs.coverage=0;Steps(fog,1);
            Assert.That(Sum(fog.PuffDensity),Is.Zero);
            Assert.That(Mass(fog),Is.GreaterThan(before+.05f),"Released mist remains after the procedural body is removed.");
        }
        [TestCase(FogSimulationBackend.Volume3D)] [TestCase(FogSimulationBackend.Planar2D)]
        public void StyleSwapReleasesBillowResourcesWithoutResettingFluid(FogSimulationBackend backend)
        {
            var fog=Create(backend);Steps(fog,10);
            var texture=fog.PuffDensity;float mass=Mass(fog);long bytes=fog.EstimatedTextureBytes;
            profile.settings=FogStyleSettings.Default;fog.Advance(.00001f);
            Assert.That(fog.StepCount,Is.EqualTo(10));Assert.That(fog.PuffDensity,Is.Null);
            Assert.That(!texture || !texture.IsCreated(),Is.True);Assert.That(Mass(fog),Is.EqualTo(mass).Within(.00001));
            Assert.That(bytes-fog.EstimatedTextureBytes,Is.EqualTo(32L*24*32*16));
        }
    }
}
#endif
