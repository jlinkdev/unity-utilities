Shader "jlinkdev/Samples/Architectural Grid"
{
    Properties
    {
        _BaseColor ("Stone", Color) = (0.205,0.22,0.245,1)
        _Line ("Tile edge", Color) = (0.265,0.282,0.31,1)
        _Cell ("Tile size (metres)", Float) = 0.5
        _Grain ("Slate grain", 2D) = "gray" {}
    }
    SubShader
    {
        PackageRequirements { "com.unity.render-pipelines.universal" }
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _Line;
                float _Cell;
            CBUFFER_END
            TEXTURE2D(_Grain); SAMPLER(sampler_Grain);
            struct Attributes { float4 position : POSITION; float3 normal : NORMAL; };
            struct Varyings { float4 clip : SV_POSITION; float3 world : TEXCOORD0; half3 normal : TEXCOORD1; half fog : TEXCOORD2; half3 vertexLight : TEXCOORD3; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.world = TransformObjectToWorld(v.position.xyz);
                o.clip = TransformWorldToHClip(o.world);
                o.normal = TransformObjectToWorldNormal(v.normal);
                o.fog = ComputeFogFactor(o.clip.z);
                o.vertexLight = VertexLighting(o.world,o.normal);
                return o;
            }
            half Grid(float2 uv)
            {
                float2 footprint = max(fwidth(uv), 0.0001);
                float2 distance = abs(frac(uv + 0.5) - 0.5);
                float2 edge = 1 - smoothstep(0.008, 0.008 + footprint, distance);
                edge *= 1 - smoothstep(0.15, 0.6, footprint);
                return max(edge.x, edge.y);
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float3 pos = i.world / max(_Cell, 0.01);
                half3 normal = normalize(i.normal);
                half3 weights = pow(abs(normal), 6);
                weights /= max(dot(weights, half3(1,1,1)), 0.0001);
                half edge = dot(weights, half3(Grid(pos.zy), Grid(pos.xz), Grid(pos.xy)));
                half grain = dot(weights, half3(SAMPLE_TEXTURE2D(_Grain,sampler_Grain,pos.zy*.36).r,
                    SAMPLE_TEXTURE2D(_Grain,sampler_Grain,pos.xz*.36).r, SAMPLE_TEXTURE2D(_Grain,sampler_Grain,pos.xy*.36).r));
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = lerp(_BaseColor.rgb, _Line.rgb, edge) * (.9 + .35 * grain);
                surface.smoothness = .14;
                surface.occlusion = surface.alpha = 1;
                InputData lighting = (InputData)0;
                lighting.positionWS = i.world;
                lighting.normalWS = normal;
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.world);
                lighting.shadowCoord = TransformWorldToShadowCoord(i.world);
                lighting.bakedGI = SampleSH(normal);
                lighting.vertexLighting = i.vertexLight;
                lighting.shadowMask = half4(1,1,1,1);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.clip);
                half4 color = UniversalFragmentPBR(lighting,surface);
                return half4(MixFog(color.rgb,i.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    Fallback "Universal Render Pipeline/Lit"
}
