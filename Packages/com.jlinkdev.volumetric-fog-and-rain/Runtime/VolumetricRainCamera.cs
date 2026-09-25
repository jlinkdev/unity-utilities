using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    [AddComponentMenu("jlinkdev/Volumetric Fog and Rain/Volumetric Rain Camera")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "jlinkdev.UnityUtilities.VolumetricRain", "jlinkdev.VolumetricRain", null)]
    public sealed class VolumetricRainCamera : MonoBehaviour
    {
        public RainProfile profile;
        [Range(0, 1)] public float intensity = 1;
        public RainDebugView debugView;
        [Tooltip("Use a fixed absolute time to inspect parallax without moving the field.")]
        public bool freezeTime;
        public float fixedTime;
        public bool useUnscaledTime;

        /// <summary>Assign runtime settings to override Profile. Null uses the asset again.</summary>
        public RainSettings RuntimeSettings { get; set; }
        private readonly RainSettings profileSnapshot = new RainSettings();
        private double lastFogTime, fogX, fogY, fogZ;
        private bool hasFogTime;

        public RainSettings ResolveSettings()
        {
            if (RuntimeSettings != null) return RuntimeSettings;
            if (profile == null) return null;
            profileSnapshot.CopyFrom(profile);
            return profileSnapshot;
        }

        public RainSettings CreateRuntimeSettings()
        {
            RuntimeSettings = profile != null ? new RainSettings(profile) : new RainSettings();
            return RuntimeSettings;
        }

        public void ResetFogMotion()
        {
            hasFogTime = false;
            fogX = fogY = fogZ = 0;
        }

        internal Vector3 EvaluateFogOffset(Vector3 velocity, float scale, double time)
        {
            double dt = hasFogTime ? time - lastFogTime : time;
            fogX += velocity.x * dt; fogY += velocity.y * dt; fogZ += velocity.z * dt;
            lastFogTime = time; hasFogTime = true;
            // Noise hashes repeat every 65536 cells. Wrap in double precision before uploading.
            return new Vector3(Wrap(fogX * scale), Wrap(fogY * scale), Wrap(fogZ * scale));
        }
        private static float Wrap(double x) => (float)(x - System.Math.Floor((x + 32768.0) / 65536.0) * 65536.0);

        public double EvaluationTime => freezeTime ? fixedTime :
            (useUnscaledTime || !Application.isPlaying ? Time.realtimeSinceStartupAsDouble : Time.timeAsDouble);
    }
}
