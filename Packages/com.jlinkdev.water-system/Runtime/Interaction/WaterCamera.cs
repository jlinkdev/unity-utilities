using System;
using UnityEngine;
using UnityEngine.Events;

namespace jlinkdev.UnityUtilities.WaterSystem
{
    public enum WaterCameraState { Above, Intersecting, Submerged }
    public enum WaterDebugMode { None = 0, UnderwaterMask = 1, SurfaceDepth = 2, WaterPathLength = 3, Caustics = 4 }

    /// <summary>Camera-centre crossing hooks and coarse near-plane diagnostics. Rendering evaluates each pixel independently.</summary>
    [AddComponentMenu("JLink/Water System/Water Camera")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class WaterCamera : MonoBehaviour
    {
        [SerializeField] private WaterBody bodyOverride;
        [SerializeField] private WaterDebugMode debugMode;
        [SerializeField] private UnityEvent enteredWater = new UnityEvent();
        [SerializeField] private UnityEvent exitedWater = new UnityEvent();
        private Camera target;
        private readonly Vector3[] corners = new Vector3[4];

        public WaterBody BodyOverride { get => bodyOverride; set => bodyOverride = value; }
        public WaterDebugMode DebugMode { get => debugMode; set => debugMode = value; }
        public WaterBody ActiveBody { get; private set; }
        public WaterCameraState State { get; private set; }
        public float Submersion01 { get; private set; }
        public bool IsUnderwater { get; private set; }
        public WaterSample SurfaceSample { get; private set; }
        public UnityEvent EnteredWater => enteredWater;
        public UnityEvent ExitedWater => exitedWater;
        public event Action<WaterCameraState> StateChanged;

        private void OnEnable()
        {
            target = GetComponent<Camera>();
            RefreshState();
        }

        private void LateUpdate() => RefreshState();

        public void RefreshState()
        {
            if (target == null) target = GetComponent<Camera>();
            WaterCameraState previousState = State;
            bool previousUnderwater = IsUnderwater;
            ActiveBody = WaterInteraction.FindBody(transform.position, bodyOverride);
            IsUnderwater = ActiveBody != null && ActiveBody.Contains(transform.position, ActiveBody.SurfaceTime);
            SurfaceSample = ActiveBody != null ? ActiveBody.Sample(transform.position, ActiveBody.SurfaceTime) : default;

            int submergedPoints = IsUnderwater ? 1 : 0;
            if (target != null)
            {
                target.CalculateFrustumCorners(new Rect(0f, 0f, 1f, 1f), target.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, corners);
                for (int i = 0; i < corners.Length; i++)
                {
                    Vector3 point = transform.position + transform.rotation * corners[i];
                    WaterBody body = WaterInteraction.FindBody(point, bodyOverride);
                    if (body == null || !body.Contains(point, body.SurfaceTime)) continue;
                    submergedPoints++;
                    if (ActiveBody == null) { ActiveBody = body; SurfaceSample = body.Sample(point, body.SurfaceTime); }
                }
            }
            Submersion01 = submergedPoints / 5f;
            State = submergedPoints == 0 ? WaterCameraState.Above
                : submergedPoints == 5 ? WaterCameraState.Submerged : WaterCameraState.Intersecting;
            if (previousUnderwater != IsUnderwater)
            {
                if (IsUnderwater) enteredWater.Invoke(); else exitedWater.Invoke();
            }
            if (previousState != State) StateChanged?.Invoke(State);
        }

        private void OnDisable()
        {
            bool wasUnderwater = IsUnderwater;
            WaterCameraState previousState = State;
            IsUnderwater = false;
            ActiveBody = null;
            State = WaterCameraState.Above;
            Submersion01 = 0f;
            SurfaceSample = default;
            if (wasUnderwater) exitedWater.Invoke();
            if (previousState != State) StateChanged?.Invoke(State);
        }
    }
}
