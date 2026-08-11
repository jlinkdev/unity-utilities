using NUnit.Framework;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Forcefields.Tests
{
    public sealed class ForcefieldTests
    {
        private GameObject gameObject;
        private Forcefield forcefield;
        private Renderer renderer;

        [SetUp]
        public void SetUp()
        {
            gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            renderer = gameObject.GetComponent<Renderer>();
            forcefield = gameObject.AddComponent<Forcefield>();
            forcefield.TargetRenderers = new[] { renderer };
            forcefield.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void AddImpact_IncrementsCountAndRaisesWorldSpaceEvent()
        {
            ForcefieldImpact received = default;
            bool eventRaised = false;
            forcefield.ImpactAdded += (_, impact) =>
            {
                received = impact;
                eventRaised = true;
            };

            Vector3 position = new Vector3(1f, 2f, 3f);
            forcefield.AddImpact(position, Vector3.forward, 1.4f, 0.2f);

            Assert.That(forcefield.ActiveImpactCount, Is.EqualTo(1));
            Assert.That(eventRaised, Is.True);
            Assert.That(received.Position, Is.EqualTo(position));
            Assert.That(received.Strength, Is.EqualTo(1.4f));
            Assert.That(received.Radius, Is.EqualTo(0.2f));
        }

        [Test]
        public void ClearImpacts_ResetsCount()
        {
            forcefield.AddImpact(Vector3.up);
            forcefield.ClearImpacts();
            Assert.That(forcefield.ActiveImpactCount, Is.Zero);
        }

        [Test]
        public void Refresh_PreservesUnrelatedPropertyBlockValues()
        {
            int unrelatedProperty = Shader.PropertyToID("_ForcefieldTestUnrelated");
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetFloat(unrelatedProperty, 17f);
            renderer.SetPropertyBlock(block);

            forcefield.Refresh();
            renderer.GetPropertyBlock(block);

            Assert.That(block.GetFloat(unrelatedProperty), Is.EqualTo(17f));
        }

        [Test]
        public void ChangingCapacity_ClearsCurrentHistory()
        {
            forcefield.AddImpact(Vector3.up);
            forcefield.ImpactBufferCapacity = ForcefieldImpactCapacity.Eight;

            Assert.That(forcefield.ImpactCapacity, Is.EqualTo(8));
            Assert.That(forcefield.ActiveImpactCount, Is.Zero);
        }
    }
}
