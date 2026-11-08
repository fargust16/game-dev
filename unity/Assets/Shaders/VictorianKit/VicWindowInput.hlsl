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
CBUFFER_END

TEXTURE2D(_MaskMap);

float _VicNight;

#endif
