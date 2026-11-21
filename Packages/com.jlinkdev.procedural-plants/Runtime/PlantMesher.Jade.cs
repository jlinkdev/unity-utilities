using UnityEngine;

namespace JLink.Plants
{
    public sealed partial class PlantMesher
    {
        void Jade(PlantOrgan node, float elapsed)
        {
            var tip = node.origin + node.direction * (node.length * PlantOrgan.StemExtension(elapsed));
            float wood = Mathf.Clamp01(elapsed / 19f);
            float radius = Mathf.Lerp(.0048f, .014f, wood) / (1 + .055f * node.depth);
            var bark = Color.Lerp(new Color(.43f,.57f,.32f), new Color(.49f,.39f,.31f), wood);
            Tube(node.origin, tip, radius, radius * .91f, bark);

            var radial = Vector3.ProjectOnPlane(node.outward, node.direction).normalized;
            float growth = Mature(elapsed, 3.6f);
            var color = Color.Lerp(new Color(.20f,.48f,.32f), new Color(.37f,.62f,.40f), node.variation);
            JadeLeaf(tip, radial, node, growth, 1f, color);
            JadeLeaf(tip, -radial, node, growth, Mathf.Lerp(.91f, 1.05f, node.variation), color);
        }

        void JadeLeaf(Vector3 tip, Vector3 radial, PlantOrgan node, float growth, float size, Color color)
        {
            var root = tip + radial * .002f;
            float length = node.size * size * growth * Mathf.Lerp(.076f, .106f, node.variation);
            var axis = (radial + node.direction * Mathf.Lerp(2.4f, .32f + .28f * node.variation, growth)).normalized;
            var across = Vector3.Cross(radial, node.direction).normalized;
            var face = Vector3.Cross(across, axis).normalized;
            Vector3 Spine(float t) => root + length * (axis * t + face * (.13f*t*t - .04f*Mathf.Sin(Mathf.PI*t)));
            int rows = Steps(8, 14, 22), start = vertices.Count;
            var rails = Rails();
            const float pattern = 8;
            Vertex(root, .5f, 0, color, pattern, node.variation);
            for (int row = 1; row < rows; row++)
            {
                float t = row / (float)rows;
                // Obovate section, narrow at the stem and fleshy around its rounded outer end.
                float width = length * .35f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(t, 1.38f)), .62f);
                Section(rails, Spine(t), across, face, Vector3.zero, width, -.055f * width,
                    .014f * width, .57f * width, 0, t, color, pattern, node.variation);
            }
            Vertex(Spine(1), .5f, 1, color, pattern, node.variation);
            BladeFaces(start, rows, rails.Length);
        }
    }
}
