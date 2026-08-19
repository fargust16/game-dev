#if UNITY_EDITOR
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;
using SurfaceLayer = GameDev.EditorTools.OverworldTerrainLayers.SurfaceLayer;

namespace GameDev.EditorTools
{
    /// <summary>
    /// One mountain massif as a <b>height delta</b> for Raise blend, built on the architecture of
    /// Vista's own <c>Mountain_TerrainGraphTemplate</c>: two noise scales → shaping → hydraulic
    /// erosion, then surface masks derived from the <c>Height Map</c> / <c>ErosionMask</c> variables.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The massif is additive geology on top of <c>LPB_IslandBase</c>, so the graph must stay near 0
    /// at the periphery — otherwise Raise stamps a plateau instead of foothills. That is what the
    /// foothill curve does; it sits where the reference graph has a plain <c>Levels</c>.
    /// </para>
    /// <para>
    /// Everything works in the graph's normalized 0..1 delta height. How many metres that is comes
    /// from the biome's <c>Set Biome Height On Spawn</c> value on the template prefab, so a change
    /// there rescales the massif without touching this file.
    /// </para>
    /// <para>
    /// The asset is authored once and then owned by the designer. The bake only ever
    /// <see cref="Load"/>s it; <see cref="Rebuild"/> is an explicit, confirmed menu action.
    /// </para>
    /// </remarks>
    public static class MountainMassifGraphBuilder
    {
        public const string GraphAssetPath = "Assets/Settings/Vista/MountainMassif_TerrainGraph.asset";

        /// <summary>Foothill skirt: the bottom slice of the massif, where green can grow.</summary>
        private const float FoothillTop01 = 0.28f;

        /// <summary>Weight Blend priority slots. One per canonical surface layer.</summary>
        private const int WeightBlendLayers = OverworldTerrainLayers.CanonicalLayerCount;

        [MenuItem("GameDev/Overworld/Vista/Rebuild Mountain Massif Graph (destructive)")]
        public static void RebuildMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild the mountain massif terrain graph?",
                    "This re-authors MountainMassif_TerrainGraph from code, in place. Every edit made in " +
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
            Debug.Log($"[MountainMassif] Rebuilt graph at {GraphAssetPath}");
        }

        /// <summary>Loads the authored graph. Never rebuilds — the asset is the source of truth.</summary>
        public static TerrainGraph Load()
        {
            var graph = AssetDatabase.LoadAssetAtPath<TerrainGraph>(GraphAssetPath);
            if (graph == null)
            {
                Debug.LogError(
                    $"[MountainMassif] No terrain graph at {GraphAssetPath}. " +
                    "Run GameDev/Overworld/Vista/Rebuild Mountain Massif Graph (destructive) once.");
            }

            return graph;
        }

        /// <summary>Re-authors the asset from code, discarding any Inspector edits.</summary>
        /// <summary>Clones Vista's Mountains template onto the massif graph asset.</summary>
        /// <remarks>
        /// This graph was the last thing still hand-authored, and it was the banding. The island was
        /// switched to a byte-for-byte clone of the Wizard template and rendered clean; the massif
        /// kept calling AddDetailNoise, which carries flipSign and applyRemapPerLayer -- the two
        /// settings measured to draw the axis-aligned hatch -- so a single world showed the new graph
        /// and the old one side by side, and only the massif striped.
        /// </remarks>
        public static TerrainGraph Rebuild()
        {
            return VistaGraphBuild.RebuildFromTemplate(
                VistaGraphBuild.MountainTemplateGraphPath,
                GraphAssetPath,
                // No ground cover: the massif is rock and scree above the treeline, and grass on it
                // was Vista's, not ours.
                graph => VistaGraphBuild.RetargetTemplateVegetation(
                    graph, VistaVegetationTemplates.Species.Pine, includeGroundCover: false));
        }

        /// <summary>
        /// The reference biome's height recipe, with only the feature scales changed.
        /// </summary>
        /// <remarks>
        /// Detail noise × broad mass noise → the reference's <c>Levels</c> → hydraulic erosion. Every
        /// node keeps the reference's own values; the massif's elongated footprint comes from the LPB
        /// anchors and falloff mask, and its height from Set Biome Height On Spawn.
        /// </remarks>
        private static void BuildTerrainStage(TerrainGraph graph)
        {
            var ridges = VistaGraphBuild.AddDetailNoise(graph, scale: 750f, seed: 17);
            var mass = VistaGraphBuild.AddMassNoise(graph, scale: 1000f, seed: 91);
            var fine = VistaGraphBuild.AddFineNoise(
                graph, scale: VistaGraphBuild.FineNoiseScaleMeters, seed: 53);

            var shape = new CombineNode { mode = CombineNode.Mode.Multiply };
            graph.AddNode(shape);

            // The reference's third noise. Our 750/1000 pair already matches it exactly; this was
            // the one node still missing — see VistaGraphBuild.AddFineNoise.
            var roughen = new CombineNode { mode = CombineNode.Mode.Multiply };
            graph.AddNode(roughen);

            var levels = VistaGraphBuild.AddReferenceLevels(graph);

            // Two passes, as the reference has: macro drainage, then fine detail. The erosion mask
            // comes off the second one, which is far less eager to call terrain "bare soil".
            var macroErosion = VistaGraphBuild.AddMacroErosion(graph);
            var microErosion = VistaGraphBuild.AddMicroErosion(graph);

            var setHeight = VistaGraphBuild.AddSetVar(graph, VistaGraphBuild.VarHeightMap);
            var heightOut = new HeightOutputNode();
            graph.AddNode(heightOut);

            VistaGraphBuild.Connect(graph, ridges.id, ridges.outputSlot.id, shape.id, shape.backgroundSlot.id);
            VistaGraphBuild.Connect(graph, mass.id, mass.outputSlot.id, shape.id, shape.foregroundSlot.id);
            VistaGraphBuild.Connect(graph, shape.id, shape.outputSlot.id, roughen.id, roughen.backgroundSlot.id);
            VistaGraphBuild.Connect(graph, fine.id, fine.outputSlot.id, roughen.id, roughen.foregroundSlot.id);
            VistaGraphBuild.Connect(graph, roughen.id, roughen.outputSlot.id, levels.id, levels.inputSlot.id);
            VistaGraphBuild.Connect(
                graph, levels.id, levels.outputSlot.id, macroErosion.id, macroErosion.inputHeightSlot.id);
            VistaGraphBuild.Connect(
                graph, macroErosion.id, macroErosion.outputHeightSlot.id,
                microErosion.id, microErosion.inputHeightSlot.id);

            VistaGraphBuild.Connect(
                graph, microErosion.id, microErosion.outputHeightSlot.id, setHeight.id, setHeight.inputSlot.id);
            VistaGraphBuild.Connect(
                graph, setHeight.id, setHeight.outputSlot.id, heightOut.id, heightOut.inputSlot.id);

            VistaGraphBuild.PublishErosionMask(graph, microErosion);
        }

        /// <summary>
        /// The five canonical surface layers, reproducing the reference biome's mask topology.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Weight Blend stacks its inputs: output i is <c>in[i] * Π(1 - in[j])</c> for every j above
        /// i, so the <b>last</b> connected layer wins and layer 0 is the base. Layer 0 is left
        /// unconnected on purpose — Weight Blend fills it white, which is what makes the weights sum
        /// to 1. Blend priority is rock (base) → soil → grass → sand (unused) → cliff, mirroring the
        /// reference's rock → soil → moss → grass → ridge stack.
        /// </para>
        /// <para>
        /// Sand has no place on a massif, but its output is still emitted so this graph and the
        /// island graph produce the same layer stack on every tile. Its Weight Blend input stays
        /// unconnected, which Vista reads as black.
        /// </para>
        /// </remarks>
        /// <returns>The Weight Blend node; the caller picks the slot the vegetation stage reads.</returns>
        private static WeightBlendNode BuildSurfaceStage(TerrainGraph graph)
        {
            // --- Soil: sediment settles in the drainage (reference layer 1) ---
            var erosionForSoil = VistaGraphBuild.AddGetVar(graph, VistaGraphBuild.VarErosionMask);
            var soil = VistaGraphBuild.AddSoilLevels(graph);

            // --- Grass: hollows low on the massif (reference layer 2: Concave → Smooth → height) ---
            var heightForGrass = VistaGraphBuild.AddGetVar(graph, VistaGraphBuild.VarHeightMap);
            var hollows = new ConcaveNode { epsilon = 0.31f, tolerance = 3 };
            var hollowsSmooth = new SmoothNode { iterationCount = 30 };
            var heightForFoothill = VistaGraphBuild.AddGetVar(graph, VistaGraphBuild.VarHeightMap);
            var foothillBand = new CurveNode
            {
                curve = VistaGraphBuild.LowAltitudeCurve(FoothillTop01 * 0.5f, FoothillTop01),
            };
            var grass = new CombineNode { mode = CombineNode.Mode.Multiply };

            // --- Cliff: the reference's ridge highlight — invert the erosion mask, with flat
            //     ground removed first so the low-activity skirt does not read as bare stone ---
            var erosionForRidge = VistaGraphBuild.AddGetVar(graph, VistaGraphBuild.VarErosionMask);
            var ridgeErosion = new CurveNode { curve = VistaGraphBuild.ErosionPresenceCurve() };
            var heightForFlat = VistaGraphBuild.AddGetVar(graph, VistaGraphBuild.VarHeightMap);
            // Flat gate, verbatim from the reference: a constant transition, because SlopeMask
            // already hard-gates at min/max and the default plateau curve would zero out 0° ground.
            var flatGround = new SlopeMaskNode
            {
                minAngle = 0f,
                maxAngle = 3f,
                transition = VistaGraphBuild.ConstantCurve(),
            };
            var ridgeOrFlat = new CombineNode { mode = CombineNode.Mode.Max };
            var cliff = VistaGraphBuild.AddInvert(graph);

            var weightBlend = new WeightBlendNode { layerCount = WeightBlendLayers };

            graph.AddNode(hollows);
            graph.AddNode(hollowsSmooth);
            graph.AddNode(foothillBand);
            graph.AddNode(grass);
            graph.AddNode(ridgeErosion);
            graph.AddNode(flatGround);
            graph.AddNode(ridgeOrFlat);
            graph.AddNode(weightBlend);

            var outputs = VistaGraphBuild.AddCanonicalTextureOutputs(graph);

            VistaGraphBuild.Connect(
                graph, erosionForSoil.id, erosionForSoil.outputSlot.id, soil.id, soil.inputSlot.id);

            VistaGraphBuild.Connect(
                graph, heightForGrass.id, heightForGrass.outputSlot.id, hollows.id, hollows.inputSlot.id);
            VistaGraphBuild.Connect(
                graph, hollows.id, hollows.outputSlot.id, hollowsSmooth.id, hollowsSmooth.inputSlot.id);
            VistaGraphBuild.Connect(
                graph, heightForFoothill.id, heightForFoothill.outputSlot.id,
                foothillBand.id, foothillBand.inputSlot.id);
            VistaGraphBuild.Connect(
                graph, hollowsSmooth.id, hollowsSmooth.outputSlot.id, grass.id, grass.backgroundSlot.id);
            VistaGraphBuild.Connect(
                graph, foothillBand.id, foothillBand.outputSlot.id, grass.id, grass.foregroundSlot.id);

            VistaGraphBuild.Connect(
                graph, erosionForRidge.id, erosionForRidge.outputSlot.id,
                ridgeErosion.id, ridgeErosion.inputSlot.id);
            VistaGraphBuild.Connect(
                graph, heightForFlat.id, heightForFlat.outputSlot.id, flatGround.id, flatGround.inputSlot.id);
            VistaGraphBuild.Connect(
                graph, ridgeErosion.id, ridgeErosion.outputSlot.id,
                ridgeOrFlat.id, ridgeOrFlat.backgroundSlot.id);
            VistaGraphBuild.Connect(
                graph, flatGround.id, flatGround.outputSlot.id, ridgeOrFlat.id, ridgeOrFlat.foregroundSlot.id);
            VistaGraphBuild.Connect(
                graph, ridgeOrFlat.id, ridgeOrFlat.outputSlot.id, cliff.id, cliff.inputSlot.id);

            // Layer 0 (rock) intentionally unconnected — Weight Blend defaults it to white.
            VistaGraphBuild.Connect(graph, soil.id, soil.outputSlot.id, weightBlend.id, weightBlend.inputSlots[1].id);
            VistaGraphBuild.Connect(graph, grass.id, grass.outputSlot.id, weightBlend.id, weightBlend.inputSlots[2].id);
            // Weight Blend input 3 (sand) stays unconnected: black on a massif.
            VistaGraphBuild.Connect(graph, cliff.id, cliff.outputSlot.id, weightBlend.id, weightBlend.inputSlots[4].id);

            ConnectBlendToOutput(graph, weightBlend, 0, outputs, SurfaceLayer.Rock);
            ConnectBlendToOutput(graph, weightBlend, 1, outputs, SurfaceLayer.Soil);
            ConnectBlendToOutput(graph, weightBlend, 2, outputs, SurfaceLayer.Grass);
            ConnectBlendToOutput(graph, weightBlend, 3, outputs, SurfaceLayer.Sand);
            ConnectBlendToOutput(graph, weightBlend, 4, outputs, SurfaceLayer.Cliff);

            return weightBlend;
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
