using UnityEngine;

namespace jlinkdev.UnityUtilities.Portals.Samples
{
    public sealed class PortalSampleOverlay : MonoBehaviour
    {
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle scaleStyle;

        [SerializeField] private GUISkin sampleSkin;

        private void OnGUI()
        {
            var previous = GUI.skin;
            if (sampleSkin) GUI.skin = sampleSkin;
            try { DrawControls(); }
            finally { GUI.skin = previous; }
        }

        private void DrawControls()
        {
            titleStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            bodyStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(0.88f, 0.94f, 1f) } };
            scaleStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(0.25f, 0.9f, 1f) } };

            float top = Screen.height - 158f;
            GUI.Box(new Rect(16f, top, 490f, 142f), GUIContent.none);
            GUI.Label(new Rect(30f, top + 10f, 330f, 28f), "Traversal controls", titleStyle);
            GUI.Label(new Rect(356f, top + 10f, 132f, 28f), ScaleLabel(), scaleStyle);
            GUI.Label(new Rect(30f, top + 42f, 455f, 88f),
                "WASD  Move    Mouse  Look    Esc  Release cursor    R  Reset\n" +
                "Linked Pair: straightforward 1:1 traversal and moving crate.\n" +
                "Recursion Window: bounded face-to-face portal rendering.\n" +
                "Size Lab: each pass shrinks you to 1/4 scale; reverse to grow.", bodyStyle);
        }

        private static string ScaleLabel()
        {
            PortalSampleController controller = PortalSampleController.Active;
            float scale = controller != null ? controller.CurrentScaleRatio : 1f;
            if (scale < 0.999f)
            {
                float denominator = 1f / Mathf.Max(scale, 0.0001f);
                int rounded = Mathf.RoundToInt(denominator);
                if (Mathf.Abs(denominator - rounded) < 0.02f)
                    return $"Scale  1:{rounded}";
            }

            return $"Scale  {scale:0.##}×";
        }
    }
}
