using System.Collections.Generic;
using UnityEngine;

namespace JLink.Frames
{
    // The same visible triangles drive profile and band picking in both editing surfaces.
    internal sealed class FrameSurfacePick
    {
        readonly List<Vector3> positions = new List<Vector3>();
        readonly List<int> indices = new List<int>();
        readonly List<int> backIndices = new List<int>();
        int mouldingCount;
        int sampledRevision = -1;

        public bool TryPick(ProceduralFrame frame,Ray worldRay,out float profileT,out Vector3 point,out Vector2 outward)
        {
            profileT = 0;
            point = default;
            outward = default;
            var tr = frame.transform;
            var ray = new Ray(tr.InverseTransformPoint(worldRay.origin),tr.InverseTransformVector(worldRay.direction));
            var mesh = frame.GeneratedMesh;
            if (!mesh || !mesh.bounds.IntersectRay(ray)) return false;
            var contour=frame.Contour;
            var undoPose=frame.recipe.easelBack ? contour.designToMesh.inverse : Matrix4x4.identity;
            if (frame.recipe.easelBack)
                ray=new Ray(undoPose.MultiplyPoint3x4(ray.origin),undoPose.MultiplyVector(ray.direction));
            if (sampledRevision!=frame.MeshRevision)
            {
                mesh.GetVertices(positions);
                if (frame.recipe.easelBack)
                    for (int i=0;i<positions.Count;i++) positions[i]=undoPose.MultiplyPoint3x4(positions[i]);
                mesh.GetTriangles(indices,(int)FrameSlot.Moulding);
                mouldingCount=indices.Count;
                if (frame.recipe.easelBack)
                {
                    mesh.GetTriangles(backIndices,(int)FrameSlot.EaselBack);
                    indices.AddRange(backIndices);
                }
                sampledRevision=frame.MeshRevision;
            }
            var r = frame.recipe;
            float seat = r.mount == FrameMount.Floater ? r.railDepth*.81f : r.Seat;
            float closest = float.PositiveInfinity;
            bool front = false;
            for (int i = 0; i < indices.Count; i += 3)
            {
                Vector3 a = positions[indices[i]], b = positions[indices[i+1]], c = positions[indices[i+2]];
                Vector3 ab = b-a, ac = c-a;
                Vector3 p = Vector3.Cross(ray.direction,ac);
                float det = Vector3.Dot(ab,p);
                if (det <= 1e-12f) continue;
                Vector3 offset = ray.origin-a;
                float u = Vector3.Dot(offset,p)/det;
                if (u < 0 || u > 1) continue;
                Vector3 q = Vector3.Cross(offset,ab);
                float v = Vector3.Dot(ray.direction,q)/det;
                if (v < 0 || u+v > 1) continue;
                float distance = Vector3.Dot(ac,q)/det;
                if (distance < 0 || distance >= closest) continue;
                closest = distance;
                front = i<mouldingCount && a.z < seat && b.z < seat && c.z < seat && Vector3.Cross(ab,ac).z < 0;
            }
            if (!front) return false;
            point = ray.GetPoint(closest);
            Vector2 xy = point;
            if (contour.sculpted)
            {
                FrameContour.Locate(r,xy,out int side,out float u,out float offset);
                float nominal=contour.InverseOffset(side,u,offset);
                profileT=Mathf.Clamp01((nominal-r.Border)/r.railWidth);
                FrameContour.Basis(r,side,u,out _,out outward);
                return true;
            }
            float inset = float.NegativeInfinity;
            foreach (var rail in contour.rails)
                for (int i = 1; i < rail.Count; i++)
                {
                    Vector2 edge = rail[i].center-rail[i-1].center;
                    Vector2 normal = new Vector2(edge.y,-edge.x).normalized;
                    float scale = Vector2.Dot(normal,rail[i].outward);
                    float dist = Vector2.Dot(xy-rail[i].center,normal)/Mathf.Max(.001f,scale);
                    if (dist > inset) { inset = dist; outward = normal; }
                }
            profileT = Mathf.Clamp01((inset-r.Border)/r.railWidth);
            return true;
        }
    }
}
