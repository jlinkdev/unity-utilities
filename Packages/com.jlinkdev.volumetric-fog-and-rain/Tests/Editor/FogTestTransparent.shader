Shader "Hidden/jlinkdev/Fog Test Transparent"
{
    Properties { _Color("Premultiplied color", Color) = (0.4,0,0,0.5) _UseCapture("Use opaque capture", Float) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            float4 _Color; float _UseCapture;
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; };
            Varyings Vert(Attributes input) { Varyings o; o.positionCS=TransformObjectToHClip(input.positionOS.xyz); return o; }
            half4 Frag(Varyings input):SV_Target
            {
                if (_UseCapture > .5) return half4(SampleSceneColor(input.positionCS.xy/_ScaledScreenParams.xy),1);
                return _Color;
            }
            ENDHLSL
        }
    }
}
