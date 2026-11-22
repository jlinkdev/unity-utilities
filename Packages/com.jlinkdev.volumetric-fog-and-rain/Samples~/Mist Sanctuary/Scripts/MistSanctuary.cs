using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.InteractiveFog.Samples
{
    [DefaultExecutionOrder(-200)]
    public sealed class MistSanctuary : MonoBehaviour
    {
        public FogVolume fog;
        [Tooltip("Used only by the --fog-profile capture argument. Normal Play Mode uses Fog Volume > Style.")]
        public FogStyleProfile referenceStyle;
        public Transform actor;
        public Camera view;
        public Transform bladeTip;
        [Tooltip("Closed world-space animation path. At least four points are required.")]
        public Vector3[] pathPoints = { new(-2,0,-3), new(1.8f,0,-3), new(2.8f,0,.8f), new(0,0,2.4f), new(-4.7f,0,.3f), new(-3.3f,0,-2) };
        public bool lowCamera;
        [Tooltip("Walk the first pass, then hold still at ten seconds to inspect fog recovery.")]
        public bool recoveryPreview;
        static readonly int Speed = Animator.StringToHash("Speed");
        static readonly int Corner = Animator.StringToHash("Corner");
        static readonly int Swipe = Animator.StringToHash("Base Layer.Sword Swipe");
        Animator explorer;
        float time;
        Vector3 widePosition;
        Quaternion wideRotation;
        Vector3 bladePrevious;
        Vector3 previousPosition;
        string capturePath;
        RenderTexture captureTarget;
        bool captureSequence;
        bool fullRate, captureLowCamera;
        Texture2D captureImage;
        bool benchmark;
        FogStyleProfile captureStyle;
        int captureFrames = 1200;
        readonly System.Collections.Generic.List<float> frameMilliseconds = new();
        readonly System.Collections.Generic.List<float> gpuMilliseconds = new();
        readonly System.Collections.Generic.List<float> marchMilliseconds = new();
        readonly System.Collections.Generic.List<float> resolveMilliseconds = new();
        readonly FrameTiming[] timing = new FrameTiming[1];
        void OnEnable()
        {
            time = 0;
            if (view) { widePosition = view.transform.position; wideRotation = view.transform.rotation; }
            if (actor)
            {
                actor.position = SamplePath(0);
                explorer = actor.GetComponent<Animator>();
            }
            if (bladeTip) bladePrevious = bladeTip.position;
            previousPosition = actor ? actor.position : Vector3.zero;
        }
        void OnDisable() { if (view) view.transform.SetPositionAndRotation(widePosition,wideRotation); }
        void Start()
        {
            var args = System.Environment.GetCommandLineArgs();
            if(System.Array.IndexOf(args,"--fog-dry-ice")>=0)
            {
                captureStyle=ScriptableObject.CreateInstance<FogStyleProfile>();
                captureStyle.settings=FogStyleSettings.DryIce;
                fog.style=captureStyle; fog.ResetSimulation();
            }
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "--fog-capture") capturePath = args[i + 1];
            if (!string.IsNullOrEmpty(capturePath))
            {
                Time.captureDeltaTime = 1f/60;
                captureLowCamera=System.Array.IndexOf(args,"--fog-low-camera")>=0;
                if(System.Array.IndexOf(args,"--fog-ortho")>=0) { view.orthographic=true; view.orthographicSize=4.8f; }
                captureSequence=System.Array.IndexOf(args,"--fog-sequence")>=0;
                fullRate=System.Array.IndexOf(args,"--fog-full-rate")>=0;
                recoveryPreview=System.Array.IndexOf(args,"--fog-recovery")>=0 || recoveryPreview;
                if(recoveryPreview) captureFrames=2400;
                if(System.Array.IndexOf(args,"--fog-cumulus")>=0)
                {
                    if(captureStyle) Destroy(captureStyle);
                    captureStyle=ScriptableObject.CreateInstance<FogStyleProfile>();
                    captureStyle.settings=FogStyleSettings.Cumulus; fog.style=captureStyle; fog.ResetSimulation();
                }
                if(System.Array.IndexOf(args,"--fog-profile")>=0 && referenceStyle)
                { if(captureStyle) Destroy(captureStyle);captureStyle=null;fog.style=referenceStyle;fog.ResetSimulation(); }
                if(fog.style && (System.Array.IndexOf(args,"--fog-no-recovery")>=0 || System.Array.IndexOf(args,"--fog-coarse")>=0))
                { if(fog.style!=captureStyle) { if(captureStyle) Destroy(captureStyle); captureStyle=Instantiate(fog.style); fog.style=captureStyle; } }
                if(System.Array.IndexOf(args,"--fog-no-recovery")>=0)
                { if(fog.style) fog.style.settings.restRecovery=0; else fog.restRecovery=0; }
                if(System.Array.IndexOf(args,"--fog-no-simulation")>=0) fog.automaticSimulation=false;
                if(System.Array.IndexOf(args,"--fog-no-render")>=0) { fog.gameObject.layer=31; view.cullingMask&=~(1<<31); }
                if(System.Array.IndexOf(args,"--fog-no-lights")>=0) fog.localLights=System.Array.Empty<Light>();
                if(System.Array.IndexOf(args,"--fog-default-grid")>=0) { fog.resolution=new Vector3Int(128,24,128); fog.raySteps=96; fog.Initialize(); }
                if(System.Array.IndexOf(args,"--fog-half")>=0) fog.renderResolution=FogRenderResolution.Half;
                if(System.Array.IndexOf(args,"--fog-quarter")>=0) fog.renderResolution=FogRenderResolution.Quarter;
                if(System.Array.IndexOf(args,"--fog-planar")>=0) { fog.simulationBackend=FogSimulationBackend.Planar2D; fog.Initialize(); }
                if(System.Array.IndexOf(args,"--fog-fine-render")>=0) { fog.raySteps=192; fog.renderResolution=FogRenderResolution.Full; }
                benchmark=System.Array.IndexOf(args,"--fog-benchmark")>=0;
                FogRendererFeature.EnableProfiling=benchmark;
                if (benchmark) Application.targetFrameRate=-1;
                if (System.Array.IndexOf(args,"--fog-disabled")>=0) fog.enabled=false;
                if (System.Array.IndexOf(args,"--fog-coarse")>=0)
                {
                    if(fog.style) { fog.style.settings.turbulence=0; fog.style.settings.wispDisplacement=0; fog.style.settings.detail=.22f; }
                    else { fog.turbulence=0; fog.wispDisplacement=0; fog.detail=.22f; }
                }
                captureTarget = new RenderTexture(System.Array.IndexOf(args,"--fog-1080")>=0?1920:1280,System.Array.IndexOf(args,"--fog-1080")>=0?1080:720,24,RenderTextureFormat.ARGB32); captureTarget.Create(); view.targetTexture=captureTarget; view.enabled=false;
                captureImage=new Texture2D(captureTarget.width,captureTarget.height,TextureFormat.RGB24,false);
                StartCoroutine(Capture());
            }
        }
        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, .05f); time += dt;
            if (!explorer || !view || dt <= 0) return;
            float t=recoveryPreview?Mathf.Min(time,10):time%20;
            float progress=t<2?0:t<5?Mathf.Lerp(0,.2f,(t-2)/3):t<8?Mathf.Lerp(.2f,.52f,(t-5)/3):
                t<10?Mathf.Lerp(.52f,.65f,(t-8)/2):Mathf.Lerp(.65f,1,Mathf.InverseLerp(14.75f,18,t));
            actor.position=SamplePath(progress);
            if(t>=12 && t-dt<12) explorer.CrossFadeInFixedTime(Swipe, .08f, 0, 0);
            bool useLowCamera=lowCamera || captureLowCamera || t>=18;
            Vector3 delta = actor.position - previousPosition;
            float previousYaw = actor.eulerAngles.y;
            if (delta.sqrMagnitude > .00001f) actor.rotation = Quaternion.Slerp(actor.rotation, Quaternion.LookRotation(delta), dt * 9);
            previousPosition = actor.position;
            float speed = delta.magnitude / dt;
            float turnRate = Mathf.Lerp(150, 120, Mathf.InverseLerp(3.2f, 5.5f, speed));
            float corner = Mathf.DeltaAngle(previousYaw, actor.eulerAngles.y) / (dt * turnRate);
            explorer.SetFloat(Speed, speed, .08f, dt);
            explorer.SetFloat(Corner, Mathf.Clamp(corner, -1, 1), .07f, dt);
            if (useLowCamera)
            {
                var lowRotation = Quaternion.Euler(7,0,0);
                view.transform.SetPositionAndRotation(new Vector3(0,.5f,.75f) - lowRotation * Vector3.forward * 8.8f,lowRotation);
            }
            else view.transform.SetPositionAndRotation(widePosition,wideRotation);
        }
        public Vector3 SamplePath(float progress)
        {
            if(pathPoints==null || pathPoints.Length<4) return actor?actor.position:Vector3.zero;
            float u=Mathf.Repeat(progress,1)*pathPoints.Length; int index=Mathf.FloorToInt(u); float t=u-index;
            Vector3 a=pathPoints[(index+pathPoints.Length-1)%pathPoints.Length],b=pathPoints[index],
                c=pathPoints[(index+1)%pathPoints.Length],d=pathPoints[(index+2)%pathPoints.Length];
            return .5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
        }
        void LateUpdate()
        {
            if (!bladeTip || !explorer || !fog) return;
            // Read the animated tip after Mecanim poses the hand and before the fog advances.
            Vector3 tip = bladeTip.position;
            var state = explorer.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash == Swipe && state.normalizedTime >= .12f && state.normalizedTime <= .34f && Time.deltaTime > 0)
                fog.AddSweep(bladePrevious, tip, Vector3.ClampMagnitude((tip - bladePrevious) / Time.deltaTime, 10), .42f, .06f);
            bladePrevious = tip;
        }
        IEnumerator Capture()
        {
            Directory.CreateDirectory(capturePath);
            var clock=System.Diagnostics.Stopwatch.StartNew(); double lastFrame=clock.Elapsed.TotalMilliseconds;
            // Record a sequence after warmup; screenshots are completed before quitting.
            bool savedPrevious=false;
            for (int i = 0; i < captureFrames; i++)
            {
                yield return new WaitForEndOfFrame();
                view.Render(); // Render after all simulation LateUpdates, including 30 Hz interpolation.
                if(benchmark)
                {
                    // Synchronize to completed GPU work. This reports conservative whole-frame wall time,
                    // not CPU submission time or an unsupported GPU timer.
                    var fence=AsyncGPUReadback.Request(captureTarget,0,0,1,0,1,0,1,TextureFormat.RGBA32);
                    fence.WaitForCompletion();
                }
                double now=clock.Elapsed.TotalMilliseconds;
                if(i>120 && !savedPrevious) frameMilliseconds.Add((float)(now-lastFrame));
                lastFrame=now;
                FrameTimingManager.CaptureFrameTimings();
                if(i>120 && FrameTimingManager.GetLatestTimings(1,timing)>0 && timing[0].gpuFrameTime>0) gpuMilliseconds.Add((float)timing[0].gpuFrameTime);
                if(i>120 && FogRendererFeature.RaymarchGpuMilliseconds>0) marchMilliseconds.Add(FogRendererFeature.RaymarchGpuMilliseconds);
                if(i>120 && FogRendererFeature.ReconstructionGpuMilliseconds>0) resolveMilliseconds.Add(FogRendererFeature.ReconstructionGpuMilliseconds);
                savedPrevious=fullRate || i==120 || i==360 || i==540 || i==780 || i==1140 || (recoveryPreview && (i==600 || i==1440 || i==2280)) || (captureSequence && i%12==0);
                if (savedPrevious)
                {
                    var active=RenderTexture.active; RenderTexture.active=captureTarget;
                    var image = captureImage;
                    image.ReadPixels(new Rect(0,0,captureTarget.width,captureTarget.height),0,0); image.Apply(); RenderTexture.active=active;
                    File.WriteAllBytes(Path.Combine(capturePath, "fog-" + i.ToString("D4") + ".png"), image.EncodeToPNG());
                }

            }
            File.WriteAllText(Path.Combine(capturePath, "capture.json"), JsonUtility.ToJson(new CaptureReport {
                device=SystemInfo.graphicsDeviceName, api=SystemInfo.graphicsDeviceType.ToString(), width=captureTarget.width, height=captureTarget.height,
                steps=fog.StepCount, textureMiB=fog.EstimatedTextureBytes/1048576f, seconds=Time.timeSinceLevelLoad,
                medianFrameMs=Percentile(frameMilliseconds,.5f),p95FrameMs=Percentile(frameMilliseconds,.95f),meanFrameMs=Mean(frameMilliseconds),medianGpuMs=Percentile(gpuMilliseconds,.5f),meanRaymarchGpuMs=Mean(marchMilliseconds),raymarchGpuSamples=marchMilliseconds.Count,meanReconstructionGpuMs=Mean(resolveMilliseconds),renderResolution=fog.renderResolution.ToString(),simulationBackend=fog.simulationBackend.ToString(),
                measurement=benchmark?"GPU-synchronized whole-frame wall time, offscreen development player":"Offscreen capture; not a GPU benchmark" }, true));
            Application.Quit(0);
        }
        void OnDestroy() { FogRendererFeature.EnableProfiling=false; if(captureStyle) Destroy(captureStyle); if(captureImage) Destroy(captureImage); if(captureTarget) { if(view) view.targetTexture=null; captureTarget.Release(); Destroy(captureTarget); } }
        static float Percentile(System.Collections.Generic.List<float> values,float p) { if(values.Count==0) return -1; values.Sort(); return values[Mathf.Min(values.Count-1,(int)(values.Count*p))]; }
        static float Mean(System.Collections.Generic.List<float> values) { if(values.Count==0)return -1; float total=0; foreach(float v in values)total+=v;return total/values.Count; }
        [System.Serializable] struct CaptureReport { public string device, api,measurement,renderResolution,simulationBackend; public int width, height, steps,raymarchGpuSamples; public float textureMiB, seconds,medianFrameMs,p95FrameMs,meanFrameMs,medianGpuMs,meanRaymarchGpuMs,meanReconstructionGpuMs; }
    }
}
