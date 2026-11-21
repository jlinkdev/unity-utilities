Shader "jlinkdev/Beams/Energy Beam"
{
    Properties
    {
        [HDR] _BaseColor("Base Color", Color) = (0.05, 0.65, 1.5, 1)
        _Opacity("Opacity", Range(0, 1)) = 0.8
        _CoreWidth("Core Width", Range(0.01, 1)) = 0.22
        _CoreIntensity("Core Intensity", Range(0, 10)) = 2.5
        _HaloIntensity("Halo Intensity", Range(0, 10)) = 0.8
        _EdgePower("Edge Power", Range(0.1, 8)) = 1.8
        _VertexAmplitude("Vertex Amplitude", Range(0, 1)) = 0.06
        _VertexFrequency("Vertex Frequency", Range(0.01, 20)) = 2.4
        _VertexSpeed("Vertex Speed", Range(-10, 10)) = 1.2
        _EndpointFalloff("Endpoint Falloff", Range(0.001, 0.5)) = 0.08
        _FlowSpeed("Flow Speed", Range(-20, 20)) = 3
        _FlowFrequency("Flow Frequency", Range(0.01, 20)) = 1.5
        _FlowStrength("Flow Strength", Range(0, 2)) = 0.35
        _FlickerRate("Flicker Rate", Range(0, 60)) = 0
        _FlickerStrength("Flicker Strength", Range(0, 1)) = 0
        _PulseWidth("Pulse Width", Range(0.001, 1)) = 0.08
        _Filament("Spiral Filament", Range(0, 3)) = 0
        [HideInInspector] _BeamTube("Tube Geometry", Float) = 0
        _PulseIntensity("Pulse Intensity", Range(0, 10)) = 0
        _EndpointOpacityFalloff("Endpoint Opacity Falloff", Range(0.001, 0.5)) = 0.01
        [HideInInspector] _BeamColor("Beam Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _BeamIntensity("Beam Intensity", Float) = 1
        [HideInInspector] _BeamLength("Beam Length", Float) = 1
        [HideInInspector] _BeamTime("Beam Time", Float) = 0
        [HideInInspector] _BeamAge("Beam Age", Float) = 0
        [HideInInspector] _BeamSeed("Beam Seed", Float) = 1
        [HideInInspector] _BeamPulsePosition("Beam Pulse Position", Float) = -1
        [HideInInspector] _BeamActivation("Beam Activation", Float) = 1
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
            Name "Energy Beam"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
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
            #include "BeamGraphFunctions.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _BeamColor;
                half _Opacity;
                half _CoreWidth;
                half _CoreIntensity;
                half _HaloIntensity;
                half _EdgePower;
                half _VertexAmplitude;
                half _VertexFrequency;
                half _VertexSpeed;
                half _EndpointFalloff;
                half _FlowSpeed;
                half _FlowFrequency;
                half _FlowStrength;
                half _FlickerRate;
                half _FlickerStrength;
                half _PulseWidth;
                half _Filament;
                float _BeamTube;
                half _PulseIntensity;
                half _EndpointOpacityFalloff;
                half _BeamIntensity;
                float _BeamLength;
                float _BeamTime;
                float _BeamAge;
                float _BeamSeed;
                float _BeamPulsePosition;
                float _BeamActivation;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float4 volume : TEXCOORD3;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 beamUV : TEXCOORD0;
                float distance : TEXCOORD1;
                float seed : TEXCOORD2;
                float4 volume : TEXCOORD3;
                float3 viewFrame : TEXCOORD4;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float2 noise;
                BeamVertexNoise_float(
                    input.uv1.x,
                    input.uv0.x,
                    _BeamTime,
                    _BeamSeed + input.uv2.x,
                    _VertexFrequency,
                    _VertexSpeed,
                    _VertexAmplitude,
                    _EndpointFalloff,
                    _EndpointFalloff,
                    noise);

                float3 tangentOS = normalize(input.tangentOS.xyz);
                float3 normalOS = normalize(input.normalOS);
                float3 binormalOS = normalize(cross(tangentOS, normalOS));
                float3 positionOS = input.positionOS.xyz + normalOS * noise.x + binormalOS * noise.y;
                if (_BeamTube > 0.5 && abs(input.uv0.y) > 0.5)
                {
                    float phase = input.uv1.x * _FlowFrequency * 3.14159 - _BeamTime * _FlowSpeed;
                    float envelope = sin(3.14159 * saturate(input.uv0.x));
                    float3 orbitWS = TransformObjectToWorldDir(normalOS * cos(phase) + binormalOS * sin(phase));
                    positionOS += mul((float3x3)GetWorldToObjectMatrix(), orbitWS * input.uv1.y * envelope * input.uv0.y);
                }
                output.positionCS = TransformObjectToHClip(positionOS);
                float3 viewWS = GetWorldSpaceViewDir(TransformObjectToWorld(positionOS));
                float3 t = TransformObjectToWorldDir(tangentOS);
                float3 n = TransformObjectToWorldDir(normalOS);
                output.viewFrame = float3(dot(viewWS,n), dot(viewWS,normalize(cross(t,n))), dot(viewWS,t));
                output.volume = input.volume;
                output.beamUV = input.uv0;
                output.distance = input.uv1.x;
                output.seed = input.uv2.x;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float core;
                float halo;
                BeamCoreHalo_float(input.beamUV.y, _CoreWidth, _EdgePower, core, halo);
                float u = input.beamUV.x;
                float filament = 0.0;
                if (_BeamTube > 0.5)
                {
                    // Integrate one view chord through a finite cylinder in the local strand frame.
                    float3 ray = normalize(input.viewFrame);
                    float radius = max(input.volume.w, 0.0001);
                    float2 r = input.volume.xy;
                    float a = dot(ray.xy,ray.xy);
                    float b = dot(r,ray.xy);
                    float radialExit = a < 1e-6 ? 1e8 :
                        radius * (sqrt(max(0.0,b*b + a*(1.0-dot(r,r)))) - b) / a;
                    float axialExit = ray.z > 0 ? input.volume.z-input.distance : input.distance;
                    float chord = min(radialExit, axialExit / max(abs(ray.z),1e-6));
                    float density = saturate(chord / (2.0 * radius));
                    halo = pow(density, max(0.1,_EdgePower));
                    core = pow(density, 1.0 / max(0.01,_CoreWidth));
                    u = saturate((input.distance + 0.5 * chord * ray.z) / max(input.volume.z,0.0001));
                    float phase = input.distance * _FlowFrequency * 6.283185 - _BeamTime * _FlowSpeed;
                    filament = pow(saturate(0.5 + 0.5 * dot(r,float2(cos(phase),sin(phase)))),8) * _Filament;
                }

                float flow;
                BeamFlow_float(input.distance, _BeamTime, _FlowSpeed, _FlowFrequency, _BeamSeed + input.seed, flow);
                float energy = core * _CoreIntensity + halo * _HaloIntensity + filament;
                energy *= 1.0 + (flow * 2.0 - 1.0) * _FlowStrength;

                float pulse;
                BeamPulse_float(input.beamUV.x, _BeamPulsePosition, _PulseWidth, pulse);
                energy += pulse * _PulseIntensity;
                float flickerSample = BeamValueNoise1D(floor(_BeamTime * _FlickerRate), _BeamSeed + input.seed) * 0.5 + 0.5;
                energy *= lerp(1.0, flickerSample, _FlickerStrength);
                float endpointOpacity = BeamEndpointMaskValue(
                    u,
                    _EndpointOpacityFalloff,
                    _EndpointOpacityFalloff);

                half4 tint = _BaseColor * _BeamColor * input.color;
                half alpha = saturate(halo * _Opacity * tint.a * endpointOpacity * _BeamActivation);
                half3 emission = tint.rgb * energy;
                if (_BeamTube > 0.5)
                    emission += core * core * _CoreIntensity * lerp(0.15, 0.8, abs(input.beamUV.y));
                return half4(emission * _BeamIntensity * _BeamActivation, alpha);
            }
            ENDHLSL
        }
    }
}
