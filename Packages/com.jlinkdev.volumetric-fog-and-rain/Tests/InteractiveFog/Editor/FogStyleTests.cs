#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace jlinkdev.UnityUtilities.InteractiveFog.Tests
{
    public sealed class FogStyleTests
    {
        GameObject go, obstacleObject;
        FogStyleProfile profile;
        FogVolume Create()
        {
            if(!SystemInfo.supportsComputeShaders) Assert.Ignore("A compute-capable GPU is required.");
            ShaderUtil.allowAsyncCompilation=false;
            go=new GameObject("Fog style test"); var fog=go.AddComponent<FogVolume>();
            fog.simulation=AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.jlinkdev.volumetric-fog-and-rain/Runtime/InteractiveFog/Shaders/FogFluid.compute");
            fog.resolution=new Vector3Int(24,8,24); fog.size=new Vector3(3,1,3); fog.automaticSimulation=false;
            fog.Initialize(); return fog;
        }
        static float[] Read(RenderTexture texture,TextureFormat format=TextureFormat.RFloat)
        {
            var request=AsyncGPUReadback.Request(texture,0,format); request.WaitForCompletion();
            Assert.That(request.hasError,Is.False);
            int perLayer=request.GetData<float>(0).Length;
            var values=new float[perLayer*request.layerCount];
            for(int layer=0;layer<request.layerCount;layer++)
            {
                var data=request.GetData<float>(layer);
                for(int i=0;i<perLayer;i++) values[layer*perLayer+i]=data[i];
            }
            Assert.That(values.Length,Is.EqualTo(texture.width*texture.height*texture.volumeDepth*(format==TextureFormat.RGBAFloat?4:1)));
            return values;
        }
        [TearDown] public void Cleanup()
        {
            if(go) Object.DestroyImmediate(go);
            if(obstacleObject) Object.DestroyImmediate(obstacleObject);
            if(profile) Object.DestroyImmediate(profile);
        }
        [Test] public void DefaultProfileExactlyPreservesInlineDensityAndQuality()
        {
            var fog=Create(); var expected=Read(fog.Density);
            var preset=AssetDatabase.LoadAssetAtPath<FogStyleProfile>("Packages/com.jlinkdev.volumetric-fog-and-rain/Runtime/InteractiveFog/Styles/Sanctuary Ground Mist.asset");
            Assert.That(preset,Is.Not.Null); profile=Object.Instantiate(preset); fog.style=profile;
            fog.ResetSimulation(); CollectionAssert.AreEqual(expected,Read(fog.Density));
            Assert.That(fog.resolution,Is.EqualTo(new Vector3Int(24,8,24)));
            Assert.That(fog.raySteps,Is.EqualTo(96)); Assert.That(fog.simulationRate,Is.EqualTo(30));
        }
        [Test] public void LiveProfileSwapPreservesWakeAndDoesNotMutateAssetOrInlineSettings()
        {
            var fog=Create(); fog.AddGust(Vector3.up*.2f,Vector3.right*3,.5f,.5f); fog.Advance(1f/30);
            var density=fog.Density; var before=Read(density); int steps=fog.StepCount;
            profile=ScriptableObject.CreateInstance<FogStyleProfile>(); profile.settings.fogHeight=.9f;
            profile.settings.wind=Vector3.left; fog.style=profile; fog.Advance(.00001f);
            Assert.That(fog.Density,Is.SameAs(density)); Assert.That(fog.StepCount,Is.EqualTo(steps));
            CollectionAssert.AreEqual(before,Read(fog.Density));
            Assert.That(fog.ResolvedStyle.fogHeight,Is.EqualTo(.9f)); Assert.That(fog.fogHeight,Is.EqualTo(.62f));
            Assert.That(profile.settings.wind,Is.EqualTo(Vector3.left));
            fog.style=null; fog.Advance(.00001f); CollectionAssert.AreEqual(before,Read(fog.Density));
            Assert.That(fog.ResolvedStyle.fogHeight,Is.EqualTo(.62f));
        }
        [TestCase(FogSimulationBackend.Volume3D)]
        [TestCase(FogSimulationBackend.Planar2D)]
        public void MorphologySwapRebuildsRecoveryTargetWithoutResettingWake(FogSimulationBackend backend)
        {
            var fog=Create(); fog.simulationBackend=backend; fog.Initialize();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var restField=typeof(FogVolume).GetField("restDensity",flags);
            var oldTarget=Read((RenderTexture)restField.GetValue(fog));
            fog.AddGust(Vector3.up*.2f,Vector3.right*3,.5f,.2f); fog.Advance(1f/30);
            var texture=fog.Density; var wake=Read(texture); var velocity=Read(fog.Velocity,TextureFormat.RGBAFloat); int tick=fog.StepCount;
            var preset=AssetDatabase.LoadAssetAtPath<FogStyleProfile>("Packages/com.jlinkdev.volumetric-fog-and-rain/Runtime/InteractiveFog/Styles/Cumulus.asset");
            Assert.That(preset,Is.Not.Null); profile=Object.Instantiate(preset);
            profile.settings.fogHeight=fog.fogHeight; fog.style=profile; fog.Advance(.000001f);
            Assert.That(fog.Density,Is.SameAs(texture)); Assert.That(fog.StepCount,Is.EqualTo(tick));
            CollectionAssert.AreEqual(wake,Read(fog.Density)); CollectionAssert.AreEqual(velocity,Read(fog.Velocity,TextureFormat.RGBAFloat));
            var target=Read((RenderTexture)restField.GetValue(fog)); CollectionAssert.AreNotEqual(oldTarget,target);
            profile.settings.puffSize=.8f; fog.Advance(.000001f);
            CollectionAssert.AreNotEqual(target,Read((RenderTexture)restField.GetValue(fog)));
            CollectionAssert.AreEqual(wake,Read(fog.Density));
            fog.ResetSimulation();
            foreach(float d in Read(fog.Density)) Assert.That(float.IsFinite(d) && d>=0,Is.True);
            fog.style=null; fog.ResetSimulation(); CollectionAssert.AreEqual(oldTarget,Read((RenderTexture)restField.GetValue(fog)));
        }

        [TestCase(FogSimulationBackend.Volume3D)]
        [TestCase(FogSimulationBackend.Planar2D)]
        public void CumulusRecoveryRestoresDisturbedDensity(FogSimulationBackend backend)
        {
            var fog=Create(); fog.simulationBackend=backend;
            profile=ScriptableObject.CreateInstance<FogStyleProfile>(); profile.settings=FogStyleSettings.Cumulus;
            profile.settings.fogHeight=.85f; fog.style=profile; fog.Initialize();
            var rest=Read(fog.Density); float[] errors=new float[2];
            for(int run=0;run<2;run++)
            {
                profile.settings.restRecovery=run==0?0:.32f; fog.ResetSimulation();
                fog.AddGust(new Vector3(0,.3f,0),Vector3.right*5,.65f,.5f);
                for(int i=0;i<900;i++) fog.Advance(1f/30);
                var final=Read(fog.Density);
                for(int i=0;i<rest.Length;i++)
                {
                    Assert.That(float.IsFinite(final[i]) && final[i]>=0,Is.True);
                    errors[run]+=Mathf.Abs(final[i]-rest[i]);
                }
            }
            TestContext.WriteLine($"{backend}: no recovery {errors[0]}, with recovery {errors[1]}");
            Assert.That(errors[1],Is.LessThan(errors[0]*.8f));
        }

        [TestCase(FogSimulationBackend.Volume3D)]
        [TestCase(FogSimulationBackend.Planar2D)]
        public void CumulusDetailProxyTracksFilteredRestingTarget(FogSimulationBackend backend)
        {
            var fog=Create(); fog.simulationBackend=backend;
            profile=ScriptableObject.CreateInstance<FogStyleProfile>(); profile.settings=FogStyleSettings.Cumulus;
            profile.settings.fogHeight=.85f; fog.style=profile; fog.Initialize();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            for(int pass=0;pass<2;pass++)
            {
                if(pass==1) { profile.settings.puffSize=.7f; fog.Advance(.000001f); }
                var restTexture=(RenderTexture)typeof(FogVolume).GetField("restDensity",flags).GetValue(fog);
                var detailTexture=(RenderTexture)typeof(FogVolume).GetField("cumulusShape",flags).GetValue(fog);
                var rest=Read(restTexture); var detail=Read(detailTexture,TextureFormat.RGBAFloat);
                for(int z=3;z<detailTexture.volumeDepth;z+=11)
                for(int y=2;y<detailTexture.height;y+=7)
                for(int x=4;x<detailTexture.width;x+=13)
                {
                    var p=new Vector3((x+.5f)*restTexture.width/detailTexture.width-.5f,
                        (y+.5f)*restTexture.height/detailTexture.height-.5f,
                        (z+.5f)*restTexture.volumeDepth/detailTexture.volumeDepth-.5f);
                    var b=new Vector3Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y),Mathf.FloorToInt(p.z));
                    var f=p-(Vector3)b; float expected=0;
                    for(int iz=0;iz<2;iz++) for(int iy=0;iy<2;iy++) for(int ix=0;ix<2;ix++)
                    {
                        int rx=Mathf.Clamp(b.x+ix,0,restTexture.width-1), ry=Mathf.Clamp(b.y+iy,0,restTexture.height-1), rz=Mathf.Clamp(b.z+iz,0,restTexture.volumeDepth-1);
                        float weight=(ix==0?1-f.x:f.x)*(iy==0?1-f.y:f.y)*(iz==0?1-f.z:f.z);
                        expected+=rest[(rz*restTexture.height+ry)*restTexture.width+rx]*weight;
                    }
                    int index=((z*detailTexture.height+y)*detailTexture.width+x)*4;
                    Assert.That(detail[index+1],Is.EqualTo(expected).Within(.002f),"Detail normalization must use the filtered current target, including after a shape edit.");
                    for(int channel=0;channel<4;channel++) Assert.That(float.IsFinite(detail[index+channel]),Is.True);
                }
            }
        }

        [TestCase(FogSimulationBackend.Volume3D)]
        [TestCase(FogSimulationBackend.Planar2D)]
        public void CumulusScatteringDoesNotFillThroughSolidWalls(FogSimulationBackend backend)
        {
            var fog=Create(); fog.simulationBackend=backend;
            profile=ScriptableObject.CreateInstance<FogStyleProfile>(); profile.settings=FogStyleSettings.Cumulus;
            fog.style=profile;
            var pointObject=new GameObject("Point light"); pointObject.transform.SetParent(go.transform);
            var point=pointObject.AddComponent<Light>(); point.type=LightType.Point; point.range=5; point.intensity=3;
            point.transform.position=new Vector3(-1,.5f,0); fog.localLights=new[]{point};
            var sunObject=new GameObject("Directional light"); sunObject.transform.SetParent(go.transform);
            var sun=sunObject.AddComponent<Light>(); sun.type=LightType.Directional;
            sun.transform.rotation=Quaternion.LookRotation(Vector3.right); fog.sun=sun;
            fog.Initialize();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            object owner=backend==FogSimulationBackend.Planar2D?typeof(FogVolume).GetField("planar",flags).GetValue(fog):fog;
            var texture=(RenderTexture)owner.GetType().GetField("lighting",flags).GetValue(owner);
            int x=texture.width*3/4,y=texture.height/2,z=texture.volumeDepth/2;
            int index=((z*texture.height+y)*texture.width+x)*4;
            var open=Read(texture,TextureFormat.RGBAFloat);
            Assert.That(open[index],Is.GreaterThan(.0001f),"The point light must reach the unoccluded probe.");
            Assert.That(open[index+3],Is.GreaterThan(.0001f),"The sun must reach the unoccluded probe.");
            obstacleObject=new GameObject("Opaque separating wall");
            obstacleObject.transform.position=Vector3.up*.5f;
            var wall=obstacleObject.AddComponent<FogObstacle>(); wall.size=new Vector3(.6f,2,4);
            fog.Advance(1f/30);
            var closed=Read(texture,TextureFormat.RGBAFloat);
            for(int channel=0;channel<4;channel++)
                Assert.That(closed[index+channel],Is.EqualTo(0).Within(.000001f),"Diffuse fill must retain solid-wall occlusion.");
        }

        [TestCase(FogSimulationBackend.Volume3D)]
        [TestCase(FogSimulationBackend.Planar2D)]
        public void CloudCoverageFillsRestingGapsWithoutResettingTheLiveWake(FogSimulationBackend backend)
        {
            var fog=Create(); profile=ScriptableObject.CreateInstance<FogStyleProfile>();
            profile.settings=FogStyleSettings.Cumulus; profile.settings.fogHeight=.8f;
            fog.style=profile; fog.simulationBackend=backend; fog.Initialize();
            fog.AddGust(Vector3.up*.2f,Vector3.right*3,.5f,.3f);
            for(int i=0;i<30;i++) fog.Advance(1f/30);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var field=typeof(FogVolume).GetField("restDensity",flags);
            var sparse=Read((RenderTexture)field.GetValue(fog)); var live=Read(fog.Density); int steps=fog.StepCount;
            profile.settings.cloudCoverage=1; fog.Advance(.00001f);
            var filled=Read((RenderTexture)field.GetValue(fog)); double before=0,after=0;
            for(int i=0;i<sparse.Length;i++) { before+=sparse[i]; after+=filled[i]; }
            Assert.That(after,Is.GreaterThan(before*1.1),"Coverage must add a connected resting cloud bed.");
            CollectionAssert.AreEqual(live,Read(fog.Density),"Editing coverage must preserve the active wake.");
            Assert.That(fog.StepCount,Is.EqualTo(steps));
        }

        [TestCase(FogSimulationBackend.Volume3D)]
        [TestCase(FogSimulationBackend.Planar2D)]
        public void EmptyDetailMapCannotCancelTheOtherMapsFade(FogSimulationBackend backend)
        {
            var fog=Create(); profile=ScriptableObject.CreateInstance<FogStyleProfile>();
            profile.settings=FogStyleSettings.Cumulus; profile.settings.fogHeight=.65f; profile.settings.wispOpacity=0;
            fog.style=profile; fog.simulationBackend=backend; fog.Initialize();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var cache=(RenderTexture)typeof(FogVolume).GetField("cumulusShape",flags).GetValue(fog);
            // Known occupied/empty halves isolate fading from procedural shape and camera placement.
            var fixture=new Texture3D(cache.width,cache.height,cache.volumeDepth,TextureFormat.RGBAHalf,true);
            var texels=new Color[cache.width*cache.height*cache.volumeDepth];
            for(int i=0;i<texels.Length;i++) texels[i]=i%cache.width<cache.width/2?new Color(1,1,64,0):new Color(0,0,64,0);
            fixture.SetPixels(texels); fixture.Apply(); Graphics.CopyTexture(fixture,cache); Object.DestroyImmediate(fixture);
            var occupied=new Color(fog.size.x*.25f,fog.size.y*.5f,fog.size.z*.5f,0);
            var empty=new Color(fog.size.x*.75f,fog.size.y*.5f,fog.size.z*.5f,0);
            object owner=backend==FogSimulationBackend.Planar2D?typeof(FogVolume).GetField("planar",flags).GetValue(fog):fog;
            void Fill(string name,Color value)
            {
                var target=(RenderTexture)owner.GetType().GetField(name,flags).GetValue(owner);
                int depth=target.dimension==TextureDimension.Tex3D?target.volumeDepth:1;
                var colors=new Color[target.width*target.height*depth]; for(int i=0;i<colors.Length;i++) colors[i]=value;
                Texture source;
                if(depth>1) { var t=new Texture3D(target.width,target.height,depth,TextureFormat.RGBAHalf,false); t.SetPixels(colors); t.Apply(); source=t; }
                else { var t=new Texture2D(target.width,target.height,TextureFormat.RGBAHalf,false); t.SetPixels(colors); t.Apply(); source=t; }
                Graphics.CopyTexture(source,target); Object.DestroyImmediate(source);
            }
            Fill("coordinatesA",occupied); Fill("coordinatesB",empty);
            var light=(RenderTexture)owner.GetType().GetField("lighting",flags).GetValue(owner);
            var update=typeof(FogVolume).GetMethod("UpdateLighting",flags);
            float[] AtTick(int tick)
            {
                typeof(FogVolume).GetProperty("StepCount").SetValue(fog,tick);
                update.Invoke(fog,null); return Read(light,TextureFormat.RGBAFloat);
            }
            var hidden=AtTick(0); var tiny=AtTick(1); var visible=AtTick(90); double smallChange=0,fullChange=0;
            for(int i=3;i<hidden.Length;i+=4) { smallChange+=System.Math.Abs(tiny[i]-hidden[i]); fullChange+=System.Math.Abs(visible[i]-hidden[i]); }
            TestContext.WriteLine("Fade lighting delta tiny="+smallChange+" full="+fullChange);
            Assert.That(fullChange,Is.GreaterThan(.1),"The occupied map must affect the light march.");
            Assert.That(smallChange,Is.LessThan(fullChange*.08),"A roughly 1% map weight must not reveal its density at full strength.");
        }

        [Test] public void CachedObstacleMaskStillTracksMovementAndRemoval()
        {
            var fog=Create(); obstacleObject=new GameObject("Moving obstacle");
            obstacleObject.transform.position=new Vector3(-.6f,.4f,0);
            var obstacle=obstacleObject.AddComponent<FogObstacle>(); obstacle.size=new Vector3(.6f,.8f,.6f);
            fog.Advance(1f/30); var initial=Read(fog.SolidMask);
            fog.Advance(1f/30); CollectionAssert.AreEqual(initial,Read(fog.SolidMask));
            obstacleObject.transform.position+=Vector3.right*1.2f; fog.Advance(1f/30);
            CollectionAssert.AreNotEqual(initial,Read(fog.SolidMask));
            obstacle.enabled=false; fog.Advance(1f/30);
            foreach(float cell in Read(fog.SolidMask)) Assert.That(cell,Is.Zero);
        }
        [Test] public void OptimizedAndReferenceWorkPreserveDensityVelocityAndLightingExactly()
        {
            var fog=Create(); obstacleObject=new GameObject("Finite range test light");
            var light=obstacleObject.AddComponent<Light>(); light.type=LightType.Point; light.range=1.3f;
            light.transform.position=Vector3.up*.8f; fog.localLights=new[]{light};
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var lightingField=typeof(FogVolume).GetField("lighting",flags);
            fog.ResetSimulation(); fog.AddGust(Vector3.up*.2f,Vector3.right*3,.5f,.5f);
            for(int i=0;i<120;i++) fog.Advance(1f/30);
            var density=Read(fog.Density); var velocity=Read(fog.Velocity,TextureFormat.RGBAFloat);
            var illumination=Read((RenderTexture)lightingField.GetValue(fog),TextureFormat.RGBAFloat);
            typeof(FogVolume).GetField("referenceWork",flags).SetValue(fog,true);
            fog.ResetSimulation(); fog.AddGust(Vector3.up*.2f,Vector3.right*3,.5f,.5f);
            for(int i=0;i<120;i++) fog.Advance(1f/30);
            CollectionAssert.AreEqual(density,Read(fog.Density));
            CollectionAssert.AreEqual(velocity,Read(fog.Velocity,TextureFormat.RGBAFloat));
            CollectionAssert.AreEqual(illumination,Read((RenderTexture)lightingField.GetValue(fog),TextureFormat.RGBAFloat));
        }
    }
}
#endif
