Shader "Hidden/jlinkdev/Portals/Near Clip Fix"
{
    Properties
    {
        [NoScaleOffset] _PortalTexture("Portal Texture", 2D) = "black" {}
        _Tint("Tint", Color) = (1, 1, 1, 1)
        _EdgeColor("Edge Color", Color) = (0.08, 0.75, 1, 1)
        _EdgeWidth("Edge Width", Range(0, 0.25)) = 0.025
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "PortalNearClipFix"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_PortalTexture);
            SAMPLER(sampler_PortalTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half4 _EdgeColor;
                half _EdgeWidth;
                float4x4 _PortalWorldToLocal;
                float4 _PortalBounds;
                float4 _PortalPlane;
                float3 _CameraForward;
                float _CapDistance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float orthographic = unity_OrthoParams.w;
                float3 perspectiveDirection = normalize(input.positionWS - _WorldSpaceCameraPos);
                float3 rayDirection = normalize(lerp(perspectiveDirection, _CameraForward, orthographic));
                float3 rayOrigin = lerp(
                    _WorldSpaceCameraPos,
                    input.positionWS - _CameraForward * _CapDistance,
                    orthographic);

                float denominator = dot(_PortalPlane.xyz, rayDirection);
                clip(abs(denominator) - 0.00001);
                float distanceToPlane = -(dot(_PortalPlane.xyz, rayOrigin) + _PortalPlane.w) / denominator;
                clip(distanceToPlane);

                float3 hitWS = rayOrigin + rayDirection * distanceToPlane;
                float3 hitLocal = mul(_PortalWorldToLocal, float4(hitWS, 1.0)).xyz;
                clip(hitLocal.x - _PortalBounds.x);
                clip(hitLocal.y - _PortalBounds.y);
                clip(_PortalBounds.z - hitLocal.x);
                clip(_PortalBounds.w - hitLocal.y);

                float2 apertureSize = max(_PortalBounds.zw - _PortalBounds.xy, 0.0001);
                float2 apertureUV = (hitLocal.xy - _PortalBounds.xy) / apertureSize;
                float2 edgeDistance = min(apertureUV, 1.0 - apertureUV);
                half edge = 1.0h - smoothstep(0.0h, _EdgeWidth, min(edgeDistance.x, edgeDistance.y));

                float2 screenUV = input.screenPos.xy / max(input.screenPos.w, 0.0001);
                half4 view = SAMPLE_TEXTURE2D(_PortalTexture, sampler_PortalTexture, screenUV) * _Tint;
                return lerp(view, _EdgeColor, edge * _EdgeColor.a);
            }
            ENDHLSL
        }
    }
}
