#ifndef JLINKDEV_BEAM_GRAPH_FUNCTIONS_INCLUDED
#define JLINKDEV_BEAM_GRAPH_FUNCTIONS_INCLUDED

float BeamHash11(float value)
{
    value = frac(value * 0.1031);
    value *= value + 33.33;
    value *= value + value;
    return frac(value);
}

float BeamValueNoise1D(float coordinate, float seed)
{
    float cell = floor(coordinate);
    float fraction = frac(coordinate);
    float blend = fraction * fraction * (3.0 - 2.0 * fraction);
    float a = BeamHash11(cell + seed * 17.17);
    float b = BeamHash11(cell + 1.0 + seed * 17.17);
    return lerp(a, b, blend) * 2.0 - 1.0;
}

float BeamFractalNoise1D(float coordinate, float seed, int octaveCount, float roughness)
{
    float value = 0.0;
    float weight = 1.0;
    float weightSum = 0.0;
    [loop]
    for (int octave = 0; octave < octaveCount; octave++)
    {
        value += BeamValueNoise1D(coordinate, seed + octave * 13.7) * weight;
        weightSum += weight;
        coordinate *= 2.0;
        weight *= roughness;
    }
    return weightSum > 0.0 ? value / weightSum : 0.0;
}

float BeamEndpointMaskValue(float normalizedPosition, float startFalloff, float endFalloff)
{
    float startMask = smoothstep(0.0, max(startFalloff, 0.0001), normalizedPosition);
    float endMask = smoothstep(0.0, max(endFalloff, 0.0001), 1.0 - normalizedPosition);
    return startMask * endMask;
}

void BeamCoordinates_float(float2 uv0, float2 uv1, out float normalizedPosition, out float side, out float distance, out float width)
{
    normalizedPosition = uv0.x;
    side = uv0.y;
    distance = uv1.x;
    width = uv1.y;
}

void BeamEndpointMask_float(float normalizedPosition, float startFalloff, float endFalloff, out float mask)
{
    mask = BeamEndpointMaskValue(normalizedPosition, startFalloff, endFalloff);
}

void BeamValueNoise_float(float coordinate, float seed, out float noise)
{
    noise = BeamValueNoise1D(coordinate, seed);
}

void BeamVertexNoise_float(
    float distance,
    float normalizedPosition,
    float time,
    float seed,
    float frequency,
    float speed,
    float amplitude,
    float startFalloff,
    float endFalloff,
    out float2 displacement)
{
    float coordinate = distance * frequency + time * speed;
    float mask = BeamEndpointMaskValue(normalizedPosition, startFalloff, endFalloff);
    displacement.x = BeamFractalNoise1D(coordinate, seed, 3, 0.5) * amplitude * mask;
    displacement.y = BeamFractalNoise1D(coordinate + 31.73, seed + 9.19, 3, 0.5) * amplitude * mask;
}

void BeamFlow_float(float distance, float time, float speed, float frequency, float seed, out float flow)
{
    float phase = distance * frequency - time * speed + seed;
    flow = 0.5 + 0.5 * sin(phase * 6.28318530718);
}

void BeamPulse_float(float normalizedPosition, float pulsePosition, float pulseWidth, out float pulse)
{
    float distanceFromPulse = abs(normalizedPosition - pulsePosition);
    pulse = 1.0 - smoothstep(0.0, max(pulseWidth, 0.0001), distanceFromPulse);
}

void BeamCoreHalo_float(float side, float coreWidth, float edgePower, out float core, out float halo)
{
    float radial = saturate(abs(side));
    core = 1.0 - smoothstep(0.0, max(coreWidth, 0.0001), radial);
    halo = pow(saturate(1.0 - radial), max(edgePower, 0.0001));
}

#endif
