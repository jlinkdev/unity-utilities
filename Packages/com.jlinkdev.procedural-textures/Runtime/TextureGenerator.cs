using System;
using UnityEngine;

namespace JLink.ProceduralTextures
{
    public enum TextureColorMode { Grayscale, AlphaMask, ColorRamp }
    public enum NoiseBasis { Value, Perlin, Simplex }
    public enum StampShape { Circle, Square, Diamond }
    public enum ValueRemap { None, Levels, Threshold, Curve }
    public enum WarpDirection { Both, Horizontal, Vertical }
    public enum BlurMode { None, Isotropic, Directional }

    public static class TextureGenerator
    {
        public static Texture2D Generate(TextureRecipe recipe) => Rasterize(recipe, recipe.width, recipe.height, recipe.colorMode);

        public static Texture2D GeneratePreview(TextureRecipe recipe, int maximumDimension) =>
            GeneratePreview(recipe, maximumDimension, false);

        public static Texture2D GeneratePreview(TextureRecipe recipe, int maximumDimension, bool scalar)
        {
            if (maximumDimension < 1) throw new ArgumentOutOfRangeException(nameof(maximumDimension));
            float scale = Mathf.Min(1f, (float)maximumDimension / Mathf.Max(recipe.width, recipe.height));
            return Rasterize(recipe, Mathf.Max(1, Mathf.RoundToInt(recipe.width * scale)),
                Mathf.Max(1, Mathf.RoundToInt(recipe.height * scale)), scalar ? TextureColorMode.Grayscale : recipe.colorMode);
        }

        static Texture2D Rasterize(TextureRecipe recipe, int width, int height, TextureColorMode colorMode)
        {
            var field = new TextureField(recipe, width, height);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = Coordinate(y, height, field.Endpoints);
                for (int x = 0; x < width; x++)
                {
                    float value = field.SamplePixel(x, y, Coordinate(x, width, field.Endpoints), v);
                    byte level = (byte)Mathf.RoundToInt(value * 255f);
                    switch (colorMode)
                    {
                        case TextureColorMode.AlphaMask: pixels[y * width + x] = new Color32(255, 255, 255, level); break;
                        case TextureColorMode.ColorRamp:
                            pixels[y * width + x] = recipe.colorRamp == null
                                ? Color.Lerp(recipe.lowColor, recipe.highColor, value)
                                : recipe.colorRamp.Evaluate(value);
                            break;
                        default: pixels[y * width + x] = new Color32(level, level, level, 255); break;
                    }
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                wrapModeU = field.RepeatU ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                wrapModeV = field.RepeatV ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            texture.SetPixels32(pixels);
            texture.Apply(false);
            return texture;
        }

        internal static float Coordinate(int pixel, int count, bool endpoints)
        {
            if (!endpoints) return (pixel + 0.5f) / count;
            return count == 1 ? 0.5f : (float)pixel / (count - 1);
        }
    }

    // Compile the coordinate transform and polygon outline once for an entire bake.
    public sealed class TextureField
    {
        readonly TextureRecipe r;
        readonly float cos, sin, aspectX, aspectY;
        readonly Vector2[] polygon;
        readonly Func<float, float, float> sample;

        readonly Vector2 stepU, stepV, warpAmplitude;
        readonly bool transformCoordinates, baseRepeatU, baseRepeatV;
        readonly ScalarFilterGrid filtered;
        internal bool Endpoints { get; }
        internal bool RepeatU { get; }
        internal bool RepeatV { get; }

        public TextureField(TextureRecipe recipe) : this(recipe, recipe.width, recipe.height) { }

        internal TextureField(TextureRecipe recipe, int width, int height)
        {
            recipe.Validate();
            r = recipe;
            float angle = r.rotation * Mathf.Deg2Rad;
            cos = Mathf.Cos(angle);
            sin = Mathf.Sin(angle);
            int shorter = Mathf.Min(r.width, r.height);
            aspectX = (float)r.width / shorter;
            aspectY = (float)r.height / shorter;
            string family = TextureCatalog.Find(r.generator).Menu.Split('/')[0];
            bool independent = family == "Noise" || r.generator == "scattered-shapes";
            baseRepeatU = independent ? r.seamlessX : family == "Patterns";
            baseRepeatV = independent ? r.seamlessY : family == "Patterns";
            Endpoints = r.fitTexelEndpoints && (r.generator == "linear" || r.generator == "curve-gradient");
            transformCoordinates = !r.coordinateScale.Equals(Vector2.one) || r.coordinateRotation != 0;
            // Inverse rotation maps an output UV back into the centered generator domain.
            float radians = (r.coordinateRotation % 360f) * Mathf.Deg2Rad;
            float c = Mathf.Cos(radians), s = Mathf.Sin(radians);
            if (r.coordinateRotation % 90f == 0) { c = Mathf.Round(c); s = Mathf.Round(s); }
            stepU = new Vector2(c * r.coordinateScale.x, -s * r.coordinateScale.y);
            stepV = new Vector2(s * r.coordinateScale.x, c * r.coordinateScale.y);
            RepeatU = IsPeriod(stepU.x, baseRepeatU) && IsPeriod(stepU.y, baseRepeatV);
            RepeatV = IsPeriod(stepV.x, baseRepeatU) && IsPeriod(stepV.y, baseRepeatV);
            if (r.domainWarp)
            {
                int extent = ScalarFilterGrid.ShortSide(r.width, r.height);
                warpAmplitude = new Vector2(r.width == 1 || r.warpDirection == WarpDirection.Vertical ? 0 : r.warpAmount * extent / r.width,
                    r.height == 1 || r.warpDirection == WarpDirection.Horizontal ? 0 : r.warpAmount * extent / r.height);
            }
            switch (family)
            {
                case "Noise": sample = new NoiseField(r).Sample; break;
                case "Gradients": sample = Gradient; break;
                case "Masks": sample = Mask; break;
                default: sample = Pattern; break;
            }
            if (r.generator == "polygon" || r.generator == "star")
            {
                bool star = r.generator == "star";
                polygon = new Vector2[r.points * (star ? 2 : 1)];
                for (int i = 0; i < polygon.Length; i++)
                {
                    float a = Mathf.PI * 2 * i / polygon.Length + Mathf.PI * 0.5f;
                    float radius = 0.5f * r.sizeX * (star && (i & 1) == 1 ? r.innerRatio : 1f);
                    polygon[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                }
            }
            if ((r.blur != BlurMode.None && r.blurRadius > 0) || r.detectEdges)
                filtered = new ScalarFilterGrid(r, width, height, Endpoints, RepeatU, RepeatV, Source);
        }

        static bool IsPeriod(float delta, bool periodic) => delta == 0 || (periodic && delta == Mathf.Round(delta));

        float Source(float u, float v)
        {
            u -= r.offset.x;
            v -= r.offset.y;
            if (transformCoordinates)
            {
                Vector2 p = stepU * (u - 0.5f) + stepV * (v - 0.5f);
                u = p.x + 0.5f;
                v = p.y + 0.5f;
            }
            if (r.domainWarp && r.warpAmount > 0)
            {
                float x = u, y = v;
                if (warpAmplitude.x != 0) u += warpAmplitude.x * Displacement(r.warpSeed, x, y);
                if (warpAmplitude.y != 0) v += warpAmplitude.y * Displacement(r.warpSeed ^ 0x519aa2, x, y);
            }
            return sample(u, v);
        }

        float Displacement(int seed, float u, float v) => 2f * ValueNoiseGenerator.Sample(seed, r.warpFrequency,
            r.warpFrequency, baseRepeatU, baseRepeatV, u, v) - 1f;

        public float Sample(float u, float v) => Remap(filtered == null ? Source(u, v) : filtered.Sample(u, v));

        internal float SamplePixel(int x, int y, float u, float v) =>
            Remap(filtered == null ? Source(u, v) : filtered.Pixel(x, y));

        float Remap(float value)
        {
            switch (r.remap)
            {
                case ValueRemap.Levels: value = Mathf.InverseLerp(r.blackPoint, r.whitePoint, value); break;
                case ValueRemap.Threshold: value = value < r.threshold ? 0f : 1f; break;
                case ValueRemap.Curve: value = r.remapCurve.Evaluate(Mathf.Clamp01(value)); break;
            }
            if (r.invert) value = 1f - value;
            if (r.contrast == 1f && r.brightness == 0f) return Mathf.Clamp01(value);
            return Mathf.Clamp01((value - 0.5f) * r.contrast + 0.5f + r.brightness);
        }

        Vector2 Rotate(Vector2 p) => new Vector2(cos * p.x + sin * p.y, -sin * p.x + cos * p.y);
        Vector2 Center(float u, float v) => Rotate(new Vector2((u - 0.5f) * aspectX, (v - 0.5f) * aspectY));

        float Gradient(float u, float v)
        {
            bool fit = r.fitTexelEndpoints && (r.generator == "linear" || r.generator == "curve-gradient");
            Vector2 p = (fit ? Rotate(new Vector2(u - 0.5f, v - 0.5f)) : Center(u, v)) / r.sizeX;
            switch (r.generator)
            {
                case "radial": return p.magnitude * 2;
                case "angular": return Mathf.Repeat(Mathf.Atan2(p.y, p.x) / (2 * Mathf.PI), 1);
                case "diamond": return (Mathf.Abs(p.x) + Mathf.Abs(p.y)) * 2;
                case "box-gradient": return Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) * 2;
                case "curve-gradient": return r.profile.Evaluate(Mathf.Clamp01(p.x + 0.5f));
                default: return p.x + 0.5f;
            }
        }

        static float BoxDistance(Vector2 p, float x, float y)
        {
            var q = new Vector2(Mathf.Abs(p.x) - x, Mathf.Abs(p.y) - y);
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0);
        }

        static float Coverage(float distance, float softness)
        {
            if (softness <= 0) return distance <= 0 ? 1 : 0;
            return Mathf.SmoothStep(0, 1, 0.5f - distance / softness);
        }

        float Mask(float u, float v)
        {
            Vector2 p = Center(u, v);
            float radius = r.sizeX * 0.5f;
            float halfWidth = radius * r.thickness;
            float d;
            switch (r.generator)
            {
                case "circle": d = p.magnitude - radius; break;
                case "soft-disc": return Mathf.SmoothStep(1, 0, p.magnitude / radius);
                case "ellipse":
                    var axes = new Vector2(radius, r.sizeY * 0.5f);
                    d = ((p / axes).magnitude - 1) * Mathf.Min(axes.x, axes.y);
                    break;
                case "rectangle": d = BoxDistance(p, radius, r.sizeY * 0.5f); break;
                case "rounded-rectangle":
                    float corner = r.corner * Mathf.Min(radius, r.sizeY * 0.5f);
                    d = BoxDistance(p, radius - corner, r.sizeY * 0.5f - corner) - corner;
                    break;
                case "ring": d = Mathf.Abs(p.magnitude - (radius - halfWidth * 0.5f)) - halfWidth * 0.5f; break;
                case "arc":
                    float a = Mathf.Clamp(Mathf.Atan2(p.y, p.x), -r.sweep * Mathf.Deg2Rad / 2, r.sweep * Mathf.Deg2Rad / 2);
                    d = (p - new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (radius - halfWidth)).magnitude - halfWidth;
                    break;
                case "line":
                    d = (p - new Vector2(Mathf.Clamp(p.x, -radius + halfWidth, radius - halfWidth), 0)).magnitude - halfWidth;
                    break;
                case "cross":
                    float major = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y));
                    float minor = Mathf.Min(Mathf.Abs(p.x), Mathf.Abs(p.y));
                    Vector2 q = new Vector2(major - radius, minor - halfWidth);
                    float side = Mathf.Max(q.x, q.y);
                    Vector2 edge = side > 0 ? q : new Vector2(halfWidth - major, -side);
                    d = Mathf.Sign(side) * Vector2.Max(edge, Vector2.zero).magnitude;
                    break;
                default: d = PolygonDistance(p); break;
            }
            return r.signedDistance ? 0.5f - d / (2 * r.distanceRange) : Coverage(d, r.softness);
        }

        float PolygonDistance(Vector2 p)
        {
            float nearest = float.PositiveInfinity;
            bool inside = false;
            Vector2 a = polygon[polygon.Length - 1];
            foreach (var b in polygon)
            {
                Vector2 edge = b - a;
                Vector2 delta = p - a;
                nearest = Mathf.Min(nearest, (delta - edge * Mathf.Clamp01(Vector2.Dot(delta, edge) / edge.sqrMagnitude)).sqrMagnitude);
                if ((a.y > p.y) != (b.y > p.y) && p.x < a.x + delta.y * edge.x / edge.y) inside = !inside;
                a = b;
            }
            return Mathf.Sqrt(nearest) * (inside ? -1f : 1f);
        }

        float Pattern(float u, float v)
        {
            float x = u * r.cellsX;
            float y = v * r.cellsY;
            float fx = Mathf.Repeat(x, 1f), fy = Mathf.Repeat(y, 1f);
            switch (r.generator)
            {
                case "checker": return (Mathf.FloorToInt(x * 2) + Mathf.FloorToInt(y * 2)) & 1;
                case "stripes": return Mathf.Repeat((r.alongU ? u : v) * r.cellsX, 1f) < r.duty ? 1f : 0f;
                case "grid": return Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fy, 1f - fy)) < r.thickness * 0.5f ? 1f : 0f;
                case "hex-grid": return HexEdges(x, y);
                case "scattered-shapes": return Scattered(x, y);
                default: return Coverage(Stamp(new Vector2(fx - 0.5f, fy - 0.5f)), r.softness);
            }
        }

        float Stamp(Vector2 delta)
        {
            Vector2 p = Rotate(delta);
            if (r.generator == "dots" || r.stamp == StampShape.Circle) return p.magnitude - r.sizeX * 0.5f;
            if (r.stamp == StampShape.Square) return BoxDistance(p, r.sizeX * 0.3535534f, r.sizeX * 0.3535534f);
            return (Mathf.Abs(p.x) + Mathf.Abs(p.y) - r.sizeX * 0.5f) * 0.7071068f;
        }

        float Scattered(float x, float y)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float distance = float.PositiveInfinity;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = ix + dx, cy = iy + dy;
                    Vector2 shift = NoiseField.FeatureOffset(r, cx, cy);
                    Vector2 delta = new Vector2(x - cx - 0.5f, y - cy - 0.5f) - shift;
                    float scale = 1f - r.sizeJitter * 0.95f * NoiseField.CellHash(r, cx, cy, 91);
                    if (r.stamp != StampShape.Circle)
                    {
                        float a = (2f * NoiseField.CellHash(r, cx, cy, 157) - 1f) * r.angleJitter * Mathf.Deg2Rad;
                        float c = Mathf.Cos(a), s = Mathf.Sin(a);
                        delta = new Vector2(c * delta.x + s * delta.y, -s * delta.x + c * delta.y);
                    }
                    distance = Mathf.Min(distance, Stamp(delta / scale) * scale);
                }
            return Coverage(distance, r.softness);
        }

        float HexEdges(float x, float y)
        {
            // Two staggered rows per repeat, with ideal triangular-lattice spacing.
            var p = new Vector2(x, y * 1.7320508f);
            int row = Mathf.RoundToInt(p.y / 0.8660254f);
            float nearest = float.PositiveInfinity;
            Vector2 local = default;
            for (int j = row - 1; j <= row + 1; j++)
            {
                float stagger = (j & 1) * 0.5f;
                var delta = p - new Vector2(Mathf.Round(p.x - stagger) + stagger, j * 0.8660254f);
                if (delta.sqrMagnitude < nearest) { nearest = delta.sqrMagnitude; local = delta; }
            }
            float edge = 0.5f - Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.x) * 0.5f + Mathf.Abs(local.y) * 0.8660254f);
            return edge < r.thickness * 0.5f ? 1f : 0f;
        }
    }
}
