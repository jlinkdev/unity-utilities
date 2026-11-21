using System.Collections.Generic;
using UnityEngine;

namespace JLink.Plants.Studio
{
    [ExecuteAlways,RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public sealed class WindowsillScenery : MonoBehaviour
    {
        public bool mossPole;
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Color32> colors=new List<Color32>();
        readonly List<int> indices=new List<int>();
        readonly List<Vector2> surfaces=new List<Vector2>();
        MaterialPropertyBlock properties;
        static readonly int PotCenters=Shader.PropertyToID("_PotCenters");
        Mesh mesh;

        void OnEnable()
        {
            vertices.Clear(); colors.Clear(); indices.Clear(); surfaces.Clear();
            if (mossPole) Pole(); else Window();
            mesh=new Mesh {name=mossPole?"Coir support":"Oak windowsill",hideFlags=HideFlags.DontSave};
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(indices,0); mesh.RecalculateNormals();
            if(mossPole)
                for(int i=0;i<vertices.Count;i++) surfaces.Add(new Vector2((float)WindowsillSurface.Coir,0));
            mesh.SetUVs(0,surfaces);
            GetComponent<MeshFilter>().sharedMesh=mesh;
            GetComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>("JLinkPlants/WindowsillDaylight");
        }

        internal void SetPotPositions(Vector3 first,Vector3 second)
        {
            var a=transform.InverseTransformPoint(first);
            var b=transform.InverseTransformPoint(second);
            var renderer=GetComponent<MeshRenderer>();
            properties ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetVector(PotCenters,new Vector4(a.x,a.z,b.x,b.z));
            renderer.SetPropertyBlock(properties);
        }

        void Window()
        {
            Color wood=new Color(.77f,.62f,.46f), frame=new Color(.83f,.84f,.78f);
            Box(new Vector3(0,-.267f,.08f),new Vector3(2.5f,.076f,.84f),wood,WindowsillSurface.Oak);
            Box(new Vector3(0,-.317f,.41f),new Vector3(2.45f,.065f,.13f),frame);
            Box(new Vector3(0,-2.32f,.55f),new Vector3(10,4,.09f),new Color(.65f,.72f,.66f));
            // A recessed sash with sky-coloured panes and narrow glazing bars.
            Box(new Vector3(0,2.16f,.72f),new Vector3(9,5,.07f),Color.white,WindowsillSurface.Glass);
            for (int i=-3;i<=3;i++)
            {
                Box(new Vector3(i*.76f,1.34f,.54f),new Vector3(.035f,3.25f,.070f),frame);
                Box(new Vector3(i*.76f+.021f,1.34f,.59f),new Vector3(.012f,3.25f,.05f),new Color(.78f,.80f,.74f));
            }
            foreach(float y in new[]{-.18f,.88f,1.94f})
                Box(new Vector3(0,y,.54f),new Vector3(6.4f,.034f,.075f),frame);
        }

        void Pole()
        {
            const int rows=64,sides=14;
            for(int i=0;i<=rows;i++)
            for(int j=0;j<=sides;j++)
            {
                float t=i/(float)rows, a=2*Mathf.PI*j/sides;
                float fiber=Mathf.Sin(a*5+i*.91f)*Mathf.Sin(i*1.41f-a*3);
                float radius=.032f+.0017f*fiber;
                vertices.Add(new Vector3(radius*Mathf.Cos(a),.92f*t,radius*Mathf.Sin(a)));
                colors.Add(Color.Lerp(new Color(.32f,.24f,.13f),new Color(.61f,.51f,.28f),.5f+.48f*fiber));
                if(i>0 && j>0)
                {
                    int b=vertices.Count-1, a0=b-sides-1;
                    Quad(a0-1,b-1,b,a0);
                }
            }
            int center=vertices.Count;
            vertices.Add(Vector3.up*.92f); colors.Add(new Color(.47f,.36f,.21f));
            for(int j=0;j<sides;j++) { indices.Add(center); indices.Add(center-sides+j); indices.Add(center-sides-1+j); }
        }

        void Box(Vector3 center,Vector3 size,Color tint,WindowsillSurface surface=WindowsillSurface.Frame)
        {
            foreach(var n in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
            {
                var x=n.y==0?Vector3.up:Vector3.right;
                var y=Vector3.Cross(n,x);
                int k=vertices.Count;
                foreach(var corner in new[]{n-x-y,n+x-y,n+x+y,n-x+y})
                { vertices.Add(center+Vector3.Scale(corner,size)*.5f); colors.Add(tint); surfaces.Add(new Vector2((float)surface,0)); }
                Quad(k,k+1,k+2,k+3);
            }
        }
        void Quad(int a,int b,int c,int d)
        { indices.Add(a); indices.Add(b); indices.Add(c); indices.Add(a); indices.Add(c); indices.Add(d); }

        void OnDisable()
        {
            if(!mesh) return;
            GetComponent<MeshFilter>().sharedMesh=null;
            if(Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
        }
    }
}
