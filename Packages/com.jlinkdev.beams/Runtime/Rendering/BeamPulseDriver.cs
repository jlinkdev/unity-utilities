using System;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Drives the standard single-pulse shader property and reports completion without defining its meaning.</summary>
    [AddComponentMenu("jlinkdev/Beams/Rendering/Beam Pulse Driver")]
    public sealed class BeamPulseDriver : MonoBehaviour
    {
        [SerializeField] private BeamRibbonRenderer targetRenderer;
        [SerializeField, Min(0.001f)] private float defaultDuration = 0.5f;
        [SerializeField] private AnimationCurve motion = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private bool useUnscaledTime;

        private float startedAt;
        private float duration;
        private bool reverse;

        public event Action<BeamPulseDriver> PulseCompleted;
        public bool IsPulsing { get; private set; }

        public void Trigger()
        {
            Trigger(defaultDuration, false);
        }

        public void TriggerReverse()
        {
            Trigger(defaultDuration, true);
        }

        public void Trigger(float pulseDuration, bool travelInReverse)
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<BeamRibbonRenderer>();
            duration = Mathf.Max(0.001f, pulseDuration);
            reverse = travelInReverse;
            startedAt = CurrentTime();
            IsPulsing = true;
            Apply(0f);
        }

        public void Stop()
        {
            IsPulsing = false;
            if (targetRenderer != null)
                targetRenderer.PulsePosition = -1f;
        }

        private void Update()
        {
            if (!IsPulsing)
                return;
            float normalized = Mathf.Clamp01((CurrentTime() - startedAt) / duration);
            Apply(normalized);
            if (normalized < 1f)
                return;
            IsPulsing = false;
            PulseCompleted?.Invoke(this);
        }

        private void Apply(float normalized)
        {
            if (targetRenderer == null)
                return;
            float position = motion != null ? motion.Evaluate(normalized) : normalized;
            targetRenderer.PulsePosition = reverse ? 1f - position : position;
        }

        private float CurrentTime()
        {
            return useUnscaledTime ? Time.unscaledTime : Time.time;
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnValidate()
        {
            defaultDuration = Mathf.Max(0.001f, defaultDuration);
            if (motion == null || motion.length == 0)
                motion = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
    }
}
