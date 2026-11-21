Shader "Hidden/jlinkdev/Water System/Boundary"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Displaced Boundary"
            ZWrite On
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Shaders/WaterCommon.hlsl"
            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float eyeDepth : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = float3(input.positionOS.x + _WaterOrigin.x, 0, input.positionOS.z + _WaterOrigin.z);
                world.y = WaterHeight(world.xz);
                output.positionCS = TransformWorldToHClip(world);
                output.eyeDepth = -TransformWorldToView(world).z;
                return output;
            }
            float Frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                // Sign carries face orientation without a second full-resolution float channel.
                return input.eyeDepth * IS_FRONT_VFACE(face, 1, -1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
