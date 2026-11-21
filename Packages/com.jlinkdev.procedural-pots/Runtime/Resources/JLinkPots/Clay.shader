Shader "JLink/Pots/Studio Clay"
{
    Properties
    {
        _Color ("Clay", Color) = (0.76,0.41,0.27,1)
        [HideInInspector] _CheckerTex ("UV checker", 2D) = "white" {}
        [HideInInspector] _ShowChecker ("Show UV checker", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            float4 _SectionPlane;
            float _Section;
            sampler2D _CheckerTex;
            float _ShowChecker;
            struct v2f { float4 pos : SV_POSITION; float3 normal : TEXCOORD0; float3 world : TEXCOORD1; float2 uv : TEXCOORD2; };
            v2f vert(appdata_base input)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(input.vertex);
                o.world = mul(unity_ObjectToWorld, input.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(input.normal);
                o.uv = input.texcoord.xy;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                if (_Section > .5) clip(dot(float4(i.world,1),_SectionPlane));
                float3 n = normalize(i.normal);
                float3 key = normalize(float3(-.55,.82,-.56));
                float3 view = normalize(_WorldSpaceCameraPos.xyz-i.world);
                float diffuse = saturate(dot(n,key));
                float sky = n.y*.5+.5;
                float bounce = saturate(dot(n,normalize(float3(.7,.33,.5))));
                float3 light = .28 + .24*sky + .56*diffuse + bounce*float3(.18,.16,.13);
                float sheen = .05*pow(saturate(dot(n,normalize(key+view))),14);
                float3 surface = _Color.rgb;
                if (_ShowChecker > .5) surface = tex2D(_CheckerTex,i.uv).rgb;
                return float4(surface*light + sheen,1);
            }
            ENDCG
        }
    }
}
