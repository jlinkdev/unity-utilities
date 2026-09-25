Shader "Hidden/jlinkdev/Volumetric Rain"
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
            #pragma multi_compile_local _ _RAIN_VOLUMES
            #pragma multi_compile_local _ _RAIN_ONLY _FOG_ONLY
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "RainField.hlsl"
            #include "RainVolumes.hlsl"

            float4 _FogColor, _FogAppearance;

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
                #if !defined(_FOG_ONLY)
                // Derivatives are evaluated before divergent traversal, including orthographic origins.
                float footprintOrigin = max(length(ddx(origin)), length(ddy(origin))) * 0.5;
                float footprintSlope = max(length(ddx(ray)), length(ddy(ray))) * 0.5;
                #endif
                #if defined(_RAIN_VOLUMES)
                    float2 wetIntervals[RAIN_MAX_INTERVALS];
                    int wetCount = RainWetIntervals(origin, ray, end, wetIntervals);
                    if (wetCount == 0) return _RainDebug == 0 ? source : half4(0,0,0,1);
                    float reservedVisits = 6.0 + 2.0 * max(0, wetCount - 1);
                #else
                    float reservedVisits = 6.0;
                #endif
                float cost = 0, streaks = 0;
                #if !defined(_FOG_ONLY)
                float3 localOrigin = RainToLocal(origin);
                float3 localRay = RainToLocal(ray);
                float3 cellSize = _RainField.x * float3(1, 3, 1);
                // Worst-case crossing bound (sum of reciprocal cell extents along ray).
                // Reserve six visits for boundary ties; fade before the cap can truncate streaks.
                float budgetRange = max(1.0, _RainSteps - reservedVisits) / max(dot(abs(localRay), rcp(cellSize)), 0.0001);
                float streakEnd = min(_RainDistances.z, budgetRange);
                // Honor the authored mid distance when the full range fits the budget.
                // If the budget shortens the range, preserve the authored transition proportions.
                float mid = _RainDistances.y * (streakEnd / _RainDistances.z);


                [unroll] for (int layer = 0; layer < 2; layer++)
                {
                    int remaining = _RainSteps;
                    #if defined(_RAIN_VOLUMES)
                        [loop] for (int s = 0; s < wetCount; s++)
                            streaks += RainTraverse(localOrigin, localRay, origin, ray, wetIntervals[s].y, mid, streakEnd,
                                footprintOrigin, footprintSlope, layer, wetIntervals[s].x, remaining, cost);
                    #else
                        streaks += RainTraverse(localOrigin, localRay, origin, ray, end, mid, streakEnd,
                            footprintOrigin, footprintSlope, layer, 0, remaining, cost);
                    #endif
                }

                #else
                    float mid = 0, streakEnd = 0;
                #endif

                // Only broad density is quadrature-sampled. Individual streaks are analytic.
                float haze = 0;
                #if !defined(_RAIN_ONLY)
                #if defined(_RAIN_VOLUMES)
                    float totalWetLength = 0;
                    [loop] for(int a=0;a<wetCount;a++)
                        totalWetLength += max(0,wetIntervals[a].y - max(mid,wetIntervals[a].x));
                    [branch] if (_FogAppearance.y > 0 && totalWetLength > 0)
                    [loop] for(int b=0;b<wetCount;b++)
                    {
                        float start = max(mid,wetIntervals[b].x), stop = wetIntervals[b].y;
                        float span = stop-start;
                        if(span<=0)continue;
                        int samples = max(1,(int)ceil(_RainHazeSteps * span / totalWetLength));
                        float dt = span/samples;
                        [loop] for(int k=0;k<samples;k++)
                        {
                            float t=start+(k+0.5)*dt;
                            float transition = 1;
                            #if !defined(_FOG_ONLY)
                                transition=smoothstep(mid,max(mid+0.001,streakEnd),t);
                            #endif
                            haze += RainDensity(origin+ray*t)*transition*dt;
                        }
                    }
                #else
                    // Preserve the original sample positions in the unbounded quality path.
                    float dt = end / _RainHazeSteps;
                    [branch] if (_FogAppearance.y > 0 && end > mid)
                    [loop] for (int k = 0; k < _RainHazeSteps; k++)
                    {
                        float t = (k + 0.5) * dt;
                        float transition = 1;
                        #if !defined(_FOG_ONLY)
                            transition = smoothstep(mid, max(mid + 0.001, streakEnd), t);
                        #endif
                        [branch] if (transition > 0)
                            haze += RainDensity(origin + ray * t) * transition * dt;
                    }
                #endif
                #endif
                float hazeTau = haze * _FogAppearance.w * _FogAppearance.y;
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
                float3 fogLight = _FogColor.rgb * _FogAppearance.x;
                float3 result = source.rgb * hazeTransmission + fogLight * _FogAppearance.z * (1 - hazeTransmission);
                result = result * (1 - streakAlpha) + rainLight * streakAlpha;
                return half4(result, source.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
