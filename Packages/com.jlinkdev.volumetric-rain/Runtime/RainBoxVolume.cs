using System.Collections.Generic;
using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricRain
{
    /// <summary>World-space mask for the shared rain field; does not create or simulate drops.</summary>
    [ExecuteAlways]
    public abstract class RainBoxVolume : MonoBehaviour
    {
        public Vector3 center;
        public Vector3 size = new Vector3(20, 10, 20);
        internal static readonly List<RainBoxVolume> Active = new List<RainBoxVolume>();
        internal abstract bool ExcludesRain { get; }

        public Matrix4x4 BoxToWorld => transform.localToWorldMatrix * Matrix4x4.TRS(center, Quaternion.identity,
            new Vector3(Mathf.Max(0.001f, size.x), Mathf.Max(0.001f, size.y), Mathf.Max(0.001f, size.z)));

        public Bounds WorldBounds
        {
            get
            {
                var m = BoxToWorld;
                var x = m.MultiplyVector(Vector3.right * 0.5f);
                var y = m.MultiplyVector(Vector3.up * 0.5f);
                var z = m.MultiplyVector(Vector3.forward * 0.5f);
                var extent = new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),
                    Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y), Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));
                return new Bounds(m.MultiplyPoint3x4(Vector3.zero), extent * 2);
            }
        }

        protected virtual void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
        protected virtual void OnDisable() => Active.Remove(this);
        protected virtual void OnDestroy() => Active.Remove(this);
        protected virtual void OnValidate()
        {
            size = new Vector3(Mathf.Max(0.001f,size.x),Mathf.Max(0.001f,size.y),Mathf.Max(0.001f,size.z));
        }
        private void OnDrawGizmos()
        {
            var oldMatrix = Gizmos.matrix;
            var oldColor = Gizmos.color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = ExcludesRain ? new Color(1,0.55f,0.15f,0.65f) : new Color(0.15f,0.7f,1,0.65f);
            Gizmos.DrawWireCube(center,size);
            Gizmos.matrix = oldMatrix;
            Gizmos.color = oldColor;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RebuildRegistry()
        {
            Active.Clear();
            foreach (var box in FindObjectsByType<RainBoxVolume>(FindObjectsSortMode.None))
                if (box.isActiveAndEnabled) Active.Add(box);
        }
    }
}
