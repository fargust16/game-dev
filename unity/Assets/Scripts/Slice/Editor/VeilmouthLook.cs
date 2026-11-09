using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The reference look's settings outside the city scene (Plans/VEILMOUTH_REFERENCE_PLAN.md R3; owner, 2026-11-08):
    /// the Linear colour space (D2), colour graded in HDR, screen-space ambient occlusion on the one renderer, and the
    /// common trees' foliage retinted toward the reference's olive (D7 (a)). Each step can run again and changes nothing
    /// the second time. Every number is an authoring default.
    /// </summary>
    public static class VeilmouthLook
    {
        public const string PipelinePath = "Assets/URP_Pipeline.asset";
        public const string RendererPath = "Assets/URP_Renderer.asset";
        public const string Folder = "Assets/Materials/Slice/Look";

        private const string Polytope = "Assets/Polytope Studio/Lowpoly_Environments/Sources/Materials/";

        /// <summary>
        /// The common trees' and shrubs' leaves (Polytope Studio): a project copy of each with the pack's own two-colour
        /// gradient, top and ground in sRGB, olive and dark olive instead of the pack's bright greens and teal, and its
        /// translucency lowered so backlit leaves do not glow green.
        /// </summary>
        private static readonly (string Vendor, string Copy, Color Top, Color Ground)[] Foliage =
        {
            ("PT_Fruit_Tree_Foliage_Mat", "Veilmouth_FruitTree_Leaves", new Color(0.46f, 0.44f, 0.25f), new Color(0.22f, 0.23f, 0.12f)),
            ("PT_Pine_Tree_Leaves_Mat", "Veilmouth_PineTree_Leaves", new Color(0.27f, 0.31f, 0.23f), new Color(0.1f, 0.13f, 0.1f)),
            ("PT_Generic_Tree_Leaves_mat", "Veilmouth_Shrub_Leaves", new Color(0.42f, 0.4f, 0.23f), new Color(0.2f, 0.2f, 0.11f)),
        };

        private const float FoliageTranslucency = 2.5f;

        /// <summary>
        /// The leaves' own wind against the pack's (owner, fifth play test, 2026-11-09: a slight stir, not a sway): its
        /// strength ×0.4 and its movement ×0.5. Authoring defaults.
        /// </summary>
        private const float FoliageWindStrength = 0.4f, FoliageWindMovement = 0.5f;

        /// <summary>
        /// The city's street and garden tree (owner, fifth play test, 2026-11-09): Polytope's fruit tree, which the owner
        /// liked by the farms, in a fuller green than the forest's olive, sRGB. Authoring defaults.
        /// </summary>
        private static readonly (string Vendor, string Copy, Color Top, Color Ground) CityFruit =
            ("PT_Fruit_Tree_Foliage_Mat", "City/City Fruit Tree Leaves", new Color(0.36f, 0.50f, 0.20f), new Color(0.13f, 0.22f, 0.08f));

        /// <summary>
        /// The street surfaces' mean albedo in sRGB (reference plan R3 step 3): the carriageways' setts cool grey, the
        /// slabs warm grey, the squares', lanes' and yards' cobbles a warmer grey-brown, all inside the facades' range.
        /// </summary>
        private static readonly (string Layer, Color Look)[] Streets =
        {
            ("Setts", new Color(0.34f, 0.34f, 0.35f)),
            ("Flags", new Color(0.50f, 0.48f, 0.45f)),
            ("Cobbles", new Color(0.42f, 0.40f, 0.37f)),
        };

        /// <summary>
        /// Everything R3 sets outside the scene, after <see cref="ApplyProjectSettings"/> has run: the sky materials and
        /// the grades, the kit's palette, the retinted trees, the street surfaces. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthLook.ApplyLook
        /// </summary>
        [MenuItem("GameDev/Slice/Reference Look/Apply Look")]
        public static void ApplyLook()
        {
            VeilmouthMystic.Relight();
            GameDev.VictorianKit.EditorTools.VicMaterials.BuildAll();
            RetintTrees();
            TuneStreets();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// The look applied and R3's check rendered in one run: the five views at four hours and the palette sheet.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthLook.ApplyAndRender -lookLabel name
        /// </summary>
        public static void ApplyAndRender()
        {
            ApplyLook();
            VeilmouthRenders.RenderLook();
            VeilmouthRenders.RenderPalette();
        }

        /// <summary>
        /// Reference plan §4 step 4 in one run: the shadows, the mapped rooms' tones, the room turn rendered before the
        /// houses are told their rooms, every house's rooms built again with their panes told (RedressAll), the room
        /// turn again, the reflections baked, and the five views. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthLook.Step4AndRender -lookLabel s4
        /// </summary>
        public static void Step4AndRender()
        {
            ApplyShadows();
            GameDev.VictorianKit.EditorTools.VicMaterials.BuildAll();
            AssetDatabase.SaveAssets();
            VeilmouthRenders.RenderRoomTurn("roomturn_before");
            GameDev.VictorianKit.EditorTools.VicRooms.PanesTold = 0;
            GameDev.VictorianKit.EditorTools.VicRooms.PanesUntold = 0;
            VeilmouthKitHouses.RedressAll();
            Debug.Log($"[VeilmouthLook] panes told their room: {GameDev.VictorianKit.EditorTools.VicRooms.PanesTold}, untold: {GameDev.VictorianKit.EditorTools.VicRooms.PanesUntold}.");
            VeilmouthRenders.RenderRoomTurn("roomturn_after");
            VeilmouthReflections.Bake();
            VeilmouthRenders.RenderLook();
            VeilmouthRenders.RenderRoomTurn("roomturn_probes");
        }

        /// <summary>
        /// Brings the slice's three street layers to <see cref="Streets"/> by the layer's own tint, measured against its
        /// texture's mean. The layers are tuned in the Inspector (VeilmouthCity), so a layer whose tint is no longer the
        /// default is left as it is: this is the agent's one pass, never an override of the owner's.
        /// </summary>
        [MenuItem("GameDev/Slice/Reference Look/Tune Street Surfaces")]
        public static void TuneStreets()
        {
            foreach (var (name, look) in Streets)
            {
                var path = $"{VeilmouthCity.LayerFolder}/Slice_{name}.terrainlayer";
                var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                if (layer == null || layer.diffuseTexture == null)
                {
                    Debug.LogError($"[VeilmouthLook] {path} or its texture is missing.");
                    continue;
                }

                if (layer.diffuseRemapMax != Vector4.one || layer.diffuseRemapMin != Vector4.zero)
                {
                    Debug.Log($"[VeilmouthLook] {name} already tuned ({layer.diffuseRemapMax}); left as it is.");
                    continue;
                }

                var mean = GameDev.VictorianKit.EditorTools.VicTextures.LinearMean(layer.diffuseTexture);
                float Scale(float target, float m) => Mathf.Clamp(Mathf.GammaToLinearSpace(target) / Mathf.Max(m, 0.01f), 0f, 1.5f);
                layer.diffuseRemapMax = new Vector4(Scale(look.r, mean.r), Scale(look.g, mean.g), Scale(look.b, mean.b), 1f);
                EditorUtility.SetDirty(layer);
                Debug.Log($"[VeilmouthLook] {name}: texture linear mean {mean}, tint {layer.diffuseRemapMax}.");
            }
        }

        /// <summary>
        /// The colour space, the HDR grade and the occlusion. Changing the colour space reimports every texture, so this
        /// runs on its own before anything is rendered. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthLook.ApplyProjectSettings
        /// </summary>
        [MenuItem("GameDev/Slice/Reference Look/Apply Project Settings")]
        public static void ApplyProjectSettings()
        {
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                PlayerSettings.colorSpace = ColorSpace.Linear;
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            pipeline.colorGradingMode = ColorGradingMode.HighDynamicRange;
            EditorUtility.SetDirty(pipeline);
            Shadows(pipeline);
            Occlusion(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath));
            AssetDatabase.SaveAssets();
            Debug.Log($"[VeilmouthLook] colour space {PlayerSettings.colorSpace}, grading {pipeline.colorGradingMode}, occlusion on {RendererPath}.");
        }

        /// <summary>
        /// The sun's shadows sized to the city (reference plan R2 step 7): a 4096 px map over 150 m in four cascades, at
        /// 10, 30 and 70 m, where it was 2048 px over 350 m (cascades at 23, 70 and 163 m), so the nearest cascade holds
        /// about 1 cm a texel instead of 4.5 cm; the last 20 % fades out, and beyond 150 m the haze of R3 carries the
        /// depth. Soft shadows at High quality, so their edges are smooth up close (R3 step 6). Authoring defaults.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthLook.ApplyShadows
        /// </summary>
        [MenuItem("GameDev/Slice/Reference Look/Apply Shadows")]
        public static void ApplyShadows()
        {
            Shadows(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath));
            AssetDatabase.SaveAssets();
        }

        private static void Shadows(UniversalRenderPipelineAsset pipeline)
        {
            pipeline.mainLightShadowmapResolution = 4096;
            pipeline.shadowDistance = 150f;
            pipeline.shadowCascadeCount = 4;
            pipeline.cascade4Split = new Vector3(0.067f, 0.2f, 0.467f);
            pipeline.cascadeBorder = 0.2f;
            var so = new SerializedObject(pipeline);
            so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            so.FindProperty("m_SoftShadowQuality").intValue = (int)SoftShadowQuality.High;
            // The street probes (VeilmouthReflections) blend where they meet.
            so.FindProperty("m_ReflectionProbeBlending").boolValue = true;
            so.FindProperty("m_ReflectionProbeBoxProjection").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            Debug.Log($"[VeilmouthLook] shadows {pipeline.mainLightShadowmapResolution} px over {pipeline.shadowDistance} m, {pipeline.shadowCascadeCount} cascades.");
        }

        /// <summary>
        /// The foliage copies, swapped for the pack's materials in every kit wrapper that wears them, in place, so the
        /// scene's instances keep their links. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthLook.RetintTrees
        /// </summary>
        [MenuItem("GameDev/Slice/Reference Look/Retint Trees")]
        public static void RetintTrees()
        {
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory(CityFolder);
            foreach (var leaf in Foliage.Append(CityFruit))
            {
                Copy(leaf);
            }

            AssetDatabase.SaveAssets();
            var changed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { SliceKitCatalogue.Folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!AssetDatabase.GetDependencies(path, true).Any(d => Foliage.Any(f => d == Polytope + f.Vendor + ".mat")))
                {
                    continue;
                }

                var root = PrefabUtility.LoadPrefabContents(path);
                if (Retint(root) > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed++;
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log($"[VeilmouthLook] {Foliage.Length} foliage materials retinted, {changed} kit wrappers changed.");
        }

        /// <summary>
        /// Swaps the pack's foliage for its olive copy on every renderer under <paramref name="root"/>, where the copy
        /// exists; the kit catalogue calls it when it wraps a model, so a rebuilt wrapper keeps the retint.
        /// </summary>
        public static int Retint(GameObject root) =>
            Swap(root, Foliage
                .Select(f => (From: AssetDatabase.LoadAssetAtPath<Material>(Polytope + f.Vendor + ".mat"), To: AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/{f.Copy}.mat")))
                .Where(s => s.From != null && s.To != null)
                .ToDictionary(s => s.From, s => s.To));

        /// <summary>
        /// The city's trees and bushes (city plan §25.1, owner 2026-11-09): the Stylized Trees Pack's leaves swapped for
        /// project copies in the city's muted green, made on first use. The catalogue calls it for the <c>City*</c>
        /// wrappers only; the forest's fellable trees keep the pack's colours as resource markers.
        /// </summary>
        public static int RetintCity(GameObject root)
        {
            var swaps = CityFoliage
                .Select(f => (From: AssetDatabase.LoadAssetAtPath<Material>(StylizedFoliage + f.Vendor + ".mat"), To: CityCopy(f.Vendor, f.Shade)))
                .Where(s => s.From != null && s.To != null)
                .ToDictionary(s => s.From, s => s.To);

            // The fruit tree's leaves, the pack's or the forest's olive copy, in the city's fuller green.
            var path = $"{Folder}/{CityFruit.Copy}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            {
                Directory.CreateDirectory(CityFolder);
                Copy(CityFruit);
                AssetDatabase.SaveAssets();
            }

            var fruit = AssetDatabase.LoadAssetAtPath<Material>(path);
            foreach (var from in new[] { Polytope + CityFruit.Vendor + ".mat", $"{Folder}/{Foliage[0].Copy}.mat" }.Select(AssetDatabase.LoadAssetAtPath<Material>))
            {
                if (from != null && fruit != null)
                {
                    swaps[from] = fruit;
                }
            }

            return Swap(root, swaps);
        }

        private const string StylizedFoliage = "Assets/Stylized Trees Pack/Assets/Materials/Foliage/";
        public const string CityFolder = Folder + "/City";

        /// <summary>The pack's leaf materials the trees and bushes wear, each copy a little lighter or darker for variety.</summary>
        private static readonly (string Vendor, float Shade)[] CityFoliage = { ("Foliage 001", 1f), ("Foliage 002", 1.06f), ("Foliage 004", 0.92f) };

        /// <summary>
        /// The city's green in sRGB, from the shadowed inside of a crown to its lit top, on the pack's own gradient
        /// (its middle key at 0.78, as the pack's): darker and greyer than the pack's teal and lime, a little greener
        /// than the forest's olive, so it reads against the stone without shouting. Authoring defaults.
        /// </summary>
        private static readonly (Color Colour, float Time)[] CityGreen =
        {
            (new Color(0.12f, 0.16f, 0.10f), 0f), (new Color(0.30f, 0.37f, 0.20f), 0.78f), (new Color(0.56f, 0.60f, 0.36f), 1f),
        };

        /// <summary>
        /// A copy of a pack leaf material with a gradient texture of its own, a 256 × 1 sRGB strip stored inside the copy
        /// and named as the pack's drawer names it, so its Inspector still reads and edits the gradient.
        /// </summary>
        private static Material CityCopy(string vendor, float shade)
        {
            var path = $"{CityFolder}/City {vendor}.mat";
            var copy = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (copy != null)
            {
                return copy;
            }

            var source = AssetDatabase.LoadAssetAtPath<Material>(StylizedFoliage + vendor + ".mat");
            if (source == null)
            {
                Debug.LogError($"[VeilmouthLook] {vendor} is missing from the Stylized Trees Pack.");
                return null;
            }

            Directory.CreateDirectory(CityFolder);
            copy = new Material(source) { name = "City " + vendor };
            AssetDatabase.CreateAsset(copy, path);
            var keys = CityGreen.Select(k => (Colour: new Color(Mathf.Clamp01(k.Colour.r * shade), Mathf.Clamp01(k.Colour.g * shade), Mathf.Clamp01(k.Colour.b * shade), 1f), k.Time)).ToArray();
            var gradient = new Gradient();
            gradient.SetKeys(keys.Select(k => new GradientColorKey(k.Colour, k.Time)).ToArray(), new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            string F(float v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            var json = "{\"mode\":0,\"colorKeys\":[" +
                       string.Join(",", keys.Select(k => $"{{\"color\":{{\"r\":{F(k.Colour.r)},\"g\":{F(k.Colour.g)},\"b\":{F(k.Colour.b)},\"a\":1.0}},\"time\":{F(k.Time)}}}")) +
                       "],\"alphaKeys\":[{\"alpha\":1.0,\"time\":0.0},{\"alpha\":1.0,\"time\":1.0}]}";
            var texture = new Texture2D(256, 1, TextureFormat.ARGB32, false, false)
            {
                name = "_Gradient_gradienttexture Texture" + json,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels(Enumerable.Range(0, 256).Select(x => gradient.Evaluate(x / 255f)).ToArray());
            texture.Apply(false, false);
            AssetDatabase.AddObjectToAsset(texture, copy);
            copy.SetTexture("_Gradient_gradienttexture", texture);
            copy.SetTexture("_TextureSample1", texture);
            EditorUtility.SetDirty(copy);
            AssetDatabase.SaveAssets();
            return copy;
        }

        private static int Swap(GameObject root, System.Collections.Generic.Dictionary<Material, Material> swaps)
        {
            var count = 0;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = r.sharedMaterials;
                var hit = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && swaps.TryGetValue(materials[i], out var to))
                    {
                        materials[i] = to;
                        hit = true;
                    }
                }

                if (hit)
                {
                    r.sharedMaterials = materials;
                    count++;
                }
            }

            return count;
        }

        private static void Copy((string Vendor, string Copy, Color Top, Color Ground) leaf)
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(Polytope + leaf.Vendor + ".mat");
            if (source == null)
            {
                Debug.LogError($"[VeilmouthLook] {leaf.Vendor} is missing from Polytope Studio.");
                return;
            }

            var path = $"{Folder}/{leaf.Copy}.mat";
            var copy = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (copy == null)
            {
                copy = new Material(source) { name = leaf.Copy };
                AssetDatabase.CreateAsset(copy, path);
            }
            else
            {
                copy.CopyPropertiesFromMaterial(source);
            }

            // The pack's own toggle: the leaves take a gradient from the ground colour to the top colour.
            copy.SetFloat("_CUSTOMCOLORSTINTING", 1f);
            copy.EnableKeyword("_CUSTOMCOLORSTINTING_ON");
            copy.SetColor("_TopColor", leaf.Top);
            copy.SetColor("_GroundColor", leaf.Ground);
            copy.SetFloat("_Gradient", 1f);
            copy.SetFloat("_Translucency", FoliageTranslucency);
            foreach (var (property, scale) in new[] { ("_WindStrength", FoliageWindStrength), ("_WindStrength1", FoliageWindStrength), ("_WindMovement", FoliageWindMovement), ("_WindMovement1", FoliageWindMovement) })
            {
                if (copy.HasProperty(property))
                {
                    copy.SetFloat(property, source.GetFloat(property) * scale);
                }
            }

            EditorUtility.SetDirty(copy);
        }

        /// <summary>
        /// URP's screen-space ambient occlusion (reference plan R3 step 6): every corner, sill and doorway grounded, fading
        /// out by 80 m where the haze carries the depth.
        /// </summary>
        private static void Occlusion(UniversalRendererData data)
        {
            var ssao = data.rendererFeatures.OfType<ScreenSpaceAmbientOcclusion>().FirstOrDefault();
            if (ssao == null)
            {
                ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ssao.name = "ScreenSpaceAmbientOcclusion";
                AssetDatabase.AddObjectToAsset(ssao, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ssao, out _, out long id);
                var so = new SerializedObject(data);
                var features = so.FindProperty("m_RendererFeatures");
                var map = so.FindProperty("m_RendererFeatureMap");
                features.arraySize++;
                features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = ssao;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var settings = new SerializedObject(ssao);
            settings.FindProperty("m_Settings.Source").enumValueIndex = 1; // depth and normals
            settings.FindProperty("m_Settings.Intensity").floatValue = 1.4f;
            settings.FindProperty("m_Settings.Radius").floatValue = 0.35f;
            settings.FindProperty("m_Settings.DirectLightingStrength").floatValue = 0.3f;
            settings.FindProperty("m_Settings.Falloff").floatValue = 80f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            ssao.SetActive(true);
            data.SetDirty();
            EditorUtility.SetDirty(data);
        }
    }
}
