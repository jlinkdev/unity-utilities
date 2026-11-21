using UnityEngine;

namespace JLink.Plants
{
    public sealed partial class PlantMesher
    {
        void Spider(PlantGrowth plant,PlantOrgan o,float elapsed)
        {
            float scale=plant.SpiderScale;
            if(!PlantGrowth.IsRunner(o))
            {
                Strap(o,elapsed,Vector3.zero,scale,plant.Recipe.spread);
                return;
            }
            float extension=Mature(elapsed,3);
            Vector3 previous=o.origin*scale;
            const int spans=10;
            for(int i=1;i<=spans;i++)
            {
                var point=PlantGrowth.RunnerPoint(o,extension*i/spans,plant.Recipe.spread)*scale;
                Tube(previous,point,.0029f*scale,.0026f*scale,new Color(.58f,.64f,.30f));
                previous=point;
            }
            if(o.plantletRemoved || elapsed<=3) return;
            float babyAge=Mathf.Min(PlantGrowth.BabyLeaves,elapsed-3);
            var daughter=PlantGrowth.DaughterRecipe(plant.Recipe,o.id);
            for(int i=0;i<PlantGrowth.BabyLeaves && i<=babyAge;i++)
                Strap(PlantGrowth.BabyLeaf(daughter,i),babyAge-i,previous,PlantGrowth.PlantletScale*scale,daughter.spread);
        }

        void Strap(PlantOrgan o,float elapsed,Vector3 socket,float scale,float spread)
        {
            float growth=Mature(elapsed,3.5f), length=o.length*growth*scale;
            var right=Vector3.Cross(o.outward,Vector3.up).normalized;
            Vector3 root=socket+o.origin*scale;
            float arch=Mathf.Lerp(.85f,1.46f,o.variation);
            Vector3 Spine(float t) => root+length*(o.outward*(spread*(.22f*t+.69f*t*t))+
                Vector3.up*(arch*t-(.62f+1.12f*o.variation)*t*t*t)+right*(.10f*(o.variation-.5f)*t*t));
            var color=Color.Lerp(new Color(.16f,.43f,.17f),new Color(.32f,.57f,.21f),o.variation);
            int rows=Steps(10,18,28),start=vertices.Count;
            var rails=Rails();
            Vertex(root,.5f,0,color,5,o.variation);
            for(int r=1;r<rows;r++)
            {
                float t=r/(float)rows;
                var axis=(Spine(t+.001f)-Spine(t-.001f)).normalized;
                var across=Quaternion.AngleAxis((o.variation-.5f)*38*t,axis)*right;
                var face=Vector3.Cross(across,axis).normalized;
                float w=.025f*o.size*scale*Mathf.Sqrt(growth)*(.38f+.62f*Mathf.Sin(Mathf.PI*Mathf.Pow(t,.75f)))*(1-.53f*t);
                Section(rails,Spine(t),across,face,Vector3.zero,w,w*.33f,w*.055f,w*.12f,0,t,color,5,o.variation);
            }
            Vertex(Spine(1),.5f,1,color,5,o.variation);
            BladeFaces(start,rows,rails.Length);
        }

        void Rubber(PlantGrowth plant,PlantOrgan o,float elapsed)
        {
            float extension=PlantOrgan.StemExtension(elapsed), growth=Mature(elapsed,4.5f);
            var tip=o.origin+o.direction*(o.length*extension);
            var bark=Color.Lerp(new Color(.36f,.29f,.21f),new Color(.47f,.48f,.29f),Mathf.Clamp01(o.depth/12f));
            float radius=Mathf.Lerp(.0125f,.0055f,Mathf.Clamp01(o.depth/14f))*Mathf.Sqrt(o.size);
            Tube(o.origin,tip,radius,radius*.94f,bark);
            Tube(tip-o.direction*.003f,tip,radius*1.08f,radius*1.08f,new Color(.57f,.42f,.32f));
            Vector3 socket=tip+(o.outward*.065f+Vector3.up*.029f)*growth;
            Tube(tip,socket,.0042f,.0028f,new Color(.52f,.30f,.24f));
            if(o.id>plant.OrganCount-plant.Recipe.crowns)
                Tube(tip,tip+(Vector3.up*.086f+o.outward*.014f)*extension,.006f,.0006f,new Color(.63f,.22f,.20f));
            float length=(.29f+.075f*o.variation)*o.size*growth;
            var axis=(o.outward*(.91f*plant.Recipe.spread)+Vector3.up*(.38f+.85f*(1-growth)+.30f*o.variation)).normalized;
            float roll=(o.id%2==0 ? 1 : -1)*Mathf.Lerp(20,48,o.variation);
            var side=Quaternion.AngleAxis(roll,axis)*Vector3.Cross(axis,Vector3.up).normalized;
            var face=Vector3.Cross(side,axis).normalized;
            Vector3 Spine(float t) => socket+length*(axis*t-Vector3.up*(.51f*t*t)+face*(.085f*Mathf.Sin(Mathf.PI*t)));
            var color=Color.Lerp(new Color(.105f,.245f,.155f),new Color(.20f,.37f,.22f),o.variation);
            float pattern=plant.Recipe.variegated?7:6;
            var rails=Rails();int start=vertices.Count,rows=Steps(12,20,32);
            Vertex(socket,.5f,0,color,pattern,o.variation);
            for(int r=1;r<rows;r++)
            {
                float t=r/(float)rows;
                float w=length*.31f*Mathf.Pow(Mathf.Sin(Mathf.PI*Mathf.Pow(t,.94f)),.79f);
                Section(rails,Spine(t),side,face,Vector3.zero,w,-.115f*w,.095f*w,.068f*w,
                    (o.variation-.5f)*.10f,t,color,pattern,o.variation);
            }
            Vertex(Spine(1),.5f,1,color,pattern,o.variation);
            BladeFaces(start,rows,rails.Length);
        }
    }
}
