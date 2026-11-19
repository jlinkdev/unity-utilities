using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JLinkDev.HumanoidStudio.Editor
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
            Validate();
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

        [MenuItem("Tools/Unity Utilities/Humanoid/Validate and capture")]
        public static void Validate()
        {
            var scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            var output = Path.GetFullPath("ArtSource/Humanoid");
            try
            {
                var review = scene.GetRootGameObjects().Select(o => o.GetComponent<HumanoidReview>()).First(r => r);
                review.studioCamera.scene = scene;
                review.studioCamera.orthographic = true;
                review.studioCamera.orthographicSize = 1.27f;
                var messages = new List<string>();
                foreach (var animator in review.figures)
                    messages.Add(CheckFigure(animator, review.motion));
                // Capture from the saved scene camera. The preview scene keeps the
                // user's open scenes, selections and animation state independent.
                Capture(review, Path.Combine(output, "Bind comparison.png"));
                var graphs = new List<PlayableGraph>();
                var players = new List<AnimationClipPlayable>();
                try
                {
                    foreach (var animator in review.figures)
                    {
                        var graph = PlayableGraph.Create();
                        graphs.Add(graph);
                        var clip = AnimationClipPlayable.Create(graph, review.motion);
                        players.Add(clip);
                        AnimationPlayableOutput.Create(graph, "Pose", animator).SetSourcePlayable(clip);
                        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                        graph.Play();
                        clip.SetTime(0);
                        graph.Evaluate(0);
                    }
                    Capture(review, Path.Combine(output, "Standing comparison.png"));
                    for (int i = 0; i < players.Count; i++)
                    {
                        players[i].SetTime(1.6);
                        graphs[i].Evaluate(0);
                    }
                    Capture(review, Path.Combine(output, "Motion comparison.png"));
                }
                finally { foreach (var g in graphs) g.Destroy(); }
                File.WriteAllLines(Path.Combine(output, "Unity validation.txt"), messages);
                Debug.Log(string.Join("\n", messages));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        static string CheckFigure(Animator a, AnimationClip motion)
        {
            void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException(a.name + " — " + reason); }
            Check(a.isHuman && a.avatar.isValid && a.avatar.humanDescription.human.Length == 52, "Avatar mapping");
            var skins = a.GetComponentsInChildren<SkinnedMeshRenderer>();
            Check(skins.Length == 1 && skins[0].sharedMaterials.Length == 1 && skins[0].sharedMaterial.name == "Studio Neutral", "material slots");
            var skin = skins[0];
            var mesh = skin.sharedMesh;
            var bones = skin.bones;
            var poses = mesh.bindposes;
            Check(bones.Length == poses.Length && bones.All(b => b), "missing bind transform");
            for (int i = 0; i < bones.Length; i++)
            {
                var bind = skin.transform.worldToLocalMatrix * bones[i].localToWorldMatrix * poses[i];
                for (int k = 0; k < 16; k++)
                    Check(Mathf.Abs(bind[k] - Matrix4x4.identity[k]) < .002f, "bind matrix " + bones[i].name);
            }
            foreach (var w in mesh.boneWeights)
                Check(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1) < .001f, "unnormalized skin");
            var baked = new Mesh();
            var graph = PlayableGraph.Create();
            var transforms = a.GetComponentsInChildren<Transform>();
            var bindPositions = transforms.Select(t => t.localPosition).ToArray();
            var bindRotations = transforms.Select(t => t.localRotation).ToArray();
            try
            {
                skin.BakeMesh(baked);
                var rest = baked.vertices;
                var height = rest.Max(v => skin.transform.TransformPoint(v).y)
                    - rest.Min(v => skin.transform.TransformPoint(v).y);
                Check(height > 1.6f && height < 1.9f, "height " + height);
                var indices = mesh.triangles;
                var ratios = new List<float>();
                var clip = AnimationClipPlayable.Create(graph, motion);
                AnimationPlayableOutput.Create(graph, "Sweep", a).SetSourcePlayable(clip);
                a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                graph.Play();
                var originalHand = a.GetBoneTransform(HumanBodyBones.LeftHand).position;
                float travel = 0;
                var low = float.PositiveInfinity;
                var high = float.NegativeInfinity;
                foreach (float t in new[] { 0f,.8f,1.6f,2.4f,3.2f,4f,4.8f,5.6f,6.4f })
                {
                    clip.SetTime(t);
                    graph.Evaluate(0);
                    skin.BakeMesh(baked);
                    var posed = baked.vertices;
                    var ankle = a.GetBoneTransform(HumanBodyBones.RightFoot).position.y;
                    low = Mathf.Min(low, ankle);
                    high = Mathf.Max(high, ankle);
                    travel = Mathf.Max(travel, Vector3.Distance(originalHand, a.GetBoneTransform(HumanBodyBones.LeftHand).position));
                    for (int k = 0; k < indices.Length; k++)
                    {
                        int i = indices[k], j = indices[k - k%3 + (k+1)%3];
                        float length = Vector3.Distance(rest[i],rest[j]);
                        if (length < .0005f) continue;
                        float ratio = Vector3.Distance(posed[i],posed[j]) / length;
                        Check(float.IsFinite(ratio), "non-finite posed vertex");
                        ratios.Add(ratio);
                    }
                }
                Check(travel > .20f, "animation did not move hand");
                Check(high - low > .10f && low < .20f, "animation did not lift and lower foot");
                ratios.Sort();
                return $"{a.name} | {indices.Length/3} triangles | {height:F3} m | valid Humanoid, 52 mapped | {bones.Length} skin bind matrices passed | hand travel {travel:F2} m | ankle height {low:F2}–{high:F2} m | posed/bind edge length p01–p99 {ratios[ratios.Count/100]:F2}–{ratios[ratios.Count*99/100]:F2} | minimum {ratios[0]:F2}, maximum {ratios[^1]:F2}";
            }
            finally
            {
                graph.Destroy();
                Object.DestroyImmediate(baked);
                for (int i = 0; i < transforms.Length; i++)
                {
                    transforms[i].localPosition = bindPositions[i];
                    transforms[i].localRotation = bindRotations[i];
                }
            }
        }

        static void Capture(HumanoidReview review, string path)
        {
            var cam = review.studioCamera;
            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(1600,680,24,RenderTextureFormat.ARGB32);
            var image = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            var standIns = new List<GameObject>();
            var bakedMeshes = new List<Mesh>();
            var skins = review.figures.Select(a => a.GetComponentInChildren<SkinnedMeshRenderer>()).ToArray();
            try
            {
                // Preview-scene GPU skinning can lag manual Playable evaluation.
                // Render the exact CPU-skinned surface measured by the validator.
                foreach (var skin in skins)
                {
                    var frozen = new GameObject("Capture mesh");
                    standIns.Add(frozen);
                    SceneManager.MoveGameObjectToScene(frozen, skin.gameObject.scene);
                    frozen.transform.SetParent(skin.transform, false);
                    var baked = new Mesh();
                    bakedMeshes.Add(baked);
                    skin.BakeMesh(baked);
                    frozen.AddComponent<MeshFilter>().sharedMesh = baked;
                    frozen.AddComponent<MeshRenderer>().sharedMaterial = skin.sharedMaterial;
                    skin.enabled = false;
                }
                cam.targetTexture = target;
                cam.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                image.Apply();
                File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally
            {
                foreach (var skin in skins) skin.enabled = true;
                foreach (var go in standIns) Object.DestroyImmediate(go);
                foreach (var mesh in bakedMeshes) Object.DestroyImmediate(mesh);
                cam.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(image);
            }
        }
    }
}
