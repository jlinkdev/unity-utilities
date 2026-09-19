using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using jlinkdev.UnityUtilities.VolumetricRain.Demo;

namespace jlinkdev.UnityUtilities.VolumetricRain.Tests
{
    public class RainDemoPipelineTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void DisableRestoresPipelinesAndPreservesSelectedQuality(bool changeQuality)
        {
            var previousGraphics = GraphicsSettings.defaultRenderPipeline;
            var previousQuality = QualitySettings.renderPipeline;
            int originalLevel = QualitySettings.GetQualityLevel();
            int originalAA = QualitySettings.antiAliasing;
            var demoPipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var go = new GameObject("Demo pipeline test");
            go.SetActive(false);
            var demo = go.AddComponent<RainDemoPipeline>();
            demo.Pipeline = demoPipeline;
            try
            {
                go.SetActive(true);
                Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(demoPipeline));
                Assert.That(QualitySettings.renderPipeline, Is.SameAs(demoPipeline));
                int selectedLevel = changeQuality ? (originalLevel + 1) % QualitySettings.names.Length : originalLevel;
                QualitySettings.SetQualityLevel(selectedLevel, false);
                demo.enabled = false;
                Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(previousGraphics));
                Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(selectedLevel));
                Assert.That(QualitySettings.GetRenderPipelineAssetAt(originalLevel), Is.SameAs(previousQuality));
                demo.enabled = true;
                Object.DestroyImmediate(go);
                Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(previousGraphics));
            }
            finally
            {
                if (go != null) Object.DestroyImmediate(go);
                GraphicsSettings.defaultRenderPipeline = previousGraphics;
                QualitySettings.SetQualityLevel(originalLevel, false);
                QualitySettings.renderPipeline = previousQuality;
                QualitySettings.antiAliasing = originalAA;
                Object.DestroyImmediate(demoPipeline);
            }
        }

        [Test]
        public void MissingPipelineLeavesSettingsUntouched()
        {
            var graphics = GraphicsSettings.defaultRenderPipeline;
            var quality = QualitySettings.renderPipeline;
            var go = new GameObject("Unconfigured demo", typeof(RainDemoPipeline));
            try
            {
                Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(graphics));
                Assert.That(QualitySettings.renderPipeline, Is.SameAs(quality));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
