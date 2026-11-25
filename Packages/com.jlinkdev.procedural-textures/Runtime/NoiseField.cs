using Unity.Mathematics;
using UnityEngine;

namespace JLink.ProceduralTextures
{
    internal sealed class NoiseField
    {
        readonly TextureRecipe r;
        readonly float2 translation;
        readonly float rotation;

        public NoiseField(TextureRecipe recipe)
        {
            r = recipe;
            translation = new float2(ValueNoiseGenerator.Hash(r.seed, 3, 5), ValueNoiseGenerator.Hash(r.seed, 7, 11)) * 211f;
            rotation = ValueNoiseGenerator.Hash(r.seed, 17, 23);
        }

        public float Sample(float u, float v)
        {
            if (r.generator == "value-noise") return ValueNoiseGenerator.Sample(r, u, v);
            if (r.seamlessX) u = Mathf.Repeat(u, 1f);
            if (r.seamlessY) v = Mathf.Repeat(v, 1f);
            var uv = new float2(u, v);
            var cells = new int2(r.cellsX, r.cellsY);
            switch (r.generator)
            {
                case "white-noise": return ValueNoiseGenerator.Lattice(r, Mathf.FloorToInt(u * cells.x), Mathf.FloorToInt(v * cells.y));
                case "worley": return Worley(uv * cells);
                case "perlin": return 0.5f + 0.5f * Wave(NoiseBasis.Perlin, uv, cells, 0);
                case "simplex": return 0.5f + 0.5f * Wave(NoiseBasis.Simplex, uv, cells, 0);
                case "directional-noise":
                    if (r.alongU) cells.y *= r.stretch;
                    else cells.x *= r.stretch;
                    return 0.5f + 0.5f * Wave(NoiseBasis.Perlin, uv, cells, 0);
                case "turbulence":
                    uv += r.warp * new float2(Wave(r.basis, uv, cells, 3), Wave(r.basis, uv, cells, 7));
                    break;
            }

            float sum = 0f, weight = 0f, amplitude = 1f;
            for (int i = 0; i < r.octaves; i++)
            {
                float n = Wave(r.basis, uv, cells, i);
                switch (r.generator)
                {
                    case "billow": n = Mathf.Abs(n); break;
                    case "ridged": n = 1f - Mathf.Abs(n); n *= n; break;
                    case "turbulence": n = Mathf.Abs(n); break;
                    default: n = 0.5f + 0.5f * n; break;
                }
                sum += n * amplitude;
                weight += amplitude;
                amplitude *= r.persistence;
                cells *= r.lacunarity;
            }
            return sum / weight;
        }

        float Wave(NoiseBasis basis, float2 uv, int2 cells, int octave)
        {
            float2 p = uv * cells + translation + octave * 13.375f;
            if (basis == NoiseBasis.Value)
            {
                int2 cell = (int2)math.floor(p);
                float2 t = math.frac(p);
                t = t * t * (3f - 2f * t);
                float a = GridValue(cell.x, cell.y, cells, octave);
                float b = GridValue(cell.x + 1, cell.y, cells, octave);
                float c = GridValue(cell.x, cell.y + 1, cells, octave);
                float d = GridValue(cell.x + 1, cell.y + 1, cells, octave);
                return 2f * math.lerp(math.lerp(a, b, t.x), math.lerp(c, d, t.x), t.y) - 1f;
            }

            // psrnoise requires an even vertical period.
            if (basis == NoiseBasis.Simplex && r.seamlessY && (cells.y & 1) != 0)
            {
                p.y *= 2f;
                cells.y *= 2;
            }
            float2 period = new float2(r.seamlessX ? cells.x : 1048576, r.seamlessY ? cells.y : 1048576);
            if (r.seamlessX) p.x = math.frac(p.x / period.x) * period.x;
            if (r.seamlessY) p.y = math.frac(p.y / period.y) * period.y;
            if (basis == NoiseBasis.Simplex)
                return r.seamlessX || r.seamlessY ? noise.psrnoise(p, period, rotation) : noise.snoise(p);
            return r.seamlessX || r.seamlessY ? noise.pnoise(p, period) : noise.cnoise(p);
        }

        static int Wrap(int n, int period) => (n % period + period) % period;

        float GridValue(int x, int y, int2 cells, int octave)
        {
            if (r.seamlessX) x = Wrap(x, cells.x);
            if (r.seamlessY) y = Wrap(y, cells.y);
            return ValueNoiseGenerator.Hash(unchecked(r.seed + octave), x, y);
        }

        internal static float CellHash(TextureRecipe r, int x, int y, int salt)
        {
            if (r.seamlessX) x = Wrap(x, r.cellsX);
            if (r.seamlessY) y = Wrap(y, r.cellsY);
            return ValueNoiseGenerator.Hash(r.seed ^ salt, x, y);
        }

        internal static Vector2 FeatureOffset(TextureRecipe r, int x, int y) =>
            new Vector2(CellHash(r, x, y, 0) - 0.5f, CellHash(r, x, y, 0x519aa2) - 0.5f) * r.jitter;

        float Worley(float2 point)
        {
            int2 cell = (int2)math.floor(point);
            float nearest = 1f;
            for (int y = cell.y - 1; y <= cell.y + 1; y++)
                for (int x = cell.x - 1; x <= cell.x + 1; x++)
                {
                    Vector2 shift = FeatureOffset(r, x, y);
                    float2 delta = point - new float2(x + 0.5f + shift.x, y + 0.5f + shift.y);
                    nearest = math.min(nearest, math.lengthsq(delta));
                }
            return math.sqrt(nearest);
        }
    }
}
