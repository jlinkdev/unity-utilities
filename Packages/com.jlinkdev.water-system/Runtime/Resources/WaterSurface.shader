Shader "jlinkdev/Water System/Surface"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-20" "RenderType" = "Transparent" }
        Pass
        {
            Name "Water Surface"
            Tags { "LightMode" = "UniversalForward" }
            Blend One Zero
            // This pass replaces opaque scene color (Blend One Zero). Writing depth prevents farther
            // wave triangles from overwriting nearer crests. Refraction samples the earlier opaque copy;
            // subsequent transparent geometry behind this interface is intentionally depth-occluded.
            ZWrite On
            Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "../Shaders/WaterCommon.hlsl"

            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float fog : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = float3(input.positionOS.x + _WaterOrigin.x, 0, input.positionOS.z + _WaterOrigin.z);
                world.y = WaterHeight(world.xz);
                output.positionWS = world;
                output.positionCS = TransformWorldToHClip(world);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                bool above = IS_FRONT_VFACE(face, true, false);
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 normal = WaterNormal(input.positionWS.xz);
                // Fine ripples change only the shading normal; the geometric wave remains shared with physics and closure.
                float2 p = input.positionWS.xz - _WaterOrigin.xz;
                float2 ripple = float2(sin(p.x * 3.7 + p.y * 2.3 + _WaterTime * 1.3),
                                       cos(p.y * 4.1 - p.x * 1.9 - _WaterTime * 1.7));
                normal = normalize(normal + float3(ripple.x, 0, ripple.y) * _WaterDetailNormalStrength);
                if (!above) normal = -normal;
                float rawDepth = SampleSceneDepth(uv);
                float3 scenePosition = WaterWorldPosition(uv, rawDepth);
                float surfaceEye = -TransformWorldToView(input.positionWS).z;
                float sceneEye = -TransformWorldToView(scenePosition).z;
                float eyeThickness = WaterIsSky(rawDepth) ? _WaterOrigin.w : max(0, sceneEye - surfaceEye);
                float thickness = min(_WaterOrigin.w, eyeThickness / max(.1, dot(view, UNITY_MATRIX_V[2].xyz)));
                // Screen-space refraction responds to surface slopes. Including the unperturbed up normal
                // would shift even a flat interface by the full UV strength, creating a false horizon jump.
                float3 interfaceNormal = above ? float3(0, 1, 0) : float3(0, -1, 0);
                float2 normalVS = TransformWorldToViewDir(normal - interfaceNormal, false).xy;
                float2 refractedUV = saturate(uv + normalVS * _WaterRefraction * saturate(eyeThickness));
                float refractedDepth = SampleSceneDepth(refractedUV);
                float3 refractedPosition = WaterWorldPosition(refractedUV, refractedDepth);
                // Refraction cannot sample an opaque foreground object that lies in front of this water fragment.
                if (!WaterIsSky(refractedDepth) && -TransformWorldToView(refractedPosition).z <= surfaceEye + .02)
                {
                    refractedUV = uv;
                    refractedPosition = scenePosition;
                }
                half3 background = SampleSceneColor(refractedUV);
                if (above && !WaterIsSky(refractedDepth) && WaterDomainDistance(refractedPosition) > 0)
                    background += WaterCaustics(refractedPosition) * _WaterShallowColor.rgb;
                float3 transmission = exp(-max(0, _WaterAbsorption.rgb + _WaterDensity) * thickness);
                half3 waterColor = lerp(_WaterShallowColor.rgb, _WaterDeepColor.rgb, 1 - exp(-thickness * .25));
                half3 color = above ? background * transmission + waterColor * (1 - transmission) : background;

                float nv = saturate(dot(normal, view));
                float fresnel = .0204 + .9796 * pow(1 - nv, 5);
                // Approximate the critical angle of the water/air interface on the underside.
                if (!above) fresnel = lerp(fresnel, 1, 1 - smoothstep(.62, .70, nv));
                half3 reflection = GlossyEnvironmentReflection(reflect(-view, normal), input.positionWS,
                    1 - _WaterSmoothness, 1, uv);
                color = lerp(color, reflection, fresnel);
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 halfVector = SafeNormalize(sun.direction + view);
                float specularPower = exp2(3 + 10 * _WaterSmoothness);
                float specular = pow(saturate(dot(normal, halfVector)), specularPower);
                color += sun.color * (sun.distanceAttenuation * sun.shadowAttenuation) * specular * (.25 + fresnel);

                float foam = above ? (1 - smoothstep(0, max(.001, _WaterFoamWidth), eyeThickness)) : 0;
                foam *= saturate(.65 + .35 * sin(p.x * 8.7 + sin(p.y * 6.1) + _WaterTime * 1.4)) * _WaterFoamStrength;
                color = lerp(color, _WaterFoamColor.rgb, saturate(foam * _WaterFoamColor.a));
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
