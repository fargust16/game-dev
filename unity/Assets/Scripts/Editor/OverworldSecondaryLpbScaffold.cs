#if UNITY_EDITOR
using GameDev.World;
using Pinwheel.Vista;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Secondary Local Procedural Biome scaffold (visual terrain only).
    /// Order bands: Geology 0–19 → Hydrology 20–29 → Ecology 30–39 → Civilization 40–49.
    /// Automated Generate still runs IslandBase + Mountain LPBs only until secondary graphs exist.
    /// </summary>
    public static class OverworldSecondaryLpbScaffold
    {
        public const int OrderMountains = 10;
        public const int OrderBeach = 5;
        public const int OrderHills = 15;
        public const int OrderLakes = 20;
        public const int OrderRivers = 25;
        public const int OrderForest = 30;
        public const int OrderMeadow = 32;
        public const int OrderSwamp = 34;
        public const int OrderFields = 36;
        public const int OrderCityGround = 40;

        [MenuItem("GameDev/Overworld/Vista/Spawn Secondary LPB Scaffold")]
        public static void SpawnSecondaryLpbScaffoldMenu()
        {
            var manager = Object.FindAnyObjectByType<VistaManager>();
            if (manager == null)
            {
                Debug.LogError("[SecondaryLPB] No VistaManager — Generate terrain first.");
                return;
            }

            // Hydrology
            EnsurePlaceholder(manager, "LPB_Lakes", OrderLakes,
                BiomeBlendOptions.HeightBlendMode.Lower,
                height: true);
            EnsurePlaceholder(manager, "LPB_Rivers", OrderRivers,
                BiomeBlendOptions.HeightBlendMode.Lower,
                height: true);

            // Ecology (surface — no height by default)
            EnsurePlaceholder(manager, "LPB_Forest", OrderForest,
                BiomeBlendOptions.HeightBlendMode.Replace,
                height: false);
            EnsurePlaceholder(manager, "LPB_Meadow", OrderMeadow,
                BiomeBlendOptions.HeightBlendMode.Replace,
                height: false);
            EnsurePlaceholder(manager, "LPB_Swamp", OrderSwamp,
                BiomeBlendOptions.HeightBlendMode.Lower,
                height: true);
            EnsurePlaceholder(manager, "LPB_Fields", OrderFields,
                BiomeBlendOptions.HeightBlendMode.Replace,
                height: false);

            // Civilization
            EnsurePlaceholder(manager, "LPB_CityGround", OrderCityGround,
                BiomeBlendOptions.HeightBlendMode.Replace,
                height: false);

            Debug.Log(
                "[SecondaryLPB] Scaffold ready under VistaManager " +
                "(Hydro 20/25, Eco 30–36, Civ 40). Mountains stay 10–19 from bake. " +
                "Assign TerrainGraphs when authored; Generate uses IslandBase + Mountains only.");
        }

        private static void EnsurePlaceholder(
            VistaManager manager,
            string name,
            int order,
            BiomeBlendOptions.HeightBlendMode heightBlend,
            bool height)
        {
            LocalProceduralBiome biome = null;
            var existing = manager.transform.Find(name);
            if (existing != null)
                biome = existing.GetComponent<LocalProceduralBiome>();

            if (biome == null)
            {
                biome = LocalProceduralBiome.CreateInstanceInScene(manager);
                biome.gameObject.name = name;
            }

            biome.order = order;
            biome.space = Space.Self;
            biome.enabled = false;
            biome.dataMask = height
                ? BiomeDataMask.HeightMap | BiomeDataMask.LayerWeightMaps
                : BiomeDataMask.LayerWeightMaps;
            biome.falloffDirection = FalloffDirection.Outer;
            biome.transform.localPosition = new Vector3(
                WorldScale.WorldExtentXMeters * 0.5f,
                0f,
                WorldScale.WorldExtentZMeters * 0.5f);

            var opts = biome.blendOptions;
            opts.heightMapBlendMode = heightBlend;
            opts.textureBlendMode = BiomeBlendOptions.TextureBlendMode.Replace;
            opts.useTransformForHeightBlend = true;
            biome.blendOptions = opts;
            EditorUtility.SetDirty(biome);
        }
    }
}
#endif
