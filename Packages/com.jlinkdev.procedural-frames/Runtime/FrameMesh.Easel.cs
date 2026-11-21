using System.Collections.Generic;
using UnityEngine;

namespace JLink.Frames
{
    public static partial class FrameMesh
    {
        sealed partial class Builder
        {
            void AddEaselBack(FrameContour insert)
            {
                float width=r.artworkSize.x+2*r.Border;
                float height=r.artworkSize.y+2*r.Border;
                // Limit rear overhang so a narrow, inward-sculpted rail still touches the table first.
                float thickness=Mathf.Min(r.railWidth*.20f,Mathf.Clamp(Mathf.Min(width,height)*.018f,.001f,.005f));
                var perimeter=new List<Vector2>();
                foreach (var rail in insert.rails)
                    for (int i=0;i<rail.Count-1;i++)
                        perimeter.Add(rail[i].At(r.Border+r.railWidth*.08f,true));

                // Overlap the rebate slightly. The panel stays inside even a pinched waist.
                float panelFront=r.railDepth-thickness*.12f;
                EaselPrism(perimeter,Vector3.forward*panelFront,Vector3.right,Vector3.up,thickness);
                float halfLeg=Mathf.Min(width*.16f,height*.23f);
                float radius=Mathf.Min(thickness,halfLeg*.22f);
                Vector3 hinge=new Vector3(0,height*.18f,panelFront+thickness+radius*.80f);
                var circle=new List<Vector2>(16);
                for (int i=0;i<16;i++)
                {
                    float angle=2*Mathf.PI*i/16;
                    circle.Add(radius*new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)));
                }
                EaselPrism(circle,hinge-Vector3.right*halfLeg,Vector3.up,Vector3.forward,2*halfLeg);

                // Pose the actual sculpted frame, including relief, before cutting the foot flat.
                Quaternion rotation=Quaternion.AngleAxis(r.leanAngle,Vector3.right);
                float lowest=float.PositiveInfinity;
                foreach (var vertex in vertices) lowest=Mathf.Min(lowest,(rotation*vertex).y);
                Vector3 lift=Vector3.down*lowest;
                outline.designToMesh=Matrix4x4.TRS(lift,rotation,Vector3.one);
                for (int i=0;i<vertices.Count;i++)
                {
                    vertices[i]=rotation*vertices[i]+lift;
                    normals[i]=rotation*normals[i];
                }

                Vector3 top=rotation*hinge+lift;
                Vector3 foot=new Vector3(0,0,top.z+top.y*.43f);
                Vector3 topWidth=Vector3.right*(halfLeg*.78f), footWidth=Vector3.right*halfLeg;
                Vector3 depth=Vector3.forward*(radius*.55f);
                Vector3 a=foot-footWidth-depth,b=foot+footWidth-depth;
                Vector3 c=top+topWidth-depth,d=top-topWidth-depth;
                Vector3 back=2*depth;
                EaselQuad(a,d,c,b);
                EaselQuad(a+back,b+back,c+back,d+back);
                EaselQuad(a,b,b+back,a+back);
                EaselQuad(d,d+back,c+back,c);
                EaselQuad(a,a+back,d+back,d);
                EaselQuad(b,c,c+back,b+back);
            }

            // Extrude a convex counter-clockwise outline. Each cap and the unrolled rim get a UV chart.
            void EaselPrism(List<Vector2> polygon,Vector3 origin,Vector3 u,Vector3 v,float depth)
            {
                Vector3 axis=Vector3.Cross(u,v);
                Vector3 Point(Vector2 p) => origin+u*p.x+v*p.y;
                Vector3 extrusion=axis*depth;
                for (int cap=0;cap<2;cap++)
                {
                    StartChart();
                    int centre=vertices.Count;
                    Vector3 shift=cap*extrusion, normal=cap==0 ? -axis : axis;
                    Add(origin+shift,normal,Vector2.zero);
                    foreach (var p in polygon) Add(Point(p)+shift,normal,p);
                    for (int i=0;i<polygon.Count;i++)
                    {
                        int a=centre+1+i, b=centre+1+(i+1)%polygon.Count;
                        Triangle(FrameSlot.EaselBack,centre,cap==0 ? b : a,cap==0 ? a : b);
                    }
                    EndChart();
                }
                StartChart();
                float distance=0;
                for (int i=0;i<polygon.Count;i++)
                {
                    Vector3 a=Point(polygon[i]),b=Point(polygon[(i+1)%polygon.Count]);
                    Vector3 normal=Vector3.Cross((b-a).normalized,axis).normalized;
                    int start=vertices.Count;
                    float next=distance+Vector3.Distance(a,b);
                    Add(a,normal,new Vector2(distance,0)); Add(b,normal,new Vector2(next,0));
                    Add(b+extrusion,normal,new Vector2(next,depth)); Add(a+extrusion,normal,new Vector2(distance,depth));
                    Quad(FrameSlot.EaselBack,start,start+1,start+2,start+3);
                    distance=next;
                }
                EndChart();
            }

            void EaselQuad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                Vector3 u=(b-a).normalized;
                Vector3 normal=Vector3.Cross(u,d-a).normalized;
                Vector3 v=Vector3.Cross(normal,u);
                Vector2 Tex(Vector3 p) => new Vector2(Vector3.Dot(p-a,u),Vector3.Dot(p-a,v));
                StartChart();
                int start=vertices.Count;
                Add(a,normal,Vector2.zero); Add(b,normal,Tex(b)); Add(c,normal,Tex(c)); Add(d,normal,Tex(d));
                Quad(FrameSlot.EaselBack,start,start+1,start+2,start+3);
                EndChart();
            }
        }
    }
}
