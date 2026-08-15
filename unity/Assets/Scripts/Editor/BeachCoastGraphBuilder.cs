#if UNITY_EDITOR
using System.IO;
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Coastal sand strip for <c>LPB_Beach</c> — texture weights only (no height carve).
    /// Coast ramp geometry comes from Island Outer falloff + low plateau.
    /// </summary>
    public static class BeachCoastGraphBuilder
    {
        public const string GraphAssetPath = "Assets/Settings/Vista/BeachCoast_TerrainGraph.asset";
        public const int ExpectedTextureOutputs = 1;

        private const string SandLayerPath =
            "Assets/Settings/TerrainLayers/MicroSplat/MS_Shore_Sand.terrainlayer";

        [MenuItem("GameDev/Overworld/Vista/Rebuild Beach Coast Graph")]
        public static void RebuildMenu()
        {
            var graph = Rebuild();
            if (graph == null)
                return;
            Selection.activeObject = graph;
            EditorGUIUtility.PingObject(graph);
            Debug.Log($"[BeachCoast] Rebuilt flat sand terrace graph at {GraphAssetPath}");
        }

        public static TerrainGraph LoadOrRebuild()
        {
            var graph = AssetDatabase.LoadAssetAtPath<TerrainGraph>(GraphAssetPath);
            if (graph != null &&
                graph.GetNode<HeightOutputNode>() != null &&
                graph.GetNodesOfType<TextureOutputNode>().Count >= ExpectedTextureOutputs)
            {
                return graph;
            }

            return Rebuild();
        }

        public static TerrainGraph Rebuild()
        {
            EnsureFolder("Assets/Settings");
            EnsureFolder("Assets/Settings/Vista");
            OverworldMicroSplatSetup.EnsureFallbackLayers();
            if (AssetDatabase.LoadAssetAtPath<TerrainGraph>(GraphAssetPath) != null)
                AssetDatabase.DeleteAsset(GraphAssetPath);

            var graph = ScriptableObject.CreateInstance<TerrainGraph>();
            graph.name = "BeachCoast_TerrainGraph";

            // Near-flat shelf (tiny micro so it is not a perfect plane).
            var body = new NoiseNode();
            body.scale = 4000f;
            body.layerCount = 1;
            body.warpMode = NoiseNode.WarpMode.None;
            body.remapCurve = AnimationCurve.Linear(0f, 0.97f, 1f, 1f);
            body.seed = 3;

            var heightOut = new HeightOutputNode();

            // Full-weight sand inside the coastal texture strip.
            var sandMask = new NoiseNode();
            sandMask.scale = 4000f;
            sandMask.layerCount = 1;
            sandMask.warpMode = NoiseNode.WarpMode.None;
            sandMask.remapCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
            sandMask.seed = 5;

            var sandOut = new TextureOutputNode();
            sandOut.order = 0;
            sandOut.terrainLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(SandLayerPath);

            graph.AddNode(body);
            graph.AddNode(heightOut);
            graph.AddNode(sandMask);
            graph.AddNode(sandOut);

            Connect(graph, body.id, body.outputSlot.id, heightOut.id, heightOut.inputSlot.id);
            Connect(graph, sandMask.id, sandMask.outputSlot.id, sandOut.id, sandOut.inputSlot.id);

            graph.Validate();
            AssetDatabase.CreateAsset(graph, GraphAssetPath);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            return graph;
        }

        private static void Connect(
            TerrainGraph graph,
            string outNodeId,
            int outSlotId,
            string inNodeId,
            int inSlotId)
        {
            graph.AddEdge(new Edge(new SlotRef(outNodeId, outSlotId), new SlotRef(inNodeId, inSlotId)));
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
