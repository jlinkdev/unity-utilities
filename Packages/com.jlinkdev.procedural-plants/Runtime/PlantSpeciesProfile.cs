using System;
using UnityEngine;

namespace JLink.Plants
{
    /// <summary>Shared species traits. Enum values are persisted in recipes and scenes.</summary>
    public sealed class PlantSpeciesProfile
    {
        public string DisplayName { get; }
        public bool IsVine { get; }
        internal int MaturationUnits { get; }
        internal double DryingRate { get; }
        internal double StressThreshold { get; }
        internal float MaximumDroop { get; }
        readonly int baseOrgans, organsPerCrown;

        PlantSpeciesProfile(string name, int baseOrgans, int organsPerCrown, double dryingRate,
            double stressThreshold, float maximumDroop, bool vine = false, int maturationUnits = 6)
        {
            DisplayName = name;
            IsVine = vine;
            this.baseOrgans = baseOrgans;
            this.organsPerCrown = organsPerCrown;
            MaturationUnits = maturationUnits;
            DryingRate = dryingRate;
            StressThreshold = stressThreshold;
            MaximumDroop = maximumDroop;
        }

        internal int Capacity(int crowns) => Mathf.Min(PlantGrowth.MaxOrgans, baseOrgans + organsPerCrown * crowns);

        static readonly PlantSpeciesProfile[] profiles = {
            new PlantSpeciesProfile("Pothos",      0, 24, .038, .42, .41f, vine: true),
            new PlantSpeciesProfile("Snake plant", 6,  4, .020, .18, .13f),
            new PlantSpeciesProfile("Fern",        6,  3, .062, .65, .58f),
            new PlantSpeciesProfile("Monstera",    6,  4, .041, .49, .37f, vine: true),
            new PlantSpeciesProfile("Spider plant",14, 4, .047, .48, .48f, maturationUnits: 9),
            new PlantSpeciesProfile("Rubber plant",6,  4, .028, .34, .27f),
            new PlantSpeciesProfile("Jade plant",  0, 35, .017, .22, .16f)
        };

        public static PlantSpeciesProfile For(PlantSpecies species)
        {
            int index = (int)species;
            if ((uint)index >= (uint)profiles.Length) throw new ArgumentOutOfRangeException(nameof(species));
            return profiles[index];
        }
    }
}
