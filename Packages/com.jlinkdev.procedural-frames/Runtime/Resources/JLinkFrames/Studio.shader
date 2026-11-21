Shader "JLink/Frames/Studio"
{
    Properties { _Color ("Finish", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            struct varyings { float4 pos : SV_POSITION; float3 n : TEXCOORD0; };
            varyings vert(appdata_base v)
            {
                varyings o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            float4 frag(varyings i) : SV_Target
            {
                float3 n = normalize(i.n);
                float key = saturate(dot(n, normalize(float3(-.5,.8,-.7))));
                float rim = pow(saturate(dot(n,normalize(float3(-.25,.4,-1)))),18);
                return float4(_Color.rgb*(.37+.60*key) + .075*rim,1);
            }
            ENDCG
        }
    }
}
