// Veilmouth's low fog (reference plan R3 step 5; WORLD_STYLE_GUIDE §5, §6.1): a full-screen pass after the opaque world
// and the sky, before the transparents, that lays an exponential height fog over the drawn picture, densest at the
// sea's surface and thinning upward, lit toward the sun or the moon, with the gas lamps' halos in it by night. Unity's
// own exponential fog stays the even distance haze; this pass adds what one value for the whole scene cannot: the fog
// lying in the low streets and over the water. Fog is only seen: nothing in play reads it. Driven by HeightFogFeature
// and VeilmouthSky.
Shader "Hidden/GameDev/Slice/HeightFog"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off Blend Off

        Pass
        {
            Name "HeightFog"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #define MAX_LAMPS 16

            // Set by VeilmouthSky by the clock.
            float4 _HeightFogColour;    // rgb: the fog's colour, linear
            float4 _HeightFogParams;    // x: density at the base, 1/m; y: scale height, m; z: the base's height, m; w: a sky ray's length, m
            float4 _HeightFogSun;       // xyz: the way toward the sun or the moon; w: how strongly the fog glows toward it
            float4 _HeightFogSunColour; // rgb: the glow's colour, linear; a: its sharpness, the power of the cosine
            float4 _HeightFogDrift;     // xy: the drift of the fog's patches, m/s; z: how uneven the fog is, 0-1; w: the patches' scale, 1/m
            float4 _HeightFogLampParams; // x: the halos' strength; y: the sea's level, m; z: a halo's core radius, m; w: the wide part's share

            // Set by HeightFogFeature for each camera: the lit lamps nearest the eye.
            float4 _HeightFogLamps[MAX_LAMPS];       // xyz: the lamp's glass, world; w: unused
            float4 _HeightFogLampColours[MAX_LAMPS]; // rgb: the light's colour times its intensity, linear; w: the halo's radius, m
            float _HeightFogLampCount;

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Smooth value noise, 0-1.
            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash(i), b = Hash(i + float2(1, 0)), c = Hash(i + float2(0, 1)), d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // exp(-(y - base) / H), kept finite below the base.
            float Falloff(float y)
            {
                return exp(min(-(y - _HeightFogParams.z) / _HeightFogParams.y, 4.0));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 colour = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);

                float raw = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    bool sky = raw <= 1e-7;
                #else
                    bool sky = raw >= 1.0 - 1e-7;
                    raw = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, raw);
                #endif

                float3 eye = _WorldSpaceCameraPos;
                float3 ray = ComputeWorldSpacePosition(uv, raw, UNITY_MATRIX_I_VP) - eye;
                float d = length(ray);
                float3 v = ray / max(d, 1e-4);
                if (sky)
                {
                    d = _HeightFogParams.w;
                }

                // The water is drawn over a riverbed and a seabed: a ray that goes under the sea's surface ends on it.
                float sea = _HeightFogLampParams.y;
                if (v.y < -1e-4 && eye.y > sea)
                {
                    d = min(d, (eye.y - sea) / -v.y);
                }

                float3 end = eye + v * d;
                float H = _HeightFogParams.y;
                float atEye = Falloff(eye.y);
                // The integral of the density along the ray, stable both ways: H (f(eye) - f(end)) / v.y.
                float rise = v.y * d;
                float column = abs(rise) > 0.01 * H ? H * (atEye - Falloff(end.y)) / v.y : d * Falloff(eye.y + 0.5 * rise);

                // Uneven fog: slow drifting patches, read where the ray ends; the sky has none, since a sky ray's end
                // runs in columns across the screen.
                float2 drift = end.xz * _HeightFogDrift.w + _HeightFogDrift.xy * _HeightFogDrift.w * _Time.y;
                float patches = sky ? 0.5 : 0.65 * Noise(drift) + 0.35 * Noise(drift * 2.7 + 17.0);
                float density = _HeightFogParams.x * (1.0 + _HeightFogDrift.z * (2.0 * patches - 1.0));

                float transmittance = exp(-density * column);

                // The fog is lit toward the sun by day and toward the crimson moon by night.
                float toward = pow(saturate(dot(v, _HeightFogSun.xyz)), _HeightFogSunColour.a) * _HeightFogSun.w;
                float3 fog = _HeightFogColour.rgb + _HeightFogSunColour.rgb * toward;
                float3 result = colour.rgb * transmittance + fog * (1.0 - transmittance);

                // The gas lamps' halos: each lamp's light scattered by the fog along the ray up to what it meets, in
                // closed form, the integral of 1/r^2 along the ray (h: the ray's nearest distance to the lamp). Fog
                // scatters mostly forward, so the glow is held close round the glass: a halo of radius w, and a faint
                // wide part.
                float3 halo = 0;
                int count = (int)_HeightFogLampCount;
                float core = _HeightFogLampParams.z * _HeightFogLampParams.z;
                for (int i = 0; i < count; i++)
                {
                    float3 w = _HeightFogLamps[i].xyz - eye;
                    float b = dot(w, v);
                    float h = sqrt(max(dot(w, w) - b * b, core));
                    float along = (atan((d - b) / h) + atan(b / h)) / h;
                    float held = _HeightFogLampParams.w + exp(-h / _HeightFogLampColours[i].w);
                    // The fog's density at the lamp, and how much of its light the fog lets through to the eye.
                    float local = density * Falloff(_HeightFogLamps[i].y);
                    float through = exp(-density * atEye * min(length(w), d));
                    halo += _HeightFogLampColours[i].rgb * (along * held * local * through);
                }

                result += halo * _HeightFogLampParams.x;
                return half4(result, colour.a);
            }
            ENDHLSL
        }
    }
}
