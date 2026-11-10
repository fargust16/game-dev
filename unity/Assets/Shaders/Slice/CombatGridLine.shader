// The Combat Grid's lines (S3a.5): unlit and see-through, fading out with the distance from the fighter the grid follows,
// and dimmed with the main light, so a pale line reads on the setts by day and does not glare at dusk. The fade's centre
// and radii come from the renderer's property block; a copy with the defaults draws at full strength everywhere.
Shader "GameDev/Combat Grid Line"
{
    Properties
    {
        _BaseColor ("Colour", Color) = (0.78, 0.82, 0.72, 0.5)
        _Focus ("Fade centre (world)", Vector) = (0, 0, 0, 0)
        _Fade ("Fade from, to (m)", Vector) = (1000, 2000, 0, 0)
        _Dark ("Strength under no light", Range(0, 1)) = 0.45
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Unlit"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _Focus;
                float4 _Fade;
                half _Dark;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half fog : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half fade = 1.0h - smoothstep(_Fade.x, _Fade.y, distance(input.positionWS.xz, _Focus.xz));
                half light = saturate(_Dark + dot(_MainLightColor.rgb, half3(0.2126h, 0.7152h, 0.0722h)) * (1.0h - _Dark));
                half3 colour = MixFog(_BaseColor.rgb * light, input.fog);
                return half4(colour, _BaseColor.a * fade);
            }
            ENDHLSL
        }
    }
}
