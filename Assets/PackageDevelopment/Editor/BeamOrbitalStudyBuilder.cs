using System.IO;
using System.Linq;
using jlinkdev.UnityUtilities.Beams.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace jlinkdev.UnityUtilities.Beams.Development
{
    public static partial class BeamDevelopmentBuilder
    {
        private const string OrbitalRoot = "Assets/PackageSamples/com.jlinkdev.beams/Orbital Descent";

        private static Camera CameraIn(Scene scene) =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>()).First();

        private static void DemoBloom(Camera camera, string root, float intensity)
        {
            const int volumeLayer = 31;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.volumeLayerMask = 1 << volumeLayer;
            camera.allowHDR = true;
            camera.allowMSAA = false;
            var zone = new GameObject("Camera bloom");
            zone.transform.SetParent(camera.transform, false);
            zone.layer = volumeLayer;
            zone.AddComponent<SphereCollider>().isTrigger = true;
            var volume = zone.AddComponent<Volume>();
            volume.isGlobal = false;
            volume.priority = 10;
            EnsureFolder(root + "/Settings");
            string path = root + "/Settings/Camera Bloom.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
                AssetDatabase.AddObjectToAsset(profile.Add<Bloom>(), profile);
            }
            profile.TryGet(out Bloom bloom);
            bloom.threshold.Override(0.72f);
            bloom.intensity.Override(intensity);
            bloom.scatter.Override(0.72f);
            EditorUtility.SetDirty(bloom);
            EditorUtility.SetDirty(profile);
            volume.sharedProfile = profile;
        }

        private static void BuildOrbital()
        {
            foreach (var folder in new[] { "Scenes", "Animations", "Meshes", "Materials", "Prefabs" })
                EnsureFolder(OrbitalRoot + "/" + folder);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var camera = new GameObject("Orbital Camera", typeof(Camera)).GetComponent<Camera>();
                camera.tag = "MainCamera";
                camera.transform.position = new Vector3(5.6f, 3.65f, -10.2f);
                camera.transform.LookAt(new Vector3(-0.7f, 2.7f, 0));
                camera.fieldOfView = 46;
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.backgroundColor = new Color(0.105f, 0.135f, 0.19f);
                camera.gameObject.AddComponent<Skybox>().material = StudyMaterial("Dusk", "Basin Sky");
                DemoBloom(camera, OrbitalRoot, 0.9f);
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = new Color(.40f,.40f,.45f);
                RenderSettings.fogDensity = 0.027f;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.26f,0.30f,0.39f);
                var sun = new GameObject("Raking dusk light", typeof(Light)).GetComponent<Light>();
                sun.type = LightType.Directional;
                sun.color = new Color(0.83f,0.86f,1);
                sun.intensity = 1.7f;
                sun.transform.rotation = Quaternion.Euler(16,-60,0);
                BuildBasin();
                BuildTalus();
                Physics.SyncTransforms();
                var emitter = CreateOrbitalEmitter();
                PrefabUtility.SaveAsPrefabAssetAndConnect(emitter, OrbitalRoot + "/Prefabs/Orbital Column.prefab", InteractionMode.AutomatedAction);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(OrbitalRoot + "/Animations/Sweep.anim");
                if (!clip)
                {
                    clip = new AnimationClip { legacy = true };
                    AssetDatabase.CreateAsset(clip, OrbitalRoot + "/Animations/Sweep.anim");
                }
                clip.wrapMode = WrapMode.PingPong;
                clip.SetCurve("",typeof(Transform),"localPosition.x", AnimationCurve.EaseInOut(0,-7.7f,5,-4.1f));
                clip.SetCurve("",typeof(Transform),"localPosition.y", AnimationCurve.Constant(0,5,emitter.transform.localPosition.y));
                clip.SetCurve("",typeof(Transform),"localPosition.z", AnimationCurve.Constant(0,5,emitter.transform.localPosition.z));
                EditorUtility.SetDirty(clip);
                var motion = emitter.AddComponent<Animation>();
                motion.AddClip(clip, "Traverse");
                motion.clip = clip;
                emitter.GetComponent<Beam>().Refresh();
                SampleScenePresentation.Apply(scene, OrbitalRoot);
                if (!EditorSceneManager.SaveScene(scene, OrbitalRoot + "/Scenes/Orbital Descent.unity"))
                    throw new IOException(OrbitalRoot);
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            PackageSamplePublisher.Publish(OrbitalRoot);
        }

        private static GameObject CreateOrbitalEmitter()
        {
            var root = new GameObject("Orbital Column");
            root.transform.position = new Vector3(-5.9f, 16, -1.1f);
            root.transform.rotation = Quaternion.LookRotation(-root.transform.position.normalized);
            var ray = root.AddComponent<RaycastBeamEndpoint>();
            ray.LayerMask = 1; // The basin is on Default.
            ray.MaximumDistance = 40;
            var path = root.AddComponent<StraightBeamPath>();
            var sampling = root.AddComponent<BeamResampleModifier>();
            var material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.jlinkdev.beams/Runtime/Materials/Orbital Column.mat");
            BeamRibbonRenderer Tube(string name, float width, float intensity, Color tint)
            {
                var tube = new GameObject(name).AddComponent<BeamRibbonRenderer>();
                tube.transform.SetParent(root.transform, false);
                tube.VolumeTube = true;
                tube.Width = width;
                tube.Intensity = intensity;
                tube.Color = tint;
                tube.GetComponent<MeshRenderer>().sharedMaterial = material;
                return tube;
            }
            var layers = root.AddComponent<BeamRendererGroup>();
            layers.Renderers = new BeamPathRenderer[] {
                Tube("Ionized sheath", 1.65f, 0.20f, new Color(1,0.61f,0.16f)),
                Tube("Incandescent column", 0.70f, 1.7f, Color.white),
                Tube("White filament",0.19f,2.8f,new Color(1,1.3f,2.7f)) };
            var beam = root.AddComponent<Beam>();
            beam.Target = ray;
            beam.PathProvider = path;
            beam.PathRenderer = layers;
            beam.Modifiers = new BeamPathModifier[]{sampling};
            BeamAccentAuthoring.Attach(beam, new Color(3.5f,1.2f,0.08f), 2.0f, true);
            Transform hit = root.transform.Find("Contact light");
            // Conform the pool to the hit collider, independently of camera depth configuration.
            var heated = hit.Find("Heated surface");
            heated.localScale = new Vector3(4.4f,4.4f,1.5f);
            heated.GetComponent<MeshRenderer>().sharedMaterial = StudyMaterial("Molten footprint","Projected Heat");
            heated.gameObject.AddComponent<BeamSurfaceFootprint>();
            var light = new GameObject("Reflected heat",typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(hit,false);
            light.transform.localPosition = Vector3.forward * 0.65f;
            light.type = LightType.Point;
            light.color = new Color(1f,0.44f,0.09f);
            light.range = 7;
            light.intensity = 12;
            Ejecta(hit, false);
            Ejecta(hit, true);
            var plume = new GameObject("Ablation plume").AddComponent<ParticleSystem>();
            plume.transform.SetParent(hit,false);
            plume.transform.localPosition = Vector3.forward * .22f;
            var smoke = plume.main;
            smoke.simulationSpace = ParticleSystemSimulationSpace.World;
            smoke.startLifetime = new ParticleSystem.MinMaxCurve(1.1f,2.1f);
            smoke.startSpeed = new ParticleSystem.MinMaxCurve(.85f,2.6f);
            smoke.startSize = new ParticleSystem.MinMaxCurve(.52f,.95f);
            smoke.startRotation = new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
            var cone = plume.shape;
            cone.shapeType = ParticleSystemShapeType.Cone;
            cone.angle = 59;
            cone.radius = .44f;
            var rate = plume.emission;
            rate.rateOverTime = 20;
            var wind = plume.velocityOverLifetime;
            wind.enabled = true;
            wind.space = ParticleSystemSimulationSpace.World;
            wind.x = 1.25f;
            var grow = plume.sizeOverLifetime;
            grow.enabled = true;
            grow.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0,.4f,1,2.1f));
            var cooling = new Gradient();
            cooling.SetKeys(new []{new GradientColorKey(new Color(1,.43f,.08f),0),new GradientColorKey(new Color(.47f,.34f,.29f),.28f),
                new GradientColorKey(new Color(.32f,.34f,.39f),1)}, new []{new GradientAlphaKey(.1f,0),new GradientAlphaKey(.67f,.16f),new GradientAlphaKey(0,1)});
            var smokeColor = plume.colorOverLifetime;
            smokeColor.enabled = true;
            smokeColor.color = cooling;
            plume.GetComponent<ParticleSystemRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.jlinkdev.beams/Runtime/Materials/Thermal Plume.mat");
            var wake = new GameObject("Cooling slag trail").AddComponent<ParticleSystem>();
            wake.transform.SetParent(hit,false);
            wake.transform.localPosition = Vector3.forward * 0.04f;
            var m = wake.main;
            m.startSpeed = 0;
            m.startLifetime = 2.3f;
            m.startSize = new ParticleSystem.MinMaxCurve(0.5f,0.78f);
            m.startColor = new Color(0.95f,0.2f,0.015f,.42f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = wake.shape;
            shape.enabled = false;
            var emission = wake.emission;
            emission.rateOverTime = 10;
            var size = wake.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0,1,1,0));
            var wr = wake.GetComponent<ParticleSystemRenderer>();
            wr.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            wr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.jlinkdev.beams/Runtime/Materials/Endpoint Light.mat");
            return root;
        }

        private static void Ejecta(Transform anchor, bool embers)
        {
            var ps = new GameObject(embers ? "Drifting embers" : "Incandescent ejecta").AddComponent<ParticleSystem>();
            ps.transform.SetParent(anchor,false);
            ps.transform.localPosition = Vector3.forward * 0.06f;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(embers ? 0.9f : 0.28f, embers ? 2f : 0.58f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(embers ? 0.7f : 3.8f, embers ? 2.2f : 9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.055f, embers ? 0.16f : 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1,0.17f,0.01f), new Color(1,0.81f,0.18f));
            main.gravityModifier = embers ? -0.09f : 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = embers ? 0.46f : 0.27f;
            shape.angle = embers ? 34 : 69;
            var emission = ps.emission;
            emission.rateOverTime = embers ? 25 : 82;
            var life = ps.sizeOverLifetime;
            life.enabled = true;
            life.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0,1,1,0));
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.jlinkdev.beams/Runtime/Materials/Endpoint Light.mat");
            if (!embers)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = 0.065f;
                renderer.lengthScale = 1.3f;
            }
            ps.Play();
        }

        private static void BuildBasin()
        {
            const int count = 112;
            const float spacing = 0.95f;
            var vertices = new Vector3[count*count];
            var faces = new int[(count-1)*(count-1)*6];
            for (int z=0; z<count; z++)
            for (int x=0; x<count; x++)
            {
                float px = (x-count/2)*spacing, pz=(z-count/2)*spacing;
                float ridge = Mathf.SmoothStep(0,1,Mathf.InverseLerp(9,32,new Vector2(px,pz).magnitude));
                float broad = Mathf.PerlinNoise(px*0.093f+19, pz*0.093f+42);
                float seams = Mathf.Abs(Mathf.Sin(pz*1.12f + px*0.27f + broad*7));
                float crags = Mathf.Abs(Mathf.PerlinNoise(px*.24f+7,pz*.24f+3)-.46f)*4.5f;
                float mesa = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.41f,.64f,broad));
                // Smooth the worn floor at the sweep and roll the mesh boundary below the horizon.
                float distance = new Vector2(px,pz).magnitude;
                float height = 0.48f*broad + Mathf.Lerp(.018f,.18f,Mathf.SmoothStep(0,1,distance/7))*seams + ridge*(1+mesa*7+crags);
                height = Mathf.Lerp(height,-3,Mathf.SmoothStep(0,1,Mathf.InverseLerp(40,51,Mathf.Max(Mathf.Abs(px),Mathf.Abs(pz)))));
                vertices[z*count+x] = new Vector3(px,height,pz);
                if(x+1==count || z+1==count) continue;
                int i=z*count+x, j=(z*(count-1)+x)*6;
                faces[j]=i;faces[j+1]=i+count;faces[j+2]=i+1;
                faces[j+3]=i+1;faces[j+4]=i+count;faces[j+5]=i+count+1;
            }
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(OrbitalRoot+"/Meshes/Basin.asset");
            if (!mesh)
            {
                mesh = new Mesh { name="Basalt Basin" };
                AssetDatabase.CreateAsset(mesh,OrbitalRoot+"/Meshes/Basin.asset");
            }
            mesh.Clear();
            mesh.vertices=vertices;
            mesh.triangles=faces;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            var basin = new GameObject("Basalt basin", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            basin.GetComponent<MeshFilter>().sharedMesh=mesh;
            basin.GetComponent<MeshCollider>().sharedMesh=mesh;
            basin.GetComponent<MeshRenderer>().sharedMaterial=StudyMaterial("Basalt","Basin Surface");
        }

        private static Material StudyMaterial(string name, string shaderFile)
        {
            string path = OrbitalRoot+"/Materials/"+name+".mat";
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(OrbitalRoot+"/Shaders/"+shaderFile+".shader");
            var rock = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!rock)
            {
                rock = new Material(shader);
                AssetDatabase.CreateAsset(rock,path);
            }
            rock.shader = shader;
            EditorUtility.SetDirty(rock);
            return rock;
        }

        private static void BuildTalus()
        {
            // Bake fractured, flat-shaded outcrops into one mesh and one collision surface.
            var basin = SceneManager.GetActiveScene().GetRootGameObjects()
                .Single(root => root.name == "Basalt basin").GetComponent<MeshCollider>();
            var vertices = new System.Collections.Generic.List<Vector3>();
            var faces = new System.Collections.Generic.List<int>();
            var random = new System.Random(8741);
            float Next() => (float)random.NextDouble();
            void Face(Vector3 a, Vector3 b, Vector3 c)
            {
                int i=vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                faces.Add(i); faces.Add(i+1); faces.Add(i+2);
            }
            Physics.SyncTransforms();
            for (int stone=0;stone<540;stone++)
            {
                float x=(Next()-.5f)*48, z=Next()*46-16;
                if (!basin.Raycast(new Ray(new Vector3(x,24,z),Vector3.down),out var hit,50)) continue;
                float scale=.09f+Mathf.Pow(Next(),4)*2.4f;
                if (Mathf.Abs(x)<4 && Mathf.Abs(z)<1.5f) continue;
                var center=hit.point-Vector3.up*scale*.20f;
                float rotation=Next()*6.28f, ratio=.48f+Next()*.45f, height=scale*(.50f+Next()*.55f);
                int count=5+random.Next(3);
                var bottom=new Vector3[count];
                var rim=new Vector3[count];
                for(int i=0;i<count;i++)
                {
                    float angle=rotation+2*Mathf.PI*i/count;
                    var radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)*ratio)*scale*(.72f+.3f*Next());
                    bottom[i]=center+radial;
                    rim[i]=center+radial*(.53f+.22f*Next())+new Vector3(scale*.21f,height*(.73f+.22f*Next()),0);
                }
                for(int i=0;i<count;i++)
                {
                    int j=(i+1)%count;
                    Face(bottom[i],rim[i],bottom[j]); Face(rim[i],rim[j],bottom[j]);
                    if(i>0 && i<count-1) Face(rim[0],rim[i+1],rim[i]);
                }
            }
            string path=OrbitalRoot+"/Meshes/Talus.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh) { mesh=new Mesh(); AssetDatabase.CreateAsset(mesh,path); }
            mesh.Clear();
            mesh.indexFormat=IndexFormat.UInt32;
            mesh.SetVertices(vertices); mesh.SetTriangles(faces,0); mesh.RecalculateNormals();
            EditorUtility.SetDirty(mesh);
            var rocks=new GameObject("Fractured outcrops",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
            rocks.GetComponent<MeshFilter>().sharedMesh=mesh;
            rocks.GetComponent<MeshCollider>().sharedMesh=mesh;
            rocks.GetComponent<MeshRenderer>().sharedMaterial=basin.GetComponent<MeshRenderer>().sharedMaterial;
        }
    }
}
