using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace jlinkdev.UnityUtilities.InteractiveFog.Editor
{
    [CustomPropertyDrawer(typeof(FogDensityAttribute))]
    sealed class FogDensityDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position,SerializedProperty property,GUIContent label)
            => EditorGUI.Slider(position,property,0,32,new GUIContent("Density",property.tooltip));
        public override UnityEngine.UIElements.VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var slider=new UnityEngine.UIElements.Slider("Density",0,32) { showInputField=true,tooltip=property.tooltip };
            slider.BindProperty(property);return slider;
        }
    }

    [CustomPropertyDrawer(typeof(FogPuffSettings))]
    sealed class FogBillowDrawer : PropertyDrawer
    {
        static readonly string[] Fields={"spacing","coverage","radiusRange","fieldSeed","growthRange","settlingRange","quietRange","density","mistAmount","spreadSpeed"};
        public override float GetPropertyHeight(SerializedProperty property,GUIContent label)
            => EditorGUIUtility.singleLineHeight+(property.isExpanded?Fields.Length*(EditorGUIUtility.singleLineHeight+2):0);
        public override void OnGUI(Rect rect,SerializedProperty property,GUIContent label)
        {
            EditorGUI.BeginProperty(rect,label,property);rect.height=EditorGUIUtility.singleLineHeight;
            property.isExpanded=EditorGUI.Foldout(rect,property.isExpanded,new GUIContent("Billows","Procedural bubble distribution, lifecycle and mist release."),true);
            if(property.isExpanded)
            {
                EditorGUI.indentLevel++;
                foreach(var name in Fields)
                {
                    rect.y+=rect.height+2;var child=property.FindPropertyRelative(name);
                    if(child.propertyType!=SerializedPropertyType.Vector2) { EditorGUI.PropertyField(rect,child);continue; }
                    string title=name=="radiusRange"?"Radius (m)":name=="growthRange"?"Growth (s)":name=="settlingRange"?"Settling (s)":"Quiet Interval (s)";
                    var valueRect=EditorGUI.PrefixLabel(rect,new GUIContent(title,child.tooltip));
                    int indent=EditorGUI.indentLevel;float labelWidth=EditorGUIUtility.labelWidth;
                    EditorGUI.indentLevel=0;EditorGUIUtility.labelWidth=30;valueRect.width=(valueRect.width-6)*.5f;
                    EditorGUI.PropertyField(valueRect,child.FindPropertyRelative("x"),new GUIContent("Min"));valueRect.x+=valueRect.width+6;
                    EditorGUI.PropertyField(valueRect,child.FindPropertyRelative("y"),new GUIContent("Max"));
                    EditorGUIUtility.labelWidth=labelWidth;EditorGUI.indentLevel=indent;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }
    }

    static class FogStyleGUI
    {
        internal static void Fields(SerializedObject owner,string prefix,params string[] names)
        { foreach(var name in names) EditorGUILayout.PropertyField(owner.FindProperty(prefix+name),true); }
        internal static void Draw(SerializedObject owner,string prefix)
        {
            var shape=owner.FindProperty(prefix+"densityShape");EditorGUILayout.PropertyField(shape,new GUIContent("Style Type"));
            if(shape.hasMultipleDifferentValues) EditorGUILayout.HelpBox("Select matching style types to edit shape-specific controls together.",MessageType.Info);
            EditorGUILayout.Space();EditorGUILayout.LabelField("Density and Recovery",EditorStyles.boldLabel);
            Fields(owner,prefix,"fogHeight","restRecovery","replenishment");
            if(!shape.hasMultipleDifferentValues && shape.intValue==(int)FogDensityShape.Cumulus) Fields(owner,prefix,"puffSize","cloudCoverage","plumeLift");
            EditorGUILayout.Space();EditorGUILayout.LabelField("Appearance",EditorStyles.boldLabel);
            Fields(owner,prefix,"extinction","albedo","ambient","anisotropy","detail","foldSize","wispDisplacement");
            if(!shape.hasMultipleDifferentValues && shape.intValue==(int)FogDensityShape.Cumulus) Fields(owner,prefix,"wispOpacity");
            EditorGUILayout.Space();EditorGUILayout.LabelField("Motion",EditorStyles.boldLabel);
            Fields(owner,prefix,"wind","vorticity","turbulence","eddySize","velocityDamping");
            if(!shape.hasMultipleDifferentValues && shape.intValue==(int)FogDensityShape.DryIce)
            { EditorGUILayout.Space();Fields(owner,prefix,"puffs");DrawBillowInfo(owner.FindProperty(prefix+"puffs")); }
        }
        static void DrawBillowInfo(SerializedProperty puffs)
        {
            var spacing=puffs.FindPropertyRelative("spacing");var range=puffs.FindPropertyRelative("radiusRange");
            if(!spacing.hasMultipleDifferentValues && !range.hasMultipleDifferentValues)
            {
                float effective=Mathf.Max(.2f,spacing.floatValue,Mathf.Clamp(Mathf.Max(range.vector2Value.x,range.vector2Value.y),.1f,2)*1.7f);
                EditorGUILayout.LabelField("Effective Spacing",$"{effective:0.##} m");
                if(effective>spacing.floatValue+.001f) EditorGUILayout.HelpBox("Spacing is limited by maximum radius. Reducing it further will not increase the bubble population.",MessageType.Info);
            }
            var coverage=puffs.FindPropertyRelative("coverage");var density=puffs.FindPropertyRelative("density");
            if(!coverage.hasMultipleDifferentValues && coverage.floatValue<=0) EditorGUILayout.HelpBox("Coverage is zero: no billows or new billow mist are generated.",MessageType.Info);
            else if(!density.hasMultipleDifferentValues && density.floatValue<=0) EditorGUILayout.HelpBox("Billow Density is zero: bodies are hidden, but Mist Amount can still release mist.",MessageType.Info);
        }
    }

    [CustomEditor(typeof(FogStyleProfile)),CanEditMultipleObjects]
    sealed class FogStyleProfileInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if(!serializedObject.isEditingMultipleObjects)
            {
                int users=0;FogVolume user=null;
                foreach(var volume in Object.FindObjectsByType<FogVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if(volume.style==(FogStyleProfile)target) { users++;user=volume; }
                EditorGUILayout.HelpBox(users==0?"No loaded fog volume uses this profile. Assign it to Fog Volume > Style to preview changes.":$"Used by {users} loaded volume(s). Shared asset edits persist after Play Mode.",MessageType.Info);
                using(new EditorGUILayout.HorizontalScope())
                {
                    if(users==1 && GUILayout.Button("Select Using Volume")) Selection.activeGameObject=user.gameObject;
                    if(GUILayout.Button("Duplicate Profile..."))
                    {
                        string path=EditorUtility.SaveFilePanelInProject("Duplicate fog profile",target.name+" Copy","asset","Save an independent profile in Assets.");
                        if(!string.IsNullOrEmpty(path)) Selection.activeObject=FogSetup.SaveProfile(((FogStyleProfile)target).settings,path);
                    }
                }
            }
            serializedObject.Update();FogStyleGUI.Draw(serializedObject,"settings.");serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(FogVolume)),CanEditMultipleObjects]
    sealed class FogVolumeInspector : UnityEditor.Editor
    {
        bool inlineStyle=true,quality=true,lighting=true,advanced;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();var volume=(FogVolume)target;var profile=serializedObject.FindProperty("style");
            EditorGUILayout.PropertyField(profile,new GUIContent("Style Profile"));
            if(profile.hasMultipleDifferentValues)
                EditorGUILayout.HelpBox("Selected volumes have different profiles. Assign one shared profile here, or select a single volume to edit its look.",MessageType.Info);
            else if(profile.objectReferenceValue)
            {
                EditorGUILayout.HelpBox("Appearance and motion come from this asset. Quality, bounds and lights stay on the volume.",MessageType.Info);
                if(GUILayout.Button("Select Style Asset")) Selection.activeObject=profile.objectReferenceValue;
            }
            else if(inlineStyle=EditorGUILayout.Foldout(inlineStyle,"Inline Style",true)) FogStyleGUI.Draw(serializedObject,"");
            EditorGUILayout.Space();EditorGUILayout.LabelField("Bounds",EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("size"),new GUIContent("Size (m)"));
            EditorGUILayout.HelpBox("Transform position is the floor center. Bounds are world-aligned; rotation and scale do not resize the fog.",MessageType.None);
            if(quality=EditorGUILayout.Foldout(quality,"Simulation and Rendering Quality",true))
                FogStyleGUI.Fields(serializedObject,"","simulationBackend","resolution","simulationRate","pressureIterations","renderResolution","raySteps");
            if(lighting=EditorGUILayout.Foldout(lighting,"Lighting",true)) FogStyleGUI.Fields(serializedObject,"","sun","localLights");
            if(advanced=EditorGUILayout.Foldout(advanced,"Advanced",true)) FogStyleGUI.Fields(serializedObject,"","simulation","automaticSimulation");
            serializedObject.ApplyModifiedProperties();
            if(!serializedObject.isEditingMultipleObjects)
            {
                if(!volume.simulation)
                {
                    EditorGUILayout.HelpBox("Assign the package compute shader to initialize this volume.",MessageType.Warning);
                    if(GUILayout.Button("Assign Package Compute Shader"))
                    { Undo.RecordObject(volume,"Assign fog compute");volume.simulation=AssetDatabase.LoadAssetAtPath<ComputeShader>(FogSetup.PackageRoot+"/Runtime/Shaders/FogFluid.compute");PrefabUtility.RecordPrefabInstancePropertyModifications(volume);EditorUtility.SetDirty(volume);volume.Initialize(); }
                }
                Vector3 cell=new(volume.size.x/Mathf.Max(1,volume.resolution.x),volume.size.y/Mathf.Max(1,volume.resolution.y),volume.size.z/Mathf.Max(1,volume.resolution.z));
                EditorGUILayout.LabelField("Cell Size (m)",$"{cell.x:0.###} x {cell.y:0.###} x {cell.z:0.###}");
                EditorGUILayout.LabelField("Allocated Fog Textures",$"{volume.EstimatedTextureBytes/(1024.0*1024):0.##} MiB");
                if(volume.ResolvedStyle.fogHeight>volume.size.y) EditorGUILayout.HelpBox("Fog Height exceeds the volume's vertical bounds; the top will be clipped.",MessageType.Warning);
                if(volume.sun && volume.sun.type!=LightType.Directional)
                    EditorGUILayout.HelpBox("Sun expects a directional light. Put point lights in Local Lights.",MessageType.Warning);
                if(volume.simulationBackend==FogSimulationBackend.Planar2D)
                    EditorGUILayout.HelpBox("Planar2D transports horizontal columns. Y resolution controls reconstructed shape and lighting; vertical motion is approximated. Changing backend restarts the simulation.",MessageType.Info);
                if(GUILayout.Button("Save Current Look as Style Profile..."))
                {
                    string path=EditorUtility.SaveFilePanelInProject("Save independent fog style",volume.style?volume.style.name+" Copy":"Fog Style","asset","Save a copy and assign it to this volume.");
                    if(!string.IsNullOrEmpty(path)) EditorGUIUtility.PingObject(FogSetup.SaveStyleCopy(volume,path));
                }
            }
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("Reset Simulation")) foreach(var item in targets) ((FogVolume)item).ResetSimulation();
                if(GUILayout.Button("Setup Tools")) FogSetup.OpenSetup();
            }
        }
    }
}
