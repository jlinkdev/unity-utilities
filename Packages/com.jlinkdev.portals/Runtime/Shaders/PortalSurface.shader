Shader "jlinkdev/Portals/Portal Surface"
{
    Properties
    {
        [NoScaleOffset] _PortalTexture("Portal Texture", 2D) = "black" {}
        _Tint("Tint", Color) = (1, 1, 1, 1)
        _EdgeColor("Edge Color", Color) = (0.08, 0.75, 1, 1)
        _EdgeWidth("Edge Width", Range(0, 0.25)) = 0.025
        [HideInInspector] _PortalTerminal("Portal Terminal", Float) = 0
        [HDR] _TerminalColor("Recursion End Color", Color) = (0.005, 0.018, 0.045, 1)
        [HDR] _TerminalGlowColor("Recursion End Glow", Color) = (0.04, 0.8, 1.6, 1)
        _TerminalGlowIntensity("Recursion End Glow Intensity", Range(0, 4)) = 1.15
        [HDR] _BackColor("Inactive Back Color", Color) = (0.006, 0.01, 0.018, 1)
        [HDR] _BackGlowColor("Inactive Back Accent", Color) = (0.015, 0.22, 0.3, 1)
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
                half _PortalTerminal;
                half4 _TerminalColor;
                half4 _TerminalGlowColor;
                half _TerminalGlowIntensity;
                half4 _BackColor;
                half4 _BackGlowColor;
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

            half4 Frag(Varyings input, FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float2 edgeDistance = min(input.uv, 1.0 - input.uv);
                half edge = 1.0h - smoothstep(0.0h, _EdgeWidth, min(edgeDistance.x, edgeDistance.y));
                // Unity's built-in Quad winding faces opposite local +Z, while
                // Portal defines its active side with transform.forward (+Z).
                half activePortalFace = IS_FRONT_VFACE(isFrontFace, 0.0h, 1.0h);
                if (activePortalFace < 0.5h)
                {
                    // The reverse side is an intentionally inactive matte panel.
                    // A faint center seam keeps it readable as portal hardware
                    // without suggesting that the live view can be entered here.
                    float2 backPosition = input.uv * 2.0 - 1.0;
                    half centerSeam = 1.0h - smoothstep(0.012h, 0.055h, abs(backPosition.x));
                    half seamFade = saturate(1.0h - abs(backPosition.y));
                    half pulse = 0.82h + 0.18h * sin(_Time.y * 1.1h);
                    half3 backView = _BackColor.rgb + _BackGlowColor.rgb *
                        (edge * 0.28h + centerSeam * seamFade * pulse * 0.16h);
                    return half4(backView, 1.0h);
                }

                float2 screenUV = input.screenPos.xy / max(input.screenPos.w, 0.0001);
                half4 view = SAMPLE_TEXTURE2D(_PortalTexture, sampler_PortalTexture, screenUV) * _Tint;

                // Give bounded recursion an intentional visual horizon. This is
                // rendered only into the deepest texture, so shallower levels
                // naturally inherit it without a temporal feedback dependency.
                float2 terminalPosition = input.uv * 2.0 - 1.0;
                terminalPosition.y *= 0.58;
                float terminalRadius = length(terminalPosition);
                half terminalFalloff = saturate(1.0h - terminalRadius);
                half terminalCore = pow(terminalFalloff, 4.0h);
                half terminalRing = pow(
                    saturate(0.5h + 0.5h * cos(terminalRadius * 48.0h - _Time.y * 1.8h)),
                    12.0h) * terminalFalloff;
                half terminalHorizon = 1.0h - smoothstep(0.06h, 0.22h, terminalRadius);
                half3 terminalView = _TerminalColor.rgb;
                terminalView += _TerminalGlowColor.rgb * _TerminalGlowIntensity *
                    (terminalCore * 0.22h + terminalRing * 0.12h + terminalHorizon * 0.72h);
                view.rgb = lerp(view.rgb, terminalView, saturate(_PortalTerminal));

                return lerp(view, _EdgeColor, edge * _EdgeColor.a);
            }
            ENDHLSL
        }
    }
}
