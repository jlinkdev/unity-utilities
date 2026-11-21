using System.Collections.Generic;
using UnityEngine;

namespace jlinkdev.ObjectHighlighting
{
    [ExecuteAlways, DisallowMultipleComponent, AddComponentMenu("Rendering/Object Highlight")]
    public sealed class ObjectHighlight : MonoBehaviour
    {
        public enum State { None, Hovered, Selected }

        [SerializeField, Tooltip("Applied when this instance first enables.")]
        private State initialState;
        [Tooltip("Leave empty to use the renderer feature's hover style.")]
        public HighlightStyle hoverStyle;
        [Tooltip("Leave empty to use the renderer feature's selection style.")]
        public HighlightStyle selectedStyle;

        private static readonly List<ObjectHighlight> active = new List<ObjectHighlight>();
        private readonly List<Renderer> renderers = new List<Renderer>();
        private bool initialized;
        private bool refreshRequired = true;

        public bool IsHovered { get; private set; }
        public bool IsSelected { get; private set; }
        public State CurrentState => IsSelected ? State.Selected : IsHovered ? State.Hovered : State.None;
        internal static IReadOnlyList<ObjectHighlight> Active => active;

        public void SetHovered(bool value)
        {
            Initialize();
            IsHovered = value;
            UpdateRegistration();
        }

        public void SetSelected(bool value)
        {
            Initialize();
            IsSelected = value;
            UpdateRegistration();
        }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            IsHovered = initialState == State.Hovered;
            IsSelected = initialState == State.Selected;
        }

        private void OnEnable()
        {
            Initialize();
            refreshRequired = true;
            UpdateRegistration();
        }

        private void OnDisable() => active.Remove(this);
        private void OnTransformChildrenChanged() => refreshRequired = true;

        private void UpdateRegistration()
        {
            if (isActiveAndEnabled && CurrentState != State.None)
            {
                if (!active.Contains(this))
                {
                    active.Add(this);
                    refreshRequired = true;
                }
            }
            else active.Remove(this);
        }

        /// <summary>Call after replacing renderers or changing a deeper descendant hierarchy while highlighted.</summary>
        public void RefreshRenderers()
        {
            GetComponentsInChildren(true, renderers);
            refreshRequired = false;
        }

        internal List<Renderer> GetRenderers()
        {
            if (refreshRequired) RefreshRenderers();
            return renderers;
        }
    }
}
