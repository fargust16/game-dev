Shader "GameDev/Overworld/LakeWater"
{
    // Polytope PT_Water look (deep/shallow + scrolling normals + shore foam)
    // WITHOUT _CameraOpaqueTexture refraction — that path reads black in Scene/Game.
    Properties
    {
        _DeepColor("Deep Color", Color) = (0.10, 0.30, 0.28, 0.98)
        _ShallowColor("Shallow Color", Color) = (0.12, 0.30, 0.28, 0.94)
        _FoamColor("Foam Color", Color) = (0.30, 1.0, 0.91, 0.35)
        _Opacity("Opacity", Range(0, 1)) = 0.99
        _Smoothness("Smoothness", Range(0, 1)) = 0.84
        [Normal] _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0, 2)) = 0.55
        _NormalSpeed("Normal Speed", Range(0, 1)) = 0.22
        _NormalTiling("Normal Tiling", Range(0.01, 20)) = 6.14
        _DepthFade("Depth Fade Metres", Range(0.1, 40)) = 6
        _ShoreFoam("Shore Foam", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _FoamColor;
                float4 _NormalMap_ST;
                float _Opacity;
                float _Smoothness;
                float _NormalScale;
                float _NormalSpeed;
                float _NormalTiling;
                float _DepthFade;
                float _ShoreFoam;
            CBUFFER_END

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                float fogFactor : TEXCOORD5;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = nrm.normalWS;
                output.tangentWS = float4(nrm.tangentWS, input.tangentOS.w);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(pos.positionCS);
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return output;
            }

            float3 SampleAnimatedNormal(float2 uv, float3 normalWS, float3 tangentWS, float3 bitangentWS)
            {
                float2 scrollA = uv * _NormalTiling + _Time.y * _NormalSpeed * float2(0.7, 0.35);
                float2 scrollB = uv * (_NormalTiling * 0.65) - _Time.y * _NormalSpeed * float2(0.4, 0.8);
                float3 n1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, scrollA), _NormalScale);
                float3 n2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, scrollB), _NormalScale * 0.75);
                float3 nTS = normalize(n1 + n2);
                float3x3 tbn = float3x3(tangentWS, bitangentWS, normalWS);
                return normalize(mul(nTS, tbn));
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3 normalWS = SampleAnimatedNormal(
                    input.uv, normalize(input.normalWS), normalize(input.tangentWS.xyz), normalize(bitangent));

                float2 screenUV = input.screenPos.xy / max(input.screenPos.w, 1e-5);
                float sceneRaw = SampleSceneDepth(screenUV);
                float sceneEye = LinearEyeDepth(sceneRaw, _ZBufferParams);
                float surfaceEye = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float waterColumn = max(sceneEye - surfaceEye, 0.0);
                // Missing/invalid depth → mid-deep teal (never pitch black).
                float depthT = (waterColumn > 200.0 || waterColumn < 1e-4)
                    ? 0.55
                    : saturate(waterColumn / max(_DepthFade, 0.1));

                float3 albedo = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthT);
                float foam = saturate(pow(1.0 - depthT, 2.2) * _ShoreFoam);
                albedo = lerp(albedo, _FoamColor.rgb, foam * 0.4);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float ndotl = saturate(dot(normalWS, mainLight.direction));
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(normalWS, halfDir)), lerp(16.0, 128.0, _Smoothness)) * _Smoothness;

                float3 ambient = SampleSH(normalWS) * 0.55;
                float3 lit = albedo * (ambient + mainLight.color * (ndotl * 0.85 + 0.15) * mainLight.shadowAttenuation);
                lit += mainLight.color * spec * 0.35 * mainLight.shadowAttenuation;

                float alpha = saturate(lerp(0.94, 1.0, depthT) * _Opacity);
                alpha = max(alpha, 0.96);

                lit = MixFog(lit, input.fogFactor);
                return half4(lit, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
