Shader "Hidden/jlinkdev/ObjectHighlight/Mask"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            ZWrite Off ZTest Equal Cull Back
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 Vert(float3 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS);
            }

            // The optional second attachment accumulates all styles for exterior-only compositing.
            void Frag(out half mask : SV_Target0, out half combined : SV_Target1)
            {
                mask = combined = 1;
            }
            ENDHLSL
        }
    }
}
