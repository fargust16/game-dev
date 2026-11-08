// The Victorian Kit's facade surface (Plans/VICTORIAN_STREETS_PLAN.md §4.2): one tiling set per slot, a tint and a
// second tint through the mask's G, roughness from the mask's R, cavity from its B, and world-space dirt from the
// height above the 0.60 m plinth, broken up by large noise, with moss on up-facing surfaces. Every material property
// is in UnityPerMaterial, so the SRP Batcher keeps one batch; variants are material assets, never property blocks.
#ifndef VIC_FACADE_INPUT_INCLUDED
#define VIC_FACADE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
half4 _Tint2;
half _Tint2Amount;
half _Tint2Sharp;
half _Roughness;
half _Roughness2;
half _Metallic;
half _BumpScale;
half _Cutoff;
half4 _DirtColor;
half _DirtAmount;
half _DirtHeight;
half4 _MossColor;
half _MossAmount;
half _NoiseScale;
half _ToneVar;
half _WorldUV;
CBUFFER_END

TEXTURE2D(_MaskMap);

// 0 by day, 1 at night (VicStreetLighting). Not a material property.
float _VicNight;

float VicHash13(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.zyx + 31.32);
    return frac((p.x + p.y) * p.z);
}

float VicValue3(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = lerp(VicHash13(i), VicHash13(i + float3(1, 0, 0)), f.x);
    float b = lerp(VicHash13(i + float3(0, 1, 0)), VicHash13(i + float3(1, 1, 0)), f.x);
    float c = lerp(VicHash13(i + float3(0, 0, 1)), VicHash13(i + float3(1, 0, 1)), f.x);
    float d = lerp(VicHash13(i + float3(0, 1, 1)), VicHash13(i + float3(1, 1, 1)), f.x);
    return lerp(lerp(a, b, f.y), lerp(c, d, f.y), f.z);
}

float VicFbm3(float3 p)
{
    return VicValue3(p) * 0.5 + VicValue3(p * 2.03 + 7.1) * 0.3 + VicValue3(p * 4.01 + 3.3) * 0.2;
}

struct VicSurface
{
    half3 albedo;
    half3 normalTS;
    half metallic;
    half smoothness;
    half occlusion;
};

// The mesh UV in metres, or world XZ for ground planes, through the tiling.
float2 VicUV(float2 meshUV, float3 positionWS)
{
    float2 uv = lerp(meshUV, positionWS.xz, _WorldUV);
    return TRANSFORM_TEX(uv, _BaseMap);
}

half3 VicTint(half4 base, half4 mask, out half m2)
{
    m2 = saturate((mask.g - (1.0h - _Tint2Amount)) * _Tint2Sharp);
    return base.rgb * lerp(_BaseColor.rgb, _Tint2.rgb, m2);
}

void VicSurfaceAt(float2 uv, float3 positionWS, half3 normalWS, out VicSurface s)
{
    half4 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
    half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_BaseMap, uv);
    half m2;
    half3 albedo = VicTint(base, mask, m2);
    half rough = lerp(saturate(mask.r * _Roughness), _Roughness2, m2);
    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, uv), _BumpScale);

    // Large world-space variation: tone, dirt rising from the plinth, grime patches, moss.
    float3 p = positionWS * _NoiseScale;
    float n = VicFbm3(p);
    float n2 = VicFbm3(p * 2.7 + 17.0);
    albedo *= 1.0 + (n - 0.5) * _ToneVar;

    float above = positionWS.y - 0.60;
    float rise = saturate(1.0 - above / max(_DirtHeight, 0.01));
    float dirt = saturate(rise * rise * (0.55 + 0.9 * n) * _DirtAmount);
    dirt = max(dirt, saturate((n - 0.55) * 2.0) * _DirtAmount * 0.35);
    dirt = saturate(dirt + (1.0 - mask.b) * _DirtAmount * 0.45);
    albedo = lerp(albedo, albedo * _DirtColor.rgb * 2.0, dirt);
    rough = lerp(rough, max(rough, 0.9), dirt * 0.5);

    half up = saturate((normalWS.y - 0.35) / 0.45);
    half low = saturate(1.0 - above / 0.6) * (1.0 - up);
    half moss = saturate((n2 - (1.0 - _MossAmount)) * 4.0) * saturate(max(up, low * 0.7));
    albedo = lerp(albedo, _MossColor.rgb * (0.8 + 0.4 * n), moss);
    rough = lerp(rough, 0.95, moss);
    normalTS = normalize(lerp(normalTS, half3(0, 0, 1), moss * 0.6));

    s.albedo = albedo;
    s.normalTS = normalTS;
    s.metallic = _Metallic * (1.0 - moss) * (1.0 - dirt * 0.5);
    s.smoothness = 1.0 - rough;
    s.occlusion = lerp(1.0h, mask.b, 0.8h);
}

#endif
