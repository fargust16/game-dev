#if UNITY_EDITOR
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;
using SurfaceLayer = GameDev.EditorTools.OverworldTerrainLayers.SurfaceLayer;

namespace GameDev.EditorTools
{
    /// <summary>
    /// The seabed under everything else: gentle relief, entirely below sea level.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Its job is to give the biomes above it something to blend <em>down to</em>. Vista's linear
    /// blend is <c>dest = lerp(dest, src, biomeMask)</c>, so without a seabed the destination starts
    /// at zero and every island's Outer falloff collapses its rim to absolute 0 — a cliff into a void
    /// instead of a shore, and most of the authored polygon drowned. With a seabed the falloff ring
    /// interpolates between seabed and island, which is a real underwater slope and gives the beach
    /// somewhere to exist.
    /// </para>
    /// <para>
    /// Deliberately cheap: no erosion pass and no vegetation. A seabed has no drainage network and
    /// nothing grows on it, and it covers the whole world, so every node here is paid for four times
    /// over.
    /// </para>
    /// </remarks>
    public static class SeaFloorGraphBuilder
    {
        public const string GraphAssetPath = "Assets/Settings/Vista/SeaFloor_TerrainGraph.asset";

        /// <summary>Weight Blend priority slots. One per canonical surface layer.</summary>
        private const int WeightBlendLayers = OverworldTerrainLayers.CanonicalLayerCount;

        [MenuItem("GameDev/Overworld/Vista/Rebuild Sea Floor Graph (destructive)")]
        public static void RebuildMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild the sea floor terrain graph?",
                    "This re-authors SeaFloor_TerrainGraph from code, in place. Every edit made in " +
                    "the Vista graph editor is lost; references to the asset are kept.",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            var graph = Rebuild();
            if (graph == null)
            {
                return;
            }

            Selection.activeObject = graph;
            EditorGUIUtility.PingObject(graph);
            Debug.Log($"[SeaFloor] Rebuilt graph at {GraphAssetPath}");
        }

        /// <summary>Loads the authored graph. Never rebuilds — the asset is the source of truth.</summary>
        public static TerrainGraph Load()
        {
            var graph = AssetDatabase.LoadAssetAtPath<TerrainGraph>(GraphAssetPath);
            if (graph == null)
            {
                Debug.LogError(
                    $"[SeaFloor] No terrain graph at {GraphAssetPath}. " +
                    "Run GameDev/Overworld/Vista/Rebuild Sea Floor Graph (destructive) once.");
            }

            return graph;
        }

        /// <summary>Re-authors the asset from code, discarding any Inspector edits.</summary>
        public static TerrainGraph Rebuild()
        {
            VistaGraphBuild.EnsureFolder("Assets/Settings/Vista");

            var graph = ScriptableObject.CreateInstance<TerrainGraph>();
            graph.name = "SeaFloor_TerrainGraph";

            BuildTerrainStage(graph);
            BuildSurfaceStage(graph);

            graph.Validate();
            return VistaGraphBuild.SaveGraph(graph, GraphAssetPath);
        }

        private static void BuildTerrainStage(TerrainGraph graph)
        {
            // Broad, low-frequency swells. Nothing at island scale — this is the shelf the island
            // sits on, not another landform competing with it.
            var detail = VistaGraphBuild.AddDetailNoise(graph, scale: 1600f, seed: 5501);
            var mass = VistaGraphBuild.AddMassNoise(graph, scale: 2600f, seed: 5502);

            var shape = new CombineNode { mode = CombineNode.Mode.Multiply };
            graph.AddNode(shape);

            var levels = VistaGraphBuild.AddReferenceLevels(graph);

            var setHeight = VistaGraphBuild.AddSetVar(graph, VistaGraphBuild.VarHeightMap);
            var heightOut = new HeightOutputNode();
            graph.AddNode(heightOut);

            VistaGraphBuild.Connect(graph, detail.id, detail.outputSlot.id, shape.id, shape.backgroundSlot.id);
            VistaGraphBuild.Connect(graph, mass.id, mass.outputSlot.id, shape.id, shape.foregroundSlot.id);
            VistaGraphBuild.Connect(graph, shape.id, shape.outputSlot.id, levels.id, levels.inputSlot.id);
            VistaGraphBuild.Connect(
                graph, levels.id, levels.outputSlot.id, setHeight.id, setHeight.inputSlot.id);
            VistaGraphBuild.Connect(
                graph, setHeight.id, setHeight.outputSlot.id, heightOut.id, heightOut.inputSlot.id);
        }

        /// <summary>
        /// Sand everywhere, rock where the seabed steepens.
        /// </summary>
        /// <remarks>
        /// All five canonical Texture Outputs are emitted even though only two carry a signal. Vista
        /// builds each tile's <c>terrainLayers</c> array from the distinct layers of the biomes
        /// covering it, so a graph that emits a shorter set would make tiles disagree about what
        /// alphamap channel <i>n</i> means — and this biome covers every tile.
        /// </remarks>
        private static void BuildSurfaceStage(TerrainGraph graph)
        {
            var heightForSlope = VistaGraphBuild.AddGetVar(graph, VistaGraphBuild.VarHeightMap);
            var steep = new SlopeMaskNode
            {
                minAngle = 25f,
                maxAngle = 90f,
                transition = VistaGraphBuild.RisingBandCurve(0.4f),
            };
            graph.AddNode(steep);

            var weightBlend = new WeightBlendNode { layerCount = WeightBlendLayers };
            graph.AddNode(weightBlend);

            var outputs = VistaGraphBuild.AddCanonicalTextureOutputs(graph);

            VistaGraphBuild.Connect(
                graph, heightForSlope.id, heightForSlope.outputSlot.id, steep.id, steep.inputSlot.id);

            // Blend slot 0 (sand) intentionally unconnected — Weight Blend defaults it to white, and
            // that white base is the only thing making the weights sum to 1.
            VistaGraphBuild.Connect(graph, steep.id, steep.outputSlot.id, weightBlend.id, weightBlend.inputSlots[4].id);

            ConnectBlendToOutput(graph, weightBlend, 0, outputs, SurfaceLayer.Sand);
            ConnectBlendToOutput(graph, weightBlend, 1, outputs, SurfaceLayer.Grass);
            ConnectBlendToOutput(graph, weightBlend, 2, outputs, SurfaceLayer.Soil);
            ConnectBlendToOutput(graph, weightBlend, 3, outputs, SurfaceLayer.Cliff);
            ConnectBlendToOutput(graph, weightBlend, 4, outputs, SurfaceLayer.Rock);
        }

        /// <summary>Routes one Weight Blend priority slot to the canonical layer it paints.</summary>
        private static void ConnectBlendToOutput(
            TerrainGraph graph,
            WeightBlendNode weightBlend,
            int blendLayer,
            TextureOutputNode[] outputs,
            SurfaceLayer layer)
        {
            var output = outputs[(int)layer];
            VistaGraphBuild.Connect(
                graph, weightBlend.id, weightBlend.outputSlots[blendLayer].id, output.id, output.inputSlot.id);
        }
    }
}
#endif
