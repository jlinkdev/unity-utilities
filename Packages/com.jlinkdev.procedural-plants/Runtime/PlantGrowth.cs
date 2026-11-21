using System;
using System.Collections.Generic;
using UnityEngine;

namespace JLink.Plants
{
    public enum PlantSpecies { Pothos = 0, SnakePlant = 1, Fern = 2, Monstera = 3, SpiderPlant = 4, RubberPlant = 5, JadePlant = 6 }

    [Serializable]
    public struct PlantRecipe
    {
        public PlantSpecies species;
        public int seed;
        [Tooltip("Cream and pink margins on rubber plant foliage.")] public bool variegated;
        [Range(1, 7)] public int crowns;
        [Range(.5f, 1.6f)] public float spread;
        [Range(.55f, 1.6f)] public float leafSize;

        public static PlantRecipe Default => new PlantRecipe
        {
            species = PlantSpecies.Pothos, seed = 37, crowns = 4, spread = 1f, leafSize = 1f
        };

        internal bool IsValid => Enum.IsDefined(typeof(PlantSpecies), species) && crowns >= 1 && crowns <= 7 &&
            spread >= .5f && spread <= 1.6f && leafSize >= .55f && leafSize <= 1.6f;
    }

    [Serializable]
    public struct PlantConditions
    {
        [Tooltip("Direction toward light in the plant's local space.")] public Vector3 lightDirection;
        [Range(0, 1)] public float lightResponse;
        [Range(0, 1)] public float moisture;

        public static PlantConditions Default => new PlantConditions
        {
            lightDirection = new Vector3(-.7f, .55f, -.4f), lightResponse = .45f, moisture = .75f
        };
    }

    // One record is a vine internode with its leaf, a basal blade, or an entire frond.
    [Serializable]
    internal struct PlantOrgan
    {
        public int id;
        public bool plantletRemoved;
        public PlantContact contact;
        public int parent;
        public int depth;
        public Vector3 origin;
        public Vector3 direction;
        public Vector3 outward;
        public float length;
        public float size;
        public float variation;
        public float birth;
        // Zero on v1.0 records, whose saved internodes are straight.
        public int shootId;
        public Vector3 startTangent;
        public Vector3 endTangent;

        internal static float StemExtension(float elapsed) => Mathf.SmoothStep(.06f, 1, Mathf.Clamp01(elapsed / .95f));

        public Vector3 Quadratic => 3*direction-2*startTangent-endTangent;
        public Vector3 Cubic => startTangent+endTangent-2*direction;
        public Vector3 StemPosition(float t) => shootId==0 ? origin+direction*(length*t) :
            origin+length*t*(startTangent+t*(Quadratic+t*Cubic));
    }

    [Serializable]
    public sealed partial class PlantGrowth
    {
        public const int MaxOrgans = 160;
        [SerializeField] int version;
        [SerializeField] PlantRecipe recipe;
        [SerializeField] double age;
        [SerializeField] List<PlantOrgan> organs = new List<PlantOrgan>();
        [SerializeField] int lastId, budId;
        [SerializeField] List<int> waitingBuds = new List<int>();
        [SerializeField] bool rootedCutting;
        [SerializeField] bool fromPlantlet;

        public PlantRecipe Recipe => recipe;
        public double Age => age;
        public int OrganCount => organs.Count;
        public bool IsRootedCutting => rootedCutting;
        // Runtime mesh invalidation, including direct mutations through ProceduralPlant.Growth.
        internal uint Revision { get; private set; }
        public PlantSpeciesProfile Profile => PlantSpeciesProfile.For(recipe.species);
        public int Capacity => Profile.Capacity(recipe.crowns);
        double MatureAge => organs[organs.Count - 1].birth + Profile.MaturationUnits;
        public bool FullyGrown => organs.Count == Capacity && age >= MatureAge;
        internal bool Initialized => version == 1 || version == 2;
        internal IReadOnlyList<PlantOrgan> Organs => organs;

        public static PlantGrowth Sow(PlantRecipe recipe, PlantConditions conditions, PlantSupports supports = default)
        {
            if (!recipe.IsValid) throw new ArgumentOutOfRangeException(nameof(recipe));
            ValidateConditions(conditions);
            var plant = new PlantGrowth { version = 2, recipe = recipe };
            plant.AddOrgan(conditions,supports,0);
            return plant;
        }

        // Time is supplied by the caller. Births occur at fixed biological ages.
        public void Advance(double growthUnits, PlantConditions conditions, PlantSupports supports = default)
        {
            if (double.IsNaN(growthUnits) || double.IsInfinity(growthUnits) || growthUnits < 0)
                throw new ArgumentOutOfRangeException(nameof(growthUnits));
            ValidateConditions(conditions);
            Upgrade();
            double target = age + growthUnits * Mathf.Lerp(.2f, 1.25f, conditions.moisture);
            double birth = Math.Floor(age) + 1;
            while (organs.Count < Capacity && birth <= target) AddOrgan(conditions,supports,(float)birth++);
            double nextAge = organs.Count == Capacity ? Math.Min(target,MatureAge) : target;
            if (nextAge != age) Revision++;
            age = nextAge;
        }

        static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);

        internal static void ValidateConditions(PlantConditions conditions)
        {
            if (!float.IsFinite(conditions.moisture) || !float.IsFinite(conditions.lightResponse) ||
                !Finite(conditions.lightDirection)) throw new ArgumentException("Invalid growth conditions.");
        }

        // Counter-based variation keeps reconstruction independent of Unity's global random state.
        float Noise(int index, int channel) => Sample(recipe.seed,index,channel);

        static float Sample(int seed,int index,int channel)
        {
            unchecked
            {
                uint h = (uint)seed ^ ((uint)index * 0x9e3779b9u) ^ ((uint)(channel + 1) * 0x85ebca6bu);
                h ^= h >> 16; h *= 0x7feb352du; h ^= h >> 15; h *= 0x846ca68bu; h ^= h >> 16;
                return (h >> 8) / 16777216f;
            }
        }

        int NextVine(int i)
        {
            if (i < recipe.crowns) return i;
            // Independent bud opportunities and shoot vigour give each vine its own length.
            int chosen = 0;
            float best = -1;
            int leader = (int)(Noise(0, 32) * recipe.crowns);
            for (int shoot = 0; shoot < recipe.crowns; shoot++)
            {
                float vigour = Mathf.Lerp(.55f, 1.4f, Noise(shoot, 14));
                if (shoot == leader) vigour *= 1.7f;
                float score = Mathf.Pow(Noise(i, 20 + shoot), 1f / vigour);
                if (score > best) { best = score; chosen = shoot; }
            }
            return chosen;
        }

        int ShootOf(PlantOrgan organ) => organ.shootId == 0 ? (organ.id-1) % recipe.crowns : organ.shootId-1;

        int LastNode(int shoot)
        {
            for (int j = organs.Count - 1; j >= 0; j--)
            {
                if (ShootOf(organs[j]) == shoot) return j;
            }
            return -1;
        }

        /// <summary>Predict the next birth and its support contact without advancing the clock.</summary>
        public bool TryPreviewNext(PlantConditions conditions, PlantSupports supports, out PlantNode next)
        {
            Upgrade();
            next=default;
            if (OrganCount>=Capacity) return false;
            ValidateConditions(conditions);
            var organ=DesignOrgan(conditions,supports,(float)(Math.Floor(age)+1));
            next=new PlantNode(organ,organ.parent<0 ? 0 : organs[organ.parent].id);
            return true;
        }

        /// <summary>Forecast the next extension on one shoot under unchanged conditions.</summary>
        public bool TryPreviewVine(int shootId, PlantConditions conditions, PlantSupports supports, out PlantNode next)
        {
            if (!TryPreviewNext(conditions,supports,out next)) return false;
            if (shootId==0 || next.ShootId==shootId) return true;
            // Forecast on independent lists. Organ records are value types.
            var forecast=(PlantGrowth)MemberwiseClone();
            forecast.organs=new List<PlantOrgan>(organs);
            forecast.waitingBuds=new List<int>(waitingBuds);
            while (forecast.OrganCount<forecast.Capacity)
            {
                float birth=(float)(Math.Floor(forecast.age)+1);
                forecast.AddOrgan(conditions,supports,birth);
                forecast.age=birth;
                next=forecast.NodeAt(forecast.OrganCount-1);
                if (next.ShootId==shootId) return true;
            }
            next=default;
            return false;
        }

        PlantOrgan DesignOrgan(PlantConditions conditions, PlantSupports supports, float birth)
        {
            if(recipe.species==PlantSpecies.SpiderPlant) return SpiderOrgan(recipe,lastId,birth,conditions);
            if(recipe.species==PlantSpecies.RubberPlant) return RubberOrgan(birth,conditions);
            if(recipe.species==PlantSpecies.JadePlant) return JadeOrgan(birth,conditions);
            int i = lastId;
            bool monstera = recipe.species == PlantSpecies.Monstera;
            bool vine = Profile.IsVine;
            int lane = vine ? NextVine(i) : i % recipe.crowns;
            int bud = budId == 0 ? -1 : IndexOf(budId);
            if (rootedCutting) lane = ShootOf(organs[0]);
            if (bud >= 0) lane = ShootOf(organs[bud]);
            float phase = Noise(0, 8) * 2 * Mathf.PI;
            float angle = phase + lane * (2 * Mathf.PI / recipe.crowns) + (Noise(i, 1) - .5f) * .48f;
            if (recipe.species != PlantSpecies.Pothos) angle += (i / recipe.crowns) * 2.39996f;
            var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            var light = conditions.lightDirection.normalized * Mathf.Clamp01(conditions.lightResponse);
            int parent = -1;
            int depth = 0;
            Vector3 origin = radial * (.025f + .032f * Noise(i, 3)) * recipe.spread;
            float variation = Noise(i, 2);
            float size = recipe.leafSize * Mathf.Lerp(.82f, 1.14f, variation);
            Vector3 direction;
            Vector3 startTangent = Vector3.zero, endTangent = Vector3.zero;
            float length;
            if (vine)
            {
                if (bud >= 0) parent = bud;
                else if (i >= recipe.crowns || rootedCutting) parent = LastNode(lane);
                if (parent >= 0)
                {
                    int prior = organs[parent].parent;
                    if (bud < 0 && !monstera && organs[parent].depth >= 4 && prior >= 0 && organs[prior].parent >= 0 && Noise(i, 11) < .045f)
                        parent = organs[prior].parent;
                    var older = organs[parent];
                    origin = older.origin + older.direction * older.length;
                    depth = older.depth + 1;
                }
                float phaseOffset = Noise(lane, 12) * Mathf.PI * 2;
                float arch = Mathf.Lerp(3.1f, 5.6f, Noise(lane, 13));
                float drape = Mathf.SmoothStep(0, 1, (depth + .5f) / arch);
                float sway = Mathf.Sin(depth * .43f + phaseOffset);
                var bearing = Quaternion.AngleAxis(22 * sway, Vector3.up) * radial;
                var lateral = Vector3.Cross(Vector3.up, bearing);
                var growingTip = bearing * recipe.spread * Mathf.Lerp(1.15f, .09f, drape) +
                    Vector3.up * Mathf.Lerp(1.05f, -1.65f, drape) + lateral * (.58f*sway) +
                    light * Mathf.Lerp(.85f, .46f, drape);
                if (monstera) growingTip = Vector3.up*1.45f + radial*.45f + light*.55f;
                if (bud >= 0) growingTip += lateral * (variation > .5f ? 1f : -1f);
                startTangent = parent < 0 ? (radial * .45f + Vector3.up).normalized :
                    (organs[parent].shootId == 0 ? organs[parent].direction : organs[parent].endTangent);
                endTangent = Vector3.Slerp(startTangent, growingTip.normalized, .78f).normalized;
                direction = (startTangent + endTangent).normalized;
                length = Mathf.Lerp(.066f, .128f, Noise(i, 4)) * Mathf.Lerp(.87f, 1.19f, Noise(lane, 14));
                float turn = (depth % 2 == 0 ? 1 : -1) * Mathf.Lerp(27, 102, Noise(i, 5)) + 15*sway;
                float twist = depth * Mathf.Lerp(-17, 17, Noise(lane, 33)) + 19 * Mathf.Sin(.37f * depth + phaseOffset);
                radial = Quaternion.AngleAxis(turn + twist, Vector3.up) * radial;
            }
            else
            {
                bool fern = recipe.species == PlantSpecies.Fern;
                // Later leaves fill the crown while older leaves retain their original inclinations.
                float lean = Mathf.Lerp(fern ? .62f : .12f, fern ? 1.6f : .7f, variation) * recipe.spread;
                direction = (Vector3.up + radial * lean + light * .55f).normalized;
                length = (fern ? .61f : .55f) * Mathf.Lerp(.68f, 1.38f, Noise(i, 4)) * size;
            }
            var node = new PlantOrgan { id = lastId+1, parent = parent, depth = depth, birth = birth, origin = origin,
                direction = direction, outward = radial, length = length, size = size, variation = variation,
                shootId = vine ? lane + 1 : 0, startTangent = startTangent, endTangent = endTangent };
            if (vine) supports.Guide(ref node,parent < 0 ? default : organs[parent]);
            return node;
        }

        void AddOrgan(PlantConditions conditions, PlantSupports supports, float birth)
        {
            var node=DesignOrgan(conditions,supports,birth);
            organs.Add(node);
            lastId=node.id;
            budId = waitingBuds.Count > 0 ? waitingBuds[0] : 0;
            if (budId != 0) waitingBuds.RemoveAt(0);
        }
    }
}
