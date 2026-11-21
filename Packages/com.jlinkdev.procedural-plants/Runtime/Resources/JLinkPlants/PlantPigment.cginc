#ifndef JLINK_PLANT_PIGMENT_INCLUDED
#define JLINK_PLANT_PIGMENT_INCLUDED
float3 PlantPigment(float3 baseColor, float2 uv, float2 pattern)
{
    float3 albedo = baseColor;
    float x = abs(2*uv.x-1);
    float y = uv.y;
    float seed = pattern.y * 19;
    if (pattern.x > 4.5 && pattern.x < 5.5)
    {
        float stripe = 1-smoothstep(.25,.36,x+.035*sin(uv.y*14+seed));
        return lerp(albedo,float3(.87,.88,.57),stripe*.87);
    }
    if (pattern.x > 7.5)
    {
        float shoulder = sin(3.14159*y) * (1-x*x);
        albedo = lerp(albedo*.94, albedo*float3(1.10,1.07,1.015), shoulder);
        float margin = smoothstep(.85,1,x) * smoothstep(.12,.55,y);
        return lerp(albedo,float3(.60,.35,.26),margin * (.24 + .28*pattern.y));
    }
    if (pattern.x > 5.5)
    {
        if (pattern.x > 6.5)
        {
            float side = uv.x < .5 ? -1 : 1;
            float phase = seed + side*2.7;
            float bias = side * .19*sin(seed*2.41+.7);
            float bay = .13*sin(y*8.1+phase) + .10*cos(y*14.5-phase*.71)
                      + .035*sin(y*35 + 2.4*sin(y*10+phase));
            float edge = x + bias + bay;
            float cream = smoothstep(.70,.77,edge);
            albedo = lerp(albedo,float3(.49,.59,.40),smoothstep(.56,.69,edge)*.70);
            albedo = lerp(albedo,float3(.86,.84,.66),cream);
            albedo = lerp(albedo,float3(.78,.58,.51),smoothstep(.91,.995,x)*cream*.38);
        }
        float rib = 1-smoothstep(.012,.042,x);
        return lerp(albedo,float3(.65,.57,.43),rib*.46);
    }
    if (pattern.x > .5)
    {
        float midrib = 1-smoothstep(.012,.046,x);
        float vein = abs(frac(y*8 - pow(x,1.3)*1.7 + .13*sin(y*15))-.5);
        float fine = (1-smoothstep(.025,.115,vein))*(1-x);
        float veinStrength = pattern.x < 1.5 ? .48 : 1;
        albedo += float3(.12,.13,.045)*(midrib+fine*.46)*veinStrength;
        if (pattern.x < 1.5)
        {
            float marbling = sin(y*15 + uv.x*9 + seed + 1.7*sin(uv.x*12-y*14+seed)) + .62*sin(x*15+y*22+seed);
            albedo = lerp(albedo,float3(.67,.71,.37),smoothstep(.45,1.54,marbling)*pattern.y*.58);
        }
        else if (pattern.x < 2.5)
        {
            float band = sin(y*115 + 2.1*sin(uv.x*11+seed) + .9*sin(uv.x*24-y*40));
            albedo += smoothstep(-.28,.65,band)*float3(.115,.135,.095);
            albedo = lerp(albedo,float3(.75,.72,.32),smoothstep(.83,.96,x));
        }
    }
    return albedo;
}
#endif
