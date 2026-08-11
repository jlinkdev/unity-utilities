Shader "jlinkdev/Forcefields/Forcefield"
{
    Properties
    {
        [HDR] _SurfaceColor("Surface Color", Color) = (0.015, 0.32, 0.55, 1)
        _SurfaceIntensity("Surface Intensity", Float) = 0.8
        _Opacity("Opacity", Range(0, 1)) = 0.12
        _BackfaceOpacity("Backface Opacity", Range(0, 1)) = 0.35
        [HDR] _FresnelColor("Fresnel Color", Color) = (0.04, 1.2, 2.4, 1)
        _FresnelIntensity("Fresnel Intensity", Float) = 1.5
        _FresnelPower("Fresnel Power", Float) = 4
        [HDR] _PatternColor("Pattern Color", Color) = (0.03, 0.75, 1.7, 1)
        [HDR] _ImpactColor("Impact Color", Color) = (0.12, 2.2, 4, 1)
        [HDR] _IntersectionColor("Intersection Color", Color) = (0.08, 1.4, 2.8, 1)
        [HideInInspector] _ForcefieldIntensity("Forcefield Intensity", Float) = 1
        [HideInInspector] _ForcefieldPropagationMode("Propagation Mode", Float) = 1
        [HideInInspector] _ForcefieldSphereRadius("Sphere Radius", Float) = 1
        [HideInInspector] _RefractionEnabled("Refraction Enabled", Float) = 1
        [HideInInspector] _RefractionStrength("Refraction Strength", Float) = 0.018
        [HideInInspector] _ChromaticSplit("Chromatic Split", Float) = 0.0015
        [HideInInspector] _NoiseEnabled("Noise Enabled", Float) = 1
        [HideInInspector] _NoiseScale("Noise Scale", Float) = 2.5
        [HideInInspector] _NoiseVelocity("Noise Velocity", Vector) = (0.08, 0.04, -0.05, 0)
        [HideInInspector] _NoiseStrength("Noise Strength", Float) = 0.3
        [HideInInspector] _PulseSpeed("Pulse Speed", Float) = 0.65
        [HideInInspector] _PulseStrength("Pulse Strength", Float) = 0.08
        [HideInInspector] _PatternEnabled("Pattern Enabled", Float) = 1
        [HideInInspector] _PatternScale("Pattern Scale", Float) = 7
        [HideInInspector] _PatternWidth("Pattern Width", Float) = 0.045
        [HideInInspector] _PatternIntensity("Pattern Intensity", Float) = 0.35
        [HideInInspector] _ImpactIntensity("Impact Intensity", Float) = 2.5
        [HideInInspector] _RippleSpeed("Ripple Speed", Float) = 2.8
        [HideInInspector] _RippleWidth("Ripple Width", Float) = 0.12
        [HideInInspector] _RippleFadePower("Ripple Fade Power", Float) = 1.8
        [HideInInspector] _RippleRefraction("Ripple Refraction", Float) = 0.025
        [HideInInspector] _IntersectionEnabled("Intersection Enabled", Float) = 1
        [HideInInspector] _IntersectionIntensity("Intersection Intensity", Float) = 1.25
        [HideInInspector] _IntersectionWidth("Intersection Width", Float) = 0.18
        [HideInInspector] _Quality("Quality", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }

        Pass
        {
            Name "Forcefield"
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _STEREO_MULTIVIEW_ON _STEREO_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #define FORCEFIELD_MAX_IMPACTS 32

            CBUFFER_START(UnityPerMaterial)
                float4x4 _ForcefieldRootLocalToWorld;
                half4 _SurfaceColor;
                half4 _FresnelColor;
                half4 _PatternColor;
                half4 _ImpactColor;
                half4 _IntersectionColor;
                half4 _NoiseVelocity;
                half _SurfaceIntensity;
                half _Opacity;
                half _BackfaceOpacity;
                half _FresnelIntensity;
                half _FresnelPower;
                half _ForcefieldIntensity;
                half _ForcefieldPropagationMode;
                half _ForcefieldSphereRadius;
                half _RefractionEnabled;
                half _RefractionStrength;
                half _ChromaticSplit;
                half _NoiseEnabled;
                half _NoiseScale;
                half _NoiseStrength;
                half _PulseSpeed;
                half _PulseStrength;
                half _PatternEnabled;
                half _PatternScale;
                half _PatternWidth;
                half _PatternIntensity;
                half _ImpactIntensity;
                half _RippleSpeed;
                half _RippleWidth;
                half _RippleFadePower;
                half _RippleRefraction;
                half _IntersectionEnabled;
                half _IntersectionIntensity;
                half _IntersectionWidth;
                half _Quality;
                int _ForcefieldImpactCount;
            CBUFFER_END

            float4 _ForcefieldImpactPositionTime[FORCEFIELD_MAX_IMPACTS];
            float4 _ForcefieldImpactNormalStrength[FORCEFIELD_MAX_IMPACTS];
            float4 _ForcefieldImpactRadiusDuration[FORCEFIELD_MAX_IMPACTS];

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise3D(float3 p)
            {
                float3 cell = floor(p);
                float3 local = frac(p);
                local = local * local * (3.0 - 2.0 * local);

                float n000 = Hash31(cell + float3(0, 0, 0));
                float n100 = Hash31(cell + float3(1, 0, 0));
                float n010 = Hash31(cell + float3(0, 1, 0));
                float n110 = Hash31(cell + float3(1, 1, 0));
                float n001 = Hash31(cell + float3(0, 0, 1));
                float n101 = Hash31(cell + float3(1, 0, 1));
                float n011 = Hash31(cell + float3(0, 1, 1));
                float n111 = Hash31(cell + float3(1, 1, 1));

                float x00 = lerp(n000, n100, local.x);
                float x10 = lerp(n010, n110, local.x);
                float x01 = lerp(n001, n101, local.x);
                float x11 = lerp(n011, n111, local.x);
                return lerp(lerp(x00, x10, local.y), lerp(x01, x11, local.y), local.z);
            }

            float HexEdge(float2 uv, float width)
            {
                const float2 grid = float2(1.0, 1.7320508);
                float2 halfGrid = grid * 0.5;
                float2 a = frac(uv / grid) * grid - halfGrid;
                float2 b = frac((uv - halfGrid) / grid) * grid - halfGrid;
                float2 cell = dot(a, a) < dot(b, b) ? a : b;
                float distanceToCenter = max(abs(cell.x) * 0.8660254 + abs(cell.y) * 0.5, abs(cell.y));
                return smoothstep(0.5 - max(width, 0.001), 0.5, distanceToCenter);
            }

            float TriplanarPattern(float3 positionWS, float3 normalWS)
            {
                float3 weights = pow(abs(normalWS), 4.0);
                weights /= max(weights.x + weights.y + weights.z, 0.0001);
                float scale = max(_PatternScale, 0.1);
                float width = max(_PatternWidth, 0.001);
                float x = HexEdge(positionWS.zy * scale, width);
                float y = HexEdge(positionWS.xz * scale, width);
                float z = HexEdge(positionWS.xy * scale, width);
                return dot(float3(x, y, z), weights);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half face = IS_FRONT_VFACE(isFrontFace, 1.0h, 0.0h);
                float3 normalWS = normalize(IS_FRONT_VFACE(isFrontFace, input.normalWS, -input.normalWS));
                float3 viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                float fresnel = pow(saturate(1.0 - dot(normalWS, viewDirectionWS)), max(_FresnelPower, 0.25));

                float noise = 0.5;
                float secondaryNoise = 0.5;
                if (_Quality > 0.5 && _NoiseEnabled > 0.001)
                {
                    float3 noisePosition = input.positionWS * max(_NoiseScale, 0.01) + _NoiseVelocity.xyz * _Time.y;
                    noise = ValueNoise3D(noisePosition);
                    secondaryNoise = ValueNoise3D(noisePosition * 1.83 + 9.17);
                }
                float noiseSignal = lerp(0.5, noise, saturate(_NoiseEnabled) * step(0.5, _Quality));
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed * 6.2831853) * _PulseStrength;

                float ripple = 0.0;
                float impactCore = 0.0;
                float3 centerWS = mul(_ForcefieldRootLocalToWorld, float4(0, 0, 0, 1)).xyz;
                float3 fragmentRadial = SafeNormalize(input.positionWS - centerWS);

                [loop]
                for (int i = 0; i < FORCEFIELD_MAX_IMPACTS; i++)
                {
                    if (i >= _ForcefieldImpactCount)
                        break;

                    float4 positionTime = _ForcefieldImpactPositionTime[i];
                    float4 normalStrength = _ForcefieldImpactNormalStrength[i];
                    float4 radiusDuration = _ForcefieldImpactRadiusDuration[i];
                    float age = _Time.y - positionTime.w;
                    float duration = max(radiusDuration.y, 0.01);
                    float active = step(0.0, age) * (1.0 - step(duration, age));

                    float3 impactWS = mul(_ForcefieldRootLocalToWorld, float4(positionTime.xyz, 1)).xyz;
                    float surfaceDistance = distance(input.positionWS, impactWS);
                    if (_ForcefieldPropagationMode > 0.5)
                    {
                        float3 impactRadial = SafeNormalize(impactWS - centerWS);
                        float angularDistance = acos(clamp(dot(fragmentRadial, impactRadial), -1.0, 1.0));
                        surfaceDistance = angularDistance * max(_ForcefieldSphereRadius, 0.001);
                    }

                    float waveRadius = radiusDuration.x + age * _RippleSpeed;
                    float waveDistance = abs(surfaceDistance - waveRadius);
                    float ring = 1.0 - smoothstep(_RippleWidth, _RippleWidth * 1.75, waveDistance);
                    float envelope = pow(saturate(1.0 - age / duration), max(_RippleFadePower, 0.1));
                    float strength = normalStrength.w * active * envelope;
                    ripple += ring * strength;

                    float coreRadius = max(radiusDuration.x + _RippleWidth * 1.5, 0.01);
                    float core = 1.0 - smoothstep(0.0, coreRadius, surfaceDistance);
                    impactCore += core * strength * saturate(1.0 - age / max(duration * 0.3, 0.01));
                }

                float pattern = 0.0;
                if (_Quality > 0.5 && _PatternEnabled > 0.001)
                    pattern = TriplanarPattern(input.positionWS, normalWS) * _PatternEnabled;

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float3 normalVS = TransformWorldToViewDir(normalWS, true);
                float2 turbulence = float2(noise - 0.5, secondaryNoise - 0.5);
                float distortionStrength = _RefractionStrength * (0.3 + noiseSignal * _NoiseStrength);
                distortionStrength += ripple * _RippleRefraction;
                float2 distortion = (normalVS.xy + turbulence * 0.65) * distortionStrength;

                float opaqueTextureAvailable = step(2.0, max(_CameraOpaqueTexture_TexelSize.z, _CameraOpaqueTexture_TexelSize.w));
                float refractionAvailable = saturate(_RefractionEnabled) * step(0.5, _Quality) * opaqueTextureAvailable;
                float3 refractedScene = 0.0;
                if (refractionAvailable > 0.001)
                {
                    float2 refractedUV = clamp(screenUV + distortion, 0.001, 0.999);
                    refractedScene = SampleSceneColor(refractedUV);
                    if (_Quality > 1.5 && _ChromaticSplit > 0.00001)
                    {
                        float2 splitDirection = distortion + float2(0.0001, 0.0);
                        splitDirection *= rsqrt(max(dot(splitDirection, splitDirection), 0.00000001));
                        float2 split = splitDirection * _ChromaticSplit;
                        refractedScene.r = SampleSceneColor(clamp(refractedUV + split, 0.001, 0.999)).r;
                        refractedScene.b = SampleSceneColor(clamp(refractedUV - split, 0.001, 0.999)).b;
                    }
                }

                float intersection = 0.0;
                float depthTextureAvailable = step(2.0, max(_CameraDepthTexture_TexelSize.z, _CameraDepthTexture_TexelSize.w));
                if (_Quality > 0.5 && _IntersectionEnabled > 0.001 && depthTextureAvailable > 0.5)
                {
                    float rawSceneDepth = SampleSceneDepth(screenUV);
                    float sceneDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                    float surfaceDepth = -TransformWorldToView(input.positionWS).z;
                    float separation = max(sceneDepth - surfaceDepth, 0.0);
                    intersection = (1.0 - smoothstep(0.0, max(_IntersectionWidth, 0.001), separation));
                    intersection *= step(surfaceDepth, sceneDepth + 0.001) * _IntersectionEnabled;
                }

                float backfaceFactor = lerp(max(_BackfaceOpacity, 0.0), 1.0, face);
                float baseAlpha = saturate((_Opacity + fresnel * 0.3 + ripple * 0.18 + intersection * 0.2) * _ForcefieldIntensity);
                baseAlpha *= backfaceFactor;

                float3 energy = _SurfaceColor.rgb * _SurfaceIntensity * (0.45 + noiseSignal * _NoiseStrength) * pulse;
                energy += _FresnelColor.rgb * fresnel * _FresnelIntensity;
                energy += _PatternColor.rgb * pattern * _PatternIntensity * (0.35 + fresnel);
                energy += _ImpactColor.rgb * (ripple + impactCore * 1.35) * _ImpactIntensity;
                energy += _IntersectionColor.rgb * intersection * _IntersectionIntensity;
                energy *= _ForcefieldIntensity;

                float3 premultipliedColor = energy * baseAlpha;
                premultipliedColor += refractedScene * baseAlpha * refractionAvailable;
                return half4(premultipliedColor, baseAlpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
