#if UNITY_EDITOR
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Authors the two overworld biome template assets (plateau and mountain massif) once, so the
    /// bake has native Vista templates to spawn instead of stamping every field from code.
    /// </summary>
    /// <remarks>
    /// Everything a designer tunes lives on the prefab and is edited in the Inspector: the terrain
    /// graph, the biome mask graph, blend options, resolution, falloff, and the two spawn
    /// components — <see cref="SetBiomeHeightOnSpawn"/> for world height and
    /// <see cref="SetBiomeAreaOnSpawn"/> for world extent. The bake never writes those fields back.
    /// <para>
    /// Creation is deliberately non-destructive: an existing prefab is returned as-is, so a rebuild
    /// of the world does not undo Inspector edits. Use the destructive menu item when the template
    /// itself has to be regenerated.
    /// </para>
    /// </remarks>
    public static class OverworldBiomeTemplates
    {
        private const string TemplatesFolder = "Assets/Settings/Vista/BiomeTemplates";

        public const string SeaFloorTemplatePath = TemplatesFolder + "/SeaFloor_BiomeTemplate.asset";
        public const string SeaFloorPrefabPath = TemplatesFolder + "/LPB_SeaFloor.prefab";
        public const string IslandTemplatePath = TemplatesFolder + "/IslandBase_BiomeTemplate.asset";
        public const string IslandPrefabPath = TemplatesFolder + "/LPB_IslandBase.prefab";
        public const string MountainTemplatePath = TemplatesFolder + "/Mountain_BiomeTemplate.asset";
        public const string MountainPrefabPath = TemplatesFolder + "/LPB_Mountain.prefab";

        /// <summary>
        /// The relief tiers from <c>Assets/Plans/WORLD_RELIEF_PRINCIPLES.md</c>, one prefab each.
        /// </summary>
        /// <remarks>
        /// Separate prefabs rather than one prefab with a height the bake overwrites, for two
        /// reasons that happen to agree. The engine one: <c>OverworldLpbBake</c> may write only
        /// seed, XZ and the anchor silhouette - height, area and falloff belong to the prefab, so
        /// per-instance height has to come from per-instance templates. The design one: the relief
        /// rules call for a small fixed set of archetypes with the seed choosing which goes where,
        /// not a continuous random height range, because that is what keeps the chaos controlled
        /// and stops the island becoming a staircase.
        /// </remarks>
        public enum ShelfArchetype
        {
            /// <summary>T3 outcrop: a rock you walk around, not up.</summary>
            Outcrop,

            /// <summary>T2 shelf: the common step, big enough to stand a camp on.</summary>
            Shelf,

            /// <summary>T2 plateau: the large one, a district in its own right.</summary>
            Plateau,
        }

        /// <summary>Height/area/order seeds written when a template prefab is first created.</summary>
        /// <remarks>
        /// These are the values a fresh prefab starts at, not the values the world runs at. After
        /// creation the Inspector is the authority and nothing in the bake reads these again.
        /// </remarks>
        /// <remarks>
        /// The island graph now spans the full 0..1 range, so this is the plateau's real top in
        /// metres above sea level and the low ground below <c>WorldScale.SeaLevelMeters</c>
        /// becomes coast on its own. That is what lets the falloff be a rim treatment instead of the
        /// only thing shaping the shore.
        /// </remarks>
        private const float IslandInitialWorldHeightMeters = 140f;

        /// <remarks>
        /// The practical ceiling for a 2800 m world: <c>2800 - 2 x IslandOceanMarginMeters</c>. The
        /// measured dry land at 1880 m was 1.74 km2, which fits two large towns uncomfortably close
        /// together; this takes it to roughly 2.5-3 km2.
        /// </remarks>
        private const float IslandInitialWorldExtentMeters = 2240f;
        private const float IslandInitialFalloffMeters = 180f;

        /// <summary>
        /// The seabed spans the whole world and stays entirely under <c>SeaLevelMeters</c> (24 m).
        /// </summary>
        private const float SeaFloorInitialWorldHeightMeters = 20f;

        /// <summary>Height, footprint and falloff per shelf archetype, in metres.</summary>
        /// <remarks>
        /// The falloff is the cliff. A shelf's canvas covers <c>area + 2 x falloff</c> at 512 px, so
        /// the Outcrop resolves at 126 m / 512 = 0.25 m/px and the Plateau at 476 m / 512 =
        /// 0.93 m/px. The graph then squeezes the height transition into roughly a third of the
        /// falloff width, which puts the Plateau's 32 m drop across ~16 m of ground - 63 degrees.
        /// Widening the falloff softens the cliff; narrowing it past the canvas cell aliases the
        /// rim into a staircase.
        /// <para>
        /// Heights keep the tiers 2.5x apart: 9 m against 20 m and 32 m. Nothing sits between them,
        /// which is the rule that stops the shapes reading as one muddy scale.
        /// </para>
        /// </remarks>
        private static (float height, float extent, float falloff) ShelfSpec(ShelfArchetype a)
        {
            switch (a)
            {
                case ShelfArchetype.Outcrop: return (9f, 90f, 18f);
                case ShelfArchetype.Plateau: return (32f, 380f, 48f);
                default: return (20f, 220f, 34f);
            }
        }

        /// <summary>Half the authored footprint of a shelf archetype, in metres.</summary>
        /// <remarks>
        /// Read from the same table the prefab is built from, so placement cannot drift away from
        /// the geometry. Asking a spawned biome instead means spawning it before knowing whether
        /// there is room for it, and un-spawning a Vista biome is not a thing you want to do: its
        /// root is shared with the manager.
        /// </remarks>
        public static float ShelfHalfExtent(ShelfArchetype archetype) =>
            ShelfSpec(archetype).extent * 0.5f;

        private const float MountainInitialWorldHeightMeters = 530f;
        private const float MountainInitialMajorAxisMeters = 1050f;
        private const float MountainInitialMinorAxisMeters = 450f;
        private const float MountainInitialFalloffMeters = 520f;

        private const int InitialBaseResolution = 512;

        /// <summary>The seabed is the bottom of the stack; everything else blends down to it.</summary>
        public const int OrderSeaFloor = 0;

        /// <summary>Geology → hydrology → ecology order band: the plateau sits on the seabed.</summary>
        public const int OrderIslandBase = 10;

        /// <summary>Shelves raise the island floor; the massif still raises over them.</summary>
        public const int OrderShelf = 15;

        /// <summary>Massifs raise the plateau, so they sit above it in the blend order.</summary>
        public const int OrderMountainBase = 20;

        /// <summary>
        /// One-shot bootstrap for a fresh checkout: author the graphs, then the biome and vegetation
        /// templates that reference them.
        /// </summary>
        /// <remarks>
        /// Only needed once. After this the assets are the designer's, and the bake never re-authors
        /// them — which is why the graph rebuilds below are the destructive path and are only run
        /// here when the asset is genuinely missing.
        /// </remarks>
        [MenuItem("GameDev/Overworld/Vista/First-Time Setup (graphs + templates)", false, -100)]
        public static void FirstTimeSetupMenu()
        {
            OverworldTerrainLayers.EnsureCanonicalLayers();

            if (AssetDatabase.LoadAssetAtPath<TerrainGraph>(
                    SeaFloorGraphBuilder.GraphAssetPath) == null)
            {
                SeaFloorGraphBuilder.Rebuild();
            }

            if (AssetDatabase.LoadAssetAtPath<TerrainGraph>(
                    IslandPlateauGraphBuilder.GraphAssetPath) == null)
            {
                IslandPlateauGraphBuilder.Rebuild();
            }

            if (AssetDatabase.LoadAssetAtPath<TerrainGraph>(
                    MountainMassifGraphBuilder.GraphAssetPath) == null)
            {
                MountainMassifGraphBuilder.Rebuild();
            }

            if (AssetDatabase.LoadAssetAtPath<TerrainGraph>(
                    ShelfPlateauGraphBuilder.GraphAssetPath) == null)
            {
                ShelfPlateauGraphBuilder.Rebuild();
            }

            if (AssetDatabase.LoadAssetAtPath<BiomeMaskGraph>(
                    SoftFalloffMaskGraphBuilder.GraphAssetPath) == null)
            {
                SoftFalloffMaskGraphBuilder.Rebuild();
            }

            if (AssetDatabase.LoadAssetAtPath<BiomeMaskGraph>(
                    SoftFalloffMaskGraphBuilder.ShelfGraphAssetPath) == null)
            {
                SoftFalloffMaskGraphBuilder.RebuildShelf();
            }

            VistaVegetationTemplates.CreateIfMissingMenu();
            CreateIfMissingMenu();
            Debug.Log("[Overworld] First-time Vista setup complete. Run GameDev/Overworld/Generate All.");
        }

        [MenuItem("GameDev/Overworld/Vista/Create Biome Templates (if missing)")]
        public static void CreateIfMissingMenu()
        {
            var seaFloor = EnsureSeaFloorTemplate();
            var island = EnsureIslandTemplate();
            var mountain = EnsureMountainTemplate();
            var shelves = 0;
            foreach (ShelfArchetype archetype in System.Enum.GetValues(typeof(ShelfArchetype)))
            {
                if (EnsureShelfTemplate(archetype) != null)
                {
                    shelves++;
                }
            }

            Debug.Log($"[Overworld] {shelves} shelf archetype template(s) ready.");
            Debug.Log(
                $"[Overworld] Biome templates ready: seaFloor={(seaFloor != null ? seaFloor.name : "FAILED")} " +
                $"island={(island != null ? island.name : "FAILED")} " +
                $"mountain={(mountain != null ? mountain.name : "FAILED")}. " +
                "Tune height/area/graph on the prefabs — the bake does not overwrite them.");
        }

        [MenuItem("GameDev/Overworld/Vista/Recreate Biome Templates (destructive)")]
        public static void RecreateMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Recreate overworld biome templates?",
                    "This deletes LPB_IslandBase and LPB_Mountain and rebuilds them from code defaults. " +
                    "Every Inspector edit on those prefabs — biome height, area, blend options, graph " +
                    "assignment — is lost.",
                    "Recreate",
                    "Cancel"))
            {
                return;
            }

            AssetDatabase.DeleteAsset(SeaFloorPrefabPath);
            AssetDatabase.DeleteAsset(SeaFloorTemplatePath);
            AssetDatabase.DeleteAsset(IslandPrefabPath);
            AssetDatabase.DeleteAsset(IslandTemplatePath);
            AssetDatabase.DeleteAsset(MountainPrefabPath);
            AssetDatabase.DeleteAsset(MountainTemplatePath);
            CreateIfMissingMenu();
        }

        /// <summary>Loads the seabed template, creating it from defaults only when absent.</summary>
        public static BiomeTemplate EnsureSeaFloorTemplate()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BiomeTemplate>(SeaFloorTemplatePath);
            if (existing != null && existing.IsValid(out _))
            {
                RepairGraphReferences(existing, SeaFloorGraphBuilder.Load());
                return existing;
            }

            var graph = SeaFloorGraphBuilder.Load();
            if (graph == null)
            {
                Debug.LogError(
                    "[Overworld] Cannot create the sea floor template: SeaFloor_TerrainGraph is missing. " +
                    "Run GameDev/Overworld/Vista/Rebuild Sea Floor Graph (destructive) first.");
                return null;
            }

            var go = new GameObject("LPB_SeaFloor");
            try
            {
                var biome = go.AddComponent<LocalProceduralBiome>();
                biome.space = Space.Self;
                biome.order = OrderSeaFloor;
                biome.baseResolution = InitialBaseResolution;
                biome.terrainGraph = graph;
                // No mask graph and no falloff: the seabed has no edge to soften, it is the floor.
                biome.falloffDirection = FalloffDirection.Inner;
                biome.falloffDistance = 0f;
                // Height and layer weights only. Asking for tree/detail channels would make Vista
                // pull vegetation outputs this graph does not have, for every tile in the world.
                biome.dataMask = BiomeDataMask.HeightMap |
                                 BiomeDataMask.HoleMap |
                                 BiomeDataMask.LayerWeightMaps;

                var options = biome.blendOptions;
                options.heightMapBlendMode = BiomeBlendOptions.HeightBlendMode.Replace;
                options.textureBlendMode = BiomeBlendOptions.TextureBlendMode.Replace;
                biome.blendOptions = options;

                go.AddComponent<SetBiomeHeightOnSpawn>().worldHeight = SeaFloorInitialWorldHeightMeters;
                // The native way to cover every tile, whatever the world size is.
                go.AddComponent<SnapBiomeToTilesOnSpawn>();

                return SavePrefabAndTemplate(go, SeaFloorPrefabPath, SeaFloorTemplatePath);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>Loads the plateau template, creating it from defaults only when absent.</summary>
        public static BiomeTemplate EnsureIslandTemplate()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BiomeTemplate>(IslandTemplatePath);
            if (existing != null && existing.IsValid(out _))
            {
                RepairGraphReferences(existing, IslandPlateauGraphBuilder.Load());
                return existing;
            }

            var graph = IslandPlateauGraphBuilder.Load();
            if (graph == null)
            {
                Debug.LogError(
                    "[Overworld] Cannot create the island template: IslandPlateau_TerrainGraph is missing. " +
                    "Run GameDev/Overworld/Vista/Rebuild Island Plateau Graph (destructive) first.");
                return null;
            }

            var go = new GameObject("LPB_IslandBase");
            try
            {
                var biome = go.AddComponent<LocalProceduralBiome>();
                biome.space = Space.Self;
                biome.order = OrderIslandBase;
                biome.baseResolution = InitialBaseResolution;
                biome.terrainGraph = graph;
                biome.biomeMaskGraph = SoftFalloffMaskGraphBuilder.Load();
                biome.falloffDirection = FalloffDirection.Outer;
                biome.falloffDistance = IslandInitialFalloffMeters;
                biome.dataMask = FullDataMask();

                var options = biome.blendOptions;
                // The plateau is the base surface: it defines both height and texture underneath.
                options.heightMapBlendMode = BiomeBlendOptions.HeightBlendMode.Replace;
                options.textureBlendMode = BiomeBlendOptions.TextureBlendMode.Replace;
                biome.blendOptions = options;

                go.AddComponent<SetBiomeHeightOnSpawn>().worldHeight = IslandInitialWorldHeightMeters;
                go.AddComponent<SetBiomeAreaOnSpawn>().worldArea = new Vector2(
                    IslandInitialWorldExtentMeters, IslandInitialWorldExtentMeters);

                // Lift the whole height band to the waterline. Vista's height blend reads
                // transform.position.y / terrainMaxHeight as an offset whenever
                // useTransformForHeightBlend is on, which SetBiomeHeightOnSpawn turns on. Without
                // this the island's own low ground sits at 0.05 x 140 = 7 m, well under the 24 m
                // sea, so most of the authored polygon was drowned and trees were spawning
                // underwater. Y is the Inspector knob for "how high the island rides".
                go.transform.position = new Vector3(0f, GameDev.World.WorldScale.SeaLevelMeters, 0f);

                return SavePrefabAndTemplate(go, IslandPrefabPath, IslandTemplatePath);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>Loads the massif template, creating it from defaults only when absent.</summary>
        public static BiomeTemplate EnsureMountainTemplate()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BiomeTemplate>(MountainTemplatePath);
            if (existing != null && existing.IsValid(out _))
            {
                RepairGraphReferences(existing, MountainMassifGraphBuilder.Load());
                return existing;
            }

            var graph = MountainMassifGraphBuilder.Load();
            if (graph == null)
            {
                Debug.LogError(
                    "[Overworld] Cannot create the mountain template: MountainMassif_TerrainGraph is missing. " +
                    "Run GameDev/Overworld/Vista/Rebuild Mountain Massif Graph (destructive) first.");
                return null;
            }

            var go = new GameObject("LPB_Mountain");
            try
            {
                var biome = go.AddComponent<LocalProceduralBiome>();
                biome.space = Space.Self;
                biome.order = OrderMountainBase;
                biome.baseResolution = InitialBaseResolution;
                biome.terrainGraph = graph;
                biome.biomeMaskGraph = SoftFalloffMaskGraphBuilder.Load();
                biome.falloffDirection = FalloffDirection.Outer;
                biome.falloffDistance = MountainInitialFalloffMeters;
                biome.dataMask = FullDataMask();

                var options = biome.blendOptions;
                // Raise: the massif adds relief onto whatever the plateau already put there, so the
                // graph's periphery fades into foothills instead of cutting a step.
                options.heightMapBlendMode = BiomeBlendOptions.HeightBlendMode.Raise;
                options.textureBlendMode = BiomeBlendOptions.TextureBlendMode.Replace;
                biome.blendOptions = options;

                go.AddComponent<SetBiomeHeightOnSpawn>().worldHeight = MountainInitialWorldHeightMeters;
                go.AddComponent<SetBiomeAreaOnSpawn>().worldArea = new Vector2(
                    MountainInitialMajorAxisMeters, MountainInitialMinorAxisMeters);

                return SavePrefabAndTemplate(go, MountainPrefabPath, MountainTemplatePath);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        public static string ShelfTemplatePath(ShelfArchetype a) =>
            $"{TemplatesFolder}/Shelf{a}_BiomeTemplate.asset";

        public static string ShelfPrefabPath(ShelfArchetype a) =>
            $"{TemplatesFolder}/LPB_Shelf{a}.prefab";

        /// <summary>Loads one shelf archetype's template, creating it from the table when absent.</summary>
        public static BiomeTemplate EnsureShelfTemplate(ShelfArchetype archetype)
        {
            var templatePath = ShelfTemplatePath(archetype);
            var existing = AssetDatabase.LoadAssetAtPath<BiomeTemplate>(templatePath);
            if (existing != null && existing.IsValid(out _))
            {
                RepairGraphReferences(existing, ShelfPlateauGraphBuilder.Load());
                return existing;
            }

            var graph = ShelfPlateauGraphBuilder.Load();
            if (graph == null)
            {
                Debug.LogError(
                    "[Overworld] Cannot create a shelf template: ShelfPlateau_TerrainGraph is "
                    + "missing. Run GameDev/Overworld/Vista/Rebuild Shelf Plateau Graph "
                    + "(destructive) first.");
                return null;
            }

            var spec = ShelfSpec(archetype);
            var go = new GameObject($"LPB_Shelf{archetype}");
            try
            {
                var biome = go.AddComponent<LocalProceduralBiome>();
                biome.space = Space.Self;
                biome.order = OrderShelf;
                biome.baseResolution = InitialBaseResolution;
                biome.terrainGraph = graph;
                biome.biomeMaskGraph = SoftFalloffMaskGraphBuilder.LoadShelf();
                biome.falloffDirection = FalloffDirection.Outer;
                biome.falloffDistance = spec.falloff;
                biome.dataMask = FullDataMask();

                var options = biome.blendOptions;
                // Raise, like the massif: a shelf adds a step onto whatever the island already put
                // there. Replace would cut a hole in the meadow wherever the shelf sits lower than
                // the ground it lands on, which on a domed island is most of the coast side.
                options.heightMapBlendMode = BiomeBlendOptions.HeightBlendMode.Raise;
                options.textureBlendMode = BiomeBlendOptions.TextureBlendMode.Replace;
                biome.blendOptions = options;

                go.AddComponent<SetBiomeHeightOnSpawn>().worldHeight = spec.height;
                go.AddComponent<SetBiomeAreaOnSpawn>().worldArea =
                    new Vector2(spec.extent, spec.extent);

                return SavePrefabAndTemplate(go, ShelfPrefabPath(archetype), templatePath);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Re-attaches a template prefab's graph references when they have gone missing.
        /// </summary>
        /// <remarks>
        /// A null reference is a broken state, not an authoring choice, so healing it does not
        /// violate the rule that the prefab owns its settings — everything else on the prefab is left
        /// exactly as the designer set it. References go missing when an asset is replaced rather
        /// than overwritten; <see cref="VistaGraphBuild.SaveGraph"/> is what prevents that happening
        /// in the first place.
        /// </remarks>
        private static void RepairGraphReferences(BiomeTemplate template, TerrainGraph terrainGraph)
        {
            if (template == null || template.biomePrefab == null)
            {
                return;
            }

            var biome = template.biomePrefab.GetComponentInChildren<LocalProceduralBiome>(true);
            if (biome == null)
            {
                return;
            }

            var repaired = false;
            if (biome.terrainGraph == null && terrainGraph != null)
            {
                biome.terrainGraph = terrainGraph;
                repaired = true;
            }

            // Only a biome with a soft edge is supposed to have the falloff-detail mask graph. The
            // seabed has falloff 0 on purpose — it is the world floor, not a shape with a rim — so
            // "no mask graph" there is a choice, not a broken reference.
            if (biome.biomeMaskGraph == null && biome.falloffDistance > 0f)
            {
                var maskGraph = SoftFalloffMaskGraphBuilder.Load();
                if (maskGraph != null)
                {
                    biome.biomeMaskGraph = maskGraph;
                    repaired = true;
                }
            }

            if (!repaired)
            {
                return;
            }

            EditorUtility.SetDirty(template.biomePrefab);
            AssetDatabase.SaveAssets();
            Debug.LogWarning(
                $"[Overworld] '{template.name}' had a missing graph reference and was repaired. " +
                "That happens when a graph asset is replaced instead of overwritten.");
        }

        /// <summary>
        /// Every channel the graphs write. Vista only pulls the outputs the mask asks for, so a
        /// missing flag silently discards a whole stage of the graph.
        /// </summary>
        private static BiomeDataMask FullDataMask()
        {
            return BiomeDataMask.HeightMap |
                   BiomeDataMask.HoleMap |
                   BiomeDataMask.LayerWeightMaps |
                   BiomeDataMask.TreeInstances |
                   BiomeDataMask.DetailDensityMaps;
        }

        private static BiomeTemplate SavePrefabAndTemplate(
            GameObject source, string prefabPath, string templatePath)
        {
            EnsureFolders();
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[Overworld] Failed to save biome prefab at '{prefabPath}'.");
                return null;
            }

            var template = AssetDatabase.LoadAssetAtPath<BiomeTemplate>(templatePath);
            if (template == null)
            {
                template = ScriptableObject.CreateInstance<BiomeTemplate>();
                AssetDatabase.CreateAsset(template, templatePath);
            }

            // biomePrefab has no public setter — it is authored through the Inspector, so bind it
            // the same way the Inspector does.
            var so = new SerializedObject(template);
            so.FindProperty("m_biomePrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            if (!template.IsValid(out var error))
            {
                Debug.LogError($"[Overworld] Biome template '{templatePath}' is invalid: {error}");
                return null;
            }

            return template;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Settings/Vista"))
            {
                AssetDatabase.CreateFolder("Assets/Settings", "Vista");
            }

            if (!AssetDatabase.IsValidFolder(TemplatesFolder))
            {
                AssetDatabase.CreateFolder("Assets/Settings/Vista", "BiomeTemplates");
            }
        }
    }
}
#endif
