using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    public enum BeamEndpointOrientation
    {
        Preserve = 0,
        EndpointForward = 1,
        AlongBeam = 2,
        SurfaceNormal = 3
    }

    /// <summary>Positions optional source and target visual objects from a beam's resolved endpoints.</summary>
    [ExecuteAlways]
    [AddComponentMenu("jlinkdev/Beams/Endpoints/Beam Endpoint Visuals")]
    public sealed class BeamEndpointVisuals : MonoBehaviour
    {
        [SerializeField] private Beam beam;
        [SerializeField] private Transform sourceVisual;
        [SerializeField] private Transform targetVisual;
        [SerializeField] private BeamEndpointOrientation sourceOrientation = BeamEndpointOrientation.AlongBeam;
        [SerializeField] private BeamEndpointOrientation targetOrientation = BeamEndpointOrientation.SurfaceNormal;
        [SerializeField] private bool synchronizeActiveState = true;

        private void OnEnable()
        {
            if (beam == null)
                beam = GetComponent<Beam>();
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Refresh()
        {
            bool active = beam != null && beam.HasResolvedPath;
            if (synchronizeActiveState)
            {
                SetActive(sourceVisual, active);
                SetActive(targetVisual, active);
            }

            if (!active)
                return;

            Vector3 alongBeam = beam.CurrentTarget.Position - beam.CurrentSource.Position;
            Apply(sourceVisual, beam.CurrentSource, alongBeam, sourceOrientation);
            Apply(targetVisual, beam.CurrentTarget, -alongBeam, targetOrientation);
        }

        private void Subscribe()
        {
            if (beam == null)
                return;
            beam.PathUpdated += OnBeamChanged;
            beam.RenderingStopped += OnBeamChanged;
        }

        private void Unsubscribe()
        {
            if (beam == null)
                return;
            beam.PathUpdated -= OnBeamChanged;
            beam.RenderingStopped -= OnBeamChanged;
        }

        private void OnBeamChanged(Beam changedBeam)
        {
            Refresh();
        }

        private static void Apply(Transform visual, in BeamEndpoint endpoint, Vector3 alongBeam, BeamEndpointOrientation orientation)
        {
            if (visual == null)
                return;
            visual.position = endpoint.Position;

            Vector3 forward = Vector3.zero;
            switch (orientation)
            {
                case BeamEndpointOrientation.EndpointForward:
                    forward = endpoint.Forward;
                    break;
                case BeamEndpointOrientation.AlongBeam:
                    forward = alongBeam;
                    break;
                case BeamEndpointOrientation.SurfaceNormal:
                    forward = endpoint.HasSurface ? endpoint.SurfaceNormal : endpoint.Forward;
                    break;
            }

            if (orientation != BeamEndpointOrientation.Preserve && forward.sqrMagnitude > 0.000001f)
                visual.rotation = Quaternion.LookRotation(forward.normalized, StableUp(forward));
        }

        private static Vector3 StableUp(Vector3 forward)
        {
            return Mathf.Abs(Vector3.Dot(forward.normalized, Vector3.up)) > 0.98f ? Vector3.right : Vector3.up;
        }

        private static void SetActive(Transform visual, bool active)
        {
            if (visual != null && visual.gameObject.activeSelf != active)
                visual.gameObject.SetActive(active);
        }
    }
}
