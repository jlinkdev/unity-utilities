using System;
using UnityEngine;

namespace jlinkdev.UnityUtilities.WorldScanning
{
    public enum ScanShape
    {
        Sphere = 0,
        Cylinder = 1
    }

    public enum ScanTimeMode
    {
        Scaled = 0,
        Unscaled = 1
    }

    public enum ScanCompletionReason
    {
        Completed = 0,
        Cancelled = 1,
        Replaced = 2
    }

    [Serializable]
    public readonly struct ScanHit
    {
        public ScanHit(ScanHandle handle, Vector3 origin, Vector3 point, float distance, float normalizedTime)
        {
            Handle = handle;
            Origin = origin;
            Point = point;
            Distance = distance;
            NormalizedTime = normalizedTime;
        }

        public ScanHandle Handle { get; }
        public Vector3 Origin { get; }
        public Vector3 Point { get; }
        public float Distance { get; }
        public float NormalizedTime { get; }
    }

    public readonly struct ScanEndedEvent
    {
        public ScanEndedEvent(ScanHandle handle, ScanCompletionReason reason)
        {
            Handle = handle;
            Reason = reason;
        }

        public ScanHandle Handle { get; }
        public ScanCompletionReason Reason { get; }
    }
}
