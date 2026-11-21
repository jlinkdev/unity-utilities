Shader "JLink/Plants/Botanical PBR"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base color (sRGB)", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1,1,1,1)
        _BotanicalColor ("Procedural coloration", Range(0,1)) = 1
        [Normal][NoScaleOffset] _BumpMap ("Tangent-space normal", 2D) = "bump" {}
        _NormalStrength ("Normal strength", Range(0,2)) = 1
        [NoScaleOffset] _MaskMap ("Occlusion (R) Roughness (G) Metallic (B)", 2D) = "white" {}
        _Occlusion ("Occlusion strength", Range(0,1)) = 1
        _Roughness ("Roughness multiplier", Range(0,2)) = .55
        _Metallic ("Metallic multiplier", Range(0,1)) = 0
    }
    SubShader
    {
        PackageRequirements { "com.unity.render-pipelines.universal": "17.0" }
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #define PLANT_TO_LINEAR SRGBToLinear
            #define PLANT_UNPACK_NORMAL UnpackNormalScale
            #include "PlantSurface.cginc"

            struct Attributes
            {
                float4 positionOS:POSITION;
                float3 normalOS:NORMAL;
                float4 tangentOS:TANGENT;
                float2 uv:TEXCOORD0;
                float2 pattern:TEXCOORD2;
                half4 color:COLOR;
            };
            struct Varyings
            {
                float4 pos:SV_POSITION;
                float3 world:TEXCOORD0;
                float3 normal:TEXCOORD1;
                float4 tangent:TEXCOORD2;
                float4 uvPattern:TEXCOORD3;
                half4 color:COLOR;
                half4 fogAndVertexLight:TEXCOORD4;
                float4 shadow:TEXCOORD5;
            };
            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.pos = pos.positionCS;
                o.world = pos.positionWS;
                o.normal = n.normalWS;
                o.tangent = float4(n.tangentWS, v.tangentOS.w * GetOddNegativeScale());
                o.uvPattern = float4(v.uv, v.pattern);
                o.color = v.color;
                o.fogAndVertexLight = half4(ComputeFogFactor(o.pos.z),VertexLighting(o.world,o.normal));
                o.shadow = GetShadowCoord(pos);
                return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                SurfaceData surface = (SurfaceData)0;
                ReadPlantSurface(i.uvPattern.xy,i.uvPattern.zw,i.color.rgb,surface.albedo,
                    surface.normalTS,surface.occlusion,surface.smoothness,surface.metallic);
                surface.alpha = 1;
                InputData data = (InputData)0;
                data.positionWS = i.world;
                data.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(surface.normalTS,
                    half3x3(i.tangent.xyz,i.tangent.w * cross(i.normal,i.tangent.xyz),i.normal)));
                data.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.world);
                data.bakedGI = SampleSH(data.normalWS);
                data.shadowMask = 1;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    data.shadowCoord = i.shadow;
                #else
                    data.shadowCoord = TransformWorldToShadowCoord(i.world);
                #endif
                data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.pos);
                data.vertexLighting = i.fogAndVertexLight.yzw;
                half4 lit = UniversalFragmentPBR(data,surface);
                lit.rgb = MixFog(lit.rgb,i.fogAndVertexLight.x);
                return lit;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows addshadow vertex:PlantVertex
        #include "UnityStandardUtils.cginc"
        #define PLANT_TO_LINEAR GammaToLinearSpace
        #define PLANT_UNPACK_NORMAL UnpackScaleNormal
        #include "PlantSurface.cginc"
        struct Input { float4 leafCoords; float4 leafColor; };
        void PlantVertex(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            o.leafCoords = float4(v.texcoord.xy,v.texcoord2.xy);
            o.leafColor = v.color;
        }
        void surf(Input i, inout SurfaceOutputStandard o)
        {
            half3 albedo, normalTS;
            half occlusion, smoothness, metallic;
            ReadPlantSurface(i.leafCoords.xy,i.leafCoords.zw,i.leafColor.rgb,albedo,
                normalTS,occlusion,smoothness,metallic);
            o.Albedo = albedo;
            o.Normal = normalTS;
            o.Occlusion = occlusion;
            o.Smoothness = smoothness;
            o.Metallic = metallic;
            o.Alpha = 1;
        }
        ENDCG
    }
}
