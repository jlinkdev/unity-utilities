using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Portals.Tests
{
    public sealed class PortalMathTests
    {
        private GameObject entryObject;
        private GameObject exitObject;

        [SetUp]
        public void SetUp()
        {
            entryObject = new GameObject("Entry");
            exitObject = new GameObject("Exit");
            entryObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            exitObject.transform.SetPositionAndRotation(new Vector3(10f, 2f, -3f), Quaternion.Euler(0f, 90f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(entryObject);
            Object.DestroyImmediate(exitObject);
        }

        [Test]
        public void MapPoint_PreservesLocalOffsetWithHalfTurn()
        {
            Vector3 mapped = PortalMath.MapPoint(entryObject.transform, exitObject.transform, new Vector3(1f, 0.5f, 2f));
            Vector3 expected = exitObject.transform.TransformPoint(new Vector3(-1f, 0.5f, -2f));
            Assert.That(Vector3.Distance(mapped, expected), Is.LessThan(0.0001f));
        }

        [Test]
        public void MapDirection_PreservesMagnitude()
        {
            Vector3 direction = new Vector3(2f, -1f, 4f);
            Vector3 mapped = PortalMath.MapDirection(entryObject.transform, exitObject.transform, direction);
            Assert.That(mapped.magnitude, Is.EqualTo(direction.magnitude).Within(0.0001f));
        }

        [Test]
        public void UniformScaleRatio_UsesPortalAperture()
        {
            entryObject.transform.localScale = Vector3.one;
            exitObject.transform.localScale = new Vector3(2f, 2f, 5f);
            Assert.That(PortalMath.UniformScaleRatio(entryObject.transform, exitObject.transform), Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void SignedDistance_ChangesAcrossPortalPlane()
        {
            Assert.That(PortalMath.SignedDistance(entryObject.transform, Vector3.forward), Is.GreaterThan(0f));
            Assert.That(PortalMath.SignedDistance(entryObject.transform, Vector3.back), Is.LessThan(0f));
        }

        [Test]
        public void NearClipOffset_ClampsZeroToSafeEpsilon()
        {
            PortalRenderSettings settings = ScriptableObject.CreateInstance<PortalRenderSettings>();
            SerializedObject serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("nearClipOffset").floatValue = 0f;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(settings.NearClipOffset, Is.EqualTo(PortalRenderSettings.MinimumNearClipOffset));
            Object.DestroyImmediate(settings);
        }
    }
}
