// The Victorian Kit's facade surface (Plans/VICTORIAN_STREETS_PLAN.md §4.2): one tiling set per slot, a tint and a
// second tint through the mask's G, roughness from the mask's R, cavity from its B, and world-space dirt from the
// height above the 0.60 m plinth, broken up by large noise, soot streaks running down the walls, and moss on up-facing
// surfaces (reference plan R3 step 2). Every material property
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
half _Streaks;
half4 _MossColor;
half _MossAmount;
half _NoiseScale;
half _ToneVar;
half _WorldUV;
CBUFFER_END

// The GPU Resident Drawer draws through DOTS instancing (VEILMOUTH_CITY_PLAN.md §29.7), which asks for one block of
// DOTS properties. No renderer overrides it, so _BaseColor stays the material's own, read once per instance as
// URP's Lit reads it.
#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

static float4 unity_DOTS_Sampled_BaseColor;

void SetupDOTSVicFacadeMaterialPropertyCaches()
{
    unity_DOTS_Sampled_BaseColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
}

#undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
#define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSVicFacadeMaterialPropertyCaches()
#define _BaseColor unity_DOTS_Sampled_BaseColor
#endif

TEXTURE2D(_MaskMap);

// 0 by day, 1 at night (VicStreetLighting). Not a material property.
float _VicNight;

// The ground under the walls (VicGround): the terrain's heightmap, where it lies, and its height scale. Level.z is 0
// when no ground is given, and the plinth is then measured from world height 0. Not material properties.
TEXTURE2D(_VicGroundHeight);
SAMPLER(sampler_vic_linear_clamp);
float4 _VicGroundRect;
float4 _VicGroundLevel;

// The ground's height under positionWS, m: the terrain's, or 0 with none.
float VicGroundY(float3 positionWS)
{
    if (_VicGroundLevel.z < 0.5)
    {
        return 0.0;
    }

    float2 uv = saturate((positionWS.xz - _VicGroundRect.xy) * _VicGroundRect.zw) + _VicGroundLevel.w;
    return _VicGroundLevel.x + SAMPLE_TEXTURE2D_LOD(_VicGroundHeight, sampler_vic_linear_clamp, uv, 0).r * _VicGroundLevel.y;
}

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

    // Dirt rises from the plinth on walls only: a pavement or a step is the ground itself (R3 step 2's fix).
    half wall = saturate(1.0h - abs(normalWS.y) * 3.0h);
    float above = positionWS.y - VicGroundY(positionWS) - 0.60;
    float rise = saturate(1.0 - above / max(_DirtHeight, 0.01)) * wall;
    float dirt = saturate(rise * rise * (0.55 + 0.9 * n) * _DirtAmount);
    dirt = max(dirt, saturate((n - 0.55) * 2.0) * _DirtAmount * 0.35);
    dirt = saturate(dirt + (1.0 - mask.b) * _DirtAmount * 0.45);
    albedo = lerp(albedo, albedo * _DirtColor.rgb * 2.0, dirt);
    rough = lerp(rough, max(rough, 0.9), dirt * 0.5);

    // Soot washed down the walls by rain (reference plan R3 step 2, "no wall reads clean"): noise stretched about
    // ten to one down the wall, streaks about 0.6 m wide and 4-6 m long, on upright faces only, patchy from house to
    // house by the large noise, and as strong as the material's dirt. A streak is a film of soot over the face, so it
    // greys the material toward a dark soot, flattening the brick's joints, rather than only darkening it. Sizes are
    // authoring defaults, in metres.
    float3 sp = float3(positionWS.x * 1.7, positionWS.y * 0.18, positionWS.z * 1.7);
    float s1 = VicValue3(sp) * 0.8 + VicValue3(sp * 2.3 + 5.0) * 0.2;
    half streak = saturate(saturate((s1 - 0.54) * 9.0) * wall * saturate(0.45 + n) * _DirtAmount * _Streaks);
    half3 soot = _DirtColor.rgb * 0.45h * (0.85h + 0.3h * (half)n2);
    albedo = lerp(albedo, soot, streak * 0.75h);
    rough = lerp(rough, max(rough, 0.85), streak * 0.5);

    half up = saturate((normalWS.y - 0.35) / 0.45);
    half low = saturate(1.0 - above / 0.6) * (1.0 - up);
    half moss = saturate((n2 - (1.0 - _MossAmount)) * 4.0) * saturate(max(up, low * 0.7));
    albedo = lerp(albedo, _MossColor.rgb * (0.8 + 0.4 * n), moss);
    rough = lerp(rough, 0.95, moss);
    normalTS = normalize(lerp(normalTS, half3(0, 0, 1), moss * 0.6));

    s.albedo = albedo;
    s.normalTS = normalTS;
    s.metallic = _Metallic * (1.0 - moss) * (1.0 - max(dirt, streak) * 0.5);
    s.smoothness = 1.0 - rough;
    s.occlusion = lerp(1.0h, mask.b, 0.8h);
}

#endif
