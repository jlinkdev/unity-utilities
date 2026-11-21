using System;
using UnityEngine;

namespace JLink.Plants
{
    public sealed partial class PlantGrowth
    {
        internal const int BabyLeaves=6;
        internal static bool IsRunner(PlantOrgan o) => o.id>=13 && (o.id-13)%7==0;
        internal const float PlantletScale=.34f;
        internal float SpiderScale => fromPlantlet ? Mathf.Lerp(PlantletScale,1f,Mathf.SmoothStep(0,1,Mathf.Clamp01(((float)age-BabyLeaves)/13))) : 1;

        static PlantOrgan SpiderOrgan(PlantRecipe design,int index,float birth,PlantConditions conditions)
        {
            float jitter=Sample(design.seed,index,2);
            float angle=Sample(design.seed,0,3)*360+index*137.508f+(jitter-.5f)*26;
            var radial=Quaternion.Euler(0,angle,0)*Vector3.forward;
            var tendency=Vector3.ProjectOnPlane(conditions.lightDirection,Vector3.up).normalized;
            radial=(radial+.15f*Mathf.Clamp01(conditions.lightResponse)*tendency).normalized;
            return new PlantOrgan {id=index+1,parent=-1,birth=birth,origin=radial*.009f,
                direction=(radial+Vector3.up).normalized,outward=radial,
                length=Mathf.Lerp(.33f,.57f,Sample(design.seed,index,1))*design.leafSize,
                size=design.leafSize,variation=jitter};
        }

        PlantOrgan RubberOrgan(float birth,PlantConditions conditions)
        {
            int i=lastId;
            int lane=i%recipe.crowns;
            int previous=i-recipe.crowns;
            float phase=Noise(0,1)*360;
            var crown=Quaternion.Euler(0,phase+360f*lane/recipe.crowns,0)*Vector3.right;
            var leaf=Quaternion.Euler(0,phase+i*137.508f+(Noise(i,2)-.5f)*20,0)*Vector3.right;
            var direction=(Vector3.up+crown*(recipe.crowns>1?.16f:.035f)+conditions.lightDirection.normalized*(.18f*conditions.lightResponse)).normalized;
            var origin=previous<0 ? crown*(recipe.crowns>1?.032f:0) : organs[previous].origin+organs[previous].direction*organs[previous].length;
            return new PlantOrgan {id=i+1,parent=previous<0?-1:previous,depth=i/recipe.crowns,birth=birth,
                origin=origin,direction=direction,outward=leaf,length=Mathf.Lerp(.047f,.073f,Noise(i,3)),
                size=recipe.leafSize*Mathf.Lerp(.82f,1.16f,Noise(i,4)),variation=Noise(i,5)};
        }

        internal static Vector3 RunnerPoint(PlantOrgan o,float t,float spread)
        {
            var side=Vector3.Cross(Vector3.up,o.outward);
            return o.origin+o.length*(o.outward*((.92f+.18f*o.variation)*spread*t)
                +side*(.12f*Mathf.Sin(t*Mathf.PI)*t)+Vector3.up*(1.03f*t-1.28f*t*t));
        }

        internal static PlantRecipe DaughterRecipe(PlantRecipe parent,int runner)
        {
            parent.seed=unchecked(parent.seed*486187739+runner*16777619);
            return parent;
        }

        public bool CanDetachPlantlet(int runnerId)
        {
            int i=IndexOf(runnerId);
            if(recipe.species!=PlantSpecies.SpiderPlant || i<0) return false;
            var o=organs[i];
            return IsRunner(o) && !o.plantletRemoved && age-o.birth>=3+BabyLeaves;
        }

        public Vector3 PlantletPosition(int runnerId)
        {
            if(!CanDetachPlantlet(runnerId)) throw new ArgumentException("Choose a mature attached plantlet.",nameof(runnerId));
            return RunnerPoint(organs[IndexOf(runnerId)],1,recipe.spread)*SpiderScale;
        }

        public PlantGrowth DetachPlantlet(int runnerId)
        {
            if(!CanDetachPlantlet(runnerId)) throw new ArgumentException("Choose a mature attached plantlet.",nameof(runnerId));
            var daughter=new PlantGrowth {version=2,recipe=DaughterRecipe(recipe,runnerId),fromPlantlet=true,age=BabyLeaves};
            for(int i=0;i<BabyLeaves;i++) daughter.AddOrgan(PlantConditions.Default,default,i);
            var index=IndexOf(runnerId);
            var runner=organs[index];runner.plantletRemoved=true;organs[index]=runner;
            Revision++;
            return daughter;
        }

        internal static PlantOrgan BabyLeaf(PlantRecipe recipe,int i) => SpiderOrgan(recipe,i,i,PlantConditions.Default);
    }
}
