using System;
using System.Collections.Generic;
using jlinkdev.UnityUtilities.WorldScanning.Rendering;
using UnityEngine;

namespace jlinkdev.UnityUtilities.WorldScanning
{
    public static class ScanSystem
    {
        public const int MaximumActiveScans = 16;

        internal struct ActiveScan
        {
            public int id;
            public uint generation;
            public Vector3 origin;
            public Vector3 axis;
            public ScanProfile profile;
            public ScanShape shape;
            public float rangeMultiplier;
            public float duration;
            public float elapsed;
            public float intensityMultiplier;
            public float previousRadius;
            public float radius;

            public ScanHandle Handle => new ScanHandle(id, generation);
            public float NormalizedTime => duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
        }

        internal readonly struct ScanRenderData
        {
            internal ScanRenderData(in ActiveScan scan)
            {
                float normalizedTime = scan.NormalizedTime;
                Origin = scan.origin;
                Axis = scan.axis;
                Radius = scan.radius;
                Shape = scan.shape;
                CylinderHalfHeight = scan.profile.CylinderHalfHeight;
                Intensity = scan.profile.EvaluateIntensity(normalizedTime) * scan.intensityMultiplier;
                Color = scan.profile.EvaluateColor(normalizedTime);
                Visuals = scan.profile.GetVisualSettings();
                NormalizedTime = normalizedTime;
            }

            public Vector3 Origin { get; }
            public Vector3 Axis { get; }
            public float Radius { get; }
            public ScanShape Shape { get; }
            public float CylinderHalfHeight { get; }
            public float Intensity { get; }
            public Color Color { get; }
            public ScanVisualSettings Visuals { get; }
            public float NormalizedTime { get; }
        }

        private static readonly ActiveScan[] ActiveScans = new ActiveScan[MaximumActiveScans];
        private static readonly List<ScanReceiver> Receivers = new List<ScanReceiver>(64);
        private static int activeCount;
        private static int nextId = 1;
        private static uint nextGeneration = 1;

        public static event Action<ScanHandle> ScanStarted;
        public static event Action<ScanEndedEvent> ScanEnded;

        public static int ActiveCount => activeCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeCount = 0;
            nextId = 1;
            nextGeneration = 1;
            Receivers.Clear();
            ScanStarted = null;
            ScanEnded = null;
        }

        public static ScanHandle Emit(Vector3 origin, ScanProfile profile)
        {
            return Emit(new ScanEmission(origin, profile, Vector3.up));
        }

        public static ScanHandle Emit(in ScanEmission emission)
        {
            if (emission.Profile == null)
            {
                Debug.LogWarning("[jlinkdev World Scanning] Cannot emit a scan without a ScanProfile.");
                return ScanHandle.Invalid;
            }

            EnsureDriver();
            if (activeCount >= MaximumActiveScans)
                EndAt(0, ScanCompletionReason.Replaced);

            int id = nextId++;
            if (nextId == int.MaxValue)
                nextId = 1;
            uint generation = nextGeneration++;
            if (nextGeneration == 0)
                nextGeneration = 1;

            ActiveScan scan = new ActiveScan
            {
                id = id,
                generation = generation,
                origin = emission.Origin,
                axis = emission.Axis,
                profile = emission.Profile,
                shape = emission.Shape,
                rangeMultiplier = emission.RangeMultiplier,
                duration = emission.Profile.Duration * emission.DurationMultiplier,
                elapsed = 0f,
                intensityMultiplier = emission.IntensityMultiplier,
                previousRadius = -0.0001f,
                radius = emission.Profile.EvaluateRadius(0f) * emission.RangeMultiplier
            };
            ActiveScans[activeCount++] = scan;
            ScanHandle handle = scan.Handle;
            ScanStarted?.Invoke(handle);
            return handle;
        }

        public static bool Cancel(ScanHandle handle)
        {
            int index = FindIndex(handle);
            if (index < 0)
                return false;
            EndAt(index, ScanCompletionReason.Cancelled);
            return true;
        }

        public static void CancelAll()
        {
            while (activeCount > 0)
                EndAt(activeCount - 1, ScanCompletionReason.Cancelled);
        }

        public static bool SetIntensity(ScanHandle handle, float multiplier)
        {
            int index = FindIndex(handle);
            if (index < 0)
                return false;
            ActiveScan scan = ActiveScans[index];
            scan.intensityMultiplier = Mathf.Max(0f, multiplier);
            ActiveScans[index] = scan;
            return true;
        }

        public static bool IsAlive(ScanHandle handle)
        {
            return FindIndex(handle) >= 0;
        }

        public static float GetNormalizedTime(ScanHandle handle)
        {
            int index = FindIndex(handle);
            return index >= 0 ? ActiveScans[index].NormalizedTime : 0f;
        }

        public static float GetRadius(ScanHandle handle)
        {
            int index = FindIndex(handle);
            return index >= 0 ? ActiveScans[index].radius : 0f;
        }

        internal static void Tick(float scaledDeltaTime, float unscaledDeltaTime)
        {
            for (int index = activeCount - 1; index >= 0; index--)
            {
                ActiveScan scan = ActiveScans[index];
                scan.previousRadius = scan.radius;
                scan.elapsed += scan.profile.TimeMode == ScanTimeMode.Unscaled ? unscaledDeltaTime : scaledDeltaTime;
                scan.radius = scan.profile.EvaluateRadius(scan.NormalizedTime) * scan.rangeMultiplier;
                NotifyReceivers(in scan);

                if (scan.elapsed >= scan.duration)
                {
                    ActiveScans[index] = scan;
                    EndAt(index, ScanCompletionReason.Completed);
                }
                else
                {
                    ActiveScans[index] = scan;
                }
            }
        }

        internal static int FillRenderData(ScanRenderData[] destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            int count = Mathf.Min(activeCount, destination.Length);
            for (int i = 0; i < count; i++)
                destination[i] = new ScanRenderData(in ActiveScans[i]);
            return count;
        }

        internal static void RegisterReceiver(ScanReceiver receiver)
        {
            if (receiver != null && !Receivers.Contains(receiver))
                Receivers.Add(receiver);
        }

        internal static void UnregisterReceiver(ScanReceiver receiver)
        {
            Receivers.Remove(receiver);
        }

        internal static void ResetForTests()
        {
            activeCount = 0;
            nextId = 1;
            nextGeneration = 1;
            Receivers.Clear();
            ScanStarted = null;
            ScanEnded = null;
        }

        private static void EnsureDriver()
        {
            if (!Application.isPlaying || ScanSystemDriver.Instance != null)
                return;
            GameObject driverObject = new GameObject("jlinkdev World Scan System")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            UnityEngine.Object.DontDestroyOnLoad(driverObject);
            driverObject.AddComponent<ScanSystemDriver>();
        }

        private static int FindIndex(ScanHandle handle)
        {
            if (handle.Id == 0)
                return -1;
            for (int i = 0; i < activeCount; i++)
            {
                if (ActiveScans[i].id == handle.Id && ActiveScans[i].generation == handle.Generation)
                    return i;
            }
            return -1;
        }

        private static void EndAt(int index, ScanCompletionReason reason)
        {
            ScanHandle handle = ActiveScans[index].Handle;
            activeCount--;
            for (int i = index; i < activeCount; i++)
                ActiveScans[i] = ActiveScans[i + 1];
            ActiveScans[activeCount] = default;
            ScanEnded?.Invoke(new ScanEndedEvent(handle, reason));
        }

        private static void NotifyReceivers(in ActiveScan scan)
        {
            if (scan.radius < scan.previousRadius)
                return;

            for (int i = Receivers.Count - 1; i >= 0; i--)
            {
                ScanReceiver receiver = Receivers[i];
                if (receiver == null)
                {
                    Receivers.RemoveAt(i);
                    continue;
                }

                Vector3 point = receiver.Position;
                Vector3 offset = point - scan.origin;
                float distance;
                if (scan.shape == ScanShape.Cylinder)
                {
                    float axialDistance = Vector3.Dot(offset, scan.axis);
                    if (scan.profile.CylinderHalfHeight > 0f && Mathf.Abs(axialDistance) > scan.profile.CylinderHalfHeight)
                        continue;
                    distance = (offset - scan.axis * axialDistance).magnitude;
                }
                else
                {
                    distance = offset.magnitude;
                }

                if (scan.previousRadius < distance && scan.radius >= distance)
                {
                    ScanHit hit = new ScanHit(scan.Handle, scan.origin, point, distance, scan.NormalizedTime);
                    receiver.Notify(in hit);
                }
            }
        }
    }

    [DefaultExecutionOrder(-10000)]
    internal sealed class ScanSystemDriver : MonoBehaviour
    {
        internal static ScanSystemDriver Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            ScanSystem.Tick(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void LateUpdate()
        {
            ScanShaderBridge.UploadGlobals();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
