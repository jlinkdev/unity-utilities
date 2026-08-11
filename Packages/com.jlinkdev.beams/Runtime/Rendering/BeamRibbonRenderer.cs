using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace jlinkdev.UnityUtilities.Beams
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("jlinkdev/Beams/Rendering/Beam Ribbon Renderer")]
    public sealed class BeamRibbonRenderer : BeamPathRenderer
    {
        public const string DefaultShaderName = "jlinkdev/Beams/Energy Beam";

        [SerializeField] private BeamRenderProfile profile;
        [SerializeField, Min(0.0001f)] private float width = 0.08f;
        [SerializeField, Min(0f)] private float boundsPadding = 0.3f;
        [SerializeField, ColorUsage(true, true)] private Color color = new Color(0.1f, 0.8f, 1.5f, 1f);
        [SerializeField, Min(0f)] private float intensity = 1f;
        [SerializeField] private Camera facingCamera;
        [SerializeField, Range(-1f, 2f)] private float pulsePosition = -1f;

        [NonSerialized] private Mesh mesh;
        [NonSerialized] private MeshFilter meshFilter;
        [NonSerialized] private MeshRenderer meshRenderer;
        [NonSerialized] private MaterialPropertyBlock propertyBlock;

        private readonly List<Vector3> vertices = new List<Vector3>(64);
        private readonly List<Vector3> normals = new List<Vector3>(64);
        private readonly List<Vector4> tangents = new List<Vector4>(64);
        private readonly List<Vector2> uv0 = new List<Vector2>(64);
        private readonly List<Vector2> uv1 = new List<Vector2>(64);
        private readonly List<Vector2> uv2 = new List<Vector2>(64);
        private readonly List<Color> colors = new List<Color>(64);
        private readonly List<int> triangles = new List<int>(192);

        public float Width
        {
            get => width;
            set => width = Mathf.Max(0.0001f, value);
        }

        public BeamRenderProfile Profile
        {
            get => profile;
            set => profile = value;
        }

        public float PulsePosition
        {
            get => pulsePosition;
            set => pulsePosition = value;
        }

        public Camera FacingCamera
        {
            get => facingCamera;
            set => facingCamera = value;
        }

        public Color Color
        {
            get => color;
            set => color = value;
        }

        public float Intensity
        {
            get => intensity;
            set => intensity = Mathf.Max(0f, value);
        }

        public override void Render(BeamPathBuffer paths, in BeamRenderContext context)
        {
            EnsureState();
            ClearLists();

            Camera camera = facingCamera != null ? facingCamera : Camera.main;
            float maximumLength = 0f;
            for (int strandIndex = 0; strandIndex < paths.Count; strandIndex++)
            {
                BeamStrand strand = paths[strandIndex];
                if (strand.Count < 2)
                    continue;

                maximumLength = Mathf.Max(maximumLength, strand.Length);
                AppendStrand(strand, strandIndex, camera);
            }

            mesh.Clear(false);
            mesh.indexFormat = vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTangents(tangents);
            mesh.SetUVs(0, uv0);
            mesh.SetUVs(1, uv1);
            mesh.SetUVs(2, uv2);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0, true);

            Bounds bounds = mesh.bounds;
            bounds.Expand(EffectiveBoundsPadding * 2f);
            mesh.bounds = bounds;

            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BeamShaderProperties.Color, EffectiveColor);
            propertyBlock.SetFloat(BeamShaderProperties.Intensity, EffectiveIntensity);
            propertyBlock.SetFloat(BeamShaderProperties.Length, maximumLength);
            propertyBlock.SetFloat(BeamShaderProperties.Time, context.Time);
            propertyBlock.SetFloat(BeamShaderProperties.Age, context.Age);
            propertyBlock.SetFloat(BeamShaderProperties.Seed, context.Seed);
            propertyBlock.SetFloat(BeamShaderProperties.PulsePosition, pulsePosition);
            propertyBlock.SetFloat(BeamShaderProperties.Activation, 1f);
            meshRenderer.SetPropertyBlock(propertyBlock);

            if (profile != null && profile.Material != null && meshRenderer.sharedMaterial != profile.Material)
                meshRenderer.sharedMaterial = profile.Material;
        }

        public override void Clear()
        {
            if (mesh != null)
                mesh.Clear(false);
            if (meshRenderer != null)
            {
                EnsureState();
                meshRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(BeamShaderProperties.Activation, 0f);
                meshRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void AppendStrand(BeamStrand strand, int strandIndex, Camera camera)
        {
            int baseVertex = vertices.Count;
            Vector3 previousWidthAxis = Vector3.zero;
            float strandSeed = Mathf.Repeat(strand.Seed * 0.61803398875f, 1024f);

            for (int pointIndex = 0; pointIndex < strand.Count; pointIndex++)
            {
                BeamPoint point = strand[pointIndex];
                Vector3 viewDirection = camera != null
                    ? camera.transform.position - point.Position
                    : transform.forward;
                Vector3 widthAxis = Vector3.Cross(point.Tangent, viewDirection);
                if (widthAxis.sqrMagnitude < 0.000001f)
                    widthAxis = previousWidthAxis.sqrMagnitude > 0.000001f ? previousWidthAxis : point.Normal;
                widthAxis.Normalize();
                if (previousWidthAxis.sqrMagnitude > 0f && Vector3.Dot(widthAxis, previousWidthAxis) < 0f)
                    widthAxis = -widthAxis;
                previousWidthAxis = widthAxis;

                float pointWidth = EffectiveWidth * EffectiveWidthCurve.Evaluate(point.NormalizedDistance);
                pointWidth *= Mathf.Pow(EffectiveBranchWidthMultiplier, strand.BranchDepth);
                Vector3 left = point.Position - widthAxis * (pointWidth * 0.5f);
                Vector3 right = point.Position + widthAxis * (pointWidth * 0.5f);
                Vector3 normalOS = transform.InverseTransformDirection(point.Normal).normalized;
                Vector3 tangentOS = transform.InverseTransformDirection(point.Tangent).normalized;

                vertices.Add(transform.InverseTransformPoint(left));
                vertices.Add(transform.InverseTransformPoint(right));
                normals.Add(normalOS);
                normals.Add(normalOS);
                tangents.Add(new Vector4(tangentOS.x, tangentOS.y, tangentOS.z, 1f));
                tangents.Add(new Vector4(tangentOS.x, tangentOS.y, tangentOS.z, 1f));
                uv0.Add(new Vector2(point.NormalizedDistance, -1f));
                uv0.Add(new Vector2(point.NormalizedDistance, 1f));
                uv1.Add(new Vector2(point.Distance, pointWidth));
                uv1.Add(new Vector2(point.Distance, pointWidth));
                uv2.Add(new Vector2(strandSeed, strandIndex));
                uv2.Add(new Vector2(strandSeed, strandIndex));
                Color vertexColor = EffectiveGradient.Evaluate(point.NormalizedDistance);
                colors.Add(vertexColor);
                colors.Add(vertexColor);

                if (pointIndex >= strand.Count - 1)
                    continue;

                int vertex = baseVertex + pointIndex * 2;
                triangles.Add(vertex);
                triangles.Add(vertex + 2);
                triangles.Add(vertex + 1);
                triangles.Add(vertex + 1);
                triangles.Add(vertex + 2);
                triangles.Add(vertex + 3);
            }
        }

        private void EnsureState()
        {
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
            if (mesh == null)
            {
                mesh = new Mesh { name = "Beam Ribbon (Generated)", hideFlags = HideFlags.DontSave };
                mesh.MarkDynamic();
                meshFilter.sharedMesh = mesh;
            }
        }

        private void ClearLists()
        {
            vertices.Clear();
            normals.Clear();
            tangents.Clear();
            uv0.Clear();
            uv1.Clear();
            uv2.Clear();
            colors.Clear();
            triangles.Clear();
        }

        private void OnEnable()
        {
            EnsureState();
        }

        private void OnDestroy()
        {
            if (mesh == null)
                return;
            if (Application.isPlaying)
                Destroy(mesh);
            else
                DestroyImmediate(mesh);
            mesh = null;
        }

        private void OnValidate()
        {
            width = Mathf.Max(0.0001f, width);
            boundsPadding = Mathf.Max(0f, boundsPadding);
            intensity = Mathf.Max(0f, intensity);
        }

        private float EffectiveWidth => profile != null ? profile.Width : width;
        private float EffectiveBoundsPadding => profile != null ? profile.BoundsPadding : boundsPadding;
        private float EffectiveBranchWidthMultiplier => profile != null ? profile.BranchWidthMultiplier : 0.62f;
        private Color EffectiveColor => profile != null ? profile.Color : color;
        private float EffectiveIntensity => profile != null ? profile.Intensity : intensity;
        private AnimationCurve EffectiveWidthCurve => profile != null && profile.WidthAlongStrand != null
            ? profile.WidthAlongStrand
            : DefaultWidthCurve;
        private Gradient EffectiveGradient => profile != null && profile.ColorAlongStrand != null
            ? profile.ColorAlongStrand
            : DefaultGradient;

        private static readonly Gradient DefaultGradient = CreateDefaultGradient();
        private static readonly AnimationCurve DefaultWidthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        private static Gradient CreateDefaultGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }
    }
}
