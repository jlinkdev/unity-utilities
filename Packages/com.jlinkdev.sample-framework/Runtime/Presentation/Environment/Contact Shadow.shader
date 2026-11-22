Shader "jlinkdev/Samples/Contact Shadow"
{
    SubShader
    {
        Tags { "Queue"="Transparent-40" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(appdata_base input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.texcoord.xy;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float radius = length(input.uv * 2 - 1);
                return half4(0.003, 0.004, 0.007, 0.76 * (1 - smoothstep(0.46, 1, radius)));
            }
            ENDHLSL
        }
    }
}
