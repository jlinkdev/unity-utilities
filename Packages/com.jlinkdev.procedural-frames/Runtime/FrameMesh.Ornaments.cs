using System;
using System.Collections.Generic;
using UnityEngine;

namespace JLink.Frames
{
    public static partial class FrameMesh
    {
        // Piecewise arc length on the same offset contour that generated the visible rail.
        sealed class OrnamentPath
        {
            readonly List<FrameContour.Point> points = new List<FrameContour.Point>();
            readonly float[] distances;
            readonly float offset;
            readonly bool mitred;
            readonly FrameContour contour;
            readonly float mitreBlend;
            public float Length => distances[distances.Length-1];

            public OrnamentPath(FrameContour contour,int rail,float offset,float mitreBlend)
            {
                this.contour=contour;
                this.offset = offset;
                this.mitreBlend = mitreBlend;
                mitred = rail >= 0;
                if (mitred) points.AddRange(contour.rails[rail]);
                else
                {
                    foreach (var run in contour.rails)
                        for (int i=0;i<run.Count-1;i++) points.Add(run[i]);
                    points.Add(points[0]);
                }
                distances = new float[points.Count];
                for (int i=1;i<points.Count;i++)
                    distances[i] = distances[i-1]+Vector2.Distance(points[i-1].At(offset),points[i].At(offset));
            }

            public float NextCorner(float distance)
            {
                int index = Array.BinarySearch(distances,distance);
                index = index < 0 ? ~index : index+1;
                return index < distances.Length ? distances[index] : Length;
            }

            public Vector2 At(float distance,float across)
            {
                int end = Array.BinarySearch(distances,distance);
                if (end < 0) end = ~end;
                end = Mathf.Clamp(end,1,points.Count-1);
                var a = points[end-1]; var b = points[end];
                float t = Mathf.InverseLerp(distances[end-1],distances[end],distance);
                if (!mitred && contour.sculpted)
                    return Vector2.Lerp(a.At(offset+across),b.At(offset+across),t);
                Vector2 xy = Vector2.Lerp(a.At(offset,contour.sculpted),b.At(offset,contour.sculpted),t);
                var axis = mitred ? new Vector2(a.tangent.y,-a.tangent.x) : Vector2.Lerp(a.outward,b.outward,t);
                if (mitred && mitreBlend > 0)
                {
                    float start = Mathf.Clamp01(1-distance/mitreBlend);
                    float finish = Mathf.Clamp01(1-(Length-distance)/mitreBlend);
                    axis += a.tangent*(finish-start);
                }
                Vector2 placed=xy+axis*across;
                return contour.sculpted ? contour.Deform(placed) : placed;
            }
        }

        sealed partial class Builder
        {
            void AddOrnaments(FrameBand band)
            {
                var motif = FrameOrnament.For(band.ornament);
                if (motif == null) return;
                float center = band.Center(r.railWidth);
                float offset = r.Border+center;
                float size = band.FittedSize(r);
                bool mitred = r.outline == FrameOutline.Rectangle;

                // Conform the relief to the actual polygonal cross section, including existing grooves.
                float[] Depths(FrameOrnament part)
                {
                    if (part == null) return null;
                    var z = new float[part.points.Count];
                    for (int i=0;i<z.Length;i++)
                    {
                        var p = part.points[i];
                        z[i] = FrontDepth(offset+p.y*size)-p.z*size;
                    }
                    return z;
                }

                var depths = Depths(motif);
                bool continuousRope = band.ornament == BandOrnament.RopeTwist && band.detailGap == 0;
                var startDepths = continuousRope ? null : Depths(motif.startCap);
                var endDepths = continuousRope ? null : Depths(motif.endCap);
                var spacerDepths = Depths(motif.spacer);
                bool continuous = continuousRope || motif.spacer != null;
                int runs = mitred ? 4 : 1;
                for (int run=0;run<runs;run++)
                {
                    var path = new OrnamentPath(outline,mitred ? run : -1,offset,continuous ? size : 0);
                    float margin = mitred && !continuous ? size*.5f : 0;
                    float length = path.Length-2*margin;
                    float footprint = motif.length*size;
                    int count = Mathf.Clamp(Mathf.FloorToInt(length/(footprint+band.detailGap)),1,256/runs);
                    float pitch = length/count;
                    float along = continuousRope ? pitch/motif.length : size;
                    bool mirror = mitred && run >= 2 && band.mirrorOppositeRails && band.ornament == BandOrnament.LeafScroll;
                    for (int i=0;i<count;i++)
                    {
                        // Short strips pack efficiently even when a long rail has hundreds of beads.
                        if (i%8 == 0) StartChart();
                        float station = margin+(i+.5f)*pitch;
                        Stamp(path,motif,depths,station,size,along,mirror);
                        if (startDepths != null) Stamp(path,motif.startCap,startDepths,station,size,along,mirror);
                        if (endDepths != null) Stamp(path,motif.endCap,endDepths,station,size,along,mirror);
                        if (spacerDepths != null)
                        {
                            FillFillet(path,motif.spacer,spacerDepths,i*pitch,station-footprint*.5f,size);
                            FillFillet(path,motif.spacer,spacerDepths,station+footprint*.5f,(i+1)*pitch,size);
                        }
                        if (i%8 == 7 || i == count-1) EndChart();
                    }
                }
            }

            void FillFillet(OrnamentPath path,FrameOrnament spacer,float[] depths,float from,float to,float size)
            {
                // Split at contour vertices so even a wide gap follows a tight curved rail.
                to = Mathf.Min(to,path.Length);
                const float tolerance = .000001f;
                while (to-from > tolerance)
                {
                    float corner = path.NextCorner(from+tolerance);
                    float next = corner < to-tolerance ? corner : to;
                    Stamp(path,spacer,depths,(from+next)*.5f,size,next-from,false);
                    from = next;
                }
            }

            float FrontDepth(float offset)
            {
                int lo = 0, hi = frontEnd;
                while (hi-lo > 1)
                {
                    int mid = (lo+hi)/2;
                    if (section[mid].x < offset) lo=mid; else hi=mid;
                }
                return Mathf.Lerp(section[lo].y,section[hi].y,Mathf.InverseLerp(section[lo].x,section[hi].x,offset));
            }

            void Stamp(OrnamentPath path,FrameOrnament motif,float[] depths,float station,float size,float along,bool mirror)
            {
                int start = vertices.Count;
                float direction = mirror ? -1 : 1;
                for (int i=0;i<motif.points.Count;i++)
                {
                    var p = motif.points[i];
                    Vector2 xy = path.At(station+direction*p.x*along,p.y*size);
                    var tex = Vector2.Scale(motif.uv[i],new Vector2(along,size));
                    tex.x += station;
                    Add(new Vector3(xy.x,xy.y,depths[i]),Vector3.zero,tex);
                }
                FinishOrnament(motif,start,mirror);
            }

            void AddCorners()
            {
                var settings=r.corners;
                var motif=FrameOrnament.Corner(settings.style);
                if (motif==null) return;
                float size=settings.FittedSize(r), centre=settings.Centre(r,size);
                for (int corner=0;corner<4;corner++)
                {
                    StartChart();
                    int start=vertices.Count;
                    for (int i=0;i<motif.points.Count;i++)
                    {
                        var p=motif.points[i];
                        var coord=motif.CornerCoordinates(p,r.outline==FrameOutline.Rectangle);
                        float across=centre+coord.y*size;
                        Vector2 xy=settings.Point(r,corner,coord.x*size,across,centre,outline);
                        Add(new Vector3(xy.x,xy.y,FrontDepth(across)-p.z*settings.relief),
                            Vector3.zero,motif.uv[i]*size);
                    }
                    FinishOrnament(motif,start,corner==0 || corner==3);
                    EndChart();
                }
            }

            void FinishOrnament(FrameOrnament motif,int start,bool mirror)
            {
                for (int i=0;i<motif.faces.Count;i+=3)
                {
                    int a = start+motif.faces[i];
                    int b = start+motif.faces[i+(mirror ? 2 : 1)];
                    int c = start+motif.faces[i+(mirror ? 1 : 2)];
                    Triangle(FrameSlot.Moulding,a,b,c);
                    var normal = Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]);
                    normals[a] += normal; normals[b] += normal; normals[c] += normal;
                }
                for (int i=start;i<vertices.Count;i++)
                {
                    Vector3 n = normals[i];
                    // Valid millimetre-scale triangles lie below Vector3.normalized's cutoff.
                    normals[i] = n.sqrMagnitude > 0 ? n/n.magnitude : Vector3.back;
                }
            }
        }
    }
}
