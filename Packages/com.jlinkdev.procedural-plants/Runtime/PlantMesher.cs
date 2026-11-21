using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace JLink.Plants
{
    public enum LeafDetail { Simple, Standard, CloseUp }

    /// <summary>Writes plant-local geometry into a caller-owned, readable mesh.</summary>
    public sealed partial class PlantMesher
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<Vector2> patterns = new List<Vector2>();
        readonly List<Color32> colors = new List<Color32>();
        readonly List<int> stems = new List<int>();
        readonly List<int> foliage = new List<int>();
        static readonly Color Stem = new Color(.37f, .48f, .19f);
        static readonly Color FernStem = new Color(.54f, .57f, .23f);

        public void Write(PlantGrowth plant, Mesh mesh, LeafDetail leafDetail = LeafDetail.Standard) =>
            Write(plant,mesh,leafDetail,default);

        public void Write(PlantGrowth plant, Mesh mesh, LeafDetail leafDetail, PlantCare care)
        {
            if (plant == null || mesh == null) throw new System.ArgumentNullException();
            detail = leafDetail;
            blades.Clear();
            float age = (float)plant.Age;
            vertices.Clear();
            uvs.Clear();
            patterns.Clear();
            colors.Clear();
            stems.Clear();
            foliage.Clear();
            foreach (var organ in plant.Organs)
            {
                float elapsed = age - organ.birth;
                switch (plant.Recipe.species)
                {
                    case PlantSpecies.Pothos: Vine(organ, elapsed); break;
                    case PlantSpecies.SnakePlant: Sword(organ, elapsed); break;
                    case PlantSpecies.Fern: Frond(organ, elapsed); break;
                    case PlantSpecies.Monstera: Monstera(organ, elapsed); break;
                    case PlantSpecies.SpiderPlant: Spider(plant,organ,elapsed); break;
                    case PlantSpecies.RubberPlant: Rubber(plant,organ,elapsed); break;
                    case PlantSpecies.JadePlant: Jade(organ,elapsed); break;
                }
            }

            mesh.Clear();
            mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            UploadLeafPose(mesh, care.Droop(plant.Recipe.species));
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(2, patterns);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(stems, 0, false);
            mesh.SetTriangles(foliage, 1, false);
            RecalculateSurface(mesh);
        }

        void Vertex(Vector3 p, float u, float v, Color color, float pattern = 0, float variation = 0)
        {
            vertices.Add(p);
            uvs.Add(new Vector2(u, v));
            colors.Add(color);
            patterns.Add(new Vector2(pattern, variation));
        }

        static void Quad(List<int> list, int a, int b, int c, int d, bool reverse = false)
        {
            if (reverse) { list.Add(a); list.Add(c); list.Add(b); list.Add(a); list.Add(d); list.Add(c); }
            else { list.Add(a); list.Add(b); list.Add(c); list.Add(a); list.Add(c); list.Add(d); }
        }

        void Tube(Vector3 a, Vector3 b, float radiusA, float radiusB, Color color)
        {
            var d = (b - a).normalized;
            if (d.sqrMagnitude < .1f) return;
            var refAxis = Mathf.Abs(d.y) < .92f ? Vector3.up : Vector3.forward;
            var x = Vector3.Cross(d, refAxis).normalized;
            var y = Vector3.Cross(d, x);
            int start = vertices.Count;
            const int sides = 7;
            for (int j = 0; j <= sides; j++)
            {
                float u = j / (float)sides;
                float angle = u * 2 * Mathf.PI;
                var radial = Mathf.Cos(angle) * x + Mathf.Sin(angle) * y;
                Vertex(a + radial * radiusA, u, 0, color);
                Vertex(b + radial * radiusB, u, 1, color);
                if (j > 0)
                {
                    int k = start + (j - 1) * 2;
                    Quad(stems, k, k + 2, k + 3, k + 1);
                }
            }
        }

        static float Mature(float elapsed, float duration) =>
            Mathf.SmoothStep(.06f, 1, Mathf.Clamp01(elapsed / duration));

        // Interior rails sample the rib and the shoulder of the blade. Edge vertices
        // belong to both faces. Each end is a single pole rather than a collapsed row.
        static readonly float[] FiveRails = { -1, -.15f, 0, .15f, 1 };
        static readonly float[] SevenRails = { -1, -.58f, -.10f, 0, .10f, .58f, 1 };
        static readonly float[] CloseRails = { -1, -.78f, -.49f, -.18f, -.055f, 0, .055f, .18f, .49f, .78f, 1 };
        LeafDetail detail;

        float[] Rails(bool small = false)
        {
            if (small) return detail == LeafDetail.CloseUp ? SevenRails : FiveRails;
            return detail == LeafDetail.Simple ? FiveRails : detail == LeafDetail.CloseUp ? CloseRails : SevenRails;
        }

        int Steps(int simple, int standard, int close) =>
            detail == LeafDetail.Simple ? simple : detail == LeafDetail.CloseUp ? close : standard;

        // Each section is a closed ring, traversing the upper surface then the underside.
        // The fold bends both surfaces. Thickness tapers continuously to zero at the margins.
        void Section(float[] rails, Vector3 center, Vector3 side, Vector3 face, Vector3 lobeOffset,
            float width, float fold, float rib, float thickness, float asymmetry, float t,
            Color color, float pattern, float variation)
        {
            int count = 2 * rails.Length - 2;
            for (int i = 0; i < count; i++)
            {
                bool upper = i < rails.Length;
                float s = rails[upper ? i : count - i];
                float edge = Mathf.Abs(s);
                float central = Mathf.Max(0, 1f - edge * 6f);
                float relief = rib * central + fold * s * s;
                float skin = thickness * (1f - s * s) * (upper ? .5f : -.5f);
                var p = center + side * (width * s * (1 + s * asymmetry)) +
                    face * (relief + skin) + lobeOffset * edge * edge;
                Vertex(p, (s + 1) * .5f, t, color, pattern, variation);
            }
        }

        void BladeFaces(int root, int rows, int rails)
        {
            RecordBlade(root, vertices[root]);
            int count = 2 * rails - 2;
            int first = root + 1;
            int last = first + (rows - 2) * count;
            int tip = last + count;
            for (int j = 0; j < count; j++)
            {
                int next = (j + 1) % count;
                foliage.Add(root); foliage.Add(first + next); foliage.Add(first + j);
                for (int row = first; row < last; row += count)
                    Quad(foliage, row + j, row + next, row + count + next, row + count + j);
                foliage.Add(last + j); foliage.Add(last + next); foliage.Add(tip);
            }
        }

        Vector3 CurvedInternode(PlantOrgan o, float extension)
        {
            // A cubic Hermite segment shares its endpoint and tangent with the next internode.
            var quadratic = o.Quadratic;
            var cubic = o.Cubic;
            int start = vertices.Count;
            const int sides = 7, spans = 4;
            Vector3 center = o.origin;
            for (int k = 0; k <= spans; k++)
            {
                float t = extension * k / spans;
                center = o.StemPosition(t);
                var tangent = (o.startTangent + t * (2 * quadratic + 3 * t * cubic)).normalized;
                var frame = Mathf.Abs(tangent.z) > .98f ? Vector3.right : Vector3.forward;
                var side = Vector3.Cross(tangent, frame).normalized;
                var up = Vector3.Cross(tangent, side);
                float radius = Mathf.Lerp(.0046f, .0025f, (o.depth + t) / 22f);
                for (int j = 0; j <= sides; j++)
                {
                    float u = j / (float)sides;
                    float angle = 2 * Mathf.PI * u;
                    Vertex(center + radius * (side * Mathf.Cos(angle) + up * Mathf.Sin(angle)), u, t, Stem);
                    if (j == 0 || k == 0) continue;
                    int b = start + k * (sides + 1) + j;
                    int a = b - sides - 1;
                    Quad(stems, a - 1, a, b, b - 1);
                }
            }
            return center;
        }

        void Vine(PlantOrgan o, float elapsed)
        {
            float extension = PlantOrgan.StemExtension(elapsed);
            var tip = o.origin + o.direction * (o.length * extension);
            if (o.shootId > 0) tip = CurvedInternode(o, extension);
            else Tube(o.origin - o.direction * .001f, tip, .0048f, .0038f, Stem);
            float growth = Mature(elapsed, 3.4f);
            float pitch = o.shootId == 0 ? -.12f : Mathf.Lerp(-.52f, .42f, o.variation);
            var axis = (o.outward * .82f + Vector3.up * Mathf.Lerp(1.8f, pitch, growth)).normalized;
            float petiole = o.shootId == 0 ? .036f : Mathf.Lerp(.03f, .064f, o.variation);
            var socket = tip + (o.outward + Vector3.up * .6f) * (petiole * growth);
            Tube(tip, socket, .0026f, .0018f, Stem);
            float length = .188f * o.size * growth;
            var side = Quaternion.AngleAxis((o.variation - .5f) * 52f, axis) *
                Vector3.Cross(o.outward, Vector3.up).normalized;
            var face = Vector3.Cross(side, axis).normalized;
            Color color = Color.Lerp(new Color(.15f, .41f, .23f), new Color(.39f, .57f, .20f), o.variation);
            var rails = Rails();
            int rows = Steps(10, 18, 28);
            int start = vertices.Count;
            Vertex(socket, .5f, 0, color, 1, o.variation);
            for (int r = 1; r < rows; r++)
            {
                float t = r / (float)rows;
                float width = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(t, .62f)), .82f) * length * .49f;
                var midrib = socket + axis * (length * t) + Vector3.down * (length * .78f * t * t);
                float lobe = length * .39f * Mathf.Exp(-Mathf.Pow((t - .17f) / .17f, 2));
                // The basal lobes extend behind the petiole. Soft waves break up the margin.
                float wave = .065f * Mathf.Sin(t * 17f + o.variation * 3);
                Section(rails, midrib, side, face, -axis * lobe, width * Mathf.Lerp(.20f, 1, growth),
                    width * (Mathf.Lerp(1.15f, .19f, growth) + wave), width * .067f,
                    width * .033f, (o.variation - .5f) * .17f, t, color, 1, o.variation);
            }
            Vertex(socket + axis * length + Vector3.down * (length * .78f), .5f, 1, color, 1, o.variation);
            BladeFaces(start, rows, rails.Length);
        }

        void Sword(PlantOrgan o, float elapsed)
        {
            float growth = Mature(elapsed, 5);
            float length = o.length * growth;
            var side = Vector3.Cross(Vector3.up, o.outward).normalized;
            Color color = Color.Lerp(new Color(.17f, .37f, .25f), new Color(.28f, .47f, .30f), o.variation);
            var rails = Rails();
            int start = vertices.Count, rows = Steps(12, 22, 34);
            Vertex(o.origin, .5f, 0, color, 2, o.variation);
            for (int r = 1; r < rows; r++)
            {
                float t = r / (float)rows;
                var lateral = Quaternion.AngleAxis((o.variation - .35f) * 66 * t, o.direction) * side;
                var face = Vector3.Cross(lateral, o.direction).normalized;
                float width = .037f * o.size * Mathf.Sqrt(growth) * (.38f + .62f * Mathf.Sin(t * 2.5f)) *
                    (1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.66f, 1f, t)));
                var center = o.origin + o.direction * (length * t) + o.outward * (length * .12f * t * t);
                Section(rails, center, lateral, face, Vector3.zero, width, width * .40f,
                    width * .09f, width * Mathf.Lerp(.27f, .11f, t), 0, t, color, 2, o.variation);
            }
            Vertex(o.origin + o.direction * length + o.outward * (length * .12f), .5f, 1, color, 2, o.variation);
            BladeFaces(start, rows, rails.Length);
        }

        static Vector3 Rachis(PlantOrgan o, float t, float growth)
        {
            float length = o.length * growth;
            return o.origin + length * (o.direction * t + o.outward * (.28f * t * t) +
                Vector3.down * (.82f * t * t * t));
        }

        void Frond(PlantOrgan o, float elapsed)
        {
            float growth = Mature(elapsed, 5.5f);
            var side = Vector3.Cross(Vector3.up, o.outward).normalized;
            var previous = o.origin;
            Color color = Color.Lerp(new Color(.20f, .44f, .20f), new Color(.45f, .61f, .22f), o.variation);
            const int pairs = 13;
            for (int i = 1; i <= pairs; i++)
            {
                float t = i / (float)pairs;
                var basePoint = Rachis(o, t, growth);
                Tube(previous, basePoint, Mathf.Lerp(.0035f, .001f, t), Mathf.Lerp(.0033f, .0006f, t), FernStem);
                previous = basePoint;
                // The terminal pair is small but keeps its shape at all detail levels.
                float pinna = Mathf.Pow(Mathf.Max(.13f, Mathf.Sin(Mathf.PI * Mathf.Pow(t, .67f))), .68f);
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    float stagger = t - (sign < 0 ? .028f : .045f);
                    var root = Rachis(o, stagger, growth);
                    var forward = (Rachis(o, stagger + .015f, growth) - root).normalized;
                    var axis = (sign * side * Mathf.Lerp(.28f, 1, growth) + forward * .56f +
                        Vector3.up * ((1 - growth) * .8f)).normalized;
                    var across = Vector3.ProjectOnPlane(forward, axis).normalized;
                    if (Vector3.Cross(across, axis).y < 0) across = -across;
                    Leaflet(root, axis, across, o.length * .28f * pinna * growth, color, o.variation);
                }
            }
        }

        void Leaflet(Vector3 root, Vector3 axis, Vector3 across, float length, Color color, float variation)
        {
            var rails = Rails(true);
            int start = vertices.Count, rows = Steps(8, 12, 20);
            var face = Vector3.Cross(across, axis).normalized;
            Vertex(root, .5f, 0, color, 3, variation);
            for (int r = 1; r < rows; r++)
            {
                float t = r / (float)rows;
                float serration = .84f + .16f * Mathf.Cos(8 * Mathf.PI * t);
                float width = length * .215f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, .74f)) * serration;
                var center = root + axis * (length * t) + Vector3.down * (length * .29f * t * t);
                Section(rails, center, across, face, -axis * (width * .13f), width, -width * .22f,
                    width * .07f, width * .048f, .055f, t, color, 3, variation);
            }
            Vertex(root + axis * length + Vector3.down * (length * .29f), .5f, 1, color, 3, variation);
            BladeFaces(start, rows, rails.Length);
        }
    }
}
