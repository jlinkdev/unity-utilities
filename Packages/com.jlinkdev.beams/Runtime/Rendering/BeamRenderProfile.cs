using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Reusable geometry and material presentation shared by compatible beam renderers.</summary>
    [CreateAssetMenu(fileName = "Beam Render Profile", menuName = "jlinkdev/Beams/Beam Render Profile")]
    public sealed class BeamRenderProfile : ScriptableObject
    {
        [SerializeField] private Material material;
        [SerializeField, Min(0.0001f)] private float width = 0.08f;
        [SerializeField] private AnimationCurve widthAlongStrand = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float branchWidthMultiplier = 0.62f;
        [SerializeField, ColorUsage(true, true)] private Color color = new Color(0.1f, 0.8f, 1.5f, 1f);
        [SerializeField] private Gradient colorAlongStrand = DefaultGradient();
        [SerializeField, Min(0f)] private float intensity = 1f;
        [SerializeField, Min(0f)] private float boundsPadding = 0.3f;

        public Material Material => material;
        public float Width => width;
        public AnimationCurve WidthAlongStrand => widthAlongStrand;
        public float BranchWidthMultiplier => branchWidthMultiplier;
        public Color Color => color;
        public Gradient ColorAlongStrand => colorAlongStrand;
        public float Intensity => intensity;
        public float BoundsPadding => boundsPadding;

        private void OnValidate()
        {
            width = Mathf.Max(0.0001f, width);
            branchWidthMultiplier = Mathf.Clamp01(branchWidthMultiplier);
            intensity = Mathf.Max(0f, intensity);
            boundsPadding = Mathf.Max(0f, boundsPadding);
            if (widthAlongStrand == null || widthAlongStrand.length == 0)
                widthAlongStrand = AnimationCurve.Linear(0f, 1f, 1f, 1f);
            if (colorAlongStrand == null)
                colorAlongStrand = DefaultGradient();
        }

        private static Gradient DefaultGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }
    }
}
