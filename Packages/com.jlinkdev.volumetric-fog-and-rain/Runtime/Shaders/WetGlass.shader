Shader "jlinkdev/Volumetric Fog and Rain/Wet Glass"
{
 Properties
 {
  _Wetness("Wetness", Range(0,1))=0
  _RainTime("Time", Float)=0
  _Seed("Pane seed", Float)=0
  _RefractionPixels("Droplet lens displacement (pixels)", Range(0,100))=13
  _BackgroundBlur("Background softness (pixels)", Range(0,24))=3.2
  _DropDensity("Bead density", Range(0,4))=.72
  _DropSize("Droplet size multiplier", Range(.1,1))=.33
  _RunoffLanes("Runoff lanes", Range(2,64))=9
  _RunoffSpeed("Runoff speed", Range(.1,8))=1
  _StreakWidth("Streak width multiplier", Range(.1,12))=.7
  _StreakStrength("Streak strength", Range(0,4))=1
  _StreakLength("Streak length (reference pane heights)", Range(0,2))=.95
  _StreakFadeLength("Fade length (reference pane heights)", Range(0,2))=.65
  _PathWander("Runoff path wander", Range(0,3))=1
  _StreakHighlightWidth("Streak highlight width (pixels)", Range(0,12))=1.3
  _StreakHighlightStrength("Streak highlight brightness", Range(0,3))=.1
  _BeadLifetime("Bead lifetime (seconds; 0 freezes)", Range(0,600))=120
  _BeadLifetimeVariation("Bead lifetime variation", Range(0,.8))=.35
  _BeadBirthSeconds("Bead arrival time (seconds)", Range(.02,3))=.18
  [HideInInspector] _BeadTime("Bead replacement clock", Float)=0
  _PaneAspect("Pane width / height", Float)=.9
  _RainScale("Rain pattern scale (relative pane height)", Range(.05,4))=1
  [Toggle] _ObjectSpaceRain("Sample rain across object", Float)=0
  _ObjectRainBounds("Object rain bounds (min XY, size XY)", Vector)=(0,0,1,1)
  [Toggle] _UseMeshUV("Use authored normalized pane UV", Float)=0
  _RimStrength("Water edge glints", Range(0,2))=.14
  _DaylightHighlightBoost("Water highlights on bright backgrounds", Range(0,8))=0
  _EdgeContrast("Water shaded-edge contrast", Range(0,1))=.65
  _GlassGrimeMap("Glass grime atlas (R opacity, G variation)", 2D)="black" {}
  _GlassGrimeStrength("Glass edge grime", Range(0,1))=0
  _GlassGrimeColor("Glass grime color", Color)=(.11,.095,.067,1)
  _GlassGrimeGrid("Grime atlas columns / rows", Vector)=(3,3,0,0)
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+10" }
  Pass
  {
   Tags { "LightMode"="SRPDefaultUnlit" }
   // Preserve the destination's alpha silhouette, including antialiased edges.
   Blend One OneMinusSrcAlpha, Zero One
   ZWrite Off
   Cull Back
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   TEXTURE2D_X(_JlinkWetGlassSceneColor);
   SAMPLER(sampler_JlinkWetGlassSceneColor);
   TEXTURE2D(_JlinkWetGlassCaptureAvailable);
   TEXTURE2D(_GlassGrimeMap);
   SAMPLER(sampler_GlassGrimeMap);
   CBUFFER_START(UnityPerMaterial)
   float _Wetness, _RainTime, _Seed, _RefractionPixels, _BackgroundBlur;
   float _DropDensity, _RunoffLanes, _RunoffSpeed, _PaneAspect, _RimStrength, _DropSize;
   float _StreakWidth, _PathWander, _StreakStrength;
   float _UseMeshUV, _RainScale, _ObjectSpaceRain;
   float4 _ObjectRainBounds;
   float4 _GlassGrimeColor, _GlassGrimeGrid, _GlassGrimeMap_TexelSize;
   float _GlassGrimeStrength, _EdgeContrast, _DaylightHighlightBoost;
   float _StreakLength, _StreakFadeLength;
   float _StreakHighlightWidth, _StreakHighlightStrength, _BeadLifetime, _BeadLifetimeVariation, _BeadBirthSeconds, _BeadTime;
   CBUFFER_END
   float RainScale() { return max(.05,_RainScale); }

   struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float paneFace:TEXCOORD1; float2 rainUV:TEXCOORD2; };
   // Authored panes use unit cube/quad XY bounds [-.5,.5]. Cube face UVs flip vertically.
   // Local height keeps runoff downward on both faces and through vertical-axis hinges.
   // Exported hinge-relative panes carry normalized UVs with upward V on both faces.
   V Vert(float4 position:POSITION,float3 normal:NORMAL,float2 uv:TEXCOORD0)
   {
    V o; o.positionCS=TransformObjectToHClip(position.xyz);
    o.uv=lerp(position.xy+.5,uv,step(.5,_UseMeshUV));
    // Rain spans the entire moving object, independent of repeated pane UVs.
    // Keep pane-local UVs separately for edge fading and safe lens lookups.
    float2 objectUV=(position.xy-_ObjectRainBounds.xy)/max(_ObjectRainBounds.zw,float2(.001,.001));
    o.rainUV=lerp(o.uv,objectUV,step(.5,_ObjectSpaceRain));
    o.paneFace=step(.5,abs(normal.z));return o;
   }

   float Hash(float2 p)
   { return frac(sin(dot(p,float2(127.1,311.7))+_Seed*41.37)*43758.5453); }

   // Smooth deterministic value noise: no per-frame jitter or scrolling bead pattern.
   float Noise(float2 p)
   {
    float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),
                lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
   }

   // xy = lens slope, z = cap height, w = antialiased coverage.
   float4 Bead(float2 q, float visible)
   {
    q.x*=1+.16*clamp(q.y,-1,1); // bounded asymmetry, including outside the drop
    float radius=length(q);
    // Bound the filter support: steep/edge-on derivatives must not light remote pixels.
    float aa=clamp(fwidth(radius),.03,.8);
    float mask=(1-smoothstep(1-aa,1+aa,radius))*visible;
    float height=sqrt(saturate(1-radius*radius));
    float2 slope=q/max(1,radius);
    return float4(slope*mask,height*visible,mask);
   }

   float4 StaticBeads(float2 uv,float scale,float salt,float strength)
   {
    scale*=sqrt(max(1,_DropDensity)); // Above one increases count instead of saturating occupancy.
    float2 p=uv*float2(_PaneAspect,1)*RainScale();
    float angle=salt*2.399963;
    float2 rotated=float2(cos(angle)*p.x-sin(angle)*p.y,sin(angle)*p.x+cos(angle)*p.y);
    float2 warp=float2(Noise(p*5+salt),Noise(p*5+salt+47))-.5;
    float2 grid=rotated*scale+warp*1.4+float2(salt,salt*.71);
    float2 id=floor(grid);
    float chance=Hash(id+salt);
    float duration=max(1,_BeadLifetime*lerp(1-_BeadLifetimeVariation,1+_BeadLifetimeVariation,Hash(id+101)));
    float clock=_BeadTime/duration+Hash(id+137);
    float cycle=_BeadLifetime>0 ? floor(clock) : 0;
    float age=frac(clock)*duration;
    float living=_BeadLifetime>0 ? smoothstep(0,max(.02,_BeadBirthSeconds),age)
         *(1-smoothstep(duration*.92,duration*.97,age)) : 1;
    // Position changes only while the old bead is invisible, never sliding over the pane.
    float2 birthId=id+float2(cycle*37.7,cycle*19.3);
    float radius=lerp(.105,.30,pow(Hash(birthId+59),1.5))*_DropSize;
    float elongation=lerp(.9,1.35,Hash(birthId+71));
    // Wide scatter within each rotated/warped cell; radius-aware margins avoid cut beads.
    float2 margin=float2(radius,radius*elongation)+.015;
    float2 random=float2(Hash(birthId+13),Hash(birthId+37));
    float2 center=lerp(margin,1-margin,random);
    float2 q=(frac(grid)-center)/float2(radius,radius*elongation);
    float presence=smoothstep(chance-.06,chance+.06,min(1,_DropDensity)*_Wetness)*living;
    float4 bead=Bead(q,presence);
    // Rotate lens slopes back into pane axes.
    bead.xy=float2(cos(angle)*bead.x+sin(angle)*bead.y,-sin(angle)*bead.x+cos(angle)*bead.y)*strength;
    return bead;
   }

   float LaneX(float lane,float y,float seed)
   {
    y*=RainScale();
    float slow=Noise(float2(y*lerp(4,8,seed),seed*91))-.5;
    float fine=Noise(float2(y*lerp(15,24,seed),seed*57+19))-.5;
    return (lane+.22+seed*.56 + (slow*.30+fine*.12)*_PathWander)/max(.05,_RunoffLanes*RainScale());
   }

   float4 RunoffLane(float2 uv,float lane,float pixelWidth,out float cleared,out float highlight)
   {

    float seed=Hash(float2(lane,93));
    float rate=lerp(.022,.047,seed)*_RunoffSpeed/RainScale();
    float phase=frac(_RainTime*rate+seed*13.1);
    // Strictly downward motion. Resets occur beyond the pane, never by reversing.
    float travel=.4*phase+.6*phase*phase;
    float headY=1.18-1.6*travel;
    float pathSeed=Hash(float2(lane,floor(_RainTime*rate+seed*13.1)+123));
    float headX=LaneX(lane,headY,pathSeed);
    float radius=lerp(.009,.016,Hash(float2(lane,43)))*_DropSize/RainScale();
    float visible=smoothstep(seed*.65,seed*.65+.15,_Wetness);
    float4 head=Bead((uv-float2(headX,headY))/float2(radius/_PaneAspect,radius*1.35),visible);
    float path=LaneX(lane,uv.y,pathSeed);
    float width=lerp(.0012,.0024,seed)*_StreakWidth/(_PaneAspect*RainScale());
    float aa=max(pixelWidth,.0002);
    float dx=uv.x-path;
    float above=uv.y-headY;
    // Total trail length includes its fading tail; both are fractions of pane height.
    float trailLength=max(0,_StreakLength)/RainScale();
    float fadeLength=clamp(_StreakFadeLength/RainScale(),0,trailLength);
    float tail=fadeLength>.0001 ? 1-smoothstep(trailLength-fadeLength,trailLength,above)
                              : 1-step(trailLength,above);
    float lengthMask=smoothstep(.008/RainScale(),.035/RainScale(),above)*tail*step(.0001,trailLength);
    float fade=1-smoothstep(.94,1,phase);
    float streak=(1-smoothstep(width,width+aa,abs(dx)))*lengthMask*fade*visible;
    cleared=(1-smoothstep(width*2,width*2+aa*3,abs(dx)))*lengthMask*fade*visible;
    float footprint=pixelWidth;
    float halfHighlight=.5*_StreakHighlightWidth*footprint;
    highlight=(1-smoothstep(max(0,halfHighlight-.5*footprint),halfHighlight+.5*footprint,
        abs(dx+width*.45)))*lengthMask*fade*visible*saturate(_StreakStrength)*step(.001,_StreakHighlightWidth);
    streak*=_StreakStrength;cleared=saturate(cleared*_StreakStrength);
    float2 normal=float2(clamp(dx/max(width,.0001),-1,1),.04)*streak*.42;
    float4 trail=float4(normal,.1*streak,saturate(.65*streak));
    return lerp(trail,head,head.w);
   }

   float4 Merge(float4 a,float4 b)
   {
    float choose=b.w*(1-a.w*.35);
    return float4(lerp(a.xy,b.xy,choose),max(a.z,b.z),saturate(a.w+b.w*(1-a.w)));
   }

   float4 Runoff(float2 uv,out float cleared,out float highlight)
   {
    float lane=floor(uv.x*_RunoffLanes*RainScale());
    float4 water=0;cleared=0;highlight=0;
    float pixelWidth=max(length(float2(ddx(uv.x),ddy(uv.x))),.00001);
    // Larger wander can cross a lane boundary; evaluate its immediate neighbors too.
    [unroll] for(int offset=-1;offset<=1;offset++)
    {
     float candidate=lane+offset;
     float valid=step(0,candidate)*step(candidate,ceil(_RunoffLanes*RainScale())-1);
     float clearLane,highlightLane;
     float4 drop=RunoffLane(uv,candidate,pixelWidth,clearLane,highlightLane)*valid;
     clearLane*=valid;highlightLane*=valid;
     water=Merge(water,drop);
     cleared=max(cleared,clearLane);highlight=max(highlight,highlightLane);
    }
    return water;
   }

   half4 ReadScene(float2 uv)
   { return SAMPLE_TEXTURE2D_X(_JlinkWetGlassSceneColor,sampler_JlinkWetGlassSceneColor,saturate(uv)); }

   half4 ReadBehind(float2 uv,float surfaceDepth)
   {
    half4 color=ReadScene(uv);
    // Color filtering can straddle a foreground silhouette. Guard its whole footprint.
    float2 pixel=4.5/_ScaledScreenParams.xy;
    float4 depths=float4(SampleSceneDepth(saturate(uv+pixel)),
        SampleSceneDepth(saturate(uv-pixel)),
        SampleSceneDepth(saturate(uv+float2(pixel.x,-pixel.y))),
        SampleSceneDepth(saturate(uv+float2(-pixel.x,pixel.y))));
    #if UNITY_REVERSED_Z
     float depth=max(SampleSceneDepth(saturate(uv)),max(max(depths.x,depths.y),max(depths.z,depths.w)));
    #else
     float depth=min(SampleSceneDepth(saturate(uv)),min(min(depths.x,depths.y),min(depths.z,depths.w)));
    #endif
    #if UNITY_REVERSED_Z
     float valid=step(depth,surfaceDepth+.000001);
    #else
     float valid=step(surfaceDepth-.000001,depth);
    #endif
    return color*valid;
   }

   // Normalize by coverage so transparent background pixels never darken the glass.
   half3 SoftScene(float2 screen,float2 radius,float surfaceDepth,half3 fallback)
   {
    half4 sum=ReadBehind(screen,surfaceDepth)*4;
    sum+=ReadBehind(screen+float2(radius.x,0),surfaceDepth)*2;
    sum+=ReadBehind(screen-float2(radius.x,0),surfaceDepth)*2;
    sum+=ReadBehind(screen+float2(0,radius.y),surfaceDepth)*2;
    sum+=ReadBehind(screen-float2(0,radius.y),surfaceDepth)*2;
    sum+=ReadBehind(screen+radius,surfaceDepth);
    sum+=ReadBehind(screen-radius,surfaceDepth);
    sum+=ReadBehind(screen+float2(radius.x,-radius.y),surfaceDepth);
    sum+=ReadBehind(screen+float2(-radius.x,radius.y),surfaceDepth);
    return sum.a>.001 ? sum.rgb/sum.a : fallback;
   }

   half4 Frag(V i):SV_Target
   {
    if(i.paneFace<.5)return 0;
    half4 grime=0;
    if(_GlassGrimeStrength>.001)
    {
     // Position selects a unique pane chart; local UV retains dirt at each frame.
     // This is independent of the continuous rain field and its lens clipping.
     float2 grid=max(_GlassGrimeGrid.xy,1);
     float2 cell=min(floor(saturate(i.rainUV)*grid),grid-1);
     float2 margin=1.5*_GlassGrimeMap_TexelSize.xy*grid;
     float2 atlas=(cell+clamp(i.uv,margin,1-margin))/grid;
     half2 soil=SAMPLE_TEXTURE2D(_GlassGrimeMap,sampler_GlassGrimeMap,atlas).rg;
     half opacity=saturate(soil.r*_GlassGrimeStrength);
     grime=half4(_GlassGrimeColor.rgb*lerp(.7,1.3,soil.g)*opacity,opacity);
    }
    // Dry dirt uses ordinary premultiplied compositing; no stale scene capture.
    if(_Wetness<.001)return grime;
    // A Scene View, probe or second camera can see this pane without a glass capture.
    if(SAMPLE_TEXTURE2D_LOD(_JlinkWetGlassCaptureAvailable,sampler_PointClamp,float2(.5,.5),0).r<.5)return grime;
    float2 screen=i.positionCS.xy/_ScaledScreenParams.xy;
    half4 center=ReadScene(screen);
    if(center.a<.001)return grime;
    float edge=smoothstep(0,.012,min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y)));
    float cleared,streakHighlight;
    float4 runoff=Runoff(i.rainUV,cleared,streakHighlight);
    float4 water=StaticBeads(i.rainUV,61,17,.45);
    water=Merge(water,StaticBeads(i.rainUV,29,39,.8));
    water=Merge(water,StaticBeads(i.rainUV,17,63,1));
    water*=1-cleared*.9;
    water=Merge(water,runoff);

    // Project pane-space lens slopes into screen pixels; this follows an opening sash.
    float2 dx=ddx(i.uv),dy=ddy(i.uv);
    float det=dx.x*dy.y-dx.y*dy.x;
    float safeDet=abs(det)>.000000001?det:(det<0?-.000000001:.000000001);
    float2 right=normalize(float2(dy.y,-dx.y)/safeDet);
    float2 up=normalize(float2(-dy.x,dx.x)/safeDet);
    float2 offsetPixels=-(right*water.x+up*water.y)*_RefractionPixels;
    float2 paneDelta=dx*offsetPixels.x+dy*offsetPixels.y;
    // Keep lens lookups inside this pane rather than pulling in its wooden frame.
    paneDelta=clamp(i.uv+paneDelta,.008,.992)-i.uv;
    offsetPixels=float2(paneDelta.x*dy.y-paneDelta.y*dy.x,dx.x*paneDelta.y-dx.y*paneDelta.x)/safeDet;
    half4 lens=ReadBehind(screen+offsetPixels/_ScaledScreenParams.xy,i.positionCS.z);
    half3 lensColor=lerp(center.rgb,lens.rgb,saturate(lens.a));
    float radius=_BackgroundBlur*_Wetness*(1-cleared*.75);
    half3 soft=SoftScene(screen,float2(radius,radius)/_ScaledScreenParams.xy,i.positionCS.z,center.rgb);
    float rim=saturate(length(water.xy)) * water.w;
    float2 lightSlope=clamp(2*water.xy/max(water.w,.001),-1,1);
    float backgroundLuma=dot(center.rgb,half3(.2126,.7152,.0722));
    float brightBackground=smoothstep(.015,.16,backgroundLuma);
    // Strengthen existing highlights against pale scenery without expanding the
    // drop masks or trail widths. Dark backgrounds retain their authored glints.
    float highlightGain=1+_DaylightHighlightBoost*brightBackground;
    float glint=pow(saturate(lightSlope.y*.8-lightSlope.x*.65),3)*_RimStrength*highlightGain;
    float shadow=saturate(-water.y*.7+water.x*.5);
    lensColor=lensColor*(1-rim*.25*shadow)+glint;
    // Pale fog offers little structure to refract. Give the opposing side of
    // each curved drop a shaded edge, weighted by the captured background so
    // dark night glass keeps its existing glints rather than gaining a halo.
    float shadedEdge=pow(saturate(-lightSlope.y*.8+lightSlope.x*.65),1.5)
        *saturate(length(lightSlope));
    lensColor*=1-_EdgeContrast*brightBackground*shadedEdge;
    half3 result=lerp(soft,lensColor,water.w);
    result+=streakHighlight*_StreakHighlightStrength*highlightGain;
    float alpha=_Wetness*edge*center.a;
    return grime+half4(result*alpha,alpha)*(1-grime.a);
   }
   ENDHLSL
  }
 }
}
