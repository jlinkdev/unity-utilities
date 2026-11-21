using UnityEngine;

namespace jlinkdev.UnityUtilities.WaterSystem
{
    internal static class WaterInteraction
    {
        internal static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        // Prefer a body that actually contains this point. Stacked finite volumes must not be
        // hidden by an unrelated pool whose surface is higher but whose bottom is above us.
        internal static WaterBody FindBody(Vector3 point, WaterBody bodyOverride)
        {
            if (!IsFinite(point)) return null;
            if (bodyOverride != null)
                return bodyOverride.isActiveAndEnabled && bodyOverride.ContainsXZ(point) ? bodyOverride : null;

            WaterBody containing = null;
            WaterBody nearest = null;
            float containingHeight = float.NegativeInfinity;
            float nearestDistance = float.PositiveInfinity;
            var bodies = WaterBody.ActiveBodies;
            for (int i = 0; i < bodies.Count; i++)
            {
                WaterBody body = bodies[i];
                if (body == null || !body.isActiveAndEnabled || !body.ContainsXZ(point)) continue;
                WaterSample sample = body.Sample(point, body.SurfaceTime);
                if (!IsFinite(sample.Height)) continue;
                if (point.y >= body.transform.position.y - body.Depth && point.y <= sample.Height && sample.Height > containingHeight)
                {
                    containing = body;
                    containingHeight = sample.Height;
                }
                float distance = Mathf.Abs(sample.Height - point.y);
                if (distance < nearestDistance)
                {
                    nearest = body;
                    nearestDistance = distance;
                }
            }
            return containing != null ? containing : nearest;
        }
    }
}
