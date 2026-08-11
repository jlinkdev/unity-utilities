using UnityEngine;

namespace jlinkdev.UnityUtilities.WorldScanning
{
    [CreateAssetMenu(menuName = "jlinkdev/World Scanning/Scan Profile", fileName = "Scan Profile")]
    public sealed class ScanProfile : ScriptableObject
    {
        [Header("Pulse")]
        [SerializeField] private ScanShape shape = ScanShape.Sphere;
        [SerializeField, Min(0.05f)] private float duration = 2.5f;
        [SerializeField, Min(0.01f)] private float range = 45f;
        [SerializeField] private AnimationCurve radiusOverLifetime = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve intensityOverLifetime = AnimationCurve.EaseInOut(0f, 0f, 0.12f, 1f);
        [SerializeField] private Gradient colorOverLifetime = new Gradient();
        [SerializeField, Min(0f)] private float cylinderHalfHeight = 12f;
        [SerializeField] private ScanTimeMode timeMode = ScanTimeMode.Scaled;

        [Header("Surface Band")]
        [SerializeField, Min(0.001f)] private float bandWidth = 1.2f;
        [SerializeField, Min(0.001f)] private float bandSoftness = 0.45f;
        [SerializeField, Min(0f)] private float trailLength = 12f;
        [SerializeField, Range(0f, 1f)] private float trailIntensity = 0.12f;
        [SerializeField, Min(0f)] private float emissionIntensity = 1.4f;

        [Header("World Grid")]
        [SerializeField] private bool gridEnabled = true;
        [SerializeField, Min(0.01f)] private float gridCellSize = 1.5f;
        [SerializeField, Range(0.001f, 0.49f)] private float gridLineWidth = 0.045f;
        [SerializeField, Min(1)] private int gridMajorEvery = 5;
        [SerializeField, Range(0f, 1f)] private float gridIntensity = 0.4f;
        [SerializeField, Range(0f, 1f)] private float gridMajorIntensity = 0.75f;

        [Header("Geometry Accents")]
        [SerializeField] private bool edgesEnabled = true;
        [SerializeField, Min(0f)] private float edgeIntensity = 0.9f;
        [SerializeField, Min(0.00001f)] private float depthEdgeThreshold = 0.08f;
        [SerializeField, Range(0.001f, 2f)] private float normalEdgeThreshold = 0.22f;
        [SerializeField, Range(0.25f, 4f)] private float edgeThickness = 1f;

        [Header("Variation")]
        [SerializeField, Min(0.001f)] private float noiseScale = 0.18f;
        [SerializeField, Range(0f, 1f)] private float noiseStrength = 0.18f;
        [SerializeField] private float noiseSpeed = 0.35f;
        [SerializeField, Min(0f)] private float cameraDistanceFadeStart = 80f;
        [SerializeField, Min(0f)] private float cameraDistanceFadeEnd = 140f;

        public ScanShape Shape => shape;
        public float Duration => duration;
        public float Range => range;
        public ScanTimeMode TimeMode => timeMode;
        public float CylinderHalfHeight => cylinderHalfHeight;

        internal float EvaluateRadius(float normalizedTime)
        {
            return range * Mathf.Clamp01(radiusOverLifetime == null ? normalizedTime : radiusOverLifetime.Evaluate(normalizedTime));
        }

        internal float EvaluateIntensity(float normalizedTime)
        {
            float curve = intensityOverLifetime == null ? 1f : intensityOverLifetime.Evaluate(normalizedTime);
            return Mathf.Max(0f, curve) * emissionIntensity;
        }

        internal Color EvaluateColor(float normalizedTime)
        {
            return colorOverLifetime == null ? new Color(0.05f, 0.8f, 1f, 1f) : colorOverLifetime.Evaluate(normalizedTime);
        }

        internal ScanVisualSettings GetVisualSettings()
        {
            return new ScanVisualSettings(
                bandWidth,
                bandSoftness,
                trailLength,
                trailIntensity,
                gridEnabled ? gridCellSize : 0f,
                gridLineWidth,
                gridMajorEvery,
                gridIntensity,
                gridMajorIntensity,
                edgesEnabled ? edgeIntensity : 0f,
                depthEdgeThreshold,
                normalEdgeThreshold,
                edgeThickness,
                noiseScale,
                noiseStrength,
                noiseSpeed,
                cameraDistanceFadeStart,
                Mathf.Max(cameraDistanceFadeStart + 0.01f, cameraDistanceFadeEnd));
        }

        private void Reset()
        {
            colorOverLifetime = new Gradient();
            colorOverLifetime.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.1f, 0.95f, 1f), 0f),
                    new GradientColorKey(new Color(0.05f, 0.38f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 1f)
                });
        }

        private void OnValidate()
        {
            duration = Mathf.Max(0.05f, duration);
            range = Mathf.Max(0.01f, range);
            bandWidth = Mathf.Max(0.001f, bandWidth);
            bandSoftness = Mathf.Max(0.001f, bandSoftness);
            gridCellSize = Mathf.Max(0.01f, gridCellSize);
            gridMajorEvery = Mathf.Max(1, gridMajorEvery);
            cameraDistanceFadeEnd = Mathf.Max(cameraDistanceFadeStart + 0.01f, cameraDistanceFadeEnd);
        }
    }

    internal readonly struct ScanVisualSettings
    {
        public ScanVisualSettings(
            float bandWidth,
            float bandSoftness,
            float trailLength,
            float trailIntensity,
            float gridCellSize,
            float gridLineWidth,
            int gridMajorEvery,
            float gridIntensity,
            float gridMajorIntensity,
            float edgeIntensity,
            float depthEdgeThreshold,
            float normalEdgeThreshold,
            float edgeThickness,
            float noiseScale,
            float noiseStrength,
            float noiseSpeed,
            float cameraDistanceFadeStart,
            float cameraDistanceFadeEnd)
        {
            BandWidth = bandWidth;
            BandSoftness = bandSoftness;
            TrailLength = trailLength;
            TrailIntensity = trailIntensity;
            GridCellSize = gridCellSize;
            GridLineWidth = gridLineWidth;
            GridMajorEvery = gridMajorEvery;
            GridIntensity = gridIntensity;
            GridMajorIntensity = gridMajorIntensity;
            EdgeIntensity = edgeIntensity;
            DepthEdgeThreshold = depthEdgeThreshold;
            NormalEdgeThreshold = normalEdgeThreshold;
            EdgeThickness = edgeThickness;
            NoiseScale = noiseScale;
            NoiseStrength = noiseStrength;
            NoiseSpeed = noiseSpeed;
            CameraDistanceFadeStart = cameraDistanceFadeStart;
            CameraDistanceFadeEnd = cameraDistanceFadeEnd;
        }

        public float BandWidth { get; }
        public float BandSoftness { get; }
        public float TrailLength { get; }
        public float TrailIntensity { get; }
        public float GridCellSize { get; }
        public float GridLineWidth { get; }
        public int GridMajorEvery { get; }
        public float GridIntensity { get; }
        public float GridMajorIntensity { get; }
        public float EdgeIntensity { get; }
        public float DepthEdgeThreshold { get; }
        public float NormalEdgeThreshold { get; }
        public float EdgeThickness { get; }
        public float NoiseScale { get; }
        public float NoiseStrength { get; }
        public float NoiseSpeed { get; }
        public float CameraDistanceFadeStart { get; }
        public float CameraDistanceFadeEnd { get; }
    }
}
