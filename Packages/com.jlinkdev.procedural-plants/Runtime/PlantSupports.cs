using System;
using UnityEngine;

namespace JLink.Plants
{
    public enum PlantContact { Free, Rim, Shelf, Pole }

    /// <summary>Analytic supports in plant-local metres. XZ coordinates use Vector2(x,z).</summary>
    [Serializable]
    public struct PlantSupports
    {
        public bool rim;
        [Min(.01f)] public float rimRadius;
        public float rimHeight;
        public bool shelf;
        public Rect shelfXZ;
        public float shelfHeight;
        public bool pole;
        public Vector2 poleXZ;
        [Min(.005f)] public float poleRadius;
        [Min(.01f)] public float poleHeight;

        public static PlantSupports Windowsill => new PlantSupports {
            rim = true, rimRadius = .153f, rimHeight = .014f,
            shelf = true, shelfHeight = -.229f, shelfXZ = Rect.MinMaxRect(-1,-.34f,1,.44f),
            poleXZ = new Vector2(.045f,.04f), poleRadius = .032f, poleHeight = .92f
        };

        internal void Guide(ref PlantOrgan organ, PlantOrgan parent)
        {
            const float clearance = .006f;
            var start = organ.origin;
            var end = start + organ.direction * organ.length;
            var a = new Vector2(start.x,start.z);
            var b = new Vector2(end.x,end.z);
            var travel = b - a;
            var previous = parent.contact;
            if (previous == PlantContact.Shelf) travel = new Vector2(parent.direction.x,parent.direction.z);
            if (travel.sqrMagnitude < .00001f) travel = new Vector2(organ.outward.x,organ.outward.z);
            travel.Normalize();

            if (pole && poleRadius > 0 && start.y < poleHeight && start.y > -.03f &&
                Vector2.Distance(a,poleXZ) < poleRadius + (previous == PlantContact.Pole ? .06f : .095f))
            {
                var normal = (a-poleXZ).normalized;
                if (normal.sqrMagnitude < .1f) normal = travel;
                var target = poleXZ + normal * (poleRadius + clearance);
                var step = Vector2.ClampMagnitude(target-a, organ.length * .64f);
                float rise=Mathf.Sqrt(Mathf.Max(0,organ.length*organ.length-step.sqrMagnitude));
                end = new Vector3(a.x+step.x,Mathf.Min(poleHeight,start.y+rise),a.y+step.y);
                Apply(ref organ,end,PlantContact.Pole);
                return;
            }
            if (rim && rimRadius > 0 && a.magnitude < rimRadius + clearance && end.y < rimHeight+clearance)
            {
                float rise=Mathf.Clamp(rimHeight+clearance-start.y,0,organ.length);
                b = a + travel * Mathf.Sqrt(Mathf.Max(0,organ.length*organ.length-rise*rise));
                end = new Vector3(b.x,start.y+rise,b.y);
                Apply(ref organ,end,PlantContact.Rim);
                return;
            }
            float top = shelfHeight+clearance;
            if (!shelf || end.y > top || start.y < shelfHeight-.001f) return;
            float crossing = Mathf.Clamp01((top-start.y) / Mathf.Min(-.000001f,end.y-start.y));
            if (!shelfXZ.Contains(Vector2.Lerp(a,b,crossing)) && !(previous == PlantContact.Shelf && shelfXZ.Contains(a))) return;
            // Complete this internode along the surface. The first tip beyond the edge is free to descend.
            b = a + travel * Mathf.Max((b-a).magnitude,organ.length - Mathf.Max(0,start.y-top));
            end = new Vector3(b.x,top,b.y);
            Apply(ref organ,end,PlantContact.Shelf);
        }

        static void Apply(ref PlantOrgan o, Vector3 end, PlantContact contact)
        {
            Vector3 delta = end-o.origin;
            if (delta.sqrMagnitude < .000001f) return;
            o.length = delta.magnitude;
            o.direction = delta/o.length;
            // Keep the entire contacting segment above the analytic surface.
            o.startTangent = o.direction;
            o.endTangent = o.direction;
            o.contact = contact;
        }
    }

    public readonly struct PlantNode
    {
        public readonly int Id, ParentId, Depth, ShootId;
        public readonly Vector3 Base, Tip;
        public readonly PlantContact Contact;
        internal PlantNode(PlantOrgan o, int parentId)
        {
            Id=o.id; ParentId=parentId; Depth=o.depth; ShootId=o.shootId; Base=o.origin;
            Tip=o.origin+o.direction*o.length; Contact=o.contact;
        }
    }
}
