Shader "JLink/Plants/Botanical"
{
    Properties
    {
        _MainTex ("Surface texture (UV0)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "PlantPigment.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST, _Color;
            struct Input { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float2 pattern:TEXCOORD2; fixed4 color:COLOR; };
            struct Output { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float2 pattern:TEXCOORD1; float3 n:TEXCOORD2; float3 view:TEXCOORD3; fixed4 color:COLOR; };
            Output vert(Input v)
            {
                Output o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.pattern = v.pattern;
                o.n = UnityObjectToWorldNormal(v.normal);
                o.view = WorldSpaceViewDir(v.vertex);
                o.color = v.color;
                return o;
            }
            float4 frag(Output i):SV_Target
            {
                float3 albedo = PlantPigment(i.color.rgb,i.uv,i.pattern);
                float3 n = normalize(i.n);
                float3 key = normalize(float3(-.55,.88,-.65));
                float diffuse = .69 + .27*dot(n,key) + .10*n.y;
                float halfDot = saturate(dot(n,normalize(key+normalize(i.view))));
                float sheen = i.pattern.x > 7.5 ? .048*pow(halfDot,8) : .065*pow(halfDot,18);
                float3 rgb = albedo*diffuse + sheen;
                #ifndef UNITY_COLORSPACE_GAMMA
                rgb = GammaToLinearSpace(rgb);
                #endif
                return float4(rgb*_Color.rgb*tex2D(_MainTex,TRANSFORM_TEX(i.uv,_MainTex)).rgb,1);
            }
            ENDCG
        }
    }
}
