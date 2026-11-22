Shader "jlinkdev/Beams/EditableSamples/Fractured Basalt"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct V { float4 clip:SV_POSITION; float3 p:TEXCOORD0; float3 n:TEXCOORD1; float fog:TEXCOORD2; };
            V Vert(float4 p:POSITION,float3 n:NORMAL)
            {
                V o; o.p=TransformObjectToWorld(p.xyz); o.clip=TransformWorldToHClip(o.p);
                o.n=TransformObjectToWorldNormal(n); o.fog=ComputeFogFactor(o.clip.z); return o;
            }
            float hash(float2 p) { float3 q=frac(p.xyx*0.1031); q+=dot(q,q.yzx+33.33); return frac((q.x+q.y)*q.z); }
            float noise(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            half4 Frag(V i):SV_Target
            {
                float2 p=i.p.xz+float2(i.p.y*.7,0);
                float macro=noise(p*.24), shard=noise(p*3.6), grit=noise(p*31);
                float2 cells=p*1.28+noise(p*2.2)*.13;
                float edge1=8,edge2=8;
                for(int z=-1;z<=1;z++) for(int x=-1;x<=1;x++)
                {
                    float2 c=floor(cells)+float2(x,z);
                    float2 delta=c+.15+.7*float2(hash(c),hash(c+17.4))-cells;
                    float d=dot(delta,delta);
                    edge2=min(edge2,max(edge1,d)); edge1=min(edge1,d);
                }
                float seam=smoothstep(0,.11,edge2-edge1);
                float ridge=sin(dot(p,float2(.43,.85))+i.p.y*4+10*macro+noise(p*.7));
                float dust=smoothstep(.26,.79,macro+.20*ridge);
                float3 basalt=lerp(float3(.026,.034,.047),float3(.20,.158,.125),dust);
                float grains=lerp(.42,1.45,shard)*lerp(.72,1.16,grit)*lerp(.34,1,seam);
                float3 n=normalize(i.n+float3((shard-.5)*.4,0,(noise(p*3.6+11)-.5)*.4));
                float lighting=.46+max(0,dot(n,normalize(float3(-.6,.55,.35))))*.95;
                float strata=smoothstep(.22,.75,abs(ridge));
                float3 col=basalt*grains*lighting*lerp(.70,1.12,strata);
                InputData inputData=(InputData)0;
                inputData.positionWS=i.p;
                inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.clip);
                uint count=GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(count)
                    Light l=GetAdditionalLight(lightIndex,i.p);
                    col+=basalt*l.color*l.distanceAttenuation*(.24+saturate(dot(n,l.direction)))*grains;
                LIGHT_LOOP_END
                float haze=1-exp(-.021*distance(_WorldSpaceCameraPos,i.p));
                return half4(lerp(col,float3(.13,.145,.18),haze),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Unlit/DepthOnly"
        UsePass "Universal Render Pipeline/Unlit/DepthNormalsOnly"
    }
}
