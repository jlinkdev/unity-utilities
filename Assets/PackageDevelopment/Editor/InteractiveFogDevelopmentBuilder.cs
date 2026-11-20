using System.IO;
using jlinkdev.UnityUtilities.InteractiveFog;
using jlinkdev.UnityUtilities.InteractiveFog.Editor;
using jlinkdev.UnityUtilities.InteractiveFog.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.InteractiveFog.Development
{
    public static class InteractiveFogDevelopmentBuilder
    {
        public const string Root = "Assets/PackageDevelopment/InteractiveFog/SampleAuthoring/Mist Sanctuary";
        public const string ScenePath = Root + "/Scenes/Mist Sanctuary.unity";
        [MenuItem("Tools/jlinkdev/Interactive Fog/Rebuild Mist Sanctuary (Development)")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(Root + "/Settings"); Directory.CreateDirectory(Root + "/Materials"); Directory.CreateDirectory(Root + "/Scenes"); AssetDatabase.Refresh();
            var renderer = Asset<UniversalRendererData>(Root + "/Settings/Mist Renderer.asset");
            renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            FogRendererFeature feature = null;
            foreach (var f in renderer.rendererFeatures) if (f is FogRendererFeature fogFeature) feature = fogFeature;
            if (!feature) { feature = ScriptableObject.CreateInstance<FogRendererFeature>(); feature.name = "Interactive Fog"; AssetDatabase.AddObjectToAsset(feature,renderer); renderer.rendererFeatures.Add(feature); }
            FogSetup.ConfigureFeature(feature); FogSetup.SynchronizeFeatureMap(renderer);
            var rendererObject = new SerializedObject(renderer); rendererObject.FindProperty("m_IntermediateTextureMode").intValue=1; rendererObject.ApplyModifiedPropertiesWithoutUndo();
            renderer.SetDirty(); EditorUtility.SetDirty(renderer);
            string pipelinePath=Root + "/Settings/Mist Pipeline.asset";
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if(!pipeline) { pipeline=UniversalRenderPipelineAsset.Create(renderer); AssetDatabase.CreateAsset(pipeline,pipelinePath); }
            pipeline.supportsCameraDepthTexture=true; pipeline.supportsHDR=true; pipeline.msaaSampleCount=1;
            pipeline.shadowDistance=40;
            var pipelineObject=new SerializedObject(pipeline);
            pipelineObject.FindProperty("m_MainLightRenderingMode").intValue=1;
            pipelineObject.FindProperty("m_MainLightShadowsSupported").boolValue=true;
            pipelineObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            var stone = Material("Blue limestone",new Color(.30f,.29f,.27f),.12f);
            var dark = Material("Dark joints",new Color(.075f,.073f,.07f),.15f);
            var pale = Material("Pale edging",new Color(.47f,.45f,.41f),.2f);
            var bronze = Material("Brushed bronze",new Color(.4f,.22f,.075f),.5f,.6f);
            var glow = Material("Lantern glass",new Color(.9f,.52f,.18f),.4f);
            glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor",new Color(2.8f,1.1f,.25f)); EditorUtility.SetDirty(glow);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            scene.name="Mist Sanctuary";
            RenderSettings.ambientMode=AmbientMode.Trilight; RenderSettings.ambientSkyColor=new Color(.31f,.33f,.36f);
            RenderSettings.ambientEquatorColor=new Color(.19f,.20f,.22f); RenderSettings.ambientGroundColor=new Color(.09f,.12f,.15f); RenderSettings.fog=false;
            var architecture=new GameObject("Sanctuary architecture").transform;
            Cube("Foundation",architecture,new Vector3(0,-.21f,0),new Vector3(18,.4f,18),dark);
            for(int x=-8;x<8;x++) for(int z=-8;z<8;z++)
                Cube("Stone floor",architecture,new Vector3(x+.5f,-.035f,z+.5f),new Vector3(.982f,.07f,.982f), (x+z)%7==0?pale:stone);
            for(int i=-1;i<=1;i++)
            {
                float z=i*5f;
                for(int side=-1;side<=1;side+=2)
                {
                    var plinth=Cube("Column plinth",architecture,new Vector3(side*6.1f,.22f,z),new Vector3(1.35f,.44f,1.35f),pale);
                    plinth.AddComponent<FogObstacle>();
                    var pillar=Primitive("Fluted column",PrimitiveType.Cylinder,architecture,new Vector3(side*6.1f,2.4f,z),new Vector3(.85f,2,.85f),stone);
                    pillar.AddComponent<FogObstacle>();
                    Cube("Capital",architecture,new Vector3(side*6.1f,4.5f,z),new Vector3(1.3f,.35f,1.3f),pale);
                    Cube("Lantern bracket",architecture,new Vector3(side*5.35f,1.3f,z),new Vector3(.7f,.12f,.35f),bronze);
                    Primitive("Amber lantern",PrimitiveType.Capsule,architecture,new Vector3(side*5.2f,1.65f,z),new Vector3(.22f,.26f,.22f),glow);
                    var lamp=new GameObject("Warm pool of light").AddComponent<Light>(); lamp.transform.position=new Vector3(side*5.0f,1.7f,z); lamp.type=LightType.Point; lamp.color=new Color(1,.58f,.22f); lamp.intensity=1.5f; lamp.range=4;
                }
            }
            Cube("Apse wall",architecture,new Vector3(0,2.5f,8.2f),new Vector3(17,5,.4f),dark);
            for(int i=-3;i<=3;i++) Cube("Wall ribs",architecture,new Vector3(i*2.4f,2.5f,7.9f),new Vector3(.2f,5,.25f),stone);
            for(int i=0;i<3;i++)
            {
                var stair=Cube("Dais step",architecture,new Vector3(0,.12f+i*.22f,4.8f+i*.2f),new Vector3(4.8f-i*.7f,.24f+i*.44f,3-i*.5f),pale);
                stair.AddComponent<FogObstacle>();
            }
            var monument=Primitive("Monolith",PrimitiveType.Cube,architecture,new Vector3(0,1.6f,5.1f),new Vector3(.75f,1.9f,.75f),bronze); monument.transform.rotation=Quaternion.Euler(0,45,0); monument.AddComponent<FogObstacle>();
            var light=new GameObject("Warm shaft of light").AddComponent<Light>(); light.type=LightType.Directional; light.color=new Color(1,.96f,.87f); light.intensity=1.65f;
            light.transform.rotation=Quaternion.Euler(35,-28,0); light.shadows=LightShadows.Soft; RenderSettings.sun=light;
            var fogGO=new GameObject("Interactive ground fog"); var fog=fogGO.AddComponent<FogVolume>();
            fog.simulation=AssetDatabase.LoadAssetAtPath<ComputeShader>(FogSetup.PackageRoot+"/Runtime/Shaders/FogFluid.compute"); fog.sun=light;
            fog.localLights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            fog.resolution=new Vector3Int(160,32,160); fog.raySteps=112;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Characters/Explorer.prefab");
            var actor = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).transform;
            actor.position = new Vector3(-2, 0, -3);
            var camera=new GameObject("Sanctuary camera").AddComponent<Camera>(); camera.tag="MainCamera"; camera.nearClipPlane=.05f; camera.farClipPlane=80;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.025f,.04f,.055f); camera.fieldOfView=52;
            camera.transform.position=new Vector3(0,6.65f,-11.2f); camera.transform.LookAt(new Vector3(0,.45f,0));
            var cameraData=camera.GetUniversalAdditionalCameraData(); cameraData.renderPostProcessing=true; cameraData.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var grade=Asset<VolumeProfile>(Root+"/Settings/Sanctuary grading.asset");
            if(!grade.TryGet<Tonemapping>(out var tone)) tone=grade.Add<Tonemapping>(); tone.mode.Override(TonemappingMode.ACES);
            if(!grade.TryGet<Bloom>(out var bloom)) bloom=grade.Add<Bloom>(); bloom.intensity.Override(.18f); bloom.threshold.Override(1.1f);
            EditorUtility.SetDirty(grade); var volume=new GameObject("Sanctuary grading").AddComponent<Volume>(); volume.isGlobal=true; volume.sharedProfile=grade;
            FogSetup.AssignRendererToCamera(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset ?? pipeline,renderer,camera);
            var controller=new GameObject("Mist Sanctuary controls").AddComponent<MistSanctuary>(); controller.fog=fog; controller.actor=actor; controller.view=camera; controller.bladeTip = actor.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand).Find("Sword/Blade tip");
            controller.referenceStyle=AssetDatabase.LoadAssetAtPath<FogStyleProfile>(FogSetup.PackageRoot+"/Runtime/Styles/Sanctuary Ground Mist.asset");
            EditorSceneManager.SaveScene(scene,ScenePath); AssetDatabase.SaveAssets();
            Debug.Log("FOG_SAMPLE_BUILT "+ScenePath);
        }
        public static void BuildPlayer()
        {
            BuildPlayer(true);
        }
        /// <summary>Build an isolated review player without overwriting published or host sample assets.</summary>
        public static void BuildPreviewPlayer()
        {
            BuildPlayer(false);
        }
        static void BuildPlayer(bool publish)
        {
            BuildAll();
            PlayerSettings.colorSpace=ColorSpace.Linear; PlayerSettings.defaultScreenWidth=1280; PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.runInBackground=true;
            PlayerSettings.enableFrameTimingStats=true;
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"/Settings/Mist Pipeline.asset");
            var previous=GraphicsSettings.defaultRenderPipeline; var quality=QualitySettings.renderPipeline;
            try
            {
                GraphicsSettings.defaultRenderPipeline=pipeline; QualitySettings.renderPipeline=pipeline;
                FogSetup.AssignRendererToCamera(pipeline,AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root+"/Settings/Mist Renderer.asset"),Object.FindFirstObjectByType<MistSanctuary>().view);
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath},locationPathName="Builds/MistSanctuary/MistSanctuary.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development });
                if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Fog player build failed: "+report.summary.result);
                if(publish) Publish();
            }
            finally { GraphicsSettings.defaultRenderPipeline=previous; QualitySettings.renderPipeline=quality; }
        }
        public static void Publish()
        {
            string package=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(FogVolume).Assembly).resolvedPath;
            CopyTree(Root,Path.Combine(package,"Samples~/Mist Sanctuary"));
            string host=Path.GetFullPath(Path.Combine(package,"../..",Root));
            if(Path.GetFullPath(Root)!=host) CopyTree(Root,host);
            Debug.Log("FOG_SAMPLE_PUBLISHED "+package);
        }
        static void CopyTree(string source,string destination)
        {
            Directory.CreateDirectory(destination);
            foreach(string file in Directory.GetFiles(source)) File.Copy(file,Path.Combine(destination,Path.GetFileName(file)),true);
            foreach(string dir in Directory.GetDirectories(source)) CopyTree(dir,Path.Combine(destination,Path.GetFileName(dir)));
        }
        static T Asset<T>(string path) where T:ScriptableObject
        { var asset=AssetDatabase.LoadAssetAtPath<T>(path); if(!asset) {asset=ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset,path);} return asset; }
        static Material Material(string name,Color color,float smoothness,float metallic=0)
        {
            string path=Root+"/Materials/"+name+".mat"; var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) {material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color); material.SetFloat("_Smoothness",smoothness); material.SetFloat("_Metallic",metallic); EditorUtility.SetDirty(material); return material;
        }
        static GameObject Cube(string name,Transform parent,Vector3 position,Vector3 scale,Material material) => Primitive(name,PrimitiveType.Cube,parent,position,scale,material);
        static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material; return go;
        }
    }
}
