Shader "jlinkdev/Samples/Studio Grid"
{
    Properties
    {
        _Ground ("Surface", Color) = (0.13, 0.16, 0.20, 1)
        _Line ("Grid", Color) = (0.24, 0.28, 0.33, 1)
        _Grain ("Slate grain", 2D) = "gray" {}
        _GridVisibility ("Grid visibility", Range(0,1)) = 1
        _Cells ("Cells per unit of mesh", Float) = 64
        _HazeDistance ("Haze e-folding distance (grid cells)", Float) = 48
    }
    SubShader
    {
        PackageRequirements { "com.unity.render-pipelines.universal" }
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Studio Atmosphere.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Ground, _Line;
                float _Cells, _GridVisibility, _HazeDistance;
            CBUFFER_END
            TEXTURE2D(_Grain); SAMPLER(sampler_Grain);
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; half fog : TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.world = position.positionWS;
                output.uv = input.uv;
                output.fog = ComputeFogFactor(position.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 grid = (input.uv - 0.5) * max(1.0, _Cells);
                float2 footprint = max(fwidth(grid), 0.0001);
                float2 edge = abs(frac(grid + 0.5) - 0.5) / footprint;
                half gridLine = (1.0 - smoothstep(0.0, 1.3, min(edge.x, edge.y))) *
                    (1.0 - smoothstep(0.18, 0.9, max(footprint.x, footprint.y)));
                Light key = GetMainLight(TransformWorldToShadowCoord(input.world));
                half light = saturate(key.direction.y) * key.shadowAttenuation;
                half3 albedo = lerp(_Ground.rgb, _Line.rgb, gridLine * 0.28h * _GridVisibility);
                half grain = SAMPLE_TEXTURE2D(_Grain, sampler_Grain, grid * .38).r;
                half3 ground = albedo * (.92 + .29 * grain) * (.48 + .52 * key.color * light);
                float cellSize = length(TransformObjectToWorldDir(float3(1,0,0), false)) / max(_Cells, 1.0);
                float travel = unity_OrthoParams.w > .5 ? -TransformWorldToView(input.world).z : distance(_WorldSpaceCameraPos.xyz, input.world);
                float transmission = exp(-max(0.0,travel) / max(.001, cellSize * _HazeDistance));
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                    float cameraDepth = max(0.0,-TransformWorldToView(input.world).z - _ProjectionParams.y);
                    return half4(MixFog(ground,ComputeFogFactorZ0ToFar(cameraDepth)),1);
                #else
                    return half4(lerp(StudioHorizonColor(),ground,transmission),1);
                #endif
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    Fallback "Unlit/Color"
}
