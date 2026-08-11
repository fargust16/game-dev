#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Den.Tools;
using GameDev.World.Seed;
using GameDev.World.Terrain;
using MapMagic.Core;
using MapMagic.Nodes;
using MapMagic.Nodes.MatrixGenerators;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Overworld V0 terrain groundwork (ahead of formal W0.3/W0.4): builds a MapMagic 2 height
    /// graph (Noise -> Erosion -> Height Output) seeded from <see cref="WorldSeed"/> and drops it
    /// into a lab scene, so terrain shape can be reviewed in the Editor before IBiomeSampler /
    /// ITerrainChunkBuilder are wired to query it.
    /// </summary>
    public static class OverworldTerrainMenu
    {
        public const string ScenePath = "Assets/Scenes/Overworld_Generator.unity";
        private const string GraphPath = "Assets/Settings/OverworldTerrainGraph.asset";
        private const int DefaultSeed = 110201;

        /// <summary>Seed of the last Generate call, so other tools (vegetation scatter) fork the same WorldSeed.</summary>
        public static int LastSeed { get; private set; }

        [MenuItem("GameDev/Overworld/Open Or Create Terrain Scene")]
        public static void OpenOrCreateScene()
        {
            EnsureSceneFolder();
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            else
            {
                CreateFreshSceneAndGenerate(DefaultSeed);
            }

            Debug.Log($"[Overworld] Scene: {ScenePath}");
        }

        [MenuItem("GameDev/Overworld/Generate Terrain From Seed")]
        public static void GenerateFromSeed()
        {
            EnsureSceneActive();
            GenerateInternal(DefaultSeed);
        }

        [MenuItem("GameDev/Overworld/Generate Terrain Random Seed")]
        public static void GenerateRandomSeed()
        {
            EnsureSceneActive();
            GenerateInternal(System.Environment.TickCount);
        }

        [MenuItem("GameDev/Overworld/Clear Terrain")]
        public static void ClearTerrain()
        {
            var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
            if (mapMagic == null)
            {
                Debug.LogWarning("[Overworld] No MapMagicObject in scene.");
                return;
            }

            mapMagic.graph = null;
            mapMagic.Refresh(true);

            WaterZoneMenu.ClearWaterZones();
            VegetationScatterMenu.ClearVegetation();
            PoiMarkerMenu.ClearMarkers();
            RoadNetworkMenu.ClearRoadNetwork();
            SettlementOverworldMenu.ClearSettlement();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static void CreateFreshSceneAndGenerate(int seed)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateSunLight();
            GenerateInternal(seed);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
        }

        /// <summary>
        /// NewSceneSetup.EmptyScene has no light at all, so terrain/water render flat and
        /// visually blend together - a directional light is the minimum needed to actually
        /// review generated shape.
        /// </summary>
        private static void CreateSunLight()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void GenerateInternal(int seed)
        {
            LastSeed = seed;

            // Vegetation/POI/Roads/Settlement are all derived from the terrain and become stale
            // (wrong biome, wrong spot relative to the new water level, routed across water that
            // didn't exist there before) the moment it regenerates - never leave them sitting on
            // the old shape. Roads are the most visibly broken by this: a stale route built against
            // an earlier lake layout can end up crossing straight through a lake the new generation
            // placed right where the old dry ground used to be.
            VegetationScatterMenu.ClearVegetation();
            PoiMarkerMenu.ClearMarkers();
            RoadNetworkMenu.ClearRoadNetwork();
            SettlementOverworldMenu.ClearSettlement();

            var graph = BuildGraph(seed);
            Debug.Log($"[Overworld] Graph built: {graph.generators.Length} generators, seed {seed}.");

            var mapMagic = FindOrCreateMapMagicObject();
            // MapMagicObject's own default (1000x1000) - bumped up so there's more room for both
            // a mountain feature and flat, buildable plains without either crowding the other out.
            mapMagic.tileSize = new Vector2D(1400, 1400);
            mapMagic.graph = graph;
            mapMagic.gameObject.SetActive(true);
            mapMagic.Refresh(true);
            Debug.Log($"[Overworld] Refresh(true) called. Pinned tiles before pin: {mapMagic.tiles.pinned.Count}.");

            // Outside Play mode MapMagic's own Update() only recalculates tile distances
            // (MapMagicObject.cs Update(): distsOnly = !isPlaying) and never constructs a tile
            // without one. Pin forces the origin tile to actually build in the Editor.
            var originCoord = new Coord(0, 0);
            mapMagic.tiles.Pin(originCoord, false, mapMagic);
            var tileCount = 0;
            foreach (var unused in mapMagic.tiles.Tiles())
            {
                tileCount++;
            }

            Debug.Log($"[Overworld] Pinned origin tile. Pinned tiles after pin: {mapMagic.tiles.pinned.Count}, " +
                      $"total tiles in grid: {tileCount}.");

            WaterZoneMenu.GenerateForTile(mapMagic, originCoord);

            SaveActive();
            Selection.activeGameObject = mapMagic.gameObject;
            Debug.Log($"[Overworld] Generated terrain from seed {seed}.");
        }

        private static MapMagicObject FindOrCreateMapMagicObject()
        {
            var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
            if (mapMagic != null)
            {
                return mapMagic;
            }

            // Stay inactive through AddComponent so OnEnable doesn't run (and throw on a null
            // graph) before the caller assigns one and reactivates it.
            var root = new GameObject("OverworldTerrain");
            root.SetActive(false);
            return root.AddComponent<MapMagicObject>();
        }

        /// <summary>
        /// Macro shape is percentile-ranked (see <see cref="PercentileRankGenerator"/>), not a raw
        /// noise field cut at a fixed threshold - the old approach (one huge-period noise, cut at a
        /// tuned constant) could not guarantee an area fraction, because a Perlin field's own value
        /// distribution shifts per seed: 0.5 covered roughly half the tile as "mountain-adjacent",
        /// 0.72 covered almost none, and no single constant fixed that, because the field's shape
        /// was the actual problem. Ranking guarantees it instead: <see cref="BiomeClassifier"/>'s
        /// height bands are literal area-fraction promises (bottom 10% Swamp-eligible, next 70%
        /// Green, next 10% Highland, top 10% Mountain) that hold for every seed, because rank IS
        /// the percentile by construction. One macroNoise field feeds the rank (not separate
        /// plateau/mountain fields - splitting them would defeat the single-distribution guarantee
        /// the ranking relies on); the mountain band is then terraced in isolation and swapped back
        /// in without changing which fraction of the map is "mountain" (see the mountain-terracing
        /// block below); six deliberate lake-pit sites (varied size/threshold, random world-space
        /// offsets, combined via max) are subtracted afterward to carve believable basins, same as
        /// before. Recipe: MacroNoise -> PercentileRank -> (+HillsNoise) -> [mountain terrace swap]
        /// -> -LakePits -> Erosion -> Height Output. Seeded from <see cref="WorldSeed"/> so the same
        /// world seed reproduces the same terrain, mountain, hills, terrace steps, and basin
        /// placement.
        /// <para>
        /// Lake-pit carving only needs to produce plausible dips - it does not need to reach any
        /// particular threshold. <see cref="GameDev.EditorTools.WaterZoneMenu"/> finds where water
        /// actually belongs by sampling the generated heightmap after this graph runs (flood-fill
        /// from real local minima, see <see cref="GameDev.World.Water.LakeBasinFinder"/>), so a
        /// basin that carves too shallow or too deep no longer produces a dry crater or a flooded
        /// plateau the way comparing against one fixed global water constant used to. Lakes are not
        /// part of the percentile-band guarantee (they carve into whichever band they land in, same
        /// as real hydrology sitting in low ground) - only the dry-land height bands are ranked.
        /// </para>
        /// </summary>
        private static Graph BuildGraph(int seed)
        {
            EnsureSettingsFolder();

            var terrainSeed = new WorldSeed(seed).Fork("terrain");
            var graphSeed = terrainSeed.Next();
            var macroSeed = terrainSeed.Next();
            var hillsSeed = terrainSeed.Next();
            var terraceSeed = terrainSeed.Next();
            var highlandTerraceSeed = terrainSeed.Next();
            var detailSeed = terrainSeed.Next();

            // Own top-level fork, not chained off terrainSeed - matches the seed hierarchy in
            // WORLD_GENERATION_PIPELINE.md ("terrain" and "biome" are independent streams), so a
            // future change to moisture generation can never shift terrain shaping the way a
            // shared sequential fork would (the doc's "Known gap" note this file already has).
            var moistureSeed = new WorldSeed(seed).Fork("biome").Next();

            var graph = Graph.Create();
            graph.random = new Noise(graphSeed, 32768);

            // One macro noise field for the whole map's elevation variety - period large enough
            // (~700m) relative to the tile that a few natural undulations show without repeating,
            // detailed enough (0.55) that it's not a single smooth blob before PercentileRank
            // reshapes its distribution.
            var macroNoise = (Noise200)Generator.Create(typeof(Noise200));
            macroNoise.type = Noise200.Type.Perlin;
            macroNoise.seed = macroSeed;
            macroNoise.size = 700f;
            macroNoise.intensity = 1f;
            macroNoise.detail = 0.55f;
            graph.Add(macroNoise);

            var rankHeightGen = (PercentileRankGenerator)Generator.Create(typeof(PercentileRankGenerator));
            graph.Add(rankHeightGen);
            graph.Link(macroNoise, rankHeightGen);

            // Percentile ranking guarantees area fractions, but it does that by locally steepening
            // the field wherever the raw noise's own value distribution is densest: "more rank
            // range packed into a narrow band of raw values" IS a steeper local gradient there, and
            // Perlin noise is densest near the middle of its range - exactly the terrain that used
            // to read as gentle rolling ground, not the guaranteed mountain fraction. Left
            // unblurred this turns the ENTIRE map jagged (every pixel-scale wiggle in the dense
            // middle of the distribution gets stretched into a real elevation swing), not just the
            // guaranteed 10% Mountain band. A moderate world-space blur afterward removes that
            // pixel-scale jaggedness while leaving the macro banding intact, since the blur radius
            // (~60m) is far smaller than a biome band's spatial scale (hundreds of metres) - it
            // smooths the noise the ranking amplified, not the banding the ranking exists to
            // guarantee.
            var rankBlur = (Blur200)Generator.Create(typeof(Blur200));
            rankBlur.blur = 60f;
            graph.Add(rankBlur);
            graph.Link((IOutlet<Den.Tools.Matrices.MatrixWorld>)rankHeightGen, (IInlet<Den.Tools.Matrices.MatrixWorld>)rankBlur);

            // Narrowed to the interface so every downstream graph.Link call below (there are many)
            // is unambiguous without needing an explicit cast each time - see the Threshold/Combine
            // doc comments for why a raw multi-interface Generator reference causes that ambiguity.
            IOutlet<Den.Tools.Matrices.MatrixWorld> rankHeight = rankBlur;

            // Rolling hills texture, added on top of the ranked elevation - period smaller than the
            // macro wave but still large enough (~220m) that only a few undulations show across the
            // tile. Kept gentle (low opacity) since a strong swing here would blur the percentile
            // bands PercentileRank just guaranteed - this is texture, not reshaping.
            var hillsNoise = (Noise200)Generator.Create(typeof(Noise200));
            hillsNoise.type = Noise200.Type.Perlin;
            hillsNoise.seed = hillsSeed;
            hillsNoise.size = 220f;
            hillsNoise.intensity = 1f;
            hillsNoise.detail = 0.6f;
            graph.Add(hillsNoise);

            var hillsBlend = Combine(graph, rankHeight, hillsNoise, Blend200.BlendAlgorithm.add, 0.04f);

            // Vertical compression for buildable plains (city sites need actually flat ground, not
            // just "technically not Mountain height" - a rank-band guarantees area, never slope).
            // Squeezes the bottom BiomeClassifier.RankGreenMaxHeight of ranked area (Swamp+Green)
            // into only BiomeClassifier.PlainsElevationCap of the vertical range, and expands
            // Highland+Mountain into the remaining, much larger, share - real hypsometric curves
            // work the same way (most land sits at modest, gently-varying elevation; a small
            // fraction of mountainous area carries most of the relief). Built the same "isolate a
            // sub-range, remap, recombine" way as the mountain terrace-swap below: greenCompressed
            // and highlandExpanded each clamp to a flat plateau (plainsElevationCap) outside their
            // own half of the range (Levels200's own clamping - see BiomeClassifier's doc comment
            // on why this needs the elevation-space constants, not the rank ones, from here on),
            // so adding them and subtracting the doubled-up plateau recovers one continuous curve.
            var greenCompressed = (Levels200)Generator.Create(typeof(Levels200));
            greenCompressed.inMin = 0f;
            greenCompressed.inMax = BiomeClassifier.RankGreenMaxHeight;
            greenCompressed.outMin = 0f;
            greenCompressed.outMax = BiomeClassifier.PlainsElevationCap;
            graph.Add(greenCompressed);
            graph.Link(hillsBlend, greenCompressed);

            var highlandExpanded = (Levels200)Generator.Create(typeof(Levels200));
            highlandExpanded.inMin = BiomeClassifier.RankGreenMaxHeight;
            highlandExpanded.inMax = 1f;
            highlandExpanded.outMin = BiomeClassifier.PlainsElevationCap;
            highlandExpanded.outMax = 1f;
            graph.Add(highlandExpanded);
            graph.Link(hillsBlend, highlandExpanded);

            var plainsCapConstant = (Constant200)Generator.Create(typeof(Constant200));
            plainsCapConstant.level = BiomeClassifier.PlainsElevationCap;
            graph.Add(plainsCapConstant);

            var elevationSum = Combine(graph, greenCompressed, highlandExpanded, Blend200.BlendAlgorithm.add);
            var flattenedHeight = Combine(graph, elevationSum, plainsCapConstant, Blend200.BlendAlgorithm.subtract);

            // Mountain terracing (WORLD_GENERATION_PIPELINE.md Iteration 01: cliffs/terraces):
            // swaps the smooth top of the compressed field for a terraced version without changing
            // WHICH fraction of the map counts as Mountain. Isolate the mountain band's own local
            // 0..1 sub-range (mountainLocal), terrace that in isolation, then subtract the smooth
            // contribution and add the terraced one back, both scaled by the band's width in the
            // outer 0..1 range - non-mountain cells are unaffected (mountainLocal reads 0 there,
            // and Terrace200 maps 0 to 0, so both scaled terms are 0 and cancel out), and mountain
            // cells swap smooth-for-stepped while staying inside their guaranteed
            // [HighlandMaxHeight, 1] slice (algebraically: flattenedHeight - mountainLocal*bandWidth
            // reduces to the constant HighlandMaxHeight for every mountain cell, since mountainLocal
            // is exactly (flattenedHeight-HighlandMaxHeight)/bandWidth there by construction - then
            // + terracedLocal*bandWidth adds the stepped version back in the same range).
            const float mountainBandWidth = 1f - BiomeClassifier.HighlandMaxHeight;

            var mountainLocal = (Levels200)Generator.Create(typeof(Levels200));
            mountainLocal.inMin = BiomeClassifier.HighlandMaxHeight;
            mountainLocal.inMax = 1f;
            mountainLocal.outMin = 0f;
            mountainLocal.outMax = 1f;
            graph.Add(mountainLocal);
            graph.Link(flattenedHeight, mountainLocal);

            var terracedLocal = (Terrace200)Generator.Create(typeof(Terrace200));
            terracedLocal.seed = terraceSeed;
            terracedLocal.num = 6;
            terracedLocal.uniformity = 0.6f;
            terracedLocal.steepness = 0.8f;
            graph.Add(terracedLocal);
            graph.Link((IOutlet<Den.Tools.Matrices.MatrixWorld>)mountainLocal, (IInlet<Den.Tools.Matrices.MatrixWorld>)terracedLocal);

            IOutlet<Den.Tools.Matrices.MatrixWorld> mountainLocalOut = mountainLocal;
            var withoutSmoothMountain = Combine(graph, flattenedHeight, mountainLocalOut, Blend200.BlendAlgorithm.subtract, mountainBandWidth);
            var terracedHeight = Combine(graph, withoutSmoothMountain, terracedLocal, Blend200.BlendAlgorithm.add, mountainBandWidth);

            // Highland terracing - the same isolate/terrace/recombine technique as the mountain
            // band above, now applied to Highland's own [GreenMaxHeight, HighlandMaxHeight) slice
            // (WORLD_GENERATION_PIPELINE.md Terrain Shaping row: "no cliff shaping independent of
            // the mountain terracing yet" - this closes that gap). Gentler params than the
            // mountain's (fewer, less uniform, less steep terraces) keep Highland reading as rocky,
            // uneven outcrops/mesas - a lesser cliff feature distinct from both the smooth green
            // plains below it and the mountain's own more dramatic steps above it, not a second
            // mountain. Safe to chain onto terracedHeight rather than flattenedHeight: outside its
            // own band, Levels200 clamps highlandLocal to a constant (0 below GreenMaxHeight, 1 at
            // or above HighlandMaxHeight - confirmed via Matrix.Terrace's source, which explicitly
            // leaves values above 0.9999 untouched and maps the exact 0 case straight through), and
            // Terrace200 preserves both those endpoints exactly, so the subtract/add pair cancels to
            // a true no-op everywhere outside the Highland band - including over cells the mountain
            // block above already terraced.
            const float highlandBandWidth = BiomeClassifier.HighlandMaxHeight - BiomeClassifier.GreenMaxHeight;

            var highlandLocal = (Levels200)Generator.Create(typeof(Levels200));
            highlandLocal.inMin = BiomeClassifier.GreenMaxHeight;
            highlandLocal.inMax = BiomeClassifier.HighlandMaxHeight;
            highlandLocal.outMin = 0f;
            highlandLocal.outMax = 1f;
            graph.Add(highlandLocal);
            graph.Link(terracedHeight, highlandLocal);

            var highlandTerracedLocal = (Terrace200)Generator.Create(typeof(Terrace200));
            highlandTerracedLocal.seed = highlandTerraceSeed;
            highlandTerracedLocal.num = 4;
            highlandTerracedLocal.uniformity = 0.4f;
            highlandTerracedLocal.steepness = 0.5f;
            graph.Add(highlandTerracedLocal);
            graph.Link((IOutlet<Den.Tools.Matrices.MatrixWorld>)highlandLocal, (IInlet<Den.Tools.Matrices.MatrixWorld>)highlandTerracedLocal);

            IOutlet<Den.Tools.Matrices.MatrixWorld> highlandLocalOut = highlandLocal;
            var withoutSmoothHighland = Combine(graph, terracedHeight, highlandLocalOut, Blend200.BlendAlgorithm.subtract, highlandBandWidth);
            var fullyTerracedHeight = Combine(graph, withoutSmoothHighland, highlandTerracedLocal, Blend200.BlendAlgorithm.add, highlandBandWidth);

            // Six guaranteed lake sites of varying size instead of a probabilistic noise
            // threshold: each is its own (Noise -> Levels cutoff) pair - same non-repeating "huge
            // period, single peak" trick as before - with a random world-space offset so the six
            // peaks land in different, spread-out spots rather than clustering, and a varied
            // size/threshold pair so they don't all read as the same lake. Combined pairwise via
            // max ("whichever site is deepest here wins") into one mask, then subtracted.
            var lakeSpecs = new (float Size, float InMin)[]
            {
                (520f, 0.55f), // large
                (430f, 0.58f), // large
                (300f, 0.60f), // medium
                (260f, 0.62f), // medium
                (160f, 0.64f), // small
                (120f, 0.66f), // small
            };

            IOutlet<Den.Tools.Matrices.MatrixWorld> lakeMask = null;
            foreach (var spec in lakeSpecs)
            {
                var noise = (Noise200)Generator.Create(typeof(Noise200));
                noise.type = Noise200.Type.Perlin;
                noise.seed = terrainSeed.Next();
                noise.size = spec.Size;
                noise.intensity = 1f;
                noise.detail = 0.35f;
                noise.offset = new Vector2D(terrainSeed.Next(-450, 450), terrainSeed.Next(-450, 450));
                graph.Add(noise);

                var cutoff = (Levels200)Generator.Create(typeof(Levels200));
                cutoff.inMin = spec.InMin;
                cutoff.inMax = 1f;
                cutoff.outMin = 0f;
                cutoff.outMax = 1f;
                graph.Add(cutoff);
                graph.Link(noise, cutoff);

                lakeMask = lakeMask == null ? (IOutlet<Den.Tools.Matrices.MatrixWorld>)cutoff : Combine(graph, lakeMask, cutoff, Blend200.BlendAlgorithm.max);
            }

            // Each of the 6 sites reaches a different peak mask value depending on its own
            // noise/threshold - higher opacity so all six reliably carve a visible dip, not just
            // the strongest ones (WaterZoneMenu's flood-fill only needs a real local minimum to
            // work with, but a dip too shallow to register at the sampling grid's resolution
            // would still fail to become a lake).
            var lakeBlend = Combine(graph, fullyTerracedHeight, lakeMask, Blend200.BlendAlgorithm.subtract, 0.7f);

            var erosion = (Erosion200)Generator.Create(typeof(Erosion200));
            graph.Add(erosion);
            graph.Link(lakeBlend, erosion);

            // Explicit small-scale feature layer (WORLD_GENERATION_PIPELINE.md Iteration 03:
            // "large/medium/small feature scales" - mountain=large ~2000m, plateau+hills=medium
            // ~600m/220m, this=small ~30m): fine surface texture so the ground doesn't read as
            // perfectly smooth noise up close. Blended in only on the branch feeding HeightOutput
            // (the rendered Terrain), never on directOutput's "height" channel below - biome
            // classification and LakeBasinFinder's local-minima flood-fill both want a smooth field,
            // and this noise's own micro-bumps would otherwise register as spurious tiny local
            // minima at the water sampling grid's resolution, cluttering lake placement with noise.
            var smallDetailNoise = (Noise200)Generator.Create(typeof(Noise200));
            smallDetailNoise.type = Noise200.Type.Perlin;
            smallDetailNoise.seed = detailSeed;
            smallDetailNoise.size = 30f;
            smallDetailNoise.intensity = 1f;
            smallDetailNoise.detail = 0.5f;
            graph.Add(smallDetailNoise);

            var visualDetailBlend = (Blend200)Generator.Create(typeof(Blend200));
            visualDetailBlend.layers[0].algorithm = Blend200.BlendAlgorithm.mix;
            visualDetailBlend.layers[0].opacity = 1f;
            visualDetailBlend.layers[1].algorithm = Blend200.BlendAlgorithm.add;
            visualDetailBlend.layers[1].opacity = 0.02f;
            graph.Add(visualDetailBlend);
            graph.Link(erosion, visualDetailBlend.layers[0].inlet);
            graph.Link(smallDetailNoise, visualDetailBlend.layers[1].inlet);

            var heightOutput = (HeightOutput200)Generator.Create(typeof(HeightOutput200));
            graph.Add(heightOutput);
            graph.Link(visualDetailBlend, heightOutput);

            // Steepness channel (WORLD_GENERATION_PIPELINE.md Iteration 04: BiomeClassifier now
            // reads slope, not just height, so a steep valley wall reads as Mountain regardless of
            // elevation). Reads from erosion - the same smooth, pre-detail-noise field the "height"
            // channel below uses - so slope reflects the actual landform, not small-scale noise
            // jitter. Default From/To/Smooth Range (30/90/30 degrees) map bare ground to ~0 and
            // near-vertical faces to ~1, which is the 0..1 range BiomeClassifier.SteepSlope expects.
            var slope = (Slope200)Generator.Create(typeof(Slope200));
            graph.Add(slope);
            graph.Link((IOutlet<Den.Tools.Matrices.MatrixWorld>)erosion, (IInlet<Den.Tools.Matrices.MatrixWorld>)slope);

            // Moisture channel: an independent noise field (not derived from height/erosion at
            // all - a real wet/dry pattern isn't a function of elevation alone) so low ground can
            // read as either Swamp (wet) or Meadow (dry) instead of always Swamp. Large period
            // (~500m) so moisture varies as broad regions, not per-cell noise.
            var moistureNoise = (Noise200)Generator.Create(typeof(Noise200));
            moistureNoise.type = Noise200.Type.Perlin;
            moistureNoise.seed = moistureSeed;
            moistureNoise.size = 500f;
            moistureNoise.intensity = 1f;
            moistureNoise.detail = 0.5f;
            graph.Add(moistureNoise);

            // Exposes height/slope/moisture as named runtime-queryable channels via
            // DirectMatricesHolder (see ASSET_LIBRARY_REFERENCE.md) so gameplay code
            // (IBiomeSampler) can sample them without touching the Terrain heightmap. "height"
            // reads straight from erosion, deliberately upstream of the small-detail noise above.
            var directOutput = (DirectMatricesOutput200)Generator.Create(typeof(DirectMatricesOutput200));
            var heightChannel = new DirectMatricesOutput200.DirectMatricesLayer { name = "height" };
            heightChannel.SetGen(directOutput);
            heightChannel.Id = Den.Tools.Id.Generate();
            var slopeChannel = new DirectMatricesOutput200.DirectMatricesLayer { name = "slope" };
            slopeChannel.SetGen(directOutput);
            slopeChannel.Id = Den.Tools.Id.Generate();
            var moistureChannel = new DirectMatricesOutput200.DirectMatricesLayer { name = "moisture" };
            moistureChannel.SetGen(directOutput);
            moistureChannel.Id = Den.Tools.Id.Generate();
            directOutput.layers = new[] { heightChannel, slopeChannel, moistureChannel };
            graph.Add(directOutput);
            graph.Link((IOutlet<Den.Tools.Matrices.MatrixWorld>)erosion, (IInlet<Den.Tools.Matrices.MatrixWorld>)heightChannel);
            graph.Link((IOutlet<Den.Tools.Matrices.MatrixWorld>)slope, (IInlet<Den.Tools.Matrices.MatrixWorld>)slopeChannel);
            graph.Link(moistureNoise, moistureChannel);

            // Terrain material painting (WORLD_GENERATION_PIPELINE.md Iteration 06): six
            // TerrainLayers (Assets/MapMagic/Demo/TerrainLayers/, ready-made grass/dirt/rock/snow
            // textures - see ASSET_LIBRARY_REFERENCE.md) painted from the same height/slope/
            // moisture fields BiomeClassifier reads, so what the player sees roughly matches what
            // the game logic thinks the ground is, not two independently tuned systems that happen
            // to agree. TexturesOutput200 blends its layer array "Photoshop style"
            // (Matrix.BlendLayers processes Length-1 down to 0): layer 0 is the background and
            // gets whatever no higher layer claims (its own inlet is ignored - MapMagic fills it to
            // 1 before blending), each higher-indexed layer claims mask*opacity of whatever budget
            // is left - so a higher-priority layer (e.g. Mountain rock) automatically wins any
            // overlap with a lower one (e.g. Highland gravel) without needing to explicitly
            // subtract the overlap from the lower layer's own mask; the leftover-budget stacking
            // handles it. Thresholds ramp softly over bandBlend instead of a hard step, so texture
            // transitions don't read as a harsh line.
            const float bandBlend = 0.03f;
            // Top ~3% of area (by rank) within the Mountain band, re-expressed relative to
            // HighlandMaxHeight so it stays correct if the compression constants above change:
            // 70% of the way from the band's start to the peak, in elevation terms, is the same
            // position as the top 3% of area in rank terms, since the mountain band's own
            // elevation-to-rank relationship stays linear even after the plains compression below
            // (only the Green/Swamp portion gets compressed).
            const float snowBandFraction = 0.7f;
            const float snowHeight = BiomeClassifier.HighlandMaxHeight + snowBandFraction * (1f - BiomeClassifier.HighlandMaxHeight);

            var wetMask = Threshold(graph, moistureNoise, BiomeClassifier.WetMoisture, bandBlend, rising: true);
            var belowLowerGreenMask = Threshold(graph, erosion, BiomeClassifier.LowerGreenMaxHeight, bandBlend, rising: false);
            var dirtMask = Combine(graph, wetMask, belowLowerGreenMask, Blend200.BlendAlgorithm.multiply);

            var aboveLowerGreenMask = Threshold(graph, erosion, BiomeClassifier.LowerGreenMaxHeight, bandBlend, rising: true);
            var belowGreenMask = Threshold(graph, erosion, BiomeClassifier.GreenMaxHeight, bandBlend, rising: false);
            var forestBandMask = Combine(graph, aboveLowerGreenMask, belowGreenMask, Blend200.BlendAlgorithm.multiply);

            var aboveGreenMask = Threshold(graph, erosion, BiomeClassifier.GreenMaxHeight, bandBlend, rising: true);
            var belowHighlandMask = Threshold(graph, erosion, BiomeClassifier.HighlandMaxHeight, bandBlend, rising: false);
            var highlandBandMask = Combine(graph, aboveGreenMask, belowHighlandMask, Blend200.BlendAlgorithm.multiply);

            var highMask = Threshold(graph, erosion, BiomeClassifier.HighlandMaxHeight, bandBlend, rising: true);
            var steepMask = Threshold(graph, slope, BiomeClassifier.SteepSlope, bandBlend, rising: true);
            // Mirrors BiomeClassifier: slope alone only counts as Mountain once at least in the
            // highland band (>= GreenMaxHeight, reusing aboveGreenMask from the highland mask
            // above), so a lake pit's steep rim carved into flat lowlands doesn't get painted as
            // bare rock - only genuinely elevated steep ground does. Placing this layer above
            // Highland's in the array means Mountain automatically wins any overlap (see the type
            // doc above), so highlandBandMask doesn't need its own explicit "not steep" subtraction.
            var steepAndElevatedMask = Combine(graph, steepMask, aboveGreenMask, Blend200.BlendAlgorithm.multiply);
            var mountainMask = Combine(graph, highMask, steepAndElevatedMask, Blend200.BlendAlgorithm.max);

            var snowMask = Threshold(graph, erosion, snowHeight, bandBlend, rising: true);

            var texturesOutput = (TexturesOutput200)Generator.Create(typeof(TexturesOutput200));
            // Source tileSize values (GrassGreen=30, GrassYellow=15, Dirt=15, Gravel=4,
            // CliffBright=20, Snow=50) were authored for MapMagic's small demo scene and alias
            // badly at our terrain scale - rescaled uniformly here (see LoadScaledTerrainLayer).
            var grassLayer = MakeTextureLayer(texturesOutput, "Assets/MapMagic/Demo/TerrainLayers/GrassGreen.terrainlayer", 45f);
            var dirtLayer = MakeTextureLayer(texturesOutput, "Assets/MapMagic/Demo/TerrainLayers/Dirt.terrainlayer", 45f);
            var forestLayer = MakeTextureLayer(texturesOutput, "Assets/MapMagic/Demo/TerrainLayers/GrassYellow.terrainlayer", 45f);
            var highlandLayer = MakeTextureLayer(texturesOutput, "Assets/MapMagic/Demo/TerrainLayers/Gravel.terrainlayer", 60f);
            var rockLayer = MakeTextureLayer(texturesOutput, "Assets/MapMagic/Demo/TerrainLayers/CliffBright.terrainlayer", 60f);
            var snowLayer = MakeTextureLayer(texturesOutput, "Assets/MapMagic/Demo/TerrainLayers/Snow.terrainlayer", 60f);
            // Bottom-to-top priority order (see BlendLayers note above): grass is the fallback
            // background, then wet-low dirt, the forest height band, highland gravel, mountain
            // rock, and finally snow last so it wins the peak over bare rock instead of being
            // buried beneath it.
            texturesOutput.layers = new[] { grassLayer, dirtLayer, forestLayer, highlandLayer, rockLayer, snowLayer };
            graph.Add(texturesOutput);
            graph.Link(dirtMask, dirtLayer);
            graph.Link(forestBandMask, forestLayer);
            graph.Link(highlandBandMask, highlandLayer);
            graph.Link(mountainMask, rockLayer);
            graph.Link(snowMask, snowLayer);

            if (AssetDatabase.LoadAssetAtPath<Graph>(GraphPath) != null)
            {
                AssetDatabase.DeleteAsset(GraphPath);
            }

            AssetDatabase.CreateAsset(graph, GraphPath);
            AssetDatabase.SaveAssets();
            return graph;
        }

        /// <summary>
        /// Levels200 threshold on <paramref name="source"/> around <paramref name="center"/>,
        /// ramping across +/-<paramref name="margin"/> - <paramref name="rising"/> true reads as
        /// "above center", false as "below center". Declared return type is the interface, not the
        /// concrete Levels200, so callers keep a single-interface reference; passing a raw
        /// Levels200/Blend200/Terrace200/Erosion200 result into another graph.Link call is what
        /// makes Graph.Link's two overloads ambiguous elsewhere in this file (both interfaces
        /// satisfied at once) - narrowing the static type here avoids needing an explicit cast at
        /// every call site downstream.
        /// </summary>
        private static IOutlet<Den.Tools.Matrices.MatrixWorld> Threshold(
            Graph graph, IOutlet<Den.Tools.Matrices.MatrixWorld> source, float center, float margin, bool rising)
        {
            var levels = (Levels200)Generator.Create(typeof(Levels200));
            levels.inMin = center - margin;
            levels.inMax = center + margin;
            levels.outMin = rising ? 0f : 1f;
            levels.outMax = rising ? 1f : 0f;
            graph.Add(levels);
            graph.Link(source, levels);
            return levels;
        }

        /// <summary>Combines two 0..1 masks via Blend200 (e.g. multiply for AND, max for OR),
        /// with an optional opacity scale on the second layer (e.g. subtract/add a fraction of it
        /// rather than the full value).</summary>
        private static IOutlet<Den.Tools.Matrices.MatrixWorld> Combine(
            Graph graph, IOutlet<Den.Tools.Matrices.MatrixWorld> a, IOutlet<Den.Tools.Matrices.MatrixWorld> b,
            Blend200.BlendAlgorithm algorithm, float opacity = 1f)
        {
            var blend = (Blend200)Generator.Create(typeof(Blend200));
            blend.layers[0].algorithm = Blend200.BlendAlgorithm.mix;
            blend.layers[0].opacity = 1f;
            blend.layers[1].algorithm = algorithm;
            blend.layers[1].opacity = opacity;
            graph.Add(blend);
            graph.Link(a, blend.layers[0].inlet);
            graph.Link(b, blend.layers[1].inlet);
            return blend;
        }

        private const string ScaledTerrainLayerFolder = "Assets/Settings/TerrainLayers";

        private static TexturesOutput200.TextureLayer MakeTextureLayer(TexturesOutput200 output, string terrainLayerPath, float tileSizeMeters)
        {
            var prototype = LoadScaledTerrainLayer(terrainLayerPath, tileSizeMeters);
            if (prototype == null)
            {
                Debug.LogWarning($"[Overworld] TerrainLayer not found at '{terrainLayerPath}' - that texture slot will render blank.");
            }

            var layer = new TexturesOutput200.TextureLayer
            {
                name = System.IO.Path.GetFileNameWithoutExtension(terrainLayerPath),
                prototype = prototype,
            };
            layer.SetGen(output);
            layer.Id = Den.Tools.Id.Generate();
            return layer;
        }

        // MapMagic's Demo TerrainLayers were authored for a small demo scene (tileSize as low as
        // 4m for Gravel) - at our ~1400m tile size that repeats hundreds of times across the
        // terrain and shows up as a "TV static" aliasing artifact on mountain slopes. Rather than
        // mutating those shared third-party assets in place (which AssetDatabase.LoadAssetAtPath
        // would do, since it returns a reference to the real asset), keep a duplicate per source
        // layer under ScaledTerrainLayerFolder with just the tileSize adjusted.
        private static TerrainLayer LoadScaledTerrainLayer(string sourcePath, float tileSizeMeters)
        {
            var sourceLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(sourcePath);
            if (sourceLayer == null)
            {
                return null;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            if (!AssetDatabase.IsValidFolder(ScaledTerrainLayerFolder))
            {
                AssetDatabase.CreateFolder("Assets/Settings", "TerrainLayers");
            }

            var scaledName = System.IO.Path.GetFileNameWithoutExtension(sourcePath) + "_Scaled";
            var scaledPath = $"{ScaledTerrainLayerFolder}/{scaledName}.terrainlayer";

            var scaled = AssetDatabase.LoadAssetAtPath<TerrainLayer>(scaledPath);
            if (scaled == null)
            {
                scaled = Object.Instantiate(sourceLayer);
                AssetDatabase.CreateAsset(scaled, scaledPath);
            }

            if (!Mathf.Approximately(scaled.tileSize.x, tileSizeMeters) || !Mathf.Approximately(scaled.tileSize.y, tileSizeMeters))
            {
                scaled.tileSize = new Vector2(tileSizeMeters, tileSizeMeters);
                EditorUtility.SetDirty(scaled);
                AssetDatabase.SaveAssets();
            }

            return scaled;
        }

        private static void EnsureSceneActive()
        {
            EnsureSceneFolder();
            var active = SceneManager.GetActiveScene();
            if (active.path == ScenePath)
            {
                return;
            }

            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                return;
            }

            CreateFreshSceneAndGenerate(DefaultSeed);
        }

        private static void SaveActive()
        {
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            if (string.IsNullOrEmpty(scene.path))
            {
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            else
            {
                EditorSceneManager.SaveScene(scene);
            }

            AddToBuildSettings(ScenePath);
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
        }

        private static void EnsureSettingsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }
        }

        private static void AddToBuildSettings(string path)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].path == path)
                {
                    list[i] = new EditorBuildSettingsScene(path, true);
                    EditorBuildSettings.scenes = list.ToArray();
                    return;
                }
            }

            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
