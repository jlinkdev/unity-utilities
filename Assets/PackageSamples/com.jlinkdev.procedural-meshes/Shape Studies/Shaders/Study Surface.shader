Shader "jlinkdev/EditableSamples/Shape Studies/Study Surface"
{
    Properties
    {
        _Tint ("Surface", Color) = (0.18,0.62,0.67,1)
        [Toggle] _Height ("Visualize height from vertex R", Float) = 0
        [Toggle] _Grid ("Show UV grid", Float) = 0
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Tint;
            float _Height, _Grid;
            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float height : TEXCOORD2;
            };
            Varyings vert(appdata_full v)
            {
                Varyings o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.height = v.color.r;
                return o;
            }
            float4 frag(Varyings i) : SV_Target
            {
                float3 tint = _Tint.rgb;
                if (_Height > 0.5)
                    tint = lerp(float3(0.04, 0.13, 0.32), float3(1, 0.65, 0.13), i.height);
                if (_Grid > 0.5)
                {
                    float2 uv = i.uv * 8;
                    float2 edge = abs(frac(uv + 0.5) - 0.5) / max(fwidth(uv), 0.0001);
                    tint *= lerp(0.5, 1, saturate(min(edge.x, edge.y)));
                }
                float light = saturate(dot(normalize(i.worldNormal), normalize(float3(-0.4, 0.75, -0.55))));
                return float4(tint * (0.40 + 0.60 * light), 1);
            }
            ENDCG
        }
    }
}
