using NUnit.Framework;
using UnityEngine;

namespace jlinkdev.UnityUtilities.WaterSystem.Tests
{
    public sealed class WaterWaveTests
    {
        private WaterProfile profile;

        [SetUp] public void SetUp() => profile = ScriptableObject.CreateInstance<WaterProfile>();
        [TearDown] public void TearDown() => Object.DestroyImmediate(profile);

        [Test]
        public void MissingWaves_ReturnsFlatSurfaceAtBodyElevation()
        {
            profile.Waves = null;
            Vector3 origin = new Vector3(8f, 3f, -2f);
            WaterSample sample = WaterWaves.Evaluate(profile, origin, new Vector3(100f, -50f, 20f), 100f);
            Assert.That(sample.Height, Is.EqualTo(3f));
            Assert.That(sample.Normal, Is.EqualTo(Vector3.up));
            Assert.That(sample.Velocity, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void SingleWave_HasKnownCrestAndTravelsAtSpeedInMetresPerSecond()
        {
            profile.Waves = new[] { new WaveSettings(0f, 2f, 8f, 2f) };
            Vector3 origin = new Vector3(10f, 4f, -3f);
            WaterSample first = WaterWaves.Evaluate(profile, origin, origin + Vector3.right * 2f, 0f);
            WaterSample later = WaterWaves.Evaluate(profile, origin, origin + Vector3.right * 4f, 1f);
            Assert.That(first.Height, Is.EqualTo(6f).Within(0.00001f));
            Assert.That(later.Height, Is.EqualTo(first.Height).Within(0.00001f));
            Assert.That(Vector3.Distance(first.Normal, Vector3.up), Is.LessThan(0.00001f));
            Assert.That(first.Velocity.magnitude, Is.LessThan(0.00001f));
        }

        [Test]
        public void AnalyticNormalAndVelocity_AgreeWithIndependentFiniteDifferences()
        {
            Vector3 origin = new Vector3(3f, 5f, 8f);
            Vector3 query = origin + new Vector3(1.23f, 0f, -0.81f);
            const float time = 0.73f;
            const float delta = 0.002f;
            WaterSample sample = WaterWaves.Evaluate(profile, origin, query, time);
            float dx = (WaterWaves.Evaluate(profile, origin, query + Vector3.right * delta, time).Height
                - WaterWaves.Evaluate(profile, origin, query - Vector3.right * delta, time).Height) / (2f * delta);
            float dz = (WaterWaves.Evaluate(profile, origin, query + Vector3.forward * delta, time).Height
                - WaterWaves.Evaluate(profile, origin, query - Vector3.forward * delta, time).Height) / (2f * delta);
            float vy = (WaterWaves.Evaluate(profile, origin, query, time + delta).Height
                - WaterWaves.Evaluate(profile, origin, query, time - delta).Height) / (2f * delta);
            Assert.That(Vector3.Distance(sample.Normal, new Vector3(-dx, 1f, -dz).normalized), Is.LessThan(0.001f));
            Assert.That(sample.Velocity.y, Is.EqualTo(vy).Within(0.001f));
        }

        [Test]
        public void WaveBudget_IgnoresWavesBeyondEightInBothSamplesAndBounds()
        {
            profile.Waves = new WaveSettings[WaterProfile.MaximumWaves + 1];
            profile.Waves[WaterProfile.MaximumWaves] = new WaveSettings(0f, 1000f, 1f, 1f, 1f);
            WaterSample sample = WaterWaves.Evaluate(profile, Vector3.zero, Vector3.one, 0f);
            Assert.That(sample.Height, Is.Zero);
            Assert.That(profile.MaximumDisplacement, Is.Zero);
        }

        [Test]
        public void NonFiniteWaveParameters_AreSanitized()
        {
            profile.Waves = new[] { new WaveSettings(float.NaN, float.PositiveInfinity, float.NaN, float.NegativeInfinity, float.NaN) };
            WaterSample sample = WaterWaves.Evaluate(profile, Vector3.zero, Vector3.one, float.NaN);
            Assert.That(float.IsNaN(sample.Height) || float.IsInfinity(sample.Height), Is.False);
            Assert.That(sample.Normal, Is.EqualTo(Vector3.up));
            Assert.That(sample.Velocity, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void ExtremeFiniteWaveParameters_AreBoundedBeforePackingAndSampling()
        {
            WaveSettings wave = new WaveSettings(float.MaxValue, float.MaxValue, float.MaxValue, float.MinValue, float.MaxValue);
            WaterWaves.Pack(wave, out Vector4 shape, out Vector4 motion);
            for (int i = 0; i < 4; i++)
            {
                Assert.That(float.IsNaN(shape[i]) || float.IsInfinity(shape[i]), Is.False);
                Assert.That(float.IsNaN(motion[i]) || float.IsInfinity(motion[i]), Is.False);
            }
            profile.Waves = new[] { wave };
            WaterSample sample = WaterWaves.Evaluate(profile, Vector3.zero, Vector3.one, 1f);
            Assert.That(Mathf.Abs(sample.Height), Is.LessThanOrEqualTo(profile.MaximumDisplacement));
            Assert.That(sample.Normal.sqrMagnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(float.IsNaN(sample.Velocity.y) || float.IsInfinity(sample.Velocity.y), Is.False);
            Assert.That(profile.MaximumDisplacement, Is.EqualTo(shape.z));
        }

        [TestCase(0.2f, 0.3f)]
        [TestCase(0.8f, 0.7f)]
        public void GridSample_LiesOnRenderedTrianglePlane(float u, float v)
        {
            profile.Waves = new[] { new WaveSettings(37f, 1.2f, 3.7f, 0.7f, 0.4f) };
            Vector3 origin = new Vector3(3f, 5f, 2f);
            Vector2 size = new Vector2(4f, 6f);
            Vector3 corner = origin - new Vector3(2f, 0f, 3f);
            Vector3 a = WaterWaves.Evaluate(profile, origin, corner, 0.7f).Position;
            Vector3 b = WaterWaves.Evaluate(profile, origin, corner + new Vector3(4f, 0f, 0f), 0.7f).Position;
            Vector3 c = WaterWaves.Evaluate(profile, origin, corner + new Vector3(0f, 0f, 6f), 0.7f).Position;
            Vector3 d = WaterWaves.Evaluate(profile, origin, corner + new Vector3(4f, 0f, 6f), 0.7f).Position;
            Plane triangle = u + v <= 1f ? new Plane(a, c, b) : new Plane(b, c, d);
            WaterSample sample = WaterWaves.EvaluateGrid(profile, origin, size, 1, corner + new Vector3(4f * u, 0f, 6f * v), 0.7f);
            Assert.That(Mathf.Abs(triangle.GetDistanceToPoint(sample.Position)), Is.LessThan(0.00001f));
            Assert.That(Vector3.Dot(sample.Normal, triangle.normal), Is.GreaterThan(0.99999f));
        }

        [Test]
        public void GridCornerAndSharedDiagonal_AreContinuous()
        {
            Vector2 size = new Vector2(4f, 4f);
            Vector3 corner = new Vector3(2f, 0f, 2f);
            WaterSample boundary = WaterWaves.EvaluateGrid(profile, Vector3.zero, size, 4, corner, 0.5f);
            Assert.That(boundary.Height, Is.EqualTo(WaterWaves.Evaluate(profile, Vector3.zero, corner, 0.5f).Height).Within(0.00001f));
            Vector3 diagonal = new Vector3(-1.4f, 0f, -1.6f);
            float left = WaterWaves.EvaluateGrid(profile, Vector3.zero, size, 4, diagonal - Vector3.right * 0.00001f, 0.5f).Height;
            float right = WaterWaves.EvaluateGrid(profile, Vector3.zero, size, 4, diagonal + Vector3.right * 0.00001f, 0.5f).Height;
            Assert.That(left, Is.EqualTo(right).Within(0.00001f));
        }
    }
}
