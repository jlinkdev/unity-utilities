Shader "Hidden/jlinkdev/World Scanning/Fullscreen Scan"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "World Scan Composite"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "WorldScan.hlsl"

            float _WorldScanGlobalOpacity;

            float RawDepthAt(float2 uv)
            {
                return SampleSceneDepth(saturate(uv));
            }

            float LinearDepthAt(float2 uv)
            {
                return LinearEyeDepth(RawDepthAt(uv), _ZBufferParams);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float rawDepth = RawDepthAt(uv);

                #if UNITY_REVERSED_Z
                    if (rawDepth <= 0.00001)
                        return source;
                #else
                    if (rawDepth >= 0.99999)
                        return source;
                #endif

                float3 positionWS = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 normalWS = normalize(SampleSceneNormals(uv));
                WorldScanResult scan = WorldScanEvaluate(positionWS, normalWS);
                if (scan.coverage <= 0.0001 && scan.edgeInfluence <= 0.0001)
                    return source;

                float2 texel = _BlitTexture_TexelSize.xy * scan.edgeThickness;
                float centerDepth = LinearDepthAt(uv);
                float depthDelta = 0.0;
                depthDelta = max(depthDelta, abs(LinearDepthAt(uv + float2(texel.x, 0.0)) - centerDepth));
                depthDelta = max(depthDelta, abs(LinearDepthAt(uv - float2(texel.x, 0.0)) - centerDepth));
                depthDelta = max(depthDelta, abs(LinearDepthAt(uv + float2(0.0, texel.y)) - centerDepth));
                depthDelta = max(depthDelta, abs(LinearDepthAt(uv - float2(0.0, texel.y)) - centerDepth));

                float normalDelta = 0.0;
                normalDelta = max(normalDelta, 1.0 - dot(normalWS, normalize(SampleSceneNormals(uv + float2(texel.x, 0.0)))));
                normalDelta = max(normalDelta, 1.0 - dot(normalWS, normalize(SampleSceneNormals(uv + float2(0.0, texel.y)))));
                float depthEdge = smoothstep(scan.depthThreshold, scan.depthThreshold * 2.0, depthDelta);
                float normalEdge = smoothstep(scan.normalThreshold, scan.normalThreshold * 2.0, normalDelta);
                float edge = max(depthEdge, normalEdge) * scan.edgeInfluence;
                float3 scanColor = scan.color * (1.0 + edge);
                return half4(source.rgb + scanColor * _WorldScanGlobalOpacity, source.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
