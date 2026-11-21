using System.Collections.Generic;
using UnityEngine;

namespace JLink.Plants.Studio
{
    // The planting point is at local zero. This independent pot is only a sample prop.
    [ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class PlantStudioStage : MonoBehaviour
    {
        readonly List<Vector3> positions = new List<Vector3>();
        readonly List<Color32> colors = new List<Color32>();
        readonly List<int> faces = new List<int>();
        Mesh mesh;
        [SerializeField] bool planterOnly;

        void OnEnable()
        {
            positions.Clear(); colors.Clear(); faces.Clear();
            Color clay = new Color(.69f, .40f, .29f);
            Color glaze = new Color(.87f, .73f, .58f);
            Ring(-.229f, .104f, -.219f, .116f, clay);
            Ring(-.219f, .116f, -.187f, .119f, clay);
            Ring(-.187f, .119f, -.048f, .145f, glaze, true);
            Ring(-.048f, .145f, -.026f, .149f, glaze);
            Ring(-.026f, .149f, -.023f, .153f, clay);
            Ring(-.023f, .153f, .009f, .153f, clay);
            Ring(.009f, .153f, .013f, .149f, clay);
            Ring(.013f, .149f, .013f, .137f, glaze);
            Ring(.013f, .137f, -.012f, .135f, clay);
            int soilStart=positions.Count;
            if (planterOnly) Soil();
            else Ring(-.010f, .136f, -.003f, .0001f, new Color(.23f, .17f, .13f));
            // A rectangular oak shelf with a lighter eased edge.
            if (!planterOnly) AddShelf();
            mesh = new Mesh { name = planterOnly ? "Fluted planter" : "Fluted planter and oak shelf", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(positions); mesh.SetColors(colors); mesh.SetTriangles(faces, 0);
            if (planterOnly)
            {
                var surface = new Vector2[positions.Count];
                for(int i=soilStart;i<surface.Length;i++) surface[i].x=(float)WindowsillSurface.Soil;
                mesh.uv=surface;
            }
            mesh.RecalculateNormals();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>(
                planterOnly ? "JLinkPlants/WindowsillDaylight" : "JLinkPlants/Botanical");
        }

        void Soil()
        {
            const int sides=72, rings=9;
            int center=positions.Count;
            positions.Add(Vector3.down*.003f); colors.Add(Color.white);
            for(int ring=1;ring<=rings;ring++)
            for(int side=0;side<sides;side++)
            {
                float t=ring/(float)rings, angle=2*Mathf.PI*side/sides;
                float x=.136f*t*Mathf.Cos(angle), z=.136f*t*Mathf.Sin(angle);
                float y=-.003f-.007f*t*t+.0024f*(Mathf.PerlinNoise(42+x*42,17+z*42)-.5f);
                positions.Add(new Vector3(x,y,z)); colors.Add(Color.white);
                int current=center+1+(ring-1)*sides+side;
                int next=center+1+(ring-1)*sides+(side+1)%sides;
                if(ring==1) { faces.Add(center); faces.Add(next); faces.Add(current); }
                else Face(current-sides,next-sides,next,current);
            }
        }

        public void SetPlanterOnly()
        {
            if (planterOnly) return;
            planterOnly=true;
            if (isActiveAndEnabled) { OnDisable(); OnEnable(); }
        }

        void AddShelf()
        {
            Box(new Vector3(0, -.254f, .14f), new Vector3(.51f, .05f, .66f), new Color(.74f,.56f,.41f), true);
            Box(new Vector3(-.16f, -.34f, .35f), new Vector3(.035f, .18f, .24f), new Color(.36f,.32f,.27f));
            Box(new Vector3(.16f, -.34f, .35f), new Vector3(.035f, .18f, .24f), new Color(.36f,.32f,.27f));
            Surface(true);
            Surface(false);
        }

        void Ring(float y0, float r0, float y1, float r1, Color color, bool ribbed = false)
        {
            int start = positions.Count;
            const int sides = 192;
            int bands = ribbed ? 3 : 1;
            for (int row=0; row<=bands; row++)
            for (int i = 0; i < sides; i++)
            {
                float h = ribbed && row==1 ? .09f : ribbed && row==2 ? .91f : row/(float)bands;
                float t = 2 * Mathf.PI * i / sides;
                // The relief ends flush with the smooth glaze bands on both sides.
                float flute = row>0 && row<bands ? .002f * Mathf.Pow(.5f + .5f * Mathf.Cos(32*t), 3) : 0;
                var dir = new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t));
                positions.Add(dir*(Mathf.Lerp(r0,r1,h)+flute)+Vector3.up*Mathf.Lerp(y0,y1,h));
                colors.Add(color);
                if (row>0)
                {
                    int a=start+(row-1)*sides+i, b=start+(row-1)*sides+(i+1)%sides;
                    Face(a,a+sides,b+sides,b);
                }
            }
        }

        void Face(int a, int b, int c, int d)
        {
            faces.Add(a); faces.Add(b); faces.Add(c); faces.Add(a); faces.Add(c); faces.Add(d);
        }

        void Box(Vector3 center, Vector3 size, Color color, bool openTop = false)
        {
            foreach (var n in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
            {
                if (n == Vector3.up && openTop) continue;
                var x = n.y == 0 ? Vector3.up : Vector3.right;
                var y = Vector3.Cross(n, x);
                int k = positions.Count;
                positions.Add(center + Vector3.Scale(n-x-y,size)*.5f);
                positions.Add(center + Vector3.Scale(n+x-y,size)*.5f);
                positions.Add(center + Vector3.Scale(n+x+y,size)*.5f);
                positions.Add(center + Vector3.Scale(n-x+y,size)*.5f);
                for (int i=0; i<4; i++) colors.Add(color);
                Face(k,k+1,k+2,k+3);
            }
        }

        void Surface(bool shelf)
        {
            const int nx = 64, ny = 44;
            int start = positions.Count;
            for (int j=0; j<=ny; j++)
            for (int i=0; i<=nx; i++)
            {
                float u = i/(float)nx, v = j/(float)ny;
                Vector3 point;
                Color color;
                if (shelf)
                {
                    float x = Mathf.Lerp(-.255f,.255f,u), z = Mathf.Lerp(-.19f,.47f,v);
                    point = new Vector3(x,-.229f,z);
                    float contact = Mathf.Exp(-45f*(x*x + z*z));
                    float grain = .012f*Mathf.Sin(77*z + .6f*Mathf.Sin(8*x));
                    color = new Color(.78f,.63f,.48f)*(1f-.35f*contact+grain);
                }
                else
                {
                    // Concentrate samples around the shelf while extending the wall behind long vines.
                    float sx = u*2-1, sy = v*2-1;
                    float x = 2.4f*sx + 6f*sx*sx*sx, y = .2f + 2.2f*sy + 6f*sy*sy*sy;
                    point = new Vector3(x,y,.473f);
                    float dx = Mathf.Max(0,Mathf.Abs(x-.035f)-.24f);
                    float shadow = Mathf.Exp(-42*dx*dx - 72*(y+.31f)*(y+.31f));
                    color = Color.Lerp(new Color(.76f,.78f,.71f),new Color(.92f,.91f,.84f),v) * (1-.23f*shadow);
                }
                positions.Add(point);
                colors.Add(color);
                if (i>0 && j>0)
                {
                    int a = start+(j-1)*(nx+1)+i-1;
                    Face(a,a+nx+1,a+nx+2,a+1);
                }
            }
        }

        void OnDisable()
        {
            if (!mesh) return;
            var filter = GetComponent<MeshFilter>();
            if (filter && filter.sharedMesh == mesh) filter.sharedMesh = null;
            if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
        }
    }
}
