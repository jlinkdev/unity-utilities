using UnityEngine;

namespace JLink.Plants
{
    public sealed partial class PlantGrowth
    {
        PlantOrgan JadeOrgan(float birth, PlantConditions conditions)
        {
            // Each shoot carries five opposite pairs. Its tip continues and a second shoot
            // emerges one node lower. Develop shallower forks before extending later forks.
            const int pairsPerShoot = 5;
            int lane = lastId % recipe.crowns;
            int node = lastId / recipe.crowns;
            int shoot = node / pairsPerShoot;
            int pair = node % pairsPerShoot;
            float phase = Noise(lane, 41) * 360;
            var crown = Quaternion.Euler(0, phase + lane * 137.5f, 0) * Vector3.forward;
            var origin = recipe.crowns == 1 ? Vector3.zero : crown * .035f;
            var direction = (Vector3.up + crown * (recipe.crowns == 1 ? .08f : .26f)).normalized;
            var bearing = crown;
            int parent = -1, depth = 0;
            if (node > 0)
            {
                int parentNode = node - 1;
                if (pair == 0)
                {
                    int parentShoot = (shoot - 1) / 2;
                    bool sideBud = shoot % 2 == 0;
                    parentNode = parentShoot * pairsPerShoot + (sideBud ? pairsPerShoot - 2 : pairsPerShoot - 1);
                }
                parent = parentNode * recipe.crowns + lane;
                var older = organs[parent];
                origin = older.origin + older.direction * older.length;
                depth = older.depth + 1;
                bearing = Quaternion.AngleAxis(90 + Mathf.Lerp(-6, 6, Noise(lastId, 42)), Vector3.up) * older.outward;
                direction = Vector3.Slerp(older.direction, Vector3.up, .06f);
                if (pair == 0)
                {
                    int fork = ((shoot - 1) / 2) * recipe.crowns + lane;
                    var lateral = Quaternion.Euler(0, phase + Noise(fork, 43) * 150, 0) * Vector3.right;
                    float opening = shoot % 2 == 0 ? -1.26f : .88f;
                    direction = (direction + lateral * (opening * recipe.spread)).normalized;
                }
            }
            var light = conditions.lightDirection.normalized * Mathf.Clamp01(conditions.lightResponse);
            direction = (direction + .075f * light).normalized;
            return new PlantOrgan {
                id = lastId + 1, parent = parent, depth = depth, birth = birth,
                origin = origin, direction = direction, outward = bearing,
                length = Mathf.Lerp(.032f, .045f, Noise(lastId, 44)),
                size = recipe.leafSize * Mathf.Lerp(.88f, 1.12f, Noise(lastId, 45)),
                variation = Noise(lastId, 46)
            };
        }
    }
}
