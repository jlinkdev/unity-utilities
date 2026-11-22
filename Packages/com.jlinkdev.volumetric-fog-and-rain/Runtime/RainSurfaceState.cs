using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain
{
    /// <summary>Retained wetness and bead lifetime for materials driven by an external weather clock.</summary>
    public sealed class RainSurfaceState
    {
        private static readonly int WetnessId = Shader.PropertyToID("_Wetness");
        private static readonly int RainTime = Shader.PropertyToID("_RainTime");
        private static readonly int BeadTime = Shader.PropertyToID("_BeadTime");
        private static readonly int RippleActivity = Shader.PropertyToID("_RippleActivity");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
        private static readonly int FilmSmoothness = Shader.PropertyToID("_FilmSmoothness");
        private MaterialPropertyBlock block;
        private Renderer[] bound;
        private MaterialPropertyBlock[] originals;
        private float beadTime;
        private float rippleActivity;

        public float Wetness { get; private set; }

        public void SetWetness(float wetness) => Wetness = Mathf.Clamp01(wetness);

        public static float Retain(float current, float exposure, float seconds, float wetSeconds, float drySeconds)
        {
            float target = Mathf.Clamp01(exposure);
            float duration = target > current ? wetSeconds : drySeconds;
            return Mathf.MoveTowards(Mathf.Clamp01(current), target, Mathf.Max(0, seconds) / Mathf.Max(.1f, duration));
        }

        public void Advance(float exposure, bool newBeads, float seconds, float wetSeconds, float drySeconds)
        {
            Wetness = Retain(Wetness, exposure, seconds, wetSeconds, drySeconds);
            // Waves settle within a second of the last impacts, even when a wet film remains.
            rippleActivity = Mathf.MoveTowards(rippleActivity, newBeads ? Mathf.Clamp01(exposure) : 0f,
                Mathf.Max(0, seconds) * 1.5f);
            if (newBeads) beadTime += Mathf.Max(0, seconds);
        }

        public void Apply(Renderer[] surfaces, bool glass, float rainTime, float wetDarkening, float wetSmoothness)
        {
            if (!SameRenderers(surfaces))
            {
                Restore();
                if (surfaces == null || surfaces.Length == 0) return;
                bound = (Renderer[])surfaces.Clone();
                originals = new MaterialPropertyBlock[bound.Length];
                for (int i = 0; i < bound.Length; i++)
                {
                    if (!bound[i]) continue;
                    originals[i] = new MaterialPropertyBlock();
                    bound[i].GetPropertyBlock(originals[i]);
                }
            }

            if (bound == null) return;
            block ??= new MaterialPropertyBlock();
            foreach (var surface in bound)
            {
                if (!surface || !surface.sharedMaterial) continue;
                var material = surface.sharedMaterial;
                surface.GetPropertyBlock(block);
                block.SetFloat(WetnessId, Wetness);
                block.SetFloat(RainTime, rainTime);
                if (material.HasProperty(RippleActivity)) block.SetFloat(RippleActivity, rippleActivity);
                // Wet Surface evaluates its own mapped roughness, water film and substrate tint.
                if (glass || material.HasProperty(FilmSmoothness)) block.SetFloat(BeadTime, beadTime);
                else if (material.HasProperty(BaseColor))
                {
                    Color tint = material.GetColor(BaseColor);
                    float brightness = 1 - Mathf.Clamp01(wetDarkening) * Wetness;
                    block.SetColor(BaseColor, new Color(tint.r * brightness, tint.g * brightness, tint.b * brightness, tint.a));
                    if (material.HasProperty(Smoothness))
                        block.SetFloat(Smoothness, Mathf.Lerp(material.GetFloat(Smoothness), wetSmoothness, Wetness));
                }
                surface.SetPropertyBlock(block);
            }
        }

        private bool SameRenderers(Renderer[] surfaces)
        {
            if (surfaces == null || bound == null) return surfaces == bound;
            if (surfaces.Length != bound.Length) return false;
            for (int i = 0; i < surfaces.Length; i++)
                if (surfaces[i] != bound[i]) return false;
            return true;
        }

        /// <summary>Restore each original renderer property block while retaining both simulation clocks.</summary>
        public void Restore()
        {
            if (bound == null) return;
            for (int i = 0; i < bound.Length; i++)
                if (bound[i]) bound[i].SetPropertyBlock(originals[i]);
            bound = null;
            originals = null;
        }

        public void ResetWetness()
        {
            Restore();
            Wetness = 0;
            beadTime = 0;
            rippleActivity = 0;
        }
    }
}
