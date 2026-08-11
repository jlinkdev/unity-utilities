using System;
using UnityEngine;
using UnityEngine.Events;

namespace jlinkdev.UnityUtilities.WorldScanning
{
    [DisallowMultipleComponent]
    [AddComponentMenu("jlinkdev/World Scanning/Scan Emitter")]
    public sealed class ScanEmitter : MonoBehaviour
    {
        private enum ShapeOverride
        {
            UseProfile = 0,
            Sphere = 1,
            Cylinder = 2
        }

        [SerializeField] private ScanProfile profile;
        [SerializeField] private Transform origin;
        [SerializeField] private Transform cylinderAxis;
        [SerializeField] private ShapeOverride shapeOverride;
        [SerializeField, Min(0.001f)] private float rangeMultiplier = 1f;
        [SerializeField, Min(0.001f)] private float durationMultiplier = 1f;
        [SerializeField, Min(0f)] private float intensityMultiplier = 1f;
        [SerializeField] private bool playOnEnable;
        [SerializeField] private UnityEvent onScanStarted = new UnityEvent();
        [SerializeField] private UnityEvent onScanCompleted = new UnityEvent();
        [SerializeField] private UnityEvent onScanCancelled = new UnityEvent();

        public event Action<ScanHandle> ScanStarted;
        public event Action<ScanEndedEvent> ScanEnded;

        public ScanProfile Profile
        {
            get => profile;
            set => profile = value;
        }

        public ScanHandle LastHandle { get; private set; }
        public Vector3 Origin => origin != null ? origin.position : transform.position;
        public Vector3 Axis => cylinderAxis != null ? cylinderAxis.up : transform.up;

        private void OnEnable()
        {
            ScanSystem.ScanEnded += OnSystemScanEnded;
            if (playOnEnable && Application.isPlaying)
                Emit();
        }

        private void OnDisable()
        {
            ScanSystem.ScanEnded -= OnSystemScanEnded;
        }

        public ScanHandle Emit()
        {
            ScanShape? requestedShape = shapeOverride switch
            {
                ShapeOverride.Sphere => ScanShape.Sphere,
                ShapeOverride.Cylinder => ScanShape.Cylinder,
                _ => null
            };
            LastHandle = ScanSystem.Emit(new ScanEmission(
                Origin,
                profile,
                Axis,
                rangeMultiplier,
                durationMultiplier,
                intensityMultiplier,
                requestedShape));
            if (LastHandle != ScanHandle.Invalid)
            {
                onScanStarted.Invoke();
                ScanStarted?.Invoke(LastHandle);
            }
            return LastHandle;
        }

        public bool CancelLast()
        {
            return LastHandle.Cancel();
        }

        private void OnSystemScanEnded(ScanEndedEvent endedEvent)
        {
            if (endedEvent.Handle != LastHandle)
                return;
            if (endedEvent.Reason == ScanCompletionReason.Completed)
                onScanCompleted.Invoke();
            else
                onScanCancelled.Invoke();
            ScanEnded?.Invoke(endedEvent);
        }

        private void OnValidate()
        {
            rangeMultiplier = Mathf.Max(0.001f, rangeMultiplier);
            durationMultiplier = Mathf.Max(0.001f, durationMultiplier);
            intensityMultiplier = Mathf.Max(0f, intensityMultiplier);
        }
    }
}
