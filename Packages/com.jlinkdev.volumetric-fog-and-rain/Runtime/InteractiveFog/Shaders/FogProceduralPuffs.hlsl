// Stateless cell features: position, size and phase derive from cell/cycle seeds.
float4 _ProceduralLayout, _ProceduralTimes, _ProceduralQuiet, _ProceduralAmounts;
float procSmooth(float value) { value=saturate(value);return value*value*(3-2*value); }
float procRelease(float age,float rise,float settle) { return procSmooth((age-rise*.65)/(settle+rise*.35)); }
void proceduralField(float3 p,out float body,out float4 sources)
{
    body=0;sources=0;
    float spacing=_ProceduralLayout.x,time=_SimTime+_Dt;
    float2 drift=_Wind.xz*time;
    int2 home=(int2)floor((p.xz-_Origin.xz-drift)/spacing);
    [loop] for(int z=-2;z<=2;z++) [loop] for(int x=-2;x<=2;x++)
    {
        int2 cell=home+int2(x,z);
        float cellSeed=hash(float3(cell,_ProceduralQuiet.z))*4093;
        float rise=lerp(_ProceduralTimes.x,_ProceduralTimes.y,hash(float3(cellSeed,13,4)));
        float settle=lerp(_ProceduralTimes.z,_ProceduralTimes.w,hash(float3(cellSeed,31,7)));
        float quiet=lerp(_ProceduralQuiet.x,_ProceduralQuiet.y,hash(float3(cellSeed,17,8)));
        float lifetime=rise+settle,period=lifetime+quiet;
        float offset=hash(float3(cellSeed,43,2))*period;
        float cycle=floor((time+offset)/period),start=cycle*period-offset;
        // Include the previous cycle only when this step straddles its boundary.
        [loop] for(int previous=0;previous<2;previous++)
        {
            if(previous==1 && time-_Dt>=start) break;
            float seed=hash(float3(cellSeed,cycle-previous,81))*4093;
            if(hash(float3(seed,9,51))>=_ProceduralLayout.y) continue;
            float age=time-(start-previous*period);
            float radius=lerp(_ProceduralLayout.z,_ProceduralLayout.w,pow(hash(float3(seed,23,19)),1.5));
            float2 jitter=float2(hash(float3(seed,3,7)),hash(float3(seed,11,6)));
            float3 foot=float3(_Origin.x+(cell.x+.15+jitter.x*.7)*spacing+drift.x,_Origin.y+.02,_Origin.z+(cell.y+.15+jitter.y*.7)*spacing+drift.y);
            float3 delta=p-foot;
            if(any(abs(delta.xz)>radius*3.3)) continue;
            float release=procRelease(age,rise,settle);
            float released=max(0,release-procRelease(age-_Dt,rise,settle));
            float life=saturate(age/lifetime),grow=procSmooth(age/rise);
            float lowering=procSmooth((age-rise*.65)/(rise*.35+settle));
            if(previous==0 && age<lifetime)
            {
                float horizontal=radius*lerp(.3,1,grow)*lerp(1,1.8,procSmooth(life));
                float vertical=radius*lerp(.06,.9,grow)*lerp(1,.12,lowering);
                float3 centered=delta-float3(0,vertical*.75,0);
                if(all(abs(centered)<float3(horizontal,vertical,horizontal)*1.8))
                {
                    float3 q=centered/float3(horizontal,vertical,horizontal);
                    float angle=hash(float3(seed,7,19))*6.283185;
                    float2 direction=float2(cos(angle),sin(angle));
                    q.xz-=direction*(q.y*.16+sin(life*3.14159)*.18);
                    q+=curlPattern(q*1.3+seed+life*.65)*.075;
                    float crown=1-smoothstep(.58,1.04,length(q));
                    float3 skirtQ=(centered+float3(0,vertical*.62,0))/float3(horizontal*1.6,vertical*.4,horizontal*1.6);
                    float skirt=(1-smoothstep(.15,1,length(skirtQ)))*.3;
                    float d=max(crown,skirt)+min(crown,skirt)*.2;
                    float3 rolling=q+float3(direction.x,0,direction.y)*life*.6;
                    d*=.72+.28*noise(rolling*float3(1.8,3.5,1.8)+curlPattern(rolling+seed)*.65+seed);
                    body+=d*_ProceduralAmounts.x*grow*(1-release);
                }
            }
            if(released>0)
            {
                float spreadRadius=radius*lerp(1,1.8,release),height=max(.15,radius*.65);
                float distance=length(delta.xz),r=distance/spreadRadius;
                float footprint=exp(-2*r*r)*(1-smoothstep(1.5,1.8,r));
                float h=delta.y/height;
                float vertical=h>0&&h<1?6*h*(1-h)/height:0;
                sources.x+=released*_ProceduralAmounts.y*radius*radius*radius*footprint*vertical/(1.5707963*spreadRadius*spreadRadius);
                float releaseRate=released/max(.00001,_Dt);
                float force=footprint*(1-smoothstep(height,height*3,abs(delta.y)))*_ProceduralAmounts.z*releaseRate;
                sources.yz+=delta.xz/max(.1,distance)*force;
                sources.w=max(sources.w,footprint*releaseRate*.6);
            }
        }
    }
    sources.w=saturate(sources.w);
}
