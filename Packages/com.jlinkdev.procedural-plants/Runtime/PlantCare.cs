using System;
using UnityEngine;

namespace JLink.Plants
{
    /// <summary>Optional per-pot simulation, advanced only by the caller's growth clock.</summary>
    [Serializable]
    public struct PlantCare
    {
        [SerializeField] bool enabled, initialized;
        [SerializeField] double soil;
        public bool Enabled => enabled;
        public float Soil => (float)soil;
        internal bool IsValid => double.IsFinite(soil) && soil >= 0 && soil <= 1 && (!enabled || initialized);

        public void SetEnabled(bool value,float initialMoisture)
        {
            if(value && !initialized) { soil=Mathf.Clamp01(initialMoisture); initialized=true; }
            enabled=value;
        }

        public void Water(float amount)
        {
            if(!float.IsFinite(amount) || amount<0) throw new ArgumentOutOfRangeException(nameof(amount));
            if(enabled) soil=Math.Min(1,soil+amount);
        }

        public float Stress(PlantSpecies species) => enabled ? 1-Mathf.Clamp01((float)(soil/PlantSpeciesProfile.For(species).StressThreshold)) : 0;
        internal float Droop(PlantSpecies species) => Stress(species)*PlantSpeciesProfile.For(species).MaximumDroop;

        internal float Dry(double units,PlantSpecies species)
        {
            var profile=PlantSpeciesProfile.For(species);
            double before=soil, after=Math.Max(0,before-units*profile.DryingRate);
            // Integrate the moisture response over the wet, stressed and dry portions of the step.
            double Area(double s) => s>=profile.StressThreshold ? s-profile.StressThreshold*.5 : s*s/(2*profile.StressThreshold);
            soil=after;
            if(units==0 || before==after) return Mathf.Clamp01((float)(soil/profile.StressThreshold));
            return Mathf.Clamp01((float)((Area(before)-Area(after))/(units*profile.DryingRate)));
        }
    }
}
