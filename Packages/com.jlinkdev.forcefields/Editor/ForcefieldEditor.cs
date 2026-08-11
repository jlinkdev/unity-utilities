using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.Forcefields.Editor
{
    [CustomEditor(typeof(Forcefield))]
    [CanEditMultipleObjects]
    internal sealed class ForcefieldEditor : UnityEditor.Editor
    {
        private SerializedProperty targetRenderers;
        private SerializedProperty preset;
        private SerializedProperty propagationMode;
        private SerializedProperty intensity;
        private SerializedProperty impactCapacity;
        private SerializedProperty defaultImpactRadius;
        private SerializedProperty drawGizmos;

        private void OnEnable()
        {
            targetRenderers = serializedObject.FindProperty("targetRenderers");
            preset = serializedObject.FindProperty("preset");
            propagationMode = serializedObject.FindProperty("propagationMode");
            intensity = serializedObject.FindProperty("intensity");
            impactCapacity = serializedObject.FindProperty("impactCapacity");
            defaultImpactRadius = serializedObject.FindProperty("defaultImpactRadius");
            drawGizmos = serializedObject.FindProperty("drawGizmos");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(targetRenderers);
            EditorGUILayout.PropertyField(preset);
            EditorGUILayout.PropertyField(propagationMode);
            EditorGUILayout.PropertyField(intensity);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(impactCapacity);
            EditorGUILayout.PropertyField(defaultImpactRadius);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(drawGizmos);

            serializedObject.ApplyModifiedProperties();

            if (targets.Length != 1)
                return;

            Forcefield forcefield = (Forcefield)target;
            DrawDiagnostics(forcefield);
            DrawTools(forcefield);
        }

        private static void DrawDiagnostics(Forcefield forcefield)
        {
            Renderer[] renderers = forcefield.TargetRenderers;
            if (renderers == null || renderers.Length == 0)
            {
                EditorGUILayout.HelpBox("Assign at least one renderer for the effect.", MessageType.Error);
                return;
            }

            bool missingRenderer = false;
            bool missingShader = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    missingRenderer = true;
                else if (!forcefield.UsesForcefieldShader(renderers[i]))
                    missingShader = true;
            }

            if (missingRenderer)
                EditorGUILayout.HelpBox("The renderer list contains a missing reference.", MessageType.Warning);
            if (missingShader)
                EditorGUILayout.HelpBox("One or more renderers do not use the supplied jlinkdev Forcefield shader.", MessageType.Warning);

            UniversalRenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null)
            {
                EditorGUILayout.HelpBox("The forcefield shader requires the Universal Render Pipeline.", MessageType.Error);
                return;
            }

            if (!pipeline.supportsCameraOpaqueTexture)
                EditorGUILayout.HelpBox("Opaque Texture is disabled on the active URP asset. Refraction will fall back to transparent energy rendering.", MessageType.Info);
            if (!pipeline.supportsCameraDepthTexture)
                EditorGUILayout.HelpBox("Depth Texture is disabled on the active URP asset. Intersection glow will be unavailable.", MessageType.Info);
        }

        private static void DrawTools(Forcefield forcefield)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                string.Format("Active impacts: {0} / {1}", forcefield.ActiveImpactCount, forcefield.ImpactCapacity),
                EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Test Impact"))
                {
                    Renderer previewRenderer = FirstRenderer(forcefield.TargetRenderers);
                    if (previewRenderer != null)
                    {
                        Bounds bounds = previewRenderer.bounds;
                        Vector3 point = bounds.center + Vector3.up * bounds.extents.y;
                        forcefield.AddImpact(point, Vector3.up, 1f, 0.04f);
                        SceneView.RepaintAll();
                    }
                }

                if (GUILayout.Button("Clear Impacts"))
                {
                    forcefield.ClearImpacts();
                    SceneView.RepaintAll();
                }
            }

            if (GUILayout.Button("Find Child Renderers"))
            {
                Undo.RecordObject(forcefield, "Find Forcefield Renderers");
                forcefield.TargetRenderers = forcefield.GetComponentsInChildren<Renderer>(true);
                EditorUtility.SetDirty(forcefield);
            }
        }

        private static Renderer FirstRenderer(Renderer[] renderers)
        {
            if (renderers == null)
                return null;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    return renderers[i];
            }

            return null;
        }
    }
}
