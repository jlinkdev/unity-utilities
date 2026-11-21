using UnityEngine;
using UnityEngine.UIElements;

namespace JLink.Pots
{
    public sealed partial class PotteryWorkshop
    {
        enum Drag { None, Orbit, Ring, Height, Handle, Thumb, Wheel }
        Drag drag;
        float brushSize = .22f;
        float wheelBandPercent = 36;
        float[] wheelStart;
        bool placingHandle;
        ThumbMark thumb;
        int markIndex, hovered = -1;
        Vector2 pointerStart;
        float originalLevel, originalRadius, originalHeight, originalReach, grabAngle;

        Vector2 Project(Vector3 p)
        {
            var screen = view.WorldToScreenPoint(Vessel.transform.TransformPoint(p));
            return new Vector2(screen.x, Screen.height-screen.y);
        }

        float Edge(float level)
        {
            float best = float.MinValue, angle = 0;
            Vector3 right = Vessel.transform.InverseTransformDirection(view.transform.right);
            for (int i = 0; i < 64; i++)
            {
                float a = i * (Mathf.PI / 32);
                float x = Vector3.Dot(PotMesh.Surface(P,level,a),right);
                if (x > best) { best = x; angle = a; }
            }
            return angle;
        }

        Vector2 HeightGrip => Project(Vector3.up*P.height) + Vector2.down*24;

        void DrawGuides(MeshGenerationContext context)
        {
            if (!Vessel || !guides || !view || cutaway) return;
            var pen = context.painter2D;
            Vector2 offset = CanvasArea.position;
            void Line(Vector2 a,Vector2 b,Color color,float width)
            {
                pen.strokeColor = color; pen.lineWidth = width;
                pen.BeginPath(); pen.MoveTo(a-offset); pen.LineTo(b-offset); pen.Stroke();
            }
            void Diamond(Vector2 point,Color color,float radius)
            {
                point -= offset;
                pen.fillColor = color;
                pen.strokeColor = new Color(.13f,.16f,.15f);
                pen.lineWidth = 1.2f;
                pen.BeginPath(); pen.MoveTo(point+Vector2.up*radius);
                pen.LineTo(point+Vector2.right*radius); pen.LineTo(point+Vector2.down*radius);
                pen.LineTo(point+Vector2.left*radius); pen.ClosePath(); pen.Fill(); pen.Stroke();
            }
            void GuideRing(float level, Color color, float width)
            {
                Vector2 prev = Project(PotMesh.Surface(P,level,0));
                for (int i = 1; i <= 64; i++)
                {
                    Vector3 local = PotMesh.Surface(P,level,i*Mathf.PI/32);
                    var next = Project(local);
                    local.y = 0;
                    bool front = Vector3.Dot(Vessel.transform.TransformDirection(local),view.transform.forward) < 0;
                    if (front || i%2 == 0) Line(prev,next,front ? color : new Color(color.r,color.g,color.b,color.a*.3f),front ? width : 1);
                    prev = next;
                }
            }
            if (tab == 0)
            {
                int active = hovered >= 0 ? hovered : selected;
                if (drag == Drag.Wheel)
                {
                    float span = wheelBandPercent / 200;
                    var edgeColor = new Color(1,.8f,.55f,.48f);
                    GuideRing(Mathf.Clamp01(originalLevel-span),edgeColor,1);
                    GuideRing(Mathf.Clamp01(originalLevel+span),edgeColor,1);
                    GuideRing(originalLevel,Accent,2);
                }
                else if (active >= 0 && active < P.rings.Count) GuideRing(P.rings[active].level,Accent,2);
                for (int i = 0; i < P.rings.Count; i++)
                {
                    float t = P.rings[i].level;
                    Diamond(Project(PotMesh.Surface(P,t,Edge(t))),i == active ? Accent : Pale,i == active ? 7 : 5);
                }
                Line(HeightGrip+Vector2.up*6,HeightGrip+Vector2.up*18,Pale,1);
                Diamond(HeightGrip,Accent,6);
            }
            if (tab == 2)
                for (int i = 0; i < P.handles; i++)
                    Diamond(Project(HandlePoint(P.handleLevel,HandleAngle(i),P.handleReach)),Accent,7);
        }

        float HandleAngle(int i) => (P.handleAngle/360+(float)i/P.handles)*2*Mathf.PI;

        Drag Pick(Vector2 mouse,out int ring,out float angle)
        {
            ring = -1; angle = 0;
            Drag hit = Drag.Orbit;
            if (!guides || cutaway) return hit;
            float best = 17;
            if (tab == 0)
            {
                if (Vector2.Distance(HeightGrip,mouse) < 15) return Drag.Height;
                for (int i = 0; i < P.rings.Count; i++)
                {
                    float t = P.rings[i].level, a = Edge(t);
                    float d = Vector2.Distance(Project(PotMesh.Surface(P,t,a)),mouse);
                    if (d < best) { best = d; ring = i; angle = a; hit = Drag.Ring; }
                }
            }
            else if (tab == 2)
            {
                for (int i = 0; i < P.handles; i++)
                {
                    float a = HandleAngle(i);
                    float d = Vector2.Distance(Project(HandlePoint(P.handleLevel,a,P.handleReach)),mouse);
                    if (d < best) { best = d; angle = a; hit = Drag.Handle; }
                }
            }
            return hit;
        }

        void Grab(PointerDownEvent e)
        {
            if (e.button > 1 || drag != Drag.None) return;
            BeginGesture();
            canvas.Focus();
            var mouse = (Vector2)e.position;
            Drag kind = Pick(mouse,out int hit,out grabAngle);
            if (e.button == 1 || cutaway) kind = Drag.Orbit;
            else if (e.clickCount == 2 && tab == 0 && guides && kind == Drag.Orbit && InsertRing(mouse))
            { EndGesture(); ShowPanel(); return; }
            else if (tab == 0 && kind == Drag.Orbit && HitWall(mouse,out var wheelContact))
            {
                kind = Drag.Wheel;
                originalLevel = Mathf.Clamp01(wheelContact.y / P.height);
                wheelStart = P.wheelOffsets;
                canvas.MarkDirtyRepaint();
            }
            else if ((tab == 1 || (tab == 2 && placingHandle)) && HitWall(mouse,out var contact))
            {
                float level = Mathf.Clamp01(contact.y/P.height);
                float degrees = Mathf.Atan2(contact.z/P.depthRatio,contact.x)*Mathf.Rad2Deg;
                if (tab == 2)
                {
                    placingHandle = false;
                    Change(() => { P.handles = Mathf.Max(1,P.handles); P.handleLevel = level; P.handleAngle = degrees; },true);
                    return;
                }
                thumb = new ThumbMark { level = level, degrees = degrees, size = brushSize };
                markIndex = -1; kind = Drag.Thumb;
            }

            drag = kind;
            pointerStart = mouse;
            originalHeight = P.height;
            if (drag == Drag.Ring)
            {
                selected = hit;
                originalLevel = P.rings[hit].level; originalRadius = P.rings[hit].radius;
                ShowPanel();
            }
            if (drag == Drag.Handle) { originalLevel = P.handleLevel; originalReach = P.handleReach; }
            canvas.CapturePointer(e.pointerId);
        }

        void Move(PointerMoveEvent e)
        {
            var mouse = (Vector2)e.position;
            if (drag == Drag.None)
            {
                Pick(mouse,out int hit,out _);
                if (hovered != hit) { hovered = hit; canvas.MarkDirtyRepaint(); }
                return;
            }
            if (!canvas.HasPointerCapture(e.pointerId)) return;
            Vector2 delta = mouse-pointerStart;
            float pixelsPerMeter = view.pixelHeight/(2*view.orthographicSize);
            float vertical = Mathf.Max(.17f,Mathf.Cos(pitch*Mathf.Deg2Rad))*pixelsPerMeter;
            if (drag == Drag.Orbit)
            {
                yaw += e.deltaPosition.x*.48f;
                pitch = Mathf.Clamp(pitch+e.deltaPosition.y*.4f,0,80);
                UpdateView(); canvas.MarkDirtyRepaint(); return;
            }
            BeginGesture();
            if (drag == Drag.Wheel) ShapeOnWheel(2 * delta.x / (pixelsPerMeter * P.width));
            else if (drag == Drag.Thumb)
            {
                thumb.depth = Mathf.Clamp(delta.x/(pixelsPerMeter*P.width),-.4f,.5f);
                if (markIndex < 0) { markIndex = P.marks.Count; P.marks.Add(thumb); }
                else P.marks[markIndex] = thumb;
            }
            else if (drag == Drag.Height) P.height = Mathf.Clamp(originalHeight-delta.y/vertical,.04f,2);
            else if (drag == Drag.Ring)
            {
                Vector3 radial = PotMesh.Surface(P,originalLevel,grabAngle);
                radial.y = 0; radial /= P.RadiusAt(originalLevel);
                Vector2 axis = Project(radial)-Project(Vector3.zero);
                float change = delta.x / Mathf.Max(1,axis.x);
                float level = originalLevel;
                if (selected > 0 && selected < P.rings.Count-1)
                    level = Mathf.Clamp(originalLevel-(delta.y-axis.y*change)/(vertical*P.height),
                        P.rings[selected-1].level+.016f,P.rings[selected+1].level-.016f);
                P.rings[selected] = new ProfileRing(level,Mathf.Clamp(originalRadius+change,.12f,2));
            }
            else
            {
                Vector2 axis = Project(HandlePoint(originalLevel,grabAngle,1))-Project(HandlePoint(originalLevel,grabAngle,0));
                float change = Mathf.Abs(axis.x) > 4 ? delta.x/axis.x : 0;
                P.handleReach = Mathf.Clamp(originalReach+change,.12f,.85f);
                P.handleLevel = originalLevel-(delta.y-axis.y*change)/(vertical*P.height);
            }
            Vessel.Rebuild(); Refresh();
        }

        void Release(PointerUpEvent e)
        {
            if (canvas.HasPointerCapture(e.pointerId)) canvas.ReleasePointer(e.pointerId);
            drag = Drag.None;
            EndGesture();
            Refresh();
        }

        void ShapeOnWheel(float displacement)
        {
            if (Mathf.Abs(displacement) < .0001f)
            {
                P.wheelOffsets = wheelStart;
                return;
            }
            // Always start from the press snapshot, so reversing a drag retraces the same shape.
            if (P.wheelOffsets == wheelStart)
                P.wheelOffsets = wheelStart.Length >= 2 ? (float[])wheelStart.Clone() : new float[81];
            float halfSpan = wheelBandPercent / 200;
            for (int i = 0; i < P.wheelOffsets.Length; i++)
            {
                float level = (float)i / (P.wheelOffsets.Length-1);
                float distance = Mathf.Abs(level-originalLevel) / halfSpan;
                float before = wheelStart.Length == 0 ? 0 : wheelStart[Mathf.Min(i,wheelStart.Length-1)];
                float weight = 1 - Mathf.SmoothStep(0,1,distance);
                float radius = P.BaseRadiusAt(level);
                P.wheelOffsets[i] = weight == 0 ? before : Mathf.Clamp(before+displacement*weight,.12f-radius,2-radius);
            }
        }

        Vector3 HandlePoint(float level, float a, float reach)
        {
            var surface = PotMesh.Surface(P, level, a);
            return surface + new Vector3(surface.x,0,surface.z).normalized *
                (reach*P.width*.5f - Mathf.Min(P.wall*.4f,P.width*.012f));
        }

        // Test only on pointer-down. Using the triangles avoids cooking a physics mesh during drags.
        bool HitWall(Vector2 mouse, out Vector3 contact)
        {
            var ray = view.ScreenPointToRay(new Vector3(mouse.x, Screen.height-mouse.y, 0));
            var t = Vessel.transform;
            var origin = t.InverseTransformPoint(ray.origin);
            var dir = t.InverseTransformVector(ray.direction);
            var mesh = Vessel.GeneratedMesh;
            var vertices = mesh.vertices;
            float nearest = float.PositiveInfinity;
            int hitGroup = -1;
            contact = default;
            for (int group = 0; group < mesh.subMeshCount; group++)
            {
                var indices = mesh.GetTriangles(group);
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector3 a = vertices[indices[i]], ab = vertices[indices[i+1]]-a, ac = vertices[indices[i+2]]-a;
                    var cross = Vector3.Cross(dir, ac);
                    float det = Vector3.Dot(ab,cross);
                    if (det <= 1e-12f) continue;
                    Vector3 offset = origin-a;
                    float u = Vector3.Dot(offset,cross)/det;
                    if (u < 0 || u > 1) continue;
                    var q = Vector3.Cross(offset,ab);
                    float v = Vector3.Dot(dir,q)/det;
                    if (v < 0 || u+v > 1) continue;
                    float d = Vector3.Dot(ac,q)/det;
                    if (d < 0 || d >= nearest) continue;
                    nearest = d; hitGroup = group;
                }
            }
            if (hitGroup != 0) return false;
            contact = origin + dir*nearest;
            return contact.y > .001f && contact.y <= P.height;
        }

        bool InsertRing(Vector2 mouse)
        {
            float best = 24, level = -1;
            for (int i = 1; i < 80; i++)
            {
                float t = i / 80f;
                float d = Vector2.Distance(mouse, Project(PotMesh.Surface(P, t, Edge(t))));
                if (d < best) { level = t; best = d; }
            }
            if (level <= 0 || P.rings.Exists(r => Mathf.Abs(r.level-level) < .035f)) return false;
            P.rings.Add(new ProfileRing(level, P.BaseRadiusAt(level)));
            Vessel.Rebuild();
            selected = P.rings.FindIndex(r => r.level == level);
            return true;
        }

        void OnApplicationFocus(bool focused)
        {
            if (focused || !Vessel) return;
            EndGesture();
            drag = Drag.None;
            canvas?.ReleasePointer(PointerId.mousePointerId);
        }
    }
}
