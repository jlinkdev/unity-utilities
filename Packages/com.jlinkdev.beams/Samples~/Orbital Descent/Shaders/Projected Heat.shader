Shader "jlinkdev/Beams/Samples/Projected Heat"
{
    Properties { [HDR] _Tint ("Heat", Color) = (2,0.5,0.03,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+9" "RenderType"="Transparent" "DisableBatching"="True" }
        Pass
        {
            Cull Off ZTest LEqual ZWrite Off Blend One One
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            CBUFFER_END
            struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            V Vert(float3 p:POSITION, float2 uv:TEXCOORD0) { V o; o.pos=TransformObjectToHClip(p); o.uv=uv*2-1; return o; }
            half4 Frag(V input):SV_Target
            {
                float2 p = input.uv;
                float r = length(p);
                float a = atan2(p.y,p.x);
                float fracture = .055*sin(7*a+2*sin(3*a)+r*12)+.018*sin(15*a-r*7);
                float ring = exp2(-78*abs(r-.42-fracture-.012*sin(3*_Time.y+5*a)));
                float veins = pow(saturate(cos(a*9+sin(a*3+r*8)*2)),14);
                float heat = (1.8+veins)*exp2(-13*r*r) + ring*1.4;
                return half4(_Tint.rgb*heat*(1-smoothstep(.68,1,r)),0);
            }
            ENDHLSL
        }
    }
}
