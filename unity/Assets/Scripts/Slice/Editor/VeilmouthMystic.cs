using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDev.Slice.Atmosphere;
using GameDev.Slice.Kit;
using GameDev.VictorianKit;
using GameDev.VictorianKit.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The mystic layer over Veilmouth (Plans/WORLD_STYLE_GUIDE.md §5–§6, city plan §24.5, placed on C5 by §29.4;
    /// owner 2026-11-08: the crimson moon and fog, the Gothic skyline, occult signs). Under one root, <see cref="RootName"/>,
    /// it places the clock-driven <see cref="VeilmouthSky"/> with its day, dusk and night skies and the twilight grade; a
    /// teal gas light in every street lamp; low fog banks on River Veil and the sea by the quay, in Lower Veil's and the
    /// Reek's lanes and yards and the cemetery; faint old wards over about one door in eight in Lower Veil and the Reek and
    /// runes on old stone; and violet-green glass in Harrow &amp; Tincture and a few Lower Veil houses.
    /// <para>
    /// Everything here is seen only: no collider, the NoGrid layer, so the Combat Grid, sight and stealth never read
    /// it, and a ward carries no rule and is no trace of a Weak Point (style guide §6.3). Variants are material assets;
    /// no MaterialPropertyBlock and no new mesh touch the kit's houses. Every size, colour and count is an authoring
    /// default. Run again, it replaces its root.
    /// </para>
    /// </summary>
    public static class VeilmouthMystic
    {
        public const string RootName = "Veilmouth Sky";
        public const string Folder = "Assets/Materials/Slice/Mystic";

        internal static readonly Color Gaslight = new(0.55f, 0.95f, 0.85f);
        // §31 (owner's walk, 2026-11-10): the wards are old chalk and paint on the stone, lit as the wall is and only a
        // little lighter than it, never glowing (authoring default).
        private static readonly Color WardColour = new(0.78f, 0.8f, 0.84f);

        /// <summary>A gas lamp's pool: about one lamp spacing, so the streets keep dark stretches between (style guide §5).</summary>
        private const float LampRange = 10f, LampIntensity = 10f;

        /// <summary>A ward over one door in this many in Lower Veil and Under the Ridge (style guide §6.3).</summary>
        private const int WardEvery = 8;

        /// <summary>A sign's height over a door's sill: the keystone, between the door's head and the first floor's sills.</summary>
        private const float KeystoneHeight = 3.5f;

        private static int _layer;

        [MenuItem("GameDev/Slice/Place Veilmouth Mystic Layer")]
        public static void Place()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            Build();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// The layer on the saved scene, the city's kit check without the rooms' dressing, and its renders.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthMystic.PlaceAndRender
        /// </summary>
        public static void PlaceAndRender()
        {
            Place();
            var blocks = Object.FindObjectsByType<KitBlock>(FindObjectsSortMode.None).Where(b => b.enabled).ToArray();
            var faults = KitLayoutCheck.Check(blocks);
            Debug.Log($"[VeilmouthMystic] city check: {blocks.Length} blocks, {faults.Count} kit faults" +
                      (faults.Count > 0 ? ":\n  " + string.Join("\n  ", faults.Take(40).Select(f => f.ToString())) : "."));
            VeilmouthRenders.RenderMystic();
        }

        /// <summary>
        /// Gives the saved scene's facades their ground (reference plan R3 step 2's fix) without placing the layer again,
        /// and saves the scene. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthMystic.PlaceGround
        /// </summary>
        public static void PlaceGround()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (root == null)
            {
                Debug.LogError($"[VeilmouthMystic] no {RootName} in the scene: place the mystic layer first.");
                return;
            }

            Ground(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>The terrain under the city, the eaves map and the sill map, given to the facade shader so its plinth dirt and wall-foot moss rise from the ground, soot gathers under the cornice and stains run down under the sills.</summary>
        internal static void Ground(GameObject root)
        {
            var ground = root.GetComponent<VicGround>();
            if (ground == null)
            {
                ground = root.AddComponent<VicGround>();
            }

            ground.Terrain = Object.FindFirstObjectByType<Terrain>();
            (ground.Eaves, ground.EavesRect) = VeilmouthEaves.Bake();
            (ground.Sills, ground.SillsRect) = VeilmouthEaves.BakeSills();
            ground.Apply();
            Debug.Log($"[VeilmouthMystic] the facades' ground: {(ground.Terrain != null ? ground.Terrain.name : "none, so world height 0")}; eaves map {(ground.Eaves != null ? $"{ground.Eaves.width} × {ground.Eaves.height}" : "none")}; sill map {(ground.Sills != null ? $"{ground.Sills.width} × {ground.Sills.height}" : "none")}.");
        }

        internal static VeilmouthSky Build()
        {
            foreach (var old in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(t => t.parent == null && t.name == RootName).ToList())
            {
                Object.DestroyImmediate(old.gameObject);
            }

            Directory.CreateDirectory(Folder);
            _layer = LayerMask.NameToLayer(KitLayers.NoGrid);
            Physics.SyncTransforms();
            var root = Make(RootName, null);

            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
            if (sun == null)
            {
                sun = Make("Sun and Moon", root.transform).AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            var (day, dusk, night) = Skies();
            Volume(root.transform, "Post", PostProfile(), 0);
            var twilight = Volume(root.transform, "Twilight", TwilightProfile(), 1);
            var lamps = Lamps(root.transform, out var glassUnlit, out var glassLit);
            var banks = FogBanks(root.transform);
            var wards = Wards(root.transform);
            var windows = StrangeWindows();
            var moon = Moon(root.transform);

            var sky = root.AddComponent<VeilmouthSky>();
            sky.Configure(sun, day, dusk, night, twilight, lamps, glassUnlit, glassLit, banks, moon);
            sky.SetBeacons(VeilmouthCity.Beacons()); // the One-Eye's lantern (city plan §38), when the city has it
            Ground(root);
            sky.Apply(sky.PreviewMinute);
            RenderSettings.skybox = day; // the saved scene holds the asset, never the live blend
            AssetDatabase.SaveAssets();
            Debug.Log($"[VeilmouthMystic] {lamps.Length} gas lamps, {banks.Length} fog banks, {wards} wards, {windows} strange windows.");
            return sky;
        }

        /// <summary>
        /// The reference look's sky and grade (reference plan R3 steps 4 and 7) written again into their assets, which the
        /// saved scene's <see cref="VeilmouthSky"/> and volumes already hold: the scene itself is not touched. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthMystic.Relight
        /// </summary>
        [MenuItem("GameDev/Slice/Relight Veilmouth Sky and Grade")]
        public static void Relight()
        {
            Directory.CreateDirectory(Folder);
            Skies();
            PostProfile();
            TwilightProfile();
            AssetDatabase.SaveAssets();
            Debug.Log("[VeilmouthMystic] the painted skies and the grade written to their assets.");
        }

        // ------------------------------------------------------------------------------------------------ sky

        public const string SkyShader = "GameDev/Slice/Sky";

        /// <summary>Free HDR Sky's two painted skies (owner, 2026-11-08: the sky packs already in the project).</summary>
        private const string DayCube = "Assets/Skybox/Cubemaps/daytime.hdr", SunsetCube = "Assets/Skybox/Cubemaps/sunset.hdr";

        /// <summary>
        /// The day, dusk and night looks on the painted-sky shader. All three hold both cubemaps, so the clock blends their
        /// numbers: by day the clouds of the day sky, washed toward grey and warmed; at dusk the sunset; by night the
        /// sunset's clouds, dark and cool, under the crimson moon. Authoring defaults, in sRGB.
        /// </summary>
        private static (Material Day, Material Dusk, Material Night) Skies()
        {
            var day = Sky("Veilmouth_SkyDay", 0f, new Color(1f, 0.93f, 0.82f), 0.85f, new Color(1f, 0.9f, 0.85f), 1f, 0.22f, 0.18f, 0.9f);
            var dusk = Sky("Veilmouth_SkyDusk", 1f, new Color(1f, 0.92f, 0.86f), 0.6f, new Color(1f, 0.88f, 0.82f), 1f, 0.6f, 0.15f, 0.8f);
            var night = Sky("Veilmouth_SkyNight", 1f, new Color(0.6f, 0.65f, 0.9f), 0.02f, new Color(0.55f, 0.62f, 0.95f), 0.035f, 0.35f, 0.25f, 0.9f);
            return (day, dusk, night);
        }

        private static Material Sky(string name, float blend, Color tintA, float exposureA, Color tintB, float exposureB,
            float saturation, float hazeHeight, float hazeStrength)
        {
            var shader = Shader.Find(SkyShader);
            var sky = Asset(name, () => new Material(shader));
            sky.shader = shader;
            sky.SetTexture("_TexA", AssetDatabase.LoadAssetAtPath<Cubemap>(DayCube));
            sky.SetTexture("_TexB", AssetDatabase.LoadAssetAtPath<Cubemap>(SunsetCube));
            sky.SetFloat("_Blend", blend);
            sky.SetColor("_TintA", tintA);
            sky.SetFloat("_ExposureA", exposureA);
            sky.SetFloat("_RotationA", 0f);
            sky.SetColor("_TintB", tintB);
            sky.SetFloat("_ExposureB", exposureB);
            sky.SetFloat("_RotationB", 0f); // turned by the clock behind the light (VeilmouthSky)
            sky.SetFloat("_Saturation", saturation);
            sky.SetFloat("_HazeHeight", hazeHeight);
            sky.SetFloat("_HazeStrength", hazeStrength);
            EditorUtility.SetDirty(sky);
            return sky;
        }

        /// <summary>
        /// The crimson moon (style guide §6.1): a disc of about 3°, six times the real moon's, with maria, a darkened limb
        /// and a halo, on a quad the sky keeps <see cref="VeilmouthSky.MoonDistance"/> from the eye. Sprites/Default
        /// draws it after the sky without the distance haze, and its HDR tint lets the bloom take its red.
        /// </summary>
        private static Transform Moon(Transform parent)
        {
            var material = Asset("Veilmouth_Moon", () => new Material(Shader.Find("Sprites/Default")));
            material.mainTexture = Texture("Veilmouth_Moon", 256, 256, TextureWrapMode.Clamp, MoonPixel);
            material.SetColor("_Color", new Color(1.5f, 1.5f, 1.5f, 1f));
            EditorUtility.SetDirty(material);
            var go = Make("Crimson Moon", parent);
            go.transform.localScale = Vector3.one * (2f * VeilmouthSky.MoonDistance * Mathf.Tan(2f * Mathf.Deg2Rad));
            go.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            go.SetActive(false);
            return go.transform;
        }

        private static Color MoonPixel(float u, float v)
        {
            const float disc = 0.72f;
            var crimson = new Color(0.85f, 0.2f, 0.17f);
            var r = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            if (r < disc)
            {
                var limb = 0.72f + (0.28f * Mathf.Sqrt(1f - ((r / disc) * (r / disc))));
                var maria = 1f - (0.28f * Mathf.SmoothStep(0f, 1f, (Mathf.PerlinNoise((u * 5f) + 3.3f, (v * 5f) + 8.1f) - 0.45f) * 4f));
                var edge = Mathf.Clamp01((disc - r) * 120f);
                var halo = 0.35f * (1f - edge);
                var c = crimson * (limb * maria);
                return new Color(c.r, c.g, c.b, edge + halo);
            }

            var glow = 1f - ((r - disc) / (1f - disc));
            return new Color(crimson.r, crimson.g, crimson.b, glow > 0f ? 0.35f * glow * glow : 0f);
        }

        private static Volume Volume(Transform parent, string name, VolumeProfile profile, int priority)
        {
            var volume = Make(name, parent).AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = priority;
            volume.sharedProfile = profile;
            return volume;
        }

        /// <summary>
        /// Sets the grade's two profiles again from the code, with nothing else of the mystic layer rebuilt. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthMystic.Regrade
        /// </summary>
        public static void Regrade()
        {
            PostProfile();
            TwilightProfile();
            AssetDatabase.SaveAssets();
            Debug.Log("[VeilmouthMystic] the grade's profiles set from the code.");
        }

        /// <summary>
        /// The grade under every hour (reference plan R3 step 7), graded in HDR: the Neutral tone mapper, colour taken down
        /// toward the reference's muted range, highlights warm and shadows a little cool, and bloom over the HDR highlights
        /// only, so the gas flames and the lit windows glow. Saturation sits at the plan's richer end, −15 (it was −22
        /// until the owner asked for richer views, 2026-11-10), with a little more contrast. Authoring defaults.
        /// </summary>
        private static VolumeProfile PostProfile()
        {
            var profile = Profile("Veilmouth_Post");
            var bloom = Component<Bloom>(profile);
            bloom.intensity.Override(0.5f);
            bloom.threshold.Override(1.2f);
            bloom.scatter.Override(0.65f);
            var tone = Component<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.Neutral);
            var colour = Component<ColorAdjustments>(profile);
            colour.saturation.Override(-15f);
            colour.contrast.Override(10f);
            colour.colorFilter.Override(Color.white);
            var split = Component<SplitToning>(profile);
            split.shadows.Override(new Color(0.45f, 0.49f, 0.55f));
            split.highlights.Override(new Color(0.62f, 0.55f, 0.45f));
            split.balance.Override(10f);
            var white = Component<WhiteBalance>(profile);
            white.temperature.Override(2f); // the sun and the split toning carry the warmth; more turned cream stucco yellow
            return profile;
        }

        /// <summary>The twilight grade (style guide §6.1): shadows toward violet, light toward amber, weighted by the hour.</summary>
        private static VolumeProfile TwilightProfile()
        {
            var profile = Profile("Veilmouth_Twilight");
            var split = Component<SplitToning>(profile);
            split.shadows.Override(new Color(0.42f, 0.32f, 0.62f));
            split.highlights.Override(new Color(1f, 0.72f, 0.42f));
            split.balance.Override(-15f);
            var colour = Component<ColorAdjustments>(profile);
            colour.colorFilter.Override(new Color(1f, 0.9f, 0.95f));
            colour.saturation.Override(-8f); // muted with the day grade (reference plan R3); it was +8 before
            return profile;
        }

        private static VolumeProfile Profile(string name)
        {
            var profile = Asset(name, ScriptableObject.CreateInstance<VolumeProfile>, "asset");
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T Component<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out var component))
            {
                component = profile.Add<T>(true);
                component.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(component, profile);
            }

            return component;
        }

        // ------------------------------------------------------------------------------------------------ lamps

        /// <summary>Every street lamp's glass, lit by a copy that glows, and a teal point light in its head; both burn from 19:00.</summary>
        private static VeilmouthSky.Lamp[] Lamps(Transform parent, out Material unlit, out Material lit)
        {
            unlit = lit = null;
            var group = Make("Gas lamps", parent).transform;
            var lamps = new List<VeilmouthSky.Lamp>();
            foreach (var post in Object.FindObjectsByType<KitBlock>(FindObjectsSortMode.None).Where(b => b.name.StartsWith("Kit_Street_Lamp")))
            {
                var renderers = post.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    continue;
                }

                var top = renderers.Max(r => r.bounds.max.y);
                var centre = renderers.Aggregate(renderers[0].bounds, (b, r) => { b.Encapsulate(r.bounds); return b; }).center;
                var lamp = new VeilmouthSky.Lamp { Slot = -1 };
                foreach (var r in renderers)
                {
                    var slot = Array.FindIndex(r.sharedMaterials, m => m != null && !m.name.StartsWith("Veilmouth_") &&
                                                                       m.name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (slot >= 0)
                    {
                        unlit ??= r.sharedMaterials[slot];
                        lamp.Renderer = r;
                        lamp.Slot = slot;
                        break;
                    }
                }

                var light = Make("Gaslight", group).AddComponent<Light>();
                // In the lantern's glass where the lamp has one of ours (the owner's Tripo lamp, city plan §30.1), else
                // under the head as on the pack's lamp post.
                light.transform.position = lamp.Renderer != null ? lamp.Renderer.bounds.center : new Vector3(centre.x, top - 0.45f, centre.z);
                light.type = LightType.Point;
                light.color = Gaslight;
                light.range = LampRange;
                light.intensity = LampIntensity;
                light.shadows = LightShadows.None;
                light.enabled = false;
                lamp.Light = light;
                lamps.Add(lamp);
            }

            if (unlit != null)
            {
                // Our own glowing pane, not a copy of the pack's glass: its transparent copy dimmed the flame to nothing.
                // Unlit, so it burns at its own colour: the Lit copy's _EMISSION keyword was never kept in the asset
                // (its m_ValidKeywords stayed empty since C3), and the Tripo lamp's panes rendered black at night (2026-11-10).
                var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                lit = Asset("Veilmouth_Gaslight", () => new Material(unlitShader));
                lit.shader = unlitShader;
                lit.SetColor("_BaseColor", Gaslight * 1.15f);
                lit.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                EditorUtility.SetDirty(lit);
            }
            else
            {
                Debug.LogWarning("[VeilmouthMystic] no lamp glass found: the lamps get their light but no glowing glass. Materials of the first lamp: " +
                                 string.Join(", ", Object.FindObjectsByType<KitBlock>(FindObjectsSortMode.None).Where(b => b.name.StartsWith("Kit_Street_Lamp"))
                                     .Take(1).SelectMany(b => b.GetComponentsInChildren<Renderer>()).SelectMany(r => r.sharedMaterials).Select(m => m != null ? m.name : "null")));
            }

            return lamps.ToArray();
        }

        // ------------------------------------------------------------------------------------------------ fog

        /// <summary>
        /// The low fog's places, plan metres (style guide §6.1), from the C5 layout (city plan §29.4): River Veil where it
        /// runs by the town wall and the embankment, the sea before Fishlip Quay, Draught Lane, Rats' Choir, the Black
        /// Puddle, Deaf Boiler Yard and Last Turning Cemetery.
        /// </summary>
        private static IEnumerable<(string Name, float X0, float Y0, float X1, float Y1, bool Water)> FogPlaces(VeilmouthLayout layout)
        {
            var north = layout.Wall("Town wall (north)").t0;
            var quay = layout.SquareById("quay");
            foreach (var r in layout.Rivers)
            {
                // Only the reach the city sees: from the north wall down, and no farther east than the quay.
                yield return ("River Veil", r.x0, Mathf.Max(r.y0, north), Mathf.Min(r.x1, quay.x1), r.y1, true);
            }

            yield return ("Sea by Fishlip Quay", quay.x1, quay.y0, quay.x1 + 40f, quay.y1, true);
            foreach (var id in new[] { "draught", "rats_choir" })
            {
                var lane = layout.StreetById(id);
                yield return (lane.name, lane.a0, lane.c0, lane.a1, lane.c1, false);
            }

            foreach (var id in new[] { "black_puddle", "deaf_boiler", "cemetery" })
            {
                var q = layout.SquareById(id);
                yield return (q.name, q.x0, q.y0, q.x1, q.y1, false);
            }
        }

        private static ParticleSystem[] FogBanks(Transform parent)
        {
            var group = Make("Fog", parent).transform;
            var material = FogMaterial();
            var banks = new List<ParticleSystem>();
            var seed = 1u;
            foreach (var place in FogPlaces(VeilmouthLayout.Load()))
            {
                // Long places go in pieces of at most 40 m, each on its own ground, so the fog follows a slope.
                var alongX = place.X1 - place.X0 >= place.Y1 - place.Y0;
                var length = alongX ? place.X1 - place.X0 : place.Y1 - place.Y0;
                var pieces = Mathf.CeilToInt(length / 40f);
                for (var i = 0; i < pieces; i++)
                {
                    var a = i * length / pieces;
                    var b = (i + 1) * length / pieces;
                    var (x0, y0, x1, y1) = alongX ? (place.X0 + a, place.Y0, place.X0 + b, place.Y1) : (place.X0, place.Y0 + a, place.X1, place.Y0 + b);
                    banks.Add(Bank(group, pieces > 1 ? $"{place.Name} {i + 1}" : place.Name, x0, y0, x1, y1, place.Water, material, seed++));
                }
            }

            return banks.ToArray();
        }

        private static ParticleSystem Bank(Transform parent, string name, float x0, float y0, float x1, float y1, bool water, Material material, uint seed)
        {
            var w = x1 - x0;
            var d = y1 - y0;
            var (cx, cy) = ((x0 + x1) / 2f, (y0 + y1) / 2f);
            var ground = Mathf.Min(Surface(x0 + (w * 0.25f), cy), Surface(x1 - (w * 0.25f), cy), Surface(cx, y0 + (d * 0.25f)), Surface(cx, y1 - (d * 0.25f)));
            if (water)
            {
                ground = Mathf.Max(ground, VeilmouthScene.SeaLevel);
            }

            var top = Mathf.Max(Surface(x0 + (w * 0.25f), cy), Surface(x1 - (w * 0.25f), cy), Surface(cx, y0 + (d * 0.25f)), Surface(cx, y1 - (d * 0.25f)));
            var depth = Mathf.Max(top - ground, 0f) + 1.2f;
            var go = Make($"Fog - {name}", parent);
            go.transform.position = new Vector3(cx - 500f, ground + 0.6f + (depth / 2f), 500f - cy);
            var size = Mathf.Min(8f, (0.9f * Mathf.Min(w, d)) + 2f);
            var count = Mathf.Clamp(Mathf.RoundToInt(w * d / 10f), 12, 220);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed = false;
            ps.randomSeed = seed;
            var main = ps.main;
            main.duration = 24f;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(16f, 24f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = count;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = count / 20f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(w, depth, d);
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            colour.color = fade;
            var spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.maxParticleSize = 1f;
            ps.Play();
            return ps;
        }

        /// <summary>
        /// Unlit, soft, fading near the camera: VeilmouthSky colours the fog by the hour, so it reads by night too, where
        /// a lit particle under the dim moon went black, and it never walls off the view.
        /// </summary>
        private static Material FogMaterial()
        {
            var texture = Texture("Veilmouth_FogPuff", 128, 128, TextureWrapMode.Clamp, Puff);
            var material = Asset("Veilmouth_Fog", () => new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")));
            material.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.DisableKeyword("_EMISSION");
            Transparent(material);
            // No soft particles: they faded every puff to nothing in the renders (measured 2026-11-08, the fog probe), and
            // the puff's own soft edge already hides where a billboard meets the ground or a wall.
            material.SetFloat("_SoftParticlesEnabled", 0f);
            material.DisableKeyword("_SOFTPARTICLES_ON");
            material.SetFloat("_CameraFadingEnabled", 1f);
            material.EnableKeyword("_FADING_ON");
            material.SetFloat("_CameraNearFadeDistance", 1f);
            material.SetFloat("_CameraFarFadeDistance", 4f);
            material.SetVector("_CameraFadeParams", new Vector4(1f, 1f / 3f, 0f, 0f));
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>A soft, cloudy round puff: a radial fall-off broken by two octaves of noise.</summary>
        private static Color Puff(float u, float v)
        {
            var r = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            var noise = (Mathf.PerlinNoise(u * 4f + 11.3f, v * 4f + 3.7f) * 0.65f) + (Mathf.PerlinNoise(u * 9f + 5.1f, v * 9f + 17.9f) * 0.35f);
            var a = Mathf.Clamp01(1f - r) * Mathf.Clamp01(1f - r);
            return new Color(1f, 1f, 1f, Mathf.Clamp01(a * (0.45f + (0.9f * noise))));
        }

        // ------------------------------------------------------------------------------------------------ wards

        private static int Wards(Transform parent)
        {
            var group = Make("Wards", parent).transform;
            var sign = WardMaterial("Veilmouth_WardSign", Texture("Veilmouth_WardSign", 256, 256, TextureWrapMode.Clamp, Sign), 0.42f);
            var runes = WardMaterial("Veilmouth_WardRunes", Texture("Veilmouth_WardRunes", 512, 64, TextureWrapMode.Repeat, RuneRow()), 0.34f);
            var quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var count = 0;

            // Over the keystone of about one door in eight in Lower Veil and the Reek, none in the richer districts (C5, §29.4).
            var layout = VeilmouthLayout.Load();
            var poor = layout.districts.Where(d => d.name == "LOWER VEIL" || d.name == "THE REEK").ToList();
            var city = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t => t.parent == null && t.name == VeilmouthCity.RootName);
            var doors = city == null ? new List<Transform>() : city.GetComponentsInChildren<Transform>().Where(t => t.name == VictorianHouses.DoorMarker).ToList();
            foreach (var door in doors.OrderBy(t => t.position.x).ThenBy(t => t.position.z))
            {
                var (px, py) = (door.position.x + 500f, 500f - door.position.z);
                var hash = (Mathf.RoundToInt(px * 10f) * 73856093) ^ (Mathf.RoundToInt(py * 10f) * 19349663);
                if (!poor.Any(d => px >= d.x0 && px <= d.x1 && py >= d.y0 && py <= d.y1) || Math.Abs(hash % WardEvery) != 0)
                {
                    continue;
                }

                var from = door.position + (Vector3.up * KeystoneHeight) + (door.forward * 1.2f);
                if (OnWall(from, -door.forward, 2.5f, out var hit) && Put(group, "Ward - door", quad, sign, hit, new Vector2(0.5f, 0.5f)))
                {
                    count++;
                }
            }

            var poorDoors = doors.Count(t => poor.Any(d => t.position.x + 500f >= d.x0 && t.position.x + 500f <= d.x1 && 500f - t.position.z >= d.y0 && 500f - t.position.z <= d.y1));
            Debug.Log($"[VeilmouthMystic] {count} wards over {poorDoors} doors in Lower Veil and the Reek.");

            // Runes on old stone (C5, §29.4): the town wall either side of the Creaking Gate and along the north, the works'
            // walls and the brick blocks either side of Furnace Road. Plan directions: Vector2.down is north.
            var gate = layout.Wall("Town wall (west)");
            var gap = gate.gaps[0];
            var northWall = layout.Wall("Town wall (north)");
            var furnace = layout.StreetById("works_s");
            var clinker = layout.StreetById("works_x");
            var worksWest = layout.Wall("Works wall (west)");
            var worksSouth = layout.Wall("Works wall (south)");
            var rays = new List<(float px, float py, Vector2 dir, string name)>
            {
                (gate.t1 + 6f, gap.a - 3f, Vector2.left, "Creaking Gate"), (gate.t1 + 6f, gap.b + 3f, Vector2.left, "Creaking Gate"),
                (430f, northWall.t1 + 6f, Vector2.down, "north wall"), (590f, northWall.t1 + 6f, Vector2.down, "north wall"),
                (670f, northWall.t1 + 6f, Vector2.down, "north wall"),
                (worksWest.t1 + 6f, (clinker.c0 + clinker.c1) / 2f, Vector2.left, "works wall"),
                ((furnace.c0 + furnace.c1) / 2f, worksSouth.t0 - 6f, Vector2.up, "works wall"),
                (furnace.c0 + 2f, 612f, Vector2.left, "works"), (furnace.c1 - 2f, 612f, Vector2.right, "works"),
            };
            foreach (var (px, py, dir, name) in rays)
            {
                count += Runes(group, quad, runes, px, py, dir, 1.9f, 12f, name) ? 1 : 0;
            }

            // The three Ritual Site Candidates: the two nearest walls round each, from the circle's middle past its rim.
            foreach (var c in layout.candidates)
            {
                var (px, py, name, reach) = (c.x, c.y, c.name, (c.size / 2f) + 10f);
                var around = Enumerable.Range(0, 8)
                    .Select(k => new Vector2(Mathf.Cos(k * Mathf.PI / 4f), Mathf.Sin(k * Mathf.PI / 4f)))
                    .Select(dir => (dir, hit: OnWall(At(px, py, 1.9f), new Vector3(dir.x, 0f, -dir.y), reach, out var h) ? h : (RaycastHit?)null))
                    .Where(p => p.hit.HasValue).OrderBy(p => p.hit.Value.distance).Take(2).ToList();
                foreach (var (_, hit) in around)
                {
                    count += Put(group, $"Runes - {name}", quad, runes, hit.Value, new Vector2(2.4f, 0.3f)) ? 1 : 0;
                }
            }

            return count;
        }

        /// <summary>A rune strip on the first wall from a plan point along a plan direction (plan y grows south, world z north).</summary>
        private static bool Runes(Transform group, Mesh quad, Material runes, float px, float py, Vector2 dir, float up, float reach, string name)
        {
            if (!OnWall(At(px, py, up), new Vector3(dir.x, 0f, -dir.y), reach, out var hit))
            {
                Debug.LogWarning($"[VeilmouthMystic] no wall for the runes at the {name} from ({px}, {py}).");
                return false;
            }

            return Put(group, $"Runes - {name}", quad, runes, hit, new Vector2(2.4f, 0.3f));
        }

        /// <summary>Casts for an upright wall at least 2 m wide: a lamp post, a bench or a tree is not old stone.</summary>
        private static bool OnWall(Vector3 from, Vector3 direction, float reach, out RaycastHit wall)
        {
            foreach (var hit in Physics.RaycastAll(from, direction.normalized, reach).OrderBy(h => h.distance))
            {
                var size = hit.collider.bounds.size;
                if (Mathf.Abs(hit.normal.y) < 0.2f && Mathf.Max(size.x, size.z) >= 2f && size.y >= 1.2f)
                {
                    wall = hit;
                    return true;
                }
            }

            wall = default;
            return false;
        }

        /// <summary>
        /// A ward on the wall a ray hit, only where the wall's face lies under the whole of it (§31, the owner's walk of
        /// 2026-11-10: strips hung past a wall's end): slid along the wall up to 1.5 m, then narrowed to two thirds, to
        /// find such a place, else not put.
        /// </summary>
        private static bool Put(Transform group, string name, Mesh quad, Material material, RaycastHit hit, Vector2 size)
        {
            var along = Vector3.Cross(Vector3.up, hit.normal).normalized;
            foreach (var width in new[] { size.x, size.x * 2f / 3f })
            {
                for (var k = 0; k <= 12; k++)
                {
                    var centre = hit.point + (along * ((k % 2 == 0 ? 1f : -1f) * ((k + 1) / 2) * 0.25f));
                    if (!Covered(centre, hit.normal, along, new Vector2(width, size.y)))
                    {
                        continue;
                    }

                    var go = Make(name, group);
                    // Off the collider's face by the depth of the kit's mouldings, so a string course never swallows the sign.
                    go.transform.SetPositionAndRotation(centre + (hit.normal * 0.06f), Quaternion.LookRotation(-hit.normal, Vector3.up));
                    go.transform.localScale = new Vector3(width, size.y, 1f);
                    go.AddComponent<MeshFilter>().sharedMesh = quad;
                    var renderer = go.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = true;
                    return true;
                }
            }

            Debug.LogWarning($"[VeilmouthMystic] no whole face of wall for the {name} at ({hit.point.x + 500f:0.0}, {500f - hit.point.z:0.0}).");
            return false;
        }

        /// <summary>Whether one flat face lies under a ward's whole rectangle: 5 × 3 rays onto it, each 0.5 m ± 0.1 m and square to it.</summary>
        private static bool Covered(Vector3 centre, Vector3 normal, Vector3 along, Vector2 size)
        {
            for (var u = -2; u <= 2; u++)
            {
                for (var v = -1; v <= 1; v++)
                {
                    var p = centre + (along * (u * size.x / 4f)) + (Vector3.up * (v * size.y / 2f));
                    if (!Physics.Raycast(p + (normal * 0.5f), -normal, out var h, 0.8f, ~0, QueryTriggerInteraction.Ignore) ||
                        Mathf.Abs(h.distance - 0.5f) > 0.1f || Vector3.Dot(h.normal, normal) < 0.95f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static Material WardMaterial(string name, Texture2D texture, float alpha)
        {
            const string lit = "Universal Render Pipeline/Simple Lit";
            var material = Asset(name, () => new Material(Shader.Find(lit)));
            material.shader = Shader.Find(lit);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", new Color(WardColour.r, WardColour.g, WardColour.b, alpha));
            material.SetColor("_SpecColor", Color.black);
            material.SetFloat("_Smoothness", 0f);
            Transparent(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>The door sign: two rings, an eye of three strokes and a fallen triangle, centuries worn.</summary>
        private static Color Sign(float u, float v)
        {
            var p = new Vector2(u - 0.5f, v - 0.5f) * 2f;
            var r = p.magnitude;
            var d = Mathf.Min(Mathf.Abs(r - 0.9f), Mathf.Abs(r - 0.66f));
            d = Mathf.Min(d, Segment(p, new Vector2(0f, -0.66f), new Vector2(0f, 0.66f)));
            d = Mathf.Min(d, Segment(p, new Vector2(-0.5f, 0.3f), new Vector2(0.5f, 0.3f)));
            d = Mathf.Min(d, Segment(p, new Vector2(-0.5f, 0.3f), new Vector2(0f, -0.5f)));
            d = Mathf.Min(d, Segment(p, new Vector2(0.5f, 0.3f), new Vector2(0f, -0.5f)));
            for (var k = 0; k < 6; k++)
            {
                var a = (k * Mathf.PI / 3f) + 0.26f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                d = Mathf.Min(d, Segment(p, dir * 0.9f, dir * 0.99f));
            }

            return Stroke(d, 0.035f, u, v);
        }

        /// <summary>A row of eight runes, each two to four strokes between the points of a 3 × 4 lattice in its cell.</summary>
        private static Func<float, float, Color> RuneRow()
        {
            var random = new Random(1888);
            var strokes = new List<(Vector2, Vector2)>();
            for (var cell = 0; cell < 8; cell++)
            {
                Vector2 Node(int i, int j) => new Vector2(cell + 0.25f + (i * 0.25f), 0.15f + (j * 0.233f));
                var n = 2 + random.Next(3);
                for (var s = 0; s < n; s++)
                {
                    var a = Node(random.Next(3), random.Next(4));
                    var b = Node(random.Next(3), random.Next(4));
                    if (a != b)
                    {
                        strokes.Add((a, b));
                    }
                }

                strokes.Add((Node(1, 0), Node(1, 3))); // every rune stands on its stem
            }

            return (u, v) =>
            {
                var p = new Vector2(u * 8f, v);
                var d = strokes.Min(s => Segment(p, s.Item1, s.Item2));
                return Stroke(d, 0.05f, u * 8f, v);
            };
        }

        private static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            return Vector2.Distance(p, a + (ab * t));
        }

        /// <summary>A worn stroke: soft edged, its strength broken by noise as old stone loses its cut.</summary>
        private static Color Stroke(float distance, float width, float u, float v)
        {
            var edge = Mathf.Clamp01(1f - ((distance - width) / (width * 0.8f)));
            var wear = 0.55f + (0.45f * Mathf.PerlinNoise(u * 13f + 2.1f, v * 13f + 7.4f));
            return new Color(1f, 1f, 1f, edge * wear);
        }

        // ------------------------------------------------------------------------------------------------ windows

        /// <summary>Violet-green glass, a material variant of the kit's poor glass, in Harrow &amp; Tincture and three Lower Veil shops.</summary>
        private static int StrangeWindows()
        {
            var poor = VicMaterials.Load("Glass_Poor");
            var rich = VicMaterials.Load("Glass_Rich");
            if (poor == null)
            {
                Debug.LogWarning("[VeilmouthMystic] the kit's glass is missing: no strange windows.");
                return 0;
            }

            var strange = Asset("Veilmouth_Glass_Strange", () => new Material(poor));
            strange.CopyPropertiesFromMaterial(poor);
            strange.SetColor("_RoomTint", new Color(0.55f, 0.95f, 0.7f));
            strange.SetColor("_CurtainColor", new Color(0.22f, 0.12f, 0.32f));
            strange.SetColor("_NightColor", new Color(0.55f, 0.42f, 0.85f));
            strange.SetFloat("_LitFraction", 1f);
            strange.SetFloat("_NightIntensity", 0.75f); // dim: the first render's 1.4 shone like a shop's gas
            strange.SetFloat("_RoomDay", 0.35f);
            EditorUtility.SetDirty(strange);

            var city = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t => t.parent == null && t.name == VeilmouthCity.RootName);
            if (city == null)
            {
                return 0;
            }

            var all = city.GetComponentsInChildren<Transform>();
            // Three of Lower Veil's houses spread through the district (C5, §29.4), besides Harrow & Tincture.
            var veil = VeilmouthLayout.Load().districts.First(d => d.name == "LOWER VEIL");
            var houses = all.Where(t => t.parent != null && t.parent.name == "Houses" && t.GetComponentsInChildren<Renderer>(true)
                    .Any(r => r.sharedMaterials.Any(m => m == poor || m == rich)))
                .Where(t => t.position.x + 500f >= veil.x0 && t.position.x + 500f <= veil.x1 && 500f - t.position.z >= veil.y0 && 500f - t.position.z <= veil.y1)
                .OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToList();
            var chosen = all.Where(t => t.name == "Harrow & Tincture")
                .Concat(Enumerable.Range(0, Math.Min(3, houses.Count)).Select(i => houses[((2 * i) + 1) * houses.Count / 6])).Distinct().ToList();
            Debug.Log($"[VeilmouthMystic] strange windows in {string.Join(", ", chosen.Select(t => $"{t.name} ({t.position.x + 500f:0}, {500f - t.position.z:0})"))}");
            foreach (var house in chosen)
            {
                foreach (var r in house.GetComponentsInChildren<Renderer>(true))
                {
                    var materials = r.sharedMaterials;
                    var changed = false;
                    for (var i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] == poor || materials[i] == rich)
                        {
                            materials[i] = strange;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        r.sharedMaterials = materials;
                    }
                }
            }

            return chosen.Count;
        }

        // ------------------------------------------------------------------------------------------------ helpers

        private static GameObject Make(string name, Transform parent)
        {
            var go = new GameObject(name) { layer = _layer };
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            return go;
        }

        /// <summary>The highest surface at a plan point, from above: a street, a stair, a deck or the ground.</summary>
        private static float Surface(float px, float py)
        {
            var from = new Vector3(px - 500f, 200f, 500f - py);
            var terrain = Terrain.activeTerrain;
            var ground = terrain != null ? terrain.SampleHeight(from) + terrain.transform.position.y : VeilmouthScene.StreetLevel;
            return Physics.Raycast(from, Vector3.down, out var hit, 400f) && hit.point.y < ground + 12f ? Mathf.Max(hit.point.y, ground) : ground;
        }

        private static Vector3 At(float px, float py, float up) => new(px - 500f, Surface(px, py) + up, 500f - py);

        private static void Transparent(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static T Asset<T>(string name, Func<T> create, string extension = "mat") where T : Object
        {
            var path = $"{Folder}/{name}.{extension}";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = create();
                asset.name = name;
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        /// <summary>A generated texture, written as a PNG and imported with its alpha: our own, from no pack.</summary>
        private static Texture2D Texture(string name, int width, int height, TextureWrapMode wrap, Func<float, float, Color> pixel)
        {
            var path = $"{Folder}/{name}.png";
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    image.SetPixel(x, y, pixel((x + 0.5f) / width, (y + 0.5f) / height));
                }
            }

            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.wrapMode = wrap;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
