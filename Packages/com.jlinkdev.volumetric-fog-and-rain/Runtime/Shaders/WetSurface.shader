Shader "jlinkdev/Volumetric Fog and Rain/Wet Surface"
{
    Properties
    {
        [Header(Substrate)]
        _BaseColor ("Base color", Color) = (1,1,1,1)
        [NoScaleOffset] _StoneMap ("Albedo (sRGB)", 2D) = "white" {}
        [NoScaleOffset][Normal] _NormalMap ("Normal", 2D) = "bump" {}
        [NoScaleOffset] _RoughnessMap ("Roughness (linear)", 2D) = "gray" {}
        _StoneRepeat ("Texture repeat (metres)", Float) = 1.8
        _NormalStrength ("Normal strength", Range(0,2)) = 1
        _RoughnessOffset ("Dry roughness offset", Range(-1,1)) = 0
        _GrainContrast ("Surface grain contrast", Range(0,2)) = .8
        [ToggleUI] _WorldAlignedStone ("Align stone and joints across objects", Float) = 0
        [Header(Optional joints)]
        _TileSize ("Grid spacing (metres; zero disables)", Float) = 0
        _GroutWidth ("Joint width (metres)", Float) = .006
        _GroutTone ("Joint albedo multiplier", Range(0,1)) = .56
        [Header(Rain)]
        _Wetness ("Wetness", Range(0,1)) = 0
        _FilmSmoothness ("Water film smoothness", Range(0,1)) = .89
        _FilmVariation ("Uneven film coverage", Range(0,1)) = .8
        _WetDarkening ("Wet substrate darkening", Range(0,1)) = .25
        _BeadStrength ("Beads on exposed faces", Range(0,2)) = .85
        _BeadSpacing ("Bead spacing (metres)", Range(.008,.09)) = .026
        _RunoffStrength ("Downhill rivulets", Range(0,2)) = .85
        _RippleStrength ("Rain ripples on pooled water", Range(0,2)) = 0
        [HideInInspector] _RippleActivity ("Recent rain impacts", Range(0,1)) = 1
        [HideInInspector] _RainTime ("Runoff clock", Float) = 0
        [HideInInspector] _BeadTime ("Bead arrival clock", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _StoneRepeat, _NormalStrength, _RoughnessOffset, _GrainContrast, _TileSize;
            float _GroutWidth, _GroutTone, _WorldAlignedStone, _Wetness, _FilmSmoothness, _FilmVariation, _WetDarkening;
            float _BeadStrength, _BeadSpacing, _RunoffStrength, _RippleStrength, _RippleActivity, _RainTime, _BeadTime;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex WetVertex
            #pragma fragment WetFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #define _CLEARCOAT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_StoneMap); SAMPLER(sampler_StoneMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_RoughnessMap); SAMPLER(sampler_RoughnessMap);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 clip : SV_POSITION;
                float3 world : TEXCOORD0;
                half3 normal : TEXCOORD1;
                half4 fogAndVertexLight : TEXCOORD2;
                float2 lightmapUV : TEXCOORD3;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord : TEXCOORD4;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings WetVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS);
                output.world = position.positionWS;
                output.clip = position.positionCS;
                output.normal = TransformObjectToWorldNormal(input.normalOS);
                output.fogAndVertexLight = half4(ComputeFogFactor(output.clip.z), VertexLighting(output.world, output.normal));
                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = GetShadowCoord(position);
                #endif
                return output;
            }

            float HashCell(float2 cell)
            {
                float3 p = frac(cell.xyx * float3(.1031, .1030, .0973));
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float FilmNoise(float2 position)
            {
                float2 cell = floor(position);
                float2 f = frac(position);
                f = f * f * (3 - 2*f);
                return lerp(lerp(HashCell(cell),HashCell(cell + float2(1,0)),f.x),
                            lerp(HashCell(cell + float2(0,1)),HashCell(cell+1),f.x),f.y);
            }

            // Return the coverage and normal slope of individual drops. Their locations stay fixed
            // between arrivals. Fade subpixel drops to the film before they turn into sparkling noise.
            float3 BeadDetail(float2 metres, float2 downhill, float slope, float pixelSize, float wetness)
            {
                float spacing = max(.004, _BeadSpacing);
                float visible = 1 - smoothstep(.15, .43, pixelSize / spacing);
                if (visible <= 0 || _BeadStrength <= 0) return 0;
                float2 across = float2(downhill.y,-downhill.x);
                float2 field = float2(dot(metres,across),dot(metres,downhill)) / spacing;
                field.y /= 1 + slope * .38;
                field.x += HashCell(float2(floor(field.y),8)); // Break alignment between rows.
                float2 cell = floor(field);
                float seed = HashCell(cell);
                float radius = lerp(.07,.235,pow(HashCell(cell - 12),1.4));
                float margin = radius + .035;
                float2 center = lerp(margin,1-margin,float2(seed,HashCell(cell+41)));
                float2 q = (frac(field) - center) / radius;
                q.x *= 1 + .18 * clamp(q.y,-1,1);
                float r2 = dot(q, q);
                float edge = clamp(pixelSize / (spacing * radius),.065,.8);
                float arrival = frac(_BeadTime * lerp(.018, .043, seed) + HashCell(cell + 25));
                float coverage = (1 - smoothstep(1-edge, 1+edge, r2)) * visible
                               * smoothstep(seed * .56, seed * .56 + .22, wetness)
                               * smoothstep(0,.04,arrival) * (1 - smoothstep(.91,1,arrival));
                float2 capSlope = q / sqrt(max(.18,1-r2)) * .84;
                return float3(across*capSlope.x + downhill*capSlope.y,1) * coverage * _BeadStrength;
            }

            float3 RivuletDetail(float2 metres, float2 downhill, float pixelSize, float wetness)
            {
                float visible = 1 - smoothstep(.005, .024, pixelSize);
                if (visible <= 0 || _RunoffStrength <= 0) return 0;
                float2 across = float2(downhill.y, -downhill.x);
                float along = dot(metres, downhill);
                float laneCoord = dot(metres, across) * 9;
                float lane = floor(laneCoord);
                float seed = HashCell(float2(lane, 17));
                float center = lerp(.23,.77,seed) + .067*sin(along*5 + seed*42) + .029*sin(along*13 + seed*29);
                float width = lerp(.016,.065,HashCell(float2(lane, 6)));
                float x = (frac(laneCoord) - center) / width;
                float edge = max(.12, pixelSize * 9 / width);
                float rivulet = (1 - smoothstep(1-edge,1+edge,abs(x))) * visible;
                // A fixed water path catches moving thicker sections as rain runs downhill.
                float pulse = .68 + .32 * sin(along * 18 - _RainTime * 3 + seed * 60);
                rivulet *= pulse * smoothstep(seed*.55, seed*.55+.32,wetness) * _RunoffStrength;
                return float3(across * clamp(x,-1,1) * rivulet * .66, rivulet);
            }

            // A damped capillary wave train propagates from each impact. Its compact envelope
            // reaches zero before the cell boundary so independently timed impacts tile continuously.
            float2 ImpactWave(float2 metres, float2 dx, float2 dy, float span, float time)
            {
                float2 grid = metres / span;
                float2 cell = floor(grid);
                float2 center = .5 + .14 * (float2(HashCell(cell+7),HashCell(cell+29))-.5);
                float2 offset = (frac(grid) - center) * span;
                float distance = length(offset);
                float2 radial = offset / max(.001,distance);
                float age = frac(time * .86 + HashCell(cell+19));
                float head = span * (.035 + .59 * age);
                float trail = (head - distance) / span;
                float packet = smoothstep(0,.032,trail) * (1-smoothstep(.07,.34,trail));
                packet *= smoothstep(.005,.055,distance/span) * (1-smoothstep(.32,.42,distance/span));
                float wavelength = span * .13;
                float footprint = max(abs(dot(radial,dx)),abs(dot(radial,dy)));
                float resolved = 1 - smoothstep(.18,.70,footprint / wavelength);
                return radial * (sin(6.283185 * (distance-head) / wavelength) * packet * resolved);
            }

            // At a grazing angle, a pixel covers a longer strip of surface along the sight
            // line. Integrate unresolved water slopes over that footprint with anisotropic GGX.
            float3 RippledFilmLighting(InputData inputData, float3 normal, float2 metres, float coverage)
            {
                float3 view = inputData.viewDirectionWS;
                float nv = max(.03,dot(normal,view));
                float3 across = normalize(cross(normal,view));
                float3 along = cross(across,normal);
                // Fine capillary crests intermittently present a mirror facet. Decorrelation
                // in both world axes keeps a lamp reflection broken as the weather advances.
                float ripple = .66*FilmNoise(metres*float2(6,25) + float2(.14,-.65)*_RainTime)
                             + .34*FilmNoise(metres*float2(19,11) + float2(-.33,.23)*_RainTime);
                float ax = lerp(.024,.083,nv);
                float ay = lerp(.50,.16,nv);
                float gv = 2*nv / (nv + sqrt(nv*nv + ay*ay*(1-nv*nv)));
                float3 reflected = 0;
                uint count = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(count)
                    Light source = GetAdditionalLight(lightIndex,inputData.positionWS,inputData.shadowMask);
                    float nl = saturate(dot(normal,source.direction));
                    float3 h = SafeNormalize(source.direction+view);
                    float3 projected = float3(dot(across,h)/ax, dot(along,h)/ay, saturate(dot(normal,h)));
                    float d = dot(projected,projected);
                    float distribution = rcp(max(.0001,PI*ax*ay*d*d));
                    float sx = ax*dot(across,source.direction);
                    float sy = ay*dot(along,source.direction);
                    float gl = 2*nl / max(.001,nl+sqrt(nl*nl+sx*sx+sy*sy));
                    float grazing = 1-saturate(dot(view,h));
                    float grazing2 = grazing * grazing;
                    float fresnel = .0204 + .9796 * grazing2 * grazing2 * grazing;
                    reflected += source.color * (source.distanceAttenuation * source.shadowAttenuation
                               * distribution * fresnel * gv * gl / (4*nv));
                LIGHT_LOOP_END
                return reflected * coverage * (.09 + 2.60*smoothstep(.32,.76,ripple));
            }

            half4 WetFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 normal = normalize(input.normal);
                // Use object axes for a stable metric mapping on moving and non-uniformly scaled props.
                float3 right = normalize(TransformObjectToWorldDir(float3(1,0,0)));
                float3 up = normalize(TransformObjectToWorldDir(float3(0,1,0)));
                float3 forward = normalize(TransformObjectToWorldDir(float3(0,0,1)));
                float3 origin = TransformObjectToWorld(float3(0,0,0));
                if (_WorldAlignedStone > .5)
                {
                    right = float3(1,0,0);
                    up = float3(0,1,0);
                    forward = float3(0,0,1);
                    origin = 0;
                }
                float3 localNormal = float3(dot(right,normal), dot(up,normal), dot(forward,normal));
                float3 axis = abs(localNormal);
                float3 tangent = (axis.x > axis.y && axis.x > axis.z) ? -sign(localNormal.x)*forward : right;
                tangent = normalize(tangent - normal * dot(normal,tangent));
                float3 bitangent = cross(normal,tangent);
                float3 relative = input.world - origin;
                float2 metres = float2(dot(relative,tangent), dot(relative,bitangent));
                float2 pixel = max(fwidth(metres),.00001);
                // A grazing tile can be foreshortened along one axis yet resolve beads along the other.
                float2 dx = ddx(metres), dy = ddy(metres);
                float pixelSize = max(.31*max(pixel.x,pixel.y),sqrt(abs(dx.x*dy.y - dx.y*dy.x)));
                float2 uv = metres / max(.001,_StoneRepeat);
                float2 dxUV = dx / max(.001,_StoneRepeat), dyUV = dy / max(.001,_StoneRepeat);
                float joint = 0;
                float slabTone = 1;
                float2 lip = 0;
                if (_TileSize > 0 && _GroutWidth > 0)
                {
                    float2 slab = floor(metres / _TileSize);
                    float2 cut = float2(HashCell(slab+4),HashCell(slab+17));
                    uv += cut;
                    slabTone = lerp(.925,1.075,cut.x);
                    float2 local = frac(metres / _TileSize);
                    float2 distance = min(local,1-local) * _TileSize;
                    float2 fade = 1 - smoothstep(.04*_TileSize,.35*_TileSize,pixel);
                    float2 groove = saturate((.5*_GroutWidth - distance) / pixel + .5) * fade;
                    joint = max(groove.x,groove.y);
                    float2 shoulder = (1-smoothstep(.3*_GroutWidth,.6*_GroutWidth+.006+pixel,distance)) * fade;
                    lip = sign(local-.5) * shoulder * .26;
                }

                half3 stone = SAMPLE_TEXTURE2D_GRAD(_StoneMap,sampler_StoneMap,uv,dxUV,dyUV).rgb;
                half3 stoneMean = SAMPLE_TEXTURE2D_GRAD(_StoneMap,sampler_StoneMap,uv,dxUV*9,dyUV*9).rgb;
                half3 albedo = saturate(stone + (stone - stoneMean)*_GrainContrast) * _BaseColor.rgb * slabTone;
                half roughness = SAMPLE_TEXTURE2D_GRAD(_RoughnessMap,sampler_RoughnessMap,uv,dxUV,dyUV).r;
                half roughnessMean = SAMPLE_TEXTURE2D_GRAD(_RoughnessMap,sampler_RoughnessMap,uv,dxUV*9,dyUV*9).r;
                float grain = roughness - roughnessMean;
                half3 relief = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_NormalMap,sampler_NormalMap,uv,dxUV,dyUV),_NormalStrength);
                float drySmoothness = 1 - saturate(roughness + grain * _GrainContrast + _RoughnessOffset + joint * .17);
                float wetness = saturate(_Wetness);
                float upward = saturate(normal.y);
                float exposed = smoothstep(-.35,.12,normal.y);
                float film = wetness * exposed * lerp(.58,1,upward);
                float3 waterDetail = 0;
                float disturbed = 0;
                if (film > 0)
                {
                    // Broad shallow patches break the lamp reflections. Finer rough grain retains a thinner coat.
                    float basin = .63 * FilmNoise(metres*1.35 + 7.3) + .37 * FilmNoise(metres*4.7 - 2.1);
                    float pooling = smoothstep(.20,.78,basin - grain*2.5);
                    film *= lerp(1,lerp(.23,1,pooling),_FilmVariation) * (1-.25*joint);
                    disturbed = _RippleStrength * _RippleActivity * film * smoothstep(.84,.97,upward);
                    float2 gravity = -float2(tangent.y,bitangent.y);
                    float slope = length(gravity);
                    float2 downhill = slope > .001 ? gravity / slope : float2(.5,.8660254);
                    waterDetail = BeadDetail(metres,downhill,slope,pixelSize,wetness) * lerp(.40,1,upward) * exposed;
                    if (slope > .15)
                        waterDetail += RivuletDetail(metres,downhill,pixelSize,wetness)
                                     * smoothstep(.15,.78,slope) * exposed;
                    if (disturbed > .001)
                    {
                        // Use a shared metric origin across adjacent paving meshes.
                        float2 p = float2(dot(input.world,tangent),dot(input.world,bitangent));
                        float2 rings = ImpactWave(p,dx,dy,.255,_RainTime)
                                     + ImpactWave(p + float2(.19,1.17),dx,dy,.39,_RainTime*1.11);
                        waterDetail.xy += rings * (.115 * disturbed);
                    }
                }
                float coat = saturate(waterDetail.z);
                float coverage = saturate(film + coat);
                float2 substrateSlope = relief.xy / max(.18,relief.z);
                float2 surfaceSlope = substrateSlope * (1 - .43*film) + lip * (1-.8*saturate(disturbed)) + waterDetail.xy;
                float3 surfaceNormal = normalize(normal + tangent*surfaceSlope.x + bitangent*surfaceSlope.y);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo * lerp(1,_GroutTone,joint) * (1 - _WetDarkening * sqrt(coverage));
                surface.smoothness = lerp(drySmoothness,max(drySmoothness,_FilmSmoothness-.12),film*.65);
                surface.clearCoatMask = coverage;
                surface.clearCoatSmoothness = lerp(saturate(_FilmSmoothness - .20*(1-film) - .08*joint),.94,coat);
                surface.occlusion = lerp(1,.9,joint);
                surface.alpha = 1;
                InputData light = (InputData)0;
                light.positionWS = input.world;
                light.normalWS = surfaceNormal;
                light.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.world);
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    light.shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    light.shadowCoord = TransformWorldToShadowCoord(input.world);
                #endif
                light.bakedGI = SAMPLE_GI(input.lightmapUV, SampleSH(surfaceNormal), surfaceNormal);
                light.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
                light.vertexLighting = input.fogAndVertexLight.yzw;
                light.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.clip);
                float integrated = 0;
                #if defined(_ADDITIONAL_LIGHTS)
                    integrated = saturate(disturbed) * smoothstep(.08,.60,1-dot(normal,light.viewDirectionWS)) * .90;
                    surface.clearCoatMask *= 1-integrated;
                #endif
                half4 color = UniversalFragmentPBR(light,surface);
                #if defined(_ADDITIONAL_LIGHTS)
                    if (integrated > .001)
                    {
                        float2 p = float2(dot(input.world,tangent),dot(input.world,bitangent));
                        color.rgb += RippledFilmLighting(light,normal,p,integrated * coverage);
                    }
                #endif
                return half4(MixFog(color.rgb,input.fogAndVertexLight.x),1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
    Fallback Off
}
