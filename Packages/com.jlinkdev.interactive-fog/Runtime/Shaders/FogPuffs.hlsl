// Procedural billows release density and stirring into either fluid backend.
int _PuffPlanar, _DryIceEnabled;
Texture3D<float4> _ProceduralSources;
RWTexture3D<float4> _ProceduralSourcesOut;
Texture3D<float> _PuffDensity;
RWTexture3D<float> _PuffDensityOut;
float4 proceduralSource(float3 p) { return _ProceduralSources.SampleLevel(sampler_linear_clamp,(p-_Origin)/_Size,0); }
float puffActivity(float3 p)
{
    float activity=0;
    if(_DryIceEnabled!=0) activity=proceduralSource(p).w;
    return saturate(activity);
}
float puffDeposit(float3 p,bool planar)
{
    float density=0;
    if(_DryIceEnabled!=0)
    {
        if(!planar) density=proceduralSource(p).x;
        else for(int y=0;y<_Resolution.y;y++) density+=proceduralSource(float3(p.x,_Origin.y+(y+.5)*_Cell.y,p.z)).x*_Cell.y;
    }
    return density;
}
float3 puffForce(float3 p)
{
    float3 force=0;
    if(_DryIceEnabled!=0) { float2 radial=proceduralSource(p).yz;force=float3(radial.x,0,radial.y); }
    return force;
}
