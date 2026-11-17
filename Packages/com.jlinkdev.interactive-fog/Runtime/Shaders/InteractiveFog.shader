Shader "Hidden/jlinkdev/InteractiveFog"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE3D(_FogDensity); SAMPLER(sampler_FogDensity);
            TEXTURE3D(_FogPreviousDensity); TEXTURE3D(_FogCoordinatesA); TEXTURE3D(_FogCoordinatesB);
            TEXTURE3D(_FogPreviousCoordinatesA); TEXTURE3D(_FogPreviousCoordinatesB);
            TEXTURE2D(_FogPlanarMass); TEXTURE2D(_FogPlanarPreviousMass); TEXTURE2D(_FogPlanarRestMass); TEXTURE2D(_FogPlanarSolid);
            TEXTURE2D(_FogPlanarCoordinatesA); TEXTURE2D(_FogPlanarCoordinatesB);
            TEXTURE2D(_FogPlanarPreviousCoordinatesA); TEXTURE2D(_FogPlanarPreviousCoordinatesB); TEXTURE3D(_FogPlanarLighting);
            TEXTURE3D(_FogRestingVolume); TEXTURE3D(_FogCumulusShape);
            TEXTURE3D(_FogPuffDensity); TEXTURE3D(_FogPreviousPuffDensity);
            float4 _FogFlow, _FogCumulusResolution;
            float _FogWispDisplacement, _FogFoldSize, _FogCumulusWispOpacity, _FogCumulusRibbonWidth;
            TEXTURE2D(_FogFoldPattern); SAMPLER(sampler_FogFoldPattern);
            TEXTURE3D(_FogLighting); SAMPLER(sampler_FogLighting);
            float3 _FogMin, _FogSize, _FogLightDirection;
            float4 _FogAlbedo, _FogAmbient, _FogLightColor, _FogParams;
            int _FogSteps;
            float hash(float3 p) { p=frac(p*.1031); p+=dot(p,p.yzx+33.33); return frac((p.x+p.y)*p.z); }
            float noise(float3 p)
            {
                float3 i=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z);
            }
float3 curlPattern(float3 p)
{
    // Analytic curl of A=(sin(y)sin(z), sin(z)sin(x), sin(x)sin(y)).
    float3 s=sin(p), c=cos(p);
    return float3(s.x*(c.y-c.z),s.y*(c.z-c.x),s.z*(c.x-c.y));
}
            float sampleDensity(float3 q)
            {
                if(any(q<0)||any(q>1)) return 0;
                #if defined(FOG_PLANAR)
                float mass=lerp(SAMPLE_TEXTURE2D_LOD(_FogPlanarPreviousMass,sampler_LinearClamp,q.xz,0).r,SAMPLE_TEXTURE2D_LOD(_FogPlanarMass,sampler_LinearClamp,q.xz,0).r,_FogFlow.x);
                float rest=SAMPLE_TEXTURE2D_LOD(_FogPlanarRestMass,sampler_LinearClamp,q.xz,0).r;
                float solid=SAMPLE_TEXTURE2D_LOD(_FogPlanarSolid,sampler_PointClamp,q.xz,0).r;
                return solid>.5?0:SAMPLE_TEXTURE3D_LOD(_FogRestingVolume,sampler_LinearClamp,q,0).r*min(3,mass/max(.001,rest));
                #else
                return lerp(SAMPLE_TEXTURE3D_LOD(_FogPreviousDensity,sampler_FogDensity,q,0).r,
                    SAMPLE_TEXTURE3D_LOD(_FogDensity,sampler_FogDensity,q,0).r,_FogFlow.x);
                #endif
            }
            float samplePuffs(float3 q)
            {
                #if defined(FOG_DRY_ICE)
                if(any(q<0)||any(q>1)) return 0;
                return lerp(SAMPLE_TEXTURE3D_LOD(_FogPreviousPuffDensity,sampler_LinearClamp,q,0).r,SAMPLE_TEXTURE3D_LOD(_FogPuffDensity,sampler_LinearClamp,q,0).r,_FogFlow.x);
                #else
                return 0;
                #endif
            }
            float4 IntegrateFog(Varyings input)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv=input.texcoord;
                float depth=SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                float3 endWS=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
                float3 origin=_WorldSpaceCameraPos;
                float3 ray=normalize(endWS-origin);
                if(unity_OrthoParams.w>0)
                {
                    ray=-UNITY_MATRIX_V[2].xyz;
                    origin=endWS-ray*dot(endWS-_WorldSpaceCameraPos,ray);
                }
                float maximum=dot(endWS-origin,ray);
                float3 invRay=rcp(ray+float3(abs(ray.x)<1e-6?1e-6:0,abs(ray.y)<1e-6?1e-6:0,abs(ray.z)<1e-6?1e-6:0));
                float3 a=(_FogMin-origin)*invRay,b=(_FogMin+_FogSize-origin)*invRay;
                float3 lo=min(a,b),hi=max(a,b);
                float entry=max(0,max(lo.x,max(lo.y,lo.z))), exit=min(maximum,min(hi.x,min(hi.y,hi.z)));
                if(exit<=entry) return float4(0,0,0,1);
                float stepSize=(exit-entry)/_FogSteps;
                // Static screen-space jitter avoids sparkling temporal noise without requiring TAA.
                float jitter=frac(52.9829189*frac(dot(input.positionCS.xy,float2(.06711056,.00583715))));
                float t=entry+stepSize*jitter, transmittance=1; float3 radiance=0;
                float g=_FogParams.y, cosTheta=dot(ray,_FogLightDirection);
                float phase=(1-g*g)/pow(max(.05,1+g*g-2*g*cosTheta),1.5);
                [loop] for(int i=0;i<_FogSteps;i++,t+=stepSize)
                {
                    float3 p=origin+ray*t, q=(p-_FogMin)/_FogSize;
                    float density=sampleDensity(q),puff=samplePuffs(q);
                    // Probe a small vertical neighborhood to retain displaced silhouettes
                    // without running detailed noise throughout the empty upper volume.
                    float reach=_FogWispDisplacement*1.8;
                    float envelope=max(density,sampleDensity(q-float3(0,reach/_FogSize.y,0)));
                    if(envelope+puff<=.001) continue;
                    float3 detailQ=q;
                    #if !defined(FOG_PROFILE_NO_DETAIL)
                    #if defined(FOG_PLANAR)
                    float4 flowA=lerp(SAMPLE_TEXTURE2D_LOD(_FogPlanarPreviousCoordinatesA,sampler_LinearClamp,q.xz,0),SAMPLE_TEXTURE2D_LOD(_FogPlanarCoordinatesA,sampler_LinearClamp,q.xz,0),_FogFlow.x);
                    float4 flowB=lerp(SAMPLE_TEXTURE2D_LOD(_FogPlanarPreviousCoordinatesB,sampler_LinearClamp,q.xz,0),SAMPLE_TEXTURE2D_LOD(_FogPlanarCoordinatesB,sampler_LinearClamp,q.xz,0),_FogFlow.x);
                    flowA.y=flowB.y=q.y*_FogSize.y;
                    #else
                    float4 flowA=SAMPLE_TEXTURE3D_LOD(_FogCoordinatesA,sampler_FogDensity,q,0);
                    float4 flowB=SAMPLE_TEXTURE3D_LOD(_FogCoordinatesB,sampler_FogDensity,q,0);
                    flowA=lerp(SAMPLE_TEXTURE3D_LOD(_FogPreviousCoordinatesA,sampler_FogDensity,q,0),flowA,_FogFlow.x);
                    flowB=lerp(SAMPLE_TEXTURE3D_LOD(_FogPreviousCoordinatesB,sampler_FogDensity,q,0),flowB,_FogFlow.x);
                    #endif
                    float3 coordA=flowA.xyz/_FogFoldSize,coordB=flowB.xyz/_FogFoldSize;
                    float blend=1-abs(frac(_FogFlow.y/6)*2-1);
                    float activity=saturate(lerp(flowB.w,flowA.w,blend));
                    // Deform the original 3D billows only inside sparse, irregular curl regions.
                    coordA.xz+=SAMPLE_TEXTURE2D_LOD(_FogFoldPattern,sampler_FogFoldPattern,coordA.xz/16,0).rg;
                    coordB.xz+=SAMPLE_TEXTURE2D_LOD(_FogFoldPattern,sampler_FogFoldPattern,coordB.xz/16,0).rg;
                    float3 curlA=curlPattern(coordA*2.6+float3(3,8,1))*.7+curlPattern(coordA.zxy*5.1+7).yzx*.3;
                    float3 curlB=curlPattern(coordB*2.6+float3(3,8,1))*.7+curlPattern(coordB.zxy*5.1+7).yzx*.3;
                    float nA=noise(coordA*float3(.7,2,2.1)+curlA*1.2)*.9+noise(coordA*7.3+curlA)*.1;
                    float nB=noise(coordB*float3(.7,2,2.1)+curlB*1.2)*.9+noise(coordB*7.3+curlB)*.1;
                    #if defined(FOG_CUMULUS)
                    float detailNoise=.5;
                    #else
                    float detailNoise=smoothstep(.22,.78,lerp(nB,nA,blend));
                    #endif
                    float3 offset=lerp(curlB,curlA,blend)*_FogWispDisplacement*(.55+activity*.45);
                    offset.y=offset.y*.4+(detailNoise-.5)*_FogWispDisplacement*1.8;
                    // Attenuate displacement at the floor to avoid a detached floating sheet.
                    offset*=smoothstep(0,.18,p.y-_FogMin.y);
                    #if defined(FOG_DRY_ICE)
                    offset*=.65+.35*activity;
                    #endif
                    detailQ=q+offset/_FogSize;
                    density=sampleDensity(detailQ);
                    #if defined(FOG_CUMULUS)
                    float shapeLod=clamp(log2(max(1,length(ray/_FogSize*_FogCumulusResolution.xyz)*stepSize)),0,_FogCumulusResolution.w);
                    float4 shapeA=SAMPLE_TEXTURE3D_LOD(_FogCumulusShape,sampler_LinearClamp,(flowA.xyz+offset)/_FogSize,0);
                    float4 shapeB=SAMPLE_TEXTURE3D_LOD(_FogCumulusShape,sampler_LinearClamp,(flowB.xyz+offset)/_FogSize,0);
                    shapeA.xy=SAMPLE_TEXTURE3D_LOD(_FogCumulusShape,sampler_LinearClamp,(flowA.xyz+offset)/_FogSize,shapeLod).rg;
                    shapeB.xy=SAMPLE_TEXTURE3D_LOD(_FogCumulusShape,sampler_LinearClamp,(flowB.xyz+offset)/_FogSize,shapeLod).rg;
                    // Filter each advected ribbon before blending: blending distances
                    // would invent a moving surface between the two coordinate maps.
                    float ribbonFilter=max(.003,stepSize*.5), width=_FogCumulusRibbonWidth;
                    float distanceA=shapeA.z*width, distanceB=shapeB.z*width;
                    float variance=width*width+ribbonFilter*ribbonFilter;
                    float coverageA=exp(-.5*distanceA*distanceA/variance)*width*width/variance*shapeA.w;
                    float coverageB=exp(-.5*distanceB*distanceB/variance)*width*width/variance*shapeB.w;
                    shapeA.x=max(shapeA.x,_FogCumulusWispOpacity*coverageA);
                    shapeB.x=max(shapeB.x,_FogCumulusWispOpacity*coverageB);
                    // Normalize each map before fading. Normalizing a blended pair cancels
                    // its fade when the other map is empty, revealing a reset map at full strength.
                    float ratioA=min(4,shapeA.x/max(.005,shapeA.y));
                    float ratioB=min(4,shapeB.x/max(.005,shapeB.y));
                    density*=lerp(ratioB,ratioA,blend);
                    density*=lerp(1,.6+_FogParams.z*1.6,1-smoothstep(.02,.3,density));
                    #else
                    // Erode only the thin fringe; never punch procedural holes through the shared layer.
                    float erosion=(1-detailNoise)*.04*(1-smoothstep(.05,.3,density));
                    density=max(0,density-erosion*_FogParams.z);
                    density*=lerp(1,.12+detailNoise*1.76,_FogParams.z);
                    #if defined(FOG_DRY_ICE)
                    // Carry broad contour folds through the authored body as well as the mist.
                    // The displacement stays below a billow radius and fades into the floor.
                    puff=samplePuffs(detailQ);
                    puff*=lerp(1,.55+detailNoise*.9,_FogParams.z);
                    #endif
                    #endif
                    #endif
                    density+=puff;
                    float edge=saturate(min(min(q.x,1-q.x)*_FogSize.x,min(q.z,1-q.z)*_FogSize.z)*2);
                    density*=edge;
                    if(density>.001)
                    {
                        #if defined(FOG_PLANAR)
                        float4 light=SAMPLE_TEXTURE3D_LOD(_FogPlanarLighting,sampler_LinearClamp,detailQ,0);
                        #else
                        float4 light=SAMPLE_TEXTURE3D_LOD(_FogLighting,sampler_FogLighting,detailQ,0);
                        #endif
                        float3 illumination=_FogAmbient.rgb*(.65+.35*saturate(q.y*3)) + _FogLightColor.rgb*(.05+light.a*.55)*phase + light.rgb;
                        #if defined(FOG_CUMULUS) || defined(FOG_DRY_ICE)
                        float powder=.55+.75*(1-exp(-density*2.5));
                        illumination=(_FogAmbient.rgb*(.7+.3*saturate(q.y*3)) + _FogLightColor.rgb*(.08+light.a*.65)*phase)*powder + light.rgb*3;
                        #endif
                        float opacity=1-exp(-density*_FogParams.x*stepSize);
                        float3 scattering=_FogAlbedo.rgb*illumination;
                        radiance+=transmittance*opacity*scattering;
                        transmittance*=1-opacity;
                        if(transmittance<.008) break;
                    }
                }
                return float4(radiance,transmittance);
            }

            TEXTURE2D(_FogLow); TEXTURE2D(_FogLowDepth);
            float4 _FogLowSize;
            float EyeDepth(float2 uv)
            {
                float depth=SampleSceneDepth(uv);
                if(unity_OrthoParams.w>0)
                {
                    #if UNITY_REVERSED_Z
                    depth=1-depth;
                    #endif
                    return lerp(_ProjectionParams.y,_ProjectionParams.z,depth);
                }
                return LinearEyeDepth(depth,_ZBufferParams);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 scene=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,input.texcoord);
                float4 fog=IntegrateFog(input);
                return half4(fog.rgb+fog.a*scene.rgb,scene.a);
            }
            struct FogOutput { float4 fog:SV_Target0; float depth:SV_Target1; };
            FogOutput FragLow(Varyings input)
            {
                FogOutput output; output.fog=IntegrateFog(input); output.depth=EyeDepth(input.texcoord); return output;
            }
            half4 FragResolve(Varyings input):SV_Target
            {
                float2 uv=input.texcoord, pixel=uv*_FogLowSize.xy-.5;
                float2 base=floor(pixel), fraction=frac(pixel);
                float depth=EyeDepth(uv), tolerance=max(.035,depth*.005);
                float4 fog=0; float weightSum=0;
                [unroll] for(int y=0;y<2;y++) [unroll] for(int x=0;x<2;x++)
                {
                    float2 tap=(clamp(base+float2(x,y),0,_FogLowSize.xy-1)+.5)*_FogLowSize.zw;
                    float z=SAMPLE_TEXTURE2D_LOD(_FogLowDepth,sampler_PointClamp,tap,0).r;
                    float weight=(x?fraction.x:1-fraction.x)*(y?fraction.y:1-fraction.y);
                    weight*=exp2(-abs(z-depth)/tolerance*4);
                    fog+=SAMPLE_TEXTURE2D_LOD(_FogLow,sampler_PointClamp,tap,0)*weight; weightSum+=weight;
                }
                // Missing matching-depth taps are evaluated exactly to preserve thin silhouettes.
                if(weightSum<.01) fog=IntegrateFog(input); else fog/=weightSum;
                half4 scene=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv);
                return half4(fog.rgb+fog.a*scene.rgb,scene.a);
            }
        ENDHLSL
        Pass
        {
            Name "Full resolution"
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local_fragment _ FOG_PROFILE_NO_DETAIL
            #pragma multi_compile_local_fragment _ FOG_CUMULUS FOG_DRY_ICE
            #pragma multi_compile_local_fragment _ FOG_PLANAR
            ENDHLSL
        }
        Pass
        {
            Name "Fog integration"
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment FragLow
            #pragma multi_compile_local_fragment _ FOG_PROFILE_NO_DETAIL
            #pragma multi_compile_local_fragment _ FOG_CUMULUS FOG_DRY_ICE
            #pragma multi_compile_local_fragment _ FOG_PLANAR
            ENDHLSL
        }
        Pass
        {
            Name "Depth-aware reconstruction"
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment FragResolve
            #pragma multi_compile_local_fragment _ FOG_PROFILE_NO_DETAIL
            #pragma multi_compile_local_fragment _ FOG_CUMULUS FOG_DRY_ICE
            #pragma multi_compile_local_fragment _ FOG_PLANAR
            ENDHLSL
        }
    }
}
