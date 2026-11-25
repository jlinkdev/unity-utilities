using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace JLink.ProceduralTextures.Editor
{
    [CustomEditor(typeof(ProceduralTextureImporter))]
    public sealed class ProceduralTextureImporterEditor : ScriptedImporterEditor
    {
        protected override Type extraDataType => typeof(TextureRecipeDraft);
        // Recipes have an explicit Apply workflow even when an editor is hosted outside the main Inspector.
        protected override bool needsApplyRevert => true;
        TextureRecipeDraft Current => (TextureRecipeDraft)extraDataTarget;
        readonly TextureInspectorPreview preview = new TextureInspectorPreview();
        bool outputExpanded;
        bool processingExpanded;
        readonly Dictionary<string, string> drafts = new Dictionary<string, string>();
        readonly AdvancedDropdownState pickerState = new AdvancedDropdownState();

        public override bool RequiresConstantRepaint() => preview.Animating;

        protected override void InitializeExtraDataInstance(UnityEngine.Object extraTarget, int targetIndex)
        {
            var draft = (TextureRecipeDraft)extraTarget;
            try
            {
                draft.recipe = ProceduralTextureImporter.Read(((AssetImporter)targets[targetIndex]).assetPath);
                draft.recipe.colorRamp ??= draft.recipe.CreateEndpointRamp();
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
            preview.Draw(Current.recipe, fields.contentHash, problem == null, Repaint);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Generator settings", EditorStyles.boldLabel);
                if (GUILayout.Button(new GUIContent("⋮", "Recipe actions"), EditorStyles.miniButton, GUILayout.Width(24)))
                    ShowRecipeActions();
            }
            DrawFields(fields, TextureCatalog.Find(Current.recipe.generator).Fields);
            processingExpanded = EditorGUILayout.Foldout(processingExpanded, "Processing", true);
            if (processingExpanded) DrawFields(fields, TextureCatalog.ProcessingFields);
            outputExpanded = EditorGUILayout.Foldout(outputExpanded, "Output and remapping", true);
            if (outputExpanded) DrawFields(fields, TextureCatalog.OutputFields);
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
            string selected = TextureCatalog.Find(Current.recipe.generator).Menu;
            if (EditorGUI.DropdownButton(field, new GUIContent(selected.Replace("/", " › ")), FocusType.Keyboard))
                new TexturePicker(pickerState, SelectGenerator).Show(field);
        }

        sealed class TexturePicker : AdvancedDropdown
        {
            readonly Action<string> choose;
            readonly Dictionary<AdvancedDropdownItem, string> ids = new Dictionary<AdvancedDropdownItem, string>();

            internal TexturePicker(AdvancedDropdownState state, Action<string> choose) : base(state)
            {
                this.choose = choose;
                minimumSize = new Vector2(240, 300);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem("Texture generator");
                foreach (var family in TextureCatalog.All.GroupBy(item => item.Menu.Split('/')[0]))
                {
                    var group = new AdvancedDropdownItem(family.Key);
                    root.AddChild(group);
                    foreach (var definition in family)
                    {
                        var item = new AdvancedDropdownItem(definition.Menu.Split('/')[1]);
                        ids[item] = definition.Id;
                        group.AddChild(item);
                    }
                }
                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (ids.TryGetValue(item, out var id)) choose(id);
            }
        }

        internal void SelectGenerator(string next)
        {
            if (next == Current.recipe.generator) return;
            TextureCatalog.Find(next);
            var previous = Current.recipe;
            drafts[previous.generator] = JsonUtility.ToJson(previous);
            var selected = new TextureRecipe();
            if (drafts.TryGetValue(next, out var saved)) JsonUtility.FromJsonOverwrite(saved, selected);
            else if (next == "white-noise")
            {
                selected.cellsX = Mathf.Clamp(previous.width, 1, 512);
                selected.cellsY = Mathf.Clamp(previous.height, 1, 512);
            }
            CopyShared(previous, selected);
            selected.generator = next;
            SetDraft(selected, "Change texture generator");
        }

        static void CopyShared(TextureRecipe from, TextureRecipe to)
        {
            var source = JObject.Parse(JsonUtility.ToJson(from));
            var shared = new JObject();
            foreach (string field in TextureCatalog.SharedFields) shared.Add(field, source[field]);
            JsonUtility.FromJsonOverwrite(shared.ToString(), to);
        }

        void SetDraft(TextureRecipe value, string action)
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
            var defaults = new TextureRecipe { generator = Current.recipe.generator };
            if (keepShared) CopyShared(Current.recipe, defaults);
            if (defaults.generator == "white-noise")
            {
                defaults.cellsX = Mathf.Clamp(defaults.width, 1, 512);
                defaults.cellsY = Mathf.Clamp(defaults.height, 1, 512);
            }
            defaults.colorRamp ??= defaults.CreateEndpointRamp();
            SetDraft(defaults, "Reset recipe settings");
        }

        void LoadSettings()
        {
            string path = EditorUtility.OpenFilePanel("Use settings from recipe", Path.GetDirectoryName(((AssetImporter)target).assetPath), "proctex");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                UseSettings(path);
            }
            catch (Exception error) { EditorUtility.DisplayDialog("Could not load recipe", error.Message, "OK"); }
        }

        void UseSettings(string path)
        {
            var loaded = ProceduralTextureImporter.Read(path);
            loaded.colorRamp ??= loaded.CreateEndpointRamp();
            drafts.Clear();
            SetDraft(loaded, "Use settings from recipe");
        }

        void DrawFields(SerializedProperty recipe, IReadOnlyList<string> names)
        {
            foreach (string name in names)
            {
                if (!Current.recipe.UsesField(name)) continue;
                var property = recipe.FindPropertyRelative(name);
                var label = new GUIContent(FieldLabel(name), property.tooltip);
                if (name == "seed" || name == "warpSeed")
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(property, label);
                        if (GUILayout.Button(new GUIContent("Roll", "Choose a new seed for this draft"), EditorStyles.miniButton, GUILayout.Width(38)))
                            property.intValue = Guid.NewGuid().GetHashCode();
                    }
                }
                else EditorGUILayout.PropertyField(property, label, true);
            }
        }

        string FieldLabel(string name)
        {
            switch (name)
            {
                case "cellsX": return "Cells across";
                case "cellsY": return "Cells down";
                case "alongU": return "Along U";
                case "sizeX": return Current.recipe.generator == "linear" || Current.recipe.generator == "curve-gradient"
                    ? "Transition width" : "Size";
                case "sizeY": return "Vertical size";
                case "seamlessX": return "Seamless U";
                case "seamlessY": return "Seamless V";
                case "rotation": return "Rotation (degrees)";
                case "coordinateRotation": return "Coordinate rotation (degrees)";
                case "blurAngle": return "Blur angle (degrees)";
                case "warpFrequency": return "Warp cells per axis";
                case "warpAmount": return "Displacement";
                case "sweep": return "Arc angle (degrees)";
                case "profile": return "Gradient curve";
                case "colorRamp": return "Gradient";
                case "fitTexelEndpoints": return "Fit to texel endpoints";
                case "signedDistance": return "Encode signed distance";
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
            ProceduralTextureImporter.Write(path, Current.recipe, false);
            base.Apply();
            drafts.Clear();
        }

        void SaveCopy()
        {
            string source = ((AssetImporter)target).assetPath;
            string path = EditorUtility.SaveFilePanelInProject("Save recipe copy", Path.GetFileNameWithoutExtension(source) + " Copy", "proctex", "Choose a new recipe file.");
            if (string.IsNullOrEmpty(path)) return;
            if (File.Exists(path))
            {
                EditorUtility.DisplayDialog("Choose a new file", "A copy needs its own filename.", "OK");
                return;
            }
            ProceduralTextureImporter.Write(path, Current.recipe, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (AssetDatabase.LoadMainAssetAtPath(path) == null) return;
            DiscardChanges();
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
            GUIUtility.ExitGUI();
        }

        public override void OnDisable()
        {
            preview.Dispose();
            base.OnDisable();
        }
    }
}
