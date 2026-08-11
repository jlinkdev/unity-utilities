using NUnit.Framework;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams.Tests
{
    public sealed class BeamPathTests
    {
        [Test]
        public void StraightPath_BuildsMeasuredPrimaryStrand()
        {
            GameObject gameObject = new GameObject("Straight Path Test");
            try
            {
                StraightBeamPath provider = gameObject.AddComponent<StraightBeamPath>();
                BeamPathBuffer buffer = new BeamPathBuffer();
                BeamPathContext context = new BeamPathContext(
                    new BeamEndpoint(Vector3.zero, Vector3.up),
                    new BeamEndpoint(Vector3.forward * 4f, Vector3.back),
                    0f,
                    0f,
                    7);

                provider.BuildPath(context, buffer);

                Assert.That(buffer.Count, Is.EqualTo(1));
                Assert.That(buffer[0].Count, Is.EqualTo(2));
                Assert.That(buffer[0].Length, Is.EqualTo(4f).Within(0.0001f));
                Assert.That(buffer[0][1].NormalizedDistance, Is.EqualTo(1f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Resampling_PreservesEndpointsAndAddsUniformPoints()
        {
            GameObject gameObject = new GameObject("Resample Test");
            try
            {
                BeamResampleModifier modifier = gameObject.AddComponent<BeamResampleModifier>();
                modifier.MaximumSegmentLength = 1f;
                BeamPathBuffer buffer = new BeamPathBuffer();
                BeamStrand strand = buffer.AddStrand();
                strand.Add(Vector3.zero);
                strand.Add(Vector3.forward * 4f);
                BeamPathUtility.RecalculateMetrics(strand, Vector3.up);
                BeamPathContext context = new BeamPathContext(
                    new BeamEndpoint(Vector3.zero, Vector3.up),
                    new BeamEndpoint(Vector3.forward * 4f, Vector3.back),
                    0f,
                    0f,
                    1);

                modifier.Modify(context, buffer);

                Assert.That(strand.Count, Is.EqualTo(5));
                Assert.That(strand[0].Position, Is.EqualTo(Vector3.zero));
                Assert.That(strand[strand.Count - 1].Position, Is.EqualTo(Vector3.forward * 4f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void NoiseModifier_KeepsEndpointsPinned()
        {
            GameObject gameObject = new GameObject("Noise Test");
            try
            {
                BeamNoiseModifier modifier = gameObject.AddComponent<BeamNoiseModifier>();
                modifier.Amplitude = 1f;
                BeamPathBuffer buffer = new BeamPathBuffer();
                BeamStrand strand = buffer.AddStrand();
                for (int i = 0; i <= 4; i++)
                    strand.Add(Vector3.forward * i);
                BeamPathUtility.RecalculateMetrics(strand, Vector3.up);
                Vector3 start = strand[0].Position;
                Vector3 end = strand[strand.Count - 1].Position;
                BeamPathContext context = new BeamPathContext(
                    new BeamEndpoint(start, Vector3.up),
                    new BeamEndpoint(end, Vector3.back),
                    1f,
                    0f,
                    4);

                modifier.Modify(context, buffer);

                Assert.That(strand[0].Position, Is.EqualTo(start));
                Assert.That(strand[strand.Count - 1].Position, Is.EqualTo(end));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void BezierPath_PreservesResolvedEndpoints()
        {
            GameObject gameObject = new GameObject("Bezier Test");
            try
            {
                BezierBeamPath provider = gameObject.AddComponent<BezierBeamPath>();
                BeamPathBuffer buffer = new BeamPathBuffer();
                Vector3 start = new Vector3(-1f, 2f, 0f);
                Vector3 end = new Vector3(3f, -1f, 5f);
                BeamPathContext context = new BeamPathContext(
                    new BeamEndpoint(start, Vector3.right),
                    new BeamEndpoint(end, Vector3.left),
                    0f,
                    0f,
                    3);

                provider.BuildPath(context, buffer);

                Assert.That(buffer[0].Count, Is.GreaterThan(2));
                Assert.That(buffer[0][0].Position, Is.EqualTo(start));
                Assert.That(buffer[0][buffer[0].Count - 1].Position, Is.EqualTo(end));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void BranchModifier_ProducesParentedSecondaryStrands()
        {
            GameObject gameObject = new GameObject("Branch Test");
            try
            {
                BeamBranchModifier modifier = gameObject.AddComponent<BeamBranchModifier>();
                BeamPathBuffer buffer = StraightBuffer(5f, 9);
                BeamPathContext context = DefaultContext(5f, 9, 0f);

                modifier.Modify(context, buffer);

                Assert.That(buffer.Count, Is.EqualTo(4));
                for (int i = 1; i < buffer.Count; i++)
                {
                    Assert.That(buffer[i].ParentStrandIndex, Is.EqualTo(0));
                    Assert.That(buffer[i].BranchDepth, Is.EqualTo(1));
                    Assert.That(buffer[i].Count, Is.GreaterThanOrEqualTo(2));
                }
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ElectricalModifier_IsDeterministicAndPinsEndpoints()
        {
            GameObject gameObject = new GameObject("Electrical Test");
            try
            {
                BeamElectricalModifier modifier = gameObject.AddComponent<BeamElectricalModifier>();
                BeamPathBuffer first = StraightBuffer(5f, 14);
                BeamPathBuffer second = StraightBuffer(5f, 14);
                BeamPathContext context = DefaultContext(5f, 14, 1.25f);

                modifier.Modify(context, first);
                modifier.Modify(context, second);

                Assert.That(first[0].Count, Is.EqualTo(second[0].Count));
                Assert.That(first[0][0].Position, Is.EqualTo(Vector3.zero));
                Assert.That(first[0][first[0].Count - 1].Position, Is.EqualTo(Vector3.forward * 5f));
                for (int i = 0; i < first[0].Count; i++)
                    Assert.That(first[0][i].Position, Is.EqualTo(second[0][i].Position));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Evaluate_ReturnsDistanceBasedPoint()
        {
            BeamPathBuffer buffer = new BeamPathBuffer();
            BeamStrand strand = buffer.AddStrand();
            strand.Add(Vector3.zero);
            strand.Add(Vector3.forward);
            strand.Add(Vector3.forward * 4f);
            BeamPathUtility.RecalculateMetrics(strand, Vector3.up);

            BeamPoint midpoint = BeamPathUtility.Evaluate(strand, 0.5f);

            Assert.That(midpoint.Position.z, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(midpoint.Distance, Is.EqualTo(2f).Within(0.0001f));
        }

        private static BeamPathBuffer StraightBuffer(float length, int seed)
        {
            BeamPathBuffer buffer = new BeamPathBuffer();
            BeamStrand strand = buffer.AddStrand(seed: seed);
            strand.Add(Vector3.zero);
            strand.Add(Vector3.forward * length);
            BeamPathUtility.RecalculateMetrics(strand, Vector3.up);
            return buffer;
        }

        private static BeamPathContext DefaultContext(float length, int seed, float time)
        {
            return new BeamPathContext(
                new BeamEndpoint(Vector3.zero, Vector3.up),
                new BeamEndpoint(Vector3.forward * length, Vector3.back),
                time,
                0f,
                seed);
        }
    }
}
