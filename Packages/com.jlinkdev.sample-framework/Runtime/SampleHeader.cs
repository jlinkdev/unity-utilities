using UnityEngine;

namespace jlinkdev.SampleFramework
{
    [AddComponentMenu("jlinkdev/Samples/Sample Header")]
    public sealed class SampleHeader : MonoBehaviour
    {
        [SerializeField] private GUISkin skin;
        [SerializeField] private string heading = "Sample Framework";
        [SerializeField] private string instructions = "Shared title and instruction styles";

        private void OnGUI()
        {
            if (skin == null) return;
            float scale = Mathf.Sqrt(Screen.width / 1024f * (Screen.height / 576f));
            float left = 23 * scale;
            float top = 68 * scale;
            GUI.Label(new Rect(left, top, Screen.width - 2 * left, 44), heading, skin.GetStyle("sample-title"));
            GUI.Label(new Rect(left, top + 40, Screen.width - 2 * left, 40), instructions, skin.GetStyle("sample-caption"));
        }
    }
}
