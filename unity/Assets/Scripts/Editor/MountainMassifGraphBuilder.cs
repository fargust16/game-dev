#if UNITY_EDITOR
using System.IO;
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// One mountain massif as a <b>height delta</b> for Raise blend: foothills near 0,
    /// ridge/peaks in the core — not a dome shaped by Outer falloff alone.
    /// </summary>
    public static class MountainMassifGraphBuilder
    {
        public const string GraphAssetPath = "Assets/Settings/Vista/MountainMassif_TerrainGraph.asset";
        public const int ExpectedTextureOutputs = 4;
        /// <summary>Bump to force LoadOrRebuild when peak curve / maxHeight changes.</summary>
        public const float ExpectedBodyMaxHeight = 1f;

        private const string GrassLayerPath = "Assets/Settings/TerrainLayers/Grass_Fields.terrainlayer";
        private const string SoilLayerPath = "Assets/Settings/TerrainLayers/Dirt_Scaled.terrainlayer";
        private const string RockLayerPath = "Assets/Settings/TerrainLayers/MicroSplat/MS_Rock_Cliff.terrainlayer";
        private const string CliffLayerPath = "Assets/Settings/TerrainLayers/Cliff_Rock.terrainlayer";

        [MenuItem("GameDev/Overworld/Vista/Rebuild Mountain Massif Graph")]
        public static void RebuildMenu()
        {
            var graph = Rebuild();
            if (graph == null)
                return;
            Selection.activeObject = graph;
            EditorGUIUtility.PingObject(graph);
            Debug.Log($"[MountainMassif] Rebuilt ridge+foothills graph at {GraphAssetPath}");
        }

        public static TerrainGraph LoadOrRebuild()
        {
            var graph = AssetDatabase.LoadAssetAtPath<TerrainGraph>(GraphAssetPath);
            if (graph != null &&
                graph.GetNode<HeightOutputNode>() != null &&
                graph.GetNode<GenericMountainNode>() != null &&
                graph.GetNodesOfType<TextureOutputNode>().Count >= ExpectedTextureOutputs)
            {
                var body = graph.GetNode<GenericMountainNode>() as GenericMountainNode;
                if (body != null && Mathf.Abs(body.maxHeight - ExpectedBodyMaxHeight) < 0.01f)
                    return graph;
            }

            return Rebuild();
        }

        public static TerrainGraph Rebuild()
        {
            EnsureFolder("Assets/Settings");
            EnsureFolder("Assets/Settings/Vista");
            if (AssetDatabase.LoadAssetAtPath<TerrainGraph>(GraphAssetPath) != null)
                AssetDatabase.DeleteAsset(GraphAssetPath);

            var graph = ScriptableObject.CreateInstance<TerrainGraph>();
            graph.name = "MountainMassif_TerrainGraph";

            // Macro mass — periphery low so Raise does not stamp a plateau; core reaches ~1.
            var body = new GenericMountainNode();
            body.mountainScale = 900f;
            body.peakVariationScale = 1200f;
            body.minHeight = 0f;
            body.maxHeight = 1f;
            body.seed = 17;

            // Secondary ridges / 2–5 peak hints (legacy RidgedMountain — one instance only).
            var ridges = new RidgedMountainNode();
            ridges.scale = 420f;
            ridges.ridgeIntensity = 8f;
            ridges.seed = 91;

            var addRidges = new CombineNode();
            addRidges.mode = CombineNode.Mode.Add;
            addRidges.backgroundMultiplier = 1f;
            addRidges.foregroundMultiplier = 0.35f;

            // Foothills stay low at the rim; mid→peak rises hard so Raise delta actually shows.
            var foothillCurve = new CurveNode();
            foothillCurve.curve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0.2f),
                new Keyframe(0.18f, 0.05f, 0.4f, 0.4f),
                new Keyframe(0.4f, 0.32f, 1.2f, 1.2f),
                new Keyframe(0.62f, 0.72f, 1.4f, 1.4f),
                new Keyframe(0.82f, 0.94f, 0.6f, 0.6f),
                new Keyframe(1f, 1f, 0f, 0f));

            // Single HydraulicErosion2 — dual instances share temp RT names and look like "fangs".
            var erosion = new HydraulicErosionNode2();
            erosion.featureSize = 12;
            erosion.iterationCount = 140;
            erosion.rainRate = 6f;
            erosion.erosionRate = 6f;
            erosion.depositionRate = 0.3f;
            erosion.evaporationRate = 10f;
            erosion.useBorderFade = true;

            var heightOut = new HeightOutputNode();

            // Textures: foothill grass/soil → rock → cliff (mountain graph must paint grass so
            // Texture Replace under Raise does not wipe island greens to bare rock).
            var soilSlope = new SlopeMaskNode();
            soilSlope.minAngle = 8f;
            soilSlope.maxAngle = 32f;
            soilSlope.transition = Utilities.EaseInOutCurve();

            var rockSlope = new SlopeMaskNode();
            rockSlope.minAngle = 20f;
            rockSlope.maxAngle = 48f;
            rockSlope.transition = Utilities.EaseInOutCurve();

            var cliffSlope = new SlopeMaskNode();
            cliffSlope.minAngle = 38f;
            cliffSlope.maxAngle = 85f;
            cliffSlope.transition = Utilities.EaseInOutCurve();

            var weightBlend = new WeightBlendNode();
            weightBlend.layerCount = 4;

            var grassOut = new TextureOutputNode();
            grassOut.order = 0;
            grassOut.terrainLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(GrassLayerPath);

            var soilOut = new TextureOutputNode();
            soilOut.order = 1;
            soilOut.terrainLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(SoilLayerPath);

            var rockOut = new TextureOutputNode();
            rockOut.order = 2;
            rockOut.terrainLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(RockLayerPath);

            var cliffOut = new TextureOutputNode();
            cliffOut.order = 3;
            cliffOut.terrainLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(CliffLayerPath);

            graph.AddNode(body);
            graph.AddNode(ridges);
            graph.AddNode(addRidges);
            graph.AddNode(foothillCurve);
            graph.AddNode(erosion);
            graph.AddNode(heightOut);
            graph.AddNode(soilSlope);
            graph.AddNode(rockSlope);
            graph.AddNode(cliffSlope);
            graph.AddNode(weightBlend);
            graph.AddNode(grassOut);
            graph.AddNode(soilOut);
            graph.AddNode(rockOut);
            graph.AddNode(cliffOut);

            Connect(graph, body.id, body.outputSlot.id, addRidges.id, addRidges.backgroundSlot.id);
            Connect(graph, ridges.id, ridges.outputSlot.id, addRidges.id, addRidges.foregroundSlot.id);
            Connect(graph, addRidges.id, addRidges.outputSlot.id, foothillCurve.id, foothillCurve.inputSlot.id);
            Connect(graph, foothillCurve.id, foothillCurve.outputSlot.id, erosion.id, erosion.inputHeightSlot.id);
            Connect(graph, erosion.id, erosion.outputHeightSlot.id, heightOut.id, heightOut.inputSlot.id);

            Connect(graph, erosion.id, erosion.outputHeightSlot.id, soilSlope.id, soilSlope.inputSlot.id);
            Connect(graph, erosion.id, erosion.outputHeightSlot.id, rockSlope.id, rockSlope.inputSlot.id);
            Connect(graph, erosion.id, erosion.outputHeightSlot.id, cliffSlope.id, cliffSlope.inputSlot.id);

            Connect(graph, soilSlope.id, soilSlope.outputSlot.id, weightBlend.id, weightBlend.inputSlots[1].id);
            Connect(graph, rockSlope.id, rockSlope.outputSlot.id, weightBlend.id, weightBlend.inputSlots[2].id);
            Connect(graph, cliffSlope.id, cliffSlope.outputSlot.id, weightBlend.id, weightBlend.inputSlots[3].id);

            Connect(graph, weightBlend.id, weightBlend.outputSlots[0].id, grassOut.id, grassOut.inputSlot.id);
            Connect(graph, weightBlend.id, weightBlend.outputSlots[1].id, soilOut.id, soilOut.inputSlot.id);
            Connect(graph, weightBlend.id, weightBlend.outputSlots[2].id, rockOut.id, rockOut.inputSlot.id);
            Connect(graph, weightBlend.id, weightBlend.outputSlots[3].id, cliffOut.id, cliffOut.inputSlot.id);

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
