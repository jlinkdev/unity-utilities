using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricRain
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    [AddComponentMenu("jlinkdev/Volumetric Rain/Volumetric Rain Camera")]
    public sealed class VolumetricRainCamera : MonoBehaviour
    {
        public RainProfile profile;
        [Range(0, 1)] public float intensity = 1;
        public RainDebugView debugView;
        [Tooltip("Use a fixed absolute time to inspect parallax without moving the field.")]
        public bool freezeTime;
        public float fixedTime;
        public bool useUnscaledTime;

        public double EvaluationTime => freezeTime ? fixedTime :
            (useUnscaledTime || !Application.isPlaying ? Time.realtimeSinceStartupAsDouble : Time.timeAsDouble);
    }
}
