using System;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    public enum BeamTimeMode
    {
        Scaled = 0,
        Unscaled = 1,
        Manual = 2
    }

    /// <summary>Composes endpoint, path, modifier, and renderer components into a beam.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("jlinkdev/Beams/Beam")]
    public sealed class Beam : MonoBehaviour
    {
        [SerializeField] private BeamEndpointProvider source;
        [SerializeField] private BeamEndpointProvider target;
        [SerializeField] private BeamPathProvider pathProvider;
        [SerializeField] private BeamPathModifier[] modifiers = Array.Empty<BeamPathModifier>();
        [SerializeField] private BeamPathRenderer pathRenderer;
        [SerializeField] private bool beamEnabled = true;
        [SerializeField] private int seed = 1;
        [SerializeField] private BeamTimeMode timeMode = BeamTimeMode.Scaled;
        [SerializeField] private float manualTime;

        [NonSerialized] private BeamPathBuffer paths;
        [NonSerialized] private float enabledAt;
        [NonSerialized] private float previousTime;
        [NonSerialized] private bool wasRendering;

        public BeamEndpoint CurrentSource { get; private set; }
        public BeamEndpoint CurrentTarget { get; private set; }
        public bool HasResolvedPath { get; private set; }

        public event Action<Beam> PathUpdated;
        public event Action<Beam> RenderingStarted;
        public event Action<Beam> RenderingStopped;

        public BeamPathBuffer Paths
        {
            get
            {
                EnsureState();
                return paths;
            }
        }

        public BeamEndpointProvider Source
        {
            get => source;
            set
            {
                source = value;
                Refresh();
            }
        }

        public BeamEndpointProvider Target
        {
            get => target;
            set
            {
                target = value;
                Refresh();
            }
        }

        public BeamPathProvider PathProvider
        {
            get => pathProvider;
            set
            {
                pathProvider = value;
                Refresh();
            }
        }

        public BeamPathRenderer PathRenderer
        {
            get => pathRenderer;
            set
            {
                pathRenderer = value;
                Refresh();
            }
        }

        public BeamPathModifier[] Modifiers
        {
            get => modifiers;
            set
            {
                modifiers = value ?? Array.Empty<BeamPathModifier>();
                Refresh();
            }
        }

        public int Seed
        {
            get => seed;
            set
            {
                seed = value;
                Refresh();
            }
        }

        public BeamTimeMode TimeMode
        {
            get => timeMode;
            set
            {
                timeMode = value;
                previousTime = CurrentTime();
                enabledAt = previousTime;
                Refresh();
            }
        }

        public float ManualTime
        {
            get => manualTime;
            set
            {
                manualTime = value;
                if (timeMode == BeamTimeMode.Manual)
                    Refresh();
            }
        }

        public void RestartAge()
        {
            enabledAt = CurrentTime();
        }

        public bool BeamEnabled
        {
            get => beamEnabled;
            set
            {
                if (beamEnabled == value)
                    return;
                beamEnabled = value;
                Refresh();
            }
        }

        public void Refresh()
        {
            Evaluate();
        }

        private void Reset()
        {
            pathProvider = GetComponent<BeamPathProvider>();
            pathRenderer = GetComponent<BeamPathRenderer>();
            modifiers = GetComponents<BeamPathModifier>();
        }

        private void OnEnable()
        {
            EnsureState();
            enabledAt = CurrentTime();
            previousTime = enabledAt;
            Evaluate();
        }

        private void OnDisable()
        {
            StopRendering();
        }

        private void LateUpdate()
        {
            Evaluate();
        }

        private void Evaluate()
        {
            EnsureState();
            float time = CurrentTime();
            float deltaTime = Mathf.Max(0f, time - previousTime);
            previousTime = time;

            if (!beamEnabled || pathProvider == null || pathRenderer == null ||
                !TryResolveSource(out BeamEndpoint sourceEndpoint) ||
                target == null || !target.TryGetEndpoint(out BeamEndpoint targetEndpoint))
            {
                StopRendering();
                return;
            }

            paths.Clear();
            CurrentSource = sourceEndpoint;
            CurrentTarget = targetEndpoint;
            HasResolvedPath = true;
            BeamPathContext pathContext = new BeamPathContext(sourceEndpoint, targetEndpoint, time, deltaTime, seed);
            pathProvider.BuildPath(pathContext, paths);
            if (modifiers != null)
            {
                for (int i = 0; i < modifiers.Length; i++)
                {
                    BeamPathModifier modifier = modifiers[i];
                    if (modifier != null && modifier.isActiveAndEnabled)
                        modifier.Modify(pathContext, paths);
                }
            }

            BeamRenderContext renderContext = new BeamRenderContext(time, deltaTime, time - enabledAt, seed);
            pathRenderer.Render(paths, renderContext);
            PathUpdated?.Invoke(this);
            if (!wasRendering)
            {
                wasRendering = true;
                RenderingStarted?.Invoke(this);
            }
        }

        private bool TryResolveSource(out BeamEndpoint endpoint)
        {
            if (source != null)
                return source.TryGetEndpoint(out endpoint);

            endpoint = new BeamEndpoint(transform.position, transform.forward, transform);
            return gameObject.activeInHierarchy;
        }

        private void StopRendering()
        {
            if (pathRenderer != null)
                pathRenderer.Clear();
            paths?.Clear();
            HasResolvedPath = false;
            if (!wasRendering)
                return;
            wasRendering = false;
            RenderingStopped?.Invoke(this);
        }

        private void EnsureState()
        {
            if (paths == null)
                paths = new BeamPathBuffer();
            if (modifiers == null)
                modifiers = Array.Empty<BeamPathModifier>();
        }

        private float CurrentTime()
        {
            if (!Application.isPlaying)
                return timeMode == BeamTimeMode.Manual ? manualTime : Time.realtimeSinceStartup;
            switch (timeMode)
            {
                case BeamTimeMode.Unscaled:
                    return Time.unscaledTime;
                case BeamTimeMode.Manual:
                    return manualTime;
                default:
                    return Time.time;
            }
        }
    }
}
