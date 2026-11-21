using System.Collections.Generic;
using UnityEngine;

namespace JLink.Pots
{
    /// <summary>Builds into a caller-owned mesh. Slots are exterior, interior, handles and saucer.</summary>
    public sealed class PotMesh
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly PotUvLayout layout = new PotUvLayout();
        readonly List<float> mappedLevels = new List<float>();
        PotRecipe reference;
        string referenceJson;
        bool measuring;
        (int sides, int handles, BaseStyle foot, bool drainage, bool saucer, float floor, float tile) mappedShape;
        readonly List<float> levels = new List<float>();
        PotRecipe pot;
        int sides;

        public void Build(PotRecipe recipe, Mesh target) => Build(recipe,target,PotUvMode.Tiled);

        public void Build(PotRecipe recipe, Mesh target, PotUvMode mode, bool singleMaterial = false)
        {
            if (recipe == null) throw new System.ArgumentNullException(nameof(recipe));
            if (!target) throw new System.ArgumentNullException(nameof(target));
            recipe.Normalize();
            pot = recipe;
            sides = Mathf.Max(pot.segments, pot.fluteDepth > 0 ? pot.flutes * 6 : 0);
            if (pot.footprint == Footprint.Faceted)
                sides = Mathf.CeilToInt((float)sides / pot.facets) * pot.facets;

            levels.Clear();
            for (int i = 0; i <= 40; i++) levels.Add(i / 40f);
            foreach (var ring in pot.rings) levels.Add(ring.level);
            levels.Add(.035f); levels.Add(.07f); levels.Add(.09f);
            for (int i = 1; i <= 12; i++) levels.Add(1 - .12f * i / 12);
            float insideFloor = pot.floor / pot.height;
            levels.Add(insideFloor);
            levels.Sort();

            if (mode == PotUvMode.Paintable)
            {
                // An exported atlas measures the final sculpt, even when the tiled glaze is stretched.
                referenceJson = null;
                layout.Clear();
                measuring = true;
                Emit(recipe,insideFloor);
                layout.Bake(vertices,recipe.tileSize,mode);
                layout.Write(target,vertices,singleMaterial);
                return;
            }
            recipe.EnsureUvReference();
            var shape = (sides,recipe.handles,recipe.baseStyle,recipe.drainage,recipe.saucer,insideFloor,recipe.tileSize);
            bool newReference = referenceJson != recipe.UvReferenceJson;
            if (newReference || !mappedShape.Equals(shape) || !SameLevels())
            {
                if (newReference)
                {
                    reference = JsonUtility.FromJson<PotRecipe>(recipe.UvReferenceJson);
                    reference.Normalize();
                }
                layout.Clear();
                measuring = true;
                pot = reference;
                Emit(recipe,insideFloor);
                layout.Bake(vertices,recipe.tileSize,mode);
                mappedShape = shape;
                referenceJson = recipe.UvReferenceJson;
                mappedLevels.Clear();
                mappedLevels.AddRange(levels);
            }
            measuring = false;
            pot = recipe;
            Emit(recipe,insideFloor);
            layout.Write(target,vertices,singleMaterial);
        }

        bool SameLevels()
        {
            if (levels.Count != mappedLevels.Count) return false;
            for (int i = 0; i < levels.Count; i++)
                if (levels[i] != mappedLevels[i]) return false;
            return true;
        }

        void Emit(PotRecipe topology,float insideFloor)
        {
            vertices.Clear();
            Ribbon();
            int bottom = Ring(0);
            int previous = bottom;
            float last = 0;
            foreach (float level in levels)
            {
                if (level - last < .00001f) continue;
                int next = Ring(level);
                Strip(previous, next, sides, 0);
                previous = next;
                last = level;
            }

            // A half-round crown joins the two walls. Its ends coincide with the wall rings.
            for (int i = 1; i <= 8; i++)
            {
                float angle = i * Mathf.PI / 8;
                int crown = Ring(1, (1 - Mathf.Cos(angle)) / 2, Mathf.Sin(angle) * .45f);
                Strip(previous, crown, sides, 0);
                previous = crown;
            }

            Ribbon();
            last = 1;
            for (int i = levels.Count - 1; i >= 0; i--)
            {
                float level = levels[i];
                if (level >= last - .00001f || level < insideFloor) continue;
                float start = pot.floor / pot.height;
                float innerLevel = start == insideFloor ? level : Mathf.Lerp(start,1,Mathf.InverseLerp(insideFloor,1,level));
                int next = Ring(innerLevel, 1);
                Strip(previous, next, sides, 1);
                previous = next;
                last = level;
            }

            // Duplicate the rim of each planar face so its flat normal stays independent.
            int floorEdge = Duplicate(previous);
            float bottomY = topology.baseStyle == BaseStyle.FootRing ? Mathf.Min(pot.floor * .45f, pot.height * .035f) : 0;
            int underEdge = Duplicate(bottom);
            if (bottomY > 0)
            {
                int inset = ScaledRing(bottom, .84f, 0);
                Ribbon();
                Strip(underEdge, inset, sides, 0, true);
                underEdge = ScaledRing(bottom, .8f, bottomY);
                Strip(inset, underEdge, sides, 0, true);
            }

            if (topology.drainage)
            {
                float hole = Mathf.Min(pot.width * .034f, MinRadius(floorEdge, underEdge) * .45f);
                int topHole = Circle(hole, pot.floor);
                int underHole = Circle(hole, bottomY);
                if (measuring) layout.FlatBand(floorEdge,topHole,sides,1,true);
                Ribbon();
                Strip(Duplicate(topHole), Duplicate(underHole), sides, 1);
                if (measuring) layout.FlatBand(underHole,underEdge,sides,0,false);
            }
            else
            {
                Cap(floorEdge, sides, true, 1);
                Cap(underEdge, sides, false, 0);
            }

            for (int i = 0; i < topology.handles; i++)
                Handle((pot.handleAngle / 360 + (float)i / topology.handles) * 2 * Mathf.PI);
            if (topology.saucer) Saucer();
        }

        public static Vector3 Surface(PotRecipe p, float level, float angle)
            => Surface(p, level, angle, p.RadiusAt(level));

        static Vector3 Surface(PotRecipe p, float level, float angle, float profile)
        {
            float x = Mathf.Cos(angle), z = Mathf.Sin(angle);
            float radial = profile * p.width * .5f;
            if (p.footprint == Footprint.SoftSquare)
                radial /= Mathf.Pow(Mathf.Pow(x, 6) + Mathf.Pow(z, 6), 1f / 6);
            else if (p.footprint == Footprint.Faceted)
            {
                float half = Mathf.PI / p.facets;
                radial *= Mathf.Cos(half) / Mathf.Cos(Mathf.Repeat(angle, half * 2) - half);
            }
            float rim = Mathf.Clamp01((level - .88f) / .12f);
            if (p.lip == Lip.Rolled) radial *= 1 + p.rimSize * Mathf.Pow(Mathf.Sin(rim * Mathf.PI * .80f), 2);
            if (p.lip == Lip.Flared) radial *= 1 + p.rimSize * rim * rim;
            if (p.baseStyle == BaseStyle.FootRing)
                radial *= 1 + .08f * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.035f, .09f, level)));
            float fade = Mathf.Clamp01(level / .1f) * (1 - rim);
            radial *= 1 - p.fluteDepth * fade * (.5f + .5f * Mathf.Cos(p.flutes * (angle - level * p.twist)));
            if (p.marks != null && p.marks.Count > 0)
            {
                float adjustment = 0;
                foreach (var mark in p.marks) adjustment += mark.depth * mark.Influence(level, angle * Mathf.Rad2Deg);
                radial *= Mathf.Clamp(1 + adjustment, .35f, 1.8f);
            }
            return new Vector3(x * radial, level * p.height, z * radial * p.depthRatio);
        }

        int Ring(float level, float inset = 0, float lift = 0)
        {
            int start = vertices.Count;
            float profile = pot.RadiusAt(level);
            for (int i = 0; i < sides; i++)
            {
                Vector3 v = Surface(pot, level, i * 2 * Mathf.PI / sides, profile);
                var radial = new Vector3(v.x, 0, v.z);
                float thickness = Mathf.Min(pot.wall, radial.magnitude * .45f);
                vertices.Add(v - radial.normalized * (thickness * inset) + Vector3.up * (lift * thickness));
            }
            return start;
        }

        int Duplicate(int start)
        {
            int copy = vertices.Count;
            for (int i = 0; i < sides; i++) vertices.Add(vertices[start + i]);
            return copy;
        }

        void Saucer()
        {
            // Sample the largest low-body footprint so a widening belly clears the saucer lip.
            float rise = Mathf.Min(pot.height*.09f,pot.width*.065f);
            float under = -Mathf.Max(.002f,rise*.32f);
            int reference = vertices.Count;
            for (int i = 0; i < sides; i++)
            {
                float angle = 2*Mathf.PI*i/sides;
                Vector3 outer = Vector3.zero;
                for (int j = 0; j <= 6; j++)
                {
                    var p = Surface(pot,rise/pot.height*j/6,angle);
                    p.y = 0;
                    if (p.sqrMagnitude > outer.sqrMagnitude) outer = p;
                }
                vertices.Add(outer*1.04f + outer.normalized*pot.width*.014f + Vector3.up*under);
            }
            Cap(reference,sides,false,3);
            Ribbon();
            int last = Duplicate(reference);
            void Next(float scale,float y)
            {
                int ring = ScaledRing(reference,scale,y);
                Strip(last,ring,sides,3); last = ring;
            }
            Next(1.05f,under*.8f); Next(1.14f,rise*.82f); Next(1.14f,rise);
            Next(1.10f,rise); Next(1.09f,rise*.82f); Next(1.015f,-.0002f);
            Cap(Duplicate(last),sides,true,3);
        }

        int ScaledRing(int start, float scale, float y)
        {
            int ring = vertices.Count;
            for (int i = 0; i < sides; i++)
            {
                Vector3 v = vertices[start + i];
                vertices.Add(new Vector3(v.x * scale, y, v.z * scale));
            }
            return ring;
        }

        float MinRadius(int a, int b)
        {
            float min = float.MaxValue;
            for (int i = 0; i < sides; i++)
            {
                Vector3 v = vertices[a+i], w = vertices[b+i];
                min = Mathf.Min(min, new Vector2(v.x,v.z).magnitude, new Vector2(w.x,w.z).magnitude);
            }
            return min;
        }

        int Circle(float radius, float y)
        {
            int start = vertices.Count;
            for (int i = 0; i < sides; i++)
            {
                float angle = 2 * Mathf.PI * i / sides;
                vertices.Add(new Vector3(radius * Mathf.Cos(angle), y, radius * Mathf.Sin(angle)));
            }
            return start;
        }

        Vector3 HandleCenter(float t, float angle)
        {
            float level = pot.handleLevel - pot.handleSpan * .5f * Mathf.Cos(t * Mathf.PI);
            Vector3 surface = Surface(pot, level, angle);
            Vector3 radial = new Vector3(surface.x, 0, surface.z).normalized;
            float arc = Mathf.Sin(t * Mathf.PI) * pot.width * .5f * pot.handleReach;
            return surface + radial * (arc - Mathf.Min(pot.wall * .4f, pot.width * .012f));
        }

        void Handle(float angle)
        {
            const int steps = 32, tube = 12;
            Ribbon(.75f);
            Vector3 atMiddle = HandleCenter(.5f, angle);
            Vector3 side = Vector3.Cross(Vector3.up, new Vector3(atMiddle.x, 0, atMiddle.z)).normalized;
            float gauge = Mathf.Min(pot.width * .5f * pot.handleGauge, pot.height * pot.handleSpan * .2f);
            int start = vertices.Count;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector3 center = HandleCenter(t, angle);
                var tangent = (HandleCenter(Mathf.Min(1, t+.001f), angle) - HandleCenter(Mathf.Max(0, t-.001f), angle)).normalized;
                var across = Vector3.Cross(side, tangent).normalized;
                int ring = vertices.Count;
                for (int j = 0; j < tube; j++)
                {
                    float a = j * 2 * Mathf.PI / tube;
                    vertices.Add(center + gauge * (side * Mathf.Cos(a) + across * Mathf.Sin(a)));
                }
                if (i > 0) Strip(ring - tube, ring, tube, 2);
            }
            Cap(start, tube, false, 2);
            Cap(vertices.Count - 1 - tube, tube, true, 2);
        }

        void Ribbon(float seam = .25f)
        {
            if (measuring) layout.Ribbon(seam);
        }

        void Strip(int a, int b, int count, int group,bool reverse = false)
        {
            if (measuring) layout.Band(a,b,count,group,reverse);
        }

        void Cap(int start, int count, bool reverse, int group)
        {
            Vector3 center = Vector3.zero;
            for (int i = 0; i < count; i++) center += vertices[start+i];
            int tip = vertices.Count;
            center /= count;
            // The vessel axis stays inside every radial wedge, even on an asymmetric floor.
            if (group != 2) center = new Vector3(0,vertices[start].y,0);
            vertices.Add(center);
            if (measuring) layout.Disc(start,tip,count,group,reverse);
        }
    }
}
