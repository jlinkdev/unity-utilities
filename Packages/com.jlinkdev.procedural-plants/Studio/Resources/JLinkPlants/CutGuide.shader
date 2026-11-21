Shader "Hidden/JLink/Plants/Cut Guide"
{
    SubShader
    {
        Tags { "Queue"="Overlay" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Output { float4 pos:SV_POSITION; fixed4 color:COLOR; };
            Output vert(float4 vertex:POSITION,fixed4 color:COLOR)
            { Output o; o.pos=UnityObjectToClipPos(vertex); o.color=color; return o; }
            fixed4 frag(Output i):SV_Target { return i.color; }
            ENDCG
        }
    }
}
