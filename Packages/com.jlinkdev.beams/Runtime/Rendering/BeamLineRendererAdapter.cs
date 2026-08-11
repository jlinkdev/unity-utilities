using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Compatibility renderer that maps strands onto an authored set of Unity Line Renderers.</summary>
    [ExecuteAlways]
    [AddComponentMenu("jlinkdev/Beams/Rendering/Beam Line Renderer Adapter")]
    public sealed class BeamLineRendererAdapter : BeamPathRenderer
    {
        [SerializeField] private LineRenderer primaryRenderer;
        [SerializeField] private LineRenderer[] secondaryRenderers = new LineRenderer[0];
        [SerializeField] private BeamRenderProfile profile;

        private MaterialPropertyBlock propertyBlock;

        public override void Render(BeamPathBuffer paths, in BeamRenderContext context)
        {
            int rendererCount = 1 + (secondaryRenderers != null ? secondaryRenderers.Length : 0);
            for (int rendererIndex = 0; rendererIndex < rendererCount; rendererIndex++)
            {
                LineRenderer line = GetRenderer(rendererIndex);
                if (line == null)
                    continue;

                bool active = rendererIndex < paths.Count && paths[rendererIndex].Count >= 2;
                line.enabled = active;
                if (!active)
                    continue;

                BeamStrand strand = paths[rendererIndex];
                line.useWorldSpace = true;
                line.positionCount = strand.Count;
                for (int i = 0; i < strand.Count; i++)
                    line.SetPosition(i, strand[i].Position);

                if (profile != null)
                {
                    line.widthMultiplier = profile.Width * Mathf.Pow(profile.BranchWidthMultiplier, strand.BranchDepth);
                    line.widthCurve = profile.WidthAlongStrand;
                    line.colorGradient = profile.ColorAlongStrand;
                    line.startColor = profile.Color;
                    line.endColor = profile.Color;
                    if (profile.Material != null)
                        line.sharedMaterial = profile.Material;
                }

                if (propertyBlock == null)
                    propertyBlock = new MaterialPropertyBlock();
                line.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BeamShaderProperties.Color, profile != null ? profile.Color : Color.white);
                propertyBlock.SetFloat(BeamShaderProperties.Intensity, profile != null ? profile.Intensity : 1f);
                propertyBlock.SetFloat(BeamShaderProperties.Length, strand.Length);
                propertyBlock.SetFloat(BeamShaderProperties.Time, context.Time);
                propertyBlock.SetFloat(BeamShaderProperties.Age, context.Age);
                propertyBlock.SetFloat(BeamShaderProperties.Seed, context.Seed + strand.Seed);
                propertyBlock.SetFloat(BeamShaderProperties.Activation, 1f);
                line.SetPropertyBlock(propertyBlock);
            }
        }

        public override void Clear()
        {
            int rendererCount = 1 + (secondaryRenderers != null ? secondaryRenderers.Length : 0);
            for (int i = 0; i < rendererCount; i++)
            {
                LineRenderer line = GetRenderer(i);
                if (line != null)
                {
                    line.positionCount = 0;
                    line.enabled = false;
                }
            }
        }

        private LineRenderer GetRenderer(int index)
        {
            if (index == 0)
                return primaryRenderer;
            int secondaryIndex = index - 1;
            return secondaryRenderers != null && secondaryIndex < secondaryRenderers.Length
                ? secondaryRenderers[secondaryIndex]
                : null;
        }

        private void Reset()
        {
            primaryRenderer = GetComponent<LineRenderer>();
        }
    }
}
