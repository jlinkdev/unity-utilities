Shader "JLink/Plants/Windowsill Daylight"
{
    Properties
    {
        _PotCenters ("Pot centers in sill XZ", Vector) = (-.49,.04,.48,.04)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _PotCenters;
            struct Input { float4 pos:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Output {
                float4 pos:SV_POSITION; float3 local:TEXCOORD0; float3 normal:TEXCOORD1;
                float3 view:TEXCOORD2; float surface:TEXCOORD3; float4 color:COLOR;
            };
            Output vert(Input i)
            {
                Output o;
                o.pos=UnityObjectToClipPos(i.pos);
                o.local=i.pos.xyz;
                o.normal=UnityObjectToWorldNormal(i.normal);
                o.view=WorldSpaceViewDir(i.pos);
                o.surface=i.uv.x;
                o.color=i.color;
                return o;
            }
            float hash21(float2 p)
            {
                float3 q=frac(float3(p.xyx)*.1031);
                q+=dot(q,q.yzx+33.33);
                return frac((q.x+q.y)*q.z);
            }
            float noise2(float2 p)
            {
                float2 cell=floor(p), t=frac(p);
                t=t*t*(3-2*t);
                return lerp(lerp(hash21(cell),hash21(cell+float2(1,0)),t.x),
                            lerp(hash21(cell+float2(0,1)),hash21(cell+1),t.x),t.y);
            }
            float contact(float2 p)
            {
                // A tight contact at the planter foot and a diffuse shadow away from the window.
                float2 d=(p-float2(.034,-.024))/float2(.195,.146);
                return .32*(1-smoothstep(.62,1.46,length(d)))+.29*(1-smoothstep(.096,.128,length(p)));
            }
            float4 frag(Output i):SV_Target
            {
                float3 n=normalize(i.normal), key=normalize(float3(-.62,.90,-.46));
                float3 rgb=i.color.rgb;
                float light=.79+.21*dot(n,key)+.07*n.y;
                float sheen=pow(saturate(dot(n,normalize(key+normalize(i.view)))),14);
                float2 p=i.local.xz;
                if (i.surface > 1.5 && i.surface < 2.5)
                {
                    float sky=smoothstep(-.23,1.18,i.local.y);
                    float glow=exp(-.52*dot((i.local.xy-float2(-.95,.76)),(i.local.xy-float2(-.95,.76))));
                    rgb=lerp(float3(.76,.80,.72),float3(.69,.81,.84),sky);
                    rgb=lerp(rgb,float3(.98,.94,.82),.37*glow);
                    float horizon=.15 + .14*noise2(float2(i.local.x*2.1,4.3));
                    rgb=lerp(rgb,float3(.64,.71,.61),.32*(1-smoothstep(horizon-.22,horizon+.18,i.local.y)));
                }
                else if (i.surface > .5 && i.surface < 1.5)
                {
                    float2 wood=float2(i.local.x*.9, (i.local.z+i.local.y*.4)*45);
                    float grain=noise2(wood+float2(0,.9*noise2(p*4))) - .5;
                    float window=.5+.5*cos(p.x*1.7+p.y*2.5+.4);
                    rgb*=light*(.95+.08*window+.045*grain+.012*(noise2(wood*3.2)-.5));
                    float shadow=max(contact(p-_PotCenters.xy),contact(p-_PotCenters.zw));
                    rgb*=1-shadow*saturate(n.y);
                }
                else if (i.surface > 2.5 && i.surface < 3.5)
                {
                    float coarse=noise2(p*98);
                    float fade=1-smoothstep(.25,1.1,length(fwidth(p)*325));
                    float grit=lerp(.5,noise2(p*325),fade);
                    rgb=lerp(float3(.235,.171,.123),float3(.41,.305,.202),coarse);
                    rgb+=float3(.19,.16,.125)*smoothstep(.70,.91,grit)-.07*(1-smoothstep(.1,.40,grit));
                    rgb*=.91+.09*light-.20*smoothstep(.110,.139,length(p));
                }
                else if (i.surface > 3.5 && i.surface < 4.5)
                {
                    rgb*=.94+.055*dot(n,key)+.025*n.y;
                }
                else
                {
                    rgb*=light;
                    rgb+=sheen*(i.surface<.5 ? .035 : .012);
                }
                #ifndef UNITY_COLORSPACE_GAMMA
                    rgb=GammaToLinearSpace(rgb);
                #endif
                return float4(rgb,1);
            }
            ENDCG
        }
    }
}
