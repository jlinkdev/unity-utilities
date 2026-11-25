Shader "jlinkdev/EditableSamples/Texture Atlas/Display Field"
{
    Properties
    {
        _MainTex ("Scalar or color texture", 2D) = "white" {}
        _Palette ("Color lookup strip", 2D) = "white" {}
        [Toggle] _UsePalette ("Look up scalar in palette", Float) = 0
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _Palette;
            float4 _MainTex_ST, _Palette_TexelSize;
            float _UsePalette;
            struct Varyings { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(appdata_base v)
            {
                Varyings o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                return o;
            }
            float4 frag(Varyings i) : SV_Target
            {
                float4 field = tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));
                if (_UsePalette > 0.5)
                {
                    float u = lerp(0.5 * _Palette_TexelSize.x, 1 - 0.5 * _Palette_TexelSize.x, field.r);
                    field = tex2D(_Palette, float2(u, 0.5));
                }
                // Composite alpha over checks so a white particle with varying opacity is visible.
                float2 cell = floor(i.uv * 12);
                float backing = lerp(0.09, 0.17, fmod(cell.x + cell.y, 2));
                return float4(lerp(backing.xxx, field.rgb, field.a), 1);
            }
            ENDCG
        }
    }
}
