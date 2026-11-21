Shader "Hidden/jlinkdev/Blueprint/Composite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_JlbLayer0);
            TEXTURE2D(_JlbLayer1);
            TEXTURE2D(_JlbLayer2);
            TEXTURE2D(_JlbLayer3);
            float4 _JlbStyle, _JlbPaper, _JlbInk;
            float4 Vert(uint id : SV_VertexID) : SV_POSITION { return GetFullScreenTriangleVertexPosition(id); }

            float Edge(Texture2D layer, int2 p, int2 limit)
            {
                float4 c = layer.Load(int3(p, 0));
                if (c.w <= 0) return 0;
                int w = (int)_JlbStyle.z;
                float4 a = layer.Load(int3(max(p - int2(w,0), 0), 0));
                float4 b = layer.Load(int3(min(p + int2(w,0), limit), 0));
                float4 d = layer.Load(int3(max(p - int2(0,w), 0), 0));
                float4 e = layer.Load(int3(min(p + int2(0,w), limit), 0));
                if (min(min(a.w,b.w), min(d.w,e.w)) <= 0) return 1;
                float cosine = min(min(dot(c.xyz,a.xyz),dot(c.xyz,b.xyz)), min(dot(c.xyz,d.xyz),dot(c.xyz,e.xyz)));
                float softness = min(0.025, (1 - _JlbStyle.w) * 0.75);
                float crease = 1 - smoothstep(_JlbStyle.w - softness, _JlbStyle.w + softness, cosine);
                // A second difference suppresses the constant depth slope of a flat face.
                float jump = max(abs(a.w + b.w - 2*c.w), abs(d.w + e.w - 2*c.w)) / max(c.w, 1e-6);
                return max(crease, smoothstep(0.0015*w, 0.004*w, jump));
            }

            float4 Frag(float4 position : SV_POSITION) : SV_Target
            {
                uint width, height;
                _JlbLayer0.GetDimensions(width, height);
                int2 limit = int2(width, height) - 1;
                int2 p = clamp(int2(position.xy), 0, limit);
                float ink = Edge(_JlbLayer0, p, limit);
                float hidden = _JlbStyle.y;
                if (_JlbStyle.x >= 2) ink = max(ink, hidden * Edge(_JlbLayer1, p, limit));
                if (_JlbStyle.x >= 3) ink = max(ink, hidden * 0.65 * Edge(_JlbLayer2, p, limit));
                if (_JlbStyle.x >= 4) ink = max(ink, hidden * 0.42 * Edge(_JlbLayer3, p, limit));
                return float4(lerp(_JlbPaper.rgb, _JlbInk.rgb, ink), 1);
            }
            ENDHLSL
        }
    }
}
