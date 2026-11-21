using System;
using UnityEngine;

namespace JLink.Plants
{
    public sealed partial class PlantMesher
    {
        bool[] bladeCells = Array.Empty<bool>();
        int[] bladeVertices = Array.Empty<int>();
        Vector2[] contours = Array.Empty<Vector2>(), nextContours = Array.Empty<Vector2>();

        void Monstera(PlantOrgan o, float elapsed)
        {
            float growth=Mature(elapsed,5);
            Vector3 tip=CurvedInternode(o,PlantOrgan.StemExtension(elapsed));
            float adult=Mathf.Clamp01(o.depth/6f);
            Vector3 side=Vector3.Cross(Vector3.up,o.outward).normalized;
            Vector3 socket=tip + (o.outward*.75f+Vector3.up*.8f)*((.09f+.14f*adult)*growth);
            Tube(tip,socket,.0048f,.0032f,Stem);
            if (o.depth>=2)
            {
                float reach=(.12f+.13f*o.variation)*growth;
                var elbow=tip-o.outward*.028f+Vector3.down*(reach*.46f);
                var rootEnd=elbow+Vector3.down*(reach*.54f)-o.outward*.016f;
                var rootColor=new Color(.50f,.39f,.24f);
                Tube(tip,elbow,.0039f,.003f,rootColor);
                Tube(elbow,rootEnd,.003f,.0009f,rootColor);
            }

            int cols=Steps(16,24,32), rows=Steps(16,22,30);
            int stride=cols+1;
            if (bladeCells.Length<cols*rows) Array.Resize(ref bladeCells,cols*rows);
            if (bladeVertices.Length<stride*(rows+1)) Array.Resize(ref bladeVertices,stride*(rows+1));
            Array.Fill(bladeVertices,-1,0,stride*(rows+1));
            for (int r=0;r<rows;r++)
            for (int c=0;c<cols;c++)
            {
                float s=(c+.5f)*2/cols-1, x=Mathf.Abs(s), t=(r+.5f)/rows;
                float shift=s>0 ? .026f*o.variation : -.025f;
                float v=t-shift;
                bool slit=adult>=.32f && x>.44f && Mathf.Abs(v-(.34f+.22f*x))<.046f;
                slit |= adult>=.65f && x>.53f && Mathf.Abs(v-(.60f+.18f*x))<.040f;
                float hx=(x-.48f)/.20f, hy=(v-.265f-.10f*(x-.48f))/.084f;
                bool hole=adult>=.62f && hx*hx+hy*hy<1;
                bladeCells[r*cols+c]=!slit && !hole;
            }
            // A diagonal-only contact would join two perimeter walls along the same edge.
            // Join the two filled quadrants through a face, including at the lowest detail.
            bool repaired;
            do
            {
                repaired=false;
                for (int r=0;r<rows-1;r++)
                for (int c=0;c<cols-1;c++)
                {
                    int k=r*cols+c;
                    if (bladeCells[k]==bladeCells[k+cols+1] && bladeCells[k+1]==bladeCells[k+cols]
                        && bladeCells[k]!=bladeCells[k+1])
                    {
                        bladeCells[bladeCells[k] ? k+1 : k]=true;
                        repaired=true;
                    }
                }
            } while (repaired);

            bool Filled(int c,int r) => c>=0 && c<cols && r>=0 && r<rows && bladeCells[r*cols+c];
            int gridSize=stride*(rows+1);
            if (contours.Length<gridSize)
            {
                Array.Resize(ref contours,gridSize);
                Array.Resize(ref nextContours,gridSize);
            }
            for (int r=0;r<=rows;r++)
            for (int c=0;c<=cols;c++) contours[r*stride+c]=new Vector2(c,r);
            // Relax the two neighbours along each exposed contour in leaf UV space.
            // Limit movement to its own cell so neighbouring faces keep their winding.
            for (int pass=0;pass<3;pass++)
            {
                for (int r=0;r<=rows;r++)
                for (int c=0;c<=cols;c++)
                {
                    int k=r*stride+c;
                    Vector2 p=contours[k], sum=Vector2.zero;
                    bool sw=Filled(c-1,r-1), se=Filled(c,r-1), nw=Filled(c-1,r), ne=Filled(c,r);
                    int edges=0;
                    if (sw!=nw) { sum+=contours[k-1]; edges++; }
                    if (se!=ne) { sum+=contours[k+1]; edges++; }
                    if (sw!=se) { sum+=contours[k-stride]; edges++; }
                    if (nw!=ne) { sum+=contours[k+stride]; edges++; }
                    var corner=new Vector2(c,r);
                    nextContours[k]=edges==2 ? corner+Vector2.ClampMagnitude(Vector2.Lerp(p,sum*.5f,.56f)-corner,.59f) : p;
                }
                (contours,nextContours)=(nextContours,contours);
            }
            float length=(.145f+.28f*adult)*o.size*growth;
            int bladeStart=vertices.Count;
            var axis=(o.outward*.8f+Vector3.up*Mathf.Lerp(1.4f,-.3f,growth)).normalized;
            var face=Vector3.Cross(side,axis).normalized;
            Color color=Color.Lerp(new Color(.12f,.34f,.19f),new Color(.28f,.48f,.24f),o.variation);
            int Pair(int c,int r)
            {
                int index=r*stride+c;
                if (bladeVertices[index]>=0) return bladeVertices[index];
                float u=contours[index].x/cols, v=contours[index].y/rows;
                float s=2*u-1, edge=Mathf.Abs(s);
                float width=length*(.007f+.54f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*Mathf.Pow(v,.72f))),.66f));
                float lobe=.31f*length*edge*edge*Mathf.Exp(-Mathf.Pow((v-.18f)/.13f,2));
                Vector3 center=socket+axis*(length*v-lobe)+Vector3.down*(length*.45f*v*v)
                    +side*(width*s*Mathf.Lerp(.24f,1,growth))
                    +face*(width*(.14f*s*s+.08f*Mathf.Exp(-18*edge)+.037f*edge*Mathf.Sin(v*21+s)));
                float thickness=length*(.002f+.005f*(1-edge));
                int top=vertices.Count;
                Vertex(center+face*thickness,u,v,color,4,o.variation);
                // A small inset gives the thin perimeter walls a finite UV footprint.
                Vertex(center-face*thickness,Mathf.Lerp(u,.4731f,.009f),Mathf.Lerp(v,.5373f,.009f),color,4,o.variation);
                return bladeVertices[index]=top;
            }
            for (int r=0;r<rows;r++)
            for (int c=0;c<cols;c++)
            {
                if (!Filled(c,r)) continue;
                int a=Pair(c,r), b=Pair(c+1,r), d=Pair(c,r+1), e=Pair(c+1,r+1);
                Quad(foliage,a,b,e,d);
                Quad(foliage,a+1,d+1,e+1,b+1);
                if (!Filled(c,r-1)) Quad(foliage,b,a,a+1,b+1);
                if (!Filled(c+1,r)) Quad(foliage,e,b,b+1,e+1);
                if (!Filled(c,r+1)) Quad(foliage,d,e,e+1,d+1);
                if (!Filled(c-1,r)) Quad(foliage,a,d,d+1,a+1);
            }
            RecordBlade(bladeStart, socket);
        }
    }
}
