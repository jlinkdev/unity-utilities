using UnityEngine;

namespace jlinkdev.UnityUtilities.Forcefields
{
    internal sealed class ForcefieldImpactBuffer
    {
        internal const int MaximumCapacity = 32;

        private readonly Vector4[] positionsAndTimes = new Vector4[MaximumCapacity];
        private readonly Vector4[] normalsAndStrengths = new Vector4[MaximumCapacity];
        private readonly Vector4[] radiiAndDurations = new Vector4[MaximumCapacity];
        private int nextIndex;

        internal ForcefieldImpactBuffer(int capacity)
        {
            Capacity = Mathf.Clamp(capacity, 1, MaximumCapacity);
            Clear();
        }

        internal int Capacity { get; private set; }
        internal int Count { get; private set; }
        internal Vector4[] PositionsAndTimes => positionsAndTimes;
        internal Vector4[] NormalsAndStrengths => normalsAndStrengths;
        internal Vector4[] RadiiAndDurations => radiiAndDurations;

        internal void SetCapacity(int capacity)
        {
            capacity = Mathf.Clamp(capacity, 1, MaximumCapacity);
            if (Capacity == capacity)
                return;

            Capacity = capacity;
            Clear();
        }

        internal void Add(
            Vector3 localPosition,
            Vector3 localNormal,
            float startTime,
            float strength,
            float radius,
            float duration)
        {
            Vector3 normalizedNormal = localNormal.sqrMagnitude > 0.000001f
                ? localNormal.normalized
                : Vector3.up;

            positionsAndTimes[nextIndex] = new Vector4(
                localPosition.x,
                localPosition.y,
                localPosition.z,
                startTime);
            normalsAndStrengths[nextIndex] = new Vector4(
                normalizedNormal.x,
                normalizedNormal.y,
                normalizedNormal.z,
                Mathf.Max(0f, strength));
            radiiAndDurations[nextIndex] = new Vector4(
                Mathf.Max(0f, radius),
                Mathf.Max(0.01f, duration),
                0f,
                0f);

            nextIndex = (nextIndex + 1) % Capacity;
            Count = Mathf.Min(Count + 1, Capacity);
        }

        internal void Clear()
        {
            Count = 0;
            nextIndex = 0;

            for (int i = 0; i < MaximumCapacity; i++)
            {
                positionsAndTimes[i] = new Vector4(0f, 0f, 0f, -100000f);
                normalsAndStrengths[i] = Vector4.zero;
                radiiAndDurations[i] = new Vector4(0f, 0.01f, 0f, 0f);
            }
        }
    }
}
