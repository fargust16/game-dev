#if UNITY_EDITOR
using System.IO;
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Rolling island shelf for <c>LPB_IslandBase</c> — macro shelves / saddles / soft basins,
    /// not mountains and not uniform ripple noise. Includes slope/height TextureOutputs.
    /// </summary>
    public static class IslandPlateauGraphBuilder
    {
        public const string GraphAssetPath = "Assets/Settings/Vista/IslandPlateau_TerrainGraph.asset";

        /// <summary>Bump when graph topology changes so LoadOrRebuild replaces stale assets.</summary>
        /// <remarks>
        /// Plateau worldHeight≈55 with remap 0.55–1.0 → rolling ASL ~30–55 on solid land
        /// (gentle sea rise, not a 90 m mesa). Outer falloff + directed C# coast soften the shore.
        /// </remarks>
        public const float ExpectedRemapMin = 0.55f;
        public const float ExpectedRemapMax = 1f;
        public const int ExpectedTextureOutputs = 4;

        private const string GrassLayerPath = "Assets/Settings/TerrainLayers/Grass_Fields.terrainlayer";
        private const string SoilLayerPath = "Assets/Settings/TerrainLayers/Dirt_Scaled.terrainlayer";
        private const string RockLayerPath = "Assets/Settings/TerrainLayers/Gravel_Highland.terrainlayer";
        private const string ShoreLayerPath = "Assets/Settings/TerrainLayers/MicroSplat/MS_Shore_Sand.terrainlayer";

        [MenuItem("GameDev/Overworld/Vista/Rebuild Island Plateau Graph")]
        public static void RebuildMenu()
        {
            var graph = Rebuild();
            if (graph == null)
                return;
            Selection.activeObject = graph;
            EditorGUIUtility.PingObject(graph);
            Debug.Log($"[IslandPlateau] Rebuilt macro shelf graph at {GraphAssetPath}");
        }

        public static TerrainGraph LoadOrRebuild()
        {
            var graph = AssetDatabase.LoadAssetAtPath<TerrainGraph>(GraphAssetPath);
            if (graph != null && graph.GetNode<HeightOutputNode>() != null)
            {
                var remaps = graph.GetNodesOfType<RemapNode>();
                var remap = remaps != null && remaps.Count > 0 ? remaps[0] : null;
                var texOuts = graph.GetNodesOfType<TextureOutputNode>();
                if (remap != null &&
                    Mathf.Abs(remap.min - ExpectedRemapMin) < 0.02f &&
                    Mathf.Abs(remap.max - ExpectedRemapMax) < 0.02f &&
                    texOuts != null &&
                    texOuts.Count >= ExpectedTextureOutputs)
                {
                    return graph;
                }
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
            graph.name = "IslandPlateau_TerrainGraph";

            // Macro: broad plains with occasional knoll / сопка — not mountain relief.
            var macroShelves = new NoiseNode();
            macroShelves.scale = 2400f;
            macroShelves.layerCount = 3;
            macroShelves.lacunarity = 2.0f;
            macroShelves.persistence = 0.38f;
            macroShelves.warpMode = NoiseNode.WarpMode.Angular;
            macroShelves.warpIntensity = 1.1f;
            macroShelves.remapCurve = AnimationCurve.EaseInOut(0f, 0.35f, 1f, 0.78f);
            macroShelves.seed = 110;

            // Local hills / knolls — small amplitude only.
            var secondary = new NoiseNode();
            secondary.scale = 700f;
            secondary.layerCount = 3;
            secondary.persistence = 0.32f;
            secondary.warpMode = NoiseNode.WarpMode.Directional;
            secondary.warpIntensity = 1.3f;
            secondary.remapCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.22f);
            secondary.seed = 277;

            // Barely-there micro.
            var micro = new NoiseNode();
            micro.scale = 260f;
            micro.layerCount = 2;
            micro.persistence = 0.28f;
            micro.warpMode = NoiseNode.WarpMode.None;
            micro.remapCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.06f);
            micro.seed = 441;

            var addSecondary = new CombineNode();
            addSecondary.mode = CombineNode.Mode.Add;
            addSecondary.backgroundMultiplier = 1f;
            addSecondary.foregroundMultiplier = 0.45f;

            var addMicro = new CombineNode();
            addMicro.mode = CombineNode.Mode.Add;
            addMicro.backgroundMultiplier = 1f;
            addMicro.foregroundMultiplier = 0.55f;

            // Mostly high plains with real vertical variation — not a constant mesa top.
            var shelfCurve = new CurveNode();
            shelfCurve.curve = new AnimationCurve(
                new Keyframe(0f, 0.25f, 0f, 0.4f),
                new Keyframe(0.3f, 0.48f, 0.35f, 0.35f),
                new Keyframe(0.55f, 0.68f, 0.25f, 0.25f),
                new Keyframe(0.78f, 0.88f, 0.3f, 0.3f),
                new Keyframe(1f, 1f, 0f, 0f));

            var remap = new RemapNode();
            remap.min = ExpectedRemapMin;
            remap.max = ExpectedRemapMax;

            var heightOut = new HeightOutputNode();

            // --- Texture weights (slope + thin low-shelf shore) ---
            var soilSlope = new SlopeMaskNode();
            soilSlope.minAngle = 6f;
            soilSlope.maxAngle = 28f;
            soilSlope.transition = Utilities.EaseInOutCurve();

            var rockSlope = new SlopeMaskNode();
            rockSlope.minAngle = 18f;
            rockSlope.maxAngle = 50f;
            rockSlope.transition = Utilities.EaseInOutCurve();

            // Soft shore hint near remap floor (C# coast paint is SoT for sand).
            var shoreCurve = new CurveNode();
            shoreCurve.curve = new AnimationCurve(
                new Keyframe(0.55f, 1f, 0f, 0f),
                new Keyframe(0.65f, 0.45f, -2f, -2f),
                new Keyframe(0.75f, 0.05f, -1f, -1f),
                new Keyframe(0.85f, 0f, 0f, 0f),
                new Keyframe(1f, 0f, 0f, 0f));

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

            var shoreOut = new TextureOutputNode();
            shoreOut.order = 3;
            shoreOut.terrainLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(ShoreLayerPath);

            graph.AddNode(macroShelves);
            graph.AddNode(secondary);
            graph.AddNode(micro);
            graph.AddNode(addSecondary);
            graph.AddNode(addMicro);
            graph.AddNode(shelfCurve);
            graph.AddNode(remap);
            graph.AddNode(heightOut);
            graph.AddNode(soilSlope);
            graph.AddNode(rockSlope);
            graph.AddNode(shoreCurve);
            graph.AddNode(weightBlend);
            graph.AddNode(grassOut);
            graph.AddNode(soilOut);
            graph.AddNode(rockOut);
            graph.AddNode(shoreOut);

            // Height path
            Connect(graph, macroShelves.id, macroShelves.outputSlot.id, addSecondary.id, addSecondary.backgroundSlot.id);
            Connect(graph, secondary.id, secondary.outputSlot.id, addSecondary.id, addSecondary.foregroundSlot.id);
            Connect(graph, addSecondary.id, addSecondary.outputSlot.id, addMicro.id, addMicro.backgroundSlot.id);
            Connect(graph, micro.id, micro.outputSlot.id, addMicro.id, addMicro.foregroundSlot.id);
            Connect(graph, addMicro.id, addMicro.outputSlot.id, shelfCurve.id, shelfCurve.inputSlot.id);
            Connect(graph, shelfCurve.id, shelfCurve.outputSlot.id, remap.id, remap.inputSlot.id);
            Connect(graph, remap.id, remap.outputSlot.id, heightOut.id, heightOut.inputSlot.id);

            // Texture masks from final height
            Connect(graph, remap.id, remap.outputSlot.id, soilSlope.id, soilSlope.inputSlot.id);
            Connect(graph, remap.id, remap.outputSlot.id, rockSlope.id, rockSlope.inputSlot.id);
            Connect(graph, remap.id, remap.outputSlot.id, shoreCurve.id, shoreCurve.inputSlot.id);

            // WeightBlend: L0 grass (default white), L1 soil, L2 rock, L3 shore
            Connect(graph, soilSlope.id, soilSlope.outputSlot.id, weightBlend.id, weightBlend.inputSlots[1].id);
            Connect(graph, rockSlope.id, rockSlope.outputSlot.id, weightBlend.id, weightBlend.inputSlots[2].id);
            Connect(graph, shoreCurve.id, shoreCurve.outputSlot.id, weightBlend.id, weightBlend.inputSlots[3].id);

            Connect(graph, weightBlend.id, weightBlend.outputSlots[0].id, grassOut.id, grassOut.inputSlot.id);
            Connect(graph, weightBlend.id, weightBlend.outputSlots[1].id, soilOut.id, soilOut.inputSlot.id);
            Connect(graph, weightBlend.id, weightBlend.outputSlots[2].id, rockOut.id, rockOut.inputSlot.id);
            Connect(graph, weightBlend.id, weightBlend.outputSlots[3].id, shoreOut.id, shoreOut.inputSlot.id);

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
