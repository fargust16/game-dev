// Veilmouth's low fog (reference plan R3 steps 5 and 6; WORLD_STYLE_GUIDE §5, §6.1): a full-screen pass after the
// opaque world and the sky, before the transparents, that lays an exponential height fog over the drawn picture,
// densest at the sea's surface and thinning upward, lit toward the sun or the moon, with the gas lamps' halos in it by
// night and the light's shafts through the gaps between roofs and trees. Unity's own exponential fog stays the even
// distance haze; this pass adds what one value for the whole scene cannot: the fog lying in the low streets and over
// the water. Fog is only seen: nothing in play reads it. Driven by HeightFogFeature and VeilmouthSky.
Shader "Hidden/GameDev/Slice/HeightFog"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

    #define MAX_LAMPS 16
    #define SHAFT_STEPS 24

    // Set by VeilmouthSky by the clock.
    float4 _HeightFogColour;     // rgb: the fog's colour, linear
    float4 _HeightFogParams;     // x: density at the base, 1/m; y: scale height, m; z: the base's height, m; w: a sky ray's length, m
    float4 _HeightFogSun;        // xyz: the way toward the sun or the moon; w: how strongly the fog glows toward it
    float4 _HeightFogSunColour;  // rgb: the glow's colour, linear; a: its sharpness, the power of the cosine
    float4 _HeightFogDrift;      // xy: the drift of the fog's patches, m/s; z: how uneven the fog is, 0-1; w: the patches' scale, 1/m
    float4 _HeightFogLampParams; // x: the halos' strength; y: the sea's level, m; z: a halo's core radius, m; w: the wide part's share
    float4 _HeightFogShaftParams; // x: the shafts' strength; y: each step's decay; z: the sharpness of the light's source in the sky; w: the share of the way to the light marched

    // Set by HeightFogFeature for each camera: the lit lamps nearest the eye, and the shafts drawn at half size.
    float4 _HeightFogLamps[MAX_LAMPS];       // xyz: the lamp's glass, world; w: unused
    float4 _HeightFogLampColours[MAX_LAMPS]; // rgb: the light's colour times its intensity, linear; w: the halo's radius, m
    float _HeightFogLampCount;
    TEXTURE2D_X(_HeightFogShafts);
    float _HeightFogShaftFade;           // 0-1: the shafts fade out as the light turns away from the eye

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

    bool IsSky(float raw)
    {
        #if UNITY_REVERSED_Z
            return raw <= 1e-7;
        #else
            return raw >= 1.0 - 1e-7;
        #endif
    }

    float3 WorldAt(float2 uv, float raw)
    {
        #if !UNITY_REVERSED_Z
            raw = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, raw);
        #endif
        return ComputeWorldSpacePosition(uv, raw, UNITY_MATRIX_I_VP);
    }

    // The light's shafts, drawn at half size: from each pixel toward the sun or the moon on the screen, how much open
    // sky near the light is seen past the roofs and trees, each step further counting less.
    half4 FragShafts(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord;
        float3 eye = _WorldSpaceCameraPos;
        float2 light = ComputeNormalizedDeviceCoordinates(eye + _HeightFogSun.xyz * 1000.0, UNITY_MATRIX_VP);
        float2 stride = (light - uv) * (_HeightFogShaftParams.w / SHAFT_STEPS);
        // A different start for each pixel hides the steps' bands; the blur of the upsampling smooths the grain.
        float2 at = uv + stride * InterleavedGradientNoise(input.positionCS.xy, 0);
        float lit = 0.0, weight = 1.0;
        UNITY_UNROLL
        for (int i = 0; i < SHAFT_STEPS; i++)
        {
            float raw = SampleSceneDepth(at);
            if (IsSky(raw) && all(at > 0.0) && all(at < 1.0))
            {
                float3 toward = normalize(WorldAt(at, raw) - eye);
                lit += weight * pow(saturate(dot(toward, _HeightFogSun.xyz)), _HeightFogShaftParams.z);
            }

            weight *= _HeightFogShaftParams.y;
            at += stride;
        }

        return half4(lit / SHAFT_STEPS, 0, 0, 1);
    }

    half4 FragFog(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord;
        half4 colour = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);

        float raw = SampleSceneDepth(uv);
        bool sky = IsSky(raw);
        float3 eye = _WorldSpaceCameraPos;
        float3 ray = WorldAt(uv, raw) - eye;
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
        // The integral of the density along the ray, stable both ways: H (f(low) - f(high)) / |v.y| over the part above
        // the height where Falloff is held, plus that part's length times the held value below it. Without the second
        // term two ends both below it gave no fog and a level ray all of it: a white line at the horizon (owner's play
        // test of 2026-11-11, in the interiors, which stand below the sea's level).
        float rise = v.y * d;
        float column;
        if (abs(rise) > 0.01 * H)
        {
            float low = min(eye.y, end.y), high = max(eye.y, end.y);
            float held = _HeightFogParams.z - 4.0 * H;
            float above = max(low, held);
            column = H * max(Falloff(above) - Falloff(high), 0.0) / abs(v.y) + d * saturate((held - low) / (high - low)) * exp(4.0);
        }
        else
        {
            column = d * Falloff(eye.y + 0.5 * rise);
        }

        // Uneven fog: slow drifting patches, read where the ray ends; the sky has none, since a sky ray's end runs in
        // columns across the screen.
        float2 drift = end.xz * _HeightFogDrift.w + _HeightFogDrift.xy * _HeightFogDrift.w * _Time.y;
        float patches = sky ? 0.5 : 0.65 * Noise(drift) + 0.35 * Noise(drift * 2.7 + 17.0);
        float density = _HeightFogParams.x * (1.0 + _HeightFogDrift.z * (2.0 * patches - 1.0));

        float transmittance = exp(-density * column);

        // The fog is lit toward the sun by day and toward the crimson moon by night.
        float toward = pow(saturate(dot(v, _HeightFogSun.xyz)), _HeightFogSunColour.a) * _HeightFogSun.w;
        float3 fog = _HeightFogColour.rgb + _HeightFogSunColour.rgb * toward;
        float3 result = colour.rgb * transmittance + fog * (1.0 - transmittance);

        // The shafts: the light's own colour where open sky near it shows past the roofs, stronger where the air
        // between the eye and what it sees is thicker.
        float shafts = SAMPLE_TEXTURE2D_X(_HeightFogShafts, sampler_LinearClamp, uv).r;
        result += _HeightFogSunColour.rgb * shafts * (_HeightFogShaftParams.x * _HeightFogShaftFade) * (0.25 + 0.75 * (1.0 - transmittance));

        // The gas lamps' halos: each lamp's light scattered by the fog along the ray up to what it meets, in closed
        // form, the integral of 1/r^2 along the ray (h: the ray's nearest distance to the lamp). Fog scatters mostly
        // forward, so the glow is held close round the glass: a halo of radius w, and a faint wide part.
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

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off Blend Off

        Pass
        {
            Name "HeightFog"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragFog
            ENDHLSL
        }

        Pass
        {
            Name "Shafts"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragShafts
            ENDHLSL
        }
    }
}
