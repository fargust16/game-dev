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

        /// <summary>
        /// Replaces the per-octave remap with a single one applied after the octaves are summed.
        /// </summary>
        /// <remarks>
        /// `applyRemapPerLayer` runs every octave through the remap curve *before* it is summed,
        /// and that is what draws the visible axis-aligned striping. Simply turning it off drops
        /// the curve entirely (the shader never applies it otherwise) and lets the un-flipped
        /// 8-octave sum run past 1.0, so the plateau clips flat. This normalises the sum by the
        /// amplitude series first, then applies the same curve once, at the top of the stack.
        /// </remarks>
        public static bool PostStackNormaliseForDiagnostics { get; set; }

        /// <summary>
        /// Builds the detail noise the way Vista's own sample graph does, in the full chain.
        /// </summary>
        /// <remarks>
        /// 4 octaves, persistence 0.333, no flipSign, no per-layer remap. This is the only noise
        /// configuration that has ever measured clean here (0.975 against our 0.527), but until now
        /// it could only be reached through <see cref="NoiseProbe.ReferenceDetail"/>, which returns
        /// early and never builds a terrain. Pair it with the post-stack normalise: its amplitude
        /// series sums past 1.0 too, and an un-normalised sum clips the plateau flat.
        /// </remarks>
        public static bool ReferenceDetailNoiseForDiagnostics { get; set; }

        /// <summary>Shape of the single post-stack curve, once the octaves are summed.</summary>
        public enum PostStackCurve
        {
            /// <summary>No curve at all: the normalise alone. The control arm.</summary>
            None,

            /// <summary>
            /// The original per-octave curve, (0.1,0)->(1,1) with flat tangents.
            /// </summary>
            /// <remarks>
            /// Not a normalisation but a contrast amplifier: it zeroes everything under 0.1 and
            /// stretches the middle, which lifts the sum's residual directional bias back into
            /// view. Measured to reintroduce the striping even applied once.
            /// </remarks>
            Shelf,

            /// <summary>
            /// Monotonic (0,0)->(1,1) with flat tangents: a smoothstep. Restores mid-range contrast
            /// without a dead shelf, so nothing is clipped to a constant.
            /// </summary>
            Smooth,
        }

        /// <summary>Which curve <see cref="PostStackNormaliseForDiagnostics"/> applies.</summary>
        public static PostStackCurve PostStackCurveForDiagnostics { get; set; } = PostStackCurve.Shelf;


        /// <summary>
        /// Diagnostic only: publish a raw noise signal as the height map, bypassing everything else.
        /// </summary>
        /// <remarks>
        /// Isolates the noise source from `Levels`, `ShelfCurve` and both erosion passes, so the
        /// banding can be measured on the signal that produces it rather than on the finished
        /// terrain. Surface and vegetation stages still run and still read `Height Map`; their output
        /// is meaningless in this mode and is not to be looked at.
        /// </remarks>
        public enum NoiseProbe
        {
            /// <summary>Normal graph.</summary>
            Off,
            /// <summary>The 8-octave detail noise alone.</summary>
            Detail,
            /// <summary>The single-octave mass noise alone.</summary>
            Mass,
            /// <summary>Detail x Mass, which is what the shaping stage actually starts from.</summary>
            Product,
            /// <summary>Detail with the sample graph's settings: 4 octaves, no flipSign, no remap.</summary>
            ReferenceDetail,

            /// <summary>Output of `Levels`, i.e. the product after the amplitude normalise.</summary>
            AfterLevels,

            /// <summary>Output of `ShelfCurve`, i.e. the shaping stage complete, before erosion.</summary>
            AfterShelf,

            /// <summary>Output of the macro erosion pass, before the micro pass.</summary>
            AfterMacroErosion,
        }

        /// <summary>Diagnostic only. See <see cref="NoiseProbe"/>.</summary>
        public static NoiseProbe NoiseProbeForDiagnostics { get; set; } = NoiseProbe.Off;

        /// <summary>Diagnostic only: overrides the detail noise scale for the scale sweep.</summary>
        public static float? DetailNoiseScaleForDiagnostics { get; set; }

        /// <summary>Diagnostic only: translates the noise sample position, to test lattice locking.</summary>
        public static UnityEngine.Vector2? NoiseOffsetForDiagnostics { get; set; }

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

        /// <summary>Clones Vista's Mountains template onto the island graph asset.</summary>
        /// <remarks>
        /// <para>
        /// STEP 1 of the additive rebuild: the template goes in untouched -- its own height chain,
        /// its own surface stage, its own vegetation -- and only the LPB wrapper around it is ours.
        /// That isolates one question: does our biome footprint, falloff mask and height budget band
        /// a graph that measures clean on its own? Compare the dump against
        /// Assets/Plans/VISTA_REFERENCE_BASELINE.md before adding anything else.
        /// </para>
        /// <para>
        /// The island silhouette now comes from the LPB mask alone. The plateau bias that used to
        /// live in the height path (ShelfCurve) is gone with the rest of the hand-authored chain.
        /// </para>
        /// </remarks>
        public static TerrainGraph Rebuild()
        {
            return VistaGraphBuild.RebuildFromTemplate(
                VistaGraphBuild.AuthoredIslandGraphPath, GraphAssetPath, RetargetVegetation);
        }

        /// <summary>
        /// Inserts one <see cref="LevelsNode"/> between the template's height chain and its output.
        /// </summary>
        /// <summary>Lowest ground vegetation is allowed on, in the biome's own metres.</summary>
        /// <remarks>
        /// Sea level is 24 m, and between the waterline and this the shore is bare sand. The gate
        /// belongs on the vegetation outputs and not on the Weight Blend layers: the layers paint the
        /// beach, and that painting is wanted -- it is only the grass and trees standing in it that
        /// are not.
        /// </remarks>
        private const float VegetationFloorMetres = 24f;

        /// <summary>Highest ground vegetation is allowed on. Above the biome, i.e. no ceiling.</summary>
        private const float VegetationFloorTopMetres = 40f;

        /// <summary>
        /// Multiplies every vegetation output by one height mask, so nothing grows on the sand.
        /// </summary>
        /// <remarks>
        /// The authored graph gates nothing by height on the vegetation side. Its three height masks
        /// sit inside Weight Blend layers 2, 3 and 4, they all start at 0, and they cut from the top
        /// -- they keep paint off the peaks, not off the beach. Trees read GreenAreaMask and
        /// DarkGreenAreaMask, grass reads the two added together, and none of those chains touch the
        /// height map at all.
        /// </remarks>
        private static void GateVegetationByHeight(TerrainGraph graph)
        {
            var heightVar = VistaGraphBuild.AddGetVar(graph, VistaGraphBuild.VarHeightMap);
            var aboveSand = new HeightMaskNode
            {
                minHeight = VegetationFloorMetres,
                maxHeight = VegetationFloorTopMetres,

                // The curve is the mask, not a falloff applied to one: min and max only say which
                // height band the curve is stretched across, and the value outside the band is the
                // clamped end of the curve. A constant 1 therefore passes everything, which is why
                // two earlier bakes came out bit-identical to having no gate at all. A 0-to-1 ramp
                // gives bare sand below the waterline band and full vegetation above it.
                transition = AnimationCurve.Linear(0f, 0f, 1f, 1f),
            };
            graph.AddNode(aboveSand);
            VistaGraphBuild.Connect(
                graph, heightVar.id, heightVar.outputSlot.id, aboveSand.id, aboveSand.inputSlot.id);

            // The gate goes on the density, never after a Scatter: a Scatter emits a point buffer
            // rather than a mask, and multiplying that by a height mask does not thin the trees, it
            // corrupts them -- a first attempt gated the Tree Outputs and lost nine tenths of the
            // trees and a whole species while the grass, which has no Scatter in its density path,
            // came through untouched.
            var gated = 0;
            foreach (var output in graph.GetNodes())
            {
                if (!(output is ScatterNode || output is DetailDensityOutputNode))
                {
                    continue;
                }

                var incoming = graph.GetEdges().Find(e => e.inputSlot.nodeId == output.id);
                if (incoming == null)
                {
                    continue;
                }

                var upstreamNode = incoming.outputSlot.nodeId;
                var upstreamSlot = incoming.outputSlot.slotId;
                var inputSlot = incoming.inputSlot.slotId;
                graph.RemoveEdge(incoming.id);

                var gate = new CombineNode { mode = CombineNode.Mode.Multiply };
                graph.AddNode(gate);
                VistaGraphBuild.Connect(
                    graph, upstreamNode, upstreamSlot, gate.id, gate.backgroundSlot.id);
                VistaGraphBuild.Connect(
                    graph, aboveSand.id, aboveSand.outputSlot.id, gate.id, gate.foregroundSlot.id);
                VistaGraphBuild.Connect(graph, gate.id, gate.outputSlot.id, output.id, inputSlot);
                gated++;
            }

            Debug.Log(
                $"[IslandPlateau] {gated} vegetation output(s) gated above "
                + $"{VegetationFloorMetres:F0}-{VegetationFloorTopMetres:F0} m.");
        }

        private static void RetargetVegetation(TerrainGraph graph)
        {
            GateVegetationByHeight(graph);
            // The authored graph already carries the height Levels, the grass-painted stone slots and
            // the two green zones. All that is left is whose trees grow in them.
            VistaGraphBuild.RetargetTemplateVegetation(
                graph,
                new[]
                {
                    VistaVegetationTemplates.Species.Fruit,
                    VistaVegetationTemplates.Species.Pine,
                });
        }

    }
}
#endif
