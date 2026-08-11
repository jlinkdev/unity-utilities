using NUnit.Framework;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Forcefields.Tests
{
    public sealed class ForcefieldImpactBufferTests
    {
        [Test]
        public void Add_StopsCountAtCapacityAndOverwritesOldestSlot()
        {
            ForcefieldImpactBuffer buffer = new ForcefieldImpactBuffer(4);

            for (int i = 0; i < 5; i++)
                buffer.Add(new Vector3(i, 0f, 0f), Vector3.up, i, 1f, 0.1f, 1f);

            Assert.That(buffer.Count, Is.EqualTo(4));
            Assert.That(buffer.PositionsAndTimes[0].x, Is.EqualTo(4f));
            Assert.That(buffer.PositionsAndTimes[1].x, Is.EqualTo(1f));
        }

        [Test]
        public void SetCapacity_ClearsExistingImpacts()
        {
            ForcefieldImpactBuffer buffer = new ForcefieldImpactBuffer(16);
            buffer.Add(Vector3.one, Vector3.forward, 2f, 1f, 0.1f, 1f);

            buffer.SetCapacity(8);

            Assert.That(buffer.Capacity, Is.EqualTo(8));
            Assert.That(buffer.Count, Is.Zero);
        }

        [Test]
        public void Add_NormalizesNormalAndClampsVisualInputs()
        {
            ForcefieldImpactBuffer buffer = new ForcefieldImpactBuffer(4);
            buffer.Add(Vector3.zero, Vector3.up * 4f, 0f, -2f, -1f, 0f);

            Assert.That(buffer.NormalsAndStrengths[0].y, Is.EqualTo(1f));
            Assert.That(buffer.NormalsAndStrengths[0].w, Is.Zero);
            Assert.That(buffer.RadiiAndDurations[0].x, Is.Zero);
            Assert.That(buffer.RadiiAndDurations[0].y, Is.EqualTo(0.01f));
        }
    }
}
