Shader "jlinkdev/Volumetric Fog and Rain/EditableSamples/Dawn Sky"
{
    Properties
    {
        [HDR] _Horizon ("Cool horizon", Color) = (.4,.45,.62,1)
        [HDR] _Zenith ("Upper sky", Color) = (.029,.053,.119,1)
        [HDR] _Dawn ("Dawn glow", Color) = (1.27,.67,.40,1)
        _Azimuth ("Glow azimuth (degrees from +Z)", Range(-180,180)) = -52
        _Elevation ("Glow elevation (degrees)", Range(-6,20)) = 5.2
        _Veil ("High cloud veil", Range(0,1)) = .75
        _Exposure ("Exposure multiplier", Range(.1,4)) = 1
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
            float4 _Horizon, _Zenith, _Dawn;
            float _Azimuth, _Elevation, _Veil, _Exposure;
            struct Varyings { float4 clip : SV_POSITION; float3 direction : TEXCOORD0; };
            Varyings Vert(float4 vertex : POSITION)
            {
                Varyings output;
                output.clip = UnityObjectToClipPos(vertex);
                output.direction = vertex.xyz;
                return output;
            }

            float Hash(float2 p)
            {
                float3 h = frac(p.xyx * float3(.1031,.1030,.0973));
                h += dot(h,h.yzx+33.33);
                return frac((h.x+h.y)*h.z);
            }
            float Noise(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                f = f*f*(3-2*f);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),
                            lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.direction);
                float alt = asin(clamp(d.y,-1,1));
                float az = radians(_Azimuth);
                float2 sunBearing = float2(sin(az),cos(az));
                float angularDistance = acos(clamp(dot(d.xz,sunBearing)/max(length(d.xz),.0001),-1,1));
                float height = max(alt,0);
                float3 color = lerp(_Horizon.rgb,_Zenith.rgb,1-exp(-height*5.5));

                // A thin, backlit cloud veil varies the broad aerosol glow without a horizon seam.
                float2 p = float2(dot(d.xz,float2(.77,.64))*3.5 + d.y*2, d.y*19 + dot(d.xz,float2(-.64,.77)));
                p += float2(Noise(p*.78+3.1),Noise(p*.64+5.7))*.85;
                float cloud = .55*Noise(p+11) + .32*Noise(p*2.31+3.7) + .13*Noise(p*5.07);
                float delta = (alt-radians(_Elevation))/radians(5.2);
                float along = angularDistance/radians(16.5);
                float glow = exp(-.5*(along*along + delta*delta));
                color = lerp(color,_Dawn.rgb,glow * (1-_Veil*(1-cloud)*.32));
                color *= 1 + (cloud-.5)*_Veil*.69*exp(-abs(alt)*1.5);
                // The brightest part of the veiled sunrise lies low behind the courtyard wall.
                float solarHeight = (alt-radians(.6))/radians(.85);
                float core = exp(-.5*(along*along*16 + solarHeight*solarHeight));
                color += _Dawn.rgb * core * 1.5;
                return half4(max(color,0)*_Exposure,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
