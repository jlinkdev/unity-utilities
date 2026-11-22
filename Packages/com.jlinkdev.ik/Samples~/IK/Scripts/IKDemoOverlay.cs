using SliderDraw = System.Func<UnityEngine.Rect, string, string, float, float, float, float>;
using UnityEngine;
using UnityEngine.UIElements;

namespace jlinkdev.UnityUtilities.Samples.IK
{
    /// <summary>
    /// Lightweight runtime controls for the IK sample scene.
    /// </summary>
    public sealed class IKDemoOverlay : MonoBehaviour
    {
        [SerializeField] [Tooltip("Demo controller that receives overlay actions.")]
        private IKDemoController _controller;
        [SerializeField] [Tooltip("Initial position and size of the runtime overlay window.")]
        private Rect _windowRect = new Rect(16f, 16f, 420f, 680f);
        [SerializeField] [Tooltip("Pixel size for 2D target pads.")]
        private float _padSize = 180f;

        [SerializeField] private GUISkin sampleSkin;
        [SerializeField] private UIDocument sampleIdentity;
        [SerializeField] private ScriptableObject sliderPresenter;
        private SliderDraw drawSampleSlider;
        private SliderDraw SampleSlider
        {
            get
            {
                if (drawSampleSlider == null && sliderPresenter)
                    drawSampleSlider = (SliderDraw)System.Delegate.CreateDelegate(typeof(SliderDraw), sliderPresenter, "Draw", false);
                return drawSampleSlider;
            }
        }


        private void OnGUI()
        {
            var previous = GUI.skin;
            if (sampleSkin) GUI.skin = sampleSkin;
            try { DrawControls(); }
            finally { GUI.skin = previous; }
        }

        private Vector2 scroll;
        private float contentHeight;

        private void DrawControls()
        {
            var root = sampleIdentity ? sampleIdentity.rootVisualElement : null;
            var header = root?.Q(className: "sample-identity");
            float top = header?.panel != null && header.worldBound.height > 0f && root.worldBound.height > 0f
                ? (header.worldBound.yMax + 8f) * Screen.height / root.worldBound.height : 0f;
            float available = Mathf.Max(1f, Screen.height - top - 8f);
            _windowRect.height = Mathf.Min(contentHeight > 0f ? contentHeight : _windowRect.height, available);
            _windowRect.y = Mathf.Clamp(_windowRect.y, top, Mathf.Max(top, Screen.height - _windowRect.height));
            _windowRect = GUILayout.Window(GetInstanceID(), _windowRect, DrawWindow, "IK Demo",
                GUILayout.Height(_windowRect.height));
        }

        private void DrawWindow(int windowId)
        {
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.BeginVertical(GUILayout.ExpandHeight(false));
            DrawWindowContents();
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
                contentHeight = GUILayoutUtility.GetLastRect().height + GUI.skin.window.padding.vertical + 8f;
            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 20f));
        }

        private void DrawWindowContents()
        {
            if (_controller == null)
            {
                GUILayout.Label("Assign IKDemoController.");
                return;
            }

            DrawStationButtons();
            DrawDescription();
            GUILayout.Space(6f);

            DrawGrid("Target Grid", _controller.CurrentTargetGrid, false);
            DrawGrid("Pole Grid", _controller.CurrentPoleGrid, true);
            DrawSolverControls();
        }

        private void DrawDescription()
        {
            if (string.IsNullOrEmpty(_controller.CurrentStationDescription))
            {
                return;
            }

            GUIStyle wrappedLabel = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true
            };

            GUILayout.Space(4f);
            GUILayout.Label(_controller.CurrentStationDescription, wrappedLabel);
        }

        private void DrawStationButtons()
        {
            if (_controller.StationCount <= 0)
            {
                GUILayout.Label("No stations assigned.");
                return;
            }

            GUILayout.Label("Station");
            for (int i = 0; i < _controller.StationCount; i++)
            {
                string stationName = _controller.GetStationName(i);
                bool isActive = i == _controller.ActiveStationIndex;
                bool nextActive = GUILayout.Toggle(isActive, stationName, GUI.skin.button);
                if (nextActive && !isActive)
                {
                    _controller.SetActiveStation(i);
                }
            }
        }

        private void DrawGrid(string title, IKDemoTargetGrid grid, bool poleGrid)
        {
            if (grid == null)
            {
                return;
            }

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(title);

            DrawPositionPad(grid, poleGrid);
            GUILayout.EndVertical();
        }

        private void DrawPositionPad(IKDemoTargetGrid grid, bool poleGrid)
        {
            float padSize = Mathf.Max(80f, _padSize);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect rect = GUILayoutUtility.GetRect(padSize, padSize, GUILayout.Width(padSize), GUILayout.Height(padSize));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            int controlId = GUIUtility.GetControlID((poleGrid ? "PolePad" : "TargetPad").GetHashCode(), FocusType.Passive, rect);
            Event currentEvent = Event.current;

            if ((currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) &&
                (rect.Contains(currentEvent.mousePosition) || GUIUtility.hotControl == controlId))
            {
                GUIUtility.hotControl = controlId;
                Vector2 normalizedPosition = MouseToNormalizedPosition(rect, currentEvent.mousePosition);
                if (poleGrid)
                {
                    _controller.SetCurrentPoleNormalizedPosition(normalizedPosition);
                }
                else
                {
                    _controller.SetCurrentTargetNormalizedPosition(normalizedPosition);
                }

                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
            {
                GUIUtility.hotControl = 0;
                currentEvent.Use();
            }

            DrawPad(rect, grid, poleGrid);
        }

        private void DrawPad(Rect rect, IKDemoTargetGrid grid, bool poleGrid)
        {
            Color previousColor = GUI.color;
            Color previousBackground = GUI.backgroundColor;

            GUI.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);
            GUI.Box(rect, GUIContent.none);

            Color lineColor = new Color(1f, 1f, 1f, 0.22f);
            DrawLine(new Vector2(rect.xMin, rect.center.y), new Vector2(rect.xMax, rect.center.y), lineColor, 1f);
            DrawLine(new Vector2(rect.center.x, rect.yMin), new Vector2(rect.center.x, rect.yMax), lineColor, 1f);

            for (int column = 1; column < grid.Columns - 1; column++)
            {
                float x = Mathf.Lerp(rect.xMin, rect.xMax, column / (float)(grid.Columns - 1));
                DrawLine(new Vector2(x, rect.yMin), new Vector2(x, rect.yMax), new Color(1f, 1f, 1f, 0.08f), 1f);
            }

            for (int row = 1; row < grid.Rows - 1; row++)
            {
                float y = Mathf.Lerp(rect.yMax, rect.yMin, row / (float)(grid.Rows - 1));
                DrawLine(new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), new Color(1f, 1f, 1f, 0.08f), 1f);
            }

            Vector2 point = NormalizedToPadPosition(rect, grid.NormalizedPosition);
            Rect pointRect = new Rect(point.x - 6f, point.y - 6f, 12f, 12f);
            GUI.color = poleGrid ? new Color(1f, .65f, .24f) : new Color(.16f, .86f, .84f);
            GUI.DrawTexture(pointRect, Texture2D.whiteTexture);

            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
        }

        private static Vector2 MouseToNormalizedPosition(Rect rect, Vector2 mousePosition)
        {
            float x = Mathf.InverseLerp(rect.xMin, rect.xMax, mousePosition.x) * 2f - 1f;
            float y = Mathf.InverseLerp(rect.yMax, rect.yMin, mousePosition.y) * 2f - 1f;
            return new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
        }

        private static Vector2 NormalizedToPadPosition(Rect rect, Vector2 normalizedPosition)
        {
            float x = Mathf.Lerp(rect.xMin, rect.xMax, (normalizedPosition.x + 1f) * 0.5f);
            float y = Mathf.Lerp(rect.yMax, rect.yMin, (normalizedPosition.y + 1f) * 0.5f);
            return new Vector2(x, y);
        }

        private static void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;

            Vector2 delta = end - start;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - (width * 0.5f), delta.magnitude, width), Texture2D.whiteTexture);

            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void DrawSolverControls()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Solver Controls");

            if (_controller.CurrentHasWeight)
            {
                float nextWeight = DrawSlider("Weight", _controller.CurrentWeight, 0f, 1f);
                if (!Mathf.Approximately(nextWeight, _controller.CurrentWeight))
                {
                    _controller.CurrentWeight = nextWeight;
                }
            }

            if (_controller.CurrentHasMatchTipRotation)
            {
                bool nextMatchTipRotation = GUILayout.Toggle(_controller.CurrentMatchTipRotation, "Match tip rotation");
                if (nextMatchTipRotation != _controller.CurrentMatchTipRotation)
                {
                    _controller.CurrentMatchTipRotation = nextMatchTipRotation;
                }
            }

            if (_controller.CurrentHasIterations)
            {
                int nextIterations = Mathf.RoundToInt(DrawSlider("Iterations", _controller.CurrentIterations, 1f, 24f));
                if (nextIterations != _controller.CurrentIterations)
                {
                    _controller.CurrentIterations = nextIterations;
                }
            }

            if (_controller.CurrentHasLockRoot)
            {
                bool nextLockRoot = GUILayout.Toggle(_controller.CurrentLockRoot, "Lock root");
                if (nextLockRoot != _controller.CurrentLockRoot)
                {
                    _controller.CurrentLockRoot = nextLockRoot;
                }
            }

            if (_controller.CurrentHasAimAxis)
            {
                DrawAimAxisControls();
            }

            if (_controller.CurrentHasGroundProbe)
            {
                DrawGroundProbeControls();
            }

            GUILayout.EndVertical();
        }

        private void DrawAimAxisControls()
        {
            GUILayout.Label($"Aim axis: {_controller.CurrentAimAxis}");
            GUILayout.BeginHorizontal();
            DrawAimAxisButton(IKDemoController.AimAxisOption.Forward, "Forward");
            DrawAimAxisButton(IKDemoController.AimAxisOption.Up, "Up");
            DrawAimAxisButton(IKDemoController.AimAxisOption.Right, "Right");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawAimAxisButton(IKDemoController.AimAxisOption.Back, "Back");
            DrawAimAxisButton(IKDemoController.AimAxisOption.Down, "Down");
            DrawAimAxisButton(IKDemoController.AimAxisOption.Left, "Left");
            GUILayout.EndHorizontal();
        }

        private void DrawAimAxisButton(IKDemoController.AimAxisOption axis, string label)
        {
            bool selected = _controller.CurrentAimAxis == axis;
            bool nextSelected = GUILayout.Toggle(selected, label, GUI.skin.button);
            if (nextSelected && !selected)
            {
                _controller.CurrentAimAxis = axis;
            }
        }

        private void DrawGroundProbeControls()
        {
            float nextRayDistance = DrawSlider("Ray distance", _controller.CurrentGroundRayDistance, 0.1f, 5f);
            if (!Mathf.Approximately(nextRayDistance, _controller.CurrentGroundRayDistance))
            {
                _controller.CurrentGroundRayDistance = nextRayDistance;
            }

            float nextSurfaceOffset = DrawSlider("Surface offset", _controller.CurrentGroundSurfaceOffset, 0f, 0.5f);
            if (!Mathf.Approximately(nextSurfaceOffset, _controller.CurrentGroundSurfaceOffset))
            {
                _controller.CurrentGroundSurfaceOffset = nextSurfaceOffset;
            }

            float nextPositionSmooth = DrawSlider("Position smooth", _controller.CurrentGroundPositionSmooth, 0f, 30f);
            if (!Mathf.Approximately(nextPositionSmooth, _controller.CurrentGroundPositionSmooth))
            {
                _controller.CurrentGroundPositionSmooth = nextPositionSmooth;
            }

            float nextRotationSmooth = DrawSlider("Rotation smooth", _controller.CurrentGroundRotationSmooth, 0f, 30f);
            if (!Mathf.Approximately(nextRotationSmooth, _controller.CurrentGroundRotationSmooth))
            {
                _controller.CurrentGroundRotationSmooth = nextRotationSmooth;
            }

            bool nextAlignToNormal = GUILayout.Toggle(_controller.CurrentGroundAlignToNormal, "Align to normal");
            if (nextAlignToNormal != _controller.CurrentGroundAlignToNormal)
            {
                _controller.CurrentGroundAlignToNormal = nextAlignToNormal;
            }
        }

        private float DrawSlider(string label, float value, float min, float max)
        {
            if (SampleSlider != null) return SampleSlider(GUILayoutUtility.GetRect(0, 35, GUILayout.ExpandWidth(true)),label,value.ToString("0.00"),value,min,max);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {value:0.00}", GUILayout.Width(145f));
            float nextValue = GUILayout.HorizontalSlider(value, min, max);
            GUILayout.EndHorizontal();
            return nextValue;
        }

    }
}
