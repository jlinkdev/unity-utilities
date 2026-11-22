// Floor-plane fluid backend. Each texel stores a complete fog column.
Texture2D<float4> _PVelocity, _PCoordA, _PCoordB;
RWTexture2D<float4> _PVelocityOut, _PCoordAOut, _PCoordBOut;
RWTexture3D<float4> _PLighting;
Texture3D<float> _PRestVolume;
int3 _PLightResolution;
Texture2D<float> _PMass, _PPressure;
RWTexture2D<float> _PMassOut, _PPressureOut, _PDivergence, _PSolid, _PRestMass, _PBudget;
RWTexture2D<float2> _PFlux;
Texture2D<float2> _PFluxRead;
int2 pr() { return _Resolution.xz; }
bool pi(int2 p) { return any(p<0)||any(p>=pr()); }
int2 pc(int2 p) { return clamp(p,0,pr()-1); }
float2 pu(int2 p) { return (p+.5)/(float2)pr(); }
float3 pw(int2 p) { return _Origin+float3(pu(p).x*_Size.x,_Fluid.z*.3,pu(p).y*_Size.z); }
bool pb(int2 p) { return pi(p)||_PSolid[pc(p)]>.5; }
float2 pv(int2 p) { return pb(p)?0:_PVelocity.Load(int3(pc(p),0)).xz; }
float pm(int2 p) { return pb(p)?0:_PMass.Load(int3(pc(p),0)); }
float pp(int2 p,int2 center) { return _PPressure.Load(int3(pb(p)?center:p,0)); }
float2 pbound(int2 p,float2 v)
{
    if(pb(p)) return 0;
    if(pb(p+int2(1,0))||pb(p-int2(1,0))) v.x=0;
    if(pb(p+int2(0,1))||pb(p-int2(0,1))) v.y=0;
    return clamp(v,-8,8);
}
float pactivity(int2 p)
{
    float activity=_PCoordA.Load(int3(p,0)).w;
    for(int i=0;i<_SourceCount;i++)
    {
        float speed=length(_Velocities[i].xyz);
        if(_Shapes[i].w<0 && speed<.01) speed=5;
        activity=max(activity,sourceWeight(pw(p),i)*saturate(speed*.5)*(_Shapes[i].w<0?1:saturate(_Shapes[i].z)));
    }
    return activity;
}
float restColumn(int2 p)
{
    float mass=0;
    for(int y=0;y<_Resolution.y;y++) mass+=_RestDensity[int3(p.x,y,p.y)];
    return mass*_Size.y/_Resolution.y;
}
[numthreads(8,8,1)] void PBuildRest(uint3 id:SV_DispatchThreadID)
{
    int2 p=id.xy;if(pi(p))return; _PRestMass[p]=restColumn(p);
}
[numthreads(8,8,1)] void PObstacles(uint3 id:SV_DispatchThreadID)
{
    int2 p=id.xy;if(pi(p))return;
    // Obstacles intersecting the lower bank block horizontal column flow.
    float3 pos=pw(p); _PSolid[p]=solidAt(pos);
}
[numthreads(8,8,1)] void PInitialize(uint3 id:SV_DispatchThreadID)
{
    int2 p=id.xy;if(pi(p))return; float3 pos=pw(p);
    float rest=restColumn(p);_PRestMass[p]=rest;_PSolid[p]=solidAt(pos);
    _PMassOut[p]=pb(p)?0:rest;_PPressureOut[p]=0;
    float2 v=_Wind.xz+broadFlow(pos.xz)*.18;_PVelocityOut[p]=float4(v.x,0,v.y,0);
    float4 coord=float4(pu(p).x*_Size.x,0,pu(p).y*_Size.z,0);
    _PCoordAOut[p]=coord;_PCoordBOut[p]=coord;
}
[numthreads(8,8,1)] void PAdvect(uint3 id:SV_DispatchThreadID)
{
    int2 p=id.xy;if(pi(p))return;
    float2 back=pu(p)-pv(p)*_Dt/_Size.xz;
    float2 v=_PVelocity.SampleLevel(sampler_linear_clamp,back,0).xz*exp(-_Fluid.y*_Dt);
    v=pbound(p,v);_PVelocityOut[p]=float4(v.x,0,v.y,0);
}
float pCurl(int2 p)
{
    return (pv(p+int2(1,0)).y-pv(p-int2(1,0)).y)/(2*_Cell.x)
        -(pv(p+int2(0,1)).x-pv(p-int2(0,1)).x)/(2*_Cell.z);
}
[numthreads(8,8,1)] void PForces(uint3 id:SV_DispatchThreadID)
{
    int2 p=id.xy;if(pi(p))return;float3 pos=pw(p);float2 v=pv(p);
    float2 eta=float2(abs(pCurl(pc(p+int2(1,0))))-abs(pCurl(pc(p-int2(1,0)))),abs(pCurl(pc(p+int2(0,1))))-abs(pCurl(pc(p-int2(0,1)))))/(2*_Cell.xz);
    eta/=length(eta)+.0001;
    v+=float2(eta.y,-eta.x)*pCurl(p)*_Fluid.x*_Dt*min(_Cell.x,_Cell.z);
    float2 target=_Wind.xz+broadFlow(pos.xz)*.18;
    v+=(target-v)*.12*_Dt;
    float activity=pactivity(p),quiet=1-smoothstep(.02,.2,activity);
    float blend=1-abs(frac(_SimTime/6)*2-1);
    float2 coord=lerp(_PCoordB.Load(int3(p,0)).xz,_PCoordA.Load(int3(p,0)).xz,blend);
    v+=broadFlow(coord*6.283185/_Turbulence.y)*(.02+activity*.85)*_Turbulence.x*_Dt;
    v=lerp(v,target,1-exp(-_RestRecovery*2*quiet*_Dt));
    for(int i=0;i<_SourceCount;i++)
    {
        float w=sourceWeight(pos,i);float2 force=_Velocities[i].xz;float speed=length(force);
        if(_Shapes[i].w>=0)
        {
            float2 direction=force/max(speed,.001),side=float2(-direction.y,direction.x);
            float radius=max(_Shapes[i].x*3.5,.3);
            float2 behind=_Sources[i].xz-direction*radius*.85;
            float vertical=1-smoothstep(_Shapes[i].y+radius*.3,_Shapes[i].y+radius*1.5,abs(pos.y-_Sources[i].y));
            for(int sign=-1;sign<=1;sign+=2)
            {
                float2 offset=(pos.xz-behind-side*radius*.7*sign)/radius;
                v+=float2(-offset.y,offset.x)*sign*exp(-dot(offset,offset))*speed*vertical*_Dt*2.7*saturate(_Shapes[i].z);
            }
            float2 outward=pos.xz-_Sources[i].xz;outward/=max(length(outward),.08);
            force=force*.65+outward*speed*.18;w*=saturate(speed*2);
        }
        if(_Shapes[i].w<0 && length(_Velocities[i].xyz)<.01) force=normalize(pos.xz-_Sources[i].xz+float2(.001,0))*5;
        v+=(force-v)*w*(1-exp(-_Shapes[i].z*_Dt*4));
    }
    v+=puffForce(pos).xz*_Dt;
    v=pbound(p,v);_PVelocityOut[p]=float4(v.x,0,v.y,0);
}
[numthreads(8,8,1)] void PDivergence(uint3 id:SV_DispatchThreadID)
{
    int2 p=id.xy;if(pi(p))return;
    _PDivergence[p]=pb(p)?0:(pv(p+int2(1,0)).x-pv(p-int2(1,0)).x)/(2*_Cell.x)+(pv(p+int2(0,1)).y-pv(p-int2(0,1)).y)/(2*_Cell.z);
    _PPressureOut[p]=0;
}
[numthreads(8,8,1)] void PPressure(uint3 id:SV_DispatchThreadID)
{
    int2 p=id.xy;if(pi(p))return;float2 w=1/(_Cell.xz*_Cell.xz);
    float sum=(pp(p+int2(1,0),p)+pp(p-int2(1,0),p))*w.x+(pp(p+int2(0,1),p)+pp(p-int2(0,1),p))*w.y;
    _PPressureOut[p]=pb(p)?0:(sum-_PDivergence[p])/(2*(w.x+w.y));
}
[numthreads(8,8,1)] void PProject(uint3 id:SV_DispatchThreadID)
{
    int2 p=id.xy;if(pi(p))return;
    float2 grad=float2(pp(p+int2(1,0),p)-pp(p-int2(1,0),p),pp(p+int2(0,1),p)-pp(p-int2(0,1),p))/(2*_Cell.xz);
    float2 v=pbound(p,pv(p)-grad);_PVelocityOut[p]=float4(v.x,0,v.y,0);
}
float pslope(int2 p,int2 axis)
{
    float c=pm(p),a=c-pm(p-axis),b=pm(p+axis)-c;
    return a*b<=0?0:sign(a)*min(.5*abs(a+b),min(2*abs(a),2*abs(b)));
}
float pflux(int2 p,int2 axis,float spacing)
{
    if(pb(p)||pb(p+axis))return 0;
    float speed=dot((pv(p)+pv(p+axis))*.5,(float2)axis);int2 donor=speed>=0?p:p+axis;
    return speed*max(0,pm(donor)+sign(speed)*.5*pslope(donor,axis)*saturate(1-abs(speed)*_Dt/spacing))/spacing;
}
float pf(int2 p,int axis) { return pi(p)?0:_PFluxRead.Load(int3(p,0))[axis]; }
[numthreads(8,8,1)] void PFlux(uint3 id:SV_DispatchThreadID)
{
    int2 p=id.xy;if(pi(p))return;_PFlux[p]=float2(pflux(p,int2(1,0),_Cell.x),pflux(p,int2(0,1),_Cell.z));
}
[numthreads(8,8,1)] void PBudget(uint3 id:SV_DispatchThreadID)
{
    int2 p=id.xy;if(pi(p))return;
    float outgoing=max(0,pf(p,0))-min(0,pf(p-int2(1,0),0))+max(0,pf(p,1))-min(0,pf(p-int2(0,1),1));
    _PBudget[p]=min(1,pm(p)/max(1e-8,_Dt*outgoing));
}
float pbf(int2 p,int2 axis,int index)
{
    float f=pf(p,index);int2 donor=f>=0?p:p+axis;return f*_PBudget[pc(donor)];
}
[numthreads(8,8,1)] void PDensity(uint3 id:SV_DispatchThreadID)
{
    int2 p=id.xy;if(pi(p))return;
    float net=pbf(p,int2(1,0),0)-pbf(p-int2(1,0),int2(1,0),0)+pbf(p,int2(0,1),1)-pbf(p-int2(0,1),int2(0,1),1);
    float d=max(0,pm(p)-_Dt*net),rest=_PRestMass[p];
    float edge=min(min(pu(p).x,1-pu(p).x)*_Size.x,min(pu(p).y,1-pu(p).y)*_Size.z);
    d=lerp(d,max(d,rest),1-exp(-_Fluid.w*(1-smoothstep(.3,1,edge))*_Dt));
    float quiet=1-smoothstep(.02,.2,max(pactivity(p),puffActivity(pw(p))));
    d=lerp(d,rest,1-exp(-_RestRecovery*quiet*_Dt));
    for(int i=0;i<_SourceCount;i++) if(_Shapes[i].w>0)
    {
        float3 q=pw(p)-_Sources[i].xyz;q.y-=clamp(q.y,0,_Shapes[i].y);
        float core=1-smoothstep(_Shapes[i].x*.6,_Shapes[i].x*1.15,length(q));d*=exp(-core*_Shapes[i].w*_Dt*18);
    }
    d+=puffDeposit(pw(p),true);
    _PMassOut[p]=pb(p)?0:max(0,d);
}
[numthreads(8,8,1)] void PCoordinates(uint3 id:SV_DispatchThreadID)
{
    int2 p=id.xy;if(pi(p))return;float2 back=pu(p)-pv(p)*_Dt/_Size.xz;
    float4 rest=float4(pu(p).x*_Size.x,0,pu(p).y*_Size.z,0);
    float4 a=_ResetCoordinates.x!=0?rest:_PCoordA.SampleLevel(sampler_linear_clamp,back,0);
    float4 b=_ResetCoordinates.y!=0?rest:_PCoordB.SampleLevel(sampler_linear_clamp,back,0);
    float activity=_PCoordA.SampleLevel(sampler_linear_clamp,back,0).w*exp(-_Dt*.65);
    for(int i=0;i<_SourceCount;i++)
    {
        float speed=length(_Velocities[i].xyz);if(_Shapes[i].w<0&&speed<.01)speed=5;
        activity=max(activity,sourceWeight(pw(p),i)*saturate(speed*.5)*(_Shapes[i].w<0?1:saturate(_Shapes[i].z)));
    }
    activity=max(activity,puffActivity(pw(p)));
    a.w=b.w=activity;_PCoordAOut[p]=a;_PCoordBOut[p]=b;
}
float pDensityAtWorld(float3 worldPos)
{
    float3 q=(worldPos-_Origin)/_Size;if(any(q<0)||any(q>1))return 0;
    float column=_PMass.SampleLevel(sampler_linear_clamp,q.xz,0);
    float rest=_PRestMass[pc((int2)(q.xz*pr()))];
    float d=_PRestVolume.SampleLevel(sampler_linear_clamp,q,0)*min(3,column/max(.001,rest));
    if(_Cumulus.x>.5 && _Cumulus.x<1.5)
    {
        float3 a=_PCoordA.SampleLevel(sampler_linear_clamp,q.xz,0).xyz,b=_PCoordB.SampleLevel(sampler_linear_clamp,q.xz,0).xyz;
        a.y=b.y=q.y*_Size.y;
        d*=cumulusDetailRatio(a,b);
    }
    return d+(_Cumulus.x>1.5?_PuffDensity.SampleLevel(sampler_linear_clamp,q,0):0);
}
[numthreads(8,4,8)] void PLight(uint3 id:SV_DispatchThreadID)
{
    if(any(id>=(uint3)_PLightResolution))return;
    float3 uvw=(id+.5)/(float3)_PLightResolution,pos=_Origin+uvw*_Size,probe=pos;float optical=0;bool sunBlocked=false;
    for(int i=0;i<12;i++)
    {
        probe+=_LightDirection*.16;
        if(any(probe<_Origin)||any(probe>_Origin+_Size))break;
        if(solidAt(probe)>.5){optical+=8;sunBlocked=true;break;}
        optical+=pDensityAtWorld(probe)*.16*_Extinction;
    }
    float3 local=0;
    for(int j=0;j<_LocalLightCount;j++)
    {
        float3 delta=_LocalLightPositions[j].xyz-pos;float d=length(delta);
        float attenuation=pow(saturate(1-d/_LocalLightPositions[j].w),2)/(1+d*d);if(attenuation<=0)continue;
        float transmission=1;
        for(int s=1;s<=4;s++)
        {
            float3 q=pos+delta*(s/5.0);if(solidAt(q)>.5){transmission=0;break;}
            transmission*=exp(-pDensityAtWorld(q)*d*.2*_Extinction*.6);
        }
        if(_Cumulus.x>.5) transmission=.35*transmission+.65*pow(max(0,transmission),.15);
        local+=_LocalLightColors[j].rgb*attenuation*transmission;
    }
    float directional=exp(-optical);
    // Approximate diffuse multiple scattering without another shadow march.
    if(_Cumulus.x>.5) directional=sunBlocked?0:.35*directional+.65*exp(-optical*.15);
    _PLighting[id]=float4(local,directional);
}
