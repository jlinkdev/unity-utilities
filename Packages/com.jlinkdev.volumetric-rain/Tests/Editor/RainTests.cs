using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricRain.Tests
{
    public class RainTests
    {
        [Test]
        public void InspectorDistanceEditsDoNotRewriteOtherFields()
        {
            var profile = ScriptableObject.CreateInstance<RainProfile>();
            try
            {
                var serialized = new SerializedObject(profile);
                serialized.FindProperty("midDistance").floatValue = 35;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(profile.midDistance, Is.EqualTo(35));
                Assert.That(profile.farDistance, Is.EqualTo(30), "An intermediate edit must not change another authored value.");
                serialized.Update();
                serialized.FindProperty("farDistance").floatValue = 6;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(profile.farDistance, Is.EqualTo(6), "Invalid ordering is handled by the renderer, not destructive authoring validation.");
                serialized.Update();
                serialized.FindProperty("farDistance").floatValue = 60;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(profile.farDistance, Is.EqualTo(60));
                Assert.That(profile.maxDistance, Is.EqualTo(100));
            }
            finally { Object.DestroyImmediate(profile); }
        }
        [Test]
        public void InvalidGeometryAndDistancesAreMadeSafe()
        {
            var p = ScriptableObject.CreateInstance<RainProfile>();
            try
            {
                p.cellSize = -1; p.streakWidth = 10; p.streakLength = 100;
                p.nearFade = 10; p.midDistance = 1; p.farDistance = -1; p.maxDistance = 0;
                p.maxCellSteps = 0; p.hazeSteps = 100; p.density = -1;
                p.Sanitize();
                Assert.That(p.cellSize, Is.GreaterThanOrEqualTo(0.1f));
                Assert.That(p.streakWidth, Is.LessThanOrEqualTo(p.cellSize * 0.04f));
                Assert.That(p.streakLength, Is.LessThanOrEqualTo(p.cellSize * 1.5f));
                Assert.That(p.midDistance, Is.GreaterThan(p.nearFade));
                Assert.That(p.farDistance, Is.GreaterThan(p.midDistance));
                Assert.That(p.maxDistance, Is.GreaterThanOrEqualTo(p.farDistance));
                Assert.That(p.maxCellSteps, Is.EqualTo(16));
                Assert.That(p.hazeSteps, Is.EqualTo(32));
                Assert.That(p.density, Is.Zero);
            }
            finally { Object.DestroyImmediate(p); }
        }

        [Test]
        public void ZeroDirectionFallsDownAndWindIsVelocity()
        {
            var p = ScriptableObject.CreateInstance<RainProfile>();
            try
            {
                p.direction = Vector3.zero; p.fallSpeed = 10; p.wind = Vector3.right * 3;
                Assert.That(p.Velocity, Is.EqualTo(new Vector3(3, -10, 0)));
            }
            finally { Object.DestroyImmediate(p); }
        }

        [Test]
        public void ShaderImportsWithoutErrors()
        {
            var shader = Shader.Find("Hidden/jlinkdev/Volumetric Rain");
            Assert.That(shader, Is.Not.Null);
            foreach (var message in ShaderUtil.GetShaderMessages(shader))
                Assert.That(message.severity.ToString(), Is.Not.EqualTo("Error"), message.message);
        }

        [Test]
        public void FrozenClockDoesNotDependOnPlayMode()
        {
            var go = new GameObject("Rain Clock Test", typeof(Camera));
            try
            {
                var rain = go.AddComponent<VolumetricRainCamera>();
                rain.freezeTime = true; rain.fixedTime = 17.25f;
                Assert.That(rain.EvaluationTime, Is.EqualTo(17.25));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
