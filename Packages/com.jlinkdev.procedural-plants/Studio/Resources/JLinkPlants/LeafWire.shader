Shader "Hidden/JLink/Plants/Leaf Wire"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 vert(float4 vertex:POSITION):SV_POSITION
            {
                float4 clipPos = UnityObjectToClipPos(vertex);
                #if defined(UNITY_REVERSED_Z)
                clipPos.z += .000012 * clipPos.w;
                #else
                clipPos.z -= .000012 * clipPos.w;
                #endif
                return clipPos;
            }
            fixed4 frag():SV_Target { return fixed4(.66,.92,.54,.78); }
            ENDCG
        }
    }
}
