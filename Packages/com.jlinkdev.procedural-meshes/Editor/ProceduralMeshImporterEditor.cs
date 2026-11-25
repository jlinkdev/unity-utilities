using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace JLink.ProceduralMeshes.Editor
{
    [CustomEditor(typeof(ProceduralMeshImporter))]
    public sealed class ProceduralMeshImporterEditor : ScriptedImporterEditor
    {
        protected override Type extraDataType => typeof(MeshRecipeDraft);
        // Recipes have an explicit Apply workflow even when an editor is hosted outside the main Inspector.
        protected override bool needsApplyRevert => true;
        MeshRecipeDraft Current => (MeshRecipeDraft)extraDataTarget;
        Mesh preview;
        uint previewKey;
        double nextBake;
        bool boundsExpanded;
        long triangleCount;
        bool outputExpanded;
        readonly Dictionary<string, string> drafts = new Dictionary<string, string>();
        readonly AdvancedDropdownState pickerState = new AdvancedDropdownState();
        MeshPreview meshPreview;

        protected override void InitializeExtraDataInstance(UnityEngine.Object extraTarget, int targetIndex)
        {
            var draft = (MeshRecipeDraft)extraTarget;
            try
            {
                draft.recipe = ProceduralMeshImporter.Read(((AssetImporter)targets[targetIndex]).assetPath);
                draft.sourceError = "";
            }
            catch (Exception error)
            {
                draft.sourceError = error.Message;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            extraDataSerializedObject.Update();
            if (!string.IsNullOrEmpty(Current.sourceError))
            {
                EditorGUILayout.HelpBox(Current.sourceError, MessageType.Error);
                ApplyRevertGUI();
                return;
            }

            DrawSelector();
            var fields = extraDataSerializedObject.FindProperty("recipe");
            var problem = ValidationError();
            if (problem == null && Event.current.type == EventType.Layout) UpdatePreview(fields.contentHash);
            if (preview != null) DrawPreview();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);
                if (GUILayout.Button(new GUIContent("⋮", "Recipe actions"), EditorStyles.miniButton, GUILayout.Width(24)))
                    ShowRecipeActions();
            }
            DrawFields(fields, MeshCatalog.Find(Current.recipe.generator).Fields);
            outputExpanded = EditorGUILayout.Foldout(outputExpanded, "Placement, UV and vertex data", true);
            if (outputExpanded) DrawFields(fields, MeshCatalog.SharedFields);
            extraDataSerializedObject.ApplyModifiedProperties();
            problem = ValidationError();
            if (problem != null) EditorGUILayout.HelpBox(problem + " Preview paused until the draft is valid.", MessageType.Error);
            bool changed = HasModified();
            EditorGUILayout.LabelField(changed ? "Unapplied changes" : "Applied", EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(!changed || problem != null))
                if (GUILayout.Button("Apply as Copy…")) SaveCopy();
            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }

        void DrawSelector()
        {
            Rect field = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(), new GUIContent("Generator"));
            if (EditorGUI.DropdownButton(field, new GUIContent(MeshCatalog.Find(Current.recipe.generator).Menu), FocusType.Keyboard))
                new MeshPicker(pickerState, SelectGenerator).Show(field);
        }

        sealed class MeshPicker : AdvancedDropdown
        {
            readonly Action<string> select;

            internal MeshPicker(AdvancedDropdownState state, Action<string> select) : base(state)
            {
                this.select = select;
                minimumSize = new Vector2(220, 275);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem("Mesh generator");
                for (int i = 0; i < MeshCatalog.All.Count; i++)
                    root.AddChild(new AdvancedDropdownItem(MeshCatalog.All[i].Menu) { id = i });
                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item) => select(MeshCatalog.All[item.id].Id);
        }

        internal void SelectGenerator(string next)
        {
            if (next == Current.recipe.generator) return;
            MeshCatalog.Find(next);
            var previous = Current.recipe;
            drafts[previous.generator] = JsonUtility.ToJson(previous);
            var selected = new MeshRecipe();
            if (drafts.TryGetValue(next, out var saved)) JsonUtility.FromJsonOverwrite(saved, selected);
            CopyShared(previous, selected);
            selected.generator = next;
            SetDraft(selected, "Change mesh generator");
        }

        static void CopyShared(MeshRecipe from, MeshRecipe to)
        {
            var source = JObject.Parse(JsonUtility.ToJson(from));
            var shared = new JObject();
            foreach (string field in MeshCatalog.SharedFields) shared.Add(field, source[field]);
            JsonUtility.FromJsonOverwrite(shared.ToString(), to);
        }

        void SetDraft(MeshRecipe value, string action)
        {
            Undo.RecordObject(Current, action);
            Current.recipe = value;
            EditorUtility.SetDirty(Current);
            extraDataSerializedObject.Update();
            Repaint();
        }

        void ShowRecipeActions()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Use settings from recipe…"), false, LoadSettings);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Reset generator settings"), false, () => ResetSettings(true));
            menu.AddItem(new GUIContent("Reset all settings"), false, () => ResetSettings(false));
            menu.ShowAsContext();
        }

        void ResetSettings(bool keepShared)
        {
            var defaults = new MeshRecipe { generator = Current.recipe.generator };
            if (keepShared) CopyShared(Current.recipe, defaults);
            SetDraft(defaults, "Reset recipe settings");
        }

        void LoadSettings()
        {
            string path = EditorUtility.OpenFilePanel("Use settings from recipe", Path.GetDirectoryName(((AssetImporter)target).assetPath), "procmesh");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                UseSettings(path);
            }
            catch (Exception error) { EditorUtility.DisplayDialog("Could not load recipe", error.Message, "OK"); }
        }

        void UseSettings(string path)
        {
            var loaded = ProceduralMeshImporter.Read(path);
            drafts.Clear();
            SetDraft(loaded, "Use settings from recipe");
        }

        void DrawFields(SerializedProperty recipe, IReadOnlyList<string> names)
        {
            foreach (string name in names)
            {
                if (name == "closeCut" && Current.recipe.sweep >= 360f) continue;
                if (name == "separateEnds" && !Current.recipe.capEnds && !(Current.recipe.sweep < 360f && Current.recipe.closeCut)) continue;
                EditorGUILayout.PropertyField(recipe.FindPropertyRelative(name), new GUIContent(FieldLabel(name)), true);
            }
        }

        string FieldLabel(string name)
        {
            switch (name)
            {
                case "columns": return "Width segments";
                case "rows": return Current.recipe.generator == "ribbon" ? "Length segments" :
                    Current.recipe.generator == "box" ? "Height segments" : "Depth segments";
                case "rings": return Current.recipe.generator == "capsule" ? "Hemisphere segments" : "Latitude segments";
                case "sideUV": return "Side UV mapping";
                case "closeCut": return "Close radial cut";
                case "separateEnds": return "Separate closure material";
                case "sides": return "Radial segments";
                case "capEnds": return "Cap ends";
                case "heightInColorR": return "Normalized height → Color R";
                case "seamDegrees": return "Seam angle (degrees)";
                case "discUV": return "UV mapping";
                case "bend": return "Bend angle (degrees)";
                case "sweep": return "Sweep (degrees)";
                default: return ObjectNames.NicifyVariableName(name);
            }
        }

        public override void DiscardChanges()
        {
            drafts.Clear();
            base.DiscardChanges();
        }

        string ValidationError()
        {
            try { Current.recipe.Validate(); return null; }
            catch (ArgumentException error) { return error.Message; }
        }

        protected override bool CanApply() => string.IsNullOrEmpty(Current.sourceError) && ValidationError() == null;

        public override void SaveChanges()
        {
            if (CanApply()) base.SaveChanges();
        }

        protected override void Apply()
        {
            extraDataSerializedObject.ApplyModifiedProperties();
            string path = ((AssetImporter)target).assetPath;
            ProceduralMeshImporter.Write(path, Current.recipe, false);
            base.Apply();
            drafts.Clear();
        }

        void SaveCopy()
        {
            string source = ((AssetImporter)target).assetPath;
            string path = EditorUtility.SaveFilePanelInProject("Save recipe copy", Path.GetFileNameWithoutExtension(source) + " Copy", "procmesh", "Choose a new recipe file.");
            if (string.IsNullOrEmpty(path)) return;
            if (File.Exists(path))
            {
                EditorUtility.DisplayDialog("Choose a new file", "A copy needs its own filename.", "OK");
                return;
            }
            ProceduralMeshImporter.Write(path, Current.recipe, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (AssetDatabase.LoadMainAssetAtPath(path) == null) return;
            DiscardChanges();
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
            GUIUtility.ExitGUI();
        }

        void UpdatePreview(uint key)
        {
            if (preview != null && previewKey == key) return;
            if (preview && EditorApplication.timeSinceStartup < nextBake) { Repaint(); return; }
            var generated = MeshGenerator.Generate(Current.recipe);
            generated.hideFlags = HideFlags.HideAndDontSave;
            // Unity checks channel availability when constructing its viewer.
            if (preview && preview.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color) !=
                generated.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color))
            {
                meshPreview.Dispose();
                meshPreview = null;
            }
            if (meshPreview == null) meshPreview = new MeshPreview(generated);
            else meshPreview.mesh = generated;
            if (preview != null) DestroyImmediate(preview);
            preview = generated;
            previewKey = key;
            nextBake = EditorApplication.timeSinceStartup + 0.09;
            triangleCount = 0;
            for (int i = 0; i < preview.subMeshCount; i++) triangleCount += preview.GetIndexCount(i) / 3;
        }

        void DrawPreview()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                meshPreview.OnPreviewSettings();
            Rect area = GUILayoutUtility.GetRect(10f, 180f, GUILayout.ExpandWidth(true));
            meshPreview.OnPreviewGUI(area, EditorStyles.helpBox);
            string slots = preview.subMeshCount == 1 ? "1 submesh" : $"{preview.subMeshCount} submeshes";
            EditorGUILayout.LabelField($"{preview.vertexCount:N0} vertices · {triangleCount:N0} triangles · {slots}", EditorStyles.miniLabel);
            boundsExpanded = EditorGUILayout.Foldout(boundsExpanded, new GUIContent("Local bounds", "Front and top extents. The cross marks the local pivot."), true);
            if (boundsExpanded) MeshBoundsPreview.Draw(preview.bounds);
        }

        void ClearPreview()
        {
            meshPreview?.Dispose();
            meshPreview = null;
            if (preview != null) DestroyImmediate(preview);
        }

        public override void OnDisable()
        {
            ClearPreview();
            base.OnDisable();
        }
    }
}
