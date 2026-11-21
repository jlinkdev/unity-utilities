using NUnit.Framework;
using UnityEngine;

namespace jlinkdev.UnityUtilities.WaterSystem.Tests
{
    public sealed class WaterShaderResourceTests
    {
        [TestCase("WaterSurface", "jlinkdev/Water System/Surface")]
        [TestCase("WaterBoundary", "Hidden/jlinkdev/Water System/Boundary")]
        [TestCase("WaterComposite", "Hidden/jlinkdev/Water System/Composite")]
        [TestCase("WaterParticles", "jlinkdev/Water System/Particles")]
        public void RequiredShader_IsPackagedAsRuntimeResource(string resource, string shaderName)
        {
            Shader shader = Resources.Load<Shader>(resource);
            Assert.That(shader, Is.Not.Null, resource + " must survive player resource inclusion.");
            Assert.That(shader.name, Is.EqualTo(shaderName));
        }
    }
}
