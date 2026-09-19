using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.VolumetricRain.Demo
{
    /// <summary>Opt-in pipeline override for the laboratory, active only in Play mode.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-10000)]
    [AddComponentMenu("jlinkdev/Volumetric Rain/Demo/Rain Demo Pipeline")]
    public sealed class RainDemoPipeline : MonoBehaviour
    {
        [SerializeField, Tooltip("Temporarily replaces global Graphics and active Quality pipelines while this demo runs.")]
        private UniversalRenderPipelineAsset pipeline;
        public UniversalRenderPipelineAsset Pipeline { get => pipeline; set => pipeline = value; }

        private static RainDemoPipeline owner;
        private RenderPipelineAsset previousGraphics, previousQuality, appliedPipeline;
        private int qualityLevel, previousAntiAliasing;
        private bool applied;

        private void OnEnable()
        {
            if (!Application.isPlaying || applied || pipeline == null) return;
            if (owner != null && owner != this)
            {
                Debug.LogWarning("Another Rain Demo Pipeline is already active. Run one rain laboratory at a time.", this);
                return;
            }
            owner = this;
            previousGraphics = GraphicsSettings.defaultRenderPipeline;
            previousQuality = QualitySettings.renderPipeline;
            qualityLevel = QualitySettings.GetQualityLevel();
            previousAntiAliasing = QualitySettings.antiAliasing;
            appliedPipeline = pipeline;
            applied = true;
            GraphicsSettings.defaultRenderPipeline = appliedPipeline;
            QualitySettings.renderPipeline = appliedPipeline;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
        }

        private void OnDisable() => Restore();
        private void OnDestroy() => Restore();
        private void OnApplicationQuit() => Restore();

        private void Restore()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
#endif
            if (!applied) return;
            applied = false;
            // Do not overwrite a pipeline deliberately assigned by another system during the demo.
            if (GraphicsSettings.defaultRenderPipeline == appliedPipeline)
                GraphicsSettings.defaultRenderPipeline = previousGraphics;
            int currentLevel = QualitySettings.GetQualityLevel();
            // Unity exposes a getter but no setter for another level's pipeline override.
            // Switch without expensive changes, restore the affected level, then return to the user's level.
            if (currentLevel != qualityLevel) QualitySettings.SetQualityLevel(qualityLevel, false);
            if (QualitySettings.renderPipeline == appliedPipeline)
            {
                QualitySettings.renderPipeline = previousQuality;
                QualitySettings.antiAliasing = previousAntiAliasing;
            }
            if (currentLevel != qualityLevel) QualitySettings.SetQualityLevel(currentLevel, false);
            if (owner == this) owner = null;
        }

#if UNITY_EDITOR
        private void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) Restore();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ActivateWithoutSceneReload()
        {
            // OnEnable is not guaranteed on repeated entries with both editor reload options disabled.
            foreach (var demo in FindObjectsByType<RainDemoPipeline>(FindObjectsSortMode.None))
                if (demo.isActiveAndEnabled) demo.OnEnable();
        }
    }
}
