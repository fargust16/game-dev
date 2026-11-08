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

                var mean = LinearMean(layer.diffuseTexture);
                float Scale(float target, float m) => Mathf.Clamp(Mathf.GammaToLinearSpace(target) / Mathf.Max(m, 0.01f), 0f, 1.5f);
                layer.diffuseRemapMax = new Vector4(Scale(look.r, mean.r), Scale(look.g, mean.g), Scale(look.b, mean.b), 1f);
                EditorUtility.SetDirty(layer);
                Debug.Log($"[VeilmouthLook] {name}: texture linear mean {mean}, tint {layer.diffuseRemapMax}.");
            }
        }

        /// <summary>A texture's mean colour in linear space, read from its small mip through a float target.</summary>
        private static Color LinearMean(Texture texture)
        {
            const int size = 32;
            var target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, target);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var read = new Texture2D(size, size, TextureFormat.RGBAFloat, false, true);
            read.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            read.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            var pixels = read.GetPixels();
            Object.DestroyImmediate(read);
            var sum = pixels.Aggregate(Vector4.zero, (s, p) => s + (Vector4)p);
            return sum / pixels.Length;
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
            Occlusion(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath));
            AssetDatabase.SaveAssets();
            Debug.Log($"[VeilmouthLook] colour space {PlayerSettings.colorSpace}, grading {pipeline.colorGradingMode}, occlusion on {RendererPath}.");
        }

        /// <summary>
        /// The foliage copies, swapped for the pack's materials in every kit wrapper that wears them, in place, so the
        /// scene's instances keep their links. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthLook.RetintTrees
        /// </summary>
        [MenuItem("GameDev/Slice/Reference Look/Retint Trees")]
        public static void RetintTrees()
        {
            Directory.CreateDirectory(Folder);
            foreach (var leaf in Foliage)
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
        public static int Retint(GameObject root)
        {
            var swaps = Foliage
                .Select(f => (From: AssetDatabase.LoadAssetAtPath<Material>(Polytope + f.Vendor + ".mat"), To: AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/{f.Copy}.mat")))
                .Where(s => s.From != null && s.To != null)
                .ToDictionary(s => s.From, s => s.To);
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
