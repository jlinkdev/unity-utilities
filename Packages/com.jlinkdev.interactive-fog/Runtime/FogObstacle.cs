using System.Collections.Generic;
using UnityEngine;

namespace jlinkdev.UnityUtilities.InteractiveFog
{
    /// <summary>An oriented solid box. Geometry must also be rendered to occlude the final image.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class FogObstacle : MonoBehaviour
    {
        internal static readonly List<FogObstacle> Active = new();
        public Vector3 size = Vector3.one;
        public Vector3 center;
        internal Matrix4x4 WorldToBox => Matrix4x4.Scale(new Vector3(1 / Mathf.Max(.001f, size.x), 1 / Mathf.Max(.001f, size.y), 1 / Mathf.Max(.001f, size.z))) * Matrix4x4.Translate(-center) * transform.worldToLocalMatrix;
        void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
        void OnDisable() => Active.Remove(this);
        void OnDrawGizmosSelected() { Gizmos.matrix = transform.localToWorldMatrix; Gizmos.color = Color.cyan; Gizmos.DrawWireCube(center, size); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Active.Clear();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RestoreRegistrations()
        {
            foreach (var obstacle in FindObjectsByType<FogObstacle>(FindObjectsSortMode.None))
                if (obstacle.isActiveAndEnabled && !Active.Contains(obstacle)) Active.Add(obstacle);
        }
    }
}
