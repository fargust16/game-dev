// Material properties of GameDev/Slice/Tree, shared by all its passes so the SRP Batcher sees one layout, and the wind
// every pass moves its vertices by (VEILMOUTH_CITY_PLAN.md §27).
#ifndef VEILMOUTH_TREE_INPUT_INCLUDED
#define VEILMOUTH_TREE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
half _Cutoff;
half _BumpScale;
half _Smoothness;
half _Translucency;
float4 _WindDirection;
float _WindStrength;
float _WindSpeed;
float _GustScale;
float _Flutter;
float _FlutterSpeed;
CBUFFER_END

// The city's night (VeilmouthLook): the bounce light baked by day mostly goes.
float _VicNight;

// The wind, in world space, so a statically batched tree moves as one placed alone. The vertex colour carries the
// weights Blender wrote (tools/blender/veilmouth_trees.py): R the sway, (height / tree height)^1.8, the same on bark
// and leaves so a twig never leaves its branch; G the flutter, 0 at a twig card's base to 1 at its tip, 0 on bark;
// B a phase of the twig's own. Gusts run along the wind, so neighbouring trees lean one after another.
float3 TreeWind(float3 positionWS, half4 weights)
{
    float2 along = normalize(_WindDirection.xz + float2(1e-5, 0.0));
    float2 across = float2(-along.y, along.x);
    float t = _Time.y * _WindSpeed;
    float wave = dot(positionWS.xz, along) * _GustScale;
    // Mostly downwind, never far upwind: a lean that breathes, under two slower swells.
    float gust = 0.6 + 0.45 * sin(t - wave) + 0.25 * sin(0.37 * t - 0.7 * wave + 1.7);
    float sway = weights.r * _WindStrength * gust;
    float side = weights.r * _WindStrength * 0.22 * sin(1.9 * t - 1.3 * wave + 6.2832 * weights.b);
    float3 offset = float3(along.x, 0.0, along.y) * sway + float3(across.x, 0.0, across.y) * side;
    // A leaning crown sinks a little, so the trunk does not seem to stretch.
    offset.y -= 0.5 * sway * sway / max(_WindStrength * 8.0, 1e-3);
    float flutter = sin(_Time.y * _FlutterSpeed + 37.1 * weights.b + 3.1 * positionWS.y);
    offset += float3(0.35, 1.0, 0.25) * (flutter * weights.g * _Flutter);
    return positionWS + offset;
}

#endif
