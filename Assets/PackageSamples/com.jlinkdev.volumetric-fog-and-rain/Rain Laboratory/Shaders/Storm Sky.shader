Shader "jlinkdev/Volumetric Fog and Rain/EditableSamples/Storm Sky"
{
    Properties
    {
        [NoScaleOffset] _Sky ("Overcast HDR panorama", Cube) = "grey" {}
        _Tint ("Storm tint", Color) = (.52,.63,.8,1)
        _Exposure ("Exposure multiplier", Range(.01,4)) = .52
        _Contrast ("Cloud contrast", Range(.25,2)) = 1.1
        _Rotation ("Rotation around up (degrees)", Range(0,360)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            samplerCUBE _Sky;
            half4 _Sky_HDR, _Tint;
            float _Exposure, _Contrast, _Rotation;
            struct Interpolators { float4 clip : SV_POSITION; float3 direction : TEXCOORD0; };
            Interpolators Vert(float4 vertex : POSITION)
            {
                Interpolators o;
                o.clip = UnityObjectToClipPos(vertex);
                float s, c;
                sincos(radians(_Rotation), s, c);
                o.direction = float3(vertex.x*c - vertex.z*s, vertex.y, vertex.x*s + vertex.z*c);
                return o;
            }
            half4 Frag(Interpolators i) : SV_Target
            {
                float3 sky = DecodeHDR(texCUBE(_Sky, normalize(i.direction)), _Sky_HDR);
                float luminance = max(dot(sky, float3(.2126,.7152,.0722)), .0001);
                // Keep the photographed cloud detail and shift the overcast daylight into storm light.
                float3 graded = lerp(luminance.xxx, sky, .25) * pow(luminance, _Contrast-1);
                return half4(graded * _Tint.rgb * _Exposure, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
