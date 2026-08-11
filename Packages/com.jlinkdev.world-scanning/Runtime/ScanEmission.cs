using UnityEngine;

namespace jlinkdev.UnityUtilities.WorldScanning
{
    public readonly struct ScanEmission
    {
        public ScanEmission(
            Vector3 origin,
            ScanProfile profile,
            Vector3 axis,
            float rangeMultiplier = 1f,
            float durationMultiplier = 1f,
            float intensityMultiplier = 1f,
            ScanShape? shapeOverride = null)
        {
            Origin = origin;
            Profile = profile;
            Axis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;
            RangeMultiplier = Mathf.Max(0.001f, rangeMultiplier);
            DurationMultiplier = Mathf.Max(0.001f, durationMultiplier);
            IntensityMultiplier = Mathf.Max(0f, intensityMultiplier);
            Shape = shapeOverride ?? (profile != null ? profile.Shape : ScanShape.Sphere);
        }

        public Vector3 Origin { get; }
        public ScanProfile Profile { get; }
        public Vector3 Axis { get; }
        public float RangeMultiplier { get; }
        public float DurationMultiplier { get; }
        public float IntensityMultiplier { get; }
        public ScanShape Shape { get; }
    }
}
