using UnityEngine;

namespace JLink.Frames
{
    internal sealed partial class FrameOrnament
    {
        internal FrameOrnament startCap, endCap, spacer;

        static FrameOrnament MakeRope()
        {
            var rope = new FrameOrnament(1.15f);
            const int stations = 18, arcs = 18;
            for (int i = 0; i <= stations; i++)
            {
                float u = i/(float)stations;
                float x = (u-.5f)*rope.length;
                for (int j = 0; j <= arcs; j++)
                {
                    float v = j/(float)arcs;
                    float angle = (v-.5f)*Mathf.PI;
                    // Radial envelope of two round plies twisting around their common axis.
                    float phase = angle-u*Mathf.PI;
                    const float offset = .20f, radius = .30f;
                    float lateral = offset*Mathf.Sin(phase);
                    float envelope = offset*Mathf.Abs(Mathf.Cos(phase))+Mathf.Sqrt(radius*radius-lateral*lateral);
                    float y = envelope*Mathf.Sin(angle);
                    float h = .84f*envelope*Mathf.Cos(angle)-.008f;
                    rope.Vertex(new Vector2(x,y),h,new Vector2(x,v-.5f));
                }
            }
            rope.GridFaces(stations,arcs);
            rope.startCap = rope.RopeEnd(0,arcs,false);
            rope.endCap = rope.RopeEnd(stations*(arcs+1),arcs,true);
            return rope;
        }

        FrameOrnament RopeEnd(int first,int arcs,bool last)
        {
            float x = points[first].x;
            // Both end faces get a separate UV strip above the unwrapped cord.
            float lane = last ? 1.20f : .62f;
            var cap = new FrameOrnament(length);
            int center = cap.Vertex(new Vector2(x,0),-.008f,new Vector2(0,lane-.008f));
            for (int j=0;j<=arcs;j++)
            {
                var p = points[first+j];
                cap.Vertex(new Vector2(x,p.y),p.z,new Vector2(p.y,lane+p.z));
                if (j == 0) continue;
                if (last) cap.Face(center,center+j+1,center+j);
                else cap.Face(center,center+j,center+j+1);
            }
            return cap;
        }

        static FrameOrnament MakeFlute()
        {
            var flute = new FrameOrnament(.46f);
            const int columns = 20, rows = 24;
            for (int i=0;i<=columns;i++)
            {
                float u = 2f*i/columns-1;
                for (int j=0;j<=rows;j++)
                {
                    float v = 2f*j/rows-1;
                    var p = new Vector2(u*.5f*flute.length,v*.5f);
                    // A stopped cylindrical flute. Its semicircular ends die into the fillet.
                    float end = Mathf.Max(0,Mathf.Abs(v)-.47f)/.39f;
                    float bowl = Mathf.Sqrt(Mathf.Max(0,1-u*u/(.79f*.79f)-end*end));
                    float crown = Mathf.SmoothStep(0,1,(1-Mathf.Abs(v))/.17f)*.168f;
                    flute.Vertex(p,crown-.152f*bowl-.006f,p);
                }
            }
            flute.GridFaces(columns,rows);
            // Carry the uncut fillet between scoops as their spacing changes.
            var spacer = new FrameOrnament(1);
            for (int i=0;i<2;i++)
                for (int j=0;j<=rows;j++)
                {
                    var edge = flute.points[j];
                    var p = new Vector2(i-.5f,edge.y);
                    spacer.Vertex(p,edge.z,p);
                }
            spacer.GridFaces(1,rows);
            flute.spacer = spacer;
            return flute;
        }

        void GridFaces(int columns,int rows)
        {
            for (int i=0;i<columns;i++)
                for (int j=0;j<rows;j++)
                {
                    int a = i*(rows+1)+j, b = a+rows+1;
                    Face(a,b,b+1); Face(a,b+1,a+1);
                }
        }

        static FrameOrnament MakeEggAndDart()
        {
            var motif = new FrameOrnament(1.20f);
            const float eggX = -.18f;
            // The cup sweeps around the narrow end of the egg, towards the artwork.
            motif.Stroke(t => {
                float a = Mathf.Lerp(-2.24f,2.24f,t);
                return new Vector2(eggX+.369f*Mathf.Sin(a),.023f-.458f*Mathf.Cos(a));
            },.047f,.097f,false,28);

            const int rings = 6, sides = 24;
            var center = new Vector2(eggX,.055f);
            int pole = motif.Vertex(center,.27f,center+Vector2.up);
            for (int ring=1;ring<=rings;ring++)
            {
                float latitude = ring*Mathf.PI/(2*rings);
                float r = Mathf.Sin(latitude);
                int first = motif.points.Count;
                for (int j=0;j<sides;j++)
                {
                    float angle = 2*Mathf.PI*j/sides;
                    float y = r*Mathf.Sin(angle);
                    var p = center+new Vector2(.282f*r*Mathf.Cos(angle)*(1+.27f*y),.38f*y);
                    motif.Vertex(p,.278f*Mathf.Cos(latitude)-.008f,p+Vector2.up);
                }
                for (int j=0;j<sides;j++)
                {
                    int a=first+j, b=first+(j+1)%sides;
                    if (ring == 1) motif.Face(pole,a,b);
                    else { motif.Face(a-sides,a,b); motif.Face(a-sides,b,b-sides); }
                }
            }

            // A spearhead with two bevelled faces and a narrow stem between adjacent cups.
            const float dartX = .404f;
            int start = motif.points.Count;
            void DartPoint(float x,float y,float h)
            {
                var p = new Vector2(dartX+x,y);
                motif.Vertex(p,h,p+Vector2.up);
            }
            DartPoint(0,-.465f,-.006f);
            void DartRow(float width,float y,float height)
            {
                DartPoint(-width,y,-.006f); DartPoint(0,y,height); DartPoint(width,y,-.006f);
            }
            DartRow(.153f,.036f,.155f);
            DartRow(.054f,.121f,.092f);
            DartRow(.043f,.390f,.078f);
            DartPoint(0,.455f,-.006f);
            motif.Face(start,start+2,start+1); motif.Face(start,start+3,start+2);
            for (int row=0;row<2;row++)
                for (int col=0;col<2;col++)
                {
                    int a=start+1+3*row+col;
                    motif.Face(a,a+1,a+4); motif.Face(a,a+4,a+3);
                }
            motif.Face(start+7,start+8,start+10); motif.Face(start+8,start+9,start+10);
            return motif;
        }
    }
}
