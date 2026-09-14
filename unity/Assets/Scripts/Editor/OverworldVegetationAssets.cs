#if UNITY_EDITOR
using System.Collections.Generic;
using GameDev.World.Terrain;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.EditorTools
{
    /// <summary>
    /// The overworld's tree and ground-cover prototypes, as Unity <c>TreePrototype</c> and
    /// <c>DetailPrototype</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Content only — which prefab stands for which species and how the grass mesh renders. Where
    /// anything grows is <see cref="TerrainVegetationRules"/>; the placement is
    /// <see cref="OverworldTerrainBuilder"/>.
    /// </para>
    /// <para>
    /// Canopy uses the Polytope Lowpoly prefabs, one per species. Colour variants are separate
    /// species rather than prefab variants of one, because a Unity tree instance names exactly one
    /// prototype. They are tinted copies whose materials shift <c>_TopColor</c> and <c>_GroundColor</c>
    /// in HSV — small shifts, so a grove reads as one ecosystem with individual variation.
    /// </para>
    /// <para>
    /// Ground cover is the Polytope <c>PT_Grass_02</c> clump rebuilt as a single-mesh prefab, because a
    /// Unity detail prototype needs a mesh on the prefab root and GPU instancing on its material.
    /// Every generated asset lives under <see cref="Folder"/> and is recreated when missing.
    /// </para>
    /// </remarks>
    public static class OverworldVegetationAssets
    {
        public const string Folder = "Assets/Settings/Terrain/Vegetation";
        private const string TintFolder = Folder + "/Tints";
        private const string GrassPrefabPath = Folder + "/Overworld_GrassDetail.prefab";
        private const string GrassMaterialPath = Folder + "/Overworld_GrassDetail.mat";

        private const string PolytopeRoot = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs";
        private const string GrassSourcePath = PolytopeRoot + "/Plants/PT_Grass_02.prefab";

        /// <summary>
        /// Grass detail size, as a multiple of the source mesh. The Polytope clump is already authored
        /// at ground-cover size, so the band stays around one; <c>DetailPrototype</c> sizes scale the
        /// mesh and are not metres.
        /// </summary>
        private const float GrassScaleMin = 0.7f;

        private const float GrassScaleMax = 1.3f;

        private static readonly int TopColor = Shader.PropertyToID("_TopColor");
        private static readonly int GroundColor = Shader.PropertyToID("_GroundColor");

        /// <summary>
        /// Tree prototypes, and the prototype index of every <see cref="TerrainTreeSpecies"/> (−1 when a
        /// species' prefab is missing).
        /// </summary>
        public static TreePrototype[] TreePrototypes(out int[] speciesPrototype)
        {
            var species = (TerrainTreeSpecies[])System.Enum.GetValues(typeof(TerrainTreeSpecies));
            speciesPrototype = new int[species.Length];
            var prototypes = new List<TreePrototype>();

            foreach (var s in species)
            {
                var prefab = SpeciesPrefab(s);
                if (prefab == null)
                {
                    speciesPrototype[(int)s] = -1;
                    continue;
                }

                speciesPrototype[(int)s] = prototypes.Count;
                prototypes.Add(new TreePrototype { prefab = prefab, bendFactor = 0f });
            }

            return prototypes.ToArray();
        }

        /// <summary>Detail prototypes, and the index of the meadow grass (−1 when unavailable).</summary>
        public static DetailPrototype[] DetailPrototypes(out int grassPrototype)
        {
            var grass = LoadOrCreateGrassPrefab();
            if (grass == null)
            {
                grassPrototype = -1;
                return System.Array.Empty<DetailPrototype>();
            }

            grassPrototype = 0;
            return new[]
            {
                new DetailPrototype
                {
                    prototype = grass,
                    usePrototypeMesh = true,
                    renderMode = DetailRenderMode.VertexLit,
                    useInstancing = CanInstance(grass),
                    minWidth = GrassScaleMin,
                    maxWidth = GrassScaleMax,
                    minHeight = GrassScaleMin,
                    maxHeight = GrassScaleMax,
                    healthyColor = Color.white,
                    dryColor = Color.white,
                },
            };
        }

        [MenuItem("GameDev/Overworld/Vegetation/Rebuild Vegetation Assets")]
        public static void RebuildMenu()
        {
            if (AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.DeleteAsset(Folder);
            }

            TreePrototypes(out var species);
            DetailPrototypes(out var grass);
            Debug.Log(
                $"[Vegetation] Rebuilt: {species.Length} species ({System.Array.FindAll(species, i => i >= 0).Length} available), " +
                $"grass prototype {(grass >= 0 ? "ready" : "missing")}.");
        }

        // ------------------------------------------------------------------------------------------

        private static GameObject SpeciesPrefab(TerrainTreeSpecies species)
        {
            string path;
            Tint? tint = null;
            switch (species)
            {
                case TerrainTreeSpecies.Pine:
                    path = PolytopeRoot + "/Trees/PT_Pine_Tree_03_green.prefab";
                    break;
                case TerrainTreeSpecies.PinePale:
                    path = PolytopeRoot + "/Trees/PT_Pine_Tree_03_green.prefab";
                    tint = new Tint("Pale", -0.03f, 0.86f, 1.22f);
                    break;
                case TerrainTreeSpecies.Fruit:
                    path = PolytopeRoot + "/Trees/PT_Fruit_Tree_01_green.prefab";
                    break;
                case TerrainTreeSpecies.FruitDeep:
                    path = PolytopeRoot + "/Trees/PT_Fruit_Tree_01_green.prefab";
                    tint = new Tint("Deep", 0.02f, 1.14f, 0.78f);
                    break;
                default:
                    path = PolytopeRoot + "/Trees/PT_Fruit_Tree_01_apples.prefab";
                    break;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[Vegetation] No prefab for species '{species}' at '{path}'.");
                return null;
            }

            if (prefab.GetComponent<LODGroup>() == null && prefab.GetComponent<MeshRenderer>() == null)
            {
                Debug.LogError(
                    $"[Vegetation] '{path}' cannot be a terrain tree: Unity needs an LOD Group or a Mesh " +
                    "Renderer on the prefab root.");
                return null;
            }

            return tint.HasValue ? LoadOrCreateTintedVariant(prefab, tint.Value) ?? prefab : prefab;
        }

        private readonly struct Tint
        {
            public readonly string Suffix;
            public readonly float HueShift;
            public readonly float SaturationScale;
            public readonly float ValueScale;

            public Tint(string suffix, float hueShift, float saturationScale, float valueScale)
            {
                Suffix = suffix;
                HueShift = hueShift;
                SaturationScale = saturationScale;
                ValueScale = valueScale;
            }
        }

        private static GameObject LoadOrCreateTintedVariant(GameObject source, Tint tint)
        {
            var path = $"{TintFolder}/{source.name}_{tint.Suffix}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(TintFolder);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var materials = renderer.sharedMaterials;
                    var changed = false;
                    for (var i = 0; i < materials.Length; i++)
                    {
                        var tinted = LoadOrCreateTintedMaterial(materials[i], tint);
                        if (tinted != null)
                        {
                            materials[i] = tinted;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        renderer.sharedMaterials = materials;
                    }
                }

                return PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static Material LoadOrCreateTintedMaterial(Material source, Tint tint)
        {
            if (source == null || !source.HasProperty(TopColor) || !source.HasProperty(GroundColor))
            {
                return null;
            }

            var path = $"{TintFolder}/{source.name}_{tint.Suffix}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(TintFolder);
            var copy = new Material(source) { name = $"{source.name}_{tint.Suffix}" };
            copy.SetColor(TopColor, Shift(source.GetColor(TopColor), tint));
            copy.SetColor(GroundColor, Shift(source.GetColor(GroundColor), tint));
            AssetDatabase.CreateAsset(copy, path);
            return copy;
        }

        private static Color Shift(Color color, Tint tint)
        {
            Color.RGBToHSV(color, out var h, out var s, out var v);
            h = Mathf.Repeat(h + tint.HueShift, 1f);
            s = Mathf.Clamp01(s * tint.SaturationScale);
            v = Mathf.Clamp01(v * tint.ValueScale);
            var shifted = Color.HSVToRGB(h, s, v);
            shifted.a = color.a;
            return shifted;
        }

        private static GameObject LoadOrCreateGrassPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(GrassPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(GrassSourcePath);
            if (source == null)
            {
                Debug.LogError($"[Vegetation] Missing grass source prefab at '{GrassSourcePath}'.");
                return null;
            }

            var filter = FindHighestDetailMesh(source);
            if (filter == null || filter.sharedMesh == null)
            {
                Debug.LogError($"[Vegetation] '{GrassSourcePath}' has no mesh to build a detail from.");
                return null;
            }

            var sourceMaterial = filter.GetComponent<MeshRenderer>() != null
                ? filter.GetComponent<MeshRenderer>().sharedMaterial
                : null;
            if (sourceMaterial == null)
            {
                Debug.LogError("[Vegetation] The grass source prefab has no material to copy.");
                return null;
            }

            EnsureFolder(Folder);
            var material = AssetDatabase.LoadAssetAtPath<Material>(GrassMaterialPath);
            if (material == null)
            {
                material = new Material(sourceMaterial) { name = "Overworld_GrassDetail", enableInstancing = true };
                AssetDatabase.CreateAsset(material, GrassMaterialPath);
            }

            var go = new GameObject("Overworld_GrassDetail");
            try
            {
                go.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = material;
                var saved = PrefabUtility.SaveAsPrefabAsset(go, GrassPrefabPath);
                AssetDatabase.SaveAssets();
                return saved;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static MeshFilter FindHighestDetailMesh(GameObject source)
        {
            var lodGroup = source.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                var lods = lodGroup.GetLODs();
                if (lods.Length > 0 && lods[0].renderers != null)
                {
                    foreach (var renderer in lods[0].renderers)
                    {
                        var filter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
                        if (filter != null && filter.sharedMesh != null)
                        {
                            return filter;
                        }
                    }
                }
            }

            return source.GetComponentInChildren<MeshFilter>();
        }

        private static bool CanInstance(GameObject prefab)
        {
            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || !material.enableInstancing)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
#endif
