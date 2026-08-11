using NUnit.Framework;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams.Tests
{
    public sealed class BeamRuntimeIntegrationTests
    {
        [Test]
        public void RaycastEndpoint_ReportsSurfaceMetadata()
        {
            GameObject endpointObject = new GameObject("Raycast Endpoint");
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                obstacle.transform.position = Vector3.forward * 3f;
                obstacle.layer = 31;
                Physics.SyncTransforms();
                RaycastBeamEndpoint endpointProvider = endpointObject.AddComponent<RaycastBeamEndpoint>();
                endpointProvider.MaximumDistance = 10f;
                endpointProvider.LayerMask = 1 << 31;

                bool valid = endpointProvider.TryGetEndpoint(out BeamEndpoint endpoint);

                Assert.That(valid, Is.True);
                Assert.That(endpoint.HasSurface, Is.True);
                Assert.That(endpoint.SurfaceCollider, Is.EqualTo(obstacle.GetComponent<Collider>()));
                Assert.That(endpoint.Position.z, Is.EqualTo(2.5f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(endpointObject);
                Object.DestroyImmediate(obstacle);
            }
        }

        [Test]
        public void PhysicsContacts_PublishesEnterAndExit()
        {
            GameObject root = new GameObject("Contact Beam");
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                Beam beam = CreateBeam(root, 5f);
                obstacle.transform.position = Vector3.forward * 2.5f;
                obstacle.layer = 31;
                Physics.SyncTransforms();
                BeamPhysicsContacts contacts = root.AddComponent<BeamPhysicsContacts>();
                contacts.Beam = beam;
                contacts.LayerMask = 1 << 31;
                contacts.ClearContacts(false);
                int entered = 0;
                int exited = 0;
                contacts.ContactEntered += (_, contact) => entered++;
                contacts.ContactExited += (_, contact) => exited++;

                contacts.QueryNow();
                obstacle.transform.position = Vector3.right * 20f;
                Physics.SyncTransforms();
                contacts.QueryNow();

                Assert.That(entered, Is.EqualTo(1));
                Assert.That(exited, Is.EqualTo(1));
                Assert.That(contacts.ContactCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(obstacle);
            }
        }

        [Test]
        public void RibbonRenderer_EmitsDocumentedVertexStreams()
        {
            GameObject root = new GameObject("Ribbon Test");
            try
            {
                BeamRibbonRenderer renderer = root.AddComponent<BeamRibbonRenderer>();
                BeamPathBuffer buffer = new BeamPathBuffer();
                BeamStrand strand = buffer.AddStrand(seed: 5);
                strand.Add(Vector3.zero);
                strand.Add(Vector3.forward * 2f);
                BeamPathUtility.RecalculateMetrics(strand, Vector3.up);

                renderer.Render(buffer, new BeamRenderContext(1f, 0.016f, 0.5f, 5));

                Mesh mesh = root.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh, Is.Not.Null);
                Assert.That(mesh.vertexCount, Is.EqualTo(4));
                Assert.That(mesh.triangles.Length, Is.EqualTo(6));
                Assert.That(mesh.uv.Length, Is.EqualTo(4));
                Assert.That(mesh.uv2.Length, Is.EqualTo(4));
                Assert.That(mesh.uv3.Length, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Beam CreateBeam(GameObject root, float length)
        {
            GameObject targetObject = new GameObject("Target");
            targetObject.transform.SetParent(root.transform, false);
            targetObject.transform.localPosition = Vector3.forward * length;
            TransformBeamEndpoint target = targetObject.AddComponent<TransformBeamEndpoint>();
            StraightBeamPath path = root.AddComponent<StraightBeamPath>();
            BeamRibbonRenderer renderer = root.AddComponent<BeamRibbonRenderer>();
            Beam beam = root.AddComponent<Beam>();
            beam.Target = target;
            beam.PathProvider = path;
            beam.PathRenderer = renderer;
            beam.Modifiers = new BeamPathModifier[0];
            beam.Refresh();
            return beam;
        }
    }
}
