using System;
using UnityEngine;

namespace JLink.Plants
{
    /// <summary>Portable, independently resumable specimen. Project materials stay with the component/prefab.</summary>
    [Serializable]
    public sealed class PlantSpecimen
    {
        public string growth;
        public PlantConditions conditions;
        public PlantCare care;
        public PlantSupports supports;
        public PlantSurface surface;
        public LeafDetail detail;

        public static PlantSpecimen Capture(ProceduralPlant plant) => CaptureState(plant, plant.SaveState());

        /// <summary>Pair a detached or newly seeded growth record with a copy of this plant's environment and care.</summary>
        public static PlantSpecimen Capture(ProceduralPlant source, PlantGrowth growth)
        {
            if (growth == null) throw new ArgumentNullException(nameof(growth));
            return CaptureState(source, growth.ToJson());
        }

        static PlantSpecimen CaptureState(ProceduralPlant plant, string state) => new PlantSpecimen {
            growth=state, conditions=plant.Conditions, supports=plant.Supports, care=plant.Care,
            surface=plant.Surface, detail=plant.Detail
        };

        /// <summary>Open an independent growth record and check the saved environment before applying it.</summary>
        public PlantGrowth OpenGrowth()
        {
            PlantGrowth.ValidateConditions(conditions);
            if (!care.IsValid || !Enum.IsDefined(typeof(LeafDetail),detail) || !Enum.IsDefined(typeof(PlantSurface),surface))
                throw new ArgumentException("Invalid specimen care, mesh detail or surface.");
            return PlantGrowth.FromJson(growth);
        }

        public void Restore(ProceduralPlant plant) => plant.RestoreSpecimen(OpenGrowth(),this,supports);

        /// <summary>Plant an independent copy using the destination pot's support geometry.</summary>
        public void PlantCutting(ProceduralPlant plant,CuttingPlacement pose,PlantSupports pot)
        {
            var placed=pose.Pose(this,pot);
            plant.RestoreSpecimen(placed,this,pose.SupportsFor(pot));
        }
    }
}
