Shader "jlinkdev/Beams/Endpoint Light"
{
    Properties
    {
        [HDR] _Tint("Tint", Color) = (1,1,1,1)
        _Facing("Face camera", Float) = 0
        _Surface("Surface footprint", Float) = 0
        _Seed("Phase", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Blend One One
            Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint;
            float _Facing, _Surface, _Seed;
            CBUFFER_END
            struct A { float3 p:POSITION; float2 uv:TEXCOORD0; half4 tint:COLOR; };
            struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; half4 tint:COLOR; };
            V Vert(A v)
            {
                V o;
                float3 ws = TransformObjectToWorld(v.p);
                if (_Facing > 0.5)
                {
                    float4x4 model = GetObjectToWorldMatrix();
                    float2 scale = float2(length(model._m00_m10_m20), length(model._m01_m11_m21));
                    ws = TransformObjectToWorld(float3(0,0,0)) +
                         UNITY_MATRIX_I_V._m00_m10_m20 * v.p.x * scale.x +
                         UNITY_MATRIX_I_V._m01_m11_m21 * v.p.y * scale.y;
                }
                o.p = TransformWorldToHClip(ws);
                o.uv = v.uv * 2 - 1;
                o.tint = v.tint * _Tint;
                return o;
            }
            half4 Frag(V v):SV_Target
            {
                float2 p = v.uv;
                float r = length(p);
                float t = _Time.y + _Seed;
                float a = atan2(p.y,p.x);
                float core = exp2(-52*r*r);
                float haze = exp2(-6*r*r) * saturate(1-r);
                float3 rgb;
                if (_Surface > 0.5)
                {
                    float ridges = sin(a*7 + 2*sin(a*3) + r*15) * 0.045 + sin(a*11-r*7)*0.025;
                    float crust = exp2(-85*abs(r - 0.46 - ridges - 0.018*sin(3*t+a*5)));
                    float cracks = pow(saturate(cos(a*12+sin(a*3+r*8)*2)),18) * haze;
                    rgb = v.tint.rgb * (haze*1.1 + crust*1.7 + cracks*2.5) + core*2;
                }
                else
                {
                    float streak = exp2(-155*abs(p.y)) * exp2(-7*abs(p.x));
                    rgb = v.tint.rgb * (haze + core*2 + streak) + core*core*2;
                }
                return half4(rgb * v.tint.a * saturate((1-r)*12),0);
            }
            ENDHLSL
        }
    }
}
