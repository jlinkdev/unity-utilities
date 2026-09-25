#ifndef JLINKDEV_RAIN_FIELD_INCLUDED
#define JLINKDEV_RAIN_FIELD_INCLUDED
float4 _RainField, _RainDistances, _RainAppearance, _RainNoise, _RainColor;
float4 _RainOffset, _RainOffsetCells, _RainRight, _RainUp, _RainForward;
int _RainSteps, _RainHazeSteps, _RainDebug, _RainSeed;

uint RainMix(uint h)
{
    h ^= h >> 16; h *= 0x7feb352du;
    h ^= h >> 15; h *= 0x846ca68bu;
    return h ^ (h >> 16);
}
uint RainHash(int3 cell, uint salt)
{
    uint3 c = (uint3)cell & 65535u;
    return RainMix(c.x ^ RainMix(c.y + 374761393u) ^ RainMix(c.z + 668265263u) ^ (uint)_RainSeed ^ salt);
}
float RainRandom(uint h) { return (h & 0x00ffffffu) / 16777216.0; }
float3 RainToLocal(float3 p) { return float3(dot(p, _RainRight.xyz), dot(p, _RainUp.xyz), dot(p, _RainForward.xyz)); }

float RainDensity(float3 world)
{
    if (_RainNoise.y <= 0) return 1;
    float3 p = world * _RainNoise.x;
    int3 i = (int3)floor(p);
    float3 f = frac(p); f = f * f * (3 - 2 * f);
    float a = lerp(RainRandom(RainHash(i, 91u)), RainRandom(RainHash(i + int3(1,0,0), 91u)), f.x);
    float b = lerp(RainRandom(RainHash(i + int3(0,1,0), 91u)), RainRandom(RainHash(i + int3(1,1,0), 91u)), f.x);
    float c = lerp(RainRandom(RainHash(i + int3(0,0,1), 91u)), RainRandom(RainHash(i + int3(1,0,1), 91u)), f.x);
    float d = lerp(RainRandom(RainHash(i + int3(0,1,1), 91u)), RainRandom(RainHash(i + int3(1,1,1), 91u)), f.x);
    return lerp(1, smoothstep(0.2, 0.8, lerp(lerp(a,b,f.y), lerp(c,d,f.y), f.z)), _RainNoise.y);
}

#if !defined(_FOG_ONLY)
float RainTraverse(float3 ro, float3 rd, float3 worldOrigin, float3 worldRay, float sceneEnd,
    float mid, float far, float footprintOrigin, float footprintSlope, int layer, float begin, inout int remaining, inout float cost)
{
    float3 size = _RainField.x * float3(1,3,1);
    float3 shift = layer == 0 ? float3(0,0,0) : float3(0.371, 0.619, 0.127);
    float3 gridOrigin = ro / size - _RainOffset.xyz + shift;
    float3 gridRay = rd / size;
    float3 cell = floor(gridOrigin + gridRay * begin);
    float3 signRay = float3(gridRay.x >= 0 ? 1 : -1, gridRay.y >= 0 ? 1 : -1, gridRay.z >= 0 ? 1 : -1);
    float3 inv = float3(abs(gridRay.x) > 1e-8 ? 1 / abs(gridRay.x) : 1e20,
        abs(gridRay.y) > 1e-8 ? 1 / abs(gridRay.y) : 1e20,
        abs(gridRay.z) > 1e-8 ? 1 / abs(gridRay.z) : 1e20);
    float3 boundary = cell + step(0, gridRay);
    float3 next = max(0, (boundary - gridOrigin) * signRay) * inv;
    float end = min(sceneEnd, far);
    float enter = begin, sum = 0;
    [loop] for (int j = 0; j < _RainSteps && enter < end; j++)
    {
                    #if defined(_RAIN_VOLUMES)
                if (remaining <= 0) break;
                remaining--;
            #endif
            cost += 1;
        float exit = min(end, min(next.x, min(next.y, next.z)));
        uint h = RainHash((int3)cell - (int3)_RainOffsetCells.xyz, (uint)layer * 1597334677u);
        float occupancy = RainRandom(h);
        // Smooth threshold maintains nested density subsets without hard brightness popping.
        float presence = saturate((_RainField.w - occupancy) * 12);
        if (presence > 0 && exit > enter)
        {
            float3 random = float3(RainRandom(RainMix(h+1u)), RainRandom(RainMix(h+2u)), RainRandom(RainMix(h+3u)));
            float streakLength = _RainField.y * lerp(0.65, 1.25, RainRandom(RainMix(h+4u)));
            float radius = _RainField.z * lerp(0.65, 1.0, RainRandom(RainMix(h+5u)));
            float3 axis = normalize(float3((random.x - 0.5) * 0.08, 1, (random.z - 0.5) * 0.08));
            float halfLength = streakLength * 0.5;
            // Contain both capsule and maximum filter support in the visited cell.
            // Two independently hashed, shifted grids reduce the resulting boundary pattern.
            float3 margin = abs(axis) * halfLength + radius + _RainField.x * 0.045;
            float3 centerInCell = lerp(margin, size - margin, random);
            // Relative coordinates avoid subtracting two large animated world positions.
            float3 center = (cell - gridOrigin) * size + centerInCell;
            float b = dot(rd, axis), e = dot(center, axis), d = dot(rd, center);
            float t = clamp((d - b * e) / max(1 - b * b, 1e-6), enter, exit);
            float u = clamp(dot(rd * t - center, axis), -halfLength, halfLength);
            t = clamp(dot(center + axis * u, rd), enter, exit);
            u = clamp(dot(rd * t - center, axis), -halfLength, halfLength);
            float distanceToAxis = length(rd * t - center - axis * u);
            float pixelRadius = min(_RainField.x * 0.04, max(0.0001, footprintOrigin + footprintSlope * t));
            float coverage = 1 - smoothstep(max(0, radius - pixelRadius), radius + pixelRadius, distanceToAxis);
            // Preserve approximate energy as subpixel streaks widen; no frame-varying jitter.
            coverage *= radius / max(radius, pixelRadius);
            float fade = smoothstep(0, max(0.001, _RainDistances.x), t) * (1 - smoothstep(mid, max(mid+0.001,far), t));
            // Most visited capsules miss this pixel. Avoid eight-corner noise for zero contribution.
            [branch] if (coverage > 0 && fade > 0)
                sum += coverage * presence * fade * RainDensity(worldOrigin + worldRay * t)
                    * lerp(0.6, 1.4, RainRandom(RainMix(h+6u)));
        }
        if (exit >= end) break;
        float nearest = min(next.x, min(next.y, next.z));
        // Advance all axes at an edge/corner: no zero-length revisits or reciprocal-zero NaNs.
        float3 advance = step(next, nearest.xxx);
        cell += advance * signRay;
        next += advance * inv;
        enter = exit;
    }
    return sum;
}
#endif // !_FOG_ONLY
#endif
