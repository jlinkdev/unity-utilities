using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain.Tests
{
    public class RainSurfaceImpactTests
    {
        [Test]
        public void FallingDropsProduceRingsAndSprayOnASolidFloor()
        {
            const string path = "Packages/com.jlinkdev.volumetric-fog-and-rain/Runtime/SurfaceImpacts/Rain Surface Impacts.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null);
            var emitter = Object.Instantiate(prefab, new Vector3(1200, 30, -1200), Quaternion.identity);
            var floor = new GameObject("Rain impact test floor");
            try
            {
                floor.layer = 31;
                floor.transform.position = emitter.transform.position - Vector3.up * .1f;
                var collider = floor.AddComponent<BoxCollider>();
                collider.size = new Vector3(12, .2f, 12);
                collider.enabled = false;

                var drops = emitter.GetComponent<ParticleSystem>();
                var collision = drops.collision;
                collision.collidesWith = 1 << floor.layer;
                Assert.That(collision.type, Is.EqualTo(ParticleSystemCollisionType.World));
                var children = drops.subEmitters;
                Assert.That(children.subEmittersCount, Is.EqualTo(2));
                var ring = children.GetSubEmitterSystem(0);
                var spray = children.GetSubEmitterSystem(1);
                Assert.That(ring, Is.Not.Null);
                Assert.That(spray, Is.Not.Null);
                Assert.That(children.GetSubEmitterType(0), Is.EqualTo(ParticleSystemSubEmitterType.Collision));
                Assert.That(children.GetSubEmitterType(1), Is.EqualTo(ParticleSystemSubEmitterType.Collision));

                drops.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                drops.useAutoRandomSeed = false;
                drops.randomSeed = 181;
                drops.Play(true);
                Physics.SyncTransforms();
                for (int i = 0; i < 60; i++) drops.Simulate(1f / 60, true, false, false);
                Assert.That(ring.particleCount + spray.particleCount, Is.Zero);

                collider.enabled = true;
                Physics.SyncTransforms();
                int peakRings = 0;
                int peakSpray = 0;
                for (int i = 0; i < 90; i++)
                {
                    drops.Simulate(1f / 60, true, false, false);
                    peakRings = Mathf.Max(peakRings, ring.particleCount);
                    peakSpray = Mathf.Max(peakSpray, spray.particleCount);
                }
                Assert.That(peakRings, Is.GreaterThan(0), "Collisions should emit expanding rings.");
                Assert.That(peakSpray, Is.GreaterThan(0), "The same collisions should emit airborne droplets.");
            }
            finally
            {
                Object.DestroyImmediate(emitter);
                Object.DestroyImmediate(floor);
            }
        }
    }
}
