#ifndef JLINKDEV_WORLD_SCAN_INCLUDED
#define JLINKDEV_WORLD_SCAN_INCLUDED

#define JLINKDEV_WORLD_SCAN_MAX_PULSES 16

int _WorldScanPulseCount;
float4 _WorldScanOriginRadius[JLINKDEV_WORLD_SCAN_MAX_PULSES];
float4 _WorldScanAxisShape[JLINKDEV_WORLD_SCAN_MAX_PULSES];
float4 _WorldScanColorIntensity[JLINKDEV_WORLD_SCAN_MAX_PULSES];
float4 _WorldScanParameters0[JLINKDEV_WORLD_SCAN_MAX_PULSES];
float4 _WorldScanParameters1[JLINKDEV_WORLD_SCAN_MAX_PULSES];
float4 _WorldScanParameters2[JLINKDEV_WORLD_SCAN_MAX_PULSES];
float4 _WorldScanParameters3[JLINKDEV_WORLD_SCAN_MAX_PULSES];
float4 _WorldScanParameters4[JLINKDEV_WORLD_SCAN_MAX_PULSES];

struct WorldScanResult
{
    float3 color;
    float coverage;
    float band;
    float fill;
    float edgeInfluence;
    float depthThreshold;
    float normalThreshold;
    float edgeThickness;
};

float WorldScanHash(float3 position)
{
    position = frac(position * 0.1031);
    position += dot(position, position.yzx + 33.33);
    return frac((position.x + position.y) * position.z);
}

float WorldScanLine(float2 coordinate, float width)
{
    float2 centered = abs(frac(coordinate) - 0.5);
    float2 derivative = max(fwidth(coordinate), 0.0001);
    float2 lineMask = 1.0 - smoothstep(width, width + derivative, 0.5 - centered);
    return max(lineMask.x, lineMask.y);
}

float WorldScanGrid(float3 positionWS, float3 normalWS, float cellSize, float lineWidth, float majorEvery, float majorIntensity)
{
    if (cellSize <= 0.0)
        return 0.0;

    float3 weights = pow(abs(normalWS), 4.0);
    weights /= max(weights.x + weights.y + weights.z, 0.0001);
    float3 minor;
    minor.x = WorldScanLine(positionWS.yz / cellSize, lineWidth);
    minor.y = WorldScanLine(positionWS.xz / cellSize, lineWidth);
    minor.z = WorldScanLine(positionWS.xy / cellSize, lineWidth);
    float minorGrid = dot(minor, weights);

    float majorSize = cellSize * max(majorEvery, 1.0);
    float3 major;
    major.x = WorldScanLine(positionWS.yz / majorSize, lineWidth * 1.6);
    major.y = WorldScanLine(positionWS.xz / majorSize, lineWidth * 1.6);
    major.z = WorldScanLine(positionWS.xy / majorSize, lineWidth * 1.6);
    return max(minorGrid, dot(major, weights) * majorIntensity);
}

WorldScanResult WorldScanEvaluate(float3 positionWS, float3 normalWS)
{
    WorldScanResult result = (WorldScanResult)0;
    result.depthThreshold = 100000.0;
    result.normalThreshold = 100000.0;
    result.edgeThickness = 1.0;

    [loop]
    for (int index = 0; index < JLINKDEV_WORLD_SCAN_MAX_PULSES; index++)
    {
        if (index >= _WorldScanPulseCount)
            break;

        float4 originRadius = _WorldScanOriginRadius[index];
        float4 axisShape = _WorldScanAxisShape[index];
        float4 colorIntensity = _WorldScanColorIntensity[index];
        float4 p0 = _WorldScanParameters0[index];
        float4 p1 = _WorldScanParameters1[index];
        float4 p2 = _WorldScanParameters2[index];
        float4 p3 = _WorldScanParameters3[index];
        float4 p4 = _WorldScanParameters4[index];

        float3 offset = positionWS - originRadius.xyz;
        float distanceToOrigin;
        float shapeMask = 1.0;
        if (axisShape.w > 0.5)
        {
            float axialDistance = dot(offset, normalize(axisShape.xyz));
            distanceToOrigin = length(offset - normalize(axisShape.xyz) * axialDistance);
            if (p4.z > 0.0)
                shapeMask = 1.0 - step(p4.z, abs(axialDistance));
        }
        else
        {
            distanceToOrigin = length(offset);
        }

        float signedDistance = distanceToOrigin - originRadius.w;
        float band = (1.0 - smoothstep(p0.x, p0.x + p0.y, abs(signedDistance))) * shapeMask;
        float fill = saturate((originRadius.w - distanceToOrigin) / max(p0.z, 0.0001)) * step(distanceToOrigin, originRadius.w) * shapeMask;
        if (p0.z <= 0.0001)
            fill = 0.0;

        float timeOffset = _Time.y * p3.w;
        float noise = WorldScanHash(positionWS * p3.y + timeOffset);
        float variation = lerp(1.0, noise, p3.z);
        float cameraDistance = distance(positionWS, _WorldSpaceCameraPos);
        float cameraFade = 1.0 - smoothstep(p4.x, p4.y, cameraDistance);
        float grid = WorldScanGrid(positionWS, normalWS, p1.x, p1.y, p1.z, p2.x);
        float surface = band + fill * p0.w;
        float gridLayer = grid * max(band, fill) * p1.w;
        float coverage = saturate(surface + gridLayer) * variation * cameraFade;
        float3 pulseColor = colorIntensity.rgb * colorIntensity.a;

        result.color += pulseColor * (surface + gridLayer) * variation * cameraFade;
        result.coverage = saturate(result.coverage + coverage);
        result.band = max(result.band, band);
        result.fill = max(result.fill, fill);
        result.edgeInfluence = max(result.edgeInfluence, max(band, fill) * p2.y * variation * cameraFade);
        if (max(band, fill) > 0.001)
        {
            result.depthThreshold = min(result.depthThreshold, p2.z);
            result.normalThreshold = min(result.normalThreshold, p2.w);
            result.edgeThickness = max(result.edgeThickness, p3.x);
        }
    }
    return result;
}

void WorldScanEvaluate_float(float3 PositionWS, float3 NormalWS, out float Band, out float Fill, out float Coverage, out float3 Color)
{
    WorldScanResult result = WorldScanEvaluate(PositionWS, normalize(NormalWS));
    Band = result.band;
    Fill = result.fill;
    Coverage = result.coverage;
    Color = result.color;
}

void WorldScanReveal_float(float3 PositionWS, float3 NormalWS, out float Reveal)
{
    WorldScanResult result = WorldScanEvaluate(PositionWS, normalize(NormalWS));
    Reveal = saturate(max(result.band, result.fill));
}

#endif
