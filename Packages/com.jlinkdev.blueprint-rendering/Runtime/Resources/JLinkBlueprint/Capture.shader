Shader "Hidden/jlinkdev/Blueprint/Capture"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite On ZTest LEqual
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        struct Attributes { float3 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float eye : TEXCOORD1; };
        Varyings Vert(Attributes v)
        {
            UNITY_SETUP_INSTANCE_ID(v);
            Varyings o;
            VertexPositionInputs p = GetVertexPositionInputs(v.positionOS);
            o.positionCS = p.positionCS;
            o.eye = -p.positionVS.z;
            o.normalWS = TransformObjectToWorldNormal(v.normalOS);
            return o;
        }
        float4 Surface(Varyings i) : SV_Target { return float4(normalize(i.normalWS), i.eye); }
        TEXTURE2D(_JlbPrevious);
        float4 Peel(Varyings i) : SV_Target
        {
            float previous = LOAD_TEXTURE2D(_JlbPrevious, int2(i.positionCS.xy)).w;
            if (previous <= 0 || i.eye <= previous + max(1e-7, previous * 2e-6)) discard;
            return Surface(i);
        }
        ENDHLSL
        Pass
        {
            Name "First surface"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Surface
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "Next surface"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Peel
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
