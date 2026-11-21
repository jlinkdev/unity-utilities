using UnityEditor;
using UnityEngine;

namespace JLink.Frames.Editor
{
    public sealed partial class FrameInspector
    {
        readonly FrameCornerTool cornerTool = new FrameCornerTool();

        void DrawCornerInspector(ProceduralFrame frame)
        {
            var r=frame.recipe;
            if (!DrawModeButton(FrameEditMode.Corners,"Edit linked corners in Scene view")) return;
            if (r.outline==FrameOutline.Oval)
            {
                EditorGUILayout.HelpBox("Choose Rectangle or Rounded to use corner ornaments.",MessageType.None);
                return;
            }
            var c=r.corners;
            var style=(CornerStyle)EditorGUILayout.EnumPopup("Four linked corners",c.style);
            if (style!=c.style) Edit(frame,()=>c.style=style);
            if (!c.Visible(r)) return;
            cornerTool.adjustment=(CornerAdjustment)GUILayout.Toolbar((int)cornerTool.adjustment,
                new[]{"Position","Size","Relief"});
            EditorGUI.BeginChangeCheck();
            float pos=EditorGUILayout.Slider("Position (%)",c.position*100,0,100);
            float size=EditorGUILayout.Slider("Size (mm)",c.size*1000,1,400);
            float relief=EditorGUILayout.Slider("Relief (mm)",c.relief*1000,.1f,100);
            if (EditorGUI.EndChangeCheck())
                Edit(frame,()=>{c.position=pos*.01f; c.size=size*.001f; c.relief=relief*.001f;});
            if (c.FittedSize(r)<c.size-.00001f)
                EditorGUILayout.HelpBox($"Fits {c.FittedSize(r)*1000:0.#} mm on this frame.",MessageType.None);
            EditorGUILayout.HelpBox("Drag any corner to adjust all four. Position follows the mitre, Size follows the rail, and dragging up increases Relief.",MessageType.None);
        }

        readonly FrameEdgeTool edgeTool=new FrameEdgeTool();

        void DrawEdgeInspector(ProceduralFrame frame)
        {
            var r=frame.recipe;
            bool oval=r.outline==FrameOutline.Oval;
            if (!DrawModeButton(FrameEditMode.Edges,oval ? "Sculpt oval edges in Scene view" : "Sculpt rectangular edges in Scene view")) return;
            if (FrameEdgeTool.GripCount(r)==0)
            {
                EditorGUILayout.HelpBox("Choose Rectangle or Oval to sculpt the silhouette.",MessageType.None); return;
            }
            var shape=r.edges;
            if (!oval)
            {
                bool split=EditorGUILayout.Toggle("Shape top rail separately",shape.independentTop);
                if (split!=shape.independentTop) Edit(frame,()=>shape.SeparateTop(split));
            }
            int first=oval ? (int)EdgeFeature.Top : 0;
            int picked=GUILayout.Toolbar((int)edgeTool.Selected(r)-first,oval ? new[]{"Top","Bottom","Sides"} : new[]{"Corner","Waist","Centre"});
            edgeTool.feature=(EdgeFeature)(first+picked);
            EditorGUILayout.LabelField(oval ? (edgeTool.feature==EdgeFeature.Sides ? "Linked left and right scallops" : edgeTool.feature+" scallop") : edgeTool.Selection(shape));
            EditorGUI.BeginChangeCheck();
            float amount=EditorGUILayout.Slider("Projection (mm)",shape.Amount(edgeTool.feature,edgeTool.side)*1000,-720*r.railWidth,400);
            float units=oval ? 360 : 100;
            float span=EditorGUILayout.Slider(oval ? "Reach (degrees)" : "Reach (% of side)",shape.Span(edgeTool.feature)*units,oval ? 12 : 5,oval ? 45 : 25);
            if (EditorGUI.EndChangeCheck()) Edit(frame,()=>{
                shape.SetAmount(edgeTool.feature,edgeTool.side,amount*.001f);
                shape.SetSpan(edgeTool.feature,span/units);
            });
            EditorGUILayout.HelpBox(oval ? "Drag the top or bottom independently. Either side handle shapes both sides. Reach widens the crest and its shoulders." : "Drag a gold corner, green waist or blue centre. Set Follow edge on each profile loop.",MessageType.None);
        }

        void DrawPerimeterHandles(ProceduralFrame frame)
        {
            var r=frame.recipe;
            if (mode==FrameEditMode.Edges ? FrameEdgeTool.GripCount(r)==0 : !r.corners.Visible(r)) return;
            var e=Event.current;
            int id=GUIUtility.GetControlID(FocusType.Passive);
            Vector2 Project(Vector3 p)=>HandleUtility.WorldToGUIPoint(frame.DesignToWorld.MultiplyPoint3x4(p));
            if (e.type==EventType.Repaint)
            {
                var camera=SceneView.currentDrawingSceneView.camera;
                for (int i=0;i<(mode==FrameEditMode.Edges ? FrameEdgeTool.GripCount(r) : 4);i++)
                    using (new Handles.DrawingScope(mode==FrameEditMode.Edges ? FrameEdgeTool.ColorFor(FrameEdgeTool.Feature(r,i)) : FrameWorkshop.Accent))
                    {
                        Vector3 p=frame.DesignToWorld.MultiplyPoint3x4(mode==FrameEditMode.Edges ? FrameEdgeTool.Handle(r,i) : r.corners.Handle(frame,i,cornerTool.adjustment));
                        float scale=Vector2.Distance(HandleUtility.WorldToGUIPoint(p),HandleUtility.WorldToGUIPoint(p+camera.transform.right*.01f));
                        Handles.DrawSolidDisc(p,camera.transform.forward,.06f/Mathf.Max(.01f,scale));
                    }
            }
            if (e.type==EventType.MouseUp && e.button==0 && GUIUtility.hotControl==id)
            {
                GUIUtility.hotControl=0; cornerTool.Release(); edgeTool.Release(); Undo.CollapseUndoOperations(gestureGroup); e.Use(); return;
            }
            if (e.alt || Tools.current==Tool.View) return;
            if (e.type==EventType.Layout) HandleUtility.AddDefaultControl(id);
            if (e.type==EventType.MouseDown && e.button==0 && HandleUtility.nearestControl==id &&
                (mode==FrameEditMode.Edges ? edgeTool.Press(r,e.mousePosition,Project) : cornerTool.Press(frame,e.mousePosition,Project)))
            {
                Undo.IncrementCurrentGroup(); gestureGroup=Undo.GetCurrentGroup();
                GUIUtility.hotControl=id; Repaint(); e.Use();
            }
            else if (e.type==EventType.MouseDrag && GUIUtility.hotControl==id)
            {
                Undo.RecordObject(frame,mode==FrameEditMode.Edges ? "Sculpt frame edge" : "Adjust four frame corners");
                if (mode==FrameEditMode.Edges) edgeTool.Drag(r,e.mousePosition); else cornerTool.Drag(r,e.mousePosition);
                CommitGesture(frame); e.Use();
            }
        }
    }
}
