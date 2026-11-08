// Veilmouth's painted sky (reference plan R3 step 4; WORLD_STYLE_GUIDE §5): two HDR cubemaps of Free HDR Sky, the day
// and the sunset, blended by the clock, each with its own tint, exposure and turn about the vertical, so its painted sun
// stands where the directional light comes from. The blend is muted toward grey (the reference's washed sky, never a
// saturated blue), and the band over the horizon takes the distance haze's colour (unity_FogColor), so the far roofs
// melt into the sky instead of standing against it. Night is the sunset's clouds, dark and cool, under the crimson moon.
// Every value is set by VeilmouthMystic as an authoring default; Material.Lerp blends the floats and colours of the day,
// dusk and night materials, which share the two cubemaps.
Shader "GameDev/Slice/Sky"
{
    Properties
    {
        [NoScaleOffset] _TexA ("Sky A, day (HDR)", Cube) = "grey" {}
        [HDR] _TintA ("Tint A", Color) = (1, 1, 1, 1)
        _ExposureA ("Exposure A", Range(0, 8)) = 1
        _RotationA ("Turn A, degrees", Range(0, 360)) = 0
        [NoScaleOffset] _TexB ("Sky B, sunset (HDR)", Cube) = "grey" {}
        [HDR] _TintB ("Tint B", Color) = (1, 1, 1, 1)
        _ExposureB ("Exposure B", Range(0, 8)) = 1
        _RotationB ("Turn B, degrees", Range(0, 360)) = 0
        _Blend ("A to B", Range(0, 1)) = 0
        _Saturation ("Saturation", Range(0, 1.5)) = 1
        _HazeHeight ("Horizon haze height (sine of elevation)", Range(0.01, 1)) = 0.2
        _HazeStrength ("Horizon haze strength", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

            TEXTURECUBE(_TexA);
            SAMPLER(sampler_TexA);
            TEXTURECUBE(_TexB);
            SAMPLER(sampler_TexB);

            CBUFFER_START(UnityPerMaterial)
                half4 _TexA_HDR;
                half4 _TexB_HDR;
                half4 _TintA;
                half4 _TintB;
                half _ExposureA;
                half _ExposureB;
                float _RotationA;
                float _RotationB;
                half _Blend;
                half _Saturation;
                half _HazeHeight;
                half _HazeStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            // As the built-in Skybox/Cubemap turns its lookup, so a turn means the same here as on the pack's materials.
            float3 TurnAboutY(float3 v, float degrees)
            {
                float s, c;
                sincos(radians(degrees), s, c);
                return float3(c * v.x - s * v.z, v.y, s * v.x + c * v.z);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.direction);
                half3 a = DecodeHDREnvironment(SAMPLE_TEXTURECUBE(_TexA, sampler_TexA, TurnAboutY(d, _RotationA)), _TexA_HDR);
                half3 b = DecodeHDREnvironment(SAMPLE_TEXTURECUBE(_TexB, sampler_TexB, TurnAboutY(d, _RotationB)), _TexB_HDR);
                half3 sky = lerp(a * _TintA.rgb * _ExposureA, b * _TintB.rgb * _ExposureB, _Blend);
                half grey = dot(sky, half3(0.2126h, 0.7152h, 0.0722h));
                sky = lerp(grey.xxx, sky, _Saturation);

                // Below the horizon and in a band over it the sky is the haze, so distant geometry fades into it.
                half haze = saturate(1.0h - max(d.y, 0.0h) / _HazeHeight);
                sky = lerp(sky, unity_FogColor.rgb, haze * haze * _HazeStrength);
                return half4(sky, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
