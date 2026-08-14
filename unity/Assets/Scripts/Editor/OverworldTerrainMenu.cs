#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Den.Tools;
using GameDev.World;
using GameDev.World.Seed;
using GameDev.World.Terrain;
using MapMagic.Core;
using MapMagic.Nodes;
using MapMagic.Nodes.MatrixGenerators;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WorldSeed = GameDev.World.Seed.WorldSeed;

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
        public const int DefaultSeed = 110201;

        /// <summary>Seed of the last Generate call, so other tools (vegetation scatter) fork the same WorldSeed.</summary>
        public static int LastSeed { get; private set; }

        /// <summary>Active generation settings (World Generator window / pipeline).</summary>
        public static WorldGenSettings ActiveSettings { get; set; } = WorldGenSettings.Default;

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
            OverworldPipeline.ClearAll();
        }

        /// <summary>Clear MapMagic graph only (caller owns downstream clears).</summary>
        public static void ClearTerrainOnly()
        {
            var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
            if (mapMagic == null)
            {
                return;
            }

            mapMagic.graph = null;
            mapMagic.Refresh(true);
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

        /// <summary>
        /// This lab scene never had a Camera at all (EmptyScene + generation code only ever added a
        /// light) - fine for Scene-View-only review, but it means Game View (and Play mode) always
        /// shows black, which came up specifically while debugging why a water material looked wrong
        /// - Scene View has its own "Scene Lighting"/"Effects" display toggles independent of the
        /// actual scene, so it's not a reliable way to judge final rendering, and there was no Game
        /// View fallback to cross-check against. A straight-down review camera high enough to frame
        /// the whole 1400x1400 tile is a reasonable default for a generation lab scene regardless of
        /// this specific incident. Only added if the scene doesn't already have a camera (from a
        /// previous run, or one the user added themselves) - never overwrites/duplicates.
        /// </summary>
        private static void EnsureReviewCamera()
        {
            if (Object.FindFirstObjectByType<Camera>() != null)
            {
                return;
            }

            var cameraObject = new GameObject("Review Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.farClipPlane = 3000f;

            // Tile spans world (0,0) to (tileSize,tileSize) - centred, high enough above the
            // tallest plausible terrain (globals.height, ~200-250m in practice) to frame the full
            // 1400m width straight down at the default ~60 degree field of view.
            cameraObject.transform.position = new Vector3(700f, 1600f, 700f);
            cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private static void GenerateInternal(int seed)
        {
            // Standalone terrain menu: surface only (no lakes). Use Part-Gen/Lakes or Generate All for water.
            GenerateSurface(seed, TerrainBakeFlags.Full);
        }

        /// <summary>
        /// Height graph bake. Cascade-clears lakes/POI/roads/veg — never leaves stale water on a new heightmap.
        /// Does not place lakes (use Lakes pipeline step).
        /// </summary>
        public static void GenerateSurface(int seed, TerrainBakeFlags flags)
        {
            LastSeed = seed;
            LastBakeFlags = flags;
            var total = OverworldGenTiming.Start();

            EnsureReviewCamera();

            WaterZoneMenu.ClearWaterZones();
            WaterZoneMenu.ClearRiverZones();
            VegetationScatterMenu.ClearVegetation();
            GrassBakeMenu.Clear();
            PoiMarkerMenu.ClearMarkers();
            RoadNetworkMenu.ClearRoadNetwork();
            SettlementOverworldMenu.ClearSettlement();
            OverworldPlayerMenu.ClearPlayer();

            Graph graph = null;
            OverworldGenTiming.Time("terrain.graph", () => { graph = BuildGraph(seed, flags); });

            var mapMagic = FindOrCreateMapMagicObject();
            mapMagic.tileSize = new Vector2D(
                GameDev.World.WorldScale.OverworldTileMeters,
                GameDev.World.WorldScale.OverworldTileMeters);
            mapMagic.graph = graph;
            mapMagic.gameObject.SetActive(true);

            OverworldGenTiming.Time("terrain.mapmagic-refresh", () =>
            {
                mapMagic.Refresh(true);
                mapMagic.tiles.Pin(new Coord(0, 0), false, mapMagic);
            });

            SaveActive();
            Selection.activeGameObject = mapMagic.gameObject;
            OverworldGenTiming.Log(
                "terrain.surface",
                OverworldGenTiming.StopMs(total),
                $"seed={seed} flags={flags}");
        }

        /// <summary>Last bake flags (Part-Gen Mountains/Zones merge against this).</summary>
        public static TerrainBakeFlags LastBakeFlags { get; private set; } = TerrainBakeFlags.Full;

        /// <summary>
        /// Legacy alias: surface with full flags only (no lake plan). Prefer
        /// <see cref="GenerateSurface"/> + Lakes step.
        /// </summary>
        public static void GenerateTerrainAndLakePlan(int seed)
        {
            GenerateSurface(seed, TerrainBakeFlags.Full);
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
        /// the percentile by construction.
        /// <para>
        /// Before ranking, a Voronoi "cellular" (F2-F1) ridge field is added on top of the macro
        /// noise, masked to the macro field's own high range so the ridge network only shows up
        /// where the map was already going to be highland - a connected range with foothills
        /// tapering into plains, not isolated peaks or a mountain that reads as a shapeless blob.
        /// Ranking still sees one combined field (ridges included), so the area-fraction guarantee
        /// holds regardless of whether the input is plain noise or noise-plus-ridges - splitting
        /// ridges/macro into separately-ranked fields would defeat that guarantee. The mountain and
        /// highland bands are then each terraced in isolation and swapped back in without changing
        /// which fraction of the map counts as Mountain/Highland (see the terracing blocks below).
        /// </para>
        /// <para>
        /// Basin carving IS back (incident: removing it entirely, on the theory that ridge structure
        /// alone would produce enough closed basins, was wrong - ridges only shape the highland/
        /// mountain band; the compressed plains band (<see cref="BiomeClassifier.PlainsElevationCap"/>)
        /// is deliberately near-flat for buildability and has no relief of its own to form a real
        /// depression). Five deliberate basin sites, masked to the plains band only and carved deep
        /// enough to guarantee a real rim, replace the six-pit system this originally had - see the
        /// basinSpecs block below for the fixes over that original version (plains-only masking,
        /// full-band-width carve depth). <see cref="GameDev.EditorTools.WaterZoneMenu"/> still finds
        /// the actual basins by flood-filling local minima in the generated height (see
        /// <see cref="GameDev.World.Water.LakeBasinFinder"/>, unchanged), and now also traces rivers
        /// into them from flow accumulation over that same height field (see
        /// <see cref="GameDev.World.Water.FlowNetwork"/>, <see cref="GameDev.World.Water.RiverTracer"/>).
        /// Recipe: MacroNoise -> (+masked ridge) -> PercentileRank -> (+HillsNoise) -> [mountain/
        /// highland terrace swaps] -> (-masked basins) -> Erosion -> Height Output. Seeded from
        /// <see cref="WorldSeed"/>, one independently-named fork per concern (terrain.macro,
        /// terrain.ridge, terrain.hills, terrain.terrace, terrain.highland-terrace, terrain.detail,
        /// terrain.basins, terrain.graph), so inserting or removing a layer here can no longer
        /// silently shift another layer's seed the way deriving them all from one shared fork via
        /// sequential calls used to.
        /// </para>
        /// </summary>
        private static Graph BuildGraph(int seed, TerrainBakeFlags flags = TerrainBakeFlags.Full)
        {
            EnsureSettingsFolder();

            var includeMountains = (flags & TerrainBakeFlags.IncludeMountains) != 0;
            var includeZones = (flags & TerrainBakeFlags.IncludeZoneTextures) != 0;

            // Each terrain concern gets its own named fork instead of sequential .Next() calls off
            // one shared "terrain" stream (WORLD_GENERATION_PIPELINE.md's own "Known gap" note this
            // file used to carry): inserting/removing a layer here can no longer silently reshuffle
            // every other layer's seed. "biome" (moisture) stays its own independent top-level
            // fork, unchanged. Skipped mountain layers still must NOT consume other forks.
            var graphSeed = new WorldSeed(seed).Fork("terrain.graph").Next();
            var macroSeed = new WorldSeed(seed).Fork("terrain.macro").Next();
            var hillsSeed = new WorldSeed(seed).Fork("terrain.hills").Next();
            var detailSeed = new WorldSeed(seed).Fork("terrain.detail").Next();
            var basinSeed = new WorldSeed(seed).Fork("terrain.basins");
            var moistureSeed = new WorldSeed(seed).Fork("biome").Next();
            // Mountain-only forks: only read when IncludeMountains so skip does not change other streams.
            var ridgeSeed = 0;
            var terraceSeed = 0;
            var highlandTerraceSeed = 0;
            if (includeMountains)
            {
                ridgeSeed = new WorldSeed(seed).Fork("terrain.ridge").Next();
                terraceSeed = new WorldSeed(seed).Fork("terrain.terrace").Next();
                highlandTerraceSeed = new WorldSeed(seed).Fork("terrain.highland-terrace").Next();
            }

            var graph = Graph.Create();
            graph.random = new Noise(graphSeed, 32768);

            // One macro noise field for the whole map's elevation variety - period large enough
            // (~700m) relative to the tile that a few natural undulations show without repeating,
            // detailed enough (0.55) that it's not a single smooth blob before PercentileRank
            // reshapes its distribution.
            var macroNoise = (Noise200)Generator.Create(typeof(Noise200));
            macroNoise.type = Noise200.Type.Perlin;
            macroNoise.seed = macroSeed;
            macroNoise.size = 950f;
            macroNoise.intensity = 1f;
            macroNoise.detail = 0.5f;
            graph.Add(macroNoise);

            // Two seeded mountain *masses* in opposite strata (jittered). Skipped when
            // TerrainBakeFlags.IncludeMountains is off — fork is not consumed.
            IOutlet<Den.Tools.Matrices.MatrixWorld> rankedInput = macroNoise;
            if (includeMountains)
            {
            var massSeed = new WorldSeed(seed).Fork("terrain.masses");
            IOutlet<Den.Tools.Matrices.MatrixWorld> mountainMasses = null;
            for (var mi = 0; mi < 2; mi++)
            {
                var massNoise = (Noise200)Generator.Create(typeof(Noise200));
                massNoise.type = Noise200.Type.Perlin;
                massNoise.seed = massSeed.Next();
                massNoise.size = 480f;
                massNoise.intensity = 1f;
                massNoise.detail = 0.35f;
                // Push peaks toward opposite halves of the tile (noise-space offset ≈ world bias).
                var ox = mi == 0
                    ? massSeed.Next(-520, -120)
                    : massSeed.Next(120, 520);
                var oz = mi == 0
                    ? massSeed.Next(-520, -80)
                    : massSeed.Next(80, 520);
                massNoise.offset = new Vector2D(ox, oz);
                graph.Add(massNoise);

                var massCutoff = (Levels200)Generator.Create(typeof(Levels200));
                massCutoff.inMin = 0.58f;
                massCutoff.inMax = 0.92f;
                massCutoff.outMin = 0f;
                massCutoff.outMax = 1f;
                graph.Add(massCutoff);
                graph.Link(massNoise, massCutoff);

                mountainMasses = mountainMasses == null
                    ? (IOutlet<Den.Tools.Matrices.MatrixWorld>)massCutoff
                    : Combine(graph, mountainMasses, massCutoff, Blend200.BlendAlgorithm.max);
            }

            var macroWithMasses = Combine(graph, macroNoise, mountainMasses, Blend200.BlendAlgorithm.add, 0.55f);

            // Ridge structure: Voronoi's "cellular" blend is F2-F1 (distance to the second-closest
            // cell point minus distance to the closest one), the standard ridge-noise trick - it
            // reads as a connected network of ridgelines along cell boundaries, not isolated peaks,
            // because every ridge is shared between two neighbouring cells the way a real drainage
            // divide is shared between two neighbouring valleys. cellSize ~380m on a 1400m tile
            // keeps a handful of massifs; combined with low add-opacity below so ridges sculpt a
            // mountain *mass* instead of a wall that bisects the tile left/right.
            var ridgeNoise = (Voronoi200)Generator.Create(typeof(Voronoi200));
            ridgeNoise.seed = ridgeSeed;
            ridgeNoise.cellSize = 380;
            ridgeNoise.uniformity = 0.55f;
            ridgeNoise.blendType = Voronoi200.BlendType.cellular;
            ridgeNoise.intensity = 1f;
            graph.Add(ridgeNoise);

            // Ridges only matter where macroNoise's own raw field already reads high - the same
            // region that ranking will later turn into the Highland/Mountain bands. Elsewhere
            // (the map's plains-and-hills majority) the mask fades to zero, so the range reads as
            // one coherent highland mass with foothills tapering into plains, not mountains
            // scattered across the whole tile. Levels200 clamps outside its own inMin/inMax the
            // same way the Threshold helper below relies on, so this is a soft-edged region mask,
            // not a hard cutoff.
            // <para>
            // Incident: the first attempt at this (inMin 0.45) started well BELOW Perlin's own
            // ~0.5 mean, so roughly half the map's raw macroNoise values picked up some ridge
            // contribution - not just the top ~20% that ranking turns into Highland/Mountain, but a
            // huge swath of what ranking turns into the compressed "plains" band too. PercentileRank
            // only cares about relative order, not magnitude, but that broad low-amplitude ridge
            // bleed still reshuffled the FINE relief within the plains band enough to give
            // LakeBasinFinder real (if shallow) local minima where the pure-noise field had none at
            // all - and once that band has minima, its extreme flatness meant nothing bounded the
            // flood (see the WaterZoneMenu.MaxLakeDepthMeters/CellCountSafetyCap incident notes,
            // which fought the symptom for two rounds before this got fixed at the source). Raised
            // well above the raw field's own mean so ridge contribution concentrates on genuinely
            // elevated territory only - the plains stay close to their original, ridge-free flatness.
            // </para>
            var ridgeRegionMask = (Levels200)Generator.Create(typeof(Levels200));
            ridgeRegionMask.inMin = 0.68f;
            ridgeRegionMask.inMax = 0.88f;
            ridgeRegionMask.outMin = 0f;
            ridgeRegionMask.outMax = 1f;
            graph.Add(ridgeRegionMask);
            graph.Link(macroWithMasses, ridgeRegionMask);

            var maskedRidge = Combine(graph, ridgeNoise, ridgeRegionMask, Blend200.BlendAlgorithm.multiply);

            // Soften cellular walls into massifs before ranking — sharp F2-F1 edges otherwise
            // survive as a continuous barrier across the tile.
            var ridgeBlur = (Blur200)Generator.Create(typeof(Blur200));
            ridgeBlur.blur = 90f;
            graph.Add(ridgeBlur);
            graph.Link((IOutlet<Den.Tools.Matrices.MatrixWorld>)maskedRidge, (IInlet<Den.Tools.Matrices.MatrixWorld>)ridgeBlur);
            IOutlet<Den.Tools.Matrices.MatrixWorld> softRidge = ridgeBlur;

            // Ridge contribution on top of the dual-mass macro field. Opacity moderate so masses
            // stay readable as separate peaks rather than one bisecting wall.
            var macroWithRidges = Combine(graph, macroWithMasses, softRidge, Blend200.BlendAlgorithm.add, 0.38f);
            rankedInput = macroWithRidges;
            } // includeMountains

            var rankHeightGen = (PercentileRankGenerator)Generator.Create(typeof(PercentileRankGenerator));
            graph.Add(rankHeightGen);
            graph.Link(rankedInput, rankHeightGen);

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

            IOutlet<Den.Tools.Matrices.MatrixWorld> fullyTerracedHeight = flattenedHeight;
            if (includeMountains)
            {
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
            fullyTerracedHeight = Combine(graph, withoutSmoothHighland, highlandTerracedLocal, Blend200.BlendAlgorithm.add, highlandBandWidth);
            } // includeMountains

            // Basin carving (incident: removing this entirely was the wrong call - see
            // WORLD_GENERATION_PIPELINE.md's Hydrology row for the full story). Ridge structure
            // gives the highland/mountain band real relief on its own, but the compressed plains
            // band (PlainsElevationCap) is DELIBERATELY near-flat for buildability and has no
            // mechanism of its own to produce a genuine depression - leaving basin carving out
            // entirely let LakeBasinFinder's flood run across the plains' leftover noise-scale
            // ripples with no real rim to stop at, rendering as a "lake" with almost no depth under
            // it (a flat quad sitting a few centimetres over real ground, with the flood's arbitrary
            // cutoff cap producing a jarring cliff-walled edge instead of a natural shoreline).
            // <para>
            // Five deliberate basin sites - same non-repeating "huge period noise, single peak
            // threshold" trick as the old lake-pit system this replaces, combined via max ("whichever
            // site is deepest here wins") - but two real fixes over that old version: masked to the
            // plains band ONLY (via plainsBandMask below) so a site can never bite into the
            // highland/mountain terracing, and subtracted with opacity equal to the WHOLE
            // PlainsElevationCap band width, not a fraction of it - a pit's strongest point can reach
            // 0 (real sea level) even though the surrounding plains sit as high as
            // PlainsElevationCap*globals.height, guaranteeing LakeBasinFinder a real, generous rim
            // instead of a few metres of noise.
            // <para>
            // Incident within this fix: the first attempt at plainsBandMask used a symmetric ramp
            // spanning the ENTIRE plains band (center = half of PlainsElevationCap, margin = the
            // same) - meaning a basin site centred on merely-average plains ground (not the very
            // lowest point in the whole band) already had its carve strength cut roughly in half or
            // more before the noise cutoff even applied, since basin site positions are random and
            // uncorrelated with where the terrain happens to already be lowest. Only sites that
            // happened to land on the very bottom of the band carved anything deep - most of the 5
            // sites ended up too shallow to register as a lake at all ("почти нет воды"). The mask's
            // actual job is narrower than that: stop a site from biting into highland, nothing more -
            // it should read ~1 across nearly the whole plains band and only fall off right at the
            // highland boundary. Narrowed to a ramp over just the top 15% of the band.
            // </para>
            var plainsBandMask = Threshold(
                graph, fullyTerracedHeight,
                BiomeClassifier.PlainsElevationCap * 0.925f, BiomeClassifier.PlainsElevationCap * 0.075f,
                rising: false);

            // Incident: raising the cutoff threshold (0.55-0.66 -> 0.68-0.77) to shrink the final
            // flooded footprint didn't work, twice - confirmed with WaterZoneMenu's
            // CellCountSafetyCap actually enforcing its new, lower value (1500) on a fresh compile,
            // 7 of 8 zones still landed exactly on that cap. The threshold controls how selectively a
            // noise field's OWN peak gets isolated, but a Perlin field's spatial features are still
            // fundamentally sized by its period regardless of where you cut it - a 320m-period field
            // cut at 0.55 or at 0.77 both isolate a peak region on the order of hundreds of metres
            // across, just with a steeper or gentler edge, not a smaller one. Size (period), not
            // threshold, is what actually controls final footprint. Dropped well below the old lake-
            // pit system's periods (150-480m) to something pond/small-lake scaled (80-200m) -
            // thresholds relaxed back down since a smaller period no longer needs an aggressive
            // cutoff to stay a distinct, modest feature.
            // Incident: shrinking the period fixed HALF the zones (932/430/267/992 cells - genuinely
            // pond/small-lake sized) but the other half still hit the safety cap - candidate minima
            // jumped 17-27 -> 120 (a shorter period naturally has more peaks per unit area), and
            // enough of those peaks now sit close enough together that their floods bridge into one
            // merged basin before either finds a real rim. Density, not size, is the remaining lever:
            // thresholds raised again (this time to cut how much total area qualifies at all, not to
            // shrink one basin's own footprint, which round seven showed doesn't work) so fewer,
            // more separated peaks get any carve at all.
            var basinSpecs = new (float Size, float InMin)[]
            {
                (180f, 0.66f), // medium lake
                (140f, 0.68f), // medium-small
                (110f, 0.70f), // small lake
                (90f, 0.72f),  // small lake B
                (70f, 0.74f),  // pond
                (55f, 0.76f),  // tiny pond
                (45f, 0.78f),  // tiny pond B
                (40f, 0.80f),  // micro pond — sparse-seed insurance
            };

            IOutlet<Den.Tools.Matrices.MatrixWorld> basinMask = null;
            foreach (var spec in basinSpecs)
            {
                var basinNoise = (Noise200)Generator.Create(typeof(Noise200));
                basinNoise.type = Noise200.Type.Perlin;
                basinNoise.seed = basinSeed.Next();
                basinNoise.size = spec.Size;
                basinNoise.intensity = 1f;
                basinNoise.detail = 0.35f;
                basinNoise.offset = new Vector2D(basinSeed.Next(-450, 450), basinSeed.Next(-450, 450));
                graph.Add(basinNoise);

                var basinCutoff = (Levels200)Generator.Create(typeof(Levels200));
                basinCutoff.inMin = spec.InMin;
                basinCutoff.inMax = 1f;
                basinCutoff.outMin = 0f;
                basinCutoff.outMax = 1f;
                graph.Add(basinCutoff);
                graph.Link(basinNoise, basinCutoff);

                basinMask = basinMask == null
                    ? (IOutlet<Den.Tools.Matrices.MatrixWorld>)basinCutoff
                    : Combine(graph, basinMask, basinCutoff, Blend200.BlendAlgorithm.max);
            }

            var maskedBasins = Combine(graph, basinMask, plainsBandMask, Blend200.BlendAlgorithm.multiply);
            // MapMagic basin subtract OFF — those pits became dry "mini-ruts" across the plains
            // because DistributedLakePlacer + LakeTerrainCarver own the real lakes now. Keep the
            // generators in the graph for determinism/history, but do not carve the height field.
            _ = maskedBasins;
            var erosion = (Erosion200)Generator.Create(typeof(Erosion200));
            // Left at MapMagic's own stock defaults (iterations 3, durability 0.9, sediment 0.75,
            // fluidity 3) after an incident chain: pushing this well past stock (6 iterations,
            // durability 0.85, sediment 0.6, fluidity 4) manufactured spurious tiny local minima
            // across the deliberately-flat plains band on its own; dialing back helped but didn't
            // fix the underlying issue, because the real cause was upstream (the ridge mask bleeding
            // into the plains, then - after that was fixed - the complete absence of any basin-
            // carving mechanism at all, see basinSpecs above). Now lakes come from the placer/carver
            // path, so erosion only softens ridge terracing — no MapMagic pit subtract underneath.
            erosion.iterations = 3;
            erosion.terrainDurability = 0.9f;
            erosion.erosionAmount = 1f;
            erosion.sedimentAmount = 0.75f;
            erosion.fluidityIterations = 3;
            graph.Add(erosion);
            graph.Link(fullyTerracedHeight, erosion);

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

            if (includeZones)
            {
            // Terrain material painting (WORLD_GENERATION_PIPELINE.md Iteration 06): six
            // TerrainLayers painted from the same height/slope/moisture fields BiomeClassifier reads.
            const float bandBlend = 0.03f;
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
            var steepAndElevatedMask = Combine(graph, steepMask, aboveGreenMask, Blend200.BlendAlgorithm.multiply);
            var mountainMask = Combine(graph, highMask, steepAndElevatedMask, Blend200.BlendAlgorithm.max);

            var snowMask = Threshold(graph, erosion, snowHeight, bandBlend, rising: true);

            var texturesOutput = (TexturesOutput200)Generator.Create(typeof(TexturesOutput200));
            var grassLayer = MakeTextureLayer(
                texturesOutput,
                "Assets/MapMagic/Demo/TerrainLayers/GrassYellow.terrainlayer",
                TerrainBiomePalette.AlbedoTileMeters,
                "Grass_Fields",
                TerrainBiomePalette.Fields);
            var dirtLayer = MakeTextureLayer(
                texturesOutput,
                "Assets/MapMagic/Demo/TerrainLayers/Dirt.terrainlayer",
                TerrainBiomePalette.AlbedoTileMeters,
                "Dirt_Swamp",
                TerrainBiomePalette.Swamp);
            var forestLayer = MakeTextureLayer(
                texturesOutput,
                "Assets/MapMagic/Demo/TerrainLayers/GrassGreen.terrainlayer",
                TerrainBiomePalette.AlbedoTileMeters,
                "Grass_Forest",
                TerrainBiomePalette.Forest);
            var highlandLayer = MakeTextureLayer(
                texturesOutput,
                "Assets/MapMagic/Demo/TerrainLayers/Gravel.terrainlayer",
                TerrainBiomePalette.HighlandTileMeters,
                "Gravel_Highland",
                TerrainBiomePalette.Highland);
            var rockLayer = MakeTextureLayer(
                texturesOutput,
                "Assets/MapMagic/Demo/TerrainLayers/CliffBright.terrainlayer",
                TerrainBiomePalette.RockTileMeters,
                "Cliff_Rock",
                TerrainBiomePalette.Rock);
            var snowLayer = MakeTextureLayer(
                texturesOutput,
                "Assets/MapMagic/Demo/TerrainLayers/Snow.terrainlayer",
                TerrainBiomePalette.SnowTileMeters,
                "Snow_Muted",
                TerrainBiomePalette.Snow);

            texturesOutput.layers = new[]
            {
                grassLayer, dirtLayer, forestLayer, highlandLayer, rockLayer, snowLayer
            };
            graph.Add(texturesOutput);
            graph.Link(dirtMask, dirtLayer);
            graph.Link(forestBandMask, forestLayer);
            graph.Link(highlandBandMask, highlandLayer);
            graph.Link(mountainMask, rockLayer);
            graph.Link(snowMask, snowLayer);
            } // includeZones

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

        private static TexturesOutput200.TextureLayer MakeTextureLayer(
            TexturesOutput200 output,
            string terrainLayerPath,
            float tileSizeMeters,
            string scaledAssetName,
            TerrainBiomePalette.Look look)
        {
            var prototype = LoadScaledTerrainLayer(
                terrainLayerPath, tileSizeMeters, scaledAssetName, look);

            var layer = new TexturesOutput200.TextureLayer
            {
                name = scaledAssetName,
                prototype = prototype,
            };
            layer.SetGen(output);
            layer.Id = Den.Tools.Id.Generate();
            return layer;
        }

        // Duplicate MapMagic demo layers under Settings/TerrainLayers with muted remaps +
        // memo tile sizes. Never mutate the shared third-party demo assets in place.
        private static TerrainLayer LoadScaledTerrainLayer(
            string sourcePath,
            float tileSizeMeters,
            string scaledAssetName,
            TerrainBiomePalette.Look look)
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

            var scaledPath = $"{ScaledTerrainLayerFolder}/{scaledAssetName}.terrainlayer";
            var scaled = AssetDatabase.LoadAssetAtPath<TerrainLayer>(scaledPath);
            if (scaled == null)
            {
                scaled = Object.Instantiate(sourceLayer);
                scaled.name = scaledAssetName;
                AssetDatabase.CreateAsset(scaled, scaledPath);
            }

            TerrainBiomePalette.Apply(scaled, look, tileSizeMeters);
            EditorUtility.SetDirty(scaled);
            AssetDatabase.SaveAssets();
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
