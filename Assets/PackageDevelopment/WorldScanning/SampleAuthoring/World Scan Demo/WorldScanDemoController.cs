using jlinkdev.UnityUtilities.WorldScanning;
using UnityEngine;

namespace jlinkdev.UnityUtilities.WorldScanning.Samples
{
    public sealed class WorldScanDemoController : MonoBehaviour
    {
        [SerializeField] private ScanEmitter emitter;
        [SerializeField] private ScanProfile[] profiles;
        [SerializeField, Min(0f)] private float automaticInterval = 5f;

        private int profileIndex;
        private float nextAutomaticScan;

        private void Start()
        {
            ApplyProfile();
            nextAutomaticScan = Time.unscaledTime + 0.8f;
        }

        private void Update()
        {
            if (automaticInterval <= 0f || emitter == null || Time.unscaledTime < nextAutomaticScan)
                return;
            emitter.Emit();
            nextAutomaticScan = Time.unscaledTime + automaticInterval;
        }

        private void OnGUI()
        {
            const float width = 330f;
            GUILayout.BeginArea(new Rect(24f, 24f, width, 220f), GUI.skin.box);
            GUILayout.Label("WORLD SCAN FACILITY", HeaderStyle());
            GUILayout.Label("A focused greybox showcase for the runtime scan stack.");
            GUILayout.Space(8f);
            GUILayout.Label($"Profile  {CurrentProfileName}");
            GUILayout.Label($"Active pulses  {ScanSystem.ActiveCount} / {ScanSystem.MaximumActiveScans}");
            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Emit Pulse", GUILayout.Height(30f)))
            {
                emitter?.Emit();
                nextAutomaticScan = Time.unscaledTime + automaticInterval;
            }
            if (GUILayout.Button("Next Profile", GUILayout.Height(30f)))
                NextProfile();
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label("The camera orbits automatically. Watch the band, triplanar grid, silhouettes, material reveals, and receiver beacons respond as one synchronized event.");
            GUILayout.EndArea();
        }

        private string CurrentProfileName => profiles != null && profiles.Length > 0 && profiles[profileIndex] != null
            ? profiles[profileIndex].name
            : "None";

        private void NextProfile()
        {
            if (profiles == null || profiles.Length == 0)
                return;
            profileIndex = (profileIndex + 1) % profiles.Length;
            ApplyProfile();
            emitter?.Emit();
            nextAutomaticScan = Time.unscaledTime + automaticInterval;
        }

        private void ApplyProfile()
        {
            if (emitter != null && profiles != null && profiles.Length > 0)
                emitter.Profile = profiles[profileIndex];
        }

        private static GUIStyle HeaderStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = new Color(0.2f, 0.95f, 1f);
            return style;
        }
    }
}
