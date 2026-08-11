using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Decorates another endpoint provider with frame-rate-independent positional and directional smoothing.</summary>
    [AddComponentMenu("jlinkdev/Beams/Endpoints/Smoothed Beam Endpoint")]
    public sealed class SmoothedBeamEndpoint : BeamEndpointProvider
    {
        [SerializeField] private BeamEndpointProvider input;
        [SerializeField, Min(0f)] private float positionHalfLife = 0.05f;
        [SerializeField, Min(0f)] private float directionHalfLife = 0.05f;

        private bool initialized;
        private Vector3 position;
        private Vector3 forward;

        public BeamEndpointProvider Input
        {
            get => input;
            set
            {
                input = value;
                initialized = false;
            }
        }

        public void Snap()
        {
            initialized = false;
            if (input != null && input != this && input.TryGetEndpoint(out BeamEndpoint endpoint))
                Initialize(endpoint);
        }

        public override bool TryGetEndpoint(out BeamEndpoint endpoint)
        {
            if (input == null || input == this || !input.TryGetEndpoint(out BeamEndpoint raw))
            {
                endpoint = default;
                initialized = false;
                return false;
            }

            if (!initialized || !Application.isPlaying)
            {
                Initialize(raw);
            }
            else
            {
                float positionBlend = HalfLifeBlend(positionHalfLife, Time.deltaTime);
                float directionBlend = HalfLifeBlend(directionHalfLife, Time.deltaTime);
                position = Vector3.LerpUnclamped(position, raw.Position, positionBlend);
                forward = Vector3.SlerpUnclamped(forward, raw.Forward, directionBlend);
            }

            endpoint = new BeamEndpoint(position, forward, raw.SurfaceNormal, raw.SurfaceCollider, raw.Anchor);
            return true;
        }

        private void Initialize(in BeamEndpoint endpoint)
        {
            position = endpoint.Position;
            forward = endpoint.Forward;
            initialized = true;
        }

        private static float HalfLifeBlend(float halfLife, float deltaTime)
        {
            return halfLife <= 0f ? 1f : 1f - Mathf.Pow(0.5f, deltaTime / halfLife);
        }

        private void OnDisable()
        {
            initialized = false;
        }

        private void OnValidate()
        {
            positionHalfLife = Mathf.Max(0f, positionHalfLife);
            directionHalfLife = Mathf.Max(0f, directionHalfLife);
        }
    }
}
