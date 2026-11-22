using System.Collections.Generic;
using UnityEngine;

namespace JLink.Plants.Studio
{
    public sealed partial class WindowsillWorkshop
    {
        GameObject guide;
        Mesh guideMesh;
        Material guideMaterial;
        readonly List<Vector3> guidePoints=new List<Vector3>();
        readonly List<int> guideLines=new List<int>();
        readonly List<Color> guideColors=new List<Color>();
        Color lineColor;

        GameObject Child(string label,Transform parent)
        {
            var go=new GameObject(label){hideFlags=HideFlags.DontSave};
            go.transform.SetParent(parent,false);
            return go;
        }

        void BuildTray()
        {
            ClearTray();
            trayRoot=Child("Variation tray preview",transform);
            trayRoot.transform.localPosition=new Vector3(0,0,-30);
            previews=new ProceduralPlant[6];
            Bounds common=new Bounds(Vector3.down*.1f,new Vector3(.33f,.27f,.33f));
            for(int i=0;i<6;i++)
            {
                var holder=Child("Tray position "+(i+1),trayRoot.transform);
                var plant=Child("Seed "+slots[i].seed,holder.transform).AddComponent<ProceduralPlant>();
                slots[i].specimen.Restore(plant);
                previews[i]=plant;
                common.Encapsulate(plant.CurrentMesh.bounds);
                Child("Planter",holder.transform).AddComponent<PlantStudioStage>().SetPlanterOnly();
                var support=plant.Supports;
                if (support.pole)
                {
                    var pole=Child("Moss pole",holder.transform);
                    pole.transform.localPosition=new Vector3(support.poleXZ.x,0,support.poleXZ.y);
                    pole.SetActive(false);
                    pole.AddComponent<WindowsillScenery>().mossPole=true;
                    pole.SetActive(true);
                    common.Encapsulate(new Vector3(support.poleXZ.x,support.poleHeight,support.poleXZ.y));
                }
            }
            float dx=Mathf.Max(.64f,common.size.x+.15f);
            float dy=Mathf.Max(.72f,common.size.y+.23f);
            for(int i=0;i<6;i++)
                previews[i].transform.parent.localPosition=new Vector3((i%3-1)*dx,(i<3?.5f:-.5f)*dy,0);
            trayCenter=trayRoot.transform.position+common.center;
            trayExtents=new Vector2(dx+common.extents.x,dy*.5f+common.extents.y+.08f);
        }

        void ClearTray() { DisposeObject(ref trayRoot); previews=null; }

        public int[] CuttableNodes()
        {
            var result=new List<int>();
            if (!Ready || Active.Growth==null || (useNursery && frozen)) return result.ToArray();
            var g=Active.Growth;
            for(int i=0;i<g.OrganCount;i++)
            {
                var n=g.NodeAt(i);
                if (g.CanCut(n.Id)) result.Add(n.Id);
            }
            return result.ToArray();
        }

        void UpdateCutGuide()
        {
            bool pruning=view==View.Cuttings && !HasCutting;
            bool inspect=placementPreview || (showTips && (view==View.Grow || IsComparing));
            var plant=placementPreview ? placementPreview : Active;
            if ((!pruning && !inspect) || !Ready || plant.Growth==null || (useNursery && frozen))
            { if(guide) guide.SetActive(false); tipReport=""; return; }
            var g=plant.Growth;
            bool spider=pruning && g.Recipe.species==PlantSpecies.SpiderPlant;
            if (pruning && !(spider ? g.CanDetachPlantlet(selectedNode) : g.CanCut(selectedNode)))
            {
                var ids=spider ? ReadyPlantlets() : CuttableNodes();
                selectedNode=ids.Length==0 ? 0 : ids[ids.Length/2];
            }
            if (pruning && selectedNode==0) { if(guide) guide.SetActive(false); return; }
            if (!guide)
            {
                guide=Child("Proposed cut",Active.transform);
                guideMesh=new Mesh {name="Cut preview",hideFlags=HideFlags.DontSave};
                guideMaterial=new Material(Resources.Load<Shader>("JLinkPlants/CutGuide")){hideFlags=HideFlags.DontSave};
                guide.AddComponent<MeshFilter>().sharedMesh=guideMesh;
                guide.AddComponent<MeshRenderer>().sharedMaterial=guideMaterial;
            }
            guide.SetActive(true);
            guide.transform.SetParent(placementPreview ? nursery.transform.parent : plant.transform,false);
            guide.transform.localPosition=Vector3.zero;
            guidePoints.Clear(); guideLines.Clear(); guideColors.Clear();
            if(inspect) DrawGrowthGuide(plant);
            else if(spider)
            {
                lineColor=new Color(1,.70f,.10f);
                var socket=g.PlantletPosition(selectedNode);
                Ring(socket,.038f,Vector3.right,Vector3.up); Ring(socket,.038f,Vector3.forward,Vector3.up);
            }
            else DrawCut(g);
            guideMesh.Clear(); guideMesh.SetVertices(guidePoints); guideMesh.SetColors(guideColors);
            guideMesh.SetIndices(guideLines,MeshTopology.Lines,0);
        }

        void DrawCut(PlantGrowth g)
        {
            lineColor=new Color(1,.37f,.035f);
            var branch=new HashSet<int>(g.PreviewCut(selectedNode));
            for(int i=0;i<g.OrganCount;i++)
            {
                var n=g.NodeAt(i);
                if(!branch.Contains(n.Id)) continue;
                Line(n.Base,n.Tip);
                if(n.Id!=selectedNode) continue;
                Ring(n.Base,.023f,Vector3.right,Vector3.up);
            }
        }

        void DrawGrowthGuide(ProceduralPlant plant)
        {
            var g=plant.Growth;
            if(!g.Profile.IsVine)
            { tipReport="Tip guides are available on vines.";return; }
            int vine=placementPreview || g.IsRootedCutting ? 0 : Mathf.Clamp(selectedVine,0,g.Recipe.crowns);
            lineColor=new Color(.12f,.91f,.92f);
            int marked=0;
            for(int i=0;i<g.OrganCount;i++)
            {
                var n=g.NodeAt(i);
                if(n.Contact==PlantContact.Free || (vine!=0 && vine!=n.ShootId)) continue;
                Line(n.Base,g.VisibleVineTip(n.Id)); marked++;
            }
            if(!g.TryPreviewVine(vine,plant.Conditions,plant.Supports,out var next))
            { tipReport=$"No further bud on this vine. {marked} supported segments.";return; }
            lineColor=new Color(1,.76f,.12f);
            Vector3 bud=next.ParentId==0 ? next.Base : g.VisibleVineTip(next.ParentId);
            Ring(bud,.021f,Vector3.right,Vector3.up);
            Ring(bud,.021f,Vector3.forward,Vector3.up);
            for(int i=0;i<6;i+=2) Line(Vector3.Lerp(next.Base,next.Tip,i/6f),Vector3.Lerp(next.Base,next.Tip,(i+1)/6f));
            string reach=next.Contact==PlantContact.Free ? "free growth" : "reaches "+next.Contact.ToString().ToLowerInvariant();
            string gap="";
            if(plant.Supports.pole)
            {
                float metres=Vector2.Distance(new Vector2(next.Base.x,next.Base.z),plant.Supports.poleXZ)-plant.Supports.poleRadius;
                gap=$" · pole gap {Mathf.Max(0,metres)*100:0.#} cm";
                if(next.Base.y>=plant.Supports.poleHeight) gap=" · at or above pole top";
                else if(next.Base.y<=-.03f) gap=" · below pole base";
            }
            tipReport=$"Gold · vine {next.ShootId}, bud {next.ParentId}\nNext segment {reach}{gap}\nCyan · {marked} supported segments";
        }

        void Ring(Vector3 center,float radius,Vector3 x,Vector3 y)
        {
            for(int j=0;j<24;j++)
            {
                float a=j*Mathf.PI/12,b=(j+1)*Mathf.PI/12;
                Line(center+radius*(x*Mathf.Cos(a)+y*Mathf.Sin(a)),center+radius*(x*Mathf.Cos(b)+y*Mathf.Sin(b)));
            }
        }
        void Line(Vector3 a,Vector3 b)
        {
            guideLines.Add(guidePoints.Count);guidePoints.Add(a);
            guideLines.Add(guidePoints.Count);guidePoints.Add(b);
            guideColors.Add(lineColor);guideColors.Add(lineColor);
        }
        void ClearGuide()
        {
            DisposeObject(ref guide);
            if (guideMesh) { if (Application.isPlaying) Destroy(guideMesh); else DestroyImmediate(guideMesh); }
            if (guideMaterial) { if (Application.isPlaying) Destroy(guideMaterial); else DestroyImmediate(guideMaterial); }
        }

        bool potCloseUp;
        public bool PotCloseUp => potCloseUp && view!=View.Tray;

        public void SetPotCloseUp(bool enabled)
        {
            potCloseUp=enabled;
            zoom=1;
            FrameCamera();
        }

        float CloseUpSize(Bounds bounds,Quaternion rotation,float available)
        {
            // Project the world AABB into camera space so orbiting a deep crown keeps it in frame.
            Vector3 extent=bounds.extents;
            float Radius(Vector3 axis) => Mathf.Abs(axis.x)*extent.x + Mathf.Abs(axis.y)*extent.y + Mathf.Abs(axis.z)*extent.z;
            float extentInView=Mathf.Max(Radius(rotation*Vector3.up),Radius(rotation*Vector3.right)/available);
            return Mathf.Max(.064f,extentInView)*1.18f*zoom;
        }

        void FrameCamera()
        {
            if(!Ready || !mother.CurrentMesh) return;
            // Reserve a fixed fraction of the Game view for the controls.
            float scale=UiScale;
            float fraction=Application.IsPlaying(gameObject)?Mathf.Min(294*scale/Mathf.Max(1,Screen.width),.44f):0;
            float bottomFraction=0;
            if (workshop.Ready)
            {
                fraction = workshop.RailWidth / workshop.PanelWidth;
                bottomFraction = workshop.ShelfHeight / workshop.PanelHeight;
            }
            float heightFraction=Mathf.Max(.1f,1-bottomFraction);
            float aspect=Mathf.Max(.3f,viewCamera.aspect), available=aspect*(1-fraction)/heightFraction;
            Vector3 center;
            float size;
            Quaternion rotation;
            if(view==View.Tray && trayRoot)
            {
                center=trayCenter;
                rotation=Quaternion.identity;
                size=Mathf.Max(trayExtents.y,trayExtents.x/available)*1.10f;
            }
            else if(PotCloseUp)
            {
                var plant=Active;
                var renderer=plant==nursery && frozen && frozenDisplay
                    ? frozenDisplay.GetComponent<MeshRenderer>() : plant.GetComponent<MeshRenderer>();
                var bounds=renderer.bounds;
                bounds.Encapsulate(plant.transform.position+Vector3.down*.018f);
                center=bounds.center;
                rotation=Quaternion.Euler(12,orbit,0);
                size=CloseUpSize(bounds,rotation,available);
            }
            else
            {
                var bounds=new Bounds(transform.position+new Vector3(0,-.10f,0),new Vector3(1.47f,.40f,.55f));
                bounds.Encapsulate(mother.GetComponent<MeshRenderer>().bounds);
                if(occupied && !frozen && nursery.CurrentMesh) bounds.Encapsulate(nursery.GetComponent<MeshRenderer>().bounds);
                if(frozenDisplay) bounds.Encapsulate(frozenDisplay.GetComponent<MeshRenderer>().bounds);
                if(placementPreview) bounds.Encapsulate(placementPreview.GetComponent<MeshRenderer>().bounds);
                if(motherClimbs) bounds.Encapsulate(motherPole.transform.position+Vector3.up*.92f);
                if(nurseryPole.activeSelf) bounds.Encapsulate(nurseryPole.transform.position+Vector3.up*.92f);
                center=bounds.center;
                rotation=Quaternion.Euler(8,orbit,0);
                size=Mathf.Max(bounds.extents.y+.16f,(bounds.extents.x+.12f)/available)*1.08f*zoom;
            }
            size /= heightFraction;
            viewCamera.orthographicSize=size;
            viewCamera.transform.SetPositionAndRotation(center-rotation*Vector3.right*(size*aspect*fraction)-rotation*Vector3.up*(size*bottomFraction)-rotation*Vector3.forward*4,rotation);
        }
    }
}
