using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace jlinkdev.UnityUtilities.Portals.Tests
{
    public sealed class PortalTraversalTests
    {
        [UnityTest]
        public IEnumerator RigidbodyTraversal_MapsPoseVelocityAndUniformScale()
        {
            GameObject entryObject = new GameObject("Entry Portal");
            GameObject exitObject = new GameObject("Exit Portal");
            entryObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            exitObject.transform.SetPositionAndRotation(new Vector3(8f, 1f, 2f), Quaternion.Euler(0f, 90f, 0f));
            exitObject.transform.localScale = Vector3.one * 0.5f;

            Portal entry = entryObject.AddComponent<Portal>();
            Portal exit = exitObject.AddComponent<Portal>();
            entry.LinkedPortal = exit;
            exit.LinkedPortal = entry;

            GameObject travellerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            travellerObject.name = "Rigidbody Traveller";
            travellerObject.transform.position = new Vector3(0.25f, 0.5f, -1f);
            Rigidbody body = travellerObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            PortalTraveller traveller = travellerObject.AddComponent<PortalTraveller>();
            yield return null;

            Vector3 startPosition = travellerObject.transform.position;
            Vector3 startVelocity = new Vector3(0f, 0f, 6f);
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = startVelocity;
#else
            body.velocity = startVelocity;
#endif
            Vector3 expectedPosition = PortalMath.MapPoint(entryObject.transform, exitObject.transform, startPosition);
            Vector3 expectedVelocity = PortalMath.MapDirection(entryObject.transform, exitObject.transform, startVelocity) * 0.5f;

            traveller.Teleport(entry, exit);

            Assert.That(Vector3.Distance(travellerObject.transform.position, expectedPosition), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(travellerObject.transform.localScale, Vector3.one * 0.5f), Is.LessThan(0.001f));
#if UNITY_6000_0_OR_NEWER
            Assert.That(Vector3.Distance(body.linearVelocity, expectedVelocity), Is.LessThan(0.001f));
#else
            Assert.That(Vector3.Distance(body.velocity, expectedVelocity), Is.LessThan(0.001f));
#endif

            Object.Destroy(travellerObject);
            Object.Destroy(entryObject);
            Object.Destroy(exitObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TransitionClipping_KeepsComplementaryPortalSides()
        {
            GameObject entryObject = new GameObject("Entry Portal");
            GameObject exitObject = new GameObject("Exit Portal");
            entryObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            exitObject.transform.SetPositionAndRotation(Vector3.forward * 10f, Quaternion.identity);

            Portal entry = entryObject.AddComponent<Portal>();
            Portal exit = exitObject.AddComponent<Portal>();
            entry.LinkedPortal = exit;
            exit.LinkedPortal = entry;

            GameObject travellerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            travellerObject.name = "Clipped Traveller";
            travellerObject.transform.position = Vector3.forward * 0.25f;
            PortalTraveller traveller = travellerObject.AddComponent<PortalTraveller>();
            yield return null;

            travellerObject.SendMessage("EnterPortal", entry, SendMessageOptions.RequireReceiver);
            yield return null;

            GameObject cloneObject = GameObject.Find("Clipped Traveller (Portal Transition)");
            Assert.That(cloneObject, Is.Not.Null);

            int clipPlaneId = Shader.PropertyToID("_PortalClipPlane");
            int clipEnabledId = Shader.PropertyToID("_PortalClipEnabled");
            MaterialPropertyBlock sourceProperties = new MaterialPropertyBlock();
            MaterialPropertyBlock cloneProperties = new MaterialPropertyBlock();
            travellerObject.GetComponent<Renderer>().GetPropertyBlock(sourceProperties);
            cloneObject.GetComponent<Renderer>().GetPropertyBlock(cloneProperties);

            Vector3 sourceNormal = sourceProperties.GetVector(clipPlaneId);
            Vector3 destinationNormal = cloneProperties.GetVector(clipPlaneId);
            Assert.That(sourceProperties.GetFloat(clipEnabledId), Is.EqualTo(1f));
            Assert.That(cloneProperties.GetFloat(clipEnabledId), Is.EqualTo(1f));
            Assert.That(Vector3.Dot(sourceNormal, entry.transform.forward), Is.GreaterThan(0.99f));
            Assert.That(Vector3.Dot(destinationNormal, exit.transform.forward), Is.GreaterThan(0.99f));

            Object.Destroy(travellerObject);
            Object.Destroy(entryObject);
            Object.Destroy(exitObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PortalTrigger_OnlyArmsTraversalFromFront()
        {
            GameObject entryObject = new GameObject("Entry Portal");
            GameObject exitObject = new GameObject("Exit Portal");
            entryObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            exitObject.transform.SetPositionAndRotation(Vector3.forward * 10f, Quaternion.Euler(0f, 180f, 0f));

            Portal entry = entryObject.AddComponent<Portal>();
            Portal exit = exitObject.AddComponent<Portal>();
            entry.LinkedPortal = exit;
            exit.LinkedPortal = entry;

            GameObject travellerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            travellerObject.name = "Backside Traveller";
            travellerObject.transform.position = Vector3.back * 0.25f;
            PortalTraveller traveller = travellerObject.AddComponent<PortalTraveller>();
            yield return null;

            entryObject.SendMessage("OnTriggerEnter", travellerObject.GetComponent<Collider>(), SendMessageOptions.RequireReceiver);

            Assert.That(traveller.ActivePortal, Is.Null);

            entryObject.SendMessage("OnTriggerExit", travellerObject.GetComponent<Collider>(), SendMessageOptions.RequireReceiver);
            travellerObject.transform.position = Vector3.forward * 0.25f;
            entryObject.SendMessage("OnTriggerEnter", travellerObject.GetComponent<Collider>(), SendMessageOptions.RequireReceiver);

            Assert.That(traveller.ActivePortal, Is.SameAs(entry));

            Object.Destroy(travellerObject);
            Object.Destroy(entryObject);
            Object.Destroy(exitObject);
            yield return null;
        }
    }
}
