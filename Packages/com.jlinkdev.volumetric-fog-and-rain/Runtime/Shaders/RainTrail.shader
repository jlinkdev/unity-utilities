Shader "jlinkdev/Volumetric Fog and Rain/Rain Trail"
{
 Properties { _Color("Tint", Color)=(.65,.78,.85,.35) _Impact("Impact ring", Float)=0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
  Pass
  {
   Tags { "LightMode"="SRPDefaultUnlit" }
   Blend One OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Color; float _Impact;
   CBUFFER_END
   struct A {float4 p:POSITION; half4 c:COLOR; float2 uv:TEXCOORD0;};
   struct V {float4 p:SV_POSITION; half4 c:COLOR; float2 uv:TEXCOORD0;};
   V Vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.c=a.c*_Color;o.uv=a.uv;return o;}
   half4 Frag(V i):SV_Target
   {
    float edge=saturate(1-abs(i.uv.y*2-1)); edge=smoothstep(0,.5,edge);
    float radius=length(i.uv*2-1);
    float ring=(1-smoothstep(.08,.24,abs(radius-.65)))*(1-smoothstep(.9,1,radius));
    float a=i.c.a*lerp(edge,ring,_Impact);
    return half4(i.c.rgb*a,a);
   }
   ENDHLSL
  }
 }
}
