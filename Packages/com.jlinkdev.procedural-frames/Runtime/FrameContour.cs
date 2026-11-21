using System.Collections.Generic;
using UnityEngine;

namespace JLink.Frames
{
    // Each run is one rail or one quarter of a curved outline. Its ends are UV seams.
    internal sealed class FrameContour
    {
        internal readonly struct Point
        {
            public readonly Vector2 center, outward, tangent;
            internal readonly EdgeSection section;
            public Point(Vector2 center, Vector2 outward, Vector2 tangent,EdgeSection section=null)
            {
                this.section=section;
                this.center = center;
                this.outward = outward;
                this.tangent = tangent;
            }

            public Vector2 At(float offset,bool fixedSeat=false) => center + outward * (fixedSeat ? offset : section?.Transform(offset) ?? offset);
        }

        public readonly List<Point>[] rails = new List<Point>[4];

        readonly FrameRecipe recipe;
        public readonly bool sculpted;
        internal Matrix4x4 designToMesh = Matrix4x4.identity;

        internal static Vector2 Normal(int side)
        {
            switch (side) { case 0:return Vector2.down; case 1:return Vector2.right;
                case 2:return Vector2.up; default:return Vector2.left; }
        }
        internal static void Basis(FrameRecipe r,int side,float u,out Vector2 origin,out Vector2 axis)
        {
            Vector2 n=Normal(side), t=new Vector2(-n.y,n.x);
            if (r.outline==FrameOutline.Oval)
            {
                Vector2 radial=u<=0 ? n : u>=1 ? t : n*Mathf.Cos(u*Mathf.PI*.5f)+t*Mathf.Sin(u*Mathf.PI*.5f);
                Vector2 half=r.artworkSize*.5f;
                origin=Vector2.Scale(radial,half);
                axis=new Vector2(radial.x/half.x,radial.y/half.y).normalized;
                return;
            }
            axis=n+(2*u-1)*t;
            origin=Vector2.Scale(axis,r.artworkSize*.5f);
        }
        internal static void Locate(FrameRecipe r,Vector2 p,out int side,out float u,out float offset)
        {
            if (r.outline==FrameOutline.Oval)
            {
                side=p.x>=0 ? (p.y>=0 ? 1 : 0) : (p.y>=0 ? 2 : 3);
                // Find the ellipse normal through the picked point on the outside of the opening.
                float lo=0, hi=1;
                for (int i=0;i<20;i++)
                {
                    float mid=(lo+hi)*.5f;
                    Basis(r,side,mid,out var at,out var nrm);
                    if (Vector2.Dot(p-at,new Vector2(-nrm.y,nrm.x))>0) lo=mid; else hi=mid;
                }
                u=(lo+hi)*.5f;
                Basis(r,side,u,out var origin,out var axis);
                offset=Vector2.Dot(p-origin,axis);
                return;
            }
            Vector2 h=r.artworkSize*.5f;
            float dx=Mathf.Abs(p.x)-h.x, dy=Mathf.Abs(p.y)-h.y;
            side=dx>dy ? (p.x>0 ? 1 : 3) : (p.y>0 ? 2 : 0);
            offset=Mathf.Max(dx,dy);
            Vector2 n=Normal(side),t=new Vector2(-n.y,n.x);
            float halfLength=(side%2==0 ? h.x : h.y)+offset;
            u=Mathf.Clamp01(.5f+.5f*Vector2.Dot(p,t)/Mathf.Max(.00001f,halfLength));
        }

        public Vector2 At(int side,float u,float offset)
        {
            var run=rails[side];
            float index=Mathf.Clamp01(u)*(run.Count-1);
            int i=Mathf.Min(Mathf.FloorToInt(index),run.Count-2);
            return Vector2.Lerp(run[i].At(offset),run[i+1].At(offset),index-i);
        }
        public Vector2 Deform(Vector2 flat)
        {
            Locate(recipe,flat,out int side,out float u,out float offset);
            return At(side,u,offset);
        }
        public float InverseOffset(int side,float u,float offset)
        {
            var run=rails[side]; float index=u*(run.Count-1);
            int i=Mathf.Min(Mathf.FloorToInt(index),run.Count-2);
            float lo=recipe.Border, hi=lo+recipe.railWidth;
            for (int j=0;j<19;j++)
            {
                float mid=(lo+hi)*.5f;
                float physical=Mathf.Lerp(run[i].section.Transform(mid),run[i+1].section.Transform(mid),index-i);
                if (physical<offset) lo=mid; else hi=mid;
            }
            return (lo+hi)*.5f;
        }

        public FrameContour(FrameRecipe r,bool shapeEdges=true)
        {
            recipe=r; sculpted=shapeEdges && r.edges!=null && r.edges.Active(r);
            var layout=sculpted ? new EdgeSection.Layout(r) : null;
            var sections=sculpted ? new Dictionary<float,EdgeSection>() : null;
            EdgeSection Section(int side,float u)
            {
                if (!sculpted) return null;
                float delta=r.edges.Displacement(r,side,u);
                if (!sections.TryGetValue(delta,out var section))
                    sections.Add(delta,section=new EdgeSection(layout,delta));
                return section;
            }
            Vector2 half = r.artworkSize * .5f;
            float radius = Mathf.Min(r.artworkSize.x,r.artworkSize.y) * r.cornerFraction;
            Vector2[] corners = { new Vector2(-half.x,-half.y), new Vector2(half.x,-half.y),
                half, new Vector2(-half.x,half.y) };
            Vector2[] normals = { Vector2.down, Vector2.right, Vector2.up, Vector2.left };
            for (int i = 0; i < 4; i++)
            {
                var run = rails[i] = new List<Point>();
                int next = (i+1)%4;
                Vector2 n = normals[i], t = normals[next], corner = corners[next];
                if (r.outline == FrameOutline.Rectangle)
                {
                    if (sculpted)
                    {
                        int segments=Mathf.Max(48,r.curveSegments*4);
                        for (int j=0;j<=segments;j++)
                        {
                            float u=j/(float)segments;
                            Basis(r,i,u,out var origin,out var axis);
                            run.Add(new Point(origin,axis,t,Section(i,u)));
                        }
                        continue;
                    }
                    run.Add(new Point(corners[i], n-t, t));
                    run.Add(new Point(corner, n+t, t));
                    continue;
                }
                if (r.outline == FrameOutline.Rounded)
                    run.Add(new Point(corners[i]+radius*t,n,t));
                if (r.outline==FrameOutline.Oval)
                {
                    // Refine the seat and insert identically so their quarter seams stay closed.
                    int segments=r.edges!=null && r.edges.Active(r) ? r.curveSegments*Mathf.Max(4,Mathf.CeilToInt(64f/r.curveSegments)) : r.curveSegments;
                    for (int j=0;j<=segments;j++)
                    {
                        float u=j/(float)segments;
                        Basis(r,i,u,out var origin,out var axis);
                        run.Add(new Point(origin,axis,new Vector2(-axis.y,axis.x),Section(i,u)));
                    }
                    continue;
                }
                for (int j = 0; j <= r.curveSegments; j++)
                {
                    float theta = .5f*Mathf.PI*j/r.curveSegments;
                    Vector2 radial = j == 0 ? n : j == r.curveSegments ? t : Mathf.Cos(theta)*n + Mathf.Sin(theta)*t;
                    Vector2 center=corner+radius*(radial-n-t);
                    run.Add(new Point(center, radial, new Vector2(-radial.y, radial.x)));
                }
            }
        }
    }
}
