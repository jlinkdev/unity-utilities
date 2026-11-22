#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace jlinkdev.UnityUtilities.InteractiveFog.Tests
{
    public sealed class FogLifecycleTests
    {
        GameObject go;
        FogVolume Create()
        {
            go=new GameObject("Play mode fog"); var fog=go.AddComponent<FogVolume>();
            fog.simulation=AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.jlinkdev.volumetric-fog-and-rain/Runtime/InteractiveFog/Shaders/FogFluid.compute");
            fog.resolution=new Vector3Int(24,8,24); fog.size=new Vector3(3,1,3); fog.Initialize(); return fog;
        }
        [UnityTearDown] public IEnumerator Cleanup() { if(go) Object.Destroy(go); yield return null; }
        [UnityTest] public IEnumerator SimulatesWithoutACameraAndReleasesOnDisable()
        {
            var previous=new HashSet<RenderTexture>(Resources.FindObjectsOfTypeAll<RenderTexture>());
            var fog=Create(); yield return new WaitForSeconds(.15f);
            Assert.That(fog.StepCount,Is.GreaterThan(0)); var texture=fog.Density;
            var owned=Resources.FindObjectsOfTypeAll<RenderTexture>().Where(t=>!previous.Contains(t)&&t.name.StartsWith("Interactive Fog")).ToArray();
            Assert.That(owned.Length,Is.GreaterThan(1));
            fog.enabled=false; yield return null;
            Assert.That(fog.IsReady,Is.False); Assert.That(texture==null,Is.True);
            foreach(var resource in owned) Assert.That(resource==null,Is.True,"All owned fog textures, including cached patterns, must be released.");
            fog.enabled=true; yield return null; Assert.That(fog.IsReady,Is.True);
        }
        [UnityTest] public IEnumerator ProceduralBillowTexturesReleaseOnDisable3D() => ProceduralRelease(FogSimulationBackend.Volume3D);
        [UnityTest] public IEnumerator ProceduralBillowTexturesReleaseOnDisable2D() => ProceduralRelease(FogSimulationBackend.Planar2D);
        IEnumerator ProceduralRelease(FogSimulationBackend backend)
        {
            var fog=Create();fog.simulationBackend=backend;fog.densityShape=FogDensityShape.DryIce;
            fog.puffs=FogPuffSettings.Default;fog.Initialize();
            yield return new WaitForSeconds(.15f);
            var resources=Resources.FindObjectsOfTypeAll<RenderTexture>().Where(t=>t.name.StartsWith("Interactive Fog Billows")||t.name=="Interactive Fog Procedural billow sources").ToArray();
            Assert.That(resources.Length,Is.EqualTo(3));fog.enabled=false;yield return null;
            foreach(var resource in resources) Assert.That(resource==null,Is.True);
            fog.enabled=true;yield return null;Assert.That(fog.PuffDensity,Is.Not.Null);Assert.That(fog.UsesProceduralPuffs,Is.True);
        }
        [UnityTest] public IEnumerator PlanarBackendReleasesEveryOwnedTexture()
        {
            var previous=new HashSet<RenderTexture>(Resources.FindObjectsOfTypeAll<RenderTexture>());
            var fog=Create();fog.simulationBackend=FogSimulationBackend.Planar2D;fog.Initialize();
            yield return null;yield return new WaitForSeconds(.1f);
            var owned=Resources.FindObjectsOfTypeAll<RenderTexture>().Where(t=>!previous.Contains(t)&&t.name.StartsWith("Interactive Fog")).ToArray();
            Assert.That(fog.StepCount,Is.GreaterThan(0));Assert.That(owned.Length,Is.GreaterThan(10));
            fog.enabled=false;yield return null;
            foreach(var texture in owned)Assert.That(texture==null,Is.True);
            Assert.That(fog.IsReady,Is.False);
        }
        [UnityTest] public IEnumerator CumulusDetailCacheIsReleasedOnStyleChangeAndDisable()
        {
            var fog=Create(); var bytes=fog.EstimatedTextureBytes;
            var profile=ScriptableObject.CreateInstance<FogStyleProfile>(); profile.settings=FogStyleSettings.Cumulus;
            var field=typeof(FogVolume).GetField("cumulusShape",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            try
            {
                fog.style=profile; fog.ResetSimulation();
                var detail=(RenderTexture)field.GetValue(fog);
                Assert.That(detail,Is.Not.Null); Assert.That(detail.width,Is.GreaterThan(fog.resolution.x));
                long detailBytes=0; int w=detail.width,h=detail.height,d=detail.volumeDepth;
                for(int i=0;i<detail.mipmapCount;i++) { detailBytes+=(long)w*h*d*8; w=Mathf.Max(1,w/2); h=Mathf.Max(1,h/2); d=Mathf.Max(1,d/2); }
                Assert.That(detail.mipmapCount,Is.GreaterThan(1));
                Assert.That(fog.EstimatedTextureBytes,Is.EqualTo(bytes+detailBytes));
                fog.style=null; fog.Advance(.000001f); yield return null;
                Assert.That(detail==null,Is.True); Assert.That(fog.EstimatedTextureBytes,Is.EqualTo(bytes));
                fog.style=profile; fog.Advance(.000001f); detail=(RenderTexture)field.GetValue(fog);
                Assert.That(detail,Is.Not.Null); fog.enabled=false; yield return null;
                Assert.That(detail==null,Is.True); Assert.That(fog.EstimatedTextureBytes,Is.Zero);
            }
            finally { Object.Destroy(profile); }
        }
        [UnityTest] public IEnumerator ManualClockDoesNotAdvanceFromCameraCount()
        {
            var fog=Create(); fog.automaticSimulation=false;
            var a=new GameObject("Camera A"); var b=new GameObject("Camera B");
            try
            {
                a.AddComponent<Camera>(); b.AddComponent<Camera>();
                yield return null; yield return null; Assert.That(fog.StepCount,Is.Zero);
                fog.Advance(1f/30); yield return null; Assert.That(fog.StepCount,Is.EqualTo(1));
            }
            finally { Object.Destroy(a); Object.Destroy(b); }
        }
    }
}
#endif
