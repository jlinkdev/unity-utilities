#include "PlantPigment.cginc"

sampler2D _BaseMap, _BumpMap, _MaskMap;
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _Color;
    float _BotanicalColor, _NormalStrength, _Roughness, _Metallic, _Occlusion;
CBUFFER_END

void ReadPlantSurface(float2 uv, float2 pattern, float3 color, out half3 albedo,
    out half3 normalTS, out half occlusion, out half smoothness, out half metallic)
{
    float2 mapped = uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
    float3 pigment = PlantPigment(color, uv, pattern);
    #ifndef UNITY_COLORSPACE_GAMMA
        pigment = PLANT_TO_LINEAR(pigment);
    #endif
    albedo = lerp(1, pigment, _BotanicalColor) * _Color.rgb * tex2D(_BaseMap, mapped).rgb;
    normalTS = PLANT_UNPACK_NORMAL(tex2D(_BumpMap, mapped), _NormalStrength);
    if (pattern.x > 7.5) normalTS = normalize(lerp(half3(0,0,1), normalTS, .20));
    else if (pattern.x > .5 && pattern.x < 1.5) normalTS = normalize(lerp(half3(0,0,1),normalTS,.70));
    half3 mask = tex2D(_MaskMap, mapped).rgb;
    occlusion = lerp(1, mask.r, _Occlusion);
    half roughnessScale = pattern.x > 7.5 ? .93 : (pattern.x > 5.5 ? .70 : 1);
    smoothness = 1 - saturate(mask.g * _Roughness * roughnessScale);
    metallic = mask.b * _Metallic;
}
