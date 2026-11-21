using UnityEngine;
using UnityEngine.Rendering;

namespace jlinkdev.UnityUtilities.WaterSystem
{
    /// <summary>An optional suspended-particle cloud clipped to the actual animated water volume.</summary>
    [AddComponentMenu("jlinkdev/Water System/Water Particles")]
    [DisallowMultipleComponent]
    public sealed class WaterParticles : MonoBehaviour
    {
        [SerializeField] private WaterBody bodyOverride;
        [SerializeField] private Transform followTarget;
        [SerializeField, Range(0, 4096)] private int count = 128;
        [SerializeField, Tooltip("World-space half extents of the particle cloud.")]
        private Vector3 radius = new Vector3(8f, 5f, 8f);
        [SerializeField, Min(0.001f)] private float particleSize = 0.035f;
        [SerializeField] private Vector3 drift = new Vector3(0.025f, 0.045f, 0.015f);
        [SerializeField] private Color color = new Color(0.65f, 0.9f, 0.9f, 0.3f);

        private ParticleSystem system;
        private Material material;
        private ParticleSystem.Particle[] particles;
        private Vector3[] positions;
        private bool[] valid;
        private System.Random random;

        public WaterBody BodyOverride { get => bodyOverride; set => bodyOverride = value; }
        public Transform FollowTarget { get => followTarget; set => followTarget = value; }
        public int Count { get => Mathf.Clamp(count, 0, 4096); set => count = Mathf.Clamp(value, 0, 4096); }
        public Vector3 Radius { get => new Vector3(Extent(radius.x), Extent(radius.y), Extent(radius.z)); set => radius = value; }
        public float ParticleSize { get => Mathf.Max(0.001f, WaterWaves.Finite(particleSize, 0.035f)); set => particleSize = value; }
        public Vector3 Drift { get => drift; set => drift = value; }
        public Color Color { get => color; set => color = value; }
        public int ActiveParticleCount { get; private set; }
        public ParticleSystem ParticleSystem => system;

        private static float Extent(float value) => Mathf.Max(0.01f, WaterWaves.Finite(value, 1f));

        private void OnEnable()
        {
            Shader shader = Resources.Load<Shader>("WaterParticles");
            if (shader == null) shader = Shader.Find("jlinkdev/Water System/Particles");
            if (shader == null) return;
            material = new Material(shader) { name = "Water Particles (Runtime)", hideFlags = HideFlags.HideAndDontSave };
            GameObject child = new GameObject("Water Particles (Runtime)") { hideFlags = HideFlags.HideAndDontSave };
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            system = child.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startSpeed = 0f;
            main.maxParticles = Mathf.Max(1, Count);
            var emission = system.emission;
            emission.enabled = false;
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            system.Play(false);
            system.Pause(false);
            random = new System.Random(18967);
            Refresh(0f);
        }

        private void LateUpdate() => Refresh(Time.deltaTime);

        /// <summary>Updates the cloud immediately. Useful after moving the follow target.</summary>
        public void Refresh(float deltaTime = 0f)
        {
            ActiveParticleCount = 0;
            if (system == null) return;
            if (particles == null || particles.Length != Count)
            {
                particles = new ParticleSystem.Particle[Count];
                positions = new Vector3[Count];
                valid = new bool[Count];
                var main = system.main;
                main.maxParticles = Mathf.Max(1, Count);
            }

            Vector3 anchor = followTarget != null ? followTarget.position : transform.position;
            WaterBody body = WaterInteraction.FindBody(anchor, bodyOverride);
            Vector3 extents = Radius;
            Bounds cloud = new Bounds(anchor, extents * 2f);
            if (body == null || !cloud.Intersects(body.WorldBounds))
            {
                system.SetParticles(particles, 0);
                return;
            }
            Vector3 movement = WaterInteraction.IsFinite(drift) ? drift * Mathf.Clamp(WaterWaves.Finite(deltaTime), 0f, 0.1f) : Vector3.zero;
            float time = body.SurfaceTime;
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 point = positions[i] + movement;
                bool usable = valid[i] && cloud.Contains(point) && body.Contains(point, time);
                for (int attempt = 0; !usable && attempt < 8; attempt++)
                {
                    point = anchor + new Vector3(RandomSigned() * extents.x, RandomSigned() * extents.y, RandomSigned() * extents.z);
                    usable = body.Contains(point, time);
                }
                positions[i] = point;
                valid[i] = usable;
                if (!usable) continue;
                particles[ActiveParticleCount++] = new ParticleSystem.Particle
                {
                    position = point,
                    startColor = color,
                    startSize = ParticleSize,
                    startLifetime = 100000f,
                    remainingLifetime = 100000f,
                    randomSeed = (uint)(i + 1)
                };
            }
            system.SetParticles(particles, ActiveParticleCount);
        }

        private float RandomSigned() => (float)random.NextDouble() * 2f - 1f;

        private void OnDisable()
        {
            if (system != null) CoreUtils.Destroy(system.gameObject);
            CoreUtils.Destroy(material);
            system = null;
            material = null;
            particles = null;
            positions = null;
            valid = null;
            ActiveParticleCount = 0;
        }
    }
}
