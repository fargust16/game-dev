#if UNITY_EDITOR
using System.Collections.Generic;
using GameDev.World;
using Pinwheel.Vista;
using UnityEditor;
using UnityEngine;
using WorldSeed = GameDev.World.Seed.WorldSeed;
using Random = System.Random;

namespace GameDev.EditorTools
{
    /// <summary>
    /// World composition for the overworld: which biomes exist, where they sit, and what silhouette
    /// they have. Everything else about a biome belongs to its template prefab.
    /// </summary>
    /// <remarks>
    /// This is the orchestration layer and nothing more. It spawns the two native Vista biome
    /// templates (<see cref="OverworldBiomeTemplates"/>) and then sets only what is genuinely a
    /// per-world decision: the seed, the XZ placement, and the anchor silhouette.
    /// <para>
    /// It deliberately does <b>not</b> write biome height, area, blend options, data mask,
    /// resolution or order — those are serialized on the prefab and edited in the Inspector.
    /// <see cref="SetBiomeHeightOnSpawn"/> and <see cref="SetBiomeAreaOnSpawn"/> have already run by
    /// the time this code sees the biome, and their results are read, never replaced.
    /// </para>
    /// <para>
    /// The one thing Vista's area component cannot express is a non-rectangular outline. So the
    /// rectangle it wrote is treated as the authoritative <em>extent</em>, and the seeded coastline
    /// or ridge silhouette is inscribed inside it. Changing Set Biome Area On Spawn in the Inspector
    /// therefore still changes the biome's size, exactly as expected.
    /// </para>
    /// </remarks>
    public static class OverworldLpbBake
    {
        public const int IslandAnchorCount = 36;
        public const int MountainAnchorCount = 28;

        /// <summary>How far the coastline may wander in or out, as a fraction of the extent.</summary>
        private const float IslandRadiusNoiseFrac = 0.04f;

        /// <summary>
        /// Diagnostic only: give the island a square, world-covering footprint with no falloff.
        /// </summary>
        /// <remarks>
        /// The control that separates "our graph bands" from "our island *shape* bands". Vista's
        /// sample graph was called clean, but it differs from ours in two ways at once — a different
        /// recipe and no silhouette, since it covers the whole square with falloff 0. This makes our
        /// own graph match the second condition so the first can be tested on its own. The visible
        /// surface is <c>mask x graphHeight</c>, and our mask is a deliberately asymmetric polygon,
        /// so it is a live suspect for the directional bias.
        /// </remarks>
        public static bool IslandWithoutSilhouetteForDiagnostics { get; set; }

        /// <summary>Overrides the island biome's vertical extent, in metres.</summary>
        /// <remarks>
        /// The prefab ships 140 m against a 700 m tile budget, so the island occupies a fifth of the
        /// heightmap range and its fine detail lands within one or two 16-bit quantisation steps
        /// (21 mm at 700 m). The Vista reference carries the same detail at roughly ten steps. This
        /// exists to test whether the banding is rounding noise from a signal too small for the
        /// heightmap, which every earlier experiment would have missed -- they judged a curvature
        /// ratio, which is invariant to vertical scale and therefore blind to exactly this.
        /// </remarks>
        public static float? IslandWorldHeightForDiagnostics { get; set; }

        /// <summary>Spawns the stack without the seabed biome underneath the island.</summary>
        /// <remarks>
        /// The Vista reference is a single biome with nothing below it; we blend the island (order
        /// 10) over a 5600 m seabed (order 0). That blend is one of the two structural differences
        /// left between the two after the mask, footprint, baseResolution and height budget were
        /// each measured and cleared.
        /// </remarks>
        public static bool SkipSeaFloorForDiagnostics { get; set; }

        /// <summary>Overrides the island biome's square extent, in metres.</summary>
        /// <remarks>
        /// The last structural difference from the Vista reference. Its biome is 4700 m across a
        /// 2800 m world, so it overhangs the edges and only ~305 of its 512 canvas pixels land on
        /// the world -- 9.18 m/px. Ours is cut to the world and spends all 512 there, 5.47 m/px.
        /// Ours is the finer canvas and ours is the one that bands, which matches the earlier
        /// finding that fewer octaves banded coarser rather than less: the more fine detail the
        /// graph resolves onto the axis-aligned world lattice, the stronger the hatch.
        /// </remarks>
        public static float? IslandBiomeWidthForDiagnostics { get; set; }

        /// <summary>Detaches the island biome's mask graph.</summary>
        /// <remarks>
        /// The Vista reference runs with <c>m_biomeMaskGraph: {fileID: 0}</c> -- no mask graph at
        /// all -- while we attach SoftFalloff_BiomeMaskGraph. The mask multiplies the height field
        /// whatever falloffDistance is set to, so it stamps straight onto the terrain, and it is the
        /// last structural difference left after the mask polygon, footprint, canvas density,
        /// baseResolution, height budget and the seabed underneath were each measured and cleared.
        /// </remarks>
        public static bool SkipIslandMaskGraphForDiagnostics { get; set; }

        /// <summary>Secondary massifs are foothills, not twin peaks.</summary>
        private const float SecondaryMassifScale = 0.38f;

        /// <summary>
        /// Spawns the world's biome stack: seabed, island, and optionally the massifs.
        /// </summary>
        /// <param name="includeMountains">
        /// When false, only the seabed and island are spawned. That is the reference bake for
        /// terrain diagnostics — it isolates the island graph from the massif's Raise blend — and
        /// it is what <c>TerrainBakeFlags.IncludeMountains</c> switches off.
        /// </param>
        public static void SpawnIslandAndMountainStack(
            VistaManager manager, int seed, bool includeMountains = true)
        {
            if (SkipSeaFloorForDiagnostics)
            {
                Debug.LogWarning(
                    "[SeaFloor] DIAGNOSTIC: seabed biome skipped; the world below the island is "
                    + "whatever the terrain defaults to.");
            }
            else
            {
                SpawnSeaFloorLpb(manager, seed);
            }
            SpawnBaseIslandLpb(manager, seed);
            if (includeMountains)
            {
                SpawnSeededMountainLpbs(manager, seed);
            }
        }

        /// <summary>
        /// The seabed under the whole world, so the island has something to blend down to.
        /// </summary>
        /// <remarks>
        /// Nothing to place and nothing to shape: <see cref="SnapBiomeToTilesOnSpawn"/> on the
        /// template already fitted the polygon to every tile the manager owns, at whatever world size
        /// the project runs. Only the seed is ours.
        /// </remarks>
        private static void SpawnSeaFloorLpb(VistaManager manager, int seed)
        {
            var template = OverworldBiomeTemplates.EnsureSeaFloorTemplate();
            if (template == null)
            {
                return;
            }

            var biome = SpawnBiome(manager, template, "SeaFloor", "LPB_SeaFloor");
            if (biome == null)
            {
                return;
            }

            biome.seed = new WorldSeed(seed).Fork("terrain.seafloor").Next();
            LogBiome("SeaFloor", biome);
        }

        /// <summary>
        /// Re-runs the spawn components on the biomes already in the scene, then regenerates.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="SetBiomeHeightOnSpawn"/> and <see cref="SetBiomeAreaOnSpawn"/> are one-shots:
        /// they write <c>transform.localScale.y</c> and the anchor polygon at spawn time and then
        /// never look at their own fields again. Editing <c>World Height</c> on a biome that is
        /// already in the scene therefore does nothing until something re-applies it — this does,
        /// using Vista's own components rather than a parallel height path.
        /// </para>
        /// <para>
        /// Area is re-applied through <see cref="SetBiomeAreaOnSpawn"/>, which writes a rectangle,
        /// so the organic silhouette is re-inscribed afterwards exactly as the bake does it.
        /// </para>
        /// <para>
        /// A full Generate still respawns from the template prefabs, so this is for iteration; the
        /// durable place to change a value is the prefab.
        /// </para>
        /// </remarks>
        [MenuItem("GameDev/Overworld/Vista/Apply Biome Inspector Settings + Regenerate")]
        public static void ApplyInspectorSettingsMenu()
        {
            var manager = Object.FindAnyObjectByType<VistaManager>();
            if (manager == null)
            {
                Debug.LogWarning("[Overworld] No VistaManager in the scene.");
                return;
            }

            var biomes = manager.GetComponentsInChildren<LocalProceduralBiome>(true);
            if (biomes.Length == 0)
            {
                Debug.LogWarning("[Overworld] No Local Procedural Biomes under the manager.");
                return;
            }

            var seed = OverworldTerrainMenu.LastSeed;
            var massifIndex = 0;
            for (var i = 0; i < biomes.Length; i++)
            {
                var biome = biomes[i];
                var isIsland = biome.gameObject.name.IndexOf(
                    "Island", System.StringComparison.OrdinalIgnoreCase) >= 0;
                // The seabed is snapped to the tiles on purpose — a silhouette would carve holes in
                // the world floor.
                var isSeaFloor = biome.gameObject.name.IndexOf(
                    "SeaFloor", System.StringComparison.OrdinalIgnoreCase) >= 0;

                var height = biome.GetComponent<SetBiomeHeightOnSpawn>();
                if (height != null)
                {
                    height.ApplyHeight(manager);
                }

                var snap = biome.GetComponent<SnapBiomeToTilesOnSpawn>();
                if (snap != null)
                {
                    snap.SnapToTiles(manager);
                }

                var area = biome.GetComponent<SetBiomeAreaOnSpawn>();
                if (area != null)
                {
                    area.ApplyArea();
                    biome.anchors = isIsland
                        ? BuildIslandSilhouette(biome, seed)
                        : BuildMassifSilhouette(biome, seed, massifIndex);
                }

                if (!isIsland && !isSeaFloor)
                {
                    massifIndex++;
                }

                LogBiome($"Re-applied {biome.gameObject.name}", biome);
            }

            manager.Generate(manager.GetTiles());
            Debug.Log(
                $"[Overworld] Re-applied Inspector settings on {biomes.Length} biome(s) and requested " +
                "a regenerate. Note: a full Generate All respawns from the template prefabs, so put " +
                "the value you want to keep on the prefab.");
        }

        private static void SpawnBaseIslandLpb(VistaManager manager, int seed)
        {
            var template = OverworldBiomeTemplates.EnsureIslandTemplate();
            if (template == null)
            {
                return;
            }

            var biome = SpawnBiome(manager, template, "IslandBase", "LPB_IslandBase");
            if (biome == null)
            {
                return;
            }

            var center = WorldCenter();
            biome.transform.position = new Vector3(center.x, biome.transform.position.y, center.z);
            biome.seed = new WorldSeed(seed).Fork("terrain.islandbase").Next();

            if (IslandWorldHeightForDiagnostics.HasValue)
            {
                // Both the component and the transform: SetBiomeHeightOnSpawn has already applied
                // its value by now, so the scale is what actually changes the bake, and the
                // component is updated so the spawn log does not lie about it.
                var target = IslandWorldHeightForDiagnostics.Value;
                var heightComponent = biome.GetComponent<SetBiomeHeightOnSpawn>();
                if (heightComponent != null)
                {
                    heightComponent.worldHeight = target;
                }

                var scale = biome.transform.localScale;
                scale.y = target / WorldScale.TerrainHeightBudgetMeters;
                biome.transform.localScale = scale;
                Debug.LogWarning(
                    $"[IslandBase] DIAGNOSTIC worldHeight override -> {target:F0} m "
                    + $"(scaleY {scale.y:F3}).");
            }

            if (SkipIslandMaskGraphForDiagnostics)
            {
                biome.biomeMaskGraph = null;
                Debug.LogWarning(
                    "[IslandBase] DIAGNOSTIC: mask graph detached; the biome contributes its raw "
                    + "graph output with no falloff shaping.");
            }

            if (IslandWithoutSilhouetteForDiagnostics || IslandBiomeWidthForDiagnostics.HasValue)
            {
                var halfX = (IslandBiomeWidthForDiagnostics ?? WorldScale.WorldExtentXMeters) * 0.5f;
                var halfZ = (IslandBiomeWidthForDiagnostics ?? WorldScale.WorldExtentZMeters) * 0.5f;
                biome.anchors = new[]
                {
                    new Vector3(-halfX, 0f, -halfZ),
                    new Vector3(-halfX, 0f, halfZ),
                    new Vector3(halfX, 0f, halfZ),
                    new Vector3(halfX, 0f, -halfZ),
                };
                biome.falloffDistance = 0f;

                // The canvas is sized from worldArea, so moving the anchors alone would leave m/px
                // untouched and measure nothing.
                var areaComponent = biome.GetComponent<SetBiomeAreaOnSpawn>();
                if (areaComponent != null)
                {
                    areaComponent.worldArea = new Vector2(halfX * 2f, halfZ * 2f);
                }
                Debug.LogWarning(
                    "[IslandBase] DIAGNOSTIC: square world-covering footprint, no falloff. " +
                    "There is no island and no coast — this bake is for measurement only.");
            }
            else
            {
                biome.anchors = BuildIslandSilhouette(biome, seed);
            }

            LogBiome("IslandBase", biome);
        }

        private static void SpawnSeededMountainLpbs(VistaManager manager, int seed)
        {
            var template = OverworldBiomeTemplates.EnsureMountainTemplate();
            if (template == null)
            {
                return;
            }

            var rng = new WorldSeed(seed).Fork("terrain.mountain.place");
            // Prefer a single major massif; occasionally a much smaller second one.
            var targetCount = rng.NextDouble() < 0.72
                ? 1
                : Mathf.Min(2, WorldScale.PeakPreferredMaxCount);
            var cliffYaw = CliffCoastYawRadians(seed);

            var center = WorldCenter();
            var usableRadius = Mathf.Max(
                200f,
                Mathf.Min(WorldScale.WorldExtentXMeters, WorldScale.WorldExtentZMeters) * 0.5f
                - WorldScale.IslandOceanMarginMeters - 280f);

            var placed = new List<Vector3>(targetCount);
            for (var i = 0; i < targetCount; i++)
            {
                if (!TryPlaceMassif(rng, center, usableRadius, cliffYaw, i, placed, out var worldPos))
                {
                    Debug.LogWarning($"[MountainLPB] Could not place massif index={i} (separation).");
                    continue;
                }

                placed.Add(worldPos);
                var biome = SpawnBiome(manager, template, $"Mountain_{i}", $"LPB_Mountain_{i}");
                if (biome == null)
                {
                    continue;
                }

                // Offset from the order authored on the prefab, so several instances of the same
                // template stack deterministically without discarding the Inspector value.
                biome.order += i;
                biome.transform.position = new Vector3(worldPos.x, biome.transform.position.y, worldPos.z);
                biome.seed = new WorldSeed(seed).Fork($"terrain.mountain.{i}").Next();
                biome.anchors = BuildMassifSilhouette(biome, seed, i);

                LogBiome($"MountainLPB {biome.gameObject.name}", biome);
            }

            Debug.Log(
                $"[MountainLPB] Spawned {placed.Count}/{targetCount} massif LPB(s), " +
                $"cliffYaw={cliffYaw * Mathf.Rad2Deg:F0}° seed={seed}");
        }

        private static bool TryPlaceMassif(
            Random rng,
            Vector3 center,
            float usableRadius,
            float cliffYaw,
            int index,
            List<Vector3> placed,
            out Vector3 worldPos)
        {
            worldPos = default;
            for (var attempt = 0; attempt < 32; attempt++)
            {
                // The major massif sits on the cliff coast; foothills go anywhere.
                var angle = index == 0 && attempt < 12
                    ? cliffYaw + ((float)rng.NextDouble() - 0.5f) * 0.9f
                    : (float)rng.NextDouble() * Mathf.PI * 2f;

                var dist = usableRadius * (0.12f + 0.5f * (float)rng.NextDouble());
                worldPos = center + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

                var ok = true;
                for (var p = 0; p < placed.Count; p++)
                {
                    if (Vector3.Distance(worldPos, placed[p]) < WorldScale.PeakMinSeparationMeters)
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok)
                {
                    return true;
                }
            }

            return false;
        }

        private static LocalProceduralBiome SpawnBiome(
            VistaManager manager, BiomeTemplate template, string ownedPrefix, string biomeName)
        {
            var spawnContext = new BiomeTemplateSpawnContext(manager);
            var spawnedRoot = BiomeTemplateSpawner.Spawn(template, spawnContext);
            RenameOwnedObjects(spawnContext, ownedPrefix);
            OverworldTerrainMenu.PersistSpawnedOwnedObjectsPublic(spawnedRoot, spawnContext);

            var biome = spawnedRoot.GetComponentInChildren<LocalProceduralBiome>(true);
            if (biome == null)
            {
                Debug.LogError($"[Overworld] Template '{template.name}' spawned no LocalProceduralBiome.");
                return null;
            }

            biome.gameObject.name = biomeName;
            return biome;
        }

        /// <summary>
        /// Reads the half-extents of whatever rectangle the biome's area component produced.
        /// </summary>
        /// <remarks>
        /// Returns false when the biome has no usable polygon, in which case the caller must leave
        /// the anchors alone rather than invent an extent of its own.
        /// </remarks>
        private static bool TryGetAuthoredExtent(
            LocalProceduralBiome biome, out float halfWidth, out float halfLength)
        {
            halfWidth = 0f;
            halfLength = 0f;
            var anchors = biome.anchors;
            if (anchors == null || anchors.Length < 3)
            {
                Debug.LogWarning(
                    $"[Overworld] '{biome.gameObject.name}' has no anchor polygon after spawn — " +
                    "add a Set Biome Area On Spawn (or Snap Biome To Tiles On Spawn) to its prefab.");
                return false;
            }

            for (var i = 0; i < anchors.Length; i++)
            {
                halfWidth = Mathf.Max(halfWidth, Mathf.Abs(anchors[i].x));
                halfLength = Mathf.Max(halfLength, Mathf.Abs(anchors[i].z));
            }

            return halfWidth > 1f && halfLength > 1f;
        }

        /// <summary>
        /// An organic coastline inscribed in the authored extent: harmonics plus seeded noise, with
        /// the cliff-facing side reaching further out than the beach side.
        /// </summary>
        private static Vector3[] BuildIslandSilhouette(LocalProceduralBiome biome, int worldSeed)
        {
            return TryGetAuthoredExtent(biome, out var halfWidth, out var halfLength)
                ? BuildIslandSilhouette(halfWidth, halfLength, worldSeed)
                : biome.anchors;
        }

        /// <inheritdoc cref="BuildIslandSilhouette(LocalProceduralBiome,int)"/>
        public static Vector3[] BuildIslandSilhouette(float halfWidth, float halfLength, int worldSeed)
        {
            var cliffYaw = CliffCoastYawRadians(worldSeed);
            var rng = new WorldSeed(worldSeed).Fork("terrain.islandring");
            var a1 = 0.16f + 0.06f * (float)rng.NextDouble();
            var a2 = 0.07f + 0.06f * (float)rng.NextDouble();
            var a3 = 0.03f + 0.04f * (float)rng.NextDouble();
            var phi1 = (float)rng.NextDouble() * Mathf.PI * 2f;
            var phi2 = (float)rng.NextDouble() * Mathf.PI * 2f;
            var phi3 = (float)rng.NextDouble() * Mathf.PI * 2f;

            var anchors = new Vector3[IslandAnchorCount];
            for (var i = 0; i < IslandAnchorCount; i++)
            {
                var angle = Mathf.PI * 2f * i / IslandAnchorCount;

                // 1 = cliff coast, 0 = beach coast on the opposite side. The beach side pulls in so
                // the Outer falloff has room to become a shelf instead of a wall.
                var cliffFacing = 0.5f + 0.5f * Mathf.Cos(angle - cliffYaw);
                cliffFacing *= cliffFacing;
                var reach = Mathf.Lerp(0.72f, 1f, cliffFacing);

                var harmonic = 1f
                               + a1 * Mathf.Cos(angle + phi1)
                               + a2 * Mathf.Cos(2f * angle + phi2)
                               + a3 * Mathf.Cos(3f * angle + phi3);
                var noise = ((float)rng.NextDouble() * 2f - 1f) * IslandRadiusNoiseFrac;
                var t = Mathf.Clamp(reach * Mathf.Max(0.55f, harmonic + noise), 0.2f, 1f);

                anchors[i] = new Vector3(
                    Mathf.Cos(angle) * halfWidth * t,
                    0f,
                    Mathf.Sin(angle) * halfLength * t);
            }

            SmoothClosedPolygonInPlace(anchors, passes: 2);
            return anchors;
        }

        /// <summary>
        /// A ridge-shaped ellipse inscribed in the authored extent, rotated to a seeded yaw.
        /// </summary>
        private static Vector3[] BuildMassifSilhouette(
            LocalProceduralBiome biome, int worldSeed, int index)
        {
            if (!TryGetAuthoredExtent(biome, out var halfMajor, out var halfMinor))
            {
                return biome.anchors;
            }

            if (index > 0)
            {
                halfMajor *= SecondaryMassifScale;
                halfMinor *= SecondaryMassifScale;
            }

            var rng = new WorldSeed(worldSeed).Fork($"terrain.mountain.ring.{index}");
            var yaw = (float)rng.NextDouble() * Mathf.PI * 2f;
            var cosY = Mathf.Cos(yaw);
            var sinY = Mathf.Sin(yaw);
            var a1 = 0.12f + 0.1f * (float)rng.NextDouble();
            var a2 = 0.06f + 0.08f * (float)rng.NextDouble();
            var phi1 = (float)rng.NextDouble() * Mathf.PI * 2f;
            var phi2 = (float)rng.NextDouble() * Mathf.PI * 2f;

            var anchors = new Vector3[MountainAnchorCount];
            for (var i = 0; i < MountainAnchorCount; i++)
            {
                var angle = Mathf.PI * 2f * i / MountainAnchorCount;
                // Keep the harmonic mean at 1 so the ridge stays inside the authored extent.
                var harmonic = 1f / (1f + a1 + a2)
                               * (1f + a1 * Mathf.Cos(angle + phi1) + a2 * Mathf.Cos(2f * angle + phi2));
                var ex = Mathf.Cos(angle) * halfMajor * harmonic;
                var ez = Mathf.Sin(angle) * halfMinor * harmonic;
                anchors[i] = new Vector3(ex * cosY - ez * sinY, 0f, ex * sinY + ez * cosY);
            }

            SmoothClosedPolygonInPlace(anchors, passes: 2);
            return anchors;
        }

        /// <summary>Seeded yaw of the cliff coast; the beach coast is the opposite side.</summary>
        public static float CliffCoastYawRadians(int worldSeed)
        {
            var rng = new WorldSeed(worldSeed).Fork("terrain.coast.yaw");
            return (float)rng.NextDouble() * Mathf.PI * 2f;
        }

        private static Vector3 WorldCenter()
        {
            return new Vector3(
                WorldScale.WorldExtentXMeters * 0.5f,
                0f,
                WorldScale.WorldExtentZMeters * 0.5f);
        }

        private static void LogBiome(string tag, LocalProceduralBiome biome)
        {
            var height = biome.GetComponent<SetBiomeHeightOnSpawn>();
            var area = biome.GetComponent<SetBiomeAreaOnSpawn>();
            Debug.Log(
                $"[{tag}] order={biome.order} " +
                $"worldHeight={(height != null ? height.worldHeight.ToString("F0") : "n/a")} " +
                $"worldArea={(area != null ? area.worldArea.ToString("F0") : "n/a")} " +
                $"scaleY={biome.transform.localScale.y:F3} " +
                $"graph={(biome.terrainGraph != null ? biome.terrainGraph.name : "NULL")} " +
                $"maskGraph={(biome.biomeMaskGraph != null)} anchors={biome.anchors?.Length ?? 0} " +
                $"falloff={biome.falloffDistance:F0} seed={biome.seed} " +
                "(height/area/blend come from the template prefab)");
        }

        private static void SmoothClosedPolygonInPlace(Vector3[] points, int passes)
        {
            if (points == null || points.Length < 3 || passes <= 0)
            {
                return;
            }

            var scratch = new Vector3[points.Length];
            for (var pass = 0; pass < passes; pass++)
            {
                for (var i = 0; i < points.Length; i++)
                {
                    var prev = points[(i - 1 + points.Length) % points.Length];
                    var next = points[(i + 1) % points.Length];
                    scratch[i] = prev * 0.25f + points[i] * 0.5f + next * 0.25f;
                }

                for (var i = 0; i < points.Length; i++)
                {
                    points[i] = scratch[i];
                }
            }
        }

        private static void RenameOwnedObjects(BiomeTemplateSpawnContext context, string prefix)
        {
            if (context == null)
            {
                return;
            }

            foreach (var data in context.ownedObjects)
            {
                if (data == null)
                {
                    continue;
                }

                data.name = $"{prefix}_{data.name}";
            }
        }
    }
}
#endif
