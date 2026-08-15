Shader "GameDev/Overworld/Grass"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (0.40, 0.58, 0.28, 1)
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.15
        _ColorVariation("Color Variation", Range(0, 1)) = 0.32
        _DryEdgeColor("Dry Edge Color", Color) = (0.42, 0.36, 0.22, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            AlphaToMask On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_OverworldRoadLawnMask);
            SAMPLER(sampler_OverworldRoadLawnMask);
            TEXTURE2D(_OverworldGrassTintMask);
            SAMPLER(sampler_OverworldGrassTintMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Cutoff;
                float _ColorVariation;
                float4 _DryEdgeColor;
            CBUFFER_END

            float4 _GrassWindDir;
            float _GrassWindStrength;
            float _GrassWindSpeed;
            float4 _GrassPlayerPos;
            float _GrassPlayerRadius;
            float _GrassPlayerStrength;

            // xy = world min XZ, zw = world size XZ. Lawn mask: 0 = road, 1 = lawn.
            float4 _OverworldRoadLawnMask_World;
            // x = height reduce, y = density reduce, z = tilt, w = dry tint
            float4 _OverworldGrassEdgeParams;
            float4 _OverworldGrassTintMask_World;
            float _OverworldGrassTintStrength;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                float colorHash : TEXCOORD4;
                float lawn01 : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float Hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float SampleLawn01(float2 worldXZ)
            {
                float2 size = max(_OverworldRoadLawnMask_World.zw, float2(1e-3, 1e-3));
                float2 uv = (worldXZ - _OverworldRoadLawnMask_World.xy) / size;
                if (uv.x < 0.0 || uv.y < 0.0 || uv.x > 1.0 || uv.y > 1.0)
                {
                    return 1.0;
                }

                return SAMPLE_TEXTURE2D_LOD(_OverworldRoadLawnMask, sampler_OverworldRoadLawnMask, uv, 0).r;
            }

            float3 SampleGrassTint(float2 worldXZ)
            {
                float2 size = max(_OverworldGrassTintMask_World.zw, float2(1e-3, 1e-3));
                float2 uv = (worldXZ - _OverworldGrassTintMask_World.xy) / size;
                if (uv.x < 0.0 || uv.y < 0.0 || uv.x > 1.0 || uv.y > 1.0 || _OverworldGrassTintStrength < 1e-3)
                {
                    return _BaseColor.rgb;
                }

                float3 tint = SAMPLE_TEXTURE2D_LOD(_OverworldGrassTintMask, sampler_OverworldGrassTintMask, uv, 0).rgb;
                return lerp(_BaseColor.rgb, tint, saturate(_OverworldGrassTintStrength));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 posOS = input.positionOS.xyz;
                float tip = saturate(posOS.y / 0.7);

                // Instance origin in world (pre-deform) for mask / hash.
                float3 pivotWS = TransformObjectToWorld(float3(0, 0, 0));
                float lawn = SampleLawn01(pivotWS.xz);
                output.lawn01 = lawn;

                float heightReduce = _OverworldGrassEdgeParams.x;
                float tiltStrength = _OverworldGrassEdgeParams.z;
                float edge = 1.0 - lawn;
                // Smoothstep-shaped edge emphasis (memo: not linear).
                float edgeS = edge * edge * (3.0 - 2.0 * edge);

                // Height falloff toward road.
                posOS.y *= lerp(1.0, 1.0 - heightReduce * 0.92, edgeS);

                // Flatten / tilt blades toward the ground in the soft shoulder.
                float2 lean = float2(Hash21(pivotWS.xz + 3.1) * 2.0 - 1.0,
                                     Hash21(pivotWS.xz + 9.7) * 2.0 - 1.0);
                lean = normalize(lean + 1e-5);
                posOS.xz += lean * tip * edgeS * tiltStrength * 0.55;
                posOS.y *= lerp(1.0, 0.45, tip * edgeS * tiltStrength);

                float heightMask = saturate(posOS.y / 0.7);
                float3 posWS = TransformObjectToWorld(posOS);
                float2 windDir = normalize(_GrassWindDir.xy + 1e-5);
                float phase = dot(posWS.xz, windDir) * 0.55
                              + Hash21(posWS.xz) * 6.28318;
                float wind = sin(_Time.y * _GrassWindSpeed + phase) * _GrassWindStrength * heightMask;
                posWS.xz += windDir * wind;

                float2 toPlayer = posWS.xz - _GrassPlayerPos.xz;
                float dist = length(toPlayer);
                float radiusJitter = _GrassPlayerRadius * (0.85 + 0.3 * Hash21(posWS.xz + 17.1));
                float bendT = 1.0 - smoothstep(radiusJitter * 0.25, radiusJitter, dist);
                bendT *= heightMask * _GrassPlayerStrength;
                float2 bendDir = dist > 1e-4 ? (toPlayer / dist) : float2(1, 0);
                posWS.xz += bendDir * bendT * 0.55;
                posWS.y -= bendT * 0.08;

                output.positionWS = posWS;
                output.positionCS = TransformWorldToHClip(posWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.colorHash = Hash21(floor(posWS.xz * 0.5));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(tex.a - _Cutoff);

                // Soft density falloff in the transition band (shader dither, no extra draws).
                float densityReduce = _OverworldGrassEdgeParams.y;
                float densKeep = lerp(1.0 - densityReduce, 1.0, input.lawn01);
                densKeep = densKeep * densKeep * (3.0 - 2.0 * densKeep);
                float dither = Hash21(floor(input.positionWS.xz * 6.0));
                clip(densKeep - dither * 0.98);

                float3 baseTint = SampleGrassTint(input.positionWS.xz);
                float3 tint = lerp(baseTint, baseTint * float3(0.92, 1.05, 0.7),
                                  input.colorHash * _ColorVariation);
                float mud = Hash21(floor(input.positionWS.xz * 0.35 + 4.2));
                tint = lerp(tint, tint * float3(0.85, 0.9, 0.65), mud * 0.3 * _ColorVariation);
                float3 albedo = tex.rgb * tint;

                float dryStrength = _OverworldGrassEdgeParams.w;
                float dryMix = (1.0 - input.lawn01) * dryStrength;
                float3 dryAlbedo = _DryEdgeColor.rgb * (0.55 + 0.45 * tex.r);
                albedo = lerp(albedo, dryAlbedo, dryMix);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 normalWS = normalize(input.normalWS);
                float ndotl = saturate(dot(normalWS, mainLight.direction));
                float3 ambient = SampleSH(normalWS) * 0.65;
                float3 lit = albedo * (ambient + mainLight.color * (ndotl * 0.7 + 0.3));
                lit = MixFog(lit, input.fogFactor);
                return half4(lit, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
