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
half _Puddles;
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

// The eaves map (VicGround): each house's wall top seen from above, an 8-bit texel of Level.x + texel × Level.y m,
// 0 where none stands; Rect.z is 0 when none is given. Not material properties.
TEXTURE2D(_VicEaves);
SAMPLER(sampler_vic_point_clamp);
float4 _VicEavesRect;
float4 _VicEavesLevel;

// The top of the wall a facade belongs to, m, read 0.75 m inside it; 0 where no house stands or no map is given.
float VicEaves(float3 positionWS, float3 normalWS)
{
    if (_VicEavesRect.z <= 0.0)
    {
        return 0.0;
    }

    float2 uv = (positionWS.xz - normalWS.xz * 0.75 - _VicEavesRect.xy) * _VicEavesRect.zw;
    if (any(uv < 0.0) || any(uv > 1.0))
    {
        return 0.0;
    }

    float t = SAMPLE_TEXTURE2D_LOD(_VicEaves, sampler_vic_point_clamp, uv, 0).r;
    return t > 0.0 ? _VicEavesLevel.x + t * _VicEavesLevel.y : 0.0;
}

// The sill map (VicGround): each column of windows seen from above, its lowest, second and highest sill in R, G and B,
// each Level.x + texel × Level.y m, 0 for none, and how much of the texel it covers in A; above the second sill the
// sills stand Level.z apart, one kit storey. Rect.z is 0 when none is given. Not material properties.
TEXTURE2D(_VicSills);
float4 _VicSillsRect;
float4 _VicSillsLevel;

// How far a facade point lies under the nearest sill over it, m, read 0.75 m inside its wall, and how much of its texel
// the window's column covers; -1 where no sill stands over it or no map is given.
float VicSillDepth(float3 positionWS, float3 normalWS, out half cover)
{
    cover = 0.0h;
    if (_VicSillsRect.z <= 0.0)
    {
        return -1.0;
    }

    float2 uv = (positionWS.xz - normalWS.xz * 0.75 - _VicSillsRect.xy) * _VicSillsRect.zw;
    if (any(uv < 0.0) || any(uv > 1.0))
    {
        return -1.0;
    }

    float4 t = SAMPLE_TEXTURE2D_LOD(_VicSills, sampler_vic_point_clamp, uv, 0);
    cover = (half)SAMPLE_TEXTURE2D_LOD(_VicSills, sampler_vic_linear_clamp, uv, 0).a;
    if (t.r <= 0.0 || cover <= 0.0h)
    {
        return -1.0;
    }

    float3 sills = _VicSillsLevel.x + t.rgb * _VicSillsLevel.y;
    float sill = sills.x;
    if (positionWS.y > sills.x)
    {
        if (t.g <= 0.0)
        {
            return -1.0;
        }

        sill = sills.y + max(ceil((positionWS.y - sills.y) / _VicSillsLevel.z - 0.001), 0.0) * _VicSillsLevel.z;
    }

    return sill <= sills.z + 0.2 ? sill - positionWS.y : -1.0;
}

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
    // Softer edges and a lighter film since the owner's play test of 2026-11-11 (the runs' outlines stood out).
    half streak = saturate(saturate((s1 - 0.53) * 5.0) * wall * saturate(0.45 + n) * _DirtAmount * _Streaks);
    half3 soot = _DirtColor.rgb * 0.45h * (0.85h + 0.3h * (half)n2);
    albedo = lerp(albedo, lerp(albedo * 0.6h, soot, 0.5h), streak * 0.6h);
    rough = lerp(rough, max(rough, 0.85), streak * 0.5);

    // Soot gathered under the cornice (R3 step 2): darkest at the wall's top, fading over 3 m down it and over 1.5 m
    // up the gables and stacks above it, as strong as the streaks. Sizes are authoring defaults, in metres.
    float eaves = VicEaves(positionWS, (float3)normalWS);
    float under = eaves - positionWS.y;
    float gather = under >= 0.0 ? saturate(1.0 - under / 3.0) : saturate(1.0 + under / 1.5);
    half cornice = eaves > 0.0 ? (half)(gather * gather) * wall * saturate(0.6h + 0.8h * (half)n) * _DirtAmount * _Streaks : 0.0h;
    albedo = lerp(albedo, soot, saturate(cornice) * 0.7h);
    rough = lerp(rough, max(rough, 0.85), saturate(cornice) * 0.5);

    // Stains under the sills (R3 step 2): rain running off each sill carries soot down the wall under its window,
    // darkest under the stone, fading over 1.4 m and narrowing as it runs, and breaking into drips of about 0.2 m
    // further down. Sizes are authoring defaults, in metres. The owner's play test of 2026-11-11 found them too loud,
    // their outlines and colour standing out: so they are as strong as the material's dirt, not twice, their edges and
    // drips soft, and their tone the wall's own darkened, with a little of the soot.
    half cover;
    float sillDepth = VicSillDepth(positionWS, (float3)normalWS, cover);
    if (sillDepth >= 0.0)
    {
        float run = saturate(sillDepth / 1.4);
        half narrow = smoothstep(0.05h + 0.4h * (half)run, 0.8h + 0.2h * (half)run, cover);
        float3 dp = float3(positionWS.x * 5.0, positionWS.y * 0.5, positionWS.z * 5.0);
        half drip = saturate(((half)VicValue3(dp) - 0.1h - 0.45h * (half)run) * 1.4h);
        half stain = saturate((half)(1.0 - run) * (half)(1.0 - run) * narrow * drip * wall * _DirtAmount * _Streaks);
        half3 tone = lerp(albedo * 0.62h, soot, 0.3h);
        albedo = lerp(albedo, tone, stain * 0.55h);
        rough = lerp(rough, max(rough, 0.85), stain * 0.35);
    }

    half up = saturate((normalWS.y - 0.35) / 0.45);
    half low = saturate(1.0 - above / 0.6) * (1.0 - up);
    half moss = saturate((n2 - (1.0 - _MossAmount)) * 4.0) * saturate(max(up, low * 0.7));
    albedo = lerp(albedo, _MossColor.rgb * (0.8 + 0.4 * n), moss);
    rough = lerp(rough, 0.95, moss);
    normalTS = normalize(lerp(normalTS, half3(0, 0, 1), moss * 0.6));

    // Puddles in the hollows of the street's paving (reference plan R3 step 3, "a wetness mask along gutters and in
    // hollows"; the gutters are R4's wet band): where _Puddles is set, flat up-facing faces hold standing water in
    // patches about 0.5-2 m across, dark and glossy, so the gas lamps glint in them, each in a damp rim. A few
    // hundredths of the paving hold water. The water's reflection is softened and dimmed, as a shallow puddle on dark
    // stone shows the sky dimly, or it reads as a pale blot from afar. Sizes are authoring defaults, in metres.
    half water = 0.0h;
    half flat = saturate((normalWS.y - 0.92h) * 12.5h) * _Puddles;
    if (flat > 0.0h)
    {
        float pn = VicValue3(float3(positionWS.x * 0.6, 3.7, positionWS.z * 0.6)) * 0.7 +
                   VicValue3(float3(positionWS.x * 2.1, 9.1, positionWS.z * 2.1)) * 0.3;
        half damp = (half)smoothstep(0.54, 0.67, pn) * flat;
        water = (half)smoothstep(0.665, 0.70, pn) * flat;
        albedo *= lerp(1.0h, 0.75h, damp) * lerp(1.0h, 0.55h, water);
        rough = lerp(rough, rough * 0.65, damp);
        rough = lerp(rough, 0.2, water);
        moss *= 1.0h - water;
        normalTS = normalize(lerp(normalTS, half3(0, 0, 1), water));
    }

    s.albedo = albedo;
    s.normalTS = normalTS;
    s.metallic = _Metallic * (1.0 - moss) * (1.0 - max(dirt, streak) * 0.5);
    s.smoothness = 1.0 - rough;
    s.occlusion = lerp(1.0h, mask.b, 0.8h) * (1.0h - water * 0.4h);
}

#endif
