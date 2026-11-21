Shader "Hidden/jlinkdev/ObjectHighlight/Filter"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        HLSLINCLUDE
        #pragma target 3.5
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_JlhSource);
        TEXTURE2D(_JlhDistance);
        TEXTURE2D(_JlhGlow);
        float4 _JlhParameters, _JlhOutline, _JlhHalo;

        struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
        Varyings Vert(uint id : SV_VertexID)
        {
            Varyings o;
            o.position = GetFullScreenTriangleVertexPosition(id);
            o.uv = GetFullScreenTriangleTexCoord(id);
            return o;
        }

        float Coverage(float2 uv)
        {
            return SAMPLE_TEXTURE2D_LOD(_JlhSource, sampler_LinearClamp, uv, 0).r;
        }

        float HorizontalDistance(Varyings i) : SV_Target
        {
            if (Coverage(i.uv) >= 0.5) return 0;
            int limit = (int)ceil(_JlhParameters.z);
            for (int x = 1; x <= limit; x++)
            {
                float2 offset = _JlhParameters.xy * x;
                if (max(Coverage(i.uv + offset), Coverage(i.uv - offset)) >= 0.5)
                    return x / 16.0;
            }
            return 1;
        }

        float Downsample(Varyings i) : SV_Target
        {
            float2 d = _JlhParameters.xy;
            return 0.25 * (Coverage(i.uv + d) + Coverage(i.uv - d)
                + Coverage(i.uv + float2(d.x, -d.y)) + Coverage(i.uv + float2(-d.x, d.y)));
        }

        float Gaussian(Varyings i) : SV_Target
        {
            float sum = Coverage(i.uv), total = 1;
            float weight = 1, ratio = _JlhParameters.w;
            float ratioStep = ratio * ratio;
            // Combine adjacent Gaussian taps in one bilinear fetch on each side.
            for (int tap = 1; tap <= (int)_JlhParameters.z; tap += 2)
            {
                float first = weight * ratio;
                ratio *= ratioStep;
                weight = first * ratio;
                ratio *= ratioStep;
                float second = tap + 1 <= (int)_JlhParameters.z ? weight : 0;
                float pair = first + second;
                float2 offset = _JlhParameters.xy * (tap + second / pair);
                sum += pair * (Coverage(i.uv + offset) + Coverage(i.uv - offset));
                total += 2 * pair;
            }
            return sum / total;
        }

        float4 Composite(Varyings i) : SV_Target
        {
            float outside = 1 - Coverage(i.uv);
            if (outside <= 0) return 0;
            float opacity = 0;
            if (_JlhParameters.z > 0)
            {
                int limit = (int)ceil(_JlhParameters.z);
                float nearestSquared = 256;
                for (int y = -limit; y <= limit; y++)
                {
                    float dx = round(16 * SAMPLE_TEXTURE2D_LOD(_JlhDistance, sampler_PointClamp,
                        i.uv + _JlhParameters.xy * y, 0).r);
                    nearestSquared = min(nearestSquared, dx * dx + y * y);
                }
                opacity = saturate(_JlhParameters.z + 0.5 - sqrt(nearestSquared)) * _JlhOutline.a;
            }
            float halo = 0;
            if (_JlhParameters.w > 0)
                halo = SAMPLE_TEXTURE2D_LOD(_JlhGlow, sampler_LinearClamp, i.uv, 0).r * _JlhParameters.w * _JlhHalo.a;
            return float4((_JlhOutline.rgb * opacity + _JlhHalo.rgb * halo * (1 - opacity)) * outside, opacity * outside);
        }
        ENDHLSL

        Pass
        {
            Name "Horizontal distance"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment HorizontalDistance
            ENDHLSL
        }
        Pass
        {
            Name "Downsample coverage"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Downsample
            ENDHLSL
        }
        Pass
        {
            Name "Gaussian blur"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Gaussian
            ENDHLSL
        }
        Pass
        {
            Name "Exterior composite"
            Blend One OneMinusSrcAlpha
            ColorMask RGB
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Composite
            ENDHLSL
        }
    }
}
