#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Den.Tools;
using GameDev.World.Seed;
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

            var water = GameObject.Find("OverworldWater");
            if (water != null)
            {
                Object.DestroyImmediate(water);
            }

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

            // Vegetation/POI are derived from the terrain and become stale (wrong biome, wrong
            // spot relative to the new water level) the moment it regenerates - never leave them
            // sitting on the old shape.
            VegetationScatterMenu.ClearVegetation();
            PoiMarkerMenu.ClearMarkers();

            var graph = BuildGraph(seed);
            Debug.Log($"[Overworld] Graph built: {graph.generators.Length} generators, seed {seed}.");

            var mapMagic = FindOrCreateMapMagicObject();
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

            CreateOrUpdateWaterPlane(mapMagic, originCoord);

            SaveActive();
            Selection.activeGameObject = mapMagic.gameObject;
            Debug.Log($"[Overworld] Generated terrain from seed {seed}.");
        }

        /// <summary>
        /// V0 hydrology, first slice: a flat plane at a fixed height threshold (matches
        /// BiomeClassifier's Swamp band start) rather than simulated flow/rivers - terrain that
        /// dips below it reads as lake/pond. Real flow-based rivers are a follow-up
        /// (WORLD_GENERATION_PIPELINE.md, Iteration 05); this only needs the height scale
        /// MapMagic already resolves per graph (mapMagic.globals.height, default 250m), no new
        /// terrain sampling.
        /// </summary>
        private static void CreateOrUpdateWaterPlane(MapMagicObject mapMagic, Coord tileCoord)
        {
            const float waterLevel01 = 0.1f;
            const float extentMeters = 1200f;
            var worldY = waterLevel01 * mapMagic.globals.height;

            // MapMagic tile coords are floor-based like our own ChunkCoord: tile (0,0) spans
            // world [0, tileSize) on each axis, not centered on the world origin. Center the
            // water plane on the actual tile instead of assuming (0,0,0) - that mismatch is why
            // the first version only partly overlapped the terrain.
            var centerX = (tileCoord.x + 0.5) * mapMagic.tileSize.x;
            var centerZ = (tileCoord.z + 0.5) * mapMagic.tileSize.z;

            var water = GameObject.Find("OverworldWater");
            if (water == null)
            {
                water = GameObject.CreatePrimitive(PrimitiveType.Plane);
                water.name = "OverworldWater";
                var collider = water.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.DestroyImmediate(collider);
                }

                var renderer = water.GetComponent<Renderer>();
                renderer.sharedMaterial = CreateWaterMaterial();
            }

            // Unity's built-in Plane primitive is 10x10 world units at scale 1.
            var scale = extentMeters / 10f;
            water.transform.position = new Vector3((float)centerX, worldY, (float)centerZ);
            water.transform.localScale = new Vector3(scale, 1f, scale);

            Debug.Log($"[Overworld] Water plane at ({centerX:F0},{worldY:F1},{centerZ:F0}) " +
                      $"(waterLevel01={waterLevel01}).");
        }

        private static Material CreateWaterMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = "OverworldWater (generated)" };
            material.color = new Color(0.15f, 0.35f, 0.55f, 0.75f);
            return material;
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
        /// Plateau noise remapped to a thin margin just above the water level, with an added
        /// rolling-hills noise layer, blended with one huge-period noise field cut off by Levels
        /// so only its highest local values rise into a single mountain mass, then six deliberate
        /// lake sites (varied size/threshold, random world-space offsets, combined via max) are
        /// subtracted so multiple lakes of varying size actually reach below the water level
        /// instead of either flooding the whole plateau or floating entirely clear of the water
        /// plane (both tried and rejected in earlier passes). SimpleForm200 was tried first for
        /// the "one deliberate mountain" shape but its "chunk-size-relative" sizing tiled the cone
        /// across the tile regardless of the Wrap setting - a huge-period Noise200 is a plain
        /// function of world position (no chunk-relative sizing), so it can't repeat inside one
        /// tile; the same trick, repeated six times with random offsets, is what makes the lake
        /// sites deliberate instead of probabilistic. Recipe: (PlateauNoise -> Levels ->
        /// +HillsNoise) blended with (MountainNoise -> Levels cutoff), then that result blended
        /// with (six lake NoiseN -> Levels cutoffs, chained via max) subtracted -> Erosion ->
        /// Height Output. Seeded from <see cref="WorldSeed"/> so the same world seed reproduces
        /// the same terrain, mountain, hills, and lake placement.
        /// </summary>
        private static Graph BuildGraph(int seed)
        {
            EnsureSettingsFolder();

            var terrainSeed = new WorldSeed(seed).Fork("terrain");
            var graphSeed = terrainSeed.Next();
            var plateauSeed = terrainSeed.Next();
            var mountainSeed = terrainSeed.Next();
            var hillsSeed = terrainSeed.Next();

            var graph = Graph.Create();
            graph.random = new Noise(graphSeed, 32768);

            // Base plains - more detail octaves than before so it's not perfectly smooth even
            // before the dedicated hills layer below adds bigger bumps.
            var plateauNoise = (Noise200)Generator.Create(typeof(Noise200));
            plateauNoise.type = Noise200.Type.Perlin;
            plateauNoise.seed = plateauSeed;
            plateauNoise.size = 600f;
            plateauNoise.intensity = 1f;
            plateauNoise.detail = 0.55f;
            graph.Add(plateauNoise);

            // Remaps the plateau's raw 0..1 noise into a range close above the water level (0.1,
            // see CreateOrUpdateWaterPlane) - deliberately thin margin (0.13-0.19) so the lake
            // pits below don't need to punch through a thick buffer to reach the water; wider
            // margins in earlier passes either flooded almost everything (0.12 flat) or floated
            // entirely clear of the water plane (0.20-0.44).
            var plateauLevels = (Levels200)Generator.Create(typeof(Levels200));
            plateauLevels.inMin = 0f;
            plateauLevels.inMax = 1f;
            plateauLevels.outMin = 0.13f;
            plateauLevels.outMax = 0.19f;
            graph.Add(plateauLevels);
            graph.Link(plateauNoise, plateauLevels);

            // Rolling hills texture, added on top of the plains - period smaller than the mountain
            // wave but still large enough (~220m) that only a few undulations show across the
            // tile, reading as gentle hills rather than a repeating bump pattern. Kept gentle
            // (low opacity) since the ground margin above water is thin now - a strong hills swing
            // would flood low spots on its own instead of leaving that to the dedicated lakes.
            var hillsNoise = (Noise200)Generator.Create(typeof(Noise200));
            hillsNoise.type = Noise200.Type.Perlin;
            hillsNoise.seed = hillsSeed;
            hillsNoise.size = 220f;
            hillsNoise.intensity = 1f;
            hillsNoise.detail = 0.6f;
            graph.Add(hillsNoise);

            var hillsBlend = (Blend200)Generator.Create(typeof(Blend200));
            hillsBlend.layers[0].algorithm = Blend200.BlendAlgorithm.mix;
            hillsBlend.layers[0].opacity = 1f;
            hillsBlend.layers[1].algorithm = Blend200.BlendAlgorithm.add;
            hillsBlend.layers[1].opacity = 0.06f;
            graph.Add(hillsBlend);
            graph.Link(plateauLevels, hillsBlend.layers[0].inlet);
            graph.Link(hillsNoise, hillsBlend.layers[1].inlet);

            // Period far larger than the ~512-1000m stub, so only a slowly-varying slice of one
            // "wave" is visible - no risk of it repeating inside the playable area.
            var mountainNoise = (Noise200)Generator.Create(typeof(Noise200));
            mountainNoise.type = Noise200.Type.Perlin;
            mountainNoise.seed = mountainSeed;
            mountainNoise.size = 2000f;
            mountainNoise.intensity = 1f;
            mountainNoise.detail = 0.3f;
            graph.Add(mountainNoise);

            // Cuts off everything below inMin to flat (0) and stretches what's left (roughly the
            // upper half of the noise field, wherever that falls) into a single rising mass.
            var levels = (Levels200)Generator.Create(typeof(Levels200));
            levels.inMin = 0.5f;
            levels.inMax = 1f;
            levels.outMin = 0f;
            levels.outMax = 1f;
            graph.Add(levels);
            graph.Link(mountainNoise, levels);

            var blend = (Blend200)Generator.Create(typeof(Blend200));
            blend.layers[0].algorithm = Blend200.BlendAlgorithm.mix;
            blend.layers[0].opacity = 1f;
            blend.layers[1].algorithm = Blend200.BlendAlgorithm.add;
            blend.layers[1].opacity = 1f;
            graph.Add(blend);
            graph.Link((IOutlet<Den.Tools.Matrices.MatrixWorld>)hillsBlend, (IInlet<Den.Tools.Matrices.MatrixWorld>)blend.layers[0].inlet);
            graph.Link(levels, blend.layers[1].inlet);

            // Six guaranteed lake sites of varying size instead of a probabilistic noise
            // threshold: each is its own (Noise -> Levels cutoff) pair - same non-repeating "huge
            // period, single peak" trick as the mountain - with a random world-space offset so
            // the six peaks land in different, spread-out spots rather than clustering, and a
            // varied size/threshold pair so they don't all read as the same lake. Combined pairwise
            // via max ("whichever site is deepest here wins") into one mask, then subtracted.
            var lakeSpecs = new (float Size, float InMin)[]
            {
                (520f, 0.55f), // large
                (430f, 0.58f), // large
                (300f, 0.60f), // medium
                (260f, 0.62f), // medium
                (160f, 0.64f), // small
                (120f, 0.66f), // small
            };

            Generator lakeMask = null;
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

                if (lakeMask == null)
                {
                    lakeMask = cutoff;
                    continue;
                }

                var combine = (Blend200)Generator.Create(typeof(Blend200));
                combine.layers[0].algorithm = Blend200.BlendAlgorithm.mix;
                combine.layers[0].opacity = 1f;
                combine.layers[1].algorithm = Blend200.BlendAlgorithm.max;
                combine.layers[1].opacity = 1f;
                graph.Add(combine);
                graph.Link((IOutlet<Den.Tools.Matrices.MatrixWorld>)lakeMask, (IInlet<Den.Tools.Matrices.MatrixWorld>)combine.layers[0].inlet);
                graph.Link(cutoff, combine.layers[1].inlet);
                lakeMask = combine;
            }

            var lakeBlend = (Blend200)Generator.Create(typeof(Blend200));
            lakeBlend.layers[0].algorithm = Blend200.BlendAlgorithm.mix;
            lakeBlend.layers[0].opacity = 1f;
            lakeBlend.layers[1].algorithm = Blend200.BlendAlgorithm.subtract;
            // Each of the 6 sites reaches a different peak mask value depending on its own
            // noise/threshold - 0.35 was enough for the deepest sites but left shallower ones as
            // a dry crater instead of a filled lake. Higher opacity so all six reliably punch
            // through to below the water level, not just the strongest ones.
            lakeBlend.layers[1].opacity = 0.7f;
            graph.Add(lakeBlend);
            graph.Link((IOutlet<Den.Tools.Matrices.MatrixWorld>)blend, (IInlet<Den.Tools.Matrices.MatrixWorld>)lakeBlend.layers[0].inlet);
            graph.Link((IOutlet<Den.Tools.Matrices.MatrixWorld>)lakeMask, (IInlet<Den.Tools.Matrices.MatrixWorld>)lakeBlend.layers[1].inlet);

            var erosion = (Erosion200)Generator.Create(typeof(Erosion200));
            graph.Add(erosion);
            graph.Link(lakeBlend, erosion);

            var heightOutput = (HeightOutput200)Generator.Create(typeof(HeightOutput200));
            graph.Add(heightOutput);
            graph.Link(erosion, heightOutput);

            // Exposes the same height matrix as a named runtime-queryable channel via
            // DirectMatricesHolder (see ASSET_LIBRARY_REFERENCE.md) so gameplay code
            // (IBiomeSampler) can sample height without touching the Terrain heightmap.
            var directOutput = (DirectMatricesOutput200)Generator.Create(typeof(DirectMatricesOutput200));
            var heightChannel = new DirectMatricesOutput200.DirectMatricesLayer { name = "height" };
            heightChannel.SetGen(directOutput);
            heightChannel.Id = Den.Tools.Id.Generate();
            directOutput.layers = new[] { heightChannel };
            graph.Add(directOutput);
            graph.Link((IOutlet<Den.Tools.Matrices.MatrixWorld>)erosion, (IInlet<Den.Tools.Matrices.MatrixWorld>)heightChannel);

            if (AssetDatabase.LoadAssetAtPath<Graph>(GraphPath) != null)
            {
                AssetDatabase.DeleteAsset(GraphPath);
            }

            AssetDatabase.CreateAsset(graph, GraphPath);
            AssetDatabase.SaveAssets();
            return graph;
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
