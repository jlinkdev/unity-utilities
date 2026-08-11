using UnityEngine;

namespace jlinkdev.UnityUtilities.Portals.Samples
{
    public sealed class PortalSampleOverlay : MonoBehaviour
    {
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;

        private void OnGUI()
        {
            titleStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            bodyStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(0.88f, 0.94f, 1f) } };

            GUI.Box(new Rect(16f, 16f, 390f, 116f), GUIContent.none);
            GUI.Label(new Rect(30f, 26f, 360f, 28f), "jlinkdev Portals — Playground", titleStyle);
            GUI.Label(new Rect(30f, 58f, 350f, 62f),
                "WASD  Move    Mouse  Look    Esc  Release cursor\n" +
                "Walk through either portal. The orange crate loops automatically.\n" +
                "The face-to-face pair demonstrates recursive rendering and traversal.", bodyStyle);
        }
    }
}
