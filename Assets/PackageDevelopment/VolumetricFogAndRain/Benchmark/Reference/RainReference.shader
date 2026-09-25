Shader "Hidden/jlinkdev/Volumetric Fog and Rain Reference"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Volumetric Rain"
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "RainField.hlsl"

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float depth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    float nearDepth = 1, farDepth = 0;
                #else
                    float nearDepth = UNITY_NEAR_CLIP_VALUE, farDepth = 1;
                    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                #endif
                float3 origin = ComputeWorldSpacePosition(uv, nearDepth, UNITY_MATRIX_I_VP);
                float3 farPoint = ComputeWorldSpacePosition(uv, farDepth, UNITY_MATRIX_I_VP);
                float3 scene = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float3 ray = normalize(farPoint - origin);
                float end = min(_RainDistances.w, max(0, dot(scene - origin, ray)));
                // Derivatives are evaluated before divergent traversal, including orthographic origins.
                float footprintOrigin = max(length(ddx(origin)), length(ddy(origin))) * 0.5;
                float footprintSlope = max(length(ddx(ray)), length(ddy(ray))) * 0.5;
                float3 localOrigin = RainToLocal(origin);
                float3 localRay = RainToLocal(ray);
                float3 cellSize = _RainField.x * float3(1, 3, 1);
                // Worst-case crossing bound (sum of reciprocal cell extents along ray).
                // Reserve six visits for boundary ties; fade before the cap can truncate streaks.
                float budgetRange = (_RainSteps - 6.0) / max(dot(abs(localRay), rcp(cellSize)), 0.0001);
                float streakEnd = min(_RainDistances.z, budgetRange);
                float mid = min(_RainDistances.y, streakEnd * 0.65);
                float cost = 0;
                float streaks = 0;
                [unroll] for (int layer = 0; layer < 2; layer++)
                    streaks += RainTraverse(localOrigin, localRay, origin, ray, end, mid, streakEnd,
                        footprintOrigin, footprintSlope, layer, cost);

                // Only broad density is quadrature-sampled. Individual streaks are analytic.
                float haze = 0;
                float dt = end / _RainHazeSteps;
                [loop] for (int k = 0; k < _RainHazeSteps; k++)
                {
                    float t = (k + 0.5) * dt;
                    float transition = smoothstep(mid, max(mid + 0.001, streakEnd), t);
                    haze += RainDensity(origin + ray * t) * transition * dt;
                }
                float hazeTau = haze * _RainField.w * _RainAppearance.z;
                float hazeTransmission = exp(-hazeTau);
                float streakAlpha = 1 - exp(-streaks * _RainAppearance.y);
                if (_RainDebug == 1) return half4(streakAlpha.xxx, 1);
                if (_RainDebug == 2) return half4((1 - hazeTransmission).xxx, 1);
                if (_RainDebug == 3)
                {
                    float work = cost / (2.0 * _RainSteps);
                    return half4(work, work * work, 1 - work, 1);
                }
                float3 rainLight = _RainColor.rgb * _RainAppearance.x;
                float3 result = source.rgb * hazeTransmission + rainLight * _RainAppearance.w * (1 - hazeTransmission);
                result = result * (1 - streakAlpha) + rainLight * streakAlpha;
                return half4(result, source.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
