Shader "Hidden/jlinkdev/ProceduralNebulae"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "NebulaDensity.hlsl"
        TEXTURE3D(_OpticalDepth); SAMPLER(sampler_OpticalDepth);
        TEXTURE3D(_CompanionDepth);
        TEXTURE2D(_NebulaLow); TEXTURE2D(_NebulaDepth);
        float4 _LowSize;
        float4x4 _WorldToBox;
        float3 _StarUVW, _ShadowFill, _StarColor, _LitDust;
        float3 _OuterGas, _InnerGas;
        float4 _TransitionGas;
        float _GasBrightness;
        float _StarRadius, _Extinction;
        float3 _CompanionUVW, _CompanionColor;
        float _CompanionRadius;
        int _RaySteps;

        struct StellarHit { float surface, centre, glow; bool disk; };
        StellarHit intersectStar(float3 offset, float radius, float3 ray, float limit)
        {
            StellarHit hit;
            hit.centre = dot(offset,ray);
            float3 across = offset-ray*hit.centre;
            float impact2 = dot(across,across);
            float chord2 = radius*radius-impact2;
            float surface = hit.centre-sqrt(max(chord2,0));
            hit.disk = radius>0 && chord2>0 && surface>=0 && surface<limit;
            hit.surface = hit.disk ? surface : limit;
            hit.glow = 0;
            float radial2 = impact2/max(radius*radius,1e-12);
            if (!hit.disk && radius>0 && hit.centre>0 && hit.centre<limit && radial2<144)
                hit.glow = .85*exp(-.47*radial2)+.13*exp(-.055*radial2);
            return hit;
        }

        float4 Integrate(Varyings input)
        {
            float2 uv = input.texcoord;
            float depth = SampleSceneDepth(uv);
            #if !UNITY_REVERSED_Z
                depth = lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
            #endif
            float3 endWS = ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
            float3 originWS = _WorldSpaceCameraPos;
            if (unity_OrthoParams.w > 0)
            {
                float3 forward = -UNITY_MATRIX_V[2].xyz;
                originWS = endWS - forward*dot(endWS-originWS,forward);
            }
            float3 origin = mul(_WorldToBox,float4(originWS,1)).xyz+.5;
            float3 delta = mul((float3x3)_WorldToBox,endWS-originWS);
            float limit = max(length(delta),1e-7);
            float3 ray = delta/limit;

            // Clip the background to the visible surface of the same sphere used for lighting.
            StellarHit primary = intersectStar(_StarUVW-origin,_StarRadius,ray,limit);
            StellarHit companion = intersectStar(_CompanionUVW-origin,_CompanionRadius,ray,limit);
            bool disk = primary.disk || companion.disk;
            limit = min(primary.surface,companion.surface);
            if (primary.centre>limit) primary.glow=0;
            if (companion.centre>limit) companion.glow=0;

            float3 invRay = (step(0,ray)*2-1)/max(abs(ray),1e-7);
            float3 a = -origin*invRay, b = (1-origin)*invRay;
            float3 lo = min(a,b), hi = max(a,b);
            float entry = max(0,max(lo.x,max(lo.y,lo.z)));
            float exit = min(limit,min(hi.x,min(hi.y,hi.z)));
            float3 radiance = 0;
            float transmission = 1;
            float2 starTransmission = 1;
            if (exit > entry)
            {
                int steps = _RaySteps;
                if (usesSurfaceDetail()) steps += (int)round(_RaySteps*_SurfaceDetail);
                if (_NebulaShape==NEBULA_SCULPTED_BANK)
                    steps += (int)round(.65*_RaySteps*_SurfaceDetail*_FoldRelief);
                if (_DomainPattern!=0) steps = max(steps,2*_RaySteps);
                float stride = (exit-entry)/steps;
                float jitter = .35 + .3*frac(52.9829189*frac(dot(input.positionCS.xy,float2(.06711056,.00583715))));
                float3 p = origin+ray*(entry+stride*jitter);
                [loop] for (int i=0;i<steps;i++,p+=ray*stride)
                {
                    float3 medium = sampleMedium(p,ray*stride);
                    if (max(medium.x,medium.y)<.0003) continue;
                    float density = medium.x;
                    float3 toStar = _StarUVW-p;
                    float d2 = max(dot(toStar,toStar),1e-6);
                    float facing = saturate(.5+.5*dot(ray,toStar*rsqrt(d2)));
                    float tau = SAMPLE_TEXTURE3D_LOD(_OpticalDepth,sampler_OpticalDepth,p,0).r*_Extinction;
                    if (usesSurfaceDetail())
                    {
                        // Resolve the first few world units of shadow at the
                        // detailed boundary; the cache supplies the remaining path.
                        float3 direction = toStar*rsqrt(d2);
                        float3 toFace = lerp(p,1-p,step(0,direction))/max(abs(direction),1e-6);
                        float remaining = min(sqrt(d2),min(toFace.x,min(toFace.y,toFace.z)));
                        float segment = min(.030*_SurfaceDetail,remaining);
                        float3 stepToStar = direction*(segment/3);
                        float localDepth = fineDensity(p+.5*stepToStar)+fineDensity(p+1.5*stepToStar)
                                         + fineDensity(p+2.5*stepToStar);
                        float beyond = segment<remaining
                            ? SAMPLE_TEXTURE3D_LOD(_OpticalDepth,sampler_OpticalDepth,p+3*stepToStar,0).r : 0;
                        tau = (beyond+localDepth*(segment/3))*_Extinction;
                    }
                    // Warmer transmitted light after passing through more dust.
                    float3 attenuation = exp(-tau*float3(.72,.95,1.25));
                    float3 lightDir = toStar*rsqrt(d2);
                    float neighbor = fineDensity(p + lightDir*.009);
                    float exposed = saturate(.38+2.8*(density-neighbor));
                    float3 light = _ShadowFill*.15 +
                        _StarColor*_LitDust*attenuation*(.09+.53*pow(facing,7))*(.38+exposed)/(1+18*d2);
                    float3 scattered = _ShadowFill*(.42+.38*exposed) +
                        _StarColor*_LitDust*attenuation*(.26+1.25*pow(facing,7))*(.42+.92*exposed)/(1+15*d2);
                    light = lerp(light,scattered,_CloudBody);
                    float opacity = 1-exp(-density*_Extinction*stride);
                    float excitation = exp(-.82*tau)/(1+18*d2);
                    float ionized = smoothstep(.12,.68,excitation);
                    float3 gasColor = lerp(_OuterGas,_InnerGas,ionized);
                    float power = dot(_StarColor,float3(.2126,.7152,.0722));
                    float3 emission = gasColor*(_GasBrightness*power*excitation*medium.y*11);
                    if (_NebulaShape==NEBULA_BACKLIT_BANK)
                    {
                        // The gas palette follows exposure to the embedded star.
                        float3 outerToMid = lerp(_OuterGas,_TransitionGas.rgb,smoothstep(0,.50,ionized));
                        float3 spectrum = lerp(outerToMid,_InnerGas,smoothstep(.50,1,ionized));
                        gasColor = lerp(gasColor,spectrum,_TransitionGas.a);
                        float incident = exp(-tau)/(1+30*d2);
                        float spill = .081*exp(-.25*tau)*medium.z/(1+22*d2);
                        spill += .047*exp(-.085*tau)*saturate(medium.z)/(1+7*d2);
                        spill += .15*saturate(medium.z-1)*exp(-.09*tau)/(1+9*d2);
                        light = _ShadowFill*(.42+1.3*medium.z) +
                                _StarColor*_LitDust*(incident*(.12+.77*pow(facing,5))+spill);
                        if (_LayeredCurtain && _SurfaceDetail>0)
                        {
                            float surfaceWeight = _SurfaceDetail*lerp(.18,.55,smoothstep(.55,.68,p.z));
                            float3 dustTint = lerp(_LitDust,gasColor,.68);
                            float scatteredPower = (exp(-tau)*(.52+.58*pow(facing,4))+.055*exp(-.28*tau))/(1+16*d2);
                            light = lerp(light,_ShadowFill*.45+_StarColor*dustTint*(2.4*scatteredPower),surfaceWeight);
                        }
                        float excitationAtBank = exp(-.70*tau)/(1+30*d2);
                        // Several thin lit surfaces can overlap along one sightline.
                        // Soften peak excitation to retain the separate silver rims.
                        if (_LayeredCurtain) excitationAtBank /= 1+1.35*excitationAtBank;
                        emission = gasColor*(_GasBrightness*power*excitationAtBank*medium.y*11);
                    }
                    else if (_NebulaShape==NEBULA_SWEPT_ARC)
                    {
                        float incident = exp(-tau)/(1+17*d2);
                        float3 warm = lerp(_OuterGas,_InnerGas,smoothstep(.085,.48,incident));
                        float3 cool = lerp(_LitDust,_TransitionGas.rgb,_TransitionGas.a);
                        cool = lerp(cool,_InnerGas,.39*smoothstep(.72,.97,incident));
                        float3 tint = lerp(cool,warm,medium.z);
                        light = _ShadowFill*(.45+.55*exposed) + _StarColor*lerp(cool,_LitDust,medium.z)*
                                (incident*(.20+.90*pow(facing,4))+.043*exp(-.25*tau)/(1+12*d2));
                        emission = tint*(_GasBrightness*power*medium.y*6.5*excitation);
                    }
                    else if (_NebulaShape==NEBULA_SCULPTED_BANK)
                    {
                        float transmitted = exp(-tau);
                        float softFill = .055*exp(-.28*tau);
                        float3 dustTint = _LitDust;
                        if (_TransitionGas.a>0)
                        {
                            // Color the scattered starlight on the same density contours
                            // as the grayscale study. Empty pockets contribute no light.
                            float exposure = transmitted/(1+16*d2);
                            float3 tint = lerp(_OuterGas,_TransitionGas.rgb,smoothstep(.035,.28,exposure));
                            tint = lerp(tint,_InnerGas,smoothstep(.26,.65,exposure));
                            dustTint = lerp(dustTint,tint,_TransitionGas.a);
                        }
                        light = _ShadowFill*.45 + _StarColor*dustTint*
                                (transmitted*(.52+.58*pow(facing,4))+softFill)/(1+16*d2);
                    }
                    else if (_NebulaShape==NEBULA_PLUMES)
                    {
                        float3 direct = exp(-tau*float3(.78,.95,1.13));
                        // Estimate the coarse lobe normal independently of the fine erosion.
                        // The derivative along the star ray alone cannot distinguish a broad
                        // grazing shoulder from a steeply lit face.
                        const float e = 1.0/128;
                        float baseDensity = _Density.SampleLevel(sampler_linear_clamp,p,0).x;
                        float3 normal = baseDensity-float3(
                            _Density.SampleLevel(sampler_linear_clamp,p+float3(e,0,0),0).x,
                            _Density.SampleLevel(sampler_linear_clamp,p+float3(0,e,0),0).x,
                            _Density.SampleLevel(sampler_linear_clamp,p+float3(0,0,e),0).x);
                        normal *= rsqrt(max(dot(normal,normal),1e-6));
                        float diffuse = saturate(dot(normal,lightDir));
                        float sky = saturate(.5+dot(normal,float3(.23,.40,-.18)));
                        float bounce = .11*exp(-.20*tau)*(.2+.8*sky);
                        light = _ShadowFill*(.24+1.7*sky) +
                            _StarColor*_LitDust*(direct*(.12+.93*diffuse)+bounce)/(1+25*d2);
                        emission = lerp(_OuterGas,_InnerGas,ionized)*(_GasBrightness*power*excitation*medium.y*7);
                    }
                    else if (_NebulaShape==NEBULA_PAINTERLY)
                    {
                        float3 transmitted = exp(-tau*float3(.49,.94,1.65));
                        light = _ShadowFill*(.39+.35*exposed) + _StarColor*_LitDust*transmitted*
                                (.23+.82*pow(facing,4))*(.32+1.4*exposed)/(1+22*d2);
                        float warmExcitation = exp(-.63*tau)/(1+30*d2);
                        float3 warm = lerp(_InnerGas*.65+float3(.15,.18,.17),_InnerGas,
                                           smoothstep(.06,.51,warmExcitation))*exp(-tau*float3(.13,.39,.85));
                        float3 spectrum = lerp(warm,_OuterGas,medium.z);
                        emission = spectrum*(_GasBrightness*power*warmExcitation*medium.y*12);
                    }
                    if (_CompanionRadius>0)
                    {
                        float3 toSecond = _CompanionUVW-p;
                        float dist2 = max(dot(toSecond,toSecond),1e-7);
                        float opticalDepth = SAMPLE_TEXTURE3D_LOD(_CompanionDepth,sampler_OpticalDepth,p,0).r*_Extinction;
                        float incident = exp(-opticalDepth)/(1+245*dist2);
                        float phase = pow(saturate(.5+.5*dot(ray,toSecond*rsqrt(dist2))),5);
                        light += _CompanionColor*(.18+.78*phase)*incident;
                        emission += _CompanionColor*(_GasBrightness*medium.y*incident*4);
                    }
                    float opticalStep = density*_Extinction*stride;
                    float gasPath = opticalStep>.001 ? opacity/opticalStep : 1-.5*opticalStep;
                    radiance += transmission*(opacity*light + emission*stride*gasPath);
                    if (primary.glow+companion.glow>0)
                        starTransmission *= exp(-opticalStep*saturate((float2(primary.centre,companion.centre)-entry)/stride-i));
                    transmission *= 1-opacity;
                    // HDR stellar radiance can remain visible past the ordinary opacity cutoff.
                    if (!disk && transmission<.003) break;
                }
            }
            if (disk)
            {
                radiance += transmission*(primary.surface<=companion.surface ? _StarColor : _CompanionColor)*4;
                transmission = 0;
            }
            radiance += starTransmission.x*primary.glow*_StarColor + starTransmission.y*companion.glow*_CompanionColor;
            return float4(radiance,transmission);
        }

        float EyeDepth(float2 uv)
        {
            float depth = SampleSceneDepth(uv);
            if (unity_OrthoParams.w>0)
            {
                #if UNITY_REVERSED_Z
                    depth=1-depth;
                #endif
                return lerp(_ProjectionParams.y,_ProjectionParams.z,depth);
            }
            return LinearEyeDepth(depth,_ZBufferParams);
        }
        float4 Composite(float2 uv,float4 cloud)
        {
            float4 background = SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv);
            return float4(cloud.rgb+cloud.a*background.rgb,background.a);
        }
        float4 Full(Varyings input):SV_Target { return Composite(input.texcoord,Integrate(input)); }
        struct LowOutput { float4 cloud:SV_Target0; float depth:SV_Target1; };
        LowOutput Low(Varyings input)
        {
            LowOutput result;
            result.cloud=Integrate(input);
            result.depth=EyeDepth(input.texcoord);
            return result;
        }
        float4 Resolve(Varyings input):SV_Target
        {
            float2 uv=input.texcoord;
            float2 pixel=uv*_LowSize.xy-.5, base=floor(pixel), f=frac(pixel);
            float depth=EyeDepth(uv);
            float4 cloud=0;
            float total=0;
            [unroll] for(int y=0;y<2;y++) [unroll] for(int x=0;x<2;x++)
            {
                float2 tap=(clamp(base+float2(x,y),0,_LowSize.xy-1)+.5)*_LowSize.zw;
                float z=SAMPLE_TEXTURE2D_LOD(_NebulaDepth,sampler_PointClamp,tap,0).r;
                float w=(x?f.x:1-f.x)*(y?f.y:1-f.y)*exp2(-4*abs(z-depth)/max(.035,depth*.005));
                cloud+=SAMPLE_TEXTURE2D_LOD(_NebulaLow,sampler_PointClamp,tap,0)*w;
                total+=w;
            }
            if(total<.01) cloud=Integrate(input);
            else cloud/=total;
            return Composite(uv,cloud);
        }
        ENDHLSL
        Pass
        {
            Name "Full resolution"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Full
            ENDHLSL
        }
        Pass
        {
            Name "Half resolution"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Low
            ENDHLSL
        }
        Pass
        {
            Name "Depth-aware reconstruction"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Resolve
            ENDHLSL
        }
    }
}
