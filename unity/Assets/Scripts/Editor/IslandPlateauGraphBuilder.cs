#if UNITY_EDITOR
using GameDev.World;
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;
using SurfaceLayer = GameDev.EditorTools.OverworldTerrainLayers.SurfaceLayer;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Rolling island shelf for <c>LPB_IslandBase</c>, built on the architecture of Vista's own
    /// <c>Mountain_TerrainGraphTemplate</c>: two noise scales → shaping → one hydraulic erosion pass,
    /// then every surface mask derived from the <c>Height Map</c> / <c>ErosionMask</c> variables.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stages talk through <see cref="VistaGraphBuild"/> variables, not through long node chains —
    /// the erosion pass is computed once and read by three separate surface masks.
    /// </para>
    /// <para>
    /// Everything here works in the graph's own normalized 0..1 height. The absolute scale is the
    /// biome's <c>Set Biome Height On Spawn</c> value, edited in the Inspector on the template
    /// prefab; no metre constant in this file depends on it.
    /// </para>
    /// <para>
    /// The asset is authored once and then owned by the designer. The bake only ever
    /// <see cref="Load"/>s it; <see cref="Rebuild"/> is an explicit, confirmed menu action because
    /// it discards every Inspector edit.
    /// </para>
    /// </remarks>
    public static class IslandPlateauGraphBuilder
    {
        public const string GraphAssetPath = "Assets/Settings/Vista/IslandPlateau_TerrainGraph.asset";

        /// <summary>
        /// The beach: the lowest slice of the graph's own height range.
        /// </summary>
        /// <remarks>
        /// Deliberately <b>not</b> anchored to sea level in metres, even though sea level is
        /// absolute. Vista multiplies every layer weight by the biome mask
        /// (<c>BiomeTextureBlend.BlendTextureWeights</c>), and the terrain the player sees is
        /// <c>mask × graphHeight</c>. The visible waterline therefore sits inside the Outer falloff
        /// ring, where the graph's own height is still high — an absolute metre band keyed on the
        /// graph height reads that as upland and paints nothing, which is exactly what the layer dump
        /// showed (sand weight 0.000 on three tiles of four).
        /// <para>
        /// A metre-accurate waterline beach needs the biome mask fed back into the graph as a texture
        /// input. Until then this band marks the graph's low ground, which the falloff then drags to
        /// the shore.
        /// </para>
        /// </remarks>
        private const float ShoreFullUntil01 = 0.03f;
        private const float ShoreZeroAt01 = 0.10f;

        /// <summary>Weight Blend priority slots. One per canonical surface layer.</summary>
        private const int WeightBlendLayers = OverworldTerrainLayers.CanonicalLayerCount;

        /// <summary>
        /// Diagnostic only: author the shaping stage with <see cref="ShelfCurve"/> replaced by a
        /// straight pass-through.
        /// </summary>
        /// <remarks>
        /// The plateau bias is the one hand-drawn curve in the height path, and it works by
        /// compressing the upper half of the distribution — which is most of the island. This flag
        /// isolates it, so a bake can answer whether that compression is what turns real relief into
        /// contour-following bands. Set it from the batch harness for one run and restore the graph
        /// assets afterwards; an island authored without the curve is not a plateau any more.
        /// </remarks>
        public static bool BypassShelfCurveForDiagnostics { get; set; }

        /// <summary>
        /// Diagnostic only: build the reference graph's height path instead of ours.
        /// </summary>
        /// <remarks>
        /// Reading <c>Mountain_TerrainGraphTemplate</c>'s wiring — as opposed to its node list, which
        /// is all anyone had checked — shows a shaping stage simpler than ours:
        /// <c>Noise(750, 8 oct) x Noise(1000, 1 oct) -> Levels -> erosion</c>. No plateau curve, and
        /// the third noise it owns feeds the **surface** stage, not height.
        /// <para>
        /// This flag swaps all three at once, because all three together are what "the reference
        /// topology" means. If it removes the banding, bisecting which one carries the effect is a
        /// separate run — bundling is what let this defect survive twelve experiments.
        /// </para>
        /// </remarks>
        public static bool ReferenceShapingForDiagnostics { get; set; }

        [MenuItem("GameDev/Overworld/Vista/Rebuild Island Plateau Graph (destructive)")]
        public static void RebuildMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild the island plateau terrain graph?",
                    "This re-authors IslandPlateau_TerrainGraph from code, in place. Every edit made in " +
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
            Debug.Log($"[IslandPlateau] Rebuilt graph at {GraphAssetPath}");
        }

        /// <summary>
        /// Loads the authored graph. Never rebuilds — the asset is the source of truth.
        /// </summary>
        public static TerrainGraph Load()
        {
            var graph = AssetDatabase.LoadAssetAtPath<TerrainGraph>(GraphAssetPath);
            if (graph == null)
            {
                Debug.LogError(
                    $"[IslandPlateau] No terrain graph at {GraphAssetPath}. " +
                    "Run GameDev/Overworld/Vista/Rebuild Island Plateau Graph (destructive) once.");
            }

            return graph;
        }

        /// <summary>Re-authors the asset from code, discarding any Inspector edits.</summary>
        public static TerrainGraph Rebuild()
        {
            VistaGraphBuild.EnsureFolder("Assets/Settings/Vista");

            var graph = ScriptableObject.CreateInstance<TerrainGraph>();
            graph.name = "IslandPlateau_TerrainGraph";

            BuildTerrainStage(graph);
            var surface = BuildSurfaceStage(graph);
            // Green source = the grass weight (Weight Blend layer 0), so canopy follows the paint.
            VistaGraphBuild.AddVegetationStage(
                graph,
                surface,
                surface.outputSlots[0].id,
                treeMaxSlopeDegrees: 28f,
                seed: 4021);

            graph.Validate();
            // Overwrite in place so the asset GUID survives and the biome template prefabs keep
            // pointing at it.
            return VistaGraphBuild.SaveGraph(graph, GraphAssetPath);
        }

        /// <summary>
        /// The reference biome's height recipe at island scale, plus one deliberate deviation.
        /// </summary>
        /// <remarks>
        /// Detail noise × broad mass noise → the reference's <c>Levels</c> → hydraulic erosion, all on
        /// the reference's own values. The one departure is <see cref="ShelfCurve"/>, which biases the
        /// distribution toward high ground so the island reads as a plateau rather than as a mountain
        /// range — that is our world design, and it is the only hand-drawn curve in the height path.
        /// </remarks>
        private static void BuildTerrainStage(TerrainGraph graph)
        {
            var reference = ReferenceShapingForDiagnostics;
            var detail = VistaGraphBuild.AddDetailNoise(
                graph, scale: reference ? 750f : 1200f, seed: 110);
            var mass = VistaGraphBuild.AddMassNoise(
                graph, scale: reference ? 1000f : 1900f, seed: 277);
            // The reference keeps its third noise out of the height path entirely — it feeds the
            // surface stage. Ours multiplies it into height, which is what cost 38 m of peak.
            var fine = reference
                ? null
                : VistaGraphBuild.AddFineNoise(
                    graph, scale: VistaGraphBuild.FineNoiseScaleMeters, seed: 431);

            var shape = new CombineNode { mode = CombineNode.Mode.Multiply };
            graph.AddNode(shape);

            CombineNode roughen = null;
            if (fine != null)
            {
                roughen = new CombineNode { mode = CombineNode.Mode.Multiply };
                graph.AddNode(roughen);
            }

            var levels = VistaGraphBuild.AddReferenceLevels(graph);

            // The plateau bias is ours alone; the reference goes straight from Levels to erosion.
            CurveNode shelf = null;
            if (!reference)
            {
                shelf = new CurveNode
                {
                    curve = BypassShelfCurveForDiagnostics
                        ? AnimationCurve.Linear(0f, 0f, 1f, 1f)
                        : ShelfCurve(),
                };
                graph.AddNode(shelf);
            }

            // Two passes, as the reference has — see the note in MountainMassifGraphBuilder.
            var macroErosion = VistaGraphBuild.AddMacroErosion(graph);
            var microErosion = VistaGraphBuild.AddMicroErosion(graph);

            var setHeight = VistaGraphBuild.AddSetVar(graph, VistaGraphBuild.VarHeightMap);
            var heightOut = new HeightOutputNode();
            graph.AddNode(heightOut);

            VistaGraphBuild.Connect(graph, detail.id, detail.outputSlot.id, shape.id, shape.backgroundSlot.id);
            VistaGraphBuild.Connect(graph, mass.id, mass.outputSlot.id, shape.id, shape.foregroundSlot.id);
            if (roughen != null)
            {
                VistaGraphBuild.Connect(
                    graph, shape.id, shape.outputSlot.id, roughen.id, roughen.backgroundSlot.id);
                VistaGraphBuild.Connect(
                    graph, fine.id, fine.outputSlot.id, roughen.id, roughen.foregroundSlot.id);
                VistaGraphBuild.Connect(
                    graph, roughen.id, roughen.outputSlot.id, levels.id, levels.inputSlot.id);
            }
            else
            {
                VistaGraphBuild.Connect(
                    graph, shape.id, shape.outputSlot.id, levels.id, levels.inputSlot.id);
            }

            // Levels -> shelf -> erosion when the plateau bias is present, Levels -> erosion when it
            // is not; either way erosion sees one upstream node.
            var beforeErosion = shelf != null ? (ImageNodeBase)shelf : levels;
            if (shelf != null)
            {
                VistaGraphBuild.Connect(
                    graph, levels.id, levels.outputSlot.id, shelf.id, shelf.inputSlot.id);
            }

            VistaGraphBuild.Connect(
                graph, beforeErosion.id,
                shelf != null ? shelf.outputSlot.id : levels.outputSlot.id,
                macroErosion.id, macroErosion.inputHeightSlot.id);
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
        /// Lifts the middle of the distribution so most of the island is high plain.
        /// </summary>
        /// <remarks>
        /// Gentle on purpose. The previous version clamped the top half flat, which is what turned
        /// the plateau into a featureless slab; this only biases, it does not clip.
        /// </remarks>
        private static AnimationCurve ShelfCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(0.45f, 0.6f, 1f, 1f),
                new Keyframe(1f, 1f, 0.55f, 0.55f));
        }

        /// <summary>
        /// The five canonical surface layers, every mask derived from terrain morphology.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Weight Blend stacks its inputs: output i is <c>in[i] * Π(1 - in[j])</c> for every j above
        /// i, so the <b>last</b> connected layer wins and layer 0 is the base. Layer 0 is left
        /// unconnected on purpose — Weight Blend fills it white, which is the only thing that makes
        /// the five weights sum to exactly 1. Blend priority here is grass (base) → rock → soil →
        /// sand → cliff.
        /// </para>
        /// <para>
        /// Altitude gates are curves over the normalized height map, not <c>HeightMaskNode</c>
        /// metres, so they keep their meaning when the biome's Set Biome Height On Spawn changes.
        /// Slope is used once, for sheer faces only: two adjacent slope bands used as independent
        /// layers draw contour rings across a rolling plateau.
        /// </para>
        /// </remarks>
        /// <returns>The grass weight, which the vegetation stage publishes as GreenAreaMask.</returns>
        private static WeightBlendNode BuildSurfaceStage(TerrainGraph graph)
        {
            // --- Rock: exposed ridges, the reference's inverted-erosion highlight ---
            var erosionForRock = VistaGraphBuild.AddGetVar(graph, VistaGraphBuild.VarErosionMask);
            var rockErosion = new CurveNode { curve = VistaGraphBuild.ErosionPresenceCurve() };

            // Flat ground is not "un-eroded ridge", it is just flat. Combining it in before the
            // invert removes the false rock the low-activity plains would otherwise pick up.
            var heightForFlat = VistaGraphBuild.AddGetVar(graph, VistaGraphBuild.VarHeightMap);
            var flatGround = new SlopeMaskNode
            {
                minAngle = 0f,
                maxAngle = 4f,
                transition = VistaGraphBuild.ConstantCurve(),
            };
            var ridgeOrFlat = new CombineNode { mode = CombineNode.Mode.Max };
            var rock = VistaGraphBuild.AddInvert(graph);

            // --- Soil: sediment settles in the drainage lines ---
            var erosionForSoil = VistaGraphBuild.AddGetVar(graph, VistaGraphBuild.VarErosionMask);
            var soil = VistaGraphBuild.AddSoilLevels(graph);

            // --- Sand: the low ground the coast forms on ---
            var heightForShore = VistaGraphBuild.AddGetVar(graph, VistaGraphBuild.VarHeightMap);
            var shoreBand = new CurveNode
            {
                curve = VistaGraphBuild.LowAltitudeCurve(ShoreFullUntil01, ShoreZeroAt01),
            };

            // Breaks the contour line a plain altitude band would otherwise draw around the island.
            var sandBreakup = new NoiseNode
            {
                scale = 95f,
                layerCount = 2,
                seed = 613,
                remapCurve = AnimationCurve.Linear(0f, 0.45f, 1f, 1f),
            };
            var sand = new CombineNode { mode = CombineNode.Mode.Multiply };

            // --- Cliff: sheer faces only, on top of everything else ---
            // The band runs to 90° because that is the physical limit, not a design choice; the curve
            // fades it in between 38° and ~62° and then holds, so vertical faces stay cliff.
            var heightForCliff = VistaGraphBuild.AddGetVar(graph, VistaGraphBuild.VarHeightMap);
            var cliff = new SlopeMaskNode
            {
                minAngle = 38f,
                maxAngle = 90f,
                transition = VistaGraphBuild.RisingBandCurve(0.46f),
            };

            var weightBlend = new WeightBlendNode { layerCount = WeightBlendLayers };

            graph.AddNode(rockErosion);
            graph.AddNode(flatGround);
            graph.AddNode(ridgeOrFlat);
            graph.AddNode(shoreBand);
            graph.AddNode(sandBreakup);
            graph.AddNode(sand);
            graph.AddNode(cliff);
            graph.AddNode(weightBlend);

            var outputs = VistaGraphBuild.AddCanonicalTextureOutputs(graph);

            VistaGraphBuild.Connect(
                graph, erosionForRock.id, erosionForRock.outputSlot.id, rockErosion.id, rockErosion.inputSlot.id);
            VistaGraphBuild.Connect(
                graph, heightForFlat.id, heightForFlat.outputSlot.id, flatGround.id, flatGround.inputSlot.id);
            VistaGraphBuild.Connect(
                graph, rockErosion.id, rockErosion.outputSlot.id, ridgeOrFlat.id, ridgeOrFlat.backgroundSlot.id);
            VistaGraphBuild.Connect(
                graph, flatGround.id, flatGround.outputSlot.id, ridgeOrFlat.id, ridgeOrFlat.foregroundSlot.id);
            VistaGraphBuild.Connect(
                graph, ridgeOrFlat.id, ridgeOrFlat.outputSlot.id, rock.id, rock.inputSlot.id);

            VistaGraphBuild.Connect(
                graph, erosionForSoil.id, erosionForSoil.outputSlot.id, soil.id, soil.inputSlot.id);

            VistaGraphBuild.Connect(
                graph, heightForShore.id, heightForShore.outputSlot.id, shoreBand.id, shoreBand.inputSlot.id);
            VistaGraphBuild.Connect(graph, shoreBand.id, shoreBand.outputSlot.id, sand.id, sand.backgroundSlot.id);
            VistaGraphBuild.Connect(
                graph, sandBreakup.id, sandBreakup.outputSlot.id, sand.id, sand.foregroundSlot.id);

            VistaGraphBuild.Connect(
                graph, heightForCliff.id, heightForCliff.outputSlot.id, cliff.id, cliff.inputSlot.id);

            // Layer 0 (grass) intentionally unconnected — Weight Blend defaults it to white.
            VistaGraphBuild.Connect(graph, rock.id, rock.outputSlot.id, weightBlend.id, weightBlend.inputSlots[1].id);
            VistaGraphBuild.Connect(graph, soil.id, soil.outputSlot.id, weightBlend.id, weightBlend.inputSlots[2].id);
            VistaGraphBuild.Connect(graph, sand.id, sand.outputSlot.id, weightBlend.id, weightBlend.inputSlots[3].id);
            VistaGraphBuild.Connect(graph, cliff.id, cliff.outputSlot.id, weightBlend.id, weightBlend.inputSlots[4].id);

            ConnectBlendToOutput(graph, weightBlend, 0, outputs, SurfaceLayer.Grass);
            ConnectBlendToOutput(graph, weightBlend, 1, outputs, SurfaceLayer.Rock);
            ConnectBlendToOutput(graph, weightBlend, 2, outputs, SurfaceLayer.Soil);
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
