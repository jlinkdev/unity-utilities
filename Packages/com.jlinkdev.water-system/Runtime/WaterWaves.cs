using UnityEngine;

namespace jlinkdev.UnityUtilities.WaterSystem
{
    public readonly struct WaterSample
    {
        public Vector3 Position { get; }
        public Vector3 Normal { get; }
        public Vector3 Velocity { get; }
        public float Height => Position.y;
        public WaterSample(Vector3 position, Vector3 normal, Vector3 velocity)
        { Position = position; Normal = normal; Velocity = velocity; }
    }

    /// <summary>CPU counterpart of WaterCommon.hlsl. Geometry is a vertical height field, without horizontal displacement.</summary>
    public static class WaterWaves
    {
        public static float Finite(float value, float fallback = 0f) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;

        public static void Pack(WaveSettings wave, out Vector4 shape, out Vector4 motion)
        {
            float angle = (Finite(wave.Direction) % 360f) * Mathf.Deg2Rad;
            float k = 2f * Mathf.PI / Mathf.Clamp(Finite(wave.Wavelength, 1f), 0.1f, 100000f);
            shape = new Vector4(Mathf.Cos(angle), Mathf.Sin(angle), Mathf.Clamp(Finite(wave.Amplitude), 0f, 1000f), k);
            motion = new Vector4(-k * Mathf.Clamp(Finite(wave.Speed), -1000f, 1000f),
                Finite(wave.Phase) % (2f * Mathf.PI), 0f, 0f);
        }

        /// <summary>Analytic wave sample. Use WaterBody.Sample for the exact tessellated surface.</summary>
        public static WaterSample Evaluate(WaterProfile profile, Vector3 origin, Vector3 query, float time)
        {
            float height = origin.y, dx = 0f, dz = 0f, velocity = 0f;
            var waves = profile != null ? profile.Waves : null;
            int count = waves == null ? 0 : Mathf.Min(waves.Length, WaterProfile.MaximumWaves);
            time = Finite(time);
            for (int i = 0; i < count; i++)
            {
                Pack(waves[i], out var w, out var m);
                float phase = ((query.x - origin.x) * w.x + (query.z - origin.z) * w.y) * w.w + time * m.x + m.y;
                float cosine = Mathf.Cos(phase) * w.z;
                height += Mathf.Sin(phase) * w.z;
                dx += cosine * w.w * w.x;
                dz += cosine * w.w * w.y;
                velocity += cosine * m.x;
            }
            return new WaterSample(new Vector3(query.x, height, query.z), new Vector3(-dx, 1f, -dz).normalized,
                new Vector3(0f, velocity, 0f));
        }

        /// <summary>Piecewise linear sample using the same b-c cell diagonal as the rendered grid.</summary>
        public static WaterSample EvaluateGrid(WaterProfile profile, Vector3 origin, Vector2 size, int resolution,
            Vector3 query, float time)
        {
            resolution = Mathf.Clamp(resolution, 1, 256);
            size.x = Mathf.Max(0.1f, Finite(size.x, 1f)); size.y = Mathf.Max(0.1f, Finite(size.y, 1f));
            float gx = Mathf.Clamp01((query.x - origin.x) / size.x + 0.5f) * resolution;
            float gz = Mathf.Clamp01((query.z - origin.z) / size.y + 0.5f) * resolution;
            int ix = Mathf.Min(Mathf.FloorToInt(gx), resolution - 1);
            int iz = Mathf.Min(Mathf.FloorToInt(gz), resolution - 1);
            float u = gx - ix, v = gz - iz;
            float sx = size.x / resolution, sz = size.y / resolution;
            Vector3 corner = origin + new Vector3(ix * sx - size.x * 0.5f, 0f, iz * sz - size.y * 0.5f);
            WaterSample b = Evaluate(profile, origin, corner + new Vector3(sx, 0f, 0f), time);
            WaterSample c = Evaluate(profile, origin, corner + new Vector3(0f, 0f, sz), time);
            WaterSample edge; float height, vy, dx, dz;
            if (u + v <= 1f)
            {
                edge = Evaluate(profile, origin, corner, time);
                height = edge.Height * (1f - u - v) + b.Height * u + c.Height * v;
                vy = edge.Velocity.y * (1f - u - v) + b.Velocity.y * u + c.Velocity.y * v;
                dx = (b.Height - edge.Height) / sx; dz = (c.Height - edge.Height) / sz;
            }
            else
            {
                edge = Evaluate(profile, origin, corner + new Vector3(sx, 0f, sz), time);
                height = edge.Height * (u + v - 1f) + b.Height * (1f - v) + c.Height * (1f - u);
                vy = edge.Velocity.y * (u + v - 1f) + b.Velocity.y * (1f - v) + c.Velocity.y * (1f - u);
                dx = (edge.Height - c.Height) / sx; dz = (edge.Height - b.Height) / sz;
            }
            return new WaterSample(new Vector3(query.x, height, query.z), new Vector3(-dx, 1f, -dz).normalized,
                new Vector3(0f, vy, 0f));
        }
    }
}
