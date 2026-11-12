// The fight's target under the cursor (the owner's play test of 2026-11-12): a second pass of the body's own meshes,
// pushed out along their normals by a few pixels and drawn behind them, so a thin line rings whom the action will hit.
// It is unlit and casts nothing; FightTargetLight adds it to the body's renderers while the cursor rests on it.
Shader "GameDev/Slice/Target Outline"
{
    Properties
    {
        _BaseColor ("Colour", Color) = (0.95, 0.78, 0.4, 1)
        _Width ("Width (pixels)", Float) = 2.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Width;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float4 clip = TransformObjectToHClip(input.positionOS.xyz);
                float2 way = TransformWorldToHClipDir(TransformObjectToWorldNormal(input.normalOS)).xy;
                way *= rsqrt(max(dot(way, way), 1e-8));
                // The same width in pixels at any distance: the push is in screen space, scaled back by w.
                clip.xy += way * (_Width * 2.0 / _ScreenParams.xy) * clip.w;
                output.positionCS = clip;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
