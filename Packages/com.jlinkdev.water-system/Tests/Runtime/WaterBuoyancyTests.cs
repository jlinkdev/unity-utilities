using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace jlinkdev.UnityUtilities.WaterSystem.Tests
{
    public sealed class WaterBuoyancyTests
    {
        private readonly List<Object> cleanup = new List<Object>();
        private WaterBody water;

        [SetUp]
        public void SetUp()
        {
            WaterProfile profile = ScriptableObject.CreateInstance<WaterProfile>();
            profile.Waves = new WaveSettings[0];
            cleanup.Add(profile);
            GameObject waterObject = new GameObject("Buoyancy test water");
            cleanup.Add(waterObject);
            water = waterObject.AddComponent<WaterBody>();
            water.Profile = profile;
            water.Size = new Vector2(20f, 20f);
            water.Depth = 5f;
            water.Resolution = 4;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = cleanup.Count - 1; i >= 0; i--) if (cleanup[i] != null) Object.Destroy(cleanup[i]);
            cleanup.Clear();
            yield return null;
        }

        private WaterBuoyancy MakeFloat(Vector3 position, float mass = 1f)
        {
            GameObject item = new GameObject("Test float");
            cleanup.Add(item);
            item.transform.position = position;
            Rigidbody body = item.AddComponent<Rigidbody>();
            body.mass = mass;
            body.useGravity = false;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            WaterBuoyancy buoyancy = item.AddComponent<WaterBuoyancy>();
            buoyancy.BodyOverride = water;
            buoyancy.FloatPoints = new[] { Vector3.zero };
            buoyancy.FullSubmersionDepth = 0.1f;
            buoyancy.Buoyancy = 2f;
            buoyancy.LinearDamping = 0f;
            buoyancy.AngularDamping = 0f;
            return buoyancy;
        }

        [UnityTest]
        public IEnumerator LiftAcceleration_IsIndependentOfMass()
        {
            WaterBuoyancy light = MakeFloat(new Vector3(-2f, -1f, 0f), 1f);
            WaterBuoyancy heavy = MakeFloat(new Vector3(2f, -1f, 0f), 100f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Rigidbody lightBody = light.GetComponent<Rigidbody>();
            Rigidbody heavyBody = heavy.GetComponent<Rigidbody>();
            Assert.That(lightBody.linearVelocity.y, Is.GreaterThan(0f));
            Assert.That(heavyBody.linearVelocity.y, Is.EqualTo(lightBody.linearVelocity.y).Within(0.0001f));
            Assert.That(light.ActivePointCount, Is.EqualTo(1));
            Assert.That(heavy.MeanSubmersion, Is.EqualTo(1f));
            Assert.That(heavyBody.useGravity, Is.False, "The component must preserve Rigidbody settings.");
        }

        [UnityTest]
        public IEnumerator FiniteVolume_NoLiftBeyondSidesOrBelowBottom()
        {
            WaterBuoyancy outside = MakeFloat(new Vector3(11f, -1f, 0f));
            WaterBuoyancy below = MakeFloat(new Vector3(0f, -6f, 0f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(outside.GetComponent<Rigidbody>().linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(below.GetComponent<Rigidbody>().linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(outside.ActivePointCount, Is.Zero);
            Assert.That(below.ActivePointCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Damping_ReducesMotionWithoutChangingRigidbodyDamping()
        {
            WaterBuoyancy item = MakeFloat(new Vector3(0f, -1f, 0f));
            Rigidbody body = item.GetComponent<Rigidbody>();
            item.Buoyancy = 0f;
            item.LinearDamping = 5f;
            body.linearVelocity = Vector3.right * 4f;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(body.linearVelocity.x, Is.InRange(0f, 3.99f));
            Assert.That(body.linearDamping, Is.Zero);
        }

        [UnityTest]
        public IEnumerator EmptyAndInvalidFloatPoints_DoNotApplyNonFiniteForces()
        {
            WaterBuoyancy empty = MakeFloat(new Vector3(-2f, -1f, 0f));
            WaterBuoyancy invalid = MakeFloat(new Vector3(2f, -1f, 0f));
            empty.FloatPoints = null;
            invalid.FloatPoints = new[] { new Vector3(float.NaN, 0f, 0f) };
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(empty.ActivePointCount, Is.Zero);
            Assert.That(invalid.ActivePointCount, Is.Zero);
            Assert.That(empty.GetComponent<Rigidbody>().linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(invalid.GetComponent<Rigidbody>().linearVelocity, Is.EqualTo(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator DisabledWater_StopsBuoyancyImmediately()
        {
            WaterBuoyancy item = MakeFloat(new Vector3(0f, -1f, 0f));
            water.enabled = false;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(item.ActivePointCount, Is.Zero);
            Assert.That(item.GetComponent<Rigidbody>().linearVelocity, Is.EqualTo(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator ContactProbe_DisableExitsExactlyOnceAndEnableResetsState()
        {
            GameObject item = new GameObject("Lifecycle contact probe");
            cleanup.Add(item);
            item.transform.position = Vector3.down;
            WaterContactProbe probe = item.AddComponent<WaterContactProbe>();
            probe.BodyOverride = water;
            int entered = 0, exited = 0;
            probe.EnteredWater.AddListener(_ => entered++);
            probe.ExitedWater.AddListener(contact =>
            {
                exited++;
                Assert.That(contact.IsUnderwater, Is.False);
                Assert.That(contact.Body, Is.SameAs(water));
            });
            probe.Refresh();
            Assert.That(probe.IsUnderwater, Is.True);
            Assert.That(entered, Is.EqualTo(1));
            probe.enabled = false;
            Assert.That(probe.IsUnderwater, Is.False);
            Assert.That(exited, Is.EqualTo(1));
            probe.enabled = false;
            Assert.That(exited, Is.EqualTo(1));
            probe.enabled = true;
            Assert.That(probe.IsUnderwater, Is.False);
            probe.Refresh();
            yield return null;
            Assert.That(probe.IsUnderwater, Is.True);
            Assert.That(entered, Is.EqualTo(2));
            Assert.That(exited, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SuspendedParticles_StayInsideAnimatedWaterAndReleaseOwnedObjects()
        {
            water.Profile.Waves = new[] { new WaveSettings(30f, 0.5f, 6f, 1f) };
            GameObject cloud = new GameObject("Particle cloud");
            cleanup.Add(cloud);
            cloud.transform.position = Vector3.down;
            WaterParticles particles = cloud.AddComponent<WaterParticles>();
            particles.BodyOverride = water;
            particles.Count = 64;
            particles.Radius = new Vector3(12f, 7f, 12f);
            particles.Refresh();
            Assert.That(particles.ParticleSystem, Is.Not.Null);
            Assert.That(particles.ActiveParticleCount, Is.GreaterThan(0));
            ParticleSystem.Particle[] values = new ParticleSystem.Particle[64];
            int count = particles.ParticleSystem.GetParticles(values);
            Assert.That(count, Is.EqualTo(particles.ActiveParticleCount));
            for (int i = 0; i < count; i++)
                Assert.That(water.Contains(values[i].position, water.SurfaceTime), Is.True, "Particle " + i);
            ParticleSystem owned = particles.ParticleSystem;
            particles.enabled = false;
            Assert.That(particles.ActiveParticleCount, Is.Zero);
            Assert.That(particles.ParticleSystem, Is.Null);
            yield return null;
            Assert.That(owned == null, Is.True);
        }

        [UnityTest]
        public IEnumerator SuspendedParticles_EmptyCloudWhenFollowTargetLeavesWater()
        {
            GameObject cloud = new GameObject("Particle cloud");
            cleanup.Add(cloud);
            cloud.transform.position = Vector3.down;
            WaterParticles particles = cloud.AddComponent<WaterParticles>();
            particles.BodyOverride = water;
            particles.Count = 32;
            particles.Radius = Vector3.one;
            particles.Refresh();
            Assert.That(particles.ActiveParticleCount, Is.GreaterThan(0));
            cloud.transform.position = Vector3.up * 100f;
            particles.Refresh();
            Assert.That(particles.ActiveParticleCount, Is.Zero);
            Assert.That(particles.ParticleSystem.particleCount, Is.Zero);
            yield return null;
        }
    }
}
