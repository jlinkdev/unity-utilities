Shader "jlinkdev/Volumetric Fog and Rain/Samples/Courtyard Stone"
{
    Properties
    {
        _StoneTint ("Stone tone", Color) = (.29,.30,.33,1)
        _StoneMap ("Aggregate albedo (sRGB)", 2D) = "gray" {}
        [Normal] _NormalMap ("Aggregate normal", 2D) = "bump" {}
        _RoughnessMap ("Aggregate roughness (linear)", 2D) = "gray" {}
        _RoughnessOffset ("Roughness offset", Range(-.65,.35)) = 0
        _NormalStrength ("Stone relief", Range(0,2)) = 1
        _Occlusion ("Indirect light visibility", Range(0,1)) = 1
        _GroutTone ("Joint albedo multiplier", Range(.1,3)) = .55
        _ScanSize ("Scan width (metres)", Range(.25,6)) = 1.8
        _TileSize ("Grid spacing (metres)", Range(.1,4)) = .75
        _JointWidth ("Joint width (metres)", Range(.001,.02)) = .003
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _StoneTint;
                half _RoughnessOffset, _NormalStrength, _Occlusion, _GroutTone;
                float _ScanSize, _TileSize, _JointWidth;
            CBUFFER_END
            TEXTURE2D(_StoneMap); SAMPLER(sampler_StoneMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_RoughnessMap); SAMPLER(sampler_RoughnessMap);

            struct Attributes { float3 position : POSITION; float3 normal : NORMAL; };
            struct Varyings
            {
                float4 clip : SV_POSITION;
                float3 world : TEXCOORD0;
                half3 normal : TEXCOORD1;
                half fog : TEXCOORD2;
                half3 vertexLighting : TEXCOORD3;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.world = TransformObjectToWorld(input.position);
                output.normal = TransformObjectToWorldNormal(input.normal);
                output.clip = TransformWorldToHClip(output.world);
                output.fog = ComputeFogFactor(output.clip.z);
                output.vertexLighting = VertexLighting(output.world,output.normal);
                return output;
            }

            float TileRandom(float2 tile)
            {
                float3 h = frac(float3(tile.xyx) * float3(.1031,.1030,.0973));
                h += dot(h,h.yzx + 33.33);
                return frac((h.x+h.y)*h.z);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half3 n = normalize(input.normal);
                half3 axis = abs(n);
                half3 tangent;
                if (axis.x > axis.y && axis.x > axis.z) tangent = half3(0,0,-sign(n.x));
                else if (axis.y > axis.z) tangent = half3(1,0,0);
                else tangent = half3(sign(n.z),0,0);
                tangent = normalize(tangent - n*dot(tangent,n));
                half3 bitangent = cross(n,tangent);

                // Project in metres so long scaled slabs keep the same grid and grain as the sill.
                float2 metres = float2(dot(input.world,tangent),dot(input.world,bitangent));
                float2 tile = floor(metres / _TileSize);
                float random = TileRandom(tile);
                float2 uv = metres / _ScanSize;
                float2 dx = ddx(uv), dy = ddy(uv);
                // Each cut stone comes from a different portion of the same seamless material.
                uv += float2(random,TileRandom(tile+17.7));
                half3 albedo = SAMPLE_TEXTURE2D_GRAD(_StoneMap,sampler_StoneMap,uv,dx,dy).rgb;
                half roughness = SAMPLE_TEXTURE2D_GRAD(_RoughnessMap,sampler_RoughnessMap,uv,dx,dy).r;
                half3 relief = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_NormalMap,sampler_NormalMap,uv,dx,dy),_NormalStrength);

                float2 local = frac(metres/_TileSize);
                float2 edgeDistance = min(local,1-local) * _TileSize;
                float2 pixel = max(fwidth(metres),.0001);
                float2 fade = 1-smoothstep(.04*_TileSize,.35*_TileSize,pixel);
                float2 groove = saturate((.5*_JointWidth-edgeDistance)/pixel+.5) * fade;
                half joint = max(groove.x,groove.y);
                float2 bevel = (1-smoothstep(.5*_JointWidth,.5*_JointWidth+.004+pixel,edgeDistance)) * fade;
                relief.xy += sign(local-.5)*bevel*.18;
                n = normalize(tangent*relief.x + bitangent*relief.y + n*relief.z);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo * _StoneTint.rgb * lerp(1.90,2.08,random) * lerp(1,_GroutTone,joint);
                surface.smoothness = 1-saturate(roughness+_RoughnessOffset+joint*.12);
                surface.occlusion = _Occlusion * lerp(1,.9,joint);
                surface.alpha = 1;
                InputData light = (InputData)0;
                light.positionWS = input.world;
                light.normalWS = n;
                light.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.world);
                light.shadowCoord = TransformWorldToShadowCoord(input.world);
                light.bakedGI = SampleSH(n);
                light.vertexLighting = input.vertexLighting;
                light.shadowMask = 1;
                light.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.clip);
                half4 color = UniversalFragmentPBR(light,surface);
                return half4(MixFog(color.rgb,input.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    Fallback Off
}
