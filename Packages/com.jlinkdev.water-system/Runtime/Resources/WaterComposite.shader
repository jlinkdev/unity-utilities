Shader "Hidden/jlinkdev/Water System/Composite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Near Plane Closure And Underwater"
            ZWrite Off
            ZTest Always
            Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
            #include "../Shaders/WaterCommon.hlsl"

            TEXTURE2D_X(_WaterSource);
            TEXTURE2D_X_FLOAT(_WaterSceneDepth);
            TEXTURE2D_X_FLOAT(_WaterBoundary);
            int _WaterDebugMode;

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float3 NearPosition(float2 uv)
            {
                return ComputeWorldSpacePosition(uv, UNITY_NEAR_CLIP_VALUE, UNITY_MATRIX_I_VP);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                half4 source = SAMPLE_TEXTURE2D_X(_WaterSource, sampler_LinearClamp, uv);
                float3 nearPosition = NearPosition(uv);
                // A point halfway through device depth is well-conditioned for both perspective and orthographic cameras.
                float3 midPosition = ComputeWorldSpacePosition(uv, .5, UNITY_MATRIX_I_VP);
                float3 ray = normalize(midPosition - nearPosition);
                float heightDistance = WaterMeshHeight(nearPosition.xz) - nearPosition.y;
                float domainDistance = WaterDomainDistance(nearPosition);
                float signedDistance = min(heightDistance, domainDistance);
                float wet = saturate(.5 + signedDistance / max(fwidth(signedDistance), 0.00001));
                float heightPixelSize = max(fwidth(heightDistance), 0.00001);
                float domainCoverage = saturate(.5 + domainDistance / max(fwidth(domainDistance), 0.00001));
                float band = (1 - smoothstep(max(0, _WaterlineWidth - .5), _WaterlineWidth + .5,
                              abs(heightDistance) / heightPixelSize)) * domainCoverage;
                band *= _WaterlineWidth > 0 ? saturate(_WaterlineStrength) : 0;
                float signedBoundaryDepth = SAMPLE_TEXTURE2D_X(_WaterBoundary, sampler_PointClamp, uv).r;
                float2 boundary = float2(abs(signedBoundaryDepth), sign(signedBoundaryDepth));

                if (_WaterDebugMode == 1) return half4(wet.xxx, source.a);
                if (_WaterDebugMode == 2)
                {
                    float depthView = boundary.x / (boundary.x + 10);
                    return half4(boundary.y > 0 ? float3(0, depthView, depthView) : float3(depthView, 0, depthView), source.a);
                }
                if (wet <= 0 && _WaterDebugMode == 0)
                    return half4(lerp(source.rgb, _WaterlineColor.rgb, band * _WaterlineColor.a), source.a);

                float rawSceneDepth = SAMPLE_TEXTURE2D_X(_WaterSceneDepth, sampler_PointClamp, uv).r;
                bool sky = WaterIsSky(rawSceneDepth);
                float3 scenePosition = WaterWorldPosition(uv, rawSceneDepth);
                float sceneDistance = sky ? 1e7 : max(0, dot(scenePosition - nearPosition, ray));
                float exitDistance = WaterVolumeExit(nearPosition, ray);
                float surfaceDistance = 1e7;
                if (boundary.x > 0)
                {
                    float nearEye = -TransformWorldToView(nearPosition).z;
                    float eyeToRay = max(0.0001, -TransformWorldToViewDir(ray, false).z);
                    surfaceDistance = max(0, (boundary.x - nearEye) / eyeToRay);
                    exitDistance = min(exitDistance, surfaceDistance);
                }
                float path = max(0, min(sceneDistance, exitDistance));
                if (_WaterDebugMode == 3) return half4((1 - exp(-path * .1)).xxx * wet, source.a);

                float caustics = 0;
                if (_WaterCausticsStrength > 0 && !sky && sceneDistance < min(exitDistance, surfaceDistance) + .02 &&
                    WaterDomainDistance(scenePosition) > 0 && WaterMeshHeight(scenePosition.xz) > scenePosition.y)
                {
                    float3 geometricNormal = normalize(cross(ddy(scenePosition), ddx(scenePosition)));
                    caustics = WaterCaustics(scenePosition) * saturate(abs(geometricNormal.y));
                }
                if (_WaterDebugMode == 4) return half4(caustics.xxx * wet, source.a);

                float edgeFade = saturate(heightDistance / heightPixelSize / 6) * saturate(domainDistance / heightPixelSize / 6);
                float2 distortion = float2(sin(uv.y * 55 + _WaterTime * 1.7), cos(uv.x * 48 - _WaterTime * 1.3));
                distortion *= _WaterDistortion * edgeFade * saturate(path);
                float2 displacedUV = saturate(uv + distortion);
                // Never pull an above-water pixel across the closure with screen distortion.
                if (_WaterDistortion > 0 && edgeFade > 0)
                {
                    float3 displacedNear = NearPosition(displacedUV);
                    float displacedWet = min(WaterMeshHeight(displacedNear.xz) - displacedNear.y, WaterDomainDistance(displacedNear));
                    if (displacedWet <= 0) displacedUV = uv;
                }
                half3 color = SAMPLE_TEXTURE2D_X(_WaterSource, sampler_LinearClamp, displacedUV).rgb;
                color += caustics * _WaterShallowColor.rgb;
                float luminance = dot(color, float3(.2126, .7152, .0722));
                color = lerp(luminance.xxx, color, _WaterUnderwaterSaturation);
                float3 transmission = exp(-max(0, _WaterAbsorption.rgb + _WaterDensity) * path);
                color = color * transmission + _WaterFogColor.rgb * (1 - transmission);
                color = lerp(source.rgb, color, wet);
                color = lerp(color, _WaterlineColor.rgb, band * _WaterlineColor.a);
                return half4(color, source.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
