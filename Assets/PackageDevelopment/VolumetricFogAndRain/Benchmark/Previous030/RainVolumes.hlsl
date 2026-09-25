#ifndef JLINKDEV_RAIN_VOLUMES_INCLUDED
#define JLINKDEV_RAIN_VOLUMES_INCLUDED
#define RAIN_MAX_INTERVALS 12
#if defined(_RAIN_VOLUMES)
float4x4 _RainBoxes[4];
float4x4 _RainDryBoxes[8];
int _RainBoxCount, _RainDryBoxCount, _RainBounded;

// Transform directions without normalizing: interval distances remain world-space metres.
bool RainBoxInterval(float3 origin, float3 ray, float end, float4x4 worldToBox, out float2 interval)
{
    float3 o = mul(worldToBox,float4(origin,1)).xyz;
    float3 d = mul(worldToBox,float4(ray,0)).xyz;
    interval = float2(0,end);
    [unroll] for (int axis=0;axis<3;axis++)
    {
        if (abs(d[axis]) < 1e-8)
        {
            if (abs(o[axis]) > 0.5) return false;
        }
        else
        {
            float a = (-0.5-o[axis])/d[axis], b = (0.5-o[axis])/d[axis];
            interval.x = max(interval.x,min(a,b));
            interval.y = min(interval.y,max(a,b));
        }
    }
    return interval.y > interval.x;
}

int RainWetIntervals(float3 origin, float3 ray, float end, out float2 result[RAIN_MAX_INTERVALS])
{
    float2 wet[4], dry[8], merged[4];
    int wetCount=0, dryCount=0, mergedCount=0, count=0;
    if (_RainBounded == 0) { wet[0]=float2(0,end); wetCount=1; }
    else
    {
        [loop] for (int i=0;i<_RainBoxCount;i++)
        {
            float2 hit;
            if (RainBoxInterval(origin,ray,end,_RainBoxes[i],hit))
            {
                int at=wetCount;
                [unroll] for(int scan=0;scan<4;scan++) { if(at==0)break; if(wet[at-1].x<=hit.x)break; wet[at]=wet[at-1]; at--; }
                wet[at]=hit; wetCount++;
            }
        }
    }
    if(wetCount==0)return 0;
    [loop] for(int j=0;j<_RainDryBoxCount;j++)
    {
        float2 hit;
        if(RainBoxInterval(origin,ray,end,_RainDryBoxes[j],hit))
        {
            int at=dryCount;
            [unroll] for(int scan=0;scan<8;scan++) { if(at==0)break; if(dry[at-1].x<=hit.x)break; dry[at]=dry[at-1]; at--; }
            dry[at]=hit; dryCount++;
        }
    }
    // Union rain intervals first so overlapping boxes cannot brighten the rain twice.
    float2 current=wet[0];
    [loop] for(int k=1;k<wetCount;k++)
    {
        if(wet[k].x<=current.y)current.y=max(current.y,wet[k].y);
        else { merged[mergedCount++]=current; current=wet[k]; }
    }
    merged[mergedCount++]=current;
    // Subtract sorted dry intervals. Four wet boxes minus eight dry boxes need <=12 intervals.
    [loop] for(int m=0;m<mergedCount;m++)
    {
        float cursor=merged[m].x, stop=merged[m].y;
        [loop] for(int n=0;n<dryCount;n++)
        {
            if(dry[n].x>=stop)break;
            if(dry[n].y<=cursor)continue;
            if(dry[n].x>cursor)result[count++]=float2(cursor,min(stop,dry[n].x));
            cursor=max(cursor,dry[n].y);
            if(cursor>=stop)break;
        }
        if(cursor<stop)result[count++]=float2(cursor,stop);
    }
    return count;
}
#endif
#endif
