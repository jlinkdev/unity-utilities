Shader "jlinkdev/Samples/Studio Sky"
{
    Properties { _Zenith ("Zenith", Color) = (.068,.09,.13,1) }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Studio Atmosphere.hlsl"
            half4 _Zenith;
            struct Varyings { float4 clip : SV_POSITION; float3 direction : TEXCOORD0; };
            Varyings vert(float4 vertex : POSITION)
            {
                Varyings o;
                o.clip = UnityObjectToClipPos(vertex);
                o.direction = vertex.xyz;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float altitude = max(normalize(i.direction).y, 0);
                return half4(lerp(StudioHorizonColor(),_Zenith.rgb,1-exp(-altitude*5)),1);
            }
            ENDCG
        }
    }
}
