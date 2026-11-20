// The tiled detail removes material primarily at the edges of the cached broad banks.
// Camera rays and stellar shadow rays evaluate the same field.
Texture3D<float4> _Density;
Texture3D<float> _Detail;
SamplerState sampler_linear_clamp;
SamplerState sampler_linear_repeat;
float _Wisps, _CloudBody;
float _SurfaceDetail, _EdgeSoftness, _FoldRelief;
// Matches NebulaShape in NebulaStyleProfile.cs.
#define NEBULA_PAINTERLY 1
#define NEBULA_PLUMES 2
#define NEBULA_BACKLIT_BANK 3
#define NEBULA_SCULPTED_BANK 4
#define NEBULA_SWEPT_ARC 5
int _NebulaShape;
int _LayeredCurtain;
int _StructureWarp;
float _StructureWarpStrength;
float3 _WarpPhase;
float3 _FormShift;
int _DomainPattern;
float _DomainStrength;
float4 _Clearing;
float _ClearingSoftness;
float3 _DetailShift;

float3 shiftedFormPosition(float3 p)
{
    return p+(.23*_FormShift+.37*cross(_FormShift,p));
}

// Inverse mappings in volume space. The variable rotation preserves radial
// distance; successive shears keep the switchback continuous even at high strength.
float3 warpStructure(float3 p)
{
    float3 seed = _WarpPhase-.5;
    float2 pivot = float2(-.035,.105)+.085*seed.xy;
    float2 xy = p.xy-pivot;
    float z = p.z-.13;
    float strength = _StructureWarpStrength;
    if (_StructureWarp == 1)
    {
        float2 axis = z*float2(.22+.32*seed.x,-.14+.30*seed.y);
        float2 radial = xy-axis;
        float angle = strength*(4.8+seed.z)*exp(-dot(radial,radial)*27) + strength*.9*z;
        float s,c;
        sincos(angle,s,c);
        p.xy = pivot+axis+float2(c*radial.x-s*radial.y,s*radial.x+c*radial.y);
    }
    else
    {
        float phase = 1.7*seed.z;
        xy.x += strength*.126*sin(16*xy.y+3.7*z+phase);
        xy.y += strength*.075*sin(12*xy.x-4.8*z-phase);
        p.xy = pivot+xy;
    }
    return p;
}

float2 formationHash(float2 id)
{
    float3 h = frac(float3(id.xyx)*float3(.1031,.1030,.0973));
    h += dot(h,h.yzx+33.33);
    return frac((h.xx+h.yz)*h.zy);
}

// Two nearest irregular sites define a shared wall between adjacent pores.
float poreWall(float2 uv)
{
    float2 grid = uv/.020;
    float2 base = floor(grid), f = frac(grid);
    float first = 8, second = 8;
    [unroll] for (int y=-1;y<=1;y++)
    [unroll] for (int x=-1;x<=1;x++)
    {
        float2 offset = float2(x,y);
        float2 delta = offset+.14+.72*formationHash(base+offset+19)-f;
        float d = dot(delta,delta);
        second = min(second,max(first,d));
        first = min(first,d);
    }
    return .012*(sqrt(second)-sqrt(first));
}

// Inverse domain maps, evaluated before the density lookup on view and light
// rays. All coordinates are in box widths. The base cache contains every bank.
float3 patternCoordinates(float3 box)
{
    float3 q = box-.5;
    float3 local = q+.11*(_WarpPhase-.5);
    float3 uv = local*1.55+_DetailShift+.31;
    float patch = _Detail.SampleLevel(sampler_linear_repeat,uv,0)-.44;
    float fray = _Detail.SampleLevel(sampler_linear_repeat,uv.yzx*2.36+.74,0)-.44;
    float3 p = local+float3(.055*patch+.015*fray,-.043*patch+.014*fray,.030*patch);
    if (_DomainPattern==3) // Overturning billows: depth-sheared rolls in two planes.
    {
        float2 flow = float2(p.x+.22*p.z,p.y-.32*p.x-.25*p.z);
        float2 period = float2(.157,.146);
        float2 cell = floor(flow/period+.5);
        float2 delta = flow-period*cell;
        float2 seed = formationHash(cell+27);
        float falloff = saturate(1-dot(delta/period,delta/period)*4);
        float angle = _DomainStrength*(4.8+1.7*seed.x)*falloff*falloff;
        float s,c;
        sincos(angle,s,c);
        float2 bend = float2(c*delta.x-s*delta.y,s*delta.x+c*delta.y)-delta;
        return box+float3(bend.x,bend.y+.32*bend.x,.80*bend.y)
                  +_DomainStrength*.024*sin(34*p.y+16*p.z)*float3(.2,.3,1);
    }
    float displacement;
    float3 direction;
    if (_DomainPattern==4) // Broad shingle faces with steep, returning free lips.
    {
        float side = .74*p.x-.67*p.y+.034*sin(12*p.z);
        float row = .74*p.y+.67*p.x-.53*abs(side)-.035*sin(17*side)+.32*p.z;
        float phase = 6.283185*row/.045;
        displacement = .055*(sin(phase)+.48*sin(2*phase-.5)+.24*sin(3*phase-1));
        direction = float3(.62,-.51,1);
    }
    else if (_DomainPattern==5) // The fan opens and bifurcates at every depth.
    {
        float2 fan = p.xy-float2(-.26-.26*p.z,-.13+.32*p.z);
        float r = length(fan);
        float phase = 48*(atan2(fan.y,fan.x+.000001)+.95*r+.15*sin(10*r)+.36*p.z);
        float split = smoothstep(.09,.36,r);
        float blades = cos(phase)+.68*split*cos(2*phase+.9+2.2*patch);
        displacement = .059*blades*smoothstep(.008,.045,r);
        direction = float3(fan/max(r,.008)*.85,1);
    }
    else // Irregular cellular walls and pockets change section through depth.
    {
        float2 plane = float2(.86*p.x+.51*p.y,.86*p.y-.51*p.x);
        plane += p.z*float2(.085,-.065)+.030*float2(sin(13*p.y+1.8*p.z),sin(15*p.x-1.5*p.z));
        float wall = poreWall(plane);
        float thickness = .0025+.007*patch+.003*fray;
        displacement = lerp(.060,-.044,smoothstep(thickness-.0025,thickness+.0035,wall));
        direction = float3(.55+.33*sin(10*p.y),-.40+.45*sin(11*p.x),1);
    }
    return box+_DomainStrength*displacement*direction;
}

// Cross-sections are evaluated after interpolating the flow coordinates. Narrow
// threads can therefore bend through many voxels without acquiring a voxel-wide edge.
float strand(float2 flow, float2 frequency, float2 flowStep)
{
    float2 grid = flow*frequency;
    float2 cell = floor(grid);
    float2 offset = frac(cell*float2(.75488,.56984)+cell.yx*float2(.43829,.32191));
    float2 d = frac(grid)-(.28+.44*offset);
    // Convolve the cross-section with the footprint of one march step. Unresolved
    // threads approach their mean emission rather than flashing as the camera moves.
    float2 span = abs(flowStep)*frequency;
    float2 sharpness = float2(52,16);
    float2 variance = 1 + sharpness*span*span*.1155;
    float filtered = exp2(-dot(d*d,sharpness/variance))*rsqrt(variance.x*variance.y);
    float cellMean = .157; // Integral of the unfiltered Gaussian over a unit cell.
    return lerp(filtered*(.3+1.5*offset.x),cellMean,smoothstep(.5,1.6,max(span.x,span.y)));
}

bool usesSurfaceDetail()
{
    return _SurfaceDetail>0 && (_NebulaShape==NEBULA_SCULPTED_BANK || _NebulaShape==NEBULA_SWEPT_ARC ||
                                (_NebulaShape==NEBULA_BACKLIT_BANK && _LayeredCurtain));
}

float fineRelief(float3 p, float3 rayStep)
{
    float3 local = float3(p.x+.30*p.y,p.y-.23*p.z,p.z+.22*p.x);
    float3 uv = local*3.4+_DetailShift;
    float roll = _Detail.SampleLevel(sampler_linear_repeat,uv,0)-.45;
    float3 foldUV = local.yzx*float3(4.7,7.8,5.3)+_DetailShift+.38*roll*float3(1,-.8,.64);
    float fold = _Detail.SampleLevel(sampler_linear_repeat,foldUV,0)-.45;
    float tips = _Detail.SampleLevel(sampler_linear_repeat,foldUV.zxy*2.23+.31,0)-.45;
    float tipWeight = 1-smoothstep(.35,1,length(rayStep)*116);
    return clamp(.075*roll+.049*fold+.017*tips*tipWeight,-.039,.039);
}

float foldedDistance(float3 p, float distance, float amount)
{
    // Bend a family of short ridges in three dimensions. Lean their crests
    // over the adjacent cut, so a ray can pass through a lip and out into a pocket.
    float3 q = p + _DetailShift;
    float bend = _Detail.SampleLevel(sampler_linear_repeat,q*2.1+.21,0)-.48;
    float3 local = float3(q.x-.38*q.y,q.y+.34*q.x+.24*q.z,q.z-.25*q.y);
    float3 uv = local*float3(3.0,6.6,4.2)+bend*float3(.31,-.54,.26);
    float crest = _Detail.SampleLevel(sampler_linear_repeat,uv,0)-.44;
    float band = crest + 4.0*distance;
    float lip = 1-smoothstep(.035,.18,abs(band));
    float pocket = 1-smoothstep(.04,.20,abs(band+.22));
    float patches = lerp(.36,1,smoothstep(-.16,.08,bend));
    return distance+amount*patches*(.016*pocket-.012*lip);
}

float2 sculptedSurface(float3 p, float3 rayStep, float4 field)
{
    // The cached signed distance remains meaningful just beyond the old silhouette.
    float margin = max(.006,_EdgeSoftness);
    float folds = _FoldRelief*_SurfaceDetail*smoothstep(.32,.47,p.x)*smoothstep(.30,.41,p.y);
    float reach = margin+.039*_SurfaceDetail+.016*folds;
    if (field.z<=0 || field.w>reach) return 0;
    if (field.w < -reach) return field.xy;
    float relief = fineRelief(p,rayStep);
    float distance = field.w+_SurfaceDetail*relief;
    float edge = lerp(margin,.008,_SurfaceDetail);
    if (folds>0)
    {
        distance = foldedDistance(p,distance,folds);
        edge = lerp(edge,.006,folds);
    }
    float fraction = 1-smoothstep(-edge,edge,distance);
    float2 reshaped = fraction*field.z*float2(.45+.95*_CloudBody,.10);
    return lerp(field.xy,reshaped,smoothstep(0,.22,_SurfaceDetail));
}

float3 sampleSourceMedium(float3 p, float3 rayStep)
{
    float3 bankSample = p;
    float4 field = _Density.SampleLevel(sampler_linear_clamp,p,0);
    if (_NebulaShape==NEBULA_BACKLIT_BANK && _LayeredCurtain && _SurfaceDetail>0 && abs(field.w)<.09)
    {
        // Move dust and its emitting skin together along the local bank normal.
        const float e = 1.0/128;
        float3 grad = (float3(_Density.SampleLevel(sampler_linear_clamp,p+float3(e,0,0),0).w,
                             _Density.SampleLevel(sampler_linear_clamp,p+float3(0,e,0),0).w,
                             _Density.SampleLevel(sampler_linear_clamp,p+float3(0,0,e),0).w)-field.w)/e;
        float strength = _SurfaceDetail*lerp(.72,1,smoothstep(.55,.68,p.z))
                       *(1-smoothstep(.052,.09,abs(field.w)));
        float shift = strength*fineRelief(p,rayStep);
        bankSample -= grad*(shift/max(.65,dot(grad,grad)));
        field = _Density.SampleLevel(sampler_linear_clamp,bankSample,0);
        // Recover a narrow dust boundary after trilinear reconstruction. Scale
        // the existing dust so the envelope of the volume and its clearings hold.
        float softEdge = smoothstep(-.009,.037,field.w);
        float fineEdge = smoothstep(.002,.018,field.w);
        float contourWeight = strength*(1-smoothstep(.019,.040,abs(field.w-.012)));
        field.x *= lerp(1,(.12+fineEdge)/(.12+softEdge),contourWeight);
    }
    float2 body = field.xy;
    if (_NebulaShape==NEBULA_SCULPTED_BANK && _SurfaceDetail>0)
        body = sculptedSurface(p,rayStep,field);
    if (max(body.x,body.y) <= .001) return 0;
    if (_StructureWarp!=0 && (_NebulaShape==NEBULA_PAINTERLY || _NebulaShape==NEBULA_SWEPT_ARC))
    {
        // Broad dust, gas and tint already moved in the bake. Carry sub-voxel
        // detail through the same displacement without sampling the cache twice.
        float3 source = shiftedFormPosition(p-.5);
        p += warpStructure(source)-source;
    }
    if (_NebulaShape==NEBULA_SWEPT_ARC)
    {
        // Fold the texture along the inner wall of the bank at sub-voxel scale.
        float lip = field.w;
        if (abs(lip)<.074) lip += fineRelief(p,rayStep)*_Wisps*_SurfaceDetail*1.6;
        float dust = body.x*smoothstep(-.007,.007,lip);
        float skin = body.x*lerp(.12,1.2,field.z)*exp2(-pow(abs(lip+.004)/.007,2));
        float gas = body.y+skin;
        return float3(dust+.045*body.y,gas,field.z*skin/max(gas,.0001));
    }
    float3 q = float3(p.x+.45*p.y,p.y-.45*p.x,p.z)*float3(2.4,6.6,4.5);
    if (_NebulaShape==NEBULA_BACKLIT_BANK)
    {
        // The signed distance grows rapidly behind the curved bank. Using it
        // as a texture offset stretched the rear cloud detail into spokes.
        float3 detailUVW = p*5.1 + _DetailShift;
        float coarse = _Detail.SampleLevel(sampler_linear_repeat,detailUVW,0);
        float fine = _Detail.SampleLevel(sampler_linear_repeat,detailUVW.yzx*2.9+.31,0);
        float grain = _Wisps*(.72*coarse+.28*fine-.44);
        // Detail along the curtain stays coherent through depth. The signed bank
        // distance isolates narrow folds after trilinear density reconstruction.
        float3 sheet = float3(p.xy,p.z*.78)*6.4 + _DetailShift;
        float foldNoise = _Detail.SampleLevel(sampler_linear_repeat,sheet,0);
        float fold = field.w+.027+_Wisps*.049*(foldNoise-.45);
        float variance = 1;
        if (_LayeredCurtain && any(rayStep))
        {
            float across = _Density.SampleLevel(sampler_linear_clamp,bankSample+rayStep,0).w-field.w;
            // Filter a thin Gaussian lip over one camera sample interval.
            variance += 65000*.1155*across*across;
        }
        float ridge = exp2(-fold*fold*65000/variance)*rsqrt(variance)*smoothstep(.29,.64,foldNoise);
        // Confine cellular erosion to the bank edge. The curtain has its own
        // folded density field and should be continuous across its interior.
        float bankEdge = 1-smoothstep(.035,.11,abs(field.w));
        float grainContrast = lerp(.95,3.5,bankEdge);
        float threads = lerp(1,.27+1.7*smoothstep(.29,.64,foldNoise),bankEdge*_Wisps);
        float back = (1-smoothstep(-.18,-.06,field.w))*_Wisps;
        float gasBody = body.y;
        if (back>0)
        {
            // Displace the emitting fold slightly, so fine texture follows its
            // gradient and has little effect inside a uniform region of gas.
            float3 uv = float3(p.x+.10*p.z,p.y-.12*p.z,.25*p.z)*6.7+_DetailShift;
            float ripple = _Detail.SampleLevel(sampler_linear_repeat,uv+.21*coarse,0)-.45;
            float3 offset = float3(fine-.45,ripple,-.7*ripple)*(.052*back);
            gasBody = _Density.SampleLevel(sampler_linear_clamp,p+offset,0).y;
        }
        float ridgeLight = _LayeredCurtain ? 9-1.5*_SurfaceDetail*smoothstep(.55,.68,p.z) : 9;
        float gas = gasBody*max(.1,1-grainContrast*grain)*threads + ridgeLight*ridge*saturate(body.y*3);
        return float3(body.x*max(.15,1-2.5*grain),gas,field.z);
    }
    if (_NebulaShape==NEBULA_PLUMES || _NebulaShape==NEBULA_SCULPTED_BANK)
    {
        float3 q = p*4.7+_DetailShift;
        float detail = _Detail.SampleLevel(sampler_linear_repeat,q,0);
        float fine = _Detail.SampleLevel(sampler_linear_repeat,q.zxy*2.9+.37,0);
        float erosion = _Wisps*(.72*detail+.28*fine-.45);
        float margin = saturate(body.x*5)*(1-saturate(body.x*.68));
        return float3(max(0,body.x-erosion*margin*1.5),body.y,0);
    }
    if (_NebulaShape==NEBULA_PAINTERLY)
    {
        // Bend the fine structure along the coarse shell instead of repeating
        // a single projected grain direction across the frame.
        q = p*4.1 + float3(2.2,-1.8,.9)*field.w+_DetailShift;
        float detail = _Detail.SampleLevel(sampler_linear_repeat,q,0);
        float fine = _Detail.SampleLevel(sampler_linear_repeat,q.yzx*3.4+.51,0);
        float erosion = _Wisps*(.73*detail+.27*fine-.42);
        float fold = field.w + erosion*.042;
        float wall = exp2(-fold*fold*7900);
        float lip = fold+.025;
        float rim = exp2(-lip*lip*6700);
        float dust = max(0,body.x*(1-erosion*2.6)-erosion*.85*saturate(body.x*7));
        dust += wall*saturate(body.x*3)*2.8;
        float gas = body.y*max(.10,1-erosion*3.6)*(.82+rim*1.45);
        // Fine striations remain coherent through the thickness of the far
        // wall. Circular camera orbits see them from different angles.
        float3 tangent = float3(p.x+.17*p.z,p.y-.23*p.z,p.z*.13);
        float3 st = tangent*6.8+float3(.77,-.54,.14)*detail+_DetailShift;
        float grain = _Detail.SampleLevel(sampler_linear_repeat,st,0);
        float fleck = _Detail.SampleLevel(sampler_linear_repeat,st.yxz*2.7+.35,0);
        float vein = .73*grain+.27*fleck;
        float wallMask = exp2(-pow(abs((p.z-.69)/.14),2))*_Wisps;
        gas *= lerp(1,.30+1.85*smoothstep(.33,.64,vein),wallMask);
        dust += wallMask*min(body.y,1.5)*2.3*(1-smoothstep(.22,.44,vein))*saturate(body.x*6);
        return float3(dust,gas,field.z);
    }
    q += _DetailShift;
    float medium = _Detail.SampleLevel(sampler_linear_repeat,q,0);
    float fine = _Detail.SampleLevel(sampler_linear_repeat,q.zxy*3.1+.31,0);
    float lace = .7*medium+.3*fine;
    float erosion = _Wisps*(lace-.22);
    float2 flow = field.zw + _Wisps*.022*(float2(medium,fine)-.5);
    float2 flowStep = _Density.SampleLevel(sampler_linear_clamp,p+rayStep,0).zw-field.zw;
    float2 fork = flow+float2(.32*flow.y,.1*flow.x);
    float2 forkStep = flowStep+float2(.32*flowStep.y,.1*flowStep.x);
    float threads = strand(flow,float2(56,24),flowStep) + .65*strand(fork,float2(89,39),forkStep);
    float dust = max(0,body.x-erosion*lerp(.42,.84,_CloudBody))*
                 max(.15,1+_Wisps*(.50-lace)*lerp(1.5,3.6,_CloudBody));
    float strandResponse = lerp(.075+3.5*threads,.70+.35*threads,_CloudBody);
    float gas = max(0,body.y-erosion*.50)*lerp(.6,strandResponse,_Wisps);
    return float3(dust,gas,0);
}

float3 sampleMedium(float3 p,float3 rayStep)
{
    float3 medium = 0;
    if (_DomainPattern == 0)
    {
        medium = sampleSourceMedium(p,rayStep);
    }
    else
    {
        float3 edge = min(p,1-p);
        float fade = smoothstep(0,.075,min(edge.x,min(edge.y,edge.z)));
        if (_Clearing.w > 0)
            fade *= smoothstep(_Clearing.w,_Clearing.w+_ClearingSoftness,length(p-_Clearing.xyz));

        if (fade > 0)
        {
            float3 source = patternCoordinates(p);
            if (all(source >= 0) && all(source <= 1))
                medium = sampleSourceMedium(source,rayStep);
        }
        // Dust and emission fade in physical space. The third channel is a color weight.
        medium.xy *= fade;
    }
    return medium;
}

float fineDensity(float3 p) { return sampleMedium(p,0).x; }
