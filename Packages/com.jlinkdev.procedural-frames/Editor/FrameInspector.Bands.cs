using UnityEditor;
using UnityEngine;

namespace JLink.Frames.Editor
{
    public sealed partial class FrameInspector
    {
        readonly FrameBandTool bandTool = new FrameBandTool();
        int gestureGroup;

        void DrawBandInspector(ProceduralFrame frame)
        {
            var r = frame.recipe;
            if (!DrawModeButton(FrameEditMode.Loops,"Edit profile loops in Scene view")) return;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Ridge")) Arm(1);
                if (GUILayout.Button("+ Groove")) Arm(-1);
                if (bandTool.Adding != 0 && GUILayout.Button("Cancel")) Arm(0);
            }
            EditorGUILayout.HelpBox(bandTool.Adding != 0 ? "Point at a rail to preview. Press to place, then drag up or down to set relief." :
                "Drag a loop to move it across the rail. Pull either gold side dot to change width. Drag the blue top dot up or down to set height.", MessageType.None);
            if (r.bands.Count == 0) return;
            int index = Mathf.Clamp(bandTool.Selected,0,r.bands.Count-1);
            if (bandTool.Selected != index && bandTool.Adding == 0) bandTool.Select(index);
            var b = r.bands[index];
            var labels = new string[r.bands.Count];
            for (int i = 0; i < labels.Length; i++) labels[i] = $"{i+1}   {(r.bands[i].height < 0 ? "Groove" : "Ridge")}";
            int next = EditorGUILayout.Popup("Selected loop",index,labels);
            if (next != index) { bandTool.Select(next); SceneView.RepaintAll(); return; }
            if (FrameEdgeTool.GripCount(r)>0)
            {
                float follow=EditorGUILayout.Slider("Follow edge (%)",b.edgeFollow*100,0,100);
                if (follow!=b.edgeFollow*100) Edit(frame,()=>b.edgeFollow=follow*.01f);
            }
            var ornament = (BandOrnament)EditorGUILayout.EnumPopup("Repeating detail",b.ornament);
            if (b.ornament != ornament) Edit(frame,() => b.ornament=ornament);
            if (b.ornament != BandOrnament.None)
            {
                float size = EditorGUILayout.Slider("Detail size (mm)",b.detailSize*1000,1,200);
                float gap = EditorGUILayout.Slider("Detail gap (mm)",b.detailGap*1000,0,250);
                if (size != b.detailSize*1000 || gap != b.detailGap*1000)
                    Edit(frame,() => { b.detailSize=size*.001f; b.detailGap=gap*.001f; });
                float fitted = b.FittedSize(r);
                if (fitted < b.detailSize-.00001f)
                    EditorGUILayout.HelpBox($"Fits {fitted*1000:0.#} mm at this loop position.",MessageType.None);
                if (b.ornament == BandOrnament.LeafScroll && r.outline == FrameOutline.Rectangle)
                {
                    bool mirror = EditorGUILayout.Toggle("Mirror opposite rails",b.mirrorOppositeRails);
                    if (mirror != b.mirrorOppositeRails) Edit(frame,() => b.mirrorOppositeRails=mirror);
                }
                EditorGUILayout.HelpBox("Drag the green dot along the rail to size every ornament. Drag the coral dot to change the gap.",MessageType.None);
            }
            var shape = (BandShape)EditorGUILayout.EnumPopup("Cross section",b.shape);
            var anchor = (BandAnchor)EditorGUILayout.EnumPopup("Anchor",b.anchor);
            if (shape != b.shape || anchor != b.anchor)
                Edit(frame,() => { b.shape = shape; if (anchor != b.anchor) b.SetAnchor(r.railWidth,anchor); });
            float center = b.Center(r.railWidth)*1000, width = b.Width(r.railWidth)*1000;
            float position = EditorGUILayout.FloatField("Centre from inside (mm)",center);
            float newWidth = EditorGUILayout.FloatField("Width (mm)",width);
            float height = EditorGUILayout.FloatField("Height (mm)",b.height*1000);
            if (position != center || width != newWidth || height != b.height*1000)
                Edit(frame,() => {
                    if (position != center) b.MoveTo(r.railWidth,position*.001f);
                    if (newWidth != width) b.SetWidth(r.railWidth,newWidth*.001f);
                    b.height = height*.001f;
                });
            if (GUILayout.Button("Remove selected loop"))
                Edit(frame,() => { r.bands.RemoveAt(index); bandTool.Select(Mathf.Min(index,r.bands.Count-1)); });
        }

        void Arm(int sign) { bandTool.Arm(sign); SceneView.RepaintAll(); }

        void DrawSceneBands(ProceduralFrame frame)
        {
            var e = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Vector2 Project(Vector3 local) => HandleUtility.WorldToGUIPoint(frame.DesignToWorld.MultiplyPoint3x4(local));
            if (e.type == EventType.Repaint)
            {
                var r = frame.recipe;
                using (new Handles.DrawingScope(frame.DesignToWorld))
                {
                    for (int i = 0; i < r.bands.Count; i++)
                    {
                        bool selected = i == bandTool.Selected && bandTool.Adding == 0;
                        Handles.color = selected ? FrameWorkshop.Accent : new Color(1,1,1,.45f);
                        Handles.DrawAAPolyLine(selected ? 3 : 1,bandTool.Loop(frame,r.bands[i].Center(r.railWidth)/r.railWidth));
                    }
                    if (bandTool.Preview >= 0)
                    {
                        Handles.color = Color.cyan;
                        Handles.DrawAAPolyLine(3,bandTool.Loop(frame,bandTool.Preview));
                    }
                }
                var b = bandTool.Current(r);
                if (b != null && bandTool.Adding == 0)
                {
                    var camera = SceneView.currentDrawingSceneView.camera;
                    for (var grip = FrameBandTool.Grip.InnerWidth; grip <= FrameBandTool.LastGrip(b); grip++)
                        using (new Handles.DrawingScope(FrameBandTool.GripColor(grip)))
                        {
                            var p = frame.DesignToWorld.MultiplyPoint3x4(bandTool.HandleFor(frame,b,grip));
                            float pixels = Vector2.Distance(HandleUtility.WorldToGUIPoint(p),
                                HandleUtility.WorldToGUIPoint(p+camera.transform.right*.01f));
                            Handles.DrawSolidDisc(p,camera.transform.forward,.06f/Mathf.Max(.01f,pixels));
                        }
                }
            }
            if (e.type == EventType.MouseUp && e.button == 0 && GUIUtility.hotControl == id)
            {
                GUIUtility.hotControl = 0; bandTool.Release(); Undo.CollapseUndoOperations(gestureGroup); e.Use(); return;
            }
            if (e.alt || Tools.current == Tool.View) return;
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(id);
            if (e.type == EventType.MouseMove && bandTool.Adding != 0)
            {
                bandTool.Hover(frame,HandleUtility.GUIPointToWorldRay(e.mousePosition)); SceneView.RepaintAll();
            }
            if (e.type == EventType.MouseDown && e.button == 0 && HandleUtility.nearestControl == id)
            {
                Undo.IncrementCurrentGroup();
                gestureGroup = Undo.GetCurrentGroup();
                Undo.RecordObject(frame,"Edit profile loop");
                bool placing=bandTool.Adding!=0;
                if (!bandTool.Press(frame,HandleUtility.GUIPointToWorldRay(e.mousePosition),e.mousePosition,Project)) return;
                GUIUtility.hotControl = id;
                if (placing) CommitGesture(frame);
                else { Repaint(); SceneView.RepaintAll(); }
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && GUIUtility.hotControl == id)
            {
                Undo.RecordObject(frame,"Edit profile loop");
                bandTool.Drag(frame.recipe,e.mousePosition);
                CommitGesture(frame); e.Use();
            }
        }

        void CommitGesture(ProceduralFrame frame)
        {
            frame.Rebuild();
            PrefabUtility.RecordPrefabInstancePropertyModifications(frame);
            EditorUtility.SetDirty(frame);
            Repaint(); SceneView.RepaintAll();
        }
    }
}
