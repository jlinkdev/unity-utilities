using System;
using UnityEngine;

namespace JLink.ProceduralTextures
{
    // Spatial filters operate on linear scalar samples. Two buffers keep combined passes
    // independent of traversal order and avoid re-evaluating expensive generators per tap.
    internal sealed class ScalarFilterGrid
    {
        readonly int width, height;
        readonly bool endpoints, repeatU, repeatV;
        float[] values;

        internal ScalarFilterGrid(TextureRecipe recipe, int width, int height, bool endpoints, bool repeatU, bool repeatV,
            Func<float, float, float> source)
        {
            this.width = width;
            this.height = height;
            this.endpoints = endpoints;
            this.repeatU = repeatU;
            this.repeatV = repeatV;
            values = new float[width * height];
            var scratch = new float[values.Length];
            for (int y = 0; y < height; y++)
            {
                float v = TextureGenerator.Coordinate(y, height, endpoints);
                for (int x = 0; x < width; x++)
                    values[y * width + x] = Mathf.Clamp01(source(TextureGenerator.Coordinate(x, width, endpoints), v));
            }

            // Use the authored aspect even when preview dimensions round to a single pixel.
            int extent = ShortSide(recipe.width, recipe.height);
            float scaleX = (endpoints ? width - 1 : width) * (float)extent / recipe.width;
            float scaleY = (endpoints ? height - 1 : height) * (float)extent / recipe.height;
            if (recipe.blur == BlurMode.Isotropic && recipe.blurRadius > 0)
            {
                // Two normalized box convolutions per axis give a tent-shaped footprint.
                for (int pass = 0; pass < 2; pass++)
                {
                    BoxAxis(ref scratch, width, height, 1, width, repeatU, recipe.blurRadius * scaleX * 0.5f);
                    BoxAxis(ref scratch, height, width, width, 1, repeatV, recipe.blurRadius * scaleY * 0.5f);
                }
            }
            else if (recipe.blur == BlurMode.Directional && recipe.blurRadius > 0)
            {
                float radians = (recipe.blurAngle % 180f) * Mathf.Deg2Rad;
                var radius = new Vector2(Mathf.Cos(radians) * scaleX, Mathf.Sin(radians) * scaleY) * recipe.blurRadius;
                Directional(ref scratch, radius);
            }
            if (recipe.detectEdges)
                Edges(ref scratch, recipe.edgeWidth * scaleX, recipe.edgeWidth * scaleY, recipe.edgeStrength);
        }

        internal static int ShortSide(int w, int h) => w == 1 || h == 1 ? Mathf.Max(w, h) : Mathf.Min(w, h);

        internal float Pixel(int x, int y) => values[y * width + x];

        internal float Sample(float u, float v)
        {
            u = repeatU ? Mathf.Repeat(u, 1f) : Mathf.Clamp01(u);
            v = repeatV ? Mathf.Repeat(v, 1f) : Mathf.Clamp01(v);
            return Read(endpoints ? u * (width - 1) : u * width - 0.5f,
                endpoints ? v * (height - 1) : v * height - 0.5f);
        }

        static int Address(int index, int count, bool repeat)
        {
            if ((uint)index < (uint)count) return index;
            if (!repeat) return index < 0 ? 0 : count - 1;
            int wrapped = index % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }

        float Read(float x, float y)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float tx = x - ix, ty = y - iy;
            int left = Address(ix, width, repeatU), right = Address(ix + 1, width, repeatU);
            int bottom = Address(iy, height, repeatV) * width, top = Address(iy + 1, height, repeatV) * width;
            float a = Mathf.LerpUnclamped(values[bottom + left], values[bottom + right], tx);
            float b = Mathf.LerpUnclamped(values[top + left], values[top + right], tx);
            return Mathf.LerpUnclamped(a, b, ty);
        }

        void BoxAxis(ref float[] scratch, int count, int lines, int stride, int lineStride, bool repeat, float radius)
        {
            if (count == 1 || radius <= 0) return;
            int whole = Mathf.FloorToInt(radius);
            float fraction = radius - whole;
            float normalization = 1f / (2f * radius + 1f);
            for (int line = 0; line < lines; line++)
            {
                int start = line * lineStride;
                double sum = 0;
                for (int tap = -whole; tap <= whole; tap++)
                    sum += values[start + Address(tap, count, repeat) * stride];
                for (int i = 0; i < count; i++)
                {
                    float outerLeft = values[start + Address(i - whole - 1, count, repeat) * stride];
                    float outerRight = values[start + Address(i + whole + 1, count, repeat) * stride];
                    scratch[start + i * stride] = (float)(sum + fraction * (outerLeft + outerRight)) * normalization;
                    sum += outerRight - (double)values[start + Address(i - whole, count, repeat) * stride];
                }
            }
            Swap(ref scratch);
        }

        void Directional(ref float[] scratch, Vector2 radius)
        {
            // Successively double the span of a line average. The finest taps are at
            // most one texel apart even for long streaks, with logarithmic pass count.
            if (width == 1) radius.x = 0;
            if (height == 1) radius.y = 0;
            float span = 2 * Mathf.Max(Mathf.Abs(radius.x), Mathf.Abs(radius.y));
            if (span <= 0) return;
            int taps = Mathf.NextPowerOfTwo(Mathf.CeilToInt(span) + 1);
            Vector2 shift = radius / (taps - 1);
            for (int samples = 1; samples < taps; samples *= 2)
            {
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        scratch[y * width + x] = 0.5f * (Read(x - shift.x, y - shift.y) + Read(x + shift.x, y + shift.y));
                Swap(ref scratch);
                shift *= 2;
            }
        }

        void Edges(ref float[] scratch, float dx, float dy, float strength)
        {
            // Sobel at a fractional, resolution-scaled distance. A full unit step
            // produces a response of one at strength one.
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float bl = Read(x - dx, y - dy), br = Read(x + dx, y - dy);
                    float tl = Read(x - dx, y + dy), tr = Read(x + dx, y + dy);
                    float gx = br + tr - bl - tl + 2 * (Read(x + dx, y) - Read(x - dx, y));
                    float gy = tl + tr - bl - br + 2 * (Read(x, y + dy) - Read(x, y - dy));
                    scratch[y * width + x] = Mathf.Clamp01(Mathf.Sqrt(gx * gx + gy * gy) * 0.25f * strength);
                }
            Swap(ref scratch);
        }

        void Swap(ref float[] scratch)
        {
            var previous = values;
            values = scratch;
            scratch = previous;
        }
    }
}
