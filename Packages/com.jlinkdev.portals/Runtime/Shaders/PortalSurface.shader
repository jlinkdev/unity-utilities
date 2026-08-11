Shader "jlinkdev/Portals/Portal Surface"
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
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "PortalSurface"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On
            // Keep the portal surface behind directly rendered traveller fragments
            // at the shared clip boundary to prevent a sub-pixel depth seam.
            Offset 1, 1

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
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / max(input.screenPos.w, 0.0001);
                half4 view = SAMPLE_TEXTURE2D(_PortalTexture, sampler_PortalTexture, screenUV) * _Tint;
                float2 edgeDistance = min(input.uv, 1.0 - input.uv);
                half edge = 1.0h - smoothstep(0.0h, _EdgeWidth, min(edgeDistance.x, edgeDistance.y));
                return lerp(view, _EdgeColor, edge * _EdgeColor.a);
            }
            ENDHLSL
        }
    }
}
