using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace JLink.Frames
{
    // A magnified cross section. The horizontal axis runs from the opening to the outside.
    internal sealed class ProfilePad : VisualElement
    {
        readonly Func<FrameRecipe> recipe;
        readonly Action begin, change, end;
        int key = -1;
        const float Inset = 10;

        public ProfilePad(Func<FrameRecipe> recipe,Action begin,Action change,Action end)
        {
            this.recipe = recipe;
            this.begin = begin;
            this.change = change;
            this.end = end;
            name = "ProfilePad";
            AddToClassList("profile-pad");
            generateVisualContent += Draw;
            RegisterCallback<PointerDownEvent>(Down);
            RegisterCallback<PointerMoveEvent>(Move);
            RegisterCallback<PointerUpEvent>(e => { if (this.HasPointerCapture(e.pointerId)) this.ReleasePointer(e.pointerId); });
            RegisterCallback<PointerCaptureOutEvent>(_ => { if (key >= 0) end(); key = -1; });
        }

        Vector2 ToPad(float t,float h) => new Vector2(Inset+t*(contentRect.width-2*Inset),
            Inset+(1-h)*(contentRect.height-2*Inset));
        Vector2 FromPad(Vector2 p) => new Vector2(Mathf.Clamp01((p.x-Inset)/Mathf.Max(1,contentRect.width-2*Inset)),
            Mathf.Clamp01(1-(p.y-Inset)/Mathf.Max(1,contentRect.height-2*Inset)));

        void Draw(MeshGenerationContext context)
        {
            var curve = recipe().profile;
            var pen = context.painter2D;
            pen.lineWidth = 2;
            pen.strokeColor = FrameWorkshop.Accent;
            pen.fillColor = new Color(.48f,.37f,.23f);
            pen.BeginPath();
            pen.MoveTo(ToPad(0,0));
            for (int i = 0; i <= 80; i++)
            {
                float t = i/80f;
                pen.LineTo(ToPad(t,Mathf.Clamp01(curve.Evaluate(t))));
            }
            pen.LineTo(ToPad(1,0));
            pen.ClosePath(); pen.Fill(); pen.Stroke();
            foreach (var k in curve.keys)
                FrameWorkshop.Dot(pen,ToPad(k.time,k.value),4,new Color(1,.94f,.8f));
        }

        void Down(PointerDownEvent e)
        {
            var curve = recipe().profile;
            Vector2 cursor = e.localPosition;
            key = -1;
            float nearest = 12;
            for (int i = 0; i < curve.length; i++)
            {
                float d = Vector2.Distance(cursor,ToPad(curve[i].time,curve[i].value));
                if (d < nearest) { nearest = d; key = i; }
            }
            if (e.button == 1)
            {
                if (key > 0 && key < curve.length-1)
                {
                    begin(); curve.RemoveKey(key); change(); end();
                }
                key = -1;
                return;
            }
            if (e.button != 0) { key = -1; return; }
            if (key < 0 && e.clickCount == 2)
            {
                Vector2 v = FromPad(cursor);
                if (v.x <= 0 || v.x >= 1 || curve.length >= 48) return;
                begin(); key = curve.AddKey(new Keyframe(v.x,v.y)); change();
                if (key < 0) { end(); return; }
            }
            else if (key >= 0) begin();
            if (key >= 0) this.CapturePointer(e.pointerId);
        }

        void Move(PointerMoveEvent e)
        {
            if (key < 0 || !this.HasPointerCapture(e.pointerId)) return;
            var curve = recipe().profile;
            Vector2 v = FromPad(e.localPosition);
            var k = curve[key];
            if (key > 0 && key < curve.length-1)
            {
                float margin = Mathf.Min(.004f,(curve[key+1].time-curve[key-1].time)*.2f);
                k.time = Mathf.Clamp(v.x,curve[key-1].time+margin,curve[key+1].time-margin);
            }
            k.value = v.y;
            key = curve.MoveKey(key,k);
            change();
        }
    }
}
