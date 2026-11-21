using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace JLink.Frames
{
    public static partial class FrameMesh
    {
        /// <summary>Replaces the contents of a caller-owned mesh in local metres. The front faces -Z.</summary>
        public static void Build(FrameRecipe recipe, Mesh destination) => BuildWithContour(recipe,destination);

        internal static FrameContour BuildWithContour(FrameRecipe recipe,Mesh destination)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (!destination) throw new ArgumentNullException(nameof(destination));
            recipe.Sanitize();
            var outline=new FrameContour(recipe);
            // Unity uploads the buffers synchronously. Keep one idle workspace for subsequent
            // edits, regardless of how many frame components are present in the scene.
            var builder = System.Threading.Interlocked.Exchange(ref idleBuilder,null) ?? new Builder();
            try { builder.Build(recipe,outline,destination); }
            finally
            {
                if (builder.CanRetain)
                {
                    builder.Reset();
                    System.Threading.Interlocked.CompareExchange(ref idleBuilder,builder,null);
                }
            }
            return outline;
        }

        static Builder idleBuilder;

        sealed class Chart
        {
            public int first, end;
            public Vector2 min, size, origin;
        }

        sealed partial class Builder
        {
            FrameRecipe r;
            FrameContour outline;
            readonly List<Vector3> vertices = new List<Vector3>(), normals = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<int>[] faces = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
            readonly List<Chart> charts = new List<Chart>();
            readonly List<Vector2> section = new List<Vector2>();
            readonly List<bool> hardEdge = new List<bool>();
            int frontEnd;

            // Keep normal editing buffers warm without retaining an unusually large one-off mesh.
            public bool CanRetain => vertices.Capacity<=131072 && faces[0].Capacity<=1048576;

            public void Reset()
            {
                r = null; outline = null; previousRail = null;
                vertices.Clear(); normals.Clear(); uv.Clear();
                foreach (var indices in faces) indices.Clear();
                charts.Clear(); section.Clear(); hardEdge.Clear();
            }

            public void Build(FrameRecipe recipe,FrameContour contour,Mesh mesh)
            {
                r = recipe;
                outline = contour;
                MakeSection();
                Write(mesh);
            }

            void MakeSection()
            {
                // Sample the keys exactly so even a narrow bead survives at low subdivision counts.
                var steps = new SortedSet<float> { 0, 1 };
                var creases = new HashSet<float>();
                // Adjacent floats can become the exact same 3D vertex on a narrow rail.
                // Keep curve keys first, then fill in the regular subdivisions around them.
                float tolerance = .000001f*Mathf.Max(1,r.artworkSize.x,r.artworkSize.y)/r.railWidth;
                if (outline.sculpted) tolerance*=4;
                void Insert(float t)
                {
                    if (t > 0 && t < 1 && steps.GetViewBetween(t-tolerance,t+tolerance).Count == 0)
                        steps.Add(t);
                }
                foreach (var k in r.profile.keys) Insert(k.time);
                foreach (var band in r.bands)
                {
                    float center = band.Center(r.railWidth), width = band.Width(r.railWidth);
                    for (int j = -8; j <= 8; j++)
                    {
                        float t = (center+width*j/16)/r.railWidth;
                        Insert(t);
                        if (band.CreaseAt(j))
                            foreach (float sample in steps.GetViewBetween(t-tolerance,t+tolerance)) creases.Add(sample);
                    }
                }
                for (int i = 1; i < r.curveSegments; i++) Insert(i/(float)r.curveSegments);
                foreach (float t in steps)
                {
                    section.Add(new Vector2(r.Border + t*r.railWidth, -r.Height(t)));
                    hardEdge.Add(creases.Contains(t));
                }
                frontEnd = section.Count - 1;
                section.Add(new Vector2(r.Border+r.railWidth, r.railDepth));
                if (r.mount == FrameMount.Floater)
                {
                    section.Add(new Vector2(0,r.railDepth));
                    section.Add(new Vector2(0,r.railDepth*.82f));
                    section.Add(new Vector2(r.Border,r.railDepth*.82f));
                }
                else
                {
                    section.Add(new Vector2(r.Border+r.Lip,r.railDepth));
                    section.Add(new Vector2(r.Border+r.Lip,r.Seat));
                    section.Add(new Vector2(r.Border,r.Seat));
                }
                section.Add(section[0]);
            }

            void Write(Mesh mesh)
            {
                foreach (var rail in outline.rails)
                {
                    StartChart();
                    float v = 0;
                    var tiledDistance=outline.sculpted && r.uvLayout==FrameUV.Tiled ? new float[rail.Count] : null;
                    for (int j = 0; j < section.Count-1; j++)
                    {
                        var a = section[j];
                        var b = section[j+1];
                        Vector2 slope = SectionDirection(a,b);
                        Vector2 slopeA = slope, slopeB = slope;
                        if (j > 0 && j < frontEnd && !hardEdge[j]) slopeA += SectionDirection(section[j-1],a);
                        if (j+1 < frontEnd && !hardEdge[j+1]) slopeB += SectionDirection(b,section[j+2]);
                        float outer=r.Border+r.railWidth;
                        Strip(rail, FrameSlot.Moulding, a, b, slopeA, slopeB, v,
                            j>frontEnd && a.x<outer,j+1>frontEnd && b.x<outer,tiledDistance);
                        v += (b-a).magnitude;
                    }
                    EndChart();
                }

                var insertOutline=outline.sculpted ? new FrameContour(r,false) : outline;
                float artZ = r.Seat;
                foreach (var band in r.bands)
                    if (band.ornament != BandOrnament.None) AddOrnaments(band);
                if (r.corners.Visible(r)) AddCorners();
                if (r.mount != FrameMount.Floater && r.matWidth > 0)
                {
                    StartChart();
                    foreach (var rail in insertOutline.rails)
                        PlanarRing(rail,r.matWidth+r.Lip*.9f,r.Seat+.0001f);
                    EndChart();
                    artZ += .0002f;
                }
                if (r.includeArtwork)
                {
                    StartChart();
                    foreach (var rail in insertOutline.rails) Artwork(rail,artZ);
                    EndChart();
                    if (r.mount == FrameMount.Floater)
                    {
                        foreach (var rail in insertOutline.rails)
                        {
                            StartChart();
                            Strip(rail,FrameSlot.Artwork,new Vector2(0,artZ),new Vector2(0,r.railDepth*.82f),
                                Vector2.up,Vector2.up,0);
                            EndChart();
                        }
                    }
                }

                if (r.easelBack) AddEaselBack(insertOutline);
                if (r.uvLayout == FrameUV.Paintable) Pack();
                mesh.Clear();
                mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0,uv);
                mesh.subMeshCount = r.easelBack ? 4 : 3;
                for (int i = 0; i < mesh.subMeshCount; i++) mesh.SetTriangles(faces[i],i,false);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
            }

            static Vector2 SectionDirection(Vector2 a,Vector2 b)
            {
                // Profile keys and loop samples may lie only a few microns apart.
                Vector2 delta = b-a;
                return delta/delta.magnitude;
            }

            // Consecutive strips share a ring of positions. Their shading can differ at a
            // crease, so retain the contour samples and compute the face normals per strip.
            struct RingSample
            {
                public Vector2 position, tangent;
                public float stretch;
            }

            RingSample[] ringA=Array.Empty<RingSample>(), ringB=Array.Empty<RingSample>();
            List<FrameContour.Point> previousRail;
            float previousOffset;
            bool previousFixed;

            static void SampleRing(List<FrameContour.Point> rail,float offset,bool fixedSeat,bool oval,RingSample[] ring)
            {
                for (int i=0;i<rail.Count;i++)
                    ring[i].position=rail[i].At(offset,fixedSeat);
                for (int i=0;i<rail.Count;i++)
                {
                    var p=rail[i];
                    ring[i].tangent=p.tangent;
                    ring[i].stretch=1;
                    if (p.section==null || fixedSeat) continue;
                    // Oval crests have zero derivative at each quadrant seam.
                    if (!oval || i>0 && i<rail.Count-1)
                        ring[i].tangent=(ring[Mathf.Min(i+1,rail.Count-1)].position-ring[Mathf.Max(0,i-1)].position).normalized;
                    const float probe=.00001f;
                    ring[i].stretch=(p.section.Transform(offset+probe)-p.section.Transform(offset-probe))/(2*probe);
                }
            }

            void Strip(List<FrameContour.Point> rail, FrameSlot slot, Vector2 a, Vector2 b,
                Vector2 slopeA, Vector2 slopeB, float v,bool fixedA=false,bool fixedB=false,float[] tiledDistance=null)
            {
                if (ringA.Length<rail.Count)
                {
                    ringA=new RingSample[rail.Count];
                    ringB=new RingSample[rail.Count];
                }
                bool oval=r.outline==FrameOutline.Oval;
                if (rail==previousRail && a.x==previousOffset && fixedA==previousFixed)
                    (ringA,ringB)=(ringB,ringA);
                else SampleRing(rail,a.x,fixedA,oval,ringA);
                SampleRing(rail,b.x,fixedB,oval,ringB);
                previousRail=rail; previousOffset=b.x; previousFixed=fixedB;
                float ua = 0, ub = 0;
                for (int i = 0; i < rail.Count; i++)
                {
                    var p = rail[i];
                    Vector2 pa=ringA[i].position, pb=ringB[i].position;
                    if (r.outline == FrameOutline.Rectangle && (!outline.sculpted || i==0))
                    {
                        ua = Vector2.Dot(pa-rail[0].center,p.tangent);
                        ub = Vector2.Dot(pb-rail[0].center,p.tangent);
                    }
                    else if (i > 0)
                    {
                        ua += Vector2.Distance(pa,ringA[i-1].position);
                        ub += Vector2.Distance(pb,ringB[i-1].position);
                    }
                    float va=v, vb=v+(b-a).magnitude;
                    if (tiledDistance!=null)
                    {
                        va=tiledDistance[i];
                        vb=va+Mathf.Sqrt((pb-pa).sqrMagnitude+(b.y-a.y)*(b.y-a.y));
                        tiledDistance[i]=vb;
                    }
                    AddStripVertex(ringA[i],p.outward,a.y,slopeA,ua,va);
                    AddStripVertex(ringB[i],p.outward,b.y,slopeB,ub,vb);
                    if (i == 0) continue;
                    int k = vertices.Count-4;
                    Quad(slot,k,k+2,k+3,k+1);
                }
            }

            void AddStripVertex(RingSample sample,Vector2 outward,float z,Vector2 slope,float u,float v)
            {
                var direction=new Vector3(outward.x*slope.x*sample.stretch,outward.y*slope.x*sample.stretch,slope.y);
                Vector3 normal=Vector3.Cross(sample.tangent,direction).normalized;
                Add(new Vector3(sample.position.x,sample.position.y,z),normal,new Vector2(u,v));
            }

            void PlanarRing(List<FrameContour.Point> rail,float width,float z)
            {
                for (int i = 0; i < rail.Count; i++)
                {
                    var p = rail[i];
                    PlaneVertex(p.center,z,false);
                    PlaneVertex(p.At(width),z,false);
                    if (i == 0) continue;
                    int k = vertices.Count-4;
                    Quad(FrameSlot.Mat,k,k+2,k+3,k+1);
                }
            }

            void Artwork(List<FrameContour.Point> rail,float z)
            {
                int center = vertices.Count;
                PlaneVertex(Vector2.zero,z,true);
                for (int i = 0; i < rail.Count; i++)
                {
                    PlaneVertex(rail[i].center,z,true);
                    if (i > 0) Triangle(FrameSlot.Artwork,center,vertices.Count-1,vertices.Count-2);
                }
            }

            void PlaneVertex(Vector2 xy,float z,bool art)
            {
                Vector2 coord = xy;
                if (art && r.uvLayout == FrameUV.Tiled)
                    coord = new Vector2(xy.x/r.artworkSize.x+.5f,xy.y/r.artworkSize.y+.5f)*r.tileSize;
                Add(new Vector3(xy.x,xy.y,z),Vector3.back,coord);
            }

            void Add(Vector3 point, Vector3 normal, Vector2 coord)
            {
                vertices.Add(point);
                normals.Add(normal);
                uv.Add(r.uvLayout == FrameUV.Tiled ? coord/r.tileSize : coord);
            }

            void Triangle(FrameSlot slot,int a,int b,int c)
            {
                var indices = faces[(int)slot];
                indices.Add(a); indices.Add(b); indices.Add(c);
            }
            void Quad(FrameSlot slot,int a,int b,int c,int d)
            {
                Triangle(slot,a,b,c);
                Triangle(slot,a,c,d);
            }

            void StartChart()
            {
                if (r.uvLayout == FrameUV.Paintable) charts.Add(new Chart { first = uv.Count });
            }
            void EndChart()
            {
                if (r.uvLayout != FrameUV.Paintable) return;
                var c = charts[charts.Count-1];
                c.end = uv.Count;
                c.min = uv[c.first];
                Vector2 max = c.min;
                for (int i = c.first+1; i < c.end; i++)
                {
                    c.min = Vector2.Min(c.min,uv[i]);
                    max = Vector2.Max(max,uv[i]);
                }
                c.size = max-c.min;
            }

            void Pack()
            {
                float area = 0, widest = 0;
                foreach (var c in charts)
                {
                    area += c.size.x*c.size.y;
                    widest = Mathf.Max(widest,c.size.x);
                }
                float rowWidth = Mathf.Max(widest, Mathf.Sqrt(area));
                float pad = rowWidth*.008f;
                charts.Sort((a,b) => b.size.y.CompareTo(a.size.y));
                float x = pad, y = pad, rowHeight = 0, usedWidth = 0;
                foreach (var c in charts)
                {
                    if (x > pad && x+c.size.x > rowWidth+pad)
                    {
                        y += rowHeight+pad;
                        x = pad;
                        rowHeight = 0;
                    }
                    c.origin = new Vector2(x,y);
                    x += c.size.x+pad;
                    usedWidth = Mathf.Max(usedWidth,x);
                    rowHeight = Mathf.Max(rowHeight,c.size.y);
                }
                float side = Mathf.Max(usedWidth,y+rowHeight+pad);
                foreach (var c in charts)
                    for (int i = c.first; i < c.end; i++) uv[i] = (uv[i]-c.min+c.origin)/side;
            }
        }
    }
}
