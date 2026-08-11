using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace jlinkdev.UnityUtilities.Forcefields.Tests
{
    public sealed class ForcefieldPlayModeTests
    {
        [UnityTest]
        public IEnumerator BlendToPreset_CompletesWithoutCreatingARequiredPreset()
        {
            GameObject gameObject = new GameObject("Forcefield Blend Test");
            Forcefield forcefield = gameObject.AddComponent<Forcefield>();

            forcefield.BlendToPreset(null, 0.01f);
            yield return new WaitForSeconds(0.03f);

            Assert.That(forcefield.IsBlendingPreset, Is.False);
            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator MovingForcefield_RetainsImpactHistory()
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Forcefield forcefield = gameObject.AddComponent<Forcefield>();
            forcefield.TargetRenderers = new[] { gameObject.GetComponent<Renderer>() };
            forcefield.AddImpact(gameObject.transform.position + Vector3.up);

            gameObject.transform.position = new Vector3(5f, 2f, -3f);
            yield return null;

            Assert.That(forcefield.ActiveImpactCount, Is.EqualTo(1));
            Object.Destroy(gameObject);
        }
    }
}
