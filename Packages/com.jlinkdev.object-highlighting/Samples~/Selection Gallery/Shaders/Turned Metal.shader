Shader "jlinkdev/Object Highlighting/Samples/Turned Metal"
{
    Properties
    {
        _BaseColor ("Alloy", Color) = (0.54,0.58,0.61,1)
        _Metallic ("Metallic", Range(0,1)) = 0.8
        _Smoothness ("Smoothness", Range(0,1)) = 0.52
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Metallic, _Smoothness;
            CBUFFER_END
            struct Attributes { float4 position : POSITION; float3 normal : NORMAL; };
            struct Varyings { float4 clip : SV_POSITION; float3 world : TEXCOORD0; float3 local : TEXCOORD1; float3 normal : TEXCOORD2; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.world = TransformObjectToWorld(v.position.xyz);
                o.clip = TransformWorldToHClip(o.world);
                o.local = v.position.xyz;
                o.normal = TransformObjectToWorldNormal(v.normal);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float3 v = GetWorldSpaceNormalizeViewDir(i.world);
                Light key = GetMainLight(TransformWorldToShadowCoord(i.world));
                float3 h = SafeNormalize(key.direction + v);
                float3 t = SafeNormalize(mul((float3x3)unity_ObjectToWorld, float3(-i.local.z, 0, i.local.x)));
                float radius = length(i.local.xz);
                float phase = radius * 1050.0;
                float groove = cos(phase) * saturate(1.0 - fwidth(phase) / 3.14);
                half3 diffuse = _BaseColor.rgb * (0.13 + max(0,dot(n,key.direction)) * key.shadowAttenuation * 0.50);
                float brush = pow(saturate(1 - (dot(t,h) * dot(t,h))), 18) * saturate(dot(n,h));
                half3 sky = GlossyEnvironmentReflection(reflect(-v,n), i.world, 1 - _Smoothness, 1, GetNormalizedScreenSpaceUV(i.clip));
                float3 reflected = reflect(-v,n);
                float strip = pow(saturate(dot(reflected,normalize(float3(-.6,.68,-.4)))),20);
                float kicker = pow(saturate(dot(reflected,normalize(float3(.65,.32,.69)))),34);
                half3 metal = lerp(0.04, _BaseColor.rgb, _Metallic);
                // A turned face catches a broad brush highlight perpendicular to its machining lines.
                float3 radial = SafeNormalize(cross(n,t));
                float band = pow(abs(dot(radial, normalize(float3(-.81,.05,-.58)))), 5);
                half3 reflection = sky * .86 + key.color * brush * key.shadowAttenuation * .42
                    + 1.40 * strip + 0.92 * kicker + band * .62;
                half fresnel = pow(1 - saturate(dot(n,v)), 4);
                return half4(diffuse * .81 + metal * reflection + (fresnel * .24 + groove * metal * .025), 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
