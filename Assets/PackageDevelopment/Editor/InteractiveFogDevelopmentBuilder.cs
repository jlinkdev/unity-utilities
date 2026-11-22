using System.IO;
using jlinkdev.UnityUtilities.InteractiveFog;
using jlinkdev.UnityUtilities.InteractiveFog.Editor;
using jlinkdev.UnityUtilities.InteractiveFog.EditableSamples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.InteractiveFog.Development
{
    public static class InteractiveFogDevelopmentBuilder
    {
        public const string Root = "Assets/PackageSamples/com.jlinkdev.interactive-fog/Mist Sanctuary";
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
            var stone = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.jlinkdev.sample-framework/Runtime/Presentation/Environment/Architectural Grid.mat");
            var dark = Material("Dark joints",new Color(.115f,.13f,.15f),.15f);
            var bronze = Material("Brushed bronze",new Color(.4f,.22f,.075f),.5f,.6f);
            var glow = Material("Lantern glass",new Color(.9f,.52f,.18f),.4f);
            glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor",new Color(5.1f,3.95f,2.25f)); EditorUtility.SetDirty(glow);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            scene.name="Mist Sanctuary";
            RenderSettings.ambientMode=AmbientMode.Trilight; RenderSettings.ambientSkyColor=new Color(.31f,.33f,.36f);
            RenderSettings.ambientEquatorColor=new Color(.19f,.20f,.22f); RenderSettings.ambientGroundColor=new Color(.09f,.12f,.15f); RenderSettings.fog=false;
            var architecture=new GameObject("Sanctuary architecture").transform;
            Cube("Foundation",architecture,new Vector3(0,-.21f,0),new Vector3(18,.4f,18),dark);
            for(int x=-8;x<8;x++) for(int z=-8;z<8;z++)
                Cube("Stone floor",architecture,new Vector3(x+.5f,-.035f,z+.5f),new Vector3(.982f,.07f,.982f), stone);
            for(int i=-1;i<=1;i++)
            {
                float z=i*4.2f;
                for(int side=-1;side<=1;side+=2)
                {
                    var plinth=Cube("Column plinth",architecture,new Vector3(side*5.75f,.22f,z),new Vector3(1.35f,.44f,1.35f),stone);
                    plinth.AddComponent<FogObstacle>();
                    var pillar=Primitive("Fluted column",PrimitiveType.Cylinder,architecture,new Vector3(side*5.75f,2.4f,z),new Vector3(.85f,2,.85f),stone);
                    pillar.AddComponent<FogObstacle>();
                    Cube("Capital",architecture,new Vector3(side*5.75f,4.5f,z),new Vector3(1.3f,.35f,1.3f),stone);
                    Cube("Lantern bracket",architecture,new Vector3(side*5f,1.3f,z),new Vector3(.7f,.12f,.35f),bronze);
                    Primitive("Amber lantern",PrimitiveType.Capsule,architecture,new Vector3(side*4.85f,1.65f,z),new Vector3(.22f,.26f,.22f),glow);
                    var lamp=new GameObject("Warm pool of light").AddComponent<Light>(); lamp.transform.position=new Vector3(side*4.65f,1.7f,z); lamp.type=LightType.Point; lamp.color=new Color(1,.58f,.22f); lamp.intensity=3.5f; lamp.range=3.4f;
                }
            }
            Cube("Apse wall",architecture,new Vector3(0,3.5f,8.2f),new Vector3(19,7,.4f),stone);
            for(int side=-1;side<=1;side+=2)
                Cube("Side gallery wall",architecture,new Vector3(side*9,3.5f,1),new Vector3(15,7,.4f),stone).transform.rotation=Quaternion.Euler(0,90,0);
            for(int i=-3;i<=3;i++) Cube("Wall ribs",architecture,new Vector3(i*2.4f,3.5f,7.9f),new Vector3(.2f,7,.25f),stone);
            for(int i=0;i<3;i++)
            {
                var stair=Cube("Dais step",architecture,new Vector3(0,.12f+i*.22f,4.8f+i*.2f),new Vector3(4.8f-i*.7f,.24f+i*.44f,3-i*.5f),stone);
                stair.AddComponent<FogObstacle>();
            }
            var monument=Primitive("Monolith",PrimitiveType.Cube,architecture,new Vector3(0,1.6f,5.1f),new Vector3(.75f,1.9f,.75f),stone); monument.transform.rotation=Quaternion.Euler(0,45,0); monument.AddComponent<FogObstacle>();
            var light=new GameObject("Warm shaft of light").AddComponent<Light>(); light.type=LightType.Directional; light.color=new Color(.94f,.955f,1); light.intensity=1.05f;
            light.transform.rotation=Quaternion.Euler(35,-28,0); light.shadows=LightShadows.Soft; light.shadowStrength=.36f; RenderSettings.sun=light;
            var altarLight=new GameObject("Altar skylight").AddComponent<Light>();
            altarLight.type=LightType.Spot; altarLight.color=new Color(.95f,.96f,1); altarLight.intensity=42; altarLight.range=14;
            altarLight.spotAngle=52; altarLight.innerSpotAngle=34;
            altarLight.transform.position=new Vector3(-2,5.2f,0); altarLight.transform.LookAt(new Vector3(0,.8f,5.1f));
            var fogGO=new GameObject("Interactive ground fog"); var fog=fogGO.AddComponent<FogVolume>();
            fog.simulation=AssetDatabase.LoadAssetAtPath<ComputeShader>(FogSetup.PackageRoot+"/Runtime/Shaders/FogFluid.compute"); fog.sun=light;
            fog.style=AssetDatabase.LoadAssetAtPath<FogStyleProfile>(Root+"/Profiles/Sanctuary Mist.asset");
            fog.localLights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            fog.resolution=new Vector3Int(160,32,160); fog.raySteps=112;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Characters/Explorer.prefab");
            var actor = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).transform;
            actor.SetPositionAndRotation(new Vector3(-2, 0, -3), Quaternion.Euler(0,69,0));
            var camera=new GameObject("Sanctuary camera").AddComponent<Camera>(); camera.tag="MainCamera"; camera.nearClipPlane=.05f; camera.farClipPlane=80;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.025f,.04f,.055f); camera.fieldOfView=48;
            camera.transform.rotation=Quaternion.Euler(21,0,0);
            camera.transform.position=new Vector3(0,.5f,.75f)-camera.transform.forward*10.1f;
            var cameraData=camera.GetUniversalAdditionalCameraData(); cameraData.renderPostProcessing=true; cameraData.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var grade=Asset<VolumeProfile>(Root+"/Settings/Sanctuary grading.asset");
            if(!grade.TryGet<Tonemapping>(out var tone)) { tone=grade.Add<Tonemapping>(); AssetDatabase.AddObjectToAsset(tone,grade); }
            tone.mode.Override(TonemappingMode.Neutral);
            if(!grade.TryGet<Bloom>(out var bloom)) { bloom=grade.Add<Bloom>(); AssetDatabase.AddObjectToAsset(bloom,grade); }
            bloom.intensity.Override(.44f); bloom.threshold.Override(.8f); bloom.scatter.Override(.8f);
            EditorUtility.SetDirty(tone); EditorUtility.SetDirty(bloom);
            EditorUtility.SetDirty(grade); var volume=new GameObject("Sanctuary grading").AddComponent<Volume>(); volume.gameObject.layer=31; cameraData.volumeLayerMask=1 << 31; volume.isGlobal=true; volume.sharedProfile=grade;
            camera.gameObject.AddComponent<FogCameraPass>().feature = feature;
            camera.allowMSAA = false;
            cameraData.requiresDepthTexture = true;
            var controller=new GameObject("Mist Sanctuary controls").AddComponent<MistSanctuary>(); controller.fog=fog; controller.actor=actor; controller.view=camera; controller.bladeTip = actor.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand).Find("Sword/Blade tip");
            controller.referenceStyle=AssetDatabase.LoadAssetAtPath<FogStyleProfile>(FogSetup.PackageRoot+"/Runtime/Styles/Sanctuary Ground Mist.asset");
            SampleScenePresentation.Apply(scene, Root);
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
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath},locationPathName="Builds/MistSanctuary/MistSanctuary.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development });
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Fog player build failed: "+report.summary.result);
            if(publish) Publish();
        }
        public static void Publish()
        {
            PackageSamplePublisher.Publish(Root);
            Debug.Log("FOG_SAMPLE_PUBLISHED " + Root);
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
            if (name == "Column plinth" || name == "Dais step" || name == "Monolith" || name == "Capital")
                go.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/Dressed stone.asset");
            go.GetComponent<Renderer>().sharedMaterial=material; return go;
        }
    }
}
