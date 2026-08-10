#ifndef JLINKDEV_PORTAL_CLIP_INCLUDED
#define JLINKDEV_PORTAL_CLIP_INCLUDED

// Shader Graph Custom Function node:
// Source: File, Function: PortalClip_float (or PortalClip_half)
// Connect Position (World), _PortalClipPlane, and _PortalClipEnabled.
// Route Keep to Alpha and use an Alpha Clip Threshold of 0.5.
void PortalClip_float(float3 PositionWS, float4 ClipPlane, float Enabled, out float Keep)
{
    float signedDistance = dot(float4(PositionWS, 1.0), ClipPlane);
    Keep = lerp(1.0, step(0.0, signedDistance), saturate(Enabled));
}

void PortalClip_half(half3 PositionWS, half4 ClipPlane, half Enabled, out half Keep)
{
    half signedDistance = dot(half4(PositionWS, 1.0h), ClipPlane);
    Keep = lerp(1.0h, step(0.0h, signedDistance), saturate(Enabled));
}

#endif
