using System.Collections.Generic;
using UnityEngine;

namespace jlinkdev.UnityUtilities.InteractiveFog
{
    /// <summary>A vertical capsule that entrains fog. Position is the base of the capsule.</summary>
    [ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(-100)]
    public sealed class FogInteractor : MonoBehaviour
    {
        internal static readonly List<FogInteractor> Active = new();
        [Min(.05f)] public float radius = .35f;
        [Min(0)] public float height = 1.6f;
        [Tooltip("Force coupling. Zero disables this interactor's force and new wake turbulence. Child interactors and optional Clearing are independent; existing wakes continue to dissipate.")]
        [Range(0, 8)] public float drag = 2f;
        [Tooltip("Optional density removal. Leave zero for displacement with natural recovery.")]
        [Range(0, 1)] public float clearing;
        public Vector3 Velocity { get; private set; }
        internal Vector3 Previous { get; private set; }
        Vector3 last;
        void OnEnable() { last = Previous = transform.position; Velocity = Vector3.zero; if (!Active.Contains(this)) Active.Add(this); }
        void OnDisable() { Active.Remove(this); }
        void Update() => SampleMotion(Time.deltaTime);
        /// <summary>Sample a transform driven by an external simulation before advancing the fog.</summary>
        public void SampleMotion(float deltaTime)
        {
            Previous = last;
            Vector3 delta = transform.position - last;
            // A teleport must not carve a line through the entire field.
            if (delta.sqrMagnitude > 9f) Previous = transform.position;
            Velocity = delta.sqrMagnitude > 9f ? Vector3.zero : Vector3.ClampMagnitude(delta / Mathf.Max(deltaTime, .001f), 12f);
            last = transform.position;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Active.Clear();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RestoreRegistrations()
        {
            foreach (var actor in FindObjectsByType<FogInteractor>(FindObjectsSortMode.None))
                if (actor.isActiveAndEnabled && !Active.Contains(actor)) actor.OnEnable();
        }
    }
}
