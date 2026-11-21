using System;
using UnityEngine;

namespace JLink.Plants
{
    /// <summary>A repeatable planting pose relative to the soil origin, in degrees and metres.</summary>
    [Serializable]
    public struct CuttingPlacement
    {
        public bool trainToPole;
        public bool pole;
        public Vector2 poleXZ;
        public float yaw, tilt;

        public static CuttingPlacement Trailing => new CuttingPlacement { poleXZ=PlantSupports.Windowsill.poleXZ };
        public static CuttingPlacement Climbing => new CuttingPlacement {
            pole=true, trainToPole=true, poleXZ=PlantSupports.Windowsill.poleXZ
        };

        public PlantSupports SupportsFor(PlantSupports pot)
        {
            if(!float.IsFinite(poleXZ.x) || !float.IsFinite(poleXZ.y) ||
                !float.IsFinite(yaw) || !float.IsFinite(tilt))
                throw new ArgumentException("Planting angles and pole position must be finite.");
            pot.pole=pole;
            pot.poleXZ=Vector2.ClampMagnitude(poleXZ,Mathf.Max(0,pot.rimRadius-pot.poleRadius-.015f));
            return pot;
        }

        /// <summary>Build an independent posed plant from the original cutting on every call.</summary>
        public PlantGrowth Pose(PlantSpecimen cutting, PlantSupports pot)
        {
            if (cutting==null) throw new ArgumentNullException(nameof(cutting));
            var result=cutting.OpenGrowth();
            var supports=SupportsFor(pot);
            var align=Quaternion.identity;
            if (trainToPole && pole && result.TryPreviewNext(cutting.conditions,supports,out var next))
            {
                Vector3 bud=next.Base;
                if (bud.sqrMagnitude>.000001f)
                {
                    var side=new Vector2(bud.x,bud.z)-supports.poleXZ;
                    if (side.sqrMagnitude<.000001f) side=Vector2.down;
                    var target=supports.poleXZ+side.normalized*(supports.poleRadius+.025f);
                    target=Vector2.ClampMagnitude(target,bud.magnitude*.9f);
                    var aim=new Vector3(target.x,Mathf.Sqrt(Mathf.Max(0,bud.sqrMagnitude-target.sqrMagnitude)),target.y);
                    align=Quaternion.FromToRotation(bud,aim);
                }
            }
            result.RotateCutting(Quaternion.Euler(0,yaw,0)*Quaternion.Euler(tilt,0,0)*align);
            return result;
        }
    }
}
