using UnityEngine;

namespace jlinkdev.UnityUtilities.Samples.IK
{
    /// <summary>
    /// Places an IK target on a simple configurable grid for sample scene controls.
    /// </summary>
    public sealed class IKDemoTargetGrid : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] [Tooltip("Transform moved by the grid. Uses this transform when left empty.")]
        private Transform _target;
        [SerializeField] [Tooltip("Optional transform used as the grid origin.")]
        private Transform _origin;
        [SerializeField] [Tooltip("Offset from the grid origin before cell offsets are applied.")]
        private Vector3 _originOffset;

        [Header("Grid")]
        [SerializeField] [Tooltip("Number of horizontal cells.")]
        [Min(1)]
        private int _columns = 5;
        [SerializeField] [Tooltip("Number of vertical cells.")]
        [Min(1)]
        private int _rows = 5;
        [SerializeField] [Tooltip("Distance between adjacent grid cells in world units.")]
        [Min(0.01f)]
        private float _cellSize = 0.5f;
        [SerializeField] [Tooltip("Current horizontal cell index.")]
        private int _column = 2;
        [SerializeField] [Tooltip("Current vertical cell index.")]
        private int _row = 2;
        [SerializeField] [Tooltip("Continuous grid position from -1 to 1 on each axis.")]
        private Vector2 _normalizedPosition;

        [Header("Axes")]
        [SerializeField] [Tooltip("Horizontal grid axis. If an origin is assigned, this can be interpreted in origin-local space.")]
        private Vector3 _horizontalAxis = Vector3.right;
        [SerializeField] [Tooltip("Vertical grid axis. If an origin is assigned, this can be interpreted in origin-local space.")]
        private Vector3 _verticalAxis = Vector3.up;
        [SerializeField] [Tooltip("Use the origin rotation when resolving grid axes.")]
        private bool _useOriginRotation = true;

        [Header("Debug")]
        [SerializeField] [Tooltip("Draw the grid in the scene view when selected.")]
        private bool _drawGizmos = true;
        [SerializeField] [Tooltip("Scene view gizmo color for this grid.")]
        private Color _gizmoColor = new Color(0.2f, 1f, 0.45f, 0.75f);
        [SerializeField, HideInInspector]
        private bool _hasFallbackOriginPosition;
        [SerializeField, HideInInspector]
        private Vector3 _fallbackOriginPosition;

        public Transform Target
        {
            get => _target != null ? _target : transform;
            set
            {
                _target = value;
                ApplyPosition();
            }
        }

        public Transform Origin
        {
            get => _origin;
            set
            {
                _origin = value;
                ApplyPosition();
            }
        }

        public int Columns
        {
            get => Mathf.Max(1, _columns);
            set
            {
                _columns = Mathf.Max(1, value);
                ClampCell();
                ApplyPosition();
            }
        }

        public int Rows
        {
            get => Mathf.Max(1, _rows);
            set
            {
                _rows = Mathf.Max(1, value);
                ClampCell();
                ApplyPosition();
            }
        }

        public int Column => Mathf.RoundToInt(Mathf.Lerp(0f, Columns - 1, (_normalizedPosition.x + 1f) * 0.5f));
        public int Row => Mathf.RoundToInt(Mathf.Lerp(0f, Rows - 1, (_normalizedPosition.y + 1f) * 0.5f));
        public float CellSize => _cellSize;
        public Vector2 NormalizedPosition => _normalizedPosition;
        public Vector3 TargetPosition => ResolvePosition();

        private void OnEnable()
        {
            ApplyPosition();
        }

        private void OnValidate()
        {
            _columns = Mathf.Max(1, _columns);
            _rows = Mathf.Max(1, _rows);
            _cellSize = Mathf.Max(0.01f, _cellSize);
            _normalizedPosition.x = Mathf.Clamp(_normalizedPosition.x, -1f, 1f);
            _normalizedPosition.y = Mathf.Clamp(_normalizedPosition.y, -1f, 1f);
            ClampCell();
            ApplyPosition();
        }

        public void SetCell(int column, int row)
        {
            _column = Mathf.Clamp(column, 0, Columns - 1);
            _row = Mathf.Clamp(row, 0, Rows - 1);
            _normalizedPosition = CellToNormalizedPosition(_column, _row);
            ApplyPosition();
        }

        public void MoveCell(int columnDelta, int rowDelta)
        {
            SetCell(_column + columnDelta, _row + rowDelta);
        }

        public void Center()
        {
            SetNormalizedPosition(Vector2.zero);
        }

        public void SetNormalizedPosition(Vector2 normalizedPosition)
        {
            _normalizedPosition.x = Mathf.Clamp(normalizedPosition.x, -1f, 1f);
            _normalizedPosition.y = Mathf.Clamp(normalizedPosition.y, -1f, 1f);
            _column = Column;
            _row = Row;
            ApplyPosition();
        }

        public void CaptureOriginFromTarget()
        {
            CaptureFallbackOriginPosition();
            ApplyPosition();
        }

        public void ApplyPosition()
        {
            Transform resolvedTarget = Target;
            if (resolvedTarget == null)
            {
                return;
            }

            resolvedTarget.position = ResolvePosition();
        }

        private Vector3 ResolvePosition()
        {
            ResolveAxes(out Vector3 horizontal, out Vector3 vertical);

            Vector3 originPosition = ResolveOriginPosition();
            float horizontalOffset = _normalizedPosition.x * (Columns - 1) * _cellSize * 0.5f;
            float verticalOffset = _normalizedPosition.y * (Rows - 1) * _cellSize * 0.5f;

            return originPosition + _originOffset + (horizontal * horizontalOffset) + (vertical * verticalOffset);
        }

        private Vector3 ResolveOriginPosition()
        {
            if (_origin != null)
            {
                return _origin.position;
            }

            if (!_hasFallbackOriginPosition)
            {
                CaptureFallbackOriginPosition();
            }

            return _fallbackOriginPosition;
        }

        private void CaptureFallbackOriginPosition()
        {
            ResolveAxes(out Vector3 horizontal, out Vector3 vertical);

            float horizontalOffset = _normalizedPosition.x * (Columns - 1) * _cellSize * 0.5f;
            float verticalOffset = _normalizedPosition.y * (Rows - 1) * _cellSize * 0.5f;

            Transform resolvedTarget = Target;
            Vector3 targetPosition = resolvedTarget != null ? resolvedTarget.position : transform.position;
            _fallbackOriginPosition = targetPosition - _originOffset - (horizontal * horizontalOffset) - (vertical * verticalOffset);
            _hasFallbackOriginPosition = true;
        }

        private void ResolveAxes(out Vector3 horizontal, out Vector3 vertical)
        {
            horizontal = ResolveAxis(_horizontalAxis, Vector3.right);
            vertical = ResolveAxis(_verticalAxis, Vector3.up);
        }

        private Vector3 ResolveAxis(Vector3 axis, Vector3 fallback)
        {
            Vector3 resolved = axis.sqrMagnitude > 0.00001f ? axis.normalized : fallback;
            if (_useOriginRotation && _origin != null)
            {
                resolved = _origin.TransformDirection(resolved);
            }

            return resolved.normalized;
        }

        private void ClampCell()
        {
            _column = Mathf.Clamp(_column, 0, Columns - 1);
            _row = Mathf.Clamp(_row, 0, Rows - 1);
        }

        private Vector2 CellToNormalizedPosition(int column, int row)
        {
            float normalizedX = Columns > 1 ? Mathf.Lerp(-1f, 1f, column / (float)(Columns - 1)) : 0f;
            float normalizedY = Rows > 1 ? Mathf.Lerp(-1f, 1f, row / (float)(Rows - 1)) : 0f;
            return new Vector2(normalizedX, normalizedY);
        }

        private void OnDrawGizmos()
        {
            DrawDebugGizmos(0.25f);
        }

        private void OnDrawGizmosSelected()
        {
            DrawDebugGizmos(1f);
        }

        private void DrawDebugGizmos(float alphaScale)
        {
            if (!_drawGizmos)
            {
                return;
            }

            ResolveAxes(out Vector3 horizontal, out Vector3 vertical);
            Vector3 originPosition = ResolveOriginPosition();
            Vector3 center = originPosition + _originOffset;
            float width = (Columns - 1) * _cellSize;
            float height = (Rows - 1) * _cellSize;
            Vector3 bottomLeft = center - (horizontal * width * 0.5f) - (vertical * height * 0.5f);

            Gizmos.color = WithAlpha(_gizmoColor, _gizmoColor.a * alphaScale);
            for (int column = 0; column < Columns; column++)
            {
                Vector3 start = bottomLeft + (horizontal * column * _cellSize);
                Gizmos.DrawLine(start, start + (vertical * height));
            }

            for (int row = 0; row < Rows; row++)
            {
                Vector3 start = bottomLeft + (vertical * row * _cellSize);
                Gizmos.DrawLine(start, start + (horizontal * width));
            }

            Gizmos.DrawWireSphere(ResolvePosition(), _cellSize * 0.12f);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }
    }
}
