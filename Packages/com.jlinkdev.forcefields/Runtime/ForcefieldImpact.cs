using UnityEngine;

namespace jlinkdev.UnityUtilities.Forcefields
{
    /// <summary>Describes a visual disturbance submitted to a <see cref="Forcefield"/>.</summary>
    public readonly struct ForcefieldImpact
    {
        public ForcefieldImpact(Vector3 position, Vector3 normal, float strength, float radius)
        {
            Position = position;
            Normal = normal.sqrMagnitude > 0.000001f ? normal.normalized : Vector3.up;
            Strength = Mathf.Max(0f, strength);
            Radius = Mathf.Max(0f, radius);
        }

        /// <summary>World-space impact position.</summary>
        public Vector3 Position { get; }

        /// <summary>World-space surface normal.</summary>
        public Vector3 Normal { get; }

        /// <summary>Normalized visual strength. Values above one are supported.</summary>
        public float Strength { get; }

        /// <summary>World-space starting radius of the expanding ring.</summary>
        public float Radius { get; }
    }
}
