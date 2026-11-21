using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace jlinkdev.UnityUtilities.WaterSystem
{
    /// <summary>A finite horizontal water body. Its mesh, queries and underwater closure share one tessellated wave surface.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("jlinkdev/Water System/Water Body")]
    public sealed class WaterBody : MonoBehaviour
    {
        static readonly List<WaterBody> bodies = new List<WaterBody>();
        public static IReadOnlyList<WaterBody> ActiveBodies => bodies;

        [SerializeField] WaterProfile profile;
        [SerializeField] Vector2 size = new Vector2(40f, 40f);
        [SerializeField, Min(0.1f)] float depth = 12f;
        [SerializeField, Range(1, 256)] int resolution = 128;
        [SerializeField, HideInInspector] Shader surfaceShader;
        WaterProfile defaultProfile;
        Mesh mesh, previousMesh;
        Material material;
        Material[] previousMaterials;
        MeshFilter filter;
        MeshRenderer surfaceRenderer;
        MaterialPropertyBlock propertyBlock;
        MaterialPropertyBlock previousPropertyBlock;
        readonly Vector4[] waveShapes = new Vector4[WaterProfile.MaximumWaves];
        readonly Vector4[] waveMotions = new Vector4[WaterProfile.MaximumWaves];
        Vector2 builtSize;
        int builtResolution;
        bool rebuildPending;
        float renderTime;

        public WaterProfile Profile
        {
            get
            {
                if (profile != null) return profile;
                if (defaultProfile == null)
                {
                    defaultProfile = ScriptableObject.CreateInstance<WaterProfile>();
                    defaultProfile.name = "Default Water Profile (Runtime)";
                    defaultProfile.hideFlags = HideFlags.HideAndDontSave;
                }
                return defaultProfile;
            }
            set { profile = value; rebuildPending = true; }
        }
        public Vector2 Size
        {
            get => new Vector2(Mathf.Max(0.1f, WaterWaves.Finite(size.x, 40f)), Mathf.Max(0.1f, WaterWaves.Finite(size.y, 40f)));
            set { size = value; rebuildPending = true; }
        }
        public float Depth { get => Mathf.Max(0.1f, WaterWaves.Finite(depth, 12f)); set => depth = value; }
        public int Resolution { get => Mathf.Clamp(resolution, 1, 256); set { resolution = value; rebuildPending = true; } }
        public float SurfaceTime => Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        public Mesh SurfaceMesh => mesh;
        public Material SurfaceMaterial => material;
        public Renderer SurfaceRenderer => surfaceRenderer;
        public bool HasSupportedTransform => Quaternion.Angle(transform.rotation, Quaternion.identity) < 0.01f &&
            (transform.lossyScale - Vector3.one).sqrMagnitude < 0.000001f;
        public Bounds WorldBounds => new Bounds(transform.position + Vector3.up * (Profile.MaximumDisplacement - Depth) * 0.5f,
            new Vector3(Size.x, Depth + Profile.MaximumDisplacement, Size.y));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry() { bodies.Clear(); }

        void OnEnable()
        {
            filter = GetComponent<MeshFilter>(); surfaceRenderer = GetComponent<MeshRenderer>();
            previousMesh = filter.sharedMesh; previousMaterials = surfaceRenderer.sharedMaterials;
            if (previousPropertyBlock == null) previousPropertyBlock = new MaterialPropertyBlock();
            surfaceRenderer.GetPropertyBlock(previousPropertyBlock);
            if (!bodies.Contains(this)) bodies.Add(this);
            RenderPipelineManager.beginCameraRendering += BeforeCamera;
            Rebuild();
        }
        void OnDisable()
        {
            bodies.Remove(this);
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            if (filter != null && filter.sharedMesh == mesh) filter.sharedMesh = previousMesh;
            if (surfaceRenderer != null && surfaceRenderer.sharedMaterial == material)
            {
                surfaceRenderer.sharedMaterials = previousMaterials ?? new Material[0];
                surfaceRenderer.SetPropertyBlock(previousPropertyBlock != null && !previousPropertyBlock.isEmpty ? previousPropertyBlock : null);
            }
            CoreUtils.Destroy(mesh); CoreUtils.Destroy(material); CoreUtils.Destroy(defaultProfile);
            mesh = null; material = null; defaultProfile = null;
        }
        void OnDestroy()
        {
            // Profile may be queried again while disabled, after OnDisable released its old default.
            CoreUtils.Destroy(defaultProfile);
            defaultProfile = null;
        }
        void OnValidate() { rebuildPending = true; }
        void Update()
        {
            // Also repopulates when domain and scene reload are both disabled.
            if (!bodies.Contains(this)) bodies.Add(this);
            if (rebuildPending || builtSize != Size || builtResolution != Resolution) Rebuild();
        }
        void BeforeCamera(ScriptableRenderContext context, Camera camera)
        {
            if (material == null || surfaceRenderer == null) return;
            renderTime = SurfaceTime;
            UpdateBounds();
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            ApplyShaderProperties(propertyBlock);
            surfaceRenderer.SetPropertyBlock(propertyBlock);
        }

        /// <summary>Rebuilds owned transient geometry; never creates or modifies project assets.</summary>
        public void Rebuild()
        {
            if (!isActiveAndEnabled) return;
            if (filter == null) filter = GetComponent<MeshFilter>();
            if (surfaceRenderer == null) surfaceRenderer = GetComponent<MeshRenderer>();
            if (surfaceShader == null) surfaceShader = Resources.Load<Shader>("WaterSurface");
            if (surfaceShader == null) surfaceShader = Shader.Find("jlinkdev/Water System/Surface");
            if (material == null && surfaceShader != null)
            {
                material = new Material(surfaceShader) { name = "Water Surface (Runtime)", hideFlags = HideFlags.HideAndDontSave };
                surfaceRenderer.sharedMaterial = material;
            }
            int r = Resolution;
            Vector2 dimensions = Size;
            if (mesh == null)
                mesh = new Mesh { name = "Water Grid (Runtime)", hideFlags = HideFlags.HideAndDontSave };
            mesh.Clear(); mesh.indexFormat = (r + 1) * (r + 1) > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            var vertices = new Vector3[(r + 1) * (r + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[r * r * 6];
            for (int z = 0; z <= r; z++)
                for (int x = 0; x <= r; x++)
                {
                    int i = z * (r + 1) + x;
                    uv[i] = new Vector2((float)x / r, (float)z / r);
                    vertices[i] = new Vector3((uv[i].x - 0.5f) * dimensions.x, 0f, (uv[i].y - 0.5f) * dimensions.y);
                }
            for (int z = 0, i = 0; z < r; z++)
                for (int x = 0; x < r; x++)
                {
                    int a = z * (r + 1) + x, b = a + 1, c = a + r + 1, d = c + 1;
                    triangles[i++] = a; triangles[i++] = c; triangles[i++] = b;
                    triangles[i++] = b; triangles[i++] = c; triangles[i++] = d;
                }
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); UpdateBounds();
            filter.sharedMesh = mesh;
            builtSize = dimensions; builtResolution = r; rebuildPending = false;
            renderTime = SurfaceTime;
            if (material != null) ApplyShaderProperties(material);
        }
        void UpdateBounds()
        {
            if (mesh != null) mesh.bounds = new Bounds(Vector3.zero,
                new Vector3(Size.x, Mathf.Max(0.1f, Profile.MaximumDisplacement * 2f + 0.1f), Size.y));
        }

        public bool ContainsXZ(Vector3 point)
        {
            var p = point - transform.position; var s = Size;
            return HasSupportedTransform && Mathf.Abs(p.x) <= s.x * 0.5f && Mathf.Abs(p.z) <= s.y * 0.5f;
        }
        public bool Contains(Vector3 point, float time) => ContainsXZ(point) &&
            point.y >= transform.position.y - Depth && point.y <= Sample(point, time).Height;
        public WaterSample Sample(Vector3 point, float time) =>
            WaterWaves.EvaluateGrid(Profile, transform.position, Size, Resolution, point, time);
        public bool TrySample(Vector3 point, float time, out WaterSample sample)
        {
            if (!isActiveAndEnabled || !ContainsXZ(point)) { sample = default; return false; }
            sample = Sample(point, time); return true;
        }
        public static WaterBody Find(Vector3 point)
        {
            WaterBody best = null; float top = float.NegativeInfinity;
            foreach (var body in bodies)
            {
                if (body == null || !body.isActiveAndEnabled || !body.ContainsXZ(point) || point.y < body.transform.position.y - body.Depth) continue;
                float height = body.Sample(point, body.SurfaceTime).Height;
                if (height > top) { top = height; best = body; }
            }
            return best;
        }

        public void ApplyShaderProperties(Material target) { if (target != null) WriteProperties(new PropertyWriter(target, null)); }
        public void ApplyShaderProperties(MaterialPropertyBlock target) { if (target != null) WriteProperties(new PropertyWriter(null, target)); }

        // No global shader state: different bodies and cameras cannot overwrite each other's material data.
        void WriteProperties(PropertyWriter writer)
        {
            var p = Profile; var s = Size;
            int count = p.Waves == null ? 0 : Mathf.Min(p.Waves.Length, WaterProfile.MaximumWaves);
            for (int i = 0; i < WaterProfile.MaximumWaves; i++)
            {
                if (i < count) WaterWaves.Pack(p.Waves[i], out waveShapes[i], out waveMotions[i]);
                else { waveShapes[i] = Vector4.zero; waveMotions[i] = Vector4.zero; }
            }
            writer.Vector("_WaterOrigin", new Vector4(transform.position.x, transform.position.y, transform.position.z, Depth));
            writer.Vector("_WaterSize", new Vector4(s.x, s.y, Resolution, 0f));
            writer.Float("_WaterTime", renderTime); writer.Int("_WaterWaveCount", count);
            writer.Array("_WaterWaves", waveShapes); writer.Array("_WaterWaveMotion", waveMotions);
            writer.Color("_WaterShallowColor", p.ShallowColor); writer.Color("_WaterDeepColor", p.DeepColor);
            writer.Color("_WaterFogColor", p.FogColor); writer.Color("_WaterFoamColor", p.FoamColor);
            writer.Color("_WaterlineColor", p.WaterlineColor);
            writer.Vector("_WaterAbsorption", new Vector4(Mathf.Max(0f, WaterWaves.Finite(p.Absorption.x)),
                Mathf.Max(0f, WaterWaves.Finite(p.Absorption.y)), Mathf.Max(0f, WaterWaves.Finite(p.Absorption.z)), 0f));
            writer.Float("_WaterDensity", p.Density, 0f, 100f);
            writer.Float("_WaterRefraction", p.Refraction, 0f, 0.05f);
            writer.Float("_WaterSmoothness", p.Smoothness, 0f, 1f);
            writer.Float("_WaterFoamWidth", p.FoamWidth, 0f, 100f);
            writer.Float("_WaterFoamStrength", p.FoamStrength, 0f, 1f);
            writer.Float("_WaterDetailNormalStrength", p.DetailNormalStrength, 0f, 0.5f);
            writer.Float("_WaterUnderwaterSaturation", p.UnderwaterSaturation, 0f, 1f);
            writer.Float("_WaterDistortion", p.Distortion, 0f, 0.02f);
            writer.Float("_WaterlineWidth", p.WaterlineWidth, 0f, 20f);
            writer.Float("_WaterlineStrength", p.WaterlineStrength, 0f, 1f);
            writer.Float("_WaterCausticsStrength", p.CausticsStrength, 0f, 3f);
            writer.Float("_WaterCausticsScale", p.CausticsScale, 0.01f, 100f);
            writer.Float("_WaterCausticsSpeed", p.CausticsSpeed, -100f, 100f);
        }
        readonly struct PropertyWriter
        {
            readonly Material material; readonly MaterialPropertyBlock block;
            public PropertyWriter(Material material, MaterialPropertyBlock block) { this.material = material; this.block = block; }
            public void Float(string name, float value, float min = float.MinValue, float max = float.MaxValue)
            { value = Mathf.Clamp(WaterWaves.Finite(value), min, max); if (material != null) material.SetFloat(name, value); else block.SetFloat(name, value); }
            public void Int(string name, int value) { if (material != null) material.SetInt(name, value); else block.SetInt(name, value); }
            public void Vector(string name, Vector4 value) { if (material != null) material.SetVector(name, value); else block.SetVector(name, value); }
            public void Color(string name, Color value)
            { if (QualitySettings.activeColorSpace == ColorSpace.Linear) value = value.linear; if (material != null) material.SetColor(name, value); else block.SetColor(name, value); }
            public void Array(string name, Vector4[] value) { if (material != null) material.SetVectorArray(name, value); else block.SetVectorArray(name, value); }
        }
    }
}
