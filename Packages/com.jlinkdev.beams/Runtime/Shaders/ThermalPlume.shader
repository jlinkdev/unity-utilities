Shader "jlinkdev/Beams/Thermal Plume"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-1" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; half4 c:COLOR; };
            struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; half4 c:COLOR; };
            V Vert(A v){V o;o.p=TransformObjectToHClip(v.p.xyz);o.uv=v.uv*2-1;o.c=v.c;return o;}
            float hash(float2 p){float3 q=frac(p.xyx*.1031);q+=dot(q,q.yxz+33.33);return frac((q.x+q.y)*q.z);}
            float noise(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            half4 Frag(V i):SV_Target
            {
                float2 p=i.uv*2.1+float2(0,-_Time.y*.31);
                float n=noise(p)*.58+noise(p*2.4+noise(p))*.29+noise(p*5.5)*.13;
                float density=saturate((.94-length(i.uv)+(n-.5)*.9)*2);
                return half4(i.c.rgb*(.34+n*1.65), i.c.a*density*smoothstep(.15,.72,n));
            }
            ENDHLSL
        }
    }
}
