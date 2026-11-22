Shader "jlinkdev/Beams/EditableSamples/Dusk Sky"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct V { float4 pos:SV_POSITION;float3 ray:TEXCOORD0; };
            V Vert(float4 p:POSITION) { V o; o.pos=TransformObjectToHClip(p.xyz); o.ray=p.xyz;return o; }
            float hash(float2 p){ return frac(sin(dot(p,float2(127.1,313.7)))*43758.5); }
            float noise(float2 p)
            {
                float2 i=floor(p), f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            half4 Frag(V i):SV_Target
            {
                float3 d=normalize(i.ray);
                float h=max(0,d.y);
                float3 sky=lerp(float3(.20,.19,.22),float3(.025,.045,.09),smoothstep(-.05,.50,h));
                float2 p=d.xz/(.15+h)*float2(2.7,4);
                float wisps=noise(p)*.52+noise(p*2.1)*.3+noise(p*4.4)*.18;
                sky+=float3(.10,.076,.075)*smoothstep(.35,.72,wisps);
                float2 atlas=float2(atan2(d.z,d.x),asin(d.y))*210;
                float star=step(.997,hash(floor(atlas)))*saturate(1-4*length(frac(atlas)-.5));
                sky+=star*smoothstep(.04,.3,h)*.8;
                return half4(sky,1);
            }
            ENDHLSL
        }
    }
}
