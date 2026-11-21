using System;
using UnityEngine;
using UnityEngine.Events;

namespace jlinkdev.UnityUtilities.WaterSystem
{
    /// <summary>A snapshot of a probe crossing, suitable for splash, audio and gameplay hooks.</summary>
    [Serializable]
    public struct WaterContact
    {
        public WaterBody Body;
        public Vector3 Position;
        public Vector3 PointVelocity;
        public WaterSample Surface;
        public float SignedDepth;
        public bool IsUnderwater;
    }

    [Serializable]
    public sealed class WaterContactEvent : UnityEvent<WaterContact> { }

    /// <summary>Raises a single event on entering or leaving a finite, animated water volume.</summary>
    [AddComponentMenu("JLink/Water System/Water Contact Probe")]
    [DisallowMultipleComponent]
    public sealed class WaterContactProbe : MonoBehaviour
    {
        [SerializeField] private WaterBody bodyOverride;
        [SerializeField, Min(0f), Tooltip("Surface crossing dead band, in metres. Prevents wave-edge event chatter.")]
        private float hysteresis = 0.025f;
        [SerializeField] private WaterContactEvent enteredWater = new WaterContactEvent();
        [SerializeField] private WaterContactEvent exitedWater = new WaterContactEvent();
        private Vector3 previousPosition;
        private bool hasPreviousPosition;

        public WaterBody BodyOverride { get => bodyOverride; set => bodyOverride = value; }
        public float Hysteresis { get => hysteresis; set => hysteresis = WaterInteraction.IsFinite(value) ? Mathf.Max(0f, value) : 0.025f; }
        public WaterContactEvent EnteredWater => enteredWater;
        public WaterContactEvent ExitedWater => exitedWater;
        public WaterContact Contact { get; private set; }
        public bool IsUnderwater => Contact.IsUnderwater;

        private void OnEnable()
        {
            hasPreviousPosition = false;
            Contact = default;
        }

        private void Update() => Refresh(Time.deltaTime);

        /// <summary>Refreshes immediately; deltaTime is used only to measure probe movement.</summary>
        public void Refresh(float deltaTime = 0f)
        {
            Vector3 position = transform.position;
            Vector3 velocity = hasPreviousPosition && WaterInteraction.IsFinite(deltaTime) && deltaTime > 0f
                ? (position - previousPosition) / deltaTime : Vector3.zero;
            if (!WaterInteraction.IsFinite(velocity)) velocity = Vector3.zero;
            previousPosition = position;
            hasPreviousPosition = true;
            WaterContact previous = Contact;
            WaterBody body = WaterInteraction.FindBody(position, bodyOverride);
            WaterContact next = new WaterContact { Body = body, Position = position, PointVelocity = velocity };
            if (body != null)
            {
                next.Surface = body.Sample(position, body.SurfaceTime);
                next.SignedDepth = next.Surface.Height - position.y;
                float threshold = previous.IsUnderwater && previous.Body == body ? -Hysteresis : Hysteresis;
                next.IsUnderwater = position.y >= body.transform.position.y - body.Depth && next.SignedDepth > threshold;
            }

            Contact = next;
            // Switching pools emits an exit and an entry, so subscribers can release pool-specific effects.
            if (previous.IsUnderwater && (!next.IsUnderwater || previous.Body != next.Body))
            {
                WaterContact exit = previous;
                exit.Position = position;
                exit.PointVelocity = velocity;
                exit.IsUnderwater = false;
                if (previous.Body == next.Body) { exit.Surface = next.Surface; exit.SignedDepth = next.SignedDepth; }
                exitedWater.Invoke(exit);
            }
            if (next.IsUnderwater && (!previous.IsUnderwater || previous.Body != next.Body)) enteredWater.Invoke(next);
        }

        private void OnDisable()
        {
            if (!Contact.IsUnderwater) return;
            WaterContact exit = Contact;
            exit.IsUnderwater = false;
            Contact = exit;
            exitedWater.Invoke(exit);
        }

        private void OnValidate() => Hysteresis = hysteresis;
    }
}
