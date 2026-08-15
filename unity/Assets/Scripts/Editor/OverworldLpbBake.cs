#if UNITY_EDITOR
using System.Collections.Generic;
using GameDev.World;
using GameDev.World.Seed;
using GameDev.World.Terrain;
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;
using WorldSeed = GameDev.World.Seed.WorldSeed;
using Random = System.Random;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Multi-LPB overworld bake: island shelf + 1–3 Mountain massif LPBs (one LPB = one massif).
    /// Geology → Hydrology → Ecology → Civilization order bands (mountains = 10–19 Raise).
    /// </summary>
    public static class OverworldLpbBake
    {
        private const string BlankBiomeTemplatePath =
            "Assets/PinwheelStudio/Vista/Personal/BiomeTemplates/Blank/Blank_BiomeTemplate.asset";

        public const int IslandAnchorCount = 36;
        public const float IslandRadiusNoiseFrac = 0.04f;

        /// <summary>
        /// Outer coast ramp. Must be wide enough for ≤15° geology (~Δh/tan15°).
        /// Beach vs cliff comes from asymmetric solid anchors, not a shorter falloff.
        /// </summary>
        public const float IslandFalloffLocalMeters = 420f;

        /// <summary>
        /// Graph 1.0 = this ASL. Keep plains in the ~30–65 m band so sea→land
        /// can be a gentle ramp (not a 90 m mesa wall).
        /// </summary>
        public const float BasePlateauWorldHeightMeters = 55f;

        public const float BaseLocalPos = 500f;
        public const float BaseLocalScaleXz = 1.05f;

        public const int MountainAnchorCount = 28;

        /// <summary>
        /// Graph resolution per massif LPB. Also the ceiling on HydraulicErosion2's sim canvas
        /// (<c>min(2048, baseResolution)</c>), so lowering this coarsens drainage, not just detail.
        /// If a bake gets too slow, cut erosion/talus iterations in
        /// <see cref="MountainMassifGraphBuilder"/> before touching this.
        /// </summary>
        public const int MountainBaseResolution = 1024;

        /// <summary>Major axis of ridge ellipse (local metres).</summary>
        public const float MountainMajorAxisMin = 900f;
        public const float MountainMajorAxisMax = 1200f;

        /// <summary>Minor axis of ridge ellipse (local metres).</summary>
        public const float MountainMinorAxisMin = 350f;
        public const float MountainMinorAxisMax = 550f;

        /// <summary>Wide geological transition into IslandBase (not the mountain slope itself).</summary>
        public const float MountainFalloffLocalMeters = 520f;

        public const float MountainLpbScaleMin = 0.9f;
        public const float MountainLpbScaleMax = 1.15f;

        /// <summary>
        /// Raise delta (world m) added onto island shelf. Peak ASL ≈ island (~90) + ~0.85×delta.
        /// </summary>
        public const float MountainPeakDeltaMinMeters = 480f;
        public const float MountainPeakDeltaMaxMeters = 580f;

        public const int OrderIslandBase = 0;
        public const int OrderMountainBase = 10;

        public static void SpawnIslandAndMountainStack(VistaManager manager, int seed)
        {
            SpawnBaseIslandBlankLpb(manager, seed);
            SpawnSeededMountainLpbs(manager, seed);
        }

        private static void SpawnBaseIslandBlankLpb(VistaManager manager, int seed)
        {
            var template = AssetDatabase.LoadAssetAtPath<BiomeTemplate>(BlankBiomeTemplatePath);
            string templateError = null;
            if (template == null || !template.IsValid(out templateError))
            {
                Debug.LogError(
                    $"[IslandBase] Blank template missing/invalid at {BlankBiomeTemplatePath}: {templateError}");
                return;
            }

            var spawnContext = new BiomeTemplateSpawnContext(manager);
            var spawnedRoot = BiomeTemplateSpawner.Spawn(template, spawnContext);
            RenameOwnedObjects(spawnContext, "IslandBase");
            OverworldTerrainMenu.PersistSpawnedOwnedObjectsPublic(spawnedRoot, spawnContext);

            var biome = spawnedRoot.GetComponentInChildren<LocalProceduralBiome>(true);
            if (biome == null)
            {
                Debug.LogError("[IslandBase] Spawned Blank has no LocalProceduralBiome.");
                return;
            }

            biome.gameObject.name = "LPB_IslandBase";
            biome.space = Space.Self;
            biome.transform.localPosition = new Vector3(BaseLocalPos, 0f, BaseLocalPos);
            biome.transform.localRotation = Quaternion.identity;
            biome.transform.localScale = new Vector3(BaseLocalScaleXz, 1f, BaseLocalScaleXz);
            biome.baseResolution = 512;
            biome.order = OrderIslandBase;
            biome.dataMask = BiomeDataMask.HeightMap | BiomeDataMask.HoleMap | BiomeDataMask.LayerWeightMaps;

            var plateauGraph = IslandPlateauGraphBuilder.LoadOrRebuild();
            if (plateauGraph == null)
            {
                Debug.LogError("[IslandBase] Failed to build IslandPlateau_TerrainGraph.");
                return;
            }

            biome.terrainGraph = plateauGraph;
            biome.biomeMaskGraph = SoftFalloffMaskGraphBuilder.LoadOrRebuild();

            var heightOnSpawn = biome.GetComponent<SetBiomeHeightOnSpawn>();
            if (heightOnSpawn == null)
                heightOnSpawn = biome.gameObject.AddComponent<SetBiomeHeightOnSpawn>();
            heightOnSpawn.worldHeight = BasePlateauWorldHeightMeters;
            heightOnSpawn.ApplyHeight(manager);

            biome.seed = new WorldSeed(seed).Fork("terrain.islandbase").Next();
            ApplySeededNaturalIslandAnchors(biome, seed);

            var opts = biome.blendOptions;
            opts.heightMapBlendMode = BiomeBlendOptions.HeightBlendMode.Replace;
            opts.textureBlendMode = BiomeBlendOptions.TextureBlendMode.Replace;
            opts.useTransformForHeightBlend = true;
            biome.blendOptions = opts;

            Debug.Log(
                $"[IslandBase] Plateau LPB order=0 plateauH={BasePlateauWorldHeightMeters:F0} " +
                $"scaleY={biome.transform.localScale.y:F3} " +
                $"graph={plateauGraph.name} anchors={biome.anchors?.Length ?? 0} " +
                $"falloff={IslandFalloffLocalMeters:F0} maskGraph={(biome.biomeMaskGraph != null)} " +
                $"seed={biome.seed}");
        }

        private static void SpawnSeededMountainLpbs(VistaManager manager, int seed)
        {
            var template = AssetDatabase.LoadAssetAtPath<BiomeTemplate>(BlankBiomeTemplatePath);
            string templateError = null;
            if (template == null || !template.IsValid(out templateError))
            {
                Debug.LogError(
                    $"[MountainLPB] Blank template missing/invalid at {BlankBiomeTemplatePath}: {templateError}");
                return;
            }

            var massifGraph = MountainMassifGraphBuilder.LoadOrRebuild();
            if (massifGraph == null)
            {
                Debug.LogError("[MountainLPB] Failed to build MountainMassif_TerrainGraph.");
                return;
            }

            var maskGraph = SoftFalloffMaskGraphBuilder.LoadOrRebuild();
            var rng = new WorldSeed(seed).Fork("terrain.mountain.place");
            // Prefer a single major massif; rarely a second drastically smaller foothill.
            var targetCount = rng.NextDouble() < 0.72
                ? 1
                : Mathf.Min(2, WorldScale.PeakPreferredMaxCount);
            var cliffYaw = SampleCliffCoastYawRadians(seed);
            Debug.Log(
                $"[MountainLPB] Target massifs={targetCount} (prefer 1 major; 2nd is small). " +
                $"cliffYaw={cliffYaw * Mathf.Rad2Deg:F0}° seed={seed}");

            var center = new Vector3(
                WorldScale.WorldExtentXMeters * 0.5f,
                0f,
                WorldScale.WorldExtentZMeters * 0.5f);
            var usableRadius = Mathf.Min(WorldScale.WorldExtentXMeters, WorldScale.WorldExtentZMeters) * 0.5f
                               - WorldScale.IslandOceanMarginMeters - 280f;
            usableRadius = Mathf.Max(200f, usableRadius);

            var placed = new List<Vector3>(targetCount);
            for (var i = 0; i < targetCount; i++)
            {
                Vector3 worldPos = default;
                var ok = false;
                for (var attempt = 0; attempt < 32; attempt++)
                {
                    // Major massif sits on the cliff/mountain coast; foothills stay random.
                    float angle;
                    if (i == 0 && attempt < 12)
                    {
                        angle = cliffYaw + ((float)rng.NextDouble() - 0.5f) * 0.9f;
                    }
                    else
                    {
                        angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    }

                    var dist = usableRadius * (0.12f + 0.5f * (float)rng.NextDouble());
                    worldPos = center + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                    ok = true;
                    for (var p = 0; p < placed.Count; p++)
                    {
                        if (Vector3.Distance(worldPos, placed[p]) < WorldScale.PeakMinSeparationMeters)
                        {
                            ok = false;
                            break;
                        }
                    }

                    if (ok)
                        break;
                }

                if (!ok)
                {
                    Debug.LogWarning($"[MountainLPB] Could not place massif index={i} (separation).");
                    continue;
                }

                placed.Add(worldPos);
                SpawnOneMountainLpb(manager, template, massifGraph, maskGraph, seed, i, worldPos, rng);
            }

            Debug.Log(
                $"[MountainLPB] Spawned {placed.Count}/{targetCount} massif LPB(s) from seed={seed}");
        }

        private static void SpawnOneMountainLpb(
            VistaManager manager,
            BiomeTemplate template,
            TerrainGraph massifGraph,
            BiomeMaskGraph maskGraph,
            int worldSeed,
            int index,
            Vector3 worldPos,
            Random rng)
        {
            var spawnContext = new BiomeTemplateSpawnContext(manager);
            var spawnedRoot = BiomeTemplateSpawner.Spawn(template, spawnContext);
            RenameOwnedObjects(spawnContext, $"Mountain_{index}");
            OverworldTerrainMenu.PersistSpawnedOwnedObjectsPublic(spawnedRoot, spawnContext);

            var biome = spawnedRoot.GetComponentInChildren<LocalProceduralBiome>(true);
            if (biome == null)
                return;

            var scale = Mathf.Lerp(MountainLpbScaleMin, MountainLpbScaleMax, (float)rng.NextDouble());
            var peakDelta = Mathf.Lerp(
                MountainPeakDeltaMinMeters,
                MountainPeakDeltaMaxMeters,
                (float)rng.NextDouble());
            // Secondary massif: drastically smaller foothill, not a twin peak.
            if (index > 0)
            {
                scale *= 0.38f;
                peakDelta *= 0.32f;
            }

            biome.gameObject.name = $"LPB_Mountain_{index}";
            biome.space = Space.Self;
            biome.transform.position = worldPos;
            biome.transform.localRotation = Quaternion.identity;
            biome.transform.localScale = new Vector3(scale, 1f, scale);
            // 512 over a ~2500 m massif is 4.9 m/px — coarser than the 1400 m / 513 (2.73 m/px) terrain
            // it is resampled onto, which is half of why the massif read as a smoothed cone. It also
            // caps HydraulicErosion2's sim canvas (min(2048, baseResolution)) at 192 px, so drainage
            // could only ever be a coarse radial comb. 1024 → 2.4 m/px and a ~424 px erosion canvas.
            biome.baseResolution = MountainBaseResolution;
            biome.order = OrderMountainBase + index;
            biome.dataMask = BiomeDataMask.HeightMap | BiomeDataMask.HoleMap | BiomeDataMask.LayerWeightMaps;
            biome.terrainGraph = massifGraph;
            biome.biomeMaskGraph = maskGraph;
            biome.seed = new WorldSeed(worldSeed).Fork($"terrain.mountain.{index}").Next();

            var heightOnSpawn = biome.GetComponent<SetBiomeHeightOnSpawn>();
            if (heightOnSpawn == null)
                heightOnSpawn = biome.gameObject.AddComponent<SetBiomeHeightOnSpawn>();
            heightOnSpawn.worldHeight = peakDelta;
            heightOnSpawn.ApplyHeight(manager);

            biome.transform.position = new Vector3(worldPos.x, 0f, worldPos.z);
            var yScale = biome.transform.localScale.y;
            biome.transform.localScale = new Vector3(scale, yScale, scale);

            // Also shrink secondary mountain anchors.
            if (index > 0)
            {
                ApplyOrganicMountainAnchors(biome, worldSeed, index);
                var anchors = biome.anchors;
                if (anchors != null)
                {
                    for (var a = 0; a < anchors.Length; a++)
                        anchors[a] *= 0.45f;
                    biome.anchors = anchors;
                }
            }
            else
            {
                ApplyOrganicMountainAnchors(biome, worldSeed, index);
            }

            var opts = biome.blendOptions;
            // Raise = additive geology on IslandBase (graph periphery ≈ 0 → soft foothills).
            opts.heightMapBlendMode = BiomeBlendOptions.HeightBlendMode.Raise;
            opts.textureBlendMode = BiomeBlendOptions.TextureBlendMode.Replace;
            opts.useTransformForHeightBlend = true;
            biome.blendOptions = opts;

            Debug.Log(
                $"[MountainLPB] {biome.gameObject.name} massif world=({worldPos.x:F0},{worldPos.z:F0}) " +
                $"deltaH={peakDelta:F0} scaleXZ={scale:F2} scaleY={yScale:F3} " +
                $"falloff={MountainFalloffLocalMeters:F0} Raise order={biome.order} seed={biome.seed}");
        }

        public static void ApplySeededNaturalIslandAnchors(LocalProceduralBiome biome, int worldSeed)
        {
            if (biome == null)
                return;
            biome.falloffDirection = FalloffDirection.Outer;
            biome.falloffDistance = IslandFalloffLocalMeters;
            biome.anchors = BuildSeededNaturalIslandLocalAnchors(biome.transform, worldSeed);
        }

        public static void ApplyOrganicMountainAnchors(
            LocalProceduralBiome biome,
            int worldSeed,
            int index)
        {
            if (biome == null)
                return;
            biome.falloffDirection = FalloffDirection.Outer;
            biome.falloffDistance = MountainFalloffLocalMeters;
            biome.anchors = BuildOrganicMountainLocalAnchors(biome.transform, worldSeed, index);
        }

        /// <summary>
        /// Asymmetric island: cliff solid toward mountain coast, beach solid inset opposite
        /// so Outer falloff becomes a sand shelf on one side only.
        /// </summary>
        public static Vector3[] BuildSeededNaturalIslandLocalAnchors(Transform biomeTransform, int worldSeed)
        {
            if (biomeTransform == null)
                return System.Array.Empty<Vector3>();

            var center = new Vector3(
                WorldScale.WorldExtentXMeters * 0.5f,
                0f,
                WorldScale.WorldExtentZMeters * 0.5f);

            var falloffWorld = IslandFalloffLocalMeters * BaseLocalScaleXz + 12f;
            var half = Mathf.Min(WorldScale.WorldExtentXMeters, WorldScale.WorldExtentZMeters) * 0.5f;
            var softRim = falloffWorld + 8f;
            var maxCliffSolid = Mathf.Max(200f, half - softRim);
            // Beach solid inset so Outer falloff becomes a long sand shelf (not a mesa wall).
            // Keep enough solid for a walkable plain; wider falloff already softens the rim.
            var maxBeachSolid = Mathf.Max(480f, maxCliffSolid - falloffWorld * 0.85f);
            var baseRadius = maxCliffSolid / 1.04f;
            var cliffYaw = SampleCliffCoastYawRadians(worldSeed);

            var rng = new WorldSeed(worldSeed).Fork("terrain.islandring");
            var a1 = 0.16f + 0.06f * (float)rng.NextDouble();
            var a2 = 0.07f + 0.06f * (float)rng.NextDouble();
            var a3 = 0.03f + 0.04f * (float)rng.NextDouble();
            var phi1 = (float)rng.NextDouble() * Mathf.PI * 2f;
            var phi2 = (float)rng.NextDouble() * Mathf.PI * 2f;
            var phi3 = (float)rng.NextDouble() * Mathf.PI * 2f;

            var localAnchors = new Vector3[IslandAnchorCount];
            for (var i = 0; i < IslandAnchorCount; i++)
            {
                var angle = (Mathf.PI * 2f * i) / IslandAnchorCount;
                // 1 = cliff/mountain coast, 0 = beach coast (opposite).
                var cliffFacing = 0.5f + 0.5f * Mathf.Cos(angle - cliffYaw);
                cliffFacing = cliffFacing * cliffFacing; // bias toward hard cliff vs soft beach
                var radiusCap = Mathf.Lerp(maxBeachSolid, maxCliffSolid, cliffFacing);

                var harmonic = 1f
                               + a1 * Mathf.Cos(angle + phi1)
                               + a2 * Mathf.Cos(2f * angle + phi2)
                               + a3 * Mathf.Cos(3f * angle + phi3);
                var noise = ((float)rng.NextDouble() * 2f - 1f) * IslandRadiusNoiseFrac;
                var radius = Mathf.Min(radiusCap, baseRadius * Mathf.Max(0.55f, harmonic + noise));
                var world = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);
                world.x = Mathf.Clamp(world.x, falloffWorld, WorldScale.WorldExtentXMeters - falloffWorld);
                world.z = Mathf.Clamp(world.z, falloffWorld, WorldScale.WorldExtentZMeters - falloffWorld);
                localAnchors[i] = biomeTransform.InverseTransformPoint(world);
            }

            SmoothClosedPolygonInPlace(localAnchors, passes: 2);
            for (var i = 0; i < localAnchors.Length; i++)
            {
                var world = biomeTransform.TransformPoint(localAnchors[i]);
                world.x = Mathf.Clamp(world.x, falloffWorld, WorldScale.WorldExtentXMeters - falloffWorld);
                world.z = Mathf.Clamp(world.z, falloffWorld, WorldScale.WorldExtentZMeters - falloffWorld);
                localAnchors[i] = biomeTransform.InverseTransformPoint(world);
            }

            Debug.Log(
                $"[IslandBase] Coast asymmetry cliffYaw={cliffYaw * Mathf.Rad2Deg:F0}° " +
                $"cliffSolid≤{maxCliffSolid:F0} beachSolid≤{maxBeachSolid:F0} " +
                $"falloff={IslandFalloffLocalMeters:F0} sea={WorldScale.SeaLevelMeters:F0}");

            return localAnchors;
        }

        /// <summary>Shared seeded yaw: cliff/mountain coast (beach is opposite).</summary>
        private static float SampleCliffCoastYawRadians(int worldSeed)
        {
            return CoastalBeachShelf.SampleCliffCoastYawRadians(worldSeed);
        }

        /// <summary>
        /// Ridge-like irregular ellipse (not a circular stamp). Random yaw + edge harmonics.
        /// </summary>
        public static Vector3[] BuildOrganicMountainLocalAnchors(
            Transform biomeTransform,
            int worldSeed,
            int index)
        {
            if (biomeTransform == null)
                return System.Array.Empty<Vector3>();

            var rng = new WorldSeed(worldSeed).Fork($"terrain.mountain.ring.{index}");
            var major = Mathf.Lerp(MountainMajorAxisMin, MountainMajorAxisMax, (float)rng.NextDouble()) * 0.5f;
            var minor = Mathf.Lerp(MountainMinorAxisMin, MountainMinorAxisMax, (float)rng.NextDouble()) * 0.5f;
            var yaw = (float)rng.NextDouble() * Mathf.PI * 2f;
            var cosY = Mathf.Cos(yaw);
            var sinY = Mathf.Sin(yaw);
            var a1 = 0.12f + 0.1f * (float)rng.NextDouble();
            var a2 = 0.06f + 0.08f * (float)rng.NextDouble();
            var phi1 = (float)rng.NextDouble() * Mathf.PI * 2f;
            var phi2 = (float)rng.NextDouble() * Mathf.PI * 2f;

            var localAnchors = new Vector3[MountainAnchorCount];
            for (var i = 0; i < MountainAnchorCount; i++)
            {
                var angle = (Mathf.PI * 2f * i) / MountainAnchorCount;
                var harmonic = 1f + a1 * Mathf.Cos(angle + phi1) + a2 * Mathf.Cos(2f * angle + phi2);
                var ex = Mathf.Cos(angle) * major * harmonic;
                var ez = Mathf.Sin(angle) * minor * harmonic;
                // Rotate ellipse in local XZ.
                var lx = ex * cosY - ez * sinY;
                var lz = ex * sinY + ez * cosY;
                localAnchors[i] = new Vector3(lx, 0f, lz);
            }

            SmoothClosedPolygonInPlace(localAnchors, passes: 2);
            return localAnchors;
        }

        private static void SmoothClosedPolygonInPlace(Vector3[] points, int passes)
        {
            if (points == null || points.Length < 3 || passes <= 0)
                return;
            var scratch = new Vector3[points.Length];
            for (var pass = 0; pass < passes; pass++)
            {
                for (var i = 0; i < points.Length; i++)
                {
                    var prev = points[(i - 1 + points.Length) % points.Length];
                    var next = points[(i + 1) % points.Length];
                    scratch[i] = (prev * 0.25f) + (points[i] * 0.5f) + (next * 0.25f);
                }

                for (var i = 0; i < points.Length; i++)
                    points[i] = scratch[i];
            }
        }

        private static void RenameOwnedObjects(BiomeTemplateSpawnContext context, string prefix)
        {
            if (context == null)
                return;
            foreach (var data in context.ownedObjects)
            {
                if (data == null)
                    continue;
                data.name = $"{prefix}_{data.name}";
            }
        }
    }
}
#endif
