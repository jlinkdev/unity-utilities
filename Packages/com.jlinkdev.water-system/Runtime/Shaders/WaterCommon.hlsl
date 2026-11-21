#ifndef JLINKDEV_WATER_COMMON_INCLUDED
#define JLINKDEV_WATER_COMMON_INCLUDED

// All consumers use the same phase, displacement and mesh diagonal. Never animate a visual-only vertex offset.
CBUFFER_START(UnityPerMaterial)
float4 _WaterOrigin; // xyz translation, w finite volume depth
float4 _WaterSize;   // xy dimensions, z number of cells along each axis
float _WaterTime;
int _WaterWaveCount;
float4 _WaterWaves[8];      // normalized direction xy, amplitude z, wave number w
float4 _WaterWaveMotion[8]; // angular frequency x, phase y
float4 _WaterShallowColor, _WaterDeepColor, _WaterFogColor, _WaterFoamColor;
float4 _WaterAbsorption, _WaterlineColor;
float _WaterDensity, _WaterRefraction, _WaterSmoothness;
float _WaterFoamWidth, _WaterFoamStrength, _WaterDetailNormalStrength;
float _WaterUnderwaterSaturation, _WaterDistortion;
float _WaterlineWidth, _WaterlineStrength;
float _WaterCausticsStrength, _WaterCausticsScale, _WaterCausticsSpeed;
CBUFFER_END

float WaterHeight(float2 positionXZ)
{
    float height = _WaterOrigin.y;
    [loop] for (int i = 0; i < min(_WaterWaveCount, 8); ++i)
    {
        float4 wave = _WaterWaves[i];
        float phase = dot(positionXZ - _WaterOrigin.xz, wave.xy) * wave.w +
                      _WaterWaveMotion[i].x * _WaterTime + _WaterWaveMotion[i].y;
        height += sin(phase) * wave.z;
    }
    return height;
}

float3 WaterNormal(float2 positionXZ)
{
    float2 slope = 0;
    [loop] for (int i = 0; i < min(_WaterWaveCount, 8); ++i)
    {
        float4 wave = _WaterWaves[i];
        float phase = dot(positionXZ - _WaterOrigin.xz, wave.xy) * wave.w +
                      _WaterWaveMotion[i].x * _WaterTime + _WaterWaveMotion[i].y;
        slope += cos(phase) * wave.z * wave.w * wave.xy;
    }
    return normalize(float3(-slope.x, 1, -slope.y));
}

// Exact barycentric height of the rendered grid, including points on cell and body boundaries.
// For a=(x,z), b=(x+1,z), c=(x,z+1), d=(x+1,z+1), indices are a,c,b and b,c,d.
float WaterMeshHeight(float2 positionXZ)
{
    float cells = max(1, _WaterSize.z);
    float2 grid = saturate((positionXZ - _WaterOrigin.xz) / _WaterSize.xy + .5) * cells;
    float2 cell = min(floor(grid), cells - 1);
    float2 f = grid - cell;
    float2 stepSize = _WaterSize.xy / cells;
    float2 a = _WaterOrigin.xz - _WaterSize.xy * .5 + cell * stepSize;
    float b = WaterHeight(a + float2(stepSize.x, 0));
    float c = WaterHeight(a + float2(0, stepSize.y));
    if (f.x + f.y <= 1)
        return WaterHeight(a) * (1 - f.x - f.y) + b * f.x + c * f.y;
    return b * (1 - f.y) + c * (1 - f.x) + WaterHeight(a + stepSize) * (f.x + f.y - 1);
}

float WaterDomainDistance(float3 positionWS)
{
    float2 edge = _WaterSize.xy * .5 - abs(positionWS.xz - _WaterOrigin.xz);
    return min(min(edge.x, edge.y), positionWS.y - (_WaterOrigin.y - _WaterOrigin.w));
}

float WaterDeviceDepth(float rawDepth)
{
    #if !UNITY_REVERSED_Z
        return lerp(UNITY_NEAR_CLIP_VALUE, 1, rawDepth);
    #else
        return rawDepth;
    #endif
}

bool WaterIsSky(float rawDepth)
{
    #if UNITY_REVERSED_Z
        return rawDepth <= 0.000001;
    #else
        return rawDepth >= 0.999999;
    #endif
}

float3 WaterWorldPosition(float2 uv, float rawDepth)
{
    return ComputeWorldSpacePosition(uv, WaterDeviceDepth(rawDepth), UNITY_MATRIX_I_VP);
}

// Distance to the finite volume sides or floor from an interior ray origin.
float WaterVolumeExit(float3 origin, float3 direction)
{
    float2 edge = _WaterOrigin.xz + sign(direction.xz) * _WaterSize.xy * .5;
    float2 distances = (edge - origin.xz) / (direction.xz + (abs(direction.xz) < 1e-6) * 1e-6);
    float result = min(abs(direction.x) < 1e-6 ? 1e7 : max(0, distances.x),
                       abs(direction.z) < 1e-6 ? 1e7 : max(0, distances.y));
    if (direction.y < -1e-6)
        result = min(result, max(0, (_WaterOrigin.y - _WaterOrigin.w - origin.y) / direction.y));
    return result;
}

float2 WaterHash(float2 p)
{
    return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
}

// Animated Voronoi ridges stay anchored to world geometry; this is an artistic pattern, not photon tracing.
float WaterCaustics(float3 positionWS)
{
    if (_WaterCausticsStrength <= 0) return 0;
    float2 p = positionWS.xz * max(.01, _WaterCausticsScale);
    // Warp the coordinate field to bend the ridges; unwarped Voronoi edges look like a rigid tiled floor.
    p += .35 * float2(sin(p.y * 1.7 + _WaterTime * _WaterCausticsSpeed * .7),
                     sin(p.x * 1.4 - _WaterTime * _WaterCausticsSpeed * .5));
    float2 cell = floor(p), f = frac(p);
    float first = 8, second = 8;
    [unroll] for (int y = -1; y <= 1; y++)
    [unroll] for (int x = -1; x <= 1; x++)
    {
        float2 neighbor = float2(x, y);
        float2 random = WaterHash(cell + neighbor);
        float2 cellPosition = .5 + .4 * sin(6.2831853 * random + _WaterTime * _WaterCausticsSpeed);
        float2 delta = neighbor + cellPosition - f;
        float distance = dot(delta, delta);
        if (distance < first) { second = first; first = distance; }
        else second = min(second, distance);
    }
    float ridge = 1 - smoothstep(.015, .14, second - first);
    float depth = max(0, WaterHeight(positionWS.xz) - positionWS.y);
    float fade = exp(-depth * max(.05, _WaterDensity) * .7) * saturate(depth * 3);
    return ridge * ridge * fade * _WaterCausticsStrength;
}

#endif
