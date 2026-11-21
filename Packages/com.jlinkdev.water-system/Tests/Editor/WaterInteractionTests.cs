using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace jlinkdev.UnityUtilities.WaterSystem.Tests
{
    public sealed class WaterInteractionTests
    {
        private readonly List<Object> cleanup = new List<Object>();
        private WaterBody water;

        [SetUp]
        public void SetUp()
        {
            WaterProfile profile = ScriptableObject.CreateInstance<WaterProfile>();
            profile.Waves = new WaveSettings[0];
            cleanup.Add(profile);
            water = MakeObject("Test water").AddComponent<WaterBody>();
            water.Profile = profile;
            water.Size = new Vector2(20f, 20f);
            water.Depth = 5f;
            water.Resolution = 4;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = cleanup.Count - 1; i >= 0; i--) if (cleanup[i] != null) Object.DestroyImmediate(cleanup[i]);
            cleanup.Clear();
        }

        private GameObject MakeObject(string name)
        {
            GameObject item = new GameObject(name);
            cleanup.Add(item);
            return item;
        }

        [Test]
        public void BodyRegistry_TracksEnableDisableAndDestruction()
        {
            Assert.That(WaterBody.ActiveBodies, Has.Member(water));
            water.enabled = false;
            Assert.That(WaterBody.ActiveBodies, Has.No.Member(water));
            water.enabled = true;
            Assert.That(WaterBody.ActiveBodies, Has.Member(water));
            Object.DestroyImmediate(water.gameObject);
            Assert.That(WaterBody.ActiveBodies, Has.No.Member(water));
        }

        [Test]
        public void Volume_RejectsSidesAndBottomAndIncludesMovedBody()
        {
            water.transform.position = new Vector3(8f, 3f, -2f);
            Assert.That(water.Contains(new Vector3(8f, 2f, -2f), 0f), Is.True);
            Assert.That(water.Contains(new Vector3(8f, -2.01f, -2f), 0f), Is.False);
            Assert.That(water.Contains(new Vector3(18.01f, 2f, -2f), 0f), Is.False);
            Assert.That(water.Contains(new Vector3(8f, 3.01f, -2f), 0f), Is.False);
            Assert.That(water.TrySample(new Vector3(8f, 500f, -2f), 0f, out WaterSample sample), Is.True);
            Assert.That(sample.Height, Is.EqualTo(3f));
        }

        [Test]
        public void BodySample_MatchesActualGeneratedMeshTriangleTopology()
        {
            water.Profile.Waves = new[] { new WaveSettings(31f, 0.7f, 3.1f, 0.8f, 0.4f) };
            water.Size = new Vector2(4f, 6f);
            water.Resolution = 2;
            water.transform.position = new Vector3(6f, 2f, -4f);
            water.Rebuild();
            Vector3[] vertices = water.SurfaceMesh.vertices;
            int[] indices = water.SurfaceMesh.triangles;
            const float time = 0.42f;
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 a = WaterWaves.Evaluate(water.Profile, water.transform.position, water.transform.position + vertices[indices[i]], time).Position;
                Vector3 b = WaterWaves.Evaluate(water.Profile, water.transform.position, water.transform.position + vertices[indices[i + 1]], time).Position;
                Vector3 c = WaterWaves.Evaluate(water.Profile, water.transform.position, water.transform.position + vertices[indices[i + 2]], time).Position;
                Plane plane = new Plane(a, b, c);
                WaterSample sample = water.Sample((a + b + c) / 3f, time);
                Assert.That(Mathf.Abs(plane.GetDistanceToPoint(sample.Position)), Is.LessThan(0.00001f), "Triangle " + i / 3);
                Assert.That(Vector3.Dot(sample.Normal, plane.normal), Is.GreaterThan(0.99999f));
            }
        }

        [Test]
        public void Disable_RestoresPreexistingMeshInsteadOfDestroyingUserAsset()
        {
            water.enabled = false;
            Mesh userMesh = new Mesh { name = "User owned mesh" };
            cleanup.Add(userMesh);
            MeshFilter filter = water.GetComponent<MeshFilter>();
            filter.sharedMesh = userMesh;
            water.enabled = true;
            Assert.That(filter.sharedMesh, Is.Not.SameAs(userMesh));
            water.enabled = false;
            Assert.That(filter.sharedMesh, Is.SameAs(userMesh));
            Assert.That(userMesh != null, Is.True);
        }

        [Test]
        public void Disable_RestoresUserMaterialsAndRendererPropertyBlock()
        {
            water.enabled = false;
            Material userMaterial = new Material(Resources.Load<Shader>("WaterSurface"));
            cleanup.Add(userMaterial);
            MeshRenderer renderer = water.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { userMaterial, userMaterial };
            var original = new MaterialPropertyBlock();
            original.SetFloat("_WaterDensity", 0.4321f);
            renderer.SetPropertyBlock(original);
            water.enabled = true;
            Assert.That(renderer.sharedMaterial, Is.Not.SameAs(userMaterial));
            var runtime = new MaterialPropertyBlock();
            water.ApplyShaderProperties(runtime);
            renderer.SetPropertyBlock(runtime);
            water.enabled = false;
            Assert.That(renderer.sharedMaterials.Length, Is.EqualTo(2));
            Assert.That(renderer.sharedMaterials[0], Is.SameAs(userMaterial));
            Assert.That(renderer.sharedMaterials[1], Is.SameAs(userMaterial));
            var restored = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(restored);
            Assert.That(restored.GetFloat("_WaterDensity"), Is.EqualTo(0.4321f));
            Assert.That(userMaterial != null, Is.True);
        }

        [Test]
        public void MissingProfiles_CreateIndependentUsableDefaultsAndReleaseAfterDisabledQuery()
        {
            water.Profile = null;
            WaterBody other = MakeObject("Default profile water").AddComponent<WaterBody>();
            WaterProfile first = water.Profile;
            WaterProfile second = other.Profile;
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.SameAs(first));
            first.Waves = new WaveSettings[0];
            Assert.That(second.Waves.Length, Is.GreaterThan(0));
            Assert.That(water.Sample(Vector3.zero, 0f).Height, Is.EqualTo(water.transform.position.y));
            water.enabled = false;
            Assert.That(first == null, Is.True);
            WaterProfile queriedWhileDisabled = water.Profile;
            Assert.That(queriedWhileDisabled != null, Is.True);
            Object.DestroyImmediate(water.gameObject);
            Assert.That(queriedWhileDisabled == null, Is.True, "Destroy must release a fallback created after OnDisable.");
        }

        [Test]
        public void UnsupportedRotationAndScale_AreRejectedByVolumeQueries()
        {
            water.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            Assert.That(water.HasSupportedTransform, Is.False);
            Assert.That(water.ContainsXZ(Vector3.zero), Is.False);
            Assert.That(water.TrySample(Vector3.zero, 0f, out _), Is.False);
            water.transform.rotation = Quaternion.identity;
            water.transform.localScale = new Vector3(2f, 1f, 1f);
            Assert.That(water.HasSupportedTransform, Is.False);
            Assert.That(water.Contains(Vector3.down, 0f), Is.False);
            water.transform.localScale = Vector3.one;
            water.transform.position = new Vector3(6f, 2f, -4f);
            Assert.That(water.HasSupportedTransform, Is.True);
            Assert.That(water.Contains(water.transform.position - Vector3.up, 0f), Is.True);
        }

        [Test]
        public void Probe_HysteresisPreventsDuplicateEventsAndReportsCrossingVelocity()
        {
            WaterContactProbe probe = MakeObject("Probe").AddComponent<WaterContactProbe>();
            probe.BodyOverride = water;
            probe.Hysteresis = 0.025f;
            int entered = 0, exited = 0;
            WaterContact entry = default;
            probe.EnteredWater.AddListener(contact => { entered++; entry = contact; });
            probe.ExitedWater.AddListener(_ => exited++);
            probe.transform.position = Vector3.up;
            probe.Refresh();
            probe.transform.position = Vector3.down * 0.1f;
            probe.Refresh(0.1f);
            probe.Refresh(0.1f);
            Assert.That(entered, Is.EqualTo(1));
            Assert.That(entry.Body, Is.SameAs(water));
            Assert.That(entry.PointVelocity.y, Is.EqualTo(-11f).Within(0.0001f));
            probe.transform.position = Vector3.up * 0.01f;
            probe.Refresh();
            Assert.That(probe.IsUnderwater, Is.True);
            Assert.That(exited, Is.Zero);
            probe.transform.position = Vector3.up * 0.03f;
            probe.Refresh();
            probe.Refresh();
            Assert.That(probe.IsUnderwater, Is.False);
            Assert.That(exited, Is.EqualTo(1));
        }

        [Test]
        public void Probe_ExitsAtSidesAndBottom()
        {
            WaterContactProbe probe = MakeObject("Probe").AddComponent<WaterContactProbe>();
            probe.BodyOverride = water;
            int exited = 0;
            probe.ExitedWater.AddListener(_ => exited++);
            probe.transform.position = Vector3.down;
            probe.Refresh();
            probe.transform.position = new Vector3(11f, -1f, 0f);
            probe.Refresh();
            Assert.That(probe.IsUnderwater, Is.False);
            Assert.That(exited, Is.EqualTo(1));
            probe.transform.position = Vector3.down;
            probe.Refresh();
            probe.transform.position = Vector3.down * 6f;
            probe.Refresh();
            Assert.That(probe.IsUnderwater, Is.False);
            Assert.That(exited, Is.EqualTo(2));
        }

        [Test]
        public void AutomaticProbe_FindsLowerWaterVolumeInStackedPools()
        {
            WaterBody lower = MakeObject("Lower water").AddComponent<WaterBody>();
            lower.Profile = water.Profile;
            lower.transform.position = Vector3.down * 10f;
            lower.Size = water.Size;
            lower.Depth = 5f;
            WaterContactProbe probe = MakeObject("Probe").AddComponent<WaterContactProbe>();
            probe.transform.position = Vector3.down * 11f;
            probe.Refresh();
            Assert.That(probe.IsUnderwater, Is.True);
            Assert.That(probe.Contact.Body, Is.SameAs(lower));
        }

        [Test]
        public void Camera_DistinguishesNearPlaneIntersectionFromCentreCrossing()
        {
            GameObject cameraObject = MakeObject("Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.3f;
            camera.fieldOfView = 60f;
            WaterCamera controller = cameraObject.AddComponent<WaterCamera>();
            controller.BodyOverride = water;
            cameraObject.transform.position = Vector3.up;
            controller.RefreshState();
            Assert.That(controller.State, Is.EqualTo(WaterCameraState.Above));
            cameraObject.transform.position = Vector3.up * 0.05f;
            controller.RefreshState();
            Assert.That(controller.State, Is.EqualTo(WaterCameraState.Intersecting));
            Assert.That(controller.IsUnderwater, Is.False);
            cameraObject.transform.position = Vector3.down;
            controller.RefreshState();
            Assert.That(controller.State, Is.EqualTo(WaterCameraState.Submerged));
            Assert.That(controller.Submersion01, Is.EqualTo(1f));
            Assert.That(controller.IsUnderwater, Is.True);
            cameraObject.transform.position = Vector3.down * 6f;
            controller.RefreshState();
            Assert.That(controller.IsUnderwater, Is.False);
            Assert.That(controller.State, Is.EqualTo(WaterCameraState.Above));
        }
    }
}
