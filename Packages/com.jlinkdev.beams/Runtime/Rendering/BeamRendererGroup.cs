using System;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Fans one path buffer out to multiple renderers for layered or mixed presentations.</summary>
    [AddComponentMenu("jlinkdev/Beams/Rendering/Beam Renderer Group")]
    public sealed class BeamRendererGroup : BeamPathRenderer
    {
        [SerializeField] private BeamPathRenderer[] renderers = Array.Empty<BeamPathRenderer>();

        public BeamPathRenderer[] Renderers
        {
            get => renderers;
            set => renderers = value ?? Array.Empty<BeamPathRenderer>();
        }

        public override void Render(BeamPathBuffer paths, in BeamRenderContext context)
        {
            if (renderers == null)
                return;
            for (int i = 0; i < renderers.Length; i++)
            {
                BeamPathRenderer renderer = renderers[i];
                if (renderer != null && renderer != this && renderer.isActiveAndEnabled)
                    renderer.Render(paths, context);
            }
        }

        public override void Clear()
        {
            if (renderers == null)
                return;
            for (int i = 0; i < renderers.Length; i++)
            {
                BeamPathRenderer renderer = renderers[i];
                if (renderer != null && renderer != this)
                    renderer.Clear();
            }
        }
    }
}
