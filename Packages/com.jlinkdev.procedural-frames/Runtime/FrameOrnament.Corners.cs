using UnityEngine;

namespace JLink.Frames
{
    internal sealed partial class FrameOrnament
    {
        static readonly FrameOrnament Fan = MakeFan();
        static readonly FrameOrnament Curls = MakeCurls();
        static readonly FrameOrnament Shell = MakeShell();

        internal static FrameOrnament Corner(CornerStyle style)
        {
            switch (style)
            {
                case CornerStyle.LeafFan: return Fan;
                case CornerStyle.TwinScroll: return Curls;
                case CornerStyle.ScallopShell: return Shell;
                default: return null;
            }
        }

        float mitreMiddle, mitreScale;
        internal float mitreReach, arcReach;
        // The planar motif uses an orthonormal basis along and across the diagonal. Measure
        // its actual distance from the sight edge before fitting it to a rectangular rail.
        internal Vector2 CornerCoordinates(Vector3 p,bool mitred)
        {
            if (!mitred) return new Vector2(p.x,p.y);
            float across=(p.y+Mathf.Abs(p.x))*.70710678f;
            return new Vector2(1.41421356f*p.x*mitreScale,(across-mitreMiddle)*mitreScale);
        }

        // Normalize the crest so relief is an independent measurement in metres.
        FrameOrnament NormalizeCrest()
        {
            float crest=0;
            float low=float.PositiveInfinity, high=float.NegativeInfinity;
            foreach (var p in points)
            {
                crest=Mathf.Max(crest,p.z);
                float across=(p.y+Mathf.Abs(p.x))*.70710678f;
                low=Mathf.Min(low,across); high=Mathf.Max(high,across);
            }
            mitreMiddle=(high+low)*.5f;
            mitreScale=1/(high-low);
            for (int i=0;i<points.Count;i++)
            {
                var p=points[i]; p.z/=crest; points[i]=p;
                mitreReach=Mathf.Max(mitreReach,Mathf.Abs(CornerCoordinates(p,true).x));
                arcReach=Mathf.Max(arcReach,Mathf.Abs(p.x));
            }
            return this;
        }

        static FrameOrnament MakeFan()
        {
            var fan=new FrameOrnament(1);
            for (int wing=-2;wing<=2;wing++)
            {
                float s=wing;
                float reach=Mathf.Abs(s);
                fan.Stroke(t => Bezier(t,new Vector2(.022f*s,-.40f),
                    new Vector2(.035f*s,-.13f),new Vector2(s*(.17f+reach*.08f),.04f),
                    new Vector2(s*(.15f+reach*.13f),.46f-.155f*reach)),
                    .112f-.009f*reach,.21f-.035f*reach,true,14);
            }
            return fan.NormalizeCrest();
        }

        static FrameOrnament MakeCurls()
        {
            var curls=new FrameOrnament(1);
            foreach (int sign in new[]{-1,1})
            {
                curls.Stroke(t => {
                    Vector2 p;
                    if (t<.45f)
                        p=Bezier(t/.45f,new Vector2(0,-.33f),new Vector2(.16f,-.30f),
                            new Vector2(.35f,.27f),new Vector2(.67f,.27f));
                    else
                    {
                        float u=(t-.45f)/.55f;
                        float a=Mathf.Lerp(Mathf.PI*.5f,-Mathf.PI*1.45f,u);
                        float radius=Mathf.Lerp(.265f,.064f,u);
                        p=new Vector2(.67f+radius*Mathf.Cos(a),.005f+radius*Mathf.Sin(a));
                    }
                    return new Vector2(p.x*sign,p.y);
                },.047f,.15f,false,32);
                curls.Stroke(t => Bezier(t,new Vector2(.17f*sign,-.16f),new Vector2(.32f*sign,-.17f),
                    new Vector2(.43f*sign,-.37f),new Vector2(.69f*sign,-.33f)),.08f,.12f,true,12);
            }
            curls.Stroke(t => new Vector2(0,Mathf.Lerp(-.35f,.32f,t)),.105f,.19f,true,14);
            return curls.NormalizeCrest();
        }

        static FrameOrnament MakeShell()
        {
            var shell=new FrameOrnament(1);
            const int rays=42,rings=9;
            var root=new Vector2(0,-.42f);
            shell.Vertex(root,-.005f,root);
            for (int i=1;i<=rings;i++)
            {
                float t=i/(float)rings;
                for (int j=0;j<=rays;j++)
                {
                    float u=j/(float)rays;
                    float angle=(u-.5f)*2.64f;
                    float lobe=.5f-.5f*Mathf.Cos(14*Mathf.PI*u);
                    var p=root+t*(.91f+.09f*lobe)*new Vector2(.92f*Mathf.Sin(angle),.89f*Mathf.Cos(angle));
                    float edge=Mathf.Clamp01(22*Mathf.Min(u,1-u));
                    float arch=Mathf.Sin(Mathf.PI*t)*edge*(.085f+.115f*lobe);
                    shell.Vertex(p,arch-.005f,p);
                }
                int first=1+(i-1)*(rays+1);
                for (int j=0;j<rays;j++)
                {
                    int a=first+j;
                    if (i==1) shell.Face(0,a+1,a);
                    else { int b=a-rays-1; shell.Face(b,b+1,a+1); shell.Face(b,a+1,a); }
                }
            }
            return shell.NormalizeCrest();
        }
    }
}
