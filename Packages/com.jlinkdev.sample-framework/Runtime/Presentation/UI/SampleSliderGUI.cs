using UnityEngine;
using UnityEngine.Scripting;

namespace JLinkDev.SampleFramework
{
    // Optional samples bind Draw once from the serialized asset. They can compile without this assembly.
    public sealed class SampleSliderGUI : ScriptableObject
    {
        GUISkin styledSkin;
        GUIStyle caption, readout, hitThumb;
        [SerializeField] Texture2D track, accent, valueBackground;

        static void Rounded(Rect rect, Texture2D texture, float radius)
        {
            GUI.DrawTexture(rect,texture,ScaleMode.StretchToFill,true,0f,Color.white,0f,radius);
        }

        [Preserve]
        public float Draw(Rect area,string label,string formattedValue,float value,float min,float max)
        {
            if (styledSkin != GUI.skin || caption == null)
            {
                styledSkin = GUI.skin;
                caption = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleLeft, padding = new RectOffset() };
                readout = new GUIStyle(caption) { alignment = TextAnchor.MiddleRight, padding = new RectOffset(4,4,0,0) };
                hitThumb = new GUIStyle { fixedWidth = 8, fixedHeight = 15 };
            }
            var valueWidth = Mathf.Max(42,readout.CalcSize(new GUIContent(formattedValue)).x);
            var box = new Rect(area.xMax-valueWidth,area.y,valueWidth,18);
            GUI.Label(new Rect(area.x,area.y,Mathf.Max(0,area.width-valueWidth-4),17),label,caption);
            var input = new Rect(area.x,area.y+18,area.width,15);
            float result = GUI.HorizontalSlider(input,value,min,max,GUIStyle.none,hitThumb);
            if (Event.current.type == EventType.Repaint)
            {
                Rounded(box,valueBackground,4f);
                float left = input.x + 4;
                float length = Mathf.Max(0,input.width-8);
                float amount = Mathf.InverseLerp(min,max,result) * length;
                float y = input.center.y;
                Rounded(new Rect(left,y-2f,length,4f),track,2f);
                if (amount > 0f) Rounded(new Rect(left,y-2f,amount,4f),accent,2f);
                Rounded(new Rect(left+amount-4f,y-7.5f,8f,15f),accent,3f);
            }
            GUI.Label(box,formattedValue,readout);
            return result;
        }
    }
}
