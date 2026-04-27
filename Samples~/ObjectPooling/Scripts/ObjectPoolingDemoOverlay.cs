using jlinkdev.UnityUtilities.ObjectPooling;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Samples.ObjectPooling
{
    /// <summary>
    /// Lightweight runtime controls for the object pooling sample.
    /// </summary>
    public sealed class ObjectPoolingDemoOverlay : MonoBehaviour
    {
        [SerializeField] [Tooltip("Demo controller that receives overlay button actions.")]
        private ObjectPoolingDemoController _controller;
        [SerializeField] [Tooltip("Optional explicit handle to display. Uses the controller handle when left empty.")]
        private GameObjectPoolHandle _poolHandle;
        [SerializeField] [Tooltip("Optional explicit registry to display. Uses the controller registry when left empty.")]
        private GameObjectPoolRegistry _poolRegistry;
        [SerializeField] [Tooltip("Initial position and size of the runtime overlay window.")]
        private Rect _windowRect = new Rect(16f, 16f, 390f, 620f);

        private void OnGUI()
        {
            _windowRect = GUILayout.Window(GetInstanceID(), _windowRect, DrawWindow, "Object Pooling Demo");
        }

        private void DrawWindow(int windowId)
        {
            if (_controller == null)
            {
                GUILayout.Label("Assign ObjectPoolingDemoController.");
                GUI.DragWindow();
                return;
            }

            GameObjectPoolHandle poolHandle = _poolHandle != null ? _poolHandle : _controller.PoolHandle;
            GameObjectPoolRegistry poolRegistry = _poolRegistry != null ? _poolRegistry : _controller.PoolRegistry;

            GUIStyle wrappedLabel = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true
            };

            GUILayout.Label("Watch the Hierarchy while objects spawn, return, and reuse instances.", wrappedLabel);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Mode: {_controller.CurrentModeName}");
            if (GUILayout.Button("Toggle Mode", GUILayout.Width(110f)))
            {
                _controller.ToggleMode();
            }

            GUILayout.EndHorizontal();
            GUILayout.Label(_controller.CurrentModeDescription, wrappedLabel);
            GUILayout.Space(6f);

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("GameObjectPoolHandle");
            if (poolHandle != null)
            {
                GUILayout.Label($"Tracked: {poolHandle.CountAll}");
                GUILayout.Label($"Active: {poolHandle.CountActive}");
                GUILayout.Label($"Inactive: {poolHandle.CountInactive}");
            }
            else
            {
                GUILayout.Label("Pool handle: unassigned");
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("GameObjectPoolRegistry");
            if (poolRegistry != null)
            {
                GUILayout.Label($"Pools: {poolRegistry.CountPools}");
                GUILayout.Label($"Tracked: {poolRegistry.CountAll}");
                GUILayout.Label($"Active: {poolRegistry.CountActive}");
                GUILayout.Label($"Inactive: {poolRegistry.CountInactive}");
            }
            else
            {
                GUILayout.Label("Registry: unassigned");
            }

            DrawRegistryLegend();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Demo Stats");
            GUILayout.Label($"Demo active list: {_controller.ActiveDemoObjects}");
            GUILayout.Label($"Spawn attempts: {_controller.TotalSpawnAttempts}");
            GUILayout.Label($"Successful spawns: {_controller.SuccessfulSpawns}");
            GUILayout.Label($"Failed spawns: {_controller.FailedSpawns}");
            GUILayout.Label($"Returned objects: {_controller.ReturnedObjects}");
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Controls");
            bool nextAutoSpawn = GUILayout.Toggle(_controller.AutoSpawn, "Auto spawn");
            if (nextAutoSpawn != _controller.AutoSpawn)
            {
                _controller.AutoSpawn = nextAutoSpawn;
            }

            if (GUILayout.Button("Spawn One"))
            {
                _controller.SpawnOne();
            }

            if (GUILayout.Button($"Spawn Burst ({_controller.BurstCount})"))
            {
                _controller.SpawnBurst();
            }

            if (GUILayout.Button("Return All"))
            {
                _controller.ReturnAll();
            }

            if (GUILayout.Button($"Prewarm +{_controller.PrewarmStep}"))
            {
                _controller.PrewarmAdditional();
            }

            if (GUILayout.Button("Clear Inactive"))
            {
                _controller.ClearInactive();
            }

            if (GUILayout.Button("Clear All"))
            {
                _controller.ClearAll();
            }
            GUILayout.EndVertical();

            GUILayout.Space(6f);
            GUILayout.Label("Set max capacity low, then use Spawn Burst to see capacity failures.", wrappedLabel);
            GUI.DragWindow();
        }

        private void DrawRegistryLegend()
        {
            if (_controller.RegistryPrefabCount <= 0)
            {
                return;
            }

            GUILayout.Space(4f);
            GUILayout.Label("Registry Prefabs");

            Color previousColor = GUI.color;
            for (int i = 0; i < _controller.RegistryPrefabCount; i++)
            {
                GUILayout.BeginHorizontal();
                Rect swatchRect = GUILayoutUtility.GetRect(16f, 16f, GUILayout.Width(16f), GUILayout.Height(16f));
                GUI.color = _controller.GetRegistryPrefabColor(i);
                GUI.DrawTexture(swatchRect, Texture2D.whiteTexture);
                GUI.color = previousColor;
                GUILayout.Label(_controller.GetRegistryPrefabName(i));
                GUILayout.EndHorizontal();
            }

            GUI.color = previousColor;
        }
    }
}
