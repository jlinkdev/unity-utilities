using System;
using jlinkdev.UnityUtilities.IK;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Samples.IK
{
    /// <summary>
    /// Coordinates the fixed IK sample stations and exposes runtime controls for the demo overlay.
    /// </summary>
    public sealed class IKDemoController : MonoBehaviour
    {
        public enum StationId
        {
            TwoBone = 0,
            FABRIK = 1,
            Aim = 2,
            GroundProbe = 3
        }

        public enum AimAxisOption
        {
            Forward,
            Back,
            Up,
            Down,
            Right,
            Left,
            Custom
        }

        [Serializable]
        public sealed class TwoBoneStation
        {
            [SerializeField] [Tooltip("Name shown in the runtime overlay.")]
            private string _displayName = "Two Bone Limb";
            [SerializeField] [Tooltip("Optional scene root enabled for this station when only the active station is shown.")]
            private GameObject _stationRoot;
            [SerializeField] [Tooltip("Target grid used by this station.")]
            private IKDemoTargetGrid _targetGrid;
            [SerializeField] [Tooltip("Pole grid used by this station.")]
            private IKDemoTargetGrid _poleGrid;
            [SerializeField] [Tooltip("TwoBoneIK component controlled by this station.")]
            private TwoBoneIK _twoBoneIK;

            public string DisplayName => ResolveName(_displayName, "Two Bone Limb");
            public GameObject StationRoot => _stationRoot;
            public IKDemoTargetGrid TargetGrid => _targetGrid;
            public IKDemoTargetGrid PoleGrid => _poleGrid;
            public TwoBoneIK TwoBoneIK => _twoBoneIK;
        }

        [Serializable]
        public sealed class FABRIKStation
        {
            [SerializeField] [Tooltip("Name shown in the runtime overlay.")]
            private string _displayName = "FABRIK Chain";
            [SerializeField] [Tooltip("Optional scene root enabled for this station when only the active station is shown.")]
            private GameObject _stationRoot;
            [SerializeField] [Tooltip("Target grid used by this station.")]
            private IKDemoTargetGrid _targetGrid;
            [SerializeField] [Tooltip("Optional pole grid used by this station.")]
            private IKDemoTargetGrid _poleGrid;
            [SerializeField] [Tooltip("FABRIKChain component controlled by this station.")]
            private FABRIKChain _fabrikChain;

            public string DisplayName => ResolveName(_displayName, "FABRIK Chain");
            public GameObject StationRoot => _stationRoot;
            public IKDemoTargetGrid TargetGrid => _targetGrid;
            public IKDemoTargetGrid PoleGrid => _poleGrid;
            public FABRIKChain FABRIKChain => _fabrikChain;
        }

        [Serializable]
        public sealed class AimStation
        {
            [SerializeField] [Tooltip("Name shown in the runtime overlay.")]
            private string _displayName = "Aim IK";
            [SerializeField] [Tooltip("Optional scene root enabled for this station when only the active station is shown.")]
            private GameObject _stationRoot;
            [SerializeField] [Tooltip("Target grid used by this station.")]
            private IKDemoTargetGrid _targetGrid;
            [SerializeField] [Tooltip("AimIK component controlled by this station.")]
            private AimIK _aimIK;

            public string DisplayName => ResolveName(_displayName, "Aim IK");
            public GameObject StationRoot => _stationRoot;
            public IKDemoTargetGrid TargetGrid => _targetGrid;
            public AimIK AimIK => _aimIK;
        }

        [Serializable]
        public sealed class GroundProbeStation
        {
            [SerializeField] [Tooltip("Name shown in the runtime overlay.")]
            private string _displayName = "Ground Probe";
            [SerializeField] [Tooltip("Optional scene root enabled for this station when only the active station is shown.")]
            private GameObject _stationRoot;
            [SerializeField] [Tooltip("Grid used to move the ray origin or other probe-driving target.")]
            private IKDemoTargetGrid _targetGrid;
            [SerializeField] [Tooltip("Optional TwoBoneIK component controlled by this station.")]
            private TwoBoneIK _twoBoneIK;
            [SerializeField] [Tooltip("GroundProbe component controlled by this station.")]
            private GroundProbe _groundProbe;

            public string DisplayName => ResolveName(_displayName, "Ground Probe");
            public GameObject StationRoot => _stationRoot;
            public IKDemoTargetGrid TargetGrid => _targetGrid;
            public TwoBoneIK TwoBoneIK => _twoBoneIK;
            public GroundProbe GroundProbe => _groundProbe;
        }

        [Header("Stations")]
        [SerializeField] private TwoBoneStation _twoBone = new TwoBoneStation();
        [SerializeField] private FABRIKStation _fabrik = new FABRIKStation();
        [SerializeField] private AimStation _aim = new AimStation();
        [SerializeField] private GroundProbeStation _groundProbe = new GroundProbeStation();

        [Header("State")]
        [SerializeField] [Tooltip("Initial active station.")]
        private StationId _activeStation = StationId.TwoBone;
        [SerializeField] [Tooltip("Enable only the active station root while the demo runs.")]
        private bool _showOnlyActiveStation = true;

        public int StationCount => 4;
        public int ActiveStationIndex => (int)_activeStation;
        public StationId ActiveStation => _activeStation;
        public IKDemoTargetGrid CurrentTargetGrid => GetCurrentTargetGrid();
        public IKDemoTargetGrid CurrentPoleGrid => GetCurrentPoleGrid();
        public string CurrentStationName => GetStationName(ActiveStationIndex);
        public string CurrentStationDescription => GetStationDescription(_activeStation);

        public bool CurrentHasWeight => GetCurrentTwoBone() != null || GetCurrentFABRIK() != null || GetCurrentAim() != null;
        public bool CurrentHasIterations => GetCurrentFABRIK() != null || GetCurrentAim() != null;
        public bool CurrentHasLockRoot => GetCurrentFABRIK() != null;
        public bool CurrentHasMatchTipRotation => GetCurrentTwoBone() != null;
        public bool CurrentHasAimAxis => GetCurrentAim() != null;
        public bool CurrentHasGroundProbe => GetCurrentGroundProbe() != null;

        private void Awake()
        {
            ApplyActiveStationVisibility();
            ApplyCurrentGridPositions();
        }

        public string GetStationName(int index)
        {
            switch ((StationId)Mathf.Clamp(index, 0, StationCount - 1))
            {
                case StationId.TwoBone:
                    return _twoBone.DisplayName;
                case StationId.FABRIK:
                    return _fabrik.DisplayName;
                case StationId.Aim:
                    return _aim.DisplayName;
                case StationId.GroundProbe:
                    return _groundProbe.DisplayName;
                default:
                    return "Station";
            }
        }

        private string GetStationDescription(StationId station)
        {
            switch (station)
            {
                case StationId.TwoBone:
                    return "TwoBoneIK solves a three-joint limb toward a target. The pole controls the bend direction, while weight blends the result and match tip rotation can align the tip to the target.";
                case StationId.FABRIK:
                    return "FABRIKChain solves a longer joint chain from root to tip. Iterations control solve quality, lock root keeps the chain anchored, and an optional pole biases the bend direction.";
                case StationId.Aim:
                    return "AimIK rotates a joint chain so the configured local aim axis points at the target. Use it for turrets, cameras, heads, or other look-at style rigs.";
                case StationId.GroundProbe:
                    return "GroundProbe raycasts downward from an origin and moves its transform to the hit surface. Use it as a foot target for terrain-aware limb placement.";
                default:
                    return string.Empty;
            }
        }

        public void SetActiveStation(int index)
        {
            StationId nextStation = (StationId)Mathf.Clamp(index, 0, StationCount - 1);
            if (_activeStation == nextStation)
            {
                ApplyCurrentGridPositions();
                return;
            }

            _activeStation = nextStation;
            ApplyActiveStationVisibility();
            ApplyCurrentGridPositions();
            SolveCurrent();
        }

        public void SetCurrentTargetCell(int column, int row)
        {
            if (CurrentTargetGrid == null)
            {
                return;
            }

            CurrentTargetGrid.SetCell(column, row);
            SolveCurrent();
        }

        public void SetCurrentTargetNormalizedPosition(Vector2 normalizedPosition)
        {
            if (CurrentTargetGrid == null)
            {
                return;
            }

            CurrentTargetGrid.SetNormalizedPosition(normalizedPosition);
            SolveCurrent();
        }

        public void CenterCurrentTarget()
        {
            if (CurrentTargetGrid == null)
            {
                return;
            }

            CurrentTargetGrid.Center();
            SolveCurrent();
        }

        public void SetCurrentPoleCell(int column, int row)
        {
            if (CurrentPoleGrid == null)
            {
                return;
            }

            CurrentPoleGrid.SetCell(column, row);
            SolveCurrent();
        }

        public void SetCurrentPoleNormalizedPosition(Vector2 normalizedPosition)
        {
            if (CurrentPoleGrid == null)
            {
                return;
            }

            CurrentPoleGrid.SetNormalizedPosition(normalizedPosition);
            SolveCurrent();
        }

        public void CenterCurrentPole()
        {
            if (CurrentPoleGrid == null)
            {
                return;
            }

            CurrentPoleGrid.Center();
            SolveCurrent();
        }

        public float CurrentWeight
        {
            get
            {
                TwoBoneIK twoBone = GetCurrentTwoBone();
                if (twoBone != null)
                {
                    return twoBone.Weight;
                }

                FABRIKChain fabrik = GetCurrentFABRIK();
                if (fabrik != null)
                {
                    return fabrik.Weight;
                }

                AimIK aim = GetCurrentAim();
                return aim != null ? aim.Weight : 0f;
            }
            set
            {
                float clampedValue = Mathf.Clamp01(value);
                TwoBoneIK twoBone = GetCurrentTwoBone();
                if (twoBone != null)
                {
                    twoBone.Weight = clampedValue;
                    SolveCurrent();
                    return;
                }

                FABRIKChain fabrik = GetCurrentFABRIK();
                if (fabrik != null)
                {
                    fabrik.Weight = clampedValue;
                    SolveCurrent();
                    return;
                }

                AimIK aim = GetCurrentAim();
                if (aim != null)
                {
                    aim.Weight = clampedValue;
                    SolveCurrent();
                }
            }
        }

        public bool CurrentMatchTipRotation
        {
            get
            {
                TwoBoneIK twoBone = GetCurrentTwoBone();
                return twoBone != null && twoBone.MatchTipRotation;
            }
            set
            {
                TwoBoneIK twoBone = GetCurrentTwoBone();
                if (twoBone == null)
                {
                    return;
                }

                twoBone.MatchTipRotation = value;
                SolveCurrent();
            }
        }

        public int CurrentIterations
        {
            get
            {
                FABRIKChain fabrik = GetCurrentFABRIK();
                if (fabrik != null)
                {
                    return fabrik.Iterations;
                }

                AimIK aim = GetCurrentAim();
                return aim != null ? aim.Iterations : 1;
            }
            set
            {
                int clampedValue = Mathf.Max(1, value);
                FABRIKChain fabrik = GetCurrentFABRIK();
                if (fabrik != null)
                {
                    fabrik.Iterations = clampedValue;
                    SolveCurrent();
                    return;
                }

                AimIK aim = GetCurrentAim();
                if (aim != null)
                {
                    aim.Iterations = clampedValue;
                    SolveCurrent();
                }
            }
        }

        public bool CurrentLockRoot
        {
            get
            {
                FABRIKChain fabrik = GetCurrentFABRIK();
                return fabrik != null && fabrik.LockRoot;
            }
            set
            {
                FABRIKChain fabrik = GetCurrentFABRIK();
                if (fabrik == null)
                {
                    return;
                }

                fabrik.LockRoot = value;
                SolveCurrent();
            }
        }

        public AimAxisOption CurrentAimAxis
        {
            get
            {
                AimIK aim = GetCurrentAim();
                if (aim == null)
                {
                    return AimAxisOption.Custom;
                }

                Vector3 axis = aim.LocalAimAxis;
                if (IsAxis(axis, Vector3.forward)) return AimAxisOption.Forward;
                if (IsAxis(axis, Vector3.back)) return AimAxisOption.Back;
                if (IsAxis(axis, Vector3.up)) return AimAxisOption.Up;
                if (IsAxis(axis, Vector3.down)) return AimAxisOption.Down;
                if (IsAxis(axis, Vector3.right)) return AimAxisOption.Right;
                if (IsAxis(axis, Vector3.left)) return AimAxisOption.Left;
                return AimAxisOption.Custom;
            }
            set
            {
                AimIK aim = GetCurrentAim();
                if (aim == null || value == AimAxisOption.Custom)
                {
                    return;
                }

                aim.LocalAimAxis = AimAxisToVector(value);
                SolveCurrent();
            }
        }

        public float CurrentGroundRayDistance
        {
            get
            {
                GroundProbe groundProbe = GetCurrentGroundProbe();
                return groundProbe != null ? groundProbe.RayDistance : 0f;
            }
            set
            {
                GroundProbe groundProbe = GetCurrentGroundProbe();
                if (groundProbe != null)
                {
                    groundProbe.RayDistance = value;
                }
            }
        }

        public float CurrentGroundSurfaceOffset
        {
            get
            {
                GroundProbe groundProbe = GetCurrentGroundProbe();
                return groundProbe != null ? groundProbe.SurfaceOffset : 0f;
            }
            set
            {
                GroundProbe groundProbe = GetCurrentGroundProbe();
                if (groundProbe != null)
                {
                    groundProbe.SurfaceOffset = value;
                }
            }
        }

        public bool CurrentGroundAlignToNormal
        {
            get
            {
                GroundProbe groundProbe = GetCurrentGroundProbe();
                return groundProbe != null && groundProbe.AlignToNormal;
            }
            set
            {
                GroundProbe groundProbe = GetCurrentGroundProbe();
                if (groundProbe != null)
                {
                    groundProbe.AlignToNormal = value;
                }
            }
        }

        public float CurrentGroundPositionSmooth
        {
            get
            {
                GroundProbe groundProbe = GetCurrentGroundProbe();
                return groundProbe != null ? groundProbe.PositionSmooth : 0f;
            }
            set
            {
                GroundProbe groundProbe = GetCurrentGroundProbe();
                if (groundProbe != null)
                {
                    groundProbe.PositionSmooth = value;
                }
            }
        }

        public float CurrentGroundRotationSmooth
        {
            get
            {
                GroundProbe groundProbe = GetCurrentGroundProbe();
                return groundProbe != null ? groundProbe.RotationSmooth : 0f;
            }
            set
            {
                GroundProbe groundProbe = GetCurrentGroundProbe();
                if (groundProbe != null)
                {
                    groundProbe.RotationSmooth = value;
                }
            }
        }

        public void SolveCurrent()
        {
            TwoBoneIK twoBone = GetCurrentTwoBone();
            if (twoBone != null)
            {
                twoBone.Solve();
                return;
            }

            FABRIKChain fabrik = GetCurrentFABRIK();
            if (fabrik != null)
            {
                fabrik.Solve();
                return;
            }

            AimIK aim = GetCurrentAim();
            if (aim != null)
            {
                aim.Solve();
            }
        }

        private void ApplyActiveStationVisibility()
        {
            if (!_showOnlyActiveStation)
            {
                return;
            }

            SetStationRootActive(_twoBone.StationRoot, _activeStation == StationId.TwoBone);
            SetStationRootActive(_fabrik.StationRoot, _activeStation == StationId.FABRIK);
            SetStationRootActive(_aim.StationRoot, _activeStation == StationId.Aim);
            SetStationRootActive(_groundProbe.StationRoot, _activeStation == StationId.GroundProbe);
        }

        private void ApplyCurrentGridPositions()
        {
            if (CurrentTargetGrid != null)
            {
                CurrentTargetGrid.ApplyPosition();
            }

            if (CurrentPoleGrid != null)
            {
                CurrentPoleGrid.ApplyPosition();
            }
        }

        private IKDemoTargetGrid GetCurrentTargetGrid()
        {
            switch (_activeStation)
            {
                case StationId.TwoBone:
                    return _twoBone.TargetGrid;
                case StationId.FABRIK:
                    return _fabrik.TargetGrid;
                case StationId.Aim:
                    return _aim.TargetGrid;
                case StationId.GroundProbe:
                    return _groundProbe.TargetGrid;
                default:
                    return null;
            }
        }

        private IKDemoTargetGrid GetCurrentPoleGrid()
        {
            switch (_activeStation)
            {
                case StationId.TwoBone:
                    return _twoBone.PoleGrid;
                case StationId.FABRIK:
                    return _fabrik.PoleGrid;
                default:
                    return null;
            }
        }

        private TwoBoneIK GetCurrentTwoBone()
        {
            switch (_activeStation)
            {
                case StationId.TwoBone:
                    return _twoBone.TwoBoneIK;
                case StationId.GroundProbe:
                    return _groundProbe.TwoBoneIK;
                default:
                    return null;
            }
        }

        private FABRIKChain GetCurrentFABRIK()
        {
            return _activeStation == StationId.FABRIK ? _fabrik.FABRIKChain : null;
        }

        private AimIK GetCurrentAim()
        {
            return _activeStation == StationId.Aim ? _aim.AimIK : null;
        }

        private GroundProbe GetCurrentGroundProbe()
        {
            return _activeStation == StationId.GroundProbe ? _groundProbe.GroundProbe : null;
        }

        private static void SetStationRootActive(GameObject stationRoot, bool active)
        {
            if (stationRoot != null)
            {
                stationRoot.SetActive(active);
            }
        }

        private static string ResolveName(string displayName, string fallback)
        {
            return string.IsNullOrEmpty(displayName) ? fallback : displayName.Trim();
        }

        private static bool IsAxis(Vector3 value, Vector3 axis)
        {
            if (value.sqrMagnitude <= 0.00001f)
            {
                return false;
            }

            return Vector3.Dot(value.normalized, axis.normalized) > 0.999f;
        }

        private static Vector3 AimAxisToVector(AimAxisOption axis)
        {
            switch (axis)
            {
                case AimAxisOption.Forward:
                    return Vector3.forward;
                case AimAxisOption.Back:
                    return Vector3.back;
                case AimAxisOption.Up:
                    return Vector3.up;
                case AimAxisOption.Down:
                    return Vector3.down;
                case AimAxisOption.Right:
                    return Vector3.right;
                case AimAxisOption.Left:
                    return Vector3.left;
                default:
                    return Vector3.forward;
            }
        }
    }
}
