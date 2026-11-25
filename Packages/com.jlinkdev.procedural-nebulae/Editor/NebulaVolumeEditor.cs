using System;
using UnityEditor;
using UnityEngine;

namespace jlinkdev.ProceduralNebulae.Editor
{
    [CustomEditor(typeof(NebulaStyleProfile))]
    public sealed class NebulaStyleProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (DrawDefaultInspector())
            {
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
        }
    }

    [CustomEditor(typeof(NebulaVolume))]
    public sealed class NebulaVolumeEditor : UnityEditor.Editor
    {
        SerializedProperty Field(string name) => serializedObject.FindProperty(name);
        void Draw(string name) => EditorGUILayout.PropertyField(Field(name));

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Draw("style");
            var profile = Field("style").objectReferenceValue as NebulaStyleProfile;
            if (!profile)
            {
                foreach (string name in LocalAppearance) Draw(name);
            }
            Draw("width");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Variation",EditorStyles.boldLabel);
            DrawSeed("Form seed","seed","overrideFormSeed",profile,profile ? profile.defaultFormSeed : 0);
            DrawSeed("Detail seed","detailSeed","overrideDetailSeed",profile,profile ? profile.defaultDetailSeed : 0);
            if (profile) Draw("variationStrength");
            Draw("varyStarPositions");
            if (Field("varyStarPositions").boolValue)
            {
                DrawSeed("Star seed","starSeed",null,null,0);
                Draw("starOffsetRadius");
            }
            Draw("clearing");
            Draw("clearingRadius");
            Draw("clearingSoftness");
            Draw("star");
            Draw("companion");
            Draw("raySteps");
            Draw("halfResolution");
            if (serializedObject.ApplyModifiedProperties())
            {
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
        }

        static readonly string[] LocalAppearance =
        {
            "banksAcross","coverage","cloudBody","emissionCavity","wisps","extinction",
            "shadowFill","litDust","outerGas","innerGas","gasBrightness"
        };

        void DrawSeed(string label,string field,string overrideField,NebulaStyleProfile profile,int defaultSeed)
        {
            var seed = Field(field);
            var local = profile ? Field(overrideField) : null;
            int current = local != null && !local.boolValue ? defaultSeed : seed.intValue;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                int edited = EditorGUILayout.IntField(label,current);
                if (EditorGUI.EndChangeCheck())
                {
                    seed.intValue = edited;
                    if (local != null) local.boolValue = true;
                }
                if (GUILayout.Button("Reroll",GUILayout.Width(52)))
                {
                    seed.intValue = Guid.NewGuid().GetHashCode();
                    if (local != null) local.boolValue = true;
                }
                if (local != null)
                {
                    using (new EditorGUI.DisabledScope(!local.boolValue))
                        if (GUILayout.Button("Reset",GUILayout.Width(45))) local.boolValue = false;
                }
            }
        }
    }
}
