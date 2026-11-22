using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace jlinkdev.UnityUtilities.InteractiveFog.Tests
{
    public sealed class FogFluidTests
    {
        GameObject go;
        FogVolume fog;
        [SetUp] public void Setup()
        {
            if(!SystemInfo.supportsComputeShaders) Assert.Ignore("A compute-capable GPU is required.");
            ShaderUtil.allowAsyncCompilation=false;
            go=new GameObject("Fog test"); fog=go.AddComponent<FogVolume>();
            fog.simulation=AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.jlinkdev.volumetric-fog-and-rain/Runtime/InteractiveFog/Shaders/FogFluid.compute");
            Assert.That(fog.simulation,Is.Not.Null);
            fog.size=new Vector3(4,1.5f,4); fog.resolution=new Vector3Int(32,12,32); fog.automaticSimulation=false;
            fog.Initialize(); Assert.That(fog.IsReady,Is.True);
        }
        [TearDown] public void Cleanup() { if(go) Object.DestroyImmediate(go); }
        static float[] Read(RenderTexture texture,TextureFormat format=TextureFormat.RFloat)
        {
            var request=AsyncGPUReadback.Request(texture,0,format); request.WaitForCompletion();
            Assert.That(request.hasError,Is.False);
            int perLayer=request.GetData<float>(0).Length;
            var result=new float[perLayer*request.layerCount];
            for(int layer=0;layer<request.layerCount;layer++)
            { var data=request.GetData<float>(layer); for(int i=0;i<perLayer;i++) result[layer*perLayer+i]=data[i]; }
            Assert.That(result.Length,Is.EqualTo(texture.width*texture.height*texture.volumeDepth*(format==TextureFormat.RGBAFloat?4:1)));
            return result;
        }
        [Test] public void InitializationAndResetProduceRepeatableFiniteDensity()
        {
            var initial=Read(fog.Density); float mass=0;
            foreach(float d in initial) { Assert.That(float.IsFinite(d)); Assert.That(d,Is.InRange(0,2)); mass+=d; }
            Assert.That(mass,Is.GreaterThan(100));
            fog.AddGust(new Vector3(0,.4f,0),Vector3.right*6,1,.2f); fog.Advance(.1f);
            var disturbed=Read(fog.Density); double difference=0;
            for(int i=0;i<initial.Length;i++) difference+=System.Math.Abs(initial[i]-disturbed[i]);
            Assert.That(difference,Is.GreaterThan(1));
            fog.ResetSimulation(); CollectionAssert.AreEqual(initial,Read(fog.Density)); Assert.That(fog.StepCount,Is.Zero);
        }
        [Test] public void FixedClockClampsHitchesAndDoesNotAdvanceForInvalidTime()
        {
            fog.Advance(float.NaN); fog.Advance(-1); Assert.That(fog.StepCount,Is.Zero);
            fog.Advance(1f/60); Assert.That(fog.StepCount,Is.Zero);
            fog.Advance(1f/60); Assert.That(fog.StepCount,Is.EqualTo(1));
            fog.Advance(10); Assert.That(fog.StepCount,Is.EqualTo(4));
        }
        [TestCase(0f, .95f, 1f)]
        [TestCase(1f, .10f, .85f)]
        public void GroundPatchinessControlsFloorCoverage(float patchiness, float minimumCoverage, float maximumCoverage)
        {
            fog.groundPatchiness = patchiness;
            fog.size=new Vector3(16,1.5f,16); fog.resolution=new Vector3Int(64,12,64); fog.Initialize();
            var density=Read(fog.Density); int covered=0,total=0;
            for(int z=2;z<62;z++) for(int x=2;x<62;x++)
            {
                total++;
                if(density[(z*fog.resolution.y)*fog.resolution.x+x]>.25f) covered++;
            }
            Assert.That((float)covered / total, Is.InRange(minimumCoverage, maximumCoverage));
        }
        [Test] public void QuietFieldDoesNotInflateOrLoseMostOfItsMass()
        {
            fog.wind=Vector3.zero; fog.vorticity=0; fog.replenishment=0; fog.restRecovery=0; fog.ResetSimulation();
            double before=0,after=0;
            foreach(var value in Read(fog.Density)) before+=value;
            for(int i=0;i<90;i++) fog.Advance(1f/30);
            foreach(var value in Read(fog.Density)) { Assert.That(float.IsFinite(value)); after+=value; }
            Assert.That(after/before,Is.InRange(.75,1.25),"Advection must not turn sparse fog into a solid slab.");
        }
        [Test] public void PressureIterationsReduceDivergenceAfterImpulse()
        {
            double Measure(int iterations)
            {
                fog.pressureIterations=iterations; fog.vorticity=0; fog.wind=Vector3.zero; fog.ResetSimulation();
                fog.AddGust(new Vector3(0,.6f,0),new Vector3(4,1,0),.75f,.1f); fog.Advance(1f/30);
                var velocity=Read(fog.Velocity,TextureFormat.RGBAFloat); var n=fog.resolution;
                int Index(int x,int y,int z,int c) => ((z*n.y+y)*n.x+x)*4+c;
                double divergence=0;
                for(int z=2;z<n.z-2;z++) for(int y=2;y<n.y-2;y++) for(int x=2;x<n.x-2;x++)
                {
                    float d=(velocity[Index(x+1,y,z,0)]-velocity[Index(x-1,y,z,0)])/(2*fog.size.x/n.x)+
                        (velocity[Index(x,y+1,z,1)]-velocity[Index(x,y-1,z,1)])/(2*fog.size.y/n.y)+
                        (velocity[Index(x,y,z+1,2)]-velocity[Index(x,y,z-1,2)])/(2*fog.size.z/n.z);
                    divergence+=d*d;
                }
                return divergence;
            }
            double weak=Measure(1), strong=Measure(40);
            Assert.That(strong,Is.LessThan(weak*.85),"Projection should measurably reduce compression.");
        }
        [Test] public void OrientedObstacleRemovesDensityInsideSolidCells()
        {
            var obstacle=new GameObject("Solid box");
            try
            {
                obstacle.transform.position=new Vector3(0,.5f,0); obstacle.transform.rotation=Quaternion.Euler(0,35,0);
                obstacle.transform.localScale=new Vector3(1.2f,1,1);
                obstacle.AddComponent<FogObstacle>();
                fog.ResetSimulation(); fog.Advance(.1f);
                var mask=Read(fog.SolidMask); var density=Read(fog.Density); int count=0;
                for(int i=0;i<mask.Length;i++) if(mask[i]>.5f) { count++; Assert.That(density[i],Is.EqualTo(0)); }
                Assert.That(count,Is.GreaterThan(100));
            }
            finally { Object.DestroyImmediate(obstacle); }
        }
        [Test] public void GustCreatesPersistentThreeDimensionalVelocityWithoutANewSource()
        {
            fog.wind=Vector3.zero; fog.vorticity=0; fog.ResetSimulation();
            fog.AddGust(new Vector3(0,.45f,0),new Vector3(5,2,1),.8f,.04f);
            fog.Advance(.1f); fog.Advance(.1f);
            var v=Read(fog.Velocity,TextureFormat.RGBAFloat); float energy=0,vertical=0;
            for(int i=0;i<v.Length;i+=4) { Assert.That(float.IsFinite(v[i])); energy+=v[i]*v[i]+v[i+1]*v[i+1]+v[i+2]*v[i+2]; vertical+=v[i+1]*v[i+1]; }
            Assert.That(energy,Is.GreaterThan(1)); Assert.That(vertical,Is.GreaterThan(.1f));
        }
        [Test] public void ReinitializeReleasesOldTexturesAndUsesNewResolution()
        {
            var old=fog.Density; fog.resolution=new Vector3Int(40,16,24); fog.Initialize();
            Assert.That(old==null,Is.True); Assert.That(fog.Density.width,Is.EqualTo(40)); Assert.That(fog.Density.volumeDepth,Is.EqualTo(24));
            go.SetActive(false); Assert.That(fog.IsReady,Is.False);
        }
        [Test] public void StationaryDefaultInteractorDoesNotEraseOrFreezeFog()
        {
            for(int i=0;i<60;i++) fog.Advance(1f/30);
            var control=Read(fog.Density); fog.ResetSimulation();
            var actor=new GameObject("Stationary actor");
            try
            {
                actor.AddComponent<FogInteractor>();
                for(int i=0;i<60;i++) fog.Advance(1f/30);
                CollectionAssert.AreEqual(control,Read(fog.Density));
            }
            finally { Object.DestroyImmediate(actor); }
        }
        [Test] public void MovingZeroDragInteractorMatchesTheUnforcedField()
        {
            for(int i=0;i<60;i++) fog.Advance(1f/30);
            var density=Read(fog.Density);
            var velocity=Read(fog.Velocity,TextureFormat.RGBAFloat);
            var coordinates=Read(fog.DetailCoordinates,TextureFormat.RGBAFloat);
            fog.ResetSimulation();
            var actor=new GameObject("Zero drag actor");
            try
            {
                var interactor=actor.AddComponent<FogInteractor>(); interactor.drag=0; interactor.clearing=0;
                for(int i=0;i<60;i++)
                {
                    float t=i/30f; actor.transform.position=new Vector3(Mathf.Sin(t*2)*.9f,0,Mathf.Cos(t*2)*.6f);
                    interactor.SampleMotion(1f/30); fog.Advance(1f/30);
                }
                CollectionAssert.AreEqual(velocity,Read(fog.Velocity,TextureFormat.RGBAFloat),"Zero drag must not inject a rolling wake.");
                CollectionAssert.AreEqual(coordinates,Read(fog.DetailCoordinates,TextureFormat.RGBAFloat),"Zero drag must not seed wake activity.");
                CollectionAssert.AreEqual(density,Read(fog.Density));
            }
            finally { Object.DestroyImmediate(actor); }
        }
        [Test] public void SweptForceTransportsDetailAndRemainsFiniteAcrossMapResets()
        {
            var initial=Read(fog.DetailCoordinates,TextureFormat.RGBAFloat);
            fog.AddSweep(new Vector3(-1,.3f,0),new Vector3(1,.3f,0),Vector3.forward*6,.35f,.1f);
            for(int i=0;i<30;i++) fog.Advance(1f/30);
            var transported=Read(fog.DetailCoordinates,TextureFormat.RGBAFloat); double difference=0;
            for(int i=0;i<initial.Length;i++) difference+=System.Math.Abs(initial[i]-transported[i]);
            Assert.That(difference,Is.GreaterThan(10),"Fine detail coordinates must follow the fluid.");
            for(int i=0;i<360;i++) fog.Advance(1f/30);
            foreach(float d in Read(fog.Density)) Assert.That(d,Is.InRange(0,2));
            foreach(float v in Read(fog.Velocity,TextureFormat.RGBAFloat)) Assert.That(float.IsFinite(v));
            foreach(float c in Read(fog.DetailCoordinates,TextureFormat.RGBAFloat)) Assert.That(float.IsFinite(c));
        }
        [Test] public void LongRunningQuietFogStaysNearTheGround()
        {
            fog.replenishment=0; fog.restRecovery=0; fog.ResetSimulation();
            double initialMass=0; foreach(float d in Read(fog.Density)) initialMass+=d;
            for(int i=0;i<600;i++) fog.Advance(1f/30);
            var density=Read(fog.Density); double mass=0,height=0;
            for(int i=0;i<density.Length;i++)
            {
                int y=(i/fog.resolution.x)%fog.resolution.y;
                mass+=density[i]; height+=density[i]*(y+.5)*fog.size.y/fog.resolution.y;
            }
            TestContext.WriteLine("Mass ratio="+(mass/initialMass)+" density-weighted height="+(height/mass));
            Assert.That(mass/initialMass,Is.InRange(.995,1.005),"Closed transport must conserve density.");
            Assert.That(height/mass,Is.LessThan(fog.fogHeight*.65));
        }
        [Test] public void MovingBodyDisplacesDensityWithoutDeletingIt()
        {
            fog.replenishment=0; fog.restRecovery=0; fog.ResetSimulation();
            var initial=Read(fog.Density); double before=0; foreach(float d in initial) before+=d;
            var actor=new GameObject("Moving actor");
            try
            {
                var interactor=actor.AddComponent<FogInteractor>();
                for(int i=0;i<300;i++)
                {
                    float t=i/30f; actor.transform.position=new Vector3(Mathf.Sin(t*2)*.9f,0,Mathf.Cos(t*2)*.6f);
                    interactor.SampleMotion(1f/30); fog.Advance(1f/30);
                }
                var moved=Read(fog.Density); double after=0,difference=0;
                for(int i=0;i<moved.Length;i++) { after+=moved[i]; difference+=System.Math.Abs(moved[i]-initial[i]); }
                TestContext.WriteLine("Moving body mass ratio="+(after/before));
                Assert.That(after/before,Is.InRange(.995,1.005));
                Assert.That(difference/before,Is.GreaterThan(.1),"Motion must actually rearrange the fog.");
            }
            finally { Object.DestroyImmediate(actor); }
        }
        [Test] public void QuietRecoveryRebuildsRestingDensityAfterADisturbance()
        {
            double Measure(float rate)
            {
                fog.replenishment=0; fog.restRecovery=rate; fog.ResetSimulation();
                var initial=Read(fog.Density);
                fog.AddSweep(new Vector3(-1,.3f,0),new Vector3(1,.3f,0),new Vector3(4,1,2),.6f,1);
                for(int i=0;i<1200;i++) fog.Advance(1f/30);
                var settled=Read(fog.Density); double difference=0,mass=0,initialMass=0;
                for(int i=0;i<initial.Length;i++)
                { Assert.That(float.IsFinite(settled[i])); difference+=System.Math.Abs(settled[i]-initial[i]); mass+=settled[i]; initialMass+=initial[i]; }
                if(rate>0) Assert.That(mass/initialMass,Is.InRange(.85,1.15),"Recovery must not continually inflate the layer.");
                return difference/initialMass;
            }
            double unassisted=Measure(0),recovered=Measure(.25f);
            TestContext.WriteLine("Rest-pattern error without recovery="+unassisted+" with recovery="+recovered);
            Assert.That(unassisted,Is.GreaterThan(.2),"The control must retain a meaningful disturbance/drift.");
            Assert.That(recovered,Is.LessThan(unassisted*.3),"Idle recovery must rebuild density ridges, not just stop the velocity.");
        }
        [Test] public void RecoveryLeavesAFreshWakeVisible()
        {
            double Measure(float rate)
            {
                fog.replenishment=0; fog.restRecovery=rate; fog.ResetSimulation();
                var initial=Read(fog.Density);
                fog.AddGust(new Vector3(0,.3f,0),Vector3.right*5,1,2);
                for(int i=0;i<30;i++) fog.Advance(1f/30);
                var active=Read(fog.Density); double difference=0;
                for(int z=10;z<22;z++) for(int y=0;y<5;y++) for(int x=10;x<22;x++)
                { int i=(z*fog.resolution.y+y)*fog.resolution.x+x; difference+=System.Math.Abs(active[i]-initial[i]); }
                return difference;
            }
            double unassisted=Measure(0),recovered=Measure(2);
            TestContext.WriteLine("Active wake difference without recovery="+unassisted+" with recovery="+recovered);
            Assert.That(unassisted,Is.GreaterThan(10));
            Assert.That(recovered,Is.GreaterThan(unassisted*.8),"Even fast recovery must not erase a fresh source's wake.");
        }
        [Test] public void DetailMapResetOccursOnTheExactSimulationTick()
        {
            var initial=Read(fog.DetailCoordinates,TextureFormat.RGBAFloat);
            fog.AddGust(new Vector3(0,.3f,0),Vector3.right*3,.8f,.3f);
            for(int i=0;i<180;i++) fog.Advance(1f/30);
            var reset=Read(fog.DetailCoordinates,TextureFormat.RGBAFloat);
            for(int i=0;i<initial.Length;i+=4) for(int c=0;c<3;c++) Assert.That(reset[i+c],Is.EqualTo(initial[i+c]));
            fog.Advance(1f/30); var next=Read(fog.DetailCoordinates,TextureFormat.RGBAFloat); double difference=0;
            for(int i=0;i<initial.Length;i+=4) for(int c=0;c<3;c++) difference+=System.Math.Abs(next[i+c]-initial[i+c]);
            Assert.That(difference,Is.GreaterThan(.1));
        }
        [TestCase(false),TestCase(true)] public void WakeActivityDecaysAfterTheSourceExpires(bool radial)
        {
            fog.AddGust(new Vector3(0,.3f,0),radial?Vector3.zero:Vector3.right*3,.8f,.2f);
            for(int i=0;i<15;i++) fog.Advance(1f/30);
            double before=0,after=0;
            var active=Read(fog.DetailCoordinates,TextureFormat.RGBAFloat);
            for(int i=3;i<active.Length;i+=4) before+=active[i];
            for(int i=0;i<240;i++) fog.Advance(1f/30);
            var settled=Read(fog.DetailCoordinates,TextureFormat.RGBAFloat);
            for(int i=3;i<settled.Length;i+=4) after+=settled[i];
            Assert.That(before,Is.GreaterThan(1));
            Assert.That(after,Is.LessThan(before*.1),"Wake stirring must not sustain itself indefinitely.");
        }
        [Test] public void TeleportDoesNotBecomeAnUnboundedImpulse()
        {
            var actor=new GameObject("Actor");
            try
            {
                var interactor=actor.AddComponent<FogInteractor>();
                actor.transform.position=Vector3.right*100; interactor.SampleMotion(1f/30);
                Assert.That(interactor.Velocity,Is.EqualTo(Vector3.zero));
                actor.transform.position+=Vector3.right*.1f; interactor.SampleMotion(1f/30);
                Assert.That(interactor.Velocity.x,Is.EqualTo(3).Within(.01));
            }
            finally { Object.DestroyImmediate(actor); }
        }
    }
}
