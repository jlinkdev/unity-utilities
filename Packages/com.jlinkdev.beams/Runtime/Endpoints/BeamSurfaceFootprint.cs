using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Drapes a small source-UV grid over the beam's hit collider, without a decal renderer feature.</summary>
    [ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class BeamSurfaceFootprint : MonoBehaviour
    {
        private const int Cells = 6, Side = Cells + 1;
        private readonly Vector3[] vertices = new Vector3[Side * Side];
        private readonly bool[] hits = new bool[Side * Side];
        private readonly int[] indices = new int[Cells * Cells * 6];
        private Mesh mesh, original;
        private MeshFilter filter;
        private Beam beam;
        private Collider previous;
        private Matrix4x4 lastPose, lastColliderPose;

        private void OnEnable()
        {
            beam = GetComponentInParent<Beam>();
            filter = GetComponent<MeshFilter>();
            original = filter.sharedMesh;
            mesh = new Mesh { name = "Beam contact footprint", hideFlags = HideFlags.DontSave };
            mesh.MarkDynamic();
            var uv = new Vector2[vertices.Length];
            for (int y = 0; y < Side; y++)
                for (int x = 0; x < Side; x++) uv[y * Side + x] = new Vector2(x, y) / Cells;
            mesh.vertices = vertices;
            mesh.uv = uv;
            filter.sharedMesh = mesh;
            previous = null;
            if (beam != null) beam.PathUpdated += Rebuild;
            Rebuild(beam);
        }

        private void OnDisable()
        {
            if (beam != null) beam.PathUpdated -= Rebuild;
            if (filter != null && filter.sharedMesh == mesh) filter.sharedMesh = original;
            if (mesh != null)
            {
                if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
            }
        }

        private void Rebuild(Beam owner)
        {
            var surface = owner != null && owner.HasResolvedPath ? owner.CurrentTarget.SurfaceCollider : null;
            if (!surface)
            {
                if (previous != null) mesh.SetTriangles(indices, 0, 0, 0);
                previous = null;
                return;
            }
            var pose = transform.localToWorldMatrix;
            var colliderPose = surface.transform.localToWorldMatrix;
            if (previous == surface && pose == lastPose && colliderPose == lastColliderPose) return;
            previous = surface;
            lastPose = pose;
            lastColliderPose = colliderPose;
            var normal = transform.forward;
            float reach = Mathf.Max(.05f, Mathf.Abs(transform.lossyScale.z));
            for (int y = 0; y < Side; y++)
            for (int x = 0; x < Side; x++)
            {
                int i = y * Side + x;
                var center = pose.MultiplyPoint3x4(new Vector3((float)x/Cells-.5f, (float)y/Cells-.5f, 0));
                hits[i] = surface.Raycast(new Ray(center + normal * reach, -normal), out var hit, 2 * reach);
                vertices[i] = hits[i] ? transform.InverseTransformPoint(hit.point + hit.normal * (.012f * reach)) : Vector3.zero;
            }
            int n = 0;
            for (int y = 0; y < Cells; y++)
            for (int x = 0; x < Cells; x++)
            {
                int a = y * Side + x, b = a + 1, c = a + Side, d = c + 1;
                if (hits[a] && hits[b] && hits[c]) { indices[n++] = a; indices[n++] = c; indices[n++] = b; }
                if (hits[b] && hits[c] && hits[d]) { indices[n++] = b; indices[n++] = c; indices[n++] = d; }
            }
            mesh.vertices = vertices;
            mesh.SetTriangles(indices, 0, n, 0);
        }
    }
}
