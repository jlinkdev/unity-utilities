using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace jlinkdev.HumanoidStudio.Editor
{
    public static class HumanoidStudioBuilder
    {
        const string Root = HumanoidImport.Root;
        const string ScenePath = Root + "/Scenes/Humanoid Review.unity";
        static readonly string[] Names = { "Anatomical Base", "Reduction Study", "Sculpted Planes", "Offset Planes" };

        static Material MaterialAsset(string model, string name, Color color)
        {
            var path = $"{Root}/Models/{model}/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat) return mat;
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", .18f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static AnimationClip Motion()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(HumanoidImport.JointSweep)
                .OfType<AnimationClip>().Where(c => c.humanMotion && !c.name.StartsWith("__preview__")).ToArray();
            if (clips.Length != 1) throw new InvalidOperationException("Expected one baked Humanoid joint sweep.");
            return clips[0];
        }

        [MenuItem("Tools/Unity Utilities/Humanoid/Rebuild review assets")]
        public static void Build()
        {
            if (SceneManager.GetSceneByPath(ScenePath).isLoaded)
                throw new InvalidOperationException("Close the Humanoid Review scene before regenerating it.");
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var surface = MaterialAsset("Humanoid", "Studio Neutral", new Color(.76f,.75f,.72f));
                var floorMat = MaterialAsset("Studio Floor", "Studio Floor", new Color(.29f,.31f,.33f));
                var studio = new GameObject("Humanoid Review").AddComponent<HumanoidReview>();
                studio.motion = Motion();
                studio.figures = new Animator[Names.Length];
                if (!AssetDatabase.IsValidFolder(Root + "/Prefabs")) AssetDatabase.CreateFolder(Root, "Prefabs");
                for (int i = 0; i < Names.Length; i++)
                {
                    string name = Names[i];
                    string modelName = name == "Offset Planes" ? "Humanoid" : name;
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{modelName}/{modelName}.fbx");
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
                    instance.name = name;
                    var skin = instance.GetComponentInChildren<SkinnedMeshRenderer>();
                    skin.sharedMaterial = surface;
                    skin.updateWhenOffscreen = true;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(skin);
                    var prefab = PrefabUtility.SaveAsPrefabAsset(instance, Root + "/Prefabs/" + name + ".prefab");
                    Object.DestroyImmediate(instance);
                    var figure = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    figure.transform.position = Vector3.right * ((1.5f - i) * 1.42f);
                    studio.figures[i] = figure.GetComponent<Animator>();
                }
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Studio Floor";
                floor.transform.localScale = new Vector3(200,.10f,200);
                floor.transform.position = Vector3.down * .051f;
                Object.DestroyImmediate(floor.GetComponent<Collider>());
                floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.48f,.50f,.53f);
                RenderSettings.skybox = null;
                RenderSettings.fog = false;
                AddLight("Key", new Vector3(52,213,0),1.25f, LightShadows.Soft);
                AddLight("Fill", new Vector3(22,102,0),.55f, LightShadows.None);
                var camera = new GameObject("Studio Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.34f,.36f,.38f);
                camera.fieldOfView = 35;
                camera.nearClipPlane = .035f;
                camera.transform.position = new Vector3(.4f,1.35f,5.6f);
                camera.transform.LookAt(Vector3.up * .90f);
                studio.studioCamera = camera;
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException(ScenePath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void AddLight(string name, Vector3 rotation, float intensity, LightShadows shadows)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.eulerAngles = rotation;
            light.intensity = intensity;
            light.shadows = shadows;
            light.shadowStrength = .38f;
        }
    }
}
