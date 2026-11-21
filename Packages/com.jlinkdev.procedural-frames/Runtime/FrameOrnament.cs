using System;
using System.Collections.Generic;
using UnityEngine;

namespace JLink.Frames
{
    // The three coordinates are distance along the loop, distance across it, and raised relief,
    // in units of ornament width. Each pattern is built once and reused by every frame.
    internal sealed partial class FrameOrnament
    {
        static readonly FrameOrnament Rope = MakeRope();
        static readonly FrameOrnament Flute = MakeFlute();
        static readonly FrameOrnament EggAndDart = MakeEggAndDart();

        internal static FrameOrnament For(BandOrnament ornament)
        {
            switch (ornament)
            {
                case BandOrnament.Beads: return Bead;
                case BandOrnament.LeafScroll: return Scroll;
                case BandOrnament.RopeTwist: return Rope;
                case BandOrnament.Fluting: return Flute;
                case BandOrnament.EggAndDart: return EggAndDart;
                default: return null;
            }
        }
        internal static readonly FrameOrnament Bead = MakeBead();
        internal static readonly FrameOrnament Scroll = MakeScroll();
        internal readonly List<Vector3> points = new List<Vector3>();
        internal readonly List<Vector2> uv = new List<Vector2>();
        internal readonly List<int> faces = new List<int>();
        internal readonly float length;
        int lanes;

        FrameOrnament(float length) { this.length = length; }

        int Vertex(Vector2 p,float h,Vector2 tex)
        {
            int index = points.Count;
            points.Add(new Vector3(p.x,p.y,h)); uv.Add(tex);
            return index;
        }

        void Face(int a,int b,int c) { faces.Add(a); faces.Add(b); faces.Add(c); }

        static FrameOrnament MakeBead()
        {
            var bead = new FrameOrnament(1);
            int pole = bead.Vertex(Vector2.zero,.34f,Vector2.zero);
            const int sides = 14, rings = 4;
            for (int ring = 1; ring <= rings; ring++)
            {
                float angle = ring*Mathf.PI/(2*rings);
                float radius = .5f*Mathf.Sin(angle);
                for (int side = 0; side < sides; side++)
                {
                    float phi = 2*Mathf.PI*side/sides;
                    var p = new Vector2(Mathf.Cos(phi),Mathf.Sin(phi))*radius;
                    bead.Vertex(p,.35f*Mathf.Cos(angle)-.01f,p);
                }
                int start = 1+(ring-1)*sides;
                for (int side = 0; side < sides; side++)
                {
                    int a = start+side, b = start+(side+1)%sides;
                    if (ring == 1) bead.Face(pole,a,b);
                    else { bead.Face(a-sides,a,b); bead.Face(a-sides,b,b-sides); }
                }
            }
            return bead;
        }

        static FrameOrnament MakeScroll()
        {
            var scroll = new FrameOrnament(3.4f);
            // Join the vine to its curled tip with a continuous crown and tangent.
            scroll.Stroke(Vine,.047f,.091f,false,40);
            var firstLeaf = Vine(.045f);
            var lowerLeaf = Vine(.20f);
            var upperLeaf = Vine(.34f);
            scroll.Stroke(t => Bezier(t,firstLeaf,new Vector2(-1.05f,-.32f),
                new Vector2(-.93f,.10f),new Vector2(-.65f,.39f)),.14f,.19f,true);
            scroll.Stroke(t => Bezier(t,lowerLeaf,new Vector2(-.31f,-.20f),
                new Vector2(.08f,-.48f),new Vector2(.57f,-.40f)),.155f,.21f,true);
            scroll.Stroke(t => Bezier(t,upperLeaf,new Vector2(.01f,.13f),
                new Vector2(.10f,.40f),new Vector2(.50f,.43f)),.092f,.145f,true);
            return scroll;
        }

        static Vector2 Vine(float t)
        {
            const float join = .60f;
            var startOfCurl = new Vector2(1.16f,.3147f);
            if (t <= join)
                return Bezier(t/join,new Vector2(-1.64f,-.08f),new Vector2(-.83f,-.78f),
                    new Vector2(.35f,.421f),startOfCurl);
            float u = (t-join)/(1-join);
            float angle = Mathf.Lerp(Mathf.PI*.5f,-3.70f,u);
            float radius = Mathf.Lerp(.43f,.035f,u);
            return new Vector2(1.16f+radius*Mathf.Cos(angle),-.025f+.79f*radius*Mathf.Sin(angle));
        }

        static Vector2 Bezier(float t,Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            float s = 1-t;
            return s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d;
        }

        void Stroke(Func<float,Vector2> spine,float halfWidth,float relief,bool leaf,int rows=20)
        {
            const int columns = 7;
            int lane = lanes++;
            int start = points.Count;
            for (int row = 0; row <= rows; row++)
            {
                float t = row/(float)rows;
                Vector2 p = spine(t);
                float u = (t-.5f)*length;
                if (row == 0 || row == rows)
                {
                    Vertex(p,-.008f,new Vector2(u,lane*.4f));
                    continue;
                }
                var tangent = (spine(t+.001f)-spine(t-.001f)).normalized;
                Vector2 normal = new Vector2(-tangent.y,tangent.x);
                float taper = leaf ? Mathf.Sin(Mathf.PI*t)*(1.22f-.48f*t) : Mathf.Clamp01(Mathf.Min(t,1-t)*12);
                for (int col = 0; col < columns; col++)
                {
                    float v = 2f*col/(columns-1)-1;
                    float offset = halfWidth*taper*v;
                    // A creased blade catches a narrow highlight; the vine has a rounded back.
                    float crown = leaf ? (1-Mathf.Abs(v))*(1-.23f*Mathf.Abs(v)) : Mathf.Sqrt(Mathf.Max(0,1-v*v));
                    Vertex(p+normal*offset,taper*relief*crown-.008f,new Vector2(u,lane*.4f+offset));
                }
            }
            for (int col = 0; col < columns-1; col++) Face(start,start+1+col,start+2+col);
            for (int row = 1; row < rows-1; row++)
            {
                int first = start+1+(row-1)*columns;
                for (int col = 0; col < columns-1; col++)
                {
                    int a = first+col, b = a+columns;
                    Face(a,b,b+1); Face(a,b+1,a+1);
                }
            }
            int end = points.Count-1;
            for (int col=0; col<columns-1; col++) Face(end-columns+col,end,end-columns+col+1);
        }
    }
}
