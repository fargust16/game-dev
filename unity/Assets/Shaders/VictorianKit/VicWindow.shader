// The glass face of the Victorian Kit's windows (Plans/VICTORIAN_STREETS_PLAN.md §4.2, §4.4): transparent, so the rooms
// built behind the facade (VicRooms) are seen through it, with the reflection probe through Fresnel, grime from the
// mask, curtains drawn on the pane, and lit rooms after dark from the global _VicNight. Beyond _RoomNear the pane turns
// opaque toward a room by interior mapping, reached at _RoomFar, where the built rooms are culled.
// A window's insert has its UV in metres: u = 0 at the pane's middle, v = 0 at the sill, 0.85 m above the storey floor;
// the mapped room is the built room behind the pane when its house recorded it (VicWindowRooms, the renderer's shader
// user value: its finish, depth, walls, floor and ceiling), else 2 m wide, 3 m high and _RoomDepth deep. The rest of a
// window's look comes from a hash of its own anchor,
// the world point where its UV is zero, which static batching keeps. Premultiplied alpha: the colour is what the glass
// adds, the alpha how much of the room behind it hides. It casts no shadow, so the sun falls into the rooms.
Shader "GameDev/VictorianKit/Window"
{
    Properties
    {
        [MainTexture] _BaseMap("Glass (sRGB)", 2D) = "white" {}
        [Normal] _BumpMap("Ripple", 2D) = "bump" {}
        _MaskMap("Mask (R rough, G grime)", 2D) = "white" {}
        [MainColor] _BaseColor("Glass tint", Color) = (0.9, 0.95, 1, 1)
        _Tint2("Grime", Color) = (0.42, 0.4, 0.34, 1)
        _Grime("Grime amount", Range(0, 1)) = 0.15
        _BumpScale("Ripple scale", Float) = 0.6
        _Roughness("Roughness scale", Range(0, 2)) = 1
        _Reflect("Reflection", Range(0, 2)) = 1
        _Transmit("Light through the glass", Range(0, 1)) = 0.8
        _RoomNear("Built rooms fade out from (m)", Float) = 32
        _RoomFar("Mapped rooms only beyond (m)", Float) = 46
        _RoomTint("Room tint", Color) = (1, 1, 1, 1)
        _RoomDepth("Room depth (m)", Float) = 3.5
        _RoomDay("Room brightness by day", Range(0, 1)) = 0.22
        _LitFraction("Rooms lit at night", Range(0, 1)) = 0.5
        _NightColor("Lamp light", Color) = (1, 0.72, 0.42, 1)
        _NightIntensity("Lamp strength", Float) = 2.2
        _CurtainColor("Curtain", Color) = (0.55, 0.18, 0.14, 1)
        _WallDark("Built rooms: dark finish", Color) = (0.3, 0.24, 0.18, 1)
        _WallPanel("Built rooms: panelled finish", Color) = (0.36, 0.26, 0.18, 1)
        _WallPaper("Built rooms: papered finish", Color) = (0.55, 0.5, 0.42, 1)
        _FloorTone("Built rooms: floor", Color) = (0.3, 0.22, 0.15, 1)
        _CeilingTone("Built rooms: ceiling", Color) = (0.75, 0.72, 0.66, 1)
        [HideInInspector] _Cutoff("Alpha cutoff", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite Off
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing

            #include "VicWindowInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uvPane : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float4 tangentWS : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                o.uvPane = input.texcoord;
                o.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                o.positionWS = p.positionWS;
                o.normalWS = n.normalWS;
                o.tangentWS = float4(n.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                o.positionCS = p.positionCS;
                return o;
            }

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            half3 Wallpaper(float h)
            {
                static const half3 tones[5] =
                {
                    half3(0.42, 0.50, 0.38), half3(0.50, 0.20, 0.18), half3(0.62, 0.50, 0.28),
                    half3(0.36, 0.42, 0.50), half3(0.70, 0.64, 0.52)
                };
                return tones[min((int)(h * 5.0), 4)];
            }

            // The room behind a pane, in the pane's UV metres: walls at x0 and x1, floor at y0 and ceiling at y1 from the
            // sill, its back wall depth behind the glass, and its finish, floor and ceiling colours.
            struct RoomBox
            {
                float x0, x1, y0, y1, depth;
                half3 wall, floor, ceiling;
            };

            // The built room recorded for this renderer (VicWindowRooms.Pack: finish 2 bits, then depth, left wall,
            // right wall, ceiling over the sill and floor under it, 6 bits each in tenths of a metre), else the old
            // made-up room from the window's hash.
            RoomBox ReadRoom(uint room, float h1, float h3)
            {
                RoomBox b;
                if (room == 0u)
                {
                    b.x0 = -1.0;
                    b.x1 = 1.0;
                    b.y0 = -0.85;
                    b.y1 = 2.15;
                    b.depth = _RoomDepth * (0.8 + 0.4 * h3);
                    b.wall = Wallpaper(h1) * _RoomTint.rgb;
                    b.floor = half3(0.26, 0.17, 0.10);
                    b.ceiling = half3(0.75, 0.72, 0.66);
                    return b;
                }

                uint finish = room & 3u;
                b.depth = max(((room >> 2) & 63u) * 0.1, 0.8);
                b.x0 = -max(((room >> 8) & 63u) * 0.1, 0.6);
                b.x1 = max(((room >> 14) & 63u) * 0.1, 0.6);
                b.y1 = max(((room >> 20) & 63u) * 0.1, 1.6);
                b.y0 = -max(((room >> 26) & 63u) * 0.1, 0.2);
                b.wall = finish == 1u ? _WallDark.rgb : (finish == 2u ? _WallPanel.rgb : _WallPaper.rgb);
                b.floor = _FloorTone.rgb;
                b.ceiling = _CeilingTone.rgb;
                return b;
            }

            // The far look: a room by interior mapping in the pane's frame, the box of ReadRoom behind the pane, with
            // the same curtains and blind as the near look.
            half3 MappedRoom(float3 dir, float2 pane, RoomBox b, float h2, float night, bool lit, out float fade)
            {
                dir.z = min(dir.z, -1e-3);
                float3 p0 = float3(pane.x, pane.y, 0.0);
                float depth = b.depth;
                float tx = max(((dir.x > 0 ? b.x1 : b.x0) - p0.x) / (abs(dir.x) > 1e-4 ? dir.x : 1e-4), 0.0);
                float ty = max(((dir.y > 0 ? b.y1 : b.y0) - p0.y) / (abs(dir.y) > 1e-4 ? dir.y : 1e-4), 0.0);
                float tz = -depth / dir.z;
                float t = min(min(tx, ty), tz);
                float3 hit = p0 + dir * t;

                half3 wall = b.wall;
                half3 room;
                if (t == tz)
                {
                    room = wall * (0.92 + 0.08 * step(0.5, frac(hit.x * 6.0)));
                    room *= hit.y < 0.05 ? 0.55 : 1.0;
                    float2 pic = abs(hit.xy - float2(clamp((h2 - 0.5) * 0.8, b.x0 + 0.4, b.x1 - 0.4), 1.0)) - float2(0.25, 0.18);
                    room = max(pic.x, pic.y) < 0 ? half3(0.18, 0.14, 0.1) : room;
                }
                else if (t == tx)
                {
                    room = wall * 0.7 * (hit.y < 0.05 ? 0.55 : 1.0);
                }
                else if (dir.y < 0)
                {
                    room = b.floor * (0.85 + 0.15 * step(0.5, frac(hit.x * 5.0)));
                }
                else
                {
                    room = b.ceiling;
                }

                fade = saturate(t / (depth * 1.6));
                room *= lerp(1.0, 0.45, fade);
                // After dark a lit room glows as the near look's lamp glow does, whatever its finish: the room's own
                // shading over its walls' brightness, so a dark panelled room is as bright as a papered one.
                half shade = 1.0 / max(dot(wall, half3(0.2126, 0.7152, 0.0722)), 0.02);
                float lamp = lit ? _NightIntensity * 0.35 * min(shade, 40.0) * (0.6 + 0.4 * (1.0 - fade)) : 0.02;
                // The lamp's light takes over the finish's hue, as the near look's glow is the lamp's colour.
                half3 lampLit = lerp(room, dot(room, half3(0.2126, 0.7152, 0.0722)).xxx, 0.6);
                return room * lerp(_RoomDay, 0.0, night) + lampLit * _NightColor.rgb * lamp * night;
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // A pane is one face, and the kit's panes face either way: the frame comes from the mesh as it is, so T
                // and B follow the UV whichever way the face points, and only then does the normal turn to the viewer.
                float3 N = normalize(input.normalWS);
                float3 T = input.tangentWS.xyz;
                T = dot(T, T) > 1e-6 ? normalize(T) : normalize(cross(N, float3(0.0, 1.0, 0.0)) + 1e-5);
                float3 B = normalize(cross(N, T) * (input.tangentWS.w < 0.0 ? -1.0 : 1.0));
                N = IS_FRONT_VFACE(face, N, -N);
                float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // The window's anchor: the world point where its pane UV is zero, the same for all its fragments.
                float2 pane = input.uvPane;
                float3 anchor = input.positionWS - pane.x * T - pane.y * B;
                float h1 = Hash13(floor(anchor * 2.0 + 0.25) + 0.37);
                float h2 = Hash13(floor(anchor * 2.0 + 0.25) + 5.11);
                float h3 = Hash13(floor(anchor * 2.0 + 0.25) + 9.73);

                half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_BaseMap, input.uv);
                half3 ripple = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, input.uv), _BumpScale);
                float3 Np = normalize(ripple.x * T + ripple.y * B + ripple.z * N);

                float night = saturate(_VicNight);
                bool lit = h3 < _LitFraction;
                float far = smoothstep(_RoomNear, _RoomFar, distance(GetCameraPositionWS(), input.positionWS));

                // The pane's own frame of the view, into the room (−N is inward from whichever side is seen).
                float3 d = -V;
                float3 dir = float3(dot(d, T), dot(d, B), dot(d, N));

                // Curtains at the sides and a blind from the top, just behind the pane, narrow so the room shows; the
                // mapped room keeps the same ones, so the turn from near to far changes no curtain (R2 step 4).
                float3 nearHit = float3(pane, 0.0) + dir * (-0.06 / min(dir.z, -1e-3));
                float edge = 0.40 + 0.12 * h2;
                bool curtain = abs(nearHit.x) > edge && nearHit.y > -0.1 && h1 > 0.25;
                bool blindOn = nearHit.y > 1.6 - 0.3 * h3 * h3 && h2 > 0.4;
                half3 curtainColor = _CurtainColor.rgb * (0.7 + 0.6 * h1) * (0.85 + 0.15 * sin(nearHit.x * 40.0));
                half3 blindColor = half3(0.62, 0.56, 0.44) * (0.9 + 0.1 * h2);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light sun = GetMainLight(shadowCoord);
                half3 ambient = SampleSH(N);
                half3 sunOnPane = sun.color * saturate(dot(N, sun.direction)) * sun.shadowAttenuation;

                // Reflection through Fresnel, plus the sun's highlight.
                float NdotV = saturate(dot(Np, V));
                float fresnel = 0.04 + 0.96 * pow(1.0 - NdotV, 5.0);
                half rough = saturate(mask.r * _Roughness);
                float3 R = reflect(-V, Np);
                half3 env = GlossyEnvironmentReflection(R, input.positionWS, rough, 1.0h, GetNormalizedScreenSpaceUV(input.positionCS));
                float3 H = normalize(sun.direction + V);
                half spec = pow(saturate(dot(Np, H)), 300.0) * 3.0 * sun.shadowAttenuation * (1.0 - rough);
                half3 glass = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                half grime = saturate((mask.g - (1.0h - _Grime)) * 4.0) * 0.7;
                half3 grimeColor = _Tint2.rgb * (ambient + sunOnPane * 0.3);

                // Near: the reflection lies over the room seen through the pane; a curtain or blind hides the room.
                float reflectNear = saturate(fresnel * _Reflect);
                half3 fabricLight = ambient * (1.0 - night) + _NightColor.rgb * (lit ? 0.9 : 0.03) * night;
                half3 lampGlow = _NightColor.rgb * _NightIntensity * 0.35 * (lit ? 1.0 : 0.0) * night;
                bool covered = curtain || blindOn;
                half3 behind = covered ? (curtain ? curtainColor : blindColor) * fabricLight : lampGlow * glass;
                float through = covered ? 0.0 : _Transmit * dot(glass, half3(0.3333, 0.3333, 0.3333)) * (lit && night > 0.5 ? 0.55 : 1.0);
                half3 nearColor = env * reflectNear + spec * sun.color + behind * (1.0 - reflectNear);
                float nearAlpha = 1.0 - (1.0 - reflectNear) * through;

                // Far: the opaque mapped room, as before the rooms were built.
                half3 farColor = nearColor;
                if (far > 0.0)
                {
                    float fade;
                    half3 inside = MappedRoom(dir, pane, ReadRoom(unity_RendererUserValue, h1, h3), h2, night, lit, fade);
                    inside = curtain ? curtainColor * (lerp(_RoomDay, 0.0, night) + _NightColor.rgb * (lit ? 0.6 : 0.0) * night) : inside;
                    inside = blindOn ? blindColor * lerp(_RoomDay, 0.0, night) : inside;
                    float reflectFar = saturate((0.08 + 0.92 * fresnel) * _Reflect);
                    farColor = inside * glass * (1.0 - reflectFar) + env * reflectFar + spec * sun.color;
                }

                half3 color = lerp(nearColor, farColor, far);
                float alpha = lerp(nearAlpha, 1.0, far);

                // Grime: a film that catches the ambient light, over everything behind it.
                color = color * (1.0 - grime) + grimeColor * grime;
                alpha = alpha * (1.0 - grime) + grime;

                // Premultiplied fog: the room behind was fogged where it stands; the glass fogs only what it adds.
                color = MixFogColor(color, unity_FogColor.rgb * alpha, InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor));
                return half4(color, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex UniversalVertexMeta
            #pragma fragment MetaFrag
            #pragma shader_feature EDITOR_VISUALIZATION
            #include "VicWindowInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"

            half4 MetaFrag(Varyings input) : SV_Target
            {
                MetaInput m = (MetaInput)0;
                m.Albedo = half3(0.06, 0.07, 0.08);
                m.Emission = half3(0, 0, 0);
                return UniversalFragmentMeta(input, m);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
