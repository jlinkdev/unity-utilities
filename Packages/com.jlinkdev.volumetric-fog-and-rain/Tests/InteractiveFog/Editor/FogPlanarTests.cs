#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace jlinkdev.UnityUtilities.InteractiveFog.Tests
{
    public sealed class FogPlanarTests
    {
        GameObject go,actor,obstacleObject;
        FogVolume Create()
        {
            if(!SystemInfo.supportsComputeShaders) Assert.Ignore("Compute GPU required.");
            ShaderUtil.allowAsyncCompilation=false;
            go=new GameObject("Planar fog test");var fog=go.AddComponent<FogVolume>();
            fog.simulation=AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.jlinkdev.volumetric-fog-and-rain/Runtime/InteractiveFog/Shaders/FogFluid.compute");
            fog.resolution=new Vector3Int(32,12,32);fog.size=new Vector3(4,1,4);fog.automaticSimulation=false;
            fog.simulationBackend=FogSimulationBackend.Planar2D;fog.Initialize();return fog;
        }
        static float[] Read(RenderTexture texture,TextureFormat format=TextureFormat.RFloat)
        {
            var r=AsyncGPUReadback.Request(texture,0,format);r.WaitForCompletion();Assert.That(r.hasError,Is.False);
            int n=r.GetData<float>(0).Length;var result=new float[n*r.layerCount];
            for(int layer=0;layer<r.layerCount;layer++){var data=r.GetData<float>(layer);for(int i=0;i<n;i++)result[layer*n+i]=data[i];}
            foreach(float value in result)Assert.That(float.IsFinite(value),Is.True);
            return result;
        }
        static void Advance(FogVolume fog,int steps){for(int i=0;i<steps;i++)fog.Advance(1f/30);}
        [TearDown] public void Cleanup(){if(go)Object.DestroyImmediate(go);if(actor)Object.DestroyImmediate(actor);if(obstacleObject)Object.DestroyImmediate(obstacleObject);}
        [Test] public void PlanarUses2DTexturesAndPreservesCachedThreeDimensionalRestShape()
        {
            var fog=Create();Assert.That(fog.Density.dimension,Is.EqualTo(TextureDimension.Tex2D));
            Assert.That(fog.Density.width,Is.EqualTo(32));Assert.That(fog.Density.height,Is.EqualTo(32));
            long bytes=fog.EstimatedTextureBytes;
            var field=typeof(FogVolume).GetField("restDensity",BindingFlags.Instance|BindingFlags.NonPublic);
            var expected=Read((RenderTexture)field.GetValue(fog));
            fog.simulationBackend=FogSimulationBackend.Volume3D;fog.Initialize();
            CollectionAssert.AreEqual(expected,Read((RenderTexture)field.GetValue(fog)));
            Assert.That(bytes,Is.LessThan(fog.EstimatedTextureBytes));
        }
        [Test] public void PlanarConservesColumnMassWithoutSourcesAndRemainsPositive()
        {
            var fog=Create();fog.restRecovery=0;fog.replenishment=0;fog.ResetSimulation();
            double initial=Read(fog.Density).Sum(v=>(double)v);
            fog.AddGust(new Vector3(0,.2f,0),Vector3.right*4,.6f,.6f);Advance(fog,600);
            var end=Read(fog.Density);Assert.That(end.Min(),Is.GreaterThanOrEqualTo(0));
            Assert.That(end.Sum(v=>(double)v)/initial,Is.EqualTo(1).Within(.0001));
        }
        [Test] public void PlanarRecoveryRebuildsDisturbedColumnPattern()
        {
            var fog=Create();fog.replenishment=0;fog.restRecovery=0;fog.ResetSimulation();var target=Read(fog.Density);
            fog.AddGust(new Vector3(0,.2f,0),Vector3.right*4,.8f,1);Advance(fog,1200);
            var without=Read(fog.Density);double errorWithout=without.Zip(target,(a,b)=>(double)Mathf.Abs(a-b)).Sum();
            fog.restRecovery=.25f;fog.ResetSimulation();fog.AddGust(new Vector3(0,.2f,0),Vector3.right*4,.8f,1);Advance(fog,1200);
            double errorWith=Read(fog.Density).Zip(target,(a,b)=>(double)Mathf.Abs(a-b)).Sum();
            Debug.Log($"PLANAR_RECOVERY without={errorWithout} with={errorWith}");
            Assert.That(errorWithout,Is.GreaterThan(.1));Assert.That(errorWith,Is.LessThan(errorWithout*.5));
        }
        [Test] public void PlanarZeroDragDoesNotInjectForceOrWake()
        {
            var fog=Create();Advance(fog,90);var expected=Read(fog.Density);var velocity=Read(fog.Velocity,TextureFormat.RGBAFloat);
            fog.ResetSimulation();actor=new GameObject("Zero drag actor");var interactor=actor.AddComponent<FogInteractor>();interactor.drag=0;interactor.clearing=0;
            for(int i=0;i<90;i++){actor.transform.position=new Vector3(-1+i*.02f,0,0);interactor.SampleMotion(1f/30);fog.Advance(1f/30);}
            CollectionAssert.AreEqual(expected,Read(fog.Density));CollectionAssert.AreEqual(velocity,Read(fog.Velocity,TextureFormat.RGBAFloat));
        }
        [Test] public void PlanarHeightChangePreservesWakeAndBackendSwitchReleasesTextures()
        {
            var fog=Create();fog.AddGust(Vector3.up*.2f,Vector3.right*3,.5f,.5f);Advance(fog,30);
            var texture=fog.Density;var before=Read(texture);int steps=fog.StepCount;
            fog.fogHeight=.85f;fog.Advance(.000001f);
            Assert.That(fog.StepCount,Is.EqualTo(steps));Assert.That(fog.Density,Is.SameAs(texture));CollectionAssert.AreEqual(before,Read(texture));
            fog.simulationBackend=FogSimulationBackend.Volume3D;fog.Advance(.000001f);
            Assert.That(texture==null || !texture.IsCreated(),Is.True);Assert.That(fog.Density.dimension,Is.EqualTo(TextureDimension.Tex3D));
            fog.simulationBackend=FogSimulationBackend.Planar2D;fog.Advance(.000001f);texture=fog.Density;fog.enabled=false;
            Assert.That(texture==null || !texture.IsCreated(),Is.True);Assert.That(fog.EstimatedTextureBytes,Is.Zero);
        }
        [Test] public void PlanarObstaclesTrackMovementAndRemoval()
        {
            var fog=Create();obstacleObject=new GameObject("Planar wall");obstacleObject.transform.position=new Vector3(-.7f,.3f,0);
            var obstacle=obstacleObject.AddComponent<FogObstacle>();obstacle.size=new Vector3(.5f,1,.5f);
            Advance(fog,1);var initial=Read(fog.SolidMask);Assert.That(initial.Max(),Is.EqualTo(1));
            obstacleObject.transform.position+=Vector3.right*1.4f;Advance(fog,1);CollectionAssert.AreNotEqual(initial,Read(fog.SolidMask));
            obstacle.enabled=false;Advance(fog,1);Assert.That(Read(fog.SolidMask).Max(),Is.Zero);
        }
    }
}
#endif
