using NUnit.Framework;
using UnityEngine;

namespace jlinkdev.UnityUtilities.WorldScanning.Tests
{
    public sealed class ScanSystemTests
    {
        private ScanProfile profile;

        [SetUp]
        public void SetUp()
        {
            ScanSystem.ResetForTests();
            profile = ScriptableObject.CreateInstance<ScanProfile>();
        }

        [TearDown]
        public void TearDown()
        {
            ScanSystem.ResetForTests();
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void Emit_ReturnsLiveHandle()
        {
            ScanHandle handle = ScanSystem.Emit(Vector3.zero, profile);

            Assert.That(handle, Is.Not.EqualTo(ScanHandle.Invalid));
            Assert.That(handle.IsValid, Is.True);
            Assert.That(ScanSystem.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_AdvancesRadiusAndCompletesPulse()
        {
            ScanHandle handle = ScanSystem.Emit(Vector3.zero, profile);

            ScanSystem.Tick(profile.Duration * 0.5f, profile.Duration * 0.5f);
            Assert.That(handle.Radius, Is.GreaterThan(0f));
            Assert.That(handle.NormalizedTime, Is.EqualTo(0.5f).Within(0.001f));

            ScanSystem.Tick(profile.Duration, profile.Duration);
            Assert.That(handle.IsValid, Is.False);
            Assert.That(ScanSystem.ActiveCount, Is.Zero);
        }

        [Test]
        public void Capacity_ReplacesOldestPulse()
        {
            ScanCompletionReason? endedReason = null;
            ScanSystem.ScanEnded += ended => endedReason = ended.Reason;
            ScanHandle oldest = ScanSystem.Emit(Vector3.zero, profile);
            ScanHandle nextOldest = ScanSystem.Emit(Vector3.right, profile);
            for (int i = 2; i <= ScanSystem.MaximumActiveScans; i++)
                ScanSystem.Emit(new Vector3(i, 0f, 0f), profile);

            Assert.That(oldest.IsValid, Is.False);
            Assert.That(nextOldest.IsValid, Is.True);
            Assert.That(ScanSystem.ActiveCount, Is.EqualTo(ScanSystem.MaximumActiveScans));
            Assert.That(endedReason, Is.EqualTo(ScanCompletionReason.Replaced));

            ScanSystem.Emit(Vector3.forward, profile);

            Assert.That(nextOldest.IsValid, Is.False);
            Assert.That(ScanSystem.ActiveCount, Is.EqualTo(ScanSystem.MaximumActiveScans));
        }

        [Test]
        public void CancelAll_EndsEveryPulseAsCancelled()
        {
            int cancelledCount = 0;
            ScanSystem.ScanEnded += ended =>
            {
                if (ended.Reason == ScanCompletionReason.Cancelled)
                    cancelledCount++;
            };
            ScanSystem.Emit(Vector3.zero, profile);
            ScanSystem.Emit(Vector3.right, profile);
            ScanSystem.Emit(Vector3.forward, profile);

            ScanSystem.CancelAll();

            Assert.That(ScanSystem.ActiveCount, Is.Zero);
            Assert.That(cancelledCount, Is.EqualTo(3));
        }

        [Test]
        public void Receiver_IsNotifiedWhenFrontCrossesPosition()
        {
            GameObject receiverObject = new GameObject("Receiver");
            receiverObject.transform.position = Vector3.right * 5f;
            ScanReceiver receiver = receiverObject.AddComponent<ScanReceiver>();
            // EditMode does not guarantee MonoBehaviour enable callbacks immediately after AddComponent.
            ScanSystem.RegisterReceiver(receiver);
            int hits = 0;
            receiver.Scanned += _ => hits++;

            ScanSystem.Emit(Vector3.zero, profile);
            ScanSystem.Tick(profile.Duration * 0.25f, profile.Duration * 0.25f);
            ScanSystem.Tick(profile.Duration * 0.25f, profile.Duration * 0.25f);

            Assert.That(hits, Is.EqualTo(1));
            Assert.That(receiver.LastHit.Distance, Is.EqualTo(5f).Within(0.001f));
            Object.DestroyImmediate(receiverObject);
        }
    }
}
