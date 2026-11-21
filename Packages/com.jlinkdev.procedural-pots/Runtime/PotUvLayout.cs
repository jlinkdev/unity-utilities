using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace JLink.Pots
{
    // Chart vertices refer back to the geometric vertices. A UV cut can then share a normal
    // with its neighbour, while deliberately separate geometric vertices retain hard edges.
    internal sealed class PotUvLayout
    {
        enum Projection { Ribbon, FlatBand, Disc }

        sealed class Island
        {
            public Projection projection;
            public int group, count, first, end, center;
            public float seam;
            public bool reverse;
            public readonly List<int> rows = new List<int>();
            public Vector2 min, size, origin;
        }

        readonly List<Island> islands = new List<Island>();
        readonly List<int> source = new List<int>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int>[] faces = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
        readonly List<(int a,int b)> seams = new List<(int,int)>();
        readonly List<Vector3> positions = new List<Vector3>(), normals = new List<Vector3>();
        readonly List<Vector4> tangents = new List<Vector4>();
        Vector3[] normalSums = System.Array.Empty<Vector3>();
        Island ribbon;

        public void Clear()
        {
            islands.Clear(); source.Clear(); uv.Clear(); seams.Clear();
            foreach (var list in faces) list.Clear();
        }

        public void Ribbon(float seam)
        {
            ribbon = new Island { projection = Projection.Ribbon, seam = seam };
            islands.Add(ribbon);
        }

        public void Band(int a,int b,int count,int group,bool reverse)
        {
            if (ribbon.rows.Count == 0) ribbon.rows.Add(a);
            else if (ribbon.rows[ribbon.rows.Count-1] != a)
                throw new System.InvalidOperationException("A UV ribbon must follow consecutive mesh rings.");
            ribbon.rows.Add(b);
            ribbon.count = count; ribbon.group = group; ribbon.reverse = reverse;
        }

        public void FlatBand(int a,int b,int count,int group,bool top)
        {
            var island = new Island { projection = Projection.FlatBand, count = count, group = group, reverse = top };
            island.rows.Add(a); island.rows.Add(b); islands.Add(island);
        }

        public void Disc(int rim,int center,int count,int group,bool reverse)
        {
            var island = new Island { projection = Projection.Disc, center = center, count = count, group = group, reverse = reverse };
            island.rows.Add(rim); islands.Add(island);
        }

        public void Bake(List<Vector3> reference,float tile,PotUvMode mode)
        {
            foreach (var island in islands)
            {
                island.first = source.Count;
                if (island.projection == Projection.Ribbon) Unroll(island,reference,tile,mode);
                else ProjectFace(island,reference);
                island.end = source.Count;
            }
            if (mode == PotUvMode.Paintable) Pack();
            else for (int i = 0; i < uv.Count; i++) uv[i] /= tile;
        }

        void Vertex(int index,Vector2 coordinate)
        {
            source.Add(index);
            uv.Add(coordinate);
        }

        void Triangle(int group,int a,int b,int c)
        {
            var list = faces[group];
            list.Add(a); list.Add(b); list.Add(c);
        }

        void Quad(int group,int a,int b,int c,int d,bool reverse)
        {
            if (reverse) { Triangle(group,b,a,d); Triangle(group,b,d,c); }
            else { Triangle(group,a,b,c); Triangle(group,a,c,d); }
        }

        void Unroll(Island chart,List<Vector3> points,float tile,PotUvMode mode)
        {
            int n = chart.count, seam = Mathf.RoundToInt(n*chart.seam) % n;
            var distances = new float[n+1];
            float width = 0, height = 0;
            for (int row = 0; row < chart.rows.Count; row++)
            {
                int start = chart.rows[row];
                float advance = 0;
                for (int j = 0; j < n; j++)
                {
                    int i = (seam+j)%n, next = (i+1)%n;
                    distances[j+1] = distances[j] + Vector3.Distance(points[start+i],points[start+next]);
                    if (row > 0) advance += Vector3.Distance(points[start+i],points[chart.rows[row-1]+i]);
                }
                height += advance/n;
                float perimeter = distances[n];
                width = Mathf.Max(width,perimeter);
                int first = source.Count;
                for (int j = 0; j <= n; j++)
                    Vertex(start+(seam+j)%n,new Vector2(distances[j]/perimeter,height));
                seams.Add((first,first+n));
                if (row == 0) continue;
                for (int j = 0; j < n; j++)
                {
                    int a = first-n-1+j, b = first+j;
                    Quad(chart.group,a,b,b+1,a+1,chart.reverse);
                }
            }
            if (mode == PotUvMode.Tiled) width = Mathf.Max(1,Mathf.RoundToInt(width/tile))*tile;
            bool interior = chart.group == 1;
            for (int i = chart.first; i < uv.Count; i++)
            {
                var metric = new Vector2(uv[i].x*width,uv[i].y);
                uv[i] = interior ? new Vector2(width,height)-metric : metric;
            }
        }

        void ProjectFace(Island island,List<Vector3> points)
        {
            int count = island.count, first = source.Count;
            Vector3 along = Vector3.right, up = island.reverse ? Vector3.forward : Vector3.back;
            Vector3 origin = Vector3.zero;
            if (island.projection == Projection.Disc && island.group == 2)
            {
                // Handle end faces lie in their tube frame, including on sideways-facing loops.
                origin = points[island.center];
                along = (points[island.rows[0]]-origin).normalized;
                up = (points[island.rows[0]+count/4]-origin).normalized;
                if (!island.reverse) up = -up;
            }
            foreach (int row in island.rows)
                for (int i = 0; i < count; i++)
                {
                    var p = points[row+i]-origin;
                    Vertex(row+i,new Vector2(Vector3.Dot(p,along),Vector3.Dot(p,up)));
                }
            if (island.projection == Projection.Disc)
            {
                var p = points[island.center]-origin;
                int tip = source.Count;
                Vertex(island.center,new Vector2(Vector3.Dot(p,along),Vector3.Dot(p,up)));
                for (int i = 0; i < count; i++)
                {
                    int a = first+i, b = first+(i+1)%count;
                    Triangle(island.group,tip,island.reverse ? b : a,island.reverse ? a : b);
                }
            }
            else for (int i = 0; i < count; i++)
            {
                int a = first+i, d = first+(i+1)%count;
                Quad(island.group,a,a+count,d+count,d,false);
            }
        }

        // Uniform island scale preserves relative texel density. Leave four pixels per edge
        // at 1024 resolution, including the external border, for dilation and filtering.
        const float Margin = 4f/1024;
        void Pack()
        {
            var order = new List<Island>(islands);
            float largest = 0;
            foreach (var island in order)
            {
                Vector2 min = uv[island.first], max = min;
                for (int i = island.first+1; i < island.end; i++)
                {
                    min = Vector2.Min(min,uv[i]); max = Vector2.Max(max,uv[i]);
                }
                island.min = min; island.size = max-min;
                largest = Mathf.Max(largest,Mathf.Max(island.size.x,island.size.y));
            }
            order.Sort((a,b) => b.size.y.CompareTo(a.size.y));
            float lo = 0, hi = 1/largest;
            for (int step = 0; step < 24; step++)
            {
                float scale = (lo+hi)*.5f;
                if (Place(order,scale)) lo = scale;
                else hi = scale;
            }
            Place(order,lo);
            foreach (var island in order)
                for (int i = island.first; i < island.end; i++)
                    uv[i] = island.origin+(uv[i]-island.min)*lo;
        }

        static bool Place(List<Island> order,float scale)
        {
            float x = 0, y = 0, rowHeight = 0;
            foreach (var island in order)
            {
                Vector2 box = island.size*scale + Vector2.one*(2*Margin);
                if (x+box.x > 1) { x = 0; y += rowHeight; rowHeight = 0; }
                if (box.x > 1 || y+box.y > 1) return false;
                island.origin = new Vector2(x+Margin,y+Margin);
                x += box.x;
                rowHeight = Mathf.Max(rowHeight,box.y);
            }
            return true;
        }

        public void Write(Mesh mesh,List<Vector3> geometry,bool singleMaterial)
        {
            if (normalSums.Length < geometry.Count) normalSums = new Vector3[geometry.Count];
            else System.Array.Clear(normalSums,0,geometry.Count);
            foreach (var group in faces)
                for (int i = 0; i < group.Count; i += 3)
                {
                    int a = source[group[i]], b = source[group[i+1]], c = source[group[i+2]];
                    var area = Vector3.Cross(geometry[b]-geometry[a],geometry[c]-geometry[a]);
                    normalSums[a] += area; normalSums[b] += area; normalSums[c] += area;
                }
            positions.Clear(); normals.Clear();
            foreach (int index in source)
            {
                positions.Add(geometry[index]);
                Vector3 n = normalSums[index];
                normals.Add(n / Mathf.Max(1e-30f,n.magnitude));
            }
            mesh.Clear();
            mesh.indexFormat = source.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(positions); mesh.SetNormals(normals); mesh.SetUVs(0,uv);
            if (singleMaterial)
            {
                var all = new List<int>();
                foreach (var group in faces) all.AddRange(group);
                mesh.subMeshCount = 1; mesh.SetTriangles(all,0,false);
            }
            else
            {
                mesh.subMeshCount = faces.Length;
                for (int i = 0; i < faces.Length; i++) mesh.SetTriangles(faces[i],i,false);
            }
            mesh.RecalculateTangents();
            mesh.GetTangents(tangents);
            foreach (var pair in seams)
            {
                var a = tangents[pair.a]; var b = tangents[pair.b];
                if (a.w != b.w) continue;
                Vector3 direction = ((Vector3)a+(Vector3)b).normalized;
                if (direction.sqrMagnitude < .5f) continue;
                tangents[pair.a] = tangents[pair.b] = new Vector4(direction.x,direction.y,direction.z,a.w);
            }
            mesh.SetTangents(tangents);
            mesh.RecalculateBounds();
        }
    }
}
