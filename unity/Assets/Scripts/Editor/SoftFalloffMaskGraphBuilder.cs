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

        /// <summary>The same treatment at shelf scale.</summary>
        /// <remarks>
        /// Edge wobble is only wobble relative to the thing it wobbles. 900 m of noise across a
        /// 2400 m island is a handful of bays; across a 220 m shelf it is a constant, and the first
        /// shelf bake came out as clean ovals - hockey pucks on a lawn. The wavelength has to scale
        /// with the biome, so shelves get their own mask graph rather than sharing the island's.
        /// </remarks>
        public const string ShelfGraphAssetPath =
            "Assets/Settings/Vista/SoftFalloffShelf_BiomeMaskGraph.asset";

        /// <summary>Edge-wobble wavelength for island and massif biomes, in metres.</summary>
        private const float IslandDetailScaleMetres = 900f;

        /// <summary>Edge-wobble wavelength for shelf biomes, in metres.</summary>
        /// <remarks>
        /// Roughly a quarter of the smallest shelf's footprint, so an Outcrop gets a few lobes
        /// rather than one. Finer than this and the rim turns to static, which at 0.25 m/px the
        /// canvas will happily resolve and the eye will read as noise rather than rock.
        /// </remarks>
        private const float ShelfDetailScaleMetres = 55f;

        /// <summary>How far the detail is allowed to push the soft edge, as a fraction.</summary>
        private const float IslandDetailMultiplier = 0.28f;

        /// <summary>Shelves can take a stronger bite: their falloff ring is metres, not hundreds.</summary>
        private const float ShelfDetailMultiplier = 0.45f;

        [MenuItem("GameDev/Overworld/Vista/Rebuild Soft Falloff Mask Graph (destructive)")]
        public static void RebuildMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild the soft falloff biome mask graph?",
                    "This re-authors SoftFalloff_BiomeMaskGraph from code, in place. Every edit made in " +
                    "the Vista graph editor is lost; references to the asset are kept.",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            var graph = Rebuild();
            if (graph == null)
                return;
            Selection.activeObject = graph;
            EditorGUIUtility.PingObject(graph);
            Debug.Log($"[SoftFalloffMask] Rebuilt at {GraphAssetPath}");
        }

        /// <summary>Loads the authored mask graph. Never rebuilds — the asset is the source of truth.</summary>
        public static BiomeMaskGraph Load()
        {
            var graph = AssetDatabase.LoadAssetAtPath<BiomeMaskGraph>(GraphAssetPath);
            if (graph == null)
            {
                Debug.LogWarning(
                    $"[SoftFalloffMask] No mask graph at {GraphAssetPath} — biome edges will be hard. " +
                    "Run GameDev/Overworld/Vista/Rebuild Soft Falloff Mask Graph (destructive) once.");
            }

            return graph;
        }

        /// <summary>Re-authors the asset from code, discarding any Inspector edits.</summary>
        public static BiomeMaskGraph Rebuild()
        {
            return Rebuild(
                "SoftFalloff_BiomeMaskGraph",
                GraphAssetPath,
                IslandDetailScaleMetres,
                IslandDetailMultiplier,
                seed: 71);
        }

        /// <summary>Re-authors the shelf-scale variant.</summary>
        public static BiomeMaskGraph RebuildShelf()
        {
            return Rebuild(
                "SoftFalloffShelf_BiomeMaskGraph",
                ShelfGraphAssetPath,
                ShelfDetailScaleMetres,
                ShelfDetailMultiplier,
                seed: 613);
        }

        /// <summary>Loads the shelf-scale mask graph.</summary>
        public static BiomeMaskGraph LoadShelf()
        {
            var graph = AssetDatabase.LoadAssetAtPath<BiomeMaskGraph>(ShelfGraphAssetPath);
            if (graph == null)
            {
                Debug.LogWarning(
                    $"[SoftFalloffMask] No shelf mask graph at {ShelfGraphAssetPath} - shelf rims "
                    + "will be clean ovals. Run First-Time Setup.");
            }

            return graph;
        }

        private static BiomeMaskGraph Rebuild(
            string graphName, string assetPath, float detailScale, float detailMultiplier, int seed)
        {
            EnsureFolder("Assets/Settings");
            EnsureFolder("Assets/Settings/Vista");

            var graph = ScriptableObject.CreateInstance<BiomeMaskGraph>();
            graph.name = graphName;

            var input = new InputNode();
            input.inputName = GraphConstants.BIOME_MASK_INPUT_NAME;
            input.SetSlotType(typeof(MaskSlot));

            // Large-scale edge wobble — not micro noise that looks like static.
            var detailNoise = new NoiseNode();
            detailNoise.scale = detailScale;
            detailNoise.layerCount = 3;
            detailNoise.lacunarity = 2.1f;
            detailNoise.persistence = 0.4f;
            detailNoise.warpMode = NoiseNode.WarpMode.Angular;
            detailNoise.warpIntensity = 1.2f;
            detailNoise.remapCurve = AnimationCurve.Linear(0f, 0.15f, 1f, 0.85f);
            detailNoise.seed = seed;

            var falloffDetail = new FalloffDetailNode();
            // Keep mild — strong detail can push the soft edge past the world rim.
            falloffDetail.detailMultiplier = detailMultiplier;

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
            // Overwrite in place so the asset GUID survives — see VistaGraphBuild.SaveGraph.
            return VistaGraphBuild.SaveGraph(graph, assetPath);
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
