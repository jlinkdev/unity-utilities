using UnityEngine;

namespace jlinkdev.UnityUtilities.WaterSystem
{
    /// <summary>Applies buoyancy at local sample points without changing Rigidbody settings.</summary>
    [AddComponentMenu("JLink/Water System/Water Buoyancy")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class WaterBuoyancy : MonoBehaviour
    {
        [SerializeField] private WaterBody bodyOverride;
        [SerializeField] private Vector3[] floatPoints =
        {
            new Vector3(-0.4f, 0f, -0.4f), new Vector3(0.4f, 0f, -0.4f),
            new Vector3(-0.4f, 0f, 0.4f), new Vector3(0.4f, 0f, 0.4f)
        };
        [SerializeField, Min(0f), Tooltip("Maximum lift as a multiple of gravity, independent of mass.")]
        private float buoyancy = 1.5f;
        [SerializeField, Min(0.001f), Tooltip("Depth in metres at which a float point receives full lift.")]
        private float fullSubmersionDepth = 0.5f;
        [SerializeField, Min(0f), Tooltip("Velocity damping relative to the water, in inverse seconds.")]
        private float linearDamping = 2f;
        [SerializeField, Min(0f), Tooltip("Angular velocity damping while immersed, in inverse seconds.")]
        private float angularDamping = 0.5f;

        private Rigidbody target;

        public WaterBody BodyOverride { get => bodyOverride; set => bodyOverride = value; }
        public Vector3[] FloatPoints { get => floatPoints; set => floatPoints = value; }
        public float Buoyancy { get => buoyancy; set => buoyancy = Positive(value, 1.5f); }
        public float FullSubmersionDepth { get => fullSubmersionDepth; set => fullSubmersionDepth = Mathf.Max(0.001f, Positive(value, 0.5f)); }
        public float LinearDamping { get => linearDamping; set => linearDamping = Positive(value, 2f); }
        public float AngularDamping { get => angularDamping; set => angularDamping = Positive(value, 0.5f); }
        public int ActivePointCount { get; private set; }
        public float MeanSubmersion { get; private set; }

        private void Awake() => target = GetComponent<Rigidbody>();

        private void FixedUpdate()
        {
            ActivePointCount = 0;
            MeanSubmersion = 0f;
            if (target == null) target = GetComponent<Rigidbody>();
            if (target == null || target.isKinematic || floatPoints == null || floatPoints.Length == 0) return;

            // Divide by the total configured point count: a half-submerged hull gets half its lift.
            float weight = 1f / floatPoints.Length;
            float step = Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            float damping = Mathf.Min(Positive(linearDamping, 2f), 1f / step);
            Vector3 lift = -Physics.gravity * Positive(buoyancy, 1.5f);
            for (int i = 0; i < floatPoints.Length; i++)
            {
                Vector3 point = transform.TransformPoint(floatPoints[i]);
                if (!WaterInteraction.IsFinite(point)) continue;
                WaterBody body = WaterInteraction.FindBody(point, bodyOverride);
                if (body == null || !body.Contains(point, body.SurfaceTime)) continue;

                WaterSample sample = body.Sample(point, body.SurfaceTime);
                float fraction = Mathf.Clamp01((sample.Height - point.y) / Mathf.Max(0.001f, Positive(fullSubmersionDepth, 0.5f)));
                if (fraction <= 0f) continue;
                Vector3 relativeVelocity = target.GetPointVelocity(point) - sample.Velocity;
                Vector3 acceleration = (lift - relativeVelocity * damping) * (fraction * weight);
                if (!WaterInteraction.IsFinite(acceleration)) continue;
                // Force/mass gives equal translational acceleration across different Rigidbody masses,
                // while ForceMode.Force preserves Unity's inertia tensor for off-centre torque.
                target.AddForceAtPosition(acceleration * target.mass, point, ForceMode.Force);
                ActivePointCount++;
                MeanSubmersion += fraction * weight;
            }

            if (MeanSubmersion > 0f)
            {
                float angularRate = Mathf.Min(Positive(angularDamping, 0.5f), 1f / step);
                target.AddTorque(-target.angularVelocity * (angularRate * MeanSubmersion), ForceMode.Acceleration);
            }
        }

        private void OnDisable()
        {
            ActivePointCount = 0;
            MeanSubmersion = 0f;
        }

        private void OnValidate()
        {
            Buoyancy = buoyancy;
            FullSubmersionDepth = fullSubmersionDepth;
            LinearDamping = linearDamping;
            AngularDamping = angularDamping;
        }

        private static float Positive(float value, float fallback) => WaterInteraction.IsFinite(value) ? Mathf.Max(0f, value) : fallback;

        private void OnDrawGizmosSelected()
        {
            if (floatPoints == null) return;
            Gizmos.color = new Color(0.15f, 0.8f, 1f, 0.9f);
            foreach (Vector3 point in floatPoints)
                if (WaterInteraction.IsFinite(point)) Gizmos.DrawWireSphere(transform.TransformPoint(point), 0.075f);
        }
    }
}
