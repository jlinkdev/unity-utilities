using System;
using System.Collections.Generic;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Queries strand segments and reports contact lifecycle and tick events without gameplay semantics.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("jlinkdev/Beams/Contacts/Beam Physics Contacts")]
    public sealed class BeamPhysicsContacts : MonoBehaviour
    {
        [SerializeField] private Beam beam;
        [SerializeField, Min(0f)] private float queryInterval;
        [SerializeField, Min(0f)] private float tickInterval = 0.1f;
        [SerializeField, Min(0f)] private float radius;
        [SerializeField] private LayerMask layerMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.UseGlobal;
        [SerializeField, Range(1, 64)] private int maximumHitsPerSegment = 8;

        [SerializeField] private BeamContactUnityEvent onContactEntered = new BeamContactUnityEvent();
        [SerializeField] private BeamContactUnityEvent onContactStayed = new BeamContactUnityEvent();
        [SerializeField] private BeamContactUnityEvent onContactExited = new BeamContactUnityEvent();
        [SerializeField] private BeamContactUnityEvent onContactTicked = new BeamContactUnityEvent();

        private Dictionary<ContactKey, BeamContact> activeContacts = new Dictionary<ContactKey, BeamContact>();
        private Dictionary<ContactKey, BeamContact> frameContacts = new Dictionary<ContactKey, BeamContact>();
        private readonly List<ContactKey> exitKeys = new List<ContactKey>();
        private RaycastHit[] hitBuffer;
        private float nextQueryTime;
        private float nextTickTime;

        public event Action<BeamPhysicsContacts, BeamContact> ContactEntered;
        public event Action<BeamPhysicsContacts, BeamContact> ContactStayed;
        public event Action<BeamPhysicsContacts, BeamContact> ContactExited;
        public event Action<BeamPhysicsContacts, BeamContact> ContactTicked;

        public int ContactCount => activeContacts.Count;
        public BeamContactUnityEvent OnContactEntered => onContactEntered;
        public BeamContactUnityEvent OnContactStayed => onContactStayed;
        public BeamContactUnityEvent OnContactExited => onContactExited;
        public BeamContactUnityEvent OnContactTicked => onContactTicked;

        public Beam Beam
        {
            get => beam;
            set
            {
                if (beam == value)
                    return;
                Unsubscribe();
                ClearContacts(true);
                beam = value;
                Subscribe();
            }
        }

        public float Radius
        {
            get => radius;
            set => radius = Mathf.Max(0f, value);
        }

        public float TickInterval
        {
            get => tickInterval;
            set => tickInterval = Mathf.Max(0f, value);
        }

        public LayerMask LayerMask
        {
            get => layerMask;
            set => layerMask = value;
        }

        public void GetContacts(List<BeamContact> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));
            results.Clear();
            foreach (KeyValuePair<ContactKey, BeamContact> pair in activeContacts)
                results.Add(pair.Value);
        }

        public void QueryNow()
        {
            if (beam == null || !beam.HasResolvedPath)
            {
                ClearContacts(true);
                return;
            }

            EnsureHitBuffer();
            frameContacts.Clear();
            BeamPathBuffer paths = beam.Paths;
            for (int strandIndex = 0; strandIndex < paths.Count; strandIndex++)
            {
                BeamStrand strand = paths[strandIndex];
                for (int segmentIndex = 0; segmentIndex < strand.Count - 1; segmentIndex++)
                    QuerySegment(strand, strandIndex, segmentIndex);
            }

            PublishLifecycle();
            float time = CurrentTime();
            if (tickInterval <= 0f || time >= nextTickTime)
            {
                foreach (KeyValuePair<ContactKey, BeamContact> pair in activeContacts)
                    PublishTick(pair.Value);
                nextTickTime = tickInterval <= 0f ? time : time + tickInterval;
            }
        }

        public void ClearContacts(bool publishExitEvents)
        {
            if (publishExitEvents)
            {
                foreach (KeyValuePair<ContactKey, BeamContact> pair in activeContacts)
                    PublishExit(pair.Value);
            }
            activeContacts.Clear();
            frameContacts.Clear();
            exitKeys.Clear();
        }

        private void OnEnable()
        {
            if (beam == null)
                beam = GetComponent<Beam>();
            EnsureHitBuffer();
            nextQueryTime = CurrentTime();
            nextTickTime = CurrentTime();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearContacts(true);
        }

        private void Subscribe()
        {
            if (beam == null)
                return;
            beam.PathUpdated -= OnPathUpdated;
            beam.RenderingStopped -= OnRenderingStopped;
            beam.PathUpdated += OnPathUpdated;
            beam.RenderingStopped += OnRenderingStopped;
        }

        private void Unsubscribe()
        {
            if (beam == null)
                return;
            beam.PathUpdated -= OnPathUpdated;
            beam.RenderingStopped -= OnRenderingStopped;
        }

        private void OnPathUpdated(Beam changedBeam)
        {
            float time = CurrentTime();
            if (queryInterval > 0f && time < nextQueryTime)
                return;
            QueryNow();
            nextQueryTime = queryInterval <= 0f ? time : time + queryInterval;
        }

        private void OnRenderingStopped(Beam changedBeam)
        {
            ClearContacts(true);
        }

        private void QuerySegment(BeamStrand strand, int strandIndex, int segmentIndex)
        {
            BeamPoint start = strand[segmentIndex];
            BeamPoint end = strand[segmentIndex + 1];
            Vector3 delta = end.Position - start.Position;
            float length = delta.magnitude;
            if (length <= 0.000001f)
                return;

            Vector3 direction = delta / length;
            int hitCount = radius > 0f
                ? Physics.SphereCastNonAlloc(start.Position, radius, direction, hitBuffer, length, layerMask, triggerInteraction)
                : Physics.RaycastNonAlloc(start.Position, direction, hitBuffer, length, layerMask, triggerInteraction);

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                RaycastHit hit = hitBuffer[hitIndex];
                if (hit.collider == null)
                    continue;
                ContactKey key = new ContactKey(hit.collider.GetInstanceID(), strandIndex);
                BeamContact contact = new BeamContact(
                    hit.collider,
                    hit.point,
                    hit.normal,
                    strandIndex,
                    segmentIndex,
                    start.Distance + hit.distance);

                if (!frameContacts.TryGetValue(key, out BeamContact existing) ||
                    contact.DistanceAlongStrand < existing.DistanceAlongStrand)
                {
                    frameContacts[key] = contact;
                }
            }
        }

        private void PublishLifecycle()
        {
            exitKeys.Clear();
            foreach (KeyValuePair<ContactKey, BeamContact> pair in activeContacts)
            {
                if (!frameContacts.ContainsKey(pair.Key))
                    exitKeys.Add(pair.Key);
            }

            foreach (KeyValuePair<ContactKey, BeamContact> pair in frameContacts)
            {
                if (activeContacts.ContainsKey(pair.Key))
                    PublishStay(pair.Value);
                else
                    PublishEnter(pair.Value);
            }

            for (int i = 0; i < exitKeys.Count; i++)
                PublishExit(activeContacts[exitKeys[i]]);

            Dictionary<ContactKey, BeamContact> swap = activeContacts;
            activeContacts = frameContacts;
            frameContacts = swap;
            frameContacts.Clear();
        }

        private void PublishEnter(in BeamContact contact)
        {
            ContactEntered?.Invoke(this, contact);
            onContactEntered.Invoke(contact);
        }

        private void PublishStay(in BeamContact contact)
        {
            ContactStayed?.Invoke(this, contact);
            onContactStayed.Invoke(contact);
        }

        private void PublishExit(in BeamContact contact)
        {
            ContactExited?.Invoke(this, contact);
            onContactExited.Invoke(contact);
        }

        private void PublishTick(in BeamContact contact)
        {
            ContactTicked?.Invoke(this, contact);
            onContactTicked.Invoke(contact);
        }

        private void EnsureHitBuffer()
        {
            int size = Mathf.Clamp(maximumHitsPerSegment, 1, 64);
            if (hitBuffer == null || hitBuffer.Length != size)
                hitBuffer = new RaycastHit[size];
        }

        private void OnValidate()
        {
            queryInterval = Mathf.Max(0f, queryInterval);
            tickInterval = Mathf.Max(0f, tickInterval);
            radius = Mathf.Max(0f, radius);
            maximumHitsPerSegment = Mathf.Clamp(maximumHitsPerSegment, 1, 64);
            EnsureHitBuffer();
        }

        private static float CurrentTime()
        {
            return Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        }

        private readonly struct ContactKey : IEquatable<ContactKey>
        {
            private readonly int colliderId;
            private readonly int strandIndex;

            public ContactKey(int colliderId, int strandIndex)
            {
                this.colliderId = colliderId;
                this.strandIndex = strandIndex;
            }

            public bool Equals(ContactKey other)
            {
                return colliderId == other.colliderId && strandIndex == other.strandIndex;
            }

            public override bool Equals(object obj)
            {
                return obj is ContactKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (colliderId * 397) ^ strandIndex;
                }
            }
        }
    }
}
