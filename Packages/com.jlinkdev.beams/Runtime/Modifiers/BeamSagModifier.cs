using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Adds a smooth directional bow while preserving strand endpoints.</summary>
    [AddComponentMenu("jlinkdev/Beams/Modifiers/Beam Sag Modifier")]
    public sealed class BeamSagModifier : BeamPathModifier
    {
        [SerializeField] private Vector3 worldDirection = Vector3.down;
        [SerializeField, Min(0f)] private float amount = 0.5f;

        public override void Modify(in BeamPathContext context, BeamPathBuffer paths)
        {
            Vector3 direction = worldDirection.sqrMagnitude > 0.000001f ? worldDirection.normalized : Vector3.down;
            for (int strandIndex = 0; strandIndex < paths.Count; strandIndex++)
            {
                BeamStrand strand = paths[strandIndex];
                for (int i = 1; i < strand.Count - 1; i++)
                {
                    BeamPoint point = strand[i];
                    float envelope = 4f * point.NormalizedDistance * (1f - point.NormalizedDistance);
                    point.Position += direction * (amount * envelope);
                    strand[i] = point;
                }
                BeamPathUtility.RecalculateMetrics(strand, context.Source.Forward);
            }
        }

        private void OnValidate()
        {
            amount = Mathf.Max(0f, amount);
        }
    }
}
