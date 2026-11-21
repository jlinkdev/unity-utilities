using System;
using System.Collections.Generic;
using UnityEngine;

namespace JLink.Plants
{
    public sealed partial class PlantGrowth
    {
        int IndexOf(int id)
        {
            // Node IDs stay ordered, including after a subtree is removed.
            int low = 0, high = organs.Count - 1;
            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                int candidate = organs[mid].id;
                if (candidate == id) return mid;
                if (candidate < id) low = mid + 1;
                else high = mid - 1;
            }
            return -1;
        }

        /// <summary>Current visible endpoint of a vine internode, in local metres.</summary>
        public Vector3 VisibleVineTip(int id)
        {
            int index=IndexOf(id);
            if (index<0 || !Profile.IsVine)
                throw new ArgumentException("Choose an existing vine node.",nameof(id));
            var node=organs[index];
            return node.StemPosition(PlantOrgan.StemExtension((float)(age-node.birth)));
        }

        public PlantNode NodeAt(int index)
        {
            Upgrade();
            var o = organs[index];
            return new PlantNode(o,o.parent < 0 ? 0 : organs[o.parent].id);
        }

        public bool CanCut(int id)
        {
            int index=IndexOf(id);
            if (index<0 || organs[index].parent<0) return false;
            if (recipe.species==PlantSpecies.Monstera)
                return organs[index].depth>=2 && age-organs[index].birth>=5;
            return recipe.species==PlantSpecies.Pothos;
        }

        /// <summary>IDs in the detached subtree, in parent-before-child order.</summary>
        public int[] PreviewCut(int id)
        {
            if (!CanCut(id)) return Array.Empty<int>();
            var selected = new HashSet<int>();
            var result = new List<int>();
            for (int i=0;i<organs.Count;i++)
            {
                var o=organs[i];
                if (o.id != id && !selected.Contains(o.parent)) continue;
                selected.Add(i);
                result.Add(o.id);
            }
            return result.ToArray();
        }

        public PlantGrowth Cut(int id, bool plantUpright=false)
        {
            var detached = new HashSet<int>(PreviewCut(id));
            if (detached.Count==0) throw new ArgumentException("Choose a non-basal pothos node or a mature Monstera node with an aerial root.",nameof(id));
            var baseNode=organs[IndexOf(id)];
            var retained=new List<PlantOrgan>(organs.Count-detached.Count);
            var cutting=new PlantGrowth { version=2, recipe=recipe, age=age, lastId=lastId, rootedCutting=true };
            var map=new int[organs.Count];
            for (int i=0;i<organs.Count;i++)
            {
                var o=organs[i];
                bool removed=detached.Contains(o.id);
                var destination=removed ? cutting.organs : retained;
                if (removed)
                {
                    o.origin -= baseNode.origin;
                    o.contact=PlantContact.Free;
                }
                o.parent=o.parent<0 || o.id==id ? -1 : map[o.parent];
                map[i]=destination.Count;
                destination.Add(o);
            }
            // Preserve still-dormant buds on whichever specimen now owns their node.
            var pending=new List<int>(waitingBuds);
            if (budId!=0) pending.Insert(0,budId);
            waitingBuds.Clear();
            budId=0;
            foreach (int previous in pending)
                (detached.Contains(previous) ? cutting : this).ScheduleBud(previous);
            if (recipe.species==PlantSpecies.Monstera) cutting.ScheduleBud(baseNode.id);
            ScheduleBud(organs[baseNode.parent].id);
            organs=retained;
            Revision++;
            if (plantUpright) cutting.OrientForPlanting();
            return cutting;
        }

        void OrientForPlanting()
        {
            // A hanging cutting enters its new pot base-first. Rotate its stem as
            // one piece, balancing the first segment with the rest of the branch.
            Vector3 centre=Vector3.zero;
            foreach (var node in organs) centre+=node.origin+node.length*node.direction;
            Vector3 axis=organs[0].direction+centre.normalized;
            if (axis.sqrMagnitude<.0001f) axis=organs[0].direction;
            RotateCutting(Quaternion.FromToRotation(axis.normalized,Vector3.up));
        }

        internal void RotateCutting(Quaternion turn)
        {
            if (!rootedCutting) throw new InvalidOperationException("Cut a branch before placing it.");
            if (turn.Equals(Quaternion.identity)) return;
            Revision++;
            for (int i=0;i<organs.Count;i++)
            {
                var n=organs[i];
                n.origin=turn*n.origin;
                n.direction=turn*n.direction;
                n.startTangent=turn*n.startTangent;
                n.endTangent=turn*n.endTangent;
                // The mesher uses outward as a horizontal azimuth for each blade.
                var bearing=Vector3.ProjectOnPlane(turn*n.outward,Vector3.up);
                if (bearing.sqrMagnitude>.00001f) n.outward=bearing.normalized;
                organs[i]=n;
            }
        }

        void ScheduleBud(int id)
        {
            if (budId==0) budId=id;
            else if (budId!=id && !waitingBuds.Contains(id)) waitingBuds.Add(id);
        }
    }
}
