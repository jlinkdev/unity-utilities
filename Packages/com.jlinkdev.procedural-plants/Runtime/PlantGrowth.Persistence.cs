using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace JLink.Plants
{
    public sealed partial class PlantGrowth
    {
        // JsonUtility's numeric double reader can round across a birth boundary.
        // Keep normal numeric state in Unity scenes and round-trip the clock exactly in saves.
        [Serializable]
        sealed class Snapshot
        {
            public int version;
            public PlantRecipe recipe;
            public string age;
            public List<PlantOrgan> organs;
            public int lastId, budId;
            public List<int> waitingBuds;
            public bool rootedCutting;
            public bool fromPlantlet;
        }

        public string ToJson() => JsonUtility.ToJson(new Snapshot {
            version = version, recipe = recipe, age = age.ToString("G17", CultureInfo.InvariantCulture),
            organs = organs, lastId = lastId, budId = budId, waitingBuds = waitingBuds, rootedCutting = rootedCutting, fromPlantlet=fromPlantlet
        });

        public static PlantGrowth FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Empty plant save.");
            var saved = JsonUtility.FromJson<Snapshot>(json);
            if (saved == null || (saved.version != 1 && saved.version != 2) || !saved.recipe.IsValid || saved.organs == null ||
                !double.TryParse(saved.age, NumberStyles.Float, CultureInfo.InvariantCulture, out double age))
                throw new ArgumentException("Unsupported or incomplete plant save.");
            var plant = new PlantGrowth { version = saved.version, recipe = saved.recipe, age = age, organs = saved.organs,
                lastId = saved.lastId, budId = saved.budId, waitingBuds = saved.waitingBuds ?? new List<int>(), rootedCutting = saved.rootedCutting, fromPlantlet=saved.fromPlantlet };
            if (!double.IsFinite(age) || age < 0 || age >= 16777216 || plant.organs.Count < 1 || plant.organs.Count > plant.Capacity ||
                (saved.version == 1 && (age > plant.Capacity+5 || plant.organs.Count != Math.Min(plant.Capacity,1+(int)age))))
                throw new ArgumentException("Unsupported or incomplete plant save.");
            plant.Upgrade();
            int previousId = 0;
            for (int i = 0; i < plant.organs.Count; i++)
            {
                var organ = plant.organs[i];
                if (organ.parent < -1 || organ.parent >= i || organ.depth < 0 || organ.depth > plant.lastId ||
                    !(organ.birth >= 0 && organ.birth <= age) || (saved.version == 1 && organ.birth != i) ||
                    organ.id <= previousId || organ.id > plant.lastId || !Enum.IsDefined(typeof(PlantContact),organ.contact) ||
                    !(organ.length > 0 && organ.length < 3) ||
                    !(organ.size > 0 && organ.size < 3) || !(organ.variation >= 0 && organ.variation <= 1) ||
                    !Finite(organ.origin) || !Finite(organ.direction) || !Finite(organ.outward) ||
                    organ.shootId < 0 || organ.shootId > plant.recipe.crowns ||
                    !Finite(organ.startTangent) || !Finite(organ.endTangent))
                    throw new ArgumentException("Invalid plant structure.");
                previousId = organ.id;
            }
            if (plant.lastId == int.MaxValue || (plant.budId != 0 && plant.IndexOf(plant.budId) < 0))
                throw new ArgumentException("Invalid plant node identity.");
            var seenBuds = new HashSet<int> { plant.budId };
            foreach (var id in plant.waitingBuds)
                if (plant.budId == 0 || !seenBuds.Add(id) || plant.IndexOf(id) < 0)
                    throw new ArgumentException("Invalid pending bud.");
            return plant;
        }

        internal void Upgrade()
        {
            if (version != 1) return;
            for (int i=0;i<organs.Count;i++)
            {
                var organ=organs[i]; organ.id=i+1; organs[i]=organ;
            }
            lastId=organs.Count;
            version=2;
        }
    }
}
