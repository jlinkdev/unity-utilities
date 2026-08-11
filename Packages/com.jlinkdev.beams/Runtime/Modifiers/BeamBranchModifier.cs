using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Adds deterministic secondary strands suitable for lightning forks and other branching presentations.</summary>
    [AddComponentMenu("jlinkdev/Beams/Modifiers/Beam Branch Modifier")]
    public sealed class BeamBranchModifier : BeamPathModifier
    {
        [SerializeField, Range(0, 32)] private int branchesPerStrand = 3;
        [SerializeField] private Vector2 attachmentRange = new Vector2(0.15f, 0.85f);
        [SerializeField] private Vector2 lengthRange = new Vector2(0.5f, 1.5f);
        [SerializeField] private Vector2 angleRange = new Vector2(25f, 75f);
        [SerializeField, Range(2, 64)] private int pointCount = 6;
        [SerializeField] private bool branchFromSecondaryStrands;
        [SerializeField] private int seedOffset;

        public override void Modify(in BeamPathContext context, BeamPathBuffer paths)
        {
            int sourceStrandCount = branchFromSecondaryStrands ? paths.Count : Mathf.Min(1, paths.Count);
            for (int strandIndex = 0; strandIndex < sourceStrandCount; strandIndex++)
            {
                BeamStrand parent = paths[strandIndex];
                if (parent.Count < 2)
                    continue;

                for (int branchIndex = 0; branchIndex < branchesPerStrand; branchIndex++)
                    AddBranch(context, paths, parent, strandIndex, branchIndex);
            }
        }

        private void AddBranch(
            in BeamPathContext context,
            BeamPathBuffer paths,
            BeamStrand parent,
            int parentIndex,
            int branchIndex)
        {
            int hashSeed = context.Seed + seedOffset + parent.Seed * 31 + branchIndex * 1013;
            float attachment = Mathf.Lerp(attachmentRange.x, attachmentRange.y, Hash01(hashSeed));
            float length = Mathf.Lerp(lengthRange.x, lengthRange.y, Hash01(hashSeed + 1));
            float angle = Mathf.Lerp(angleRange.x, angleRange.y, Hash01(hashSeed + 2)) * Mathf.Deg2Rad;
            float radialAngle = Hash01(hashSeed + 3) * Mathf.PI * 2f;
            BeamPoint root = BeamPathUtility.Evaluate(parent, attachment);
            Vector3 binormal = Vector3.Cross(root.Tangent, root.Normal).normalized;
            Vector3 radial = root.Normal * Mathf.Cos(radialAngle) + binormal * Mathf.Sin(radialAngle);
            Vector3 direction = (root.Tangent * Mathf.Cos(angle) + radial * Mathf.Sin(angle)).normalized;

            BeamStrand branch = paths.AddStrand(parentIndex, attachment, hashSeed);
            int count = Mathf.Clamp(pointCount, 2, 64);
            float bend = (Hash01(hashSeed + 4) * 2f - 1f) * length * 0.2f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (count - 1f);
                Vector3 position = root.Position + direction * (length * t) + radial * (bend * Mathf.Sin(Mathf.PI * t));
                branch.Add(position);
            }
            BeamPathUtility.RecalculateMetrics(branch, root.Normal);
        }

        private static float Hash01(int value)
        {
            unchecked
            {
                uint hash = (uint)value;
                hash ^= hash >> 16;
                hash *= 0x7feb352d;
                hash ^= hash >> 15;
                hash *= 0x846ca68b;
                hash ^= hash >> 16;
                return (hash & 0x00ffffff) / 16777215f;
            }
        }

        private void OnValidate()
        {
            branchesPerStrand = Mathf.Clamp(branchesPerStrand, 0, 32);
            attachmentRange.x = Mathf.Clamp01(attachmentRange.x);
            attachmentRange.y = Mathf.Clamp(attachmentRange.y, attachmentRange.x, 1f);
            lengthRange.x = Mathf.Max(0f, lengthRange.x);
            lengthRange.y = Mathf.Max(lengthRange.x, lengthRange.y);
            angleRange.x = Mathf.Clamp(angleRange.x, 0f, 180f);
            angleRange.y = Mathf.Clamp(angleRange.y, angleRange.x, 180f);
            pointCount = Mathf.Clamp(pointCount, 2, 64);
        }
    }
}
