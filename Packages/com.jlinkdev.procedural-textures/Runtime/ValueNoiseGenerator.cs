using System;
using UnityEngine;

namespace JLink.ProceduralTextures
{
    [Serializable]
    public sealed class TextureRecipe
    {
        public const int CurrentSchema = 4;
        public int schemaVersion = CurrentSchema;
        public string generator = "value-noise";
        public int generatorVersion = 1;
        public int width = 256;
        public int height = 256;
        public int seed = 1;
        public int cellsX = 8;
        public int cellsY = 8;
        public bool seamlessX = true;
        public bool seamlessY = true;
        public Vector2 offset;
        [Tooltip("UV sampling frequency. A scale of 2 samples twice as much of the generator along that axis.")]
        public Vector2 coordinateScale = Vector2.one;
        public float coordinateRotation;
        public bool domainWarp;
        [Tooltip("Maximum displacement per enabled axis as a fraction of the shorter image side.")]
        [Range(0f, 0.5f)] public float warpAmount = 0.1f;
        [Range(1, 64)] public int warpFrequency = 4;
        public int warpSeed = 1;
        public WarpDirection warpDirection;
        public BlurMode blur;
        [Tooltip("Blur radius as a fraction of the shorter side, or the length of a one-dimensional strip.")]
        [Range(0f, 0.25f)] public float blurRadius = 0.02f;
        public float blurAngle;
        public bool detectEdges;
        [Tooltip("Distance to neighbouring Sobel samples as a fraction of the shorter side, or strip length.")]
        [Range(0.001f, 0.1f)] public float edgeWidth = 0.01f;
        [Range(0f, 8f)] public float edgeStrength = 1f;
        public TextureColorMode colorMode;
        public Color lowColor = Color.black;
        public Color highColor = Color.white;
        public Gradient colorRamp;
        public bool fitTexelEndpoints;
        public bool invert;
        [Range(0f, 4f)] public float contrast = 1f;
        [Range(-1f, 1f)] public float brightness;
        public ValueRemap remap;
        [Range(0f, 1f)] public float blackPoint;
        [Range(0f, 1f)] public float whitePoint = 1f;
        [Range(0f, 1f)] public float threshold = 0.5f;
        public AnimationCurve remapCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public float rotation;
        [Range(0.01f, 1f)] public float sizeX = 0.7f;
        [Range(0.01f, 1f)] public float sizeY = 0.45f;
        [Range(0f, 0.25f)] public float softness = 0.015f;
        [Range(0.01f, 0.99f)] public float thickness = 0.15f;
        [Range(0f, 1f)] public float corner = 0.25f;
        [Range(3, 16)] public int points = 5;
        [Range(0.05f, 0.95f)] public float innerRatio = 0.45f;
        [Range(1f, 360f)] public float sweep = 240f;
        [Range(0.01f, 0.99f)] public float duty = 0.5f;
        [Range(0f, 1f)] public float jitter = 0.8f;
        public bool alongU = true;
        [Range(2, 16)] public int stretch = 4;
        public NoiseBasis basis = NoiseBasis.Perlin;
        [Range(1, 7)] public int octaves = 4;
        [Range(1, 3)] public int lacunarity = 2;
        [Range(0f, 1f)] public float persistence = 0.5f;
        [Range(0f, 1f)] public float warp = 0.25f;
        public StampShape stamp = StampShape.Square;
        [Range(0f, 1f)] public float sizeJitter = 0.35f;
        [Range(0f, 180f)] public float angleJitter = 30f;
        public AnimationCurve profile = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool signedDistance;
        [Range(0.001f, 0.5f)] public float distanceRange = 0.1f;

        public bool UsesField(string field)
        {
            switch (field)
            {
                case "warpAmount": case "warpFrequency": case "warpSeed": case "warpDirection": return domainWarp;
                case "blurRadius": return blur != BlurMode.None;
                case "blurAngle": return blur == BlurMode.Directional;
                case "edgeWidth": case "edgeStrength": return detectEdges;
                case "colorRamp": return colorMode == TextureColorMode.ColorRamp;
                case "blackPoint": case "whitePoint": return remap == ValueRemap.Levels;
                case "threshold": return remap == ValueRemap.Threshold;
                case "remapCurve": return remap == ValueRemap.Curve;
                case "distanceRange": return signedDistance;
                case "softness": return !signedDistance;
                case "angleJitter": return stamp != StampShape.Circle;
                default: return true;
            }
        }

        public Gradient CreateEndpointRamp()
        {
            var ramp = new Gradient();
            ramp.SetKeys(new[] { new GradientColorKey(lowColor, 0f), new GradientColorKey(highColor, 1f) },
                new[] { new GradientAlphaKey(lowColor.a, 0f), new GradientAlphaKey(highColor.a, 1f) });
            return ramp;
        }

        public static bool SupportsSchema(int version, string generator) =>
            (version >= 2 && version <= CurrentSchema) || (version == 1 && generator == "value-noise");

        public void Validate()
        {
            if (!SupportsSchema(schemaVersion, generator) || generatorVersion != 1)
                throw new ArgumentException("Unsupported texture recipe or generator version.");
            TextureCatalog.Find(generator);
            if (!float.IsFinite(coordinateRotation) || !float.IsFinite(coordinateScale.x) || !float.IsFinite(coordinateScale.y) ||
                Mathf.Abs(coordinateScale.x) > 512 || Mathf.Abs(coordinateScale.y) > 512)
                throw new ArgumentException("Coordinate scale must be within ±512 and rotation must be finite.");
            if (!(warpAmount >= 0 && warpAmount <= 0.5f && warpFrequency >= 1 && warpFrequency <= 64 &&
                blurRadius >= 0 && blurRadius <= 0.25f && edgeWidth >= 0.001f && edgeWidth <= 0.1f &&
                edgeStrength >= 0 && edgeStrength <= 8) || !float.IsFinite(blurAngle) ||
                !Enum.IsDefined(typeof(BlurMode), blur) || !Enum.IsDefined(typeof(WarpDirection), warpDirection))
                throw new ArgumentException("Texture processing settings are outside their supported ranges.");
            if (width < 1 || width > 4096 || height < 1 || height > 4096)
                throw new ArgumentOutOfRangeException(nameof(width), "Width and height must be between 1 and 4096.");
            if (cellsX < 1 || cellsX > 512 || cellsY < 1 || cellsY > 512)
                throw new ArgumentOutOfRangeException(nameof(cellsX), "Cell counts must be between 1 and 512.");
            if (!(sizeX > 0 && sizeX <= 1 && sizeY > 0 && sizeY <= 1 && distanceRange > 0 && distanceRange <= 0.5f) ||
                octaves < 1 || octaves > 7 ||
                lacunarity < 1 || lacunarity > 3 || points < 3 || points > 16 || stretch < 1 || stretch > 16)
                throw new ArgumentException("Invalid shape size, distance range, point count or noise detail.");
            if (!(softness >= 0 && softness <= 0.25f && thickness > 0 && thickness < 1 &&
                corner >= 0 && corner <= 1 && innerRatio > 0 && innerRatio < 1 && sweep > 0 && sweep <= 360 &&
                duty > 0 && duty < 1 && jitter >= 0 && jitter <= 1 && sizeJitter >= 0 && sizeJitter <= 1 &&
                persistence >= 0 && persistence <= 1 && warp >= 0 && warp <= 1))
                throw new ArgumentException("Shape, pattern or noise settings are outside their supported ranges.");
            if (!float.IsFinite(rotation) || !float.IsFinite(offset.x) || !float.IsFinite(offset.y) ||
                !float.IsFinite(angleJitter) || !float.IsFinite(brightness) || !float.IsFinite(contrast))
                throw new ArgumentException("Coordinate and output settings must be finite.");
            if (generator == "curve-gradient" && (profile == null || profile.length < 2))
                throw new ArgumentException("The gradient curve needs at least two keys.");
            if (remap == ValueRemap.Levels && !(blackPoint < whitePoint))
                throw new ArgumentException("White point must exceed black point.");
            if (remap == ValueRemap.Curve && (remapCurve == null || remapCurve.length < 2))
                throw new ArgumentException("The output curve needs at least two keys.");
        }
    }

    public static class ValueNoiseGenerator
    {
        public static Texture2D Generate(TextureRecipe recipe) => TextureGenerator.Generate(recipe);

        public static Texture2D GeneratePreview(TextureRecipe recipe, int maximumDimension) =>
            TextureGenerator.GeneratePreview(recipe, maximumDimension);

        public static float Sample(TextureRecipe recipe, float u, float v) =>
            Sample(recipe.seed, recipe.cellsX, recipe.cellsY, recipe.seamlessX, recipe.seamlessY, u, v);

        internal static float Sample(int seed, int cellsX, int cellsY, bool repeatX, bool repeatY, float u, float v)
        {
            float x = u * cellsX;
            float y = v * cellsY;
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float sx = Mathf.SmoothStep(0f, 1f, x - x0);
            float sy = Mathf.SmoothStep(0f, 1f, y - y0);
            int x1 = x0 + 1, y1 = y0 + 1;
            if (repeatX) { x0 = Wrap(x0, cellsX); x1 = Wrap(x1, cellsX); }
            if (repeatY) { y0 = Wrap(y0, cellsY); y1 = Wrap(y1, cellsY); }
            float bottom = Mathf.Lerp(Hash(seed, x0, y0), Hash(seed, x1, y0), sx);
            float top = Mathf.Lerp(Hash(seed, x0, y1), Hash(seed, x1, y1), sx);
            return Mathf.Lerp(bottom, top, sy);
        }

        internal static float Lattice(TextureRecipe recipe, int x, int y)
        {
            if (recipe.seamlessX) x = Wrap(x, recipe.cellsX);
            if (recipe.seamlessY) y = Wrap(y, recipe.cellsY);
            return Hash(recipe.seed, x, y);
        }

        static int Wrap(int value, int period) => (value % period + period) % period;

        internal static float Hash(int seed, int x, int y)
        {
            unchecked
            {
                uint hash = (uint)seed ^ (uint)x * 0x9e3779b1u ^ (uint)y * 0x85ebca77u;
                hash ^= hash >> 16;
                hash *= 0x7feb352du;
                hash ^= hash >> 15;
                hash *= 0x846ca68bu;
                return ((hash ^ (hash >> 16)) >> 8) / 16777215f;
            }
        }
    }
}
