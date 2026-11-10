// Material properties of GameDev/VictorianKit/Window, shared by all its passes so the SRP Batcher sees one layout.
#ifndef VIC_WINDOW_INPUT_INCLUDED
#define VIC_WINDOW_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
half _Cutoff;
half4 _Tint2;
half _Grime;
half _BumpScale;
half _Roughness;
half _Reflect;
half4 _RoomTint;
half _RoomDepth;
half _RoomDay;
half _LitFraction;
half4 _NightColor;
half _NightIntensity;
half4 _CurtainColor;
half _Transmit;
half _RoomNear;
half _RoomFar;
half4 _WallDark;
half4 _WallPanel;
half4 _WallPaper;
half4 _FloorTone;
half4 _CeilingTone;
CBUFFER_END

// The GPU Resident Drawer draws through DOTS instancing (VEILMOUTH_CITY_PLAN.md §29.7), which asks for one block of
// DOTS properties. No renderer overrides it, so _BaseColor stays the material's own, read once per instance as
// URP's Lit reads it.
#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

static float4 unity_DOTS_Sampled_BaseColor;

void SetupDOTSVicWindowMaterialPropertyCaches()
{
    unity_DOTS_Sampled_BaseColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
}

#undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
#define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSVicWindowMaterialPropertyCaches()
#define _BaseColor unity_DOTS_Sampled_BaseColor
#endif

TEXTURE2D(_MaskMap);

float _VicNight;

#endif
