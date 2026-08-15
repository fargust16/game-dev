#if UNITY_EDITOR && VISTA
using System.Collections.Generic;
using System.IO;
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Working overworld TerrainGraph — height-safe at true world metres (~2800 m footprint).
    /// Demo MountainRange is not cloned raw: PointsNode OOMs; dual HydraulicErosion clashes on
    /// shared temp RT names. One Landslide at detailLevel ≤ 0.5 is safe at 2800 m (~1400 px RT).
    /// Critical: LPB must use metre anchors with localScale=1 (never Blank ±500 × scale=extent).
    /// </summary>
    public static class NaturalIslandGraphBuilder
    {
        public const string GraphAssetPath = "Assets/Settings/Vista/NaturalIsland_TerrainGraph.asset";

        /// <summary>Max Landslide detailLevel allowed for world-metre biomes (2800 m → ~1400 px RT).</summary>
        public const float MaxLandslideDetailLevel = 0.5f;

        [MenuItem("GameDev/Overworld/Vista/Rebuild Natural Island Graph")]
        public static void RebuildMenu()
        {
            var graph = RebuildSafeHeightGraph();
            if (graph != null)
            {
                Selection.activeObject = graph;
                EditorGUIUtility.PingObject(graph);
                Debug.Log(
                    $"[NaturalIsland] Rebuilt world-metre height graph " +
                    $"(Noise+Ridge+Landslide≤{MaxLandslideDetailLevel}+Hydraulic×1) at {GraphAssetPath}");
            }
        }

        /// <summary>
        /// Demo-inspired world-metre height: Noise×3 + one RidgedMountain + Combines + Remap +
        /// one Landslide (detail 0.5) + one HydraulicErosion2 + HeightOutput.
        /// </summary>
        public static TerrainGraph RebuildSafeHeightGraph()
        {
            EnsureFolder("Assets/Settings");
            EnsureFolder("Assets/Settings/Vista");

            if (AssetDatabase.LoadAssetAtPath<TerrainGraph>(GraphAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(GraphAssetPath);
            }

            var graph = ScriptableObject.CreateInstance<TerrainGraph>();
            graph.name = "NaturalIsland_TerrainGraph";

            // Macro base — broad warped landform (inspired by Vista MountainRange noise scales).
            var baseNoise = new NoiseNode();
            baseNoise.scale = 1200f;
            baseNoise.layerCount = 8;
            baseNoise.lacunarity = 2.25f;
            baseNoise.persistence = 0.38f;
            baseNoise.warpMode = NoiseNode.WarpMode.Angular;
            baseNoise.warpIntensity = 2.5f;
            baseNoise.remapCurve = AnimationCurve.EaseInOut(0f, 0.08f, 1f, 0.55f);
            baseNoise.seed = 321;

            // One RidgedMountain only — Vista shares static temp RT names across instances.
            var ridge = new RidgedMountainNode();
            ridge.scale = 750f;
            ridge.ridgeIntensity = 10f;
            ridge.seed = 855;

            // Secondary ridge contribution via warped noise (no second RidgedMountain).
            var ridgeNoise = new NoiseNode();
            ridgeNoise.scale = 500f;
            ridgeNoise.layerCount = 6;
            ridgeNoise.persistence = 0.32f;
            ridgeNoise.mode = NoiseMode.Perlin01;
            ridgeNoise.warpMode = NoiseNode.WarpMode.Directional;
            ridgeNoise.warpIntensity = 4f;
            ridgeNoise.remapCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.45f);
            ridgeNoise.seed = 42;

            // Fine detail — small amplitude only.
            var detailNoise = new NoiseNode();
            detailNoise.scale = 200f;
            detailNoise.layerCount = 4;
            detailNoise.persistence = 0.45f;
            detailNoise.warpMode = NoiseNode.WarpMode.None;
            detailNoise.remapCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.12f);
            detailNoise.seed = 818;

            var addRidge = new CombineNode();
            addRidge.mode = CombineNode.Mode.Add;
            addRidge.backgroundMultiplier = 1f;
            addRidge.foregroundMultiplier = 0.65f;

            var addRidgeNoise = new CombineNode();
            addRidgeNoise.mode = CombineNode.Mode.Add;
            addRidgeNoise.backgroundMultiplier = 1f;
            addRidgeNoise.foregroundMultiplier = 0.4f;

            var addDetail = new CombineNode();
            addDetail.mode = CombineNode.Mode.Add;
            addDetail.backgroundMultiplier = 1f;
            addDetail.foregroundMultiplier = 1f;

            var remap = new RemapNode();
            remap.min = 0.02f;
            remap.max = 0.92f;

            // Landslide at world metres — detailLevel 0.5 × 2800 m ≈ 1400 px (safe under 16384).
            var landslide = new LandslideNode();
            landslide.detailLevel = MaxLandslideDetailLevel;
            landslide.intensity = 1f;
            landslide.flowRate = 1f;
            landslide.restingAngle = 18f;
            landslide.iterationCount = 18;
            landslide.highQualityMode = true;

            // Single erosion node — dual HydraulicErosion* registers the same temp shader twice.
            var erosion = new HydraulicErosionNode2();
            erosion.featureSize = 10;
            erosion.iterationCount = 160;
            erosion.rainRate = 6f;
            erosion.erosionRate = 6f;
            erosion.depositionRate = 0.35f;
            erosion.evaporationRate = 10f;
            erosion.useBorderFade = true;

            var heightOut = new HeightOutputNode();

            graph.AddNode(baseNoise);
            graph.AddNode(ridge);
            graph.AddNode(ridgeNoise);
            graph.AddNode(detailNoise);
            graph.AddNode(addRidge);
            graph.AddNode(addRidgeNoise);
            graph.AddNode(addDetail);
            graph.AddNode(remap);
            graph.AddNode(landslide);
            graph.AddNode(erosion);
            graph.AddNode(heightOut);

            Connect(graph, baseNoise.id, baseNoise.outputSlot.id, addRidge.id, addRidge.backgroundSlot.id);
            Connect(graph, ridge.id, ridge.outputSlot.id, addRidge.id, addRidge.foregroundSlot.id);
            Connect(graph, addRidge.id, addRidge.outputSlot.id, addRidgeNoise.id, addRidgeNoise.backgroundSlot.id);
            Connect(graph, ridgeNoise.id, ridgeNoise.outputSlot.id, addRidgeNoise.id, addRidgeNoise.foregroundSlot.id);
            Connect(graph, addRidgeNoise.id, addRidgeNoise.outputSlot.id, addDetail.id, addDetail.backgroundSlot.id);
            Connect(graph, detailNoise.id, detailNoise.outputSlot.id, addDetail.id, addDetail.foregroundSlot.id);
            Connect(graph, addDetail.id, addDetail.outputSlot.id, remap.id, remap.inputSlot.id);
            Connect(graph, remap.id, remap.outputSlot.id, landslide.id, landslide.inputHeightSlot.id);
            Connect(graph, landslide.id, landslide.outputHeightSlot.id, erosion.id, erosion.inputHeightSlot.id);
            Connect(graph, erosion.id, erosion.outputHeightSlot.id, heightOut.id, heightOut.inputSlot.id);

            graph.Validate();
            AssetDatabase.CreateAsset(graph, GraphAssetPath);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            return graph;
        }

        public static TerrainGraph LoadOrRebuild()
        {
            var graph = AssetDatabase.LoadAssetAtPath<TerrainGraph>(GraphAssetPath);
            var landslides = graph != null ? graph.GetNodesOfType<LandslideNode>() : null;
            var hasTileLandslide = landslides != null && landslides.Count == 1;
            if (graph != null &&
                graph.GetNode<HeightOutputNode>() != null &&
                IsHeightSafe(graph) &&
                hasTileLandslide)
            {
                return graph;
            }

            if (graph != null)
            {
                Debug.LogWarning(
                    "[NaturalIsland] Working graph outdated/unsafe for world-metre biome " +
                    "(need one Landslide≤0.5, no Points/multi-erosion). Rebuilding.");
            }

            return RebuildSafeHeightGraph();
        }

        public static bool IsHeightSafe(TerrainGraph graph)
        {
            if (graph == null)
            {
                return false;
            }

            if (graph.GetNodesOfType<PointsNode>().Count > 0)
            {
                return false;
            }

            var landslides = graph.GetNodesOfType<LandslideNode>();
            if (landslides.Count > 1)
            {
                return false;
            }

            for (var i = 0; i < landslides.Count; i++)
            {
                if (landslides[i] != null && landslides[i].detailLevel > MaxLandslideDetailLevel + 0.001f)
                {
                    return false;
                }
            }

            if (graph.GetNodesOfType<TreeOutputNode>().Count > 0 ||
                graph.GetNodesOfType<ObjectOutputNode>().Count > 0 ||
                graph.GetNodesOfType<DetailInstanceOutputNode>().Count > 0)
            {
                return false;
            }

            var erosion1 = graph.GetNodesOfType<HydraulicErosionNode>().Count;
            var erosion2 = graph.GetNodesOfType<HydraulicErosionNode2>().Count;
            if (erosion1 + erosion2 > 1)
            {
                return false;
            }

            if (graph.GetNodesOfType<RidgedMountainNode>().Count > 1)
            {
                return false;
            }

            return graph.GetNode<HeightOutputNode>() != null;
        }

        /// <summary>Strip known OOM nodes from an existing graph (best-effort; prefer RebuildSafeHeightGraph).</summary>
        public static int StripUnsafeNodes(TerrainGraph graph)
        {
            if (graph == null)
            {
                return 0;
            }

            var toRemove = new List<string>();
            CollectIds<PointsNode>(graph, toRemove);
            CollectIds<TreeOutputNode>(graph, toRemove);
            CollectIds<ObjectOutputNode>(graph, toRemove);
            CollectIds<DetailInstanceOutputNode>(graph, toRemove);

            // Keep at most one Landslide / one erosion / one RidgedMountain; clamp Landslide detail.
            var landslides = graph.GetNodesOfType<LandslideNode>();
            TrimExtras(landslides, toRemove, keep: 1);
            for (var i = 0; i < landslides.Count; i++)
            {
                if (landslides[i] != null && landslides[i].detailLevel > MaxLandslideDetailLevel)
                {
                    landslides[i].detailLevel = MaxLandslideDetailLevel;
                }
            }

            TrimExtras(graph.GetNodesOfType<HydraulicErosionNode>(), toRemove, keep: 0);
            TrimExtras(graph.GetNodesOfType<HydraulicErosionNode2>(), toRemove, keep: 1);
            TrimExtras(graph.GetNodesOfType<RidgedMountainNode>(), toRemove, keep: 1);

            var all = graph.GetNodes();
            for (var i = 0; i < all.Count; i++)
            {
                var n = all[i];
                if (n == null)
                {
                    continue;
                }

                var typeName = n.GetType().Name;
                if ((typeName.Contains("Scatter") || typeName.Contains("Splatter")) &&
                    !toRemove.Contains(n.id))
                {
                    toRemove.Add(n.id);
                }
            }

            var removed = 0;
            for (var i = 0; i < toRemove.Count; i++)
            {
                if (graph.HasNode(toRemove[i]))
                {
                    graph.RemoveNode(toRemove[i]);
                    removed++;
                }
            }

            if (removed > 0)
            {
                graph.Validate();
                graph.InvokeChangeEvent();
            }

            return removed;
        }

        public static void InjectSeeds(TerrainGraph graph, World.Seed.IWorldSeed worldSeed)
        {
            if (graph == null || worldSeed == null)
            {
                return;
            }

            if (!IsHeightSafe(graph))
            {
                StripUnsafeNodes(graph);
            }

            var island = worldSeed.Fork("terrain.island").Next();
            var bas = worldSeed.Fork("terrain.base").Next();
            var mountain = worldSeed.Fork("terrain.mountain").Next();
            var erosionSeed = worldSeed.Fork("terrain.erosion").Next();
            var detail = worldSeed.Fork("terrain.detail").Next();

            var noises = graph.GetNodesOfType<NoiseNode>();
            for (var i = 0; i < noises.Count; i++)
            {
                noises[i].seed = bas ^ island ^ (i * 7919);
            }

            var ridges = graph.GetNodesOfType<RidgedMountainNode>();
            for (var i = 0; i < ridges.Count; i++)
            {
                ridges[i].seed = mountain ^ (i * 9973) ^ erosionSeed;
            }

            var all = graph.GetNodes();
            for (var i = 0; i < all.Count; i++)
            {
                if (all[i] is NoiseNode || all[i] is RidgedMountainNode)
                {
                    continue;
                }

                if (all[i] is IHasSeed hasSeed)
                {
                    hasSeed.seed = bas ^ mountain ^ detail ^ (i * 13);
                }
            }

            graph.InvokeChangeEvent();
        }

        private static void TrimExtras<T>(List<T> nodes, List<string> toRemove, int keep)
            where T : class, INode
        {
            for (var i = keep; i < nodes.Count; i++)
            {
                if (nodes[i] != null && !toRemove.Contains(nodes[i].id))
                {
                    toRemove.Add(nodes[i].id);
                }
            }
        }

        private static void CollectIds<T>(TerrainGraph graph, List<string> ids) where T : class, INode
        {
            var nodes = graph.GetNodesOfType<T>();
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && !ids.Contains(nodes[i].id))
                {
                    ids.Add(nodes[i].id);
                }
            }
        }

        private static void Connect(
            TerrainGraph graph,
            string outNodeId,
            int outSlotId,
            string inNodeId,
            int inSlotId)
        {
            var edge = new Edge(new SlotRef(outNodeId, outSlotId), new SlotRef(inNodeId, inSlotId));
            graph.AddEdge(edge);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
