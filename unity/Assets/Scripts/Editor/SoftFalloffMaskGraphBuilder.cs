#if UNITY_EDITOR
using System.IO;
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Shared <see cref="BiomeMaskGraph"/>: polygon falloff + soft noise detail so LPB edges
    /// are not constant-width rings (Vista FalloffDetail + Noise).
    /// </summary>
    public static class SoftFalloffMaskGraphBuilder
    {
        public const string GraphAssetPath = "Assets/Settings/Vista/SoftFalloff_BiomeMaskGraph.asset";

        [MenuItem("GameDev/Overworld/Vista/Rebuild Soft Falloff Mask Graph")]
        public static void RebuildMenu()
        {
            var graph = Rebuild();
            if (graph == null)
                return;
            Selection.activeObject = graph;
            EditorGUIUtility.PingObject(graph);
            Debug.Log($"[SoftFalloffMask] Rebuilt at {GraphAssetPath}");
        }

        public static BiomeMaskGraph LoadOrRebuild()
        {
            var graph = AssetDatabase.LoadAssetAtPath<BiomeMaskGraph>(GraphAssetPath);
            if (graph != null &&
                graph.GetNode<FalloffDetailNode>() != null &&
                graph.GetNode<OutputNode>() != null)
            {
                var detail = graph.GetNode<FalloffDetailNode>() as FalloffDetailNode;
                if (detail != null && detail.detailMultiplier <= 0.35f)
                    return graph;
            }

            return Rebuild();
        }

        public static BiomeMaskGraph Rebuild()
        {
            EnsureFolder("Assets/Settings");
            EnsureFolder("Assets/Settings/Vista");
            if (AssetDatabase.LoadAssetAtPath<BiomeMaskGraph>(GraphAssetPath) != null)
                AssetDatabase.DeleteAsset(GraphAssetPath);

            var graph = ScriptableObject.CreateInstance<BiomeMaskGraph>();
            graph.name = "SoftFalloff_BiomeMaskGraph";

            var input = new InputNode();
            input.inputName = GraphConstants.BIOME_MASK_INPUT_NAME;
            input.SetSlotType(typeof(MaskSlot));

            // Large-scale edge wobble — not micro noise that looks like static.
            var detailNoise = new NoiseNode();
            detailNoise.scale = 900f;
            detailNoise.layerCount = 3;
            detailNoise.lacunarity = 2.1f;
            detailNoise.persistence = 0.4f;
            detailNoise.warpMode = NoiseNode.WarpMode.Angular;
            detailNoise.warpIntensity = 1.2f;
            detailNoise.remapCurve = AnimationCurve.Linear(0f, 0.15f, 1f, 0.85f);
            detailNoise.seed = 71;

            var falloffDetail = new FalloffDetailNode();
            // Keep mild — strong detail can push the soft edge past the world rim.
            falloffDetail.detailMultiplier = 0.28f;

            var output = new OutputNode();
            output.outputName = GraphConstants.BIOME_MASK_OUTPUT_NAME;
            output.SetSlotType(typeof(MaskSlot));

            graph.AddNode(input);
            graph.AddNode(detailNoise);
            graph.AddNode(falloffDetail);
            graph.AddNode(output);

            Connect(graph, input.id, input.outputSlot.id, falloffDetail.id, falloffDetail.baseMaskSlot.id);
            Connect(graph, detailNoise.id, detailNoise.outputSlot.id, falloffDetail.id, falloffDetail.detailMaskSlot.id);
            Connect(graph, falloffDetail.id, falloffDetail.outputSlot.id, output.id, output.inputSlot.id);

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
