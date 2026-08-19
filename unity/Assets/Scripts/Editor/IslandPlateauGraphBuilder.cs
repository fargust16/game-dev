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
                VistaGraphBuild.MountainTemplateGraphPath, GraphAssetPath, FlattenAndLift);
        }

        /// <summary>Island floor above the sea line, as a fraction of the biome's vertical extent.</summary>
        /// <remarks>
        /// Sea level is 24 m and the island LPB is 140 m tall, so 0.171 is the waterline. This sits
        /// just above it: high enough that trees do not stand in water, low enough that the coast is
        /// a beach rather than the sea wall a bigger lift produced.
        /// </remarks>
        private const float IslandFloor = 0.22f;

        /// <summary>Island ceiling. The gap to <see cref="IslandFloor"/> is all the relief there is.</summary>
        /// <remarks>
        /// The template is a mountain generator, so used raw it grows a massif in the middle of the
        /// island; the actual mountains belong to LPB_Mountain_0, which runs the same template at its
        /// own scale. Compressing to a narrow band fixed that but flattened the island into a pancake,
        /// because the thing that read as "mountain" was mostly the cliff and rock paint, not the
        /// relief. With those layers off the ground can keep real height variation, so this band is
        /// wide enough to be a landscape. The shape is the template's throughout; only its amplitude
        /// changes.
        /// </remarks>
        private const float IslandCeiling = 0.70f;

        /// <summary>
        /// Inserts one <see cref="LevelsNode"/> between the template's height chain and its output.
        /// </summary>
        private static void FlattenAndLift(TerrainGraph graph)
        {
            var outputs = graph.GetNodesOfType<HeightOutputNode>();
            if (outputs.Count == 0)
            {
                Debug.LogError("[IslandPlateau] Template has no HeightOutputNode; nothing to patch.");
                return;
            }

            var heightOut = outputs[0];
            var incoming = graph.GetEdges().Find(e => e.inputSlot.nodeId == heightOut.id);
            if (incoming == null)
            {
                Debug.LogError("[IslandPlateau] HeightOutputNode has no input edge; nothing to patch.");
                return;
            }

            var upstreamNodeId = incoming.outputSlot.nodeId;
            var upstreamSlotId = incoming.outputSlot.slotId;
            graph.RemoveEdge(incoming.id);

            var levels = new LevelsNode
            {
                inLow = 0f,
                inMid = 0.5f,
                inHigh = 1f,
                outLow = IslandFloor,
                outHigh = IslandCeiling,
            };
            graph.AddNode(levels);

            VistaGraphBuild.Connect(graph, upstreamNodeId, upstreamSlotId, levels.id, levels.inputSlot.id);
            VistaGraphBuild.Connect(
                graph, levels.id, levels.outputSlot.id, heightOut.id, heightOut.inputSlot.id);
            Debug.Log(
                $"[IslandPlateau] Height output remapped to [{IslandFloor:F2}, {IslandCeiling:F2}].");

            RemoveErosion(graph);
            SilenceRockLayers(graph);
            GreenTheWholeIsland(graph);
        }

        /// <summary>Makes every dry part of the island count as green area.</summary>
        /// <remarks>
        /// <para>
        /// The template builds GreenAreaMask from a Combine(Max) over Weight Blend slots 2 and 3 --
        /// the moss and grass it paints into drainage -- and every vegetation stage downstream reads
        /// that variable. So the ground repainted from stone stayed bare: it looked like grass but
        /// did not count as green. Removing erosion makes it worse, because both of those slots are
        /// erosion-driven and go empty, which would leave the island with no vegetation at all.
        /// </para>
        /// <para>
        /// So the mask is redefined as the biome itself, gated to dry land: the falloff ring runs 1
        /// inland and 0 outside, and AddDryLandGate cuts it at the waterline so nothing is planted
        /// in the shallows. Grass and trees then cover all of the island and none of the sea.
        /// </para>
        /// </remarks>
        private static void GreenTheWholeIsland(TerrainGraph graph)
        {
            var setGreen = graph.GetNodesOfType<SetVariableNode>()
                .Find(n => n.varName == VistaGraphBuild.VarGreenAreaMask);
            if (setGreen == null)
            {
                Debug.LogError("[IslandPlateau] Template publishes no GreenAreaMask; cannot widen it.");
                return;
            }

            foreach (var edge in graph.GetEdges().FindAll(e => e.inputSlot.nodeId == setGreen.id))
            {
                graph.RemoveEdge(edge.id);
            }

            VistaGraphBuild.PublishBiomeMask(graph);
            var mask = VistaGraphBuild.AddGetVar(graph, VistaGraphBuild.VarBiomeMask);
            var dryLand = VistaGraphBuild.AddDryLandGate(graph);
            VistaGraphBuild.Connect(
                graph, mask.id, mask.outputSlot.id, dryLand.id, dryLand.inputSlot.id);
            VistaGraphBuild.Connect(
                graph, dryLand.id, dryLand.outputSlot.id, setGreen.id, setGreen.inputSlot.id);
            Debug.Log("[IslandPlateau] GreenAreaMask widened to the whole dry biome.");
        }

        /// <summary>Takes the hydraulic erosion passes out of the island's height chain.</summary>
        /// <remarks>
        /// <para>
        /// The template is a mountain biome and erodes hard, which on flat island ground reads as
        /// streaks rather than as drainage. Each pass is spliced out: whatever fed its height input
        /// is wired to whatever its height output fed, then the node goes.
        /// </para>
        /// <para>
        /// This costs the surface stage its erosion-driven paint. The template derives three of its
        /// five layers from EroisonMask -- soil in the drainage, grass in the drainage, and the peak
        /// highlight -- and with no erosion those masks come out empty, leaving the Weight Blend's
        /// unconnected base to cover the ground. That is the intended trade here, not an oversight.
        /// </para>
        /// </remarks>
        private static void RemoveErosion(TerrainGraph graph)
        {
            var passes = graph.GetNodesOfType<HydraulicErosionNode2>();
            foreach (var pass in passes)
            {
                var edges = graph.GetEdges();
                var incoming = edges.Find(
                    e => e.inputSlot.nodeId == pass.id && e.inputSlot.slotId == pass.inputHeightSlot.id);
                if (incoming == null)
                {
                    continue;
                }

                var upstreamNode = incoming.outputSlot.nodeId;
                var upstreamSlot = incoming.outputSlot.slotId;
                var outgoing = edges.FindAll(
                    e => e.outputSlot.nodeId == pass.id &&
                         e.outputSlot.slotId == pass.outputHeightSlot.id);

                foreach (var edge in outgoing)
                {
                    VistaGraphBuild.Connect(
                        graph, upstreamNode, upstreamSlot, edge.inputSlot.nodeId, edge.inputSlot.slotId);
                }

                graph.RemoveNode(pass.id);
            }

            Debug.Log($"[IslandPlateau] Spliced out {passes.Count} hydraulic erosion pass(es).");
        }

        /// <summary>Weight Blend slots the template paints stone into.</summary>
        /// <remarks>
        /// Read off the graph, not guessed: slot 0 is grey stone and slot 4 is white rock, while 1,
        /// 2 and 3 are soil, moss and grass. An earlier pass filtered on TextureOutputNode.order --
        /// how our own builder identifies layers -- and silenced nothing, because all five of the
        /// template's outputs carry order 0. The slot feeding the output is the only discriminator.
        /// </remarks>
        private static readonly int[] RockBlendSlots = { 0, 4 };

        /// <summary>Turns the island's stone layers into ground that grass and trees grow on.</summary>
        /// <remarks>
        /// The template is a mountain biome and paints stone across anything with slope, which is
        /// what made the island read as a rock field. Bypassing those outputs would leave bare holes
        /// in the paint, so instead each one is pointed at the canonical Grass layer and its weight
        /// is folded into GreenAreaMask, which is what every vegetation stage downstream reads. The
        /// ground keeps its relief and gains cover instead of losing paint. Stone stays live on
        /// LPB_Mountain_0, which runs the same template where cliffs belong.
        /// </remarks>
        private static void SilenceRockLayers(TerrainGraph graph)
        {
            var blends = graph.GetNodesOfType<WeightBlendNode>();
            if (blends.Count == 0)
            {
                Debug.LogError("[IslandPlateau] Template has no Weight Blend; cannot retarget rock.");
                return;
            }

            var blend = blends[0];
            var grass = OverworldTerrainLayers.GetCanonicalLayer(
                OverworldTerrainLayers.SurfaceLayer.Grass);
            var edges = graph.GetEdges();
            var greened = 0;

            foreach (var slotIndex in RockBlendSlots)
            {
                if (slotIndex >= blend.outputSlots.Length)
                {
                    continue;
                }

                var slotId = blend.outputSlots[slotIndex].id;
                foreach (var edge in edges)
                {
                    if (edge.outputSlot.nodeId != blend.id || edge.outputSlot.slotId != slotId)
                    {
                        continue;
                    }

                    var target = graph.GetNode(edge.inputSlot.nodeId) as TextureOutputNode;
                    if (target == null)
                    {
                        continue;
                    }

                    target.terrainLayer = grass;
                    greened++;
                }
            }

            Debug.Log(
                $"[IslandPlateau] Repainted {greened} stone output(s) as grass "
                + $"(blend slots {string.Join(", ", RockBlendSlots)}).");
        }
    }
}
#endif
