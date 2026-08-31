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
        private const float VegetationFloorMetres = 15f;

        /// <summary>Highest ground vegetation is allowed on. Above the biome, i.e. no ceiling.</summary>
        private const float VegetationFloorTopMetres = 22f;

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
        /// <summary>New floor for the island's height Levels, as a fraction of the 140 m biome.</summary>
        /// <remarks>
        /// The authored graph maps its height to [0.22, 0.70], a floor of 31 m against a 24 m
        /// waterline. The falloff ring therefore has to climb 31 m in its 100 m, which is the steep
        /// coast. Dropping the floor shortens that climb without touching the ceiling, so the
        /// interior keeps the relief it has -- compressing both ends is what produced a pancake
        /// last time.
        /// </remarks>
        private const float IslandFloor = 0.14f;

        /// <summary>The ceiling the authored graph carries, used to find the node to edit.</summary>
        private const float AuthoredIslandCeiling = 0.70f;

        /// <summary>Output floor the template's shaping Levels ships - how the node is found.</summary>
        private const float ShapingFloor = 0.05f;

        /// <summary>Lowers the floor of the island's height Levels, leaving its ceiling alone.</summary>
        private static void LowerIslandFloor(TerrainGraph graph)
        {
            var levels = graph.GetNodesOfType<LevelsNode>()
                .Find(n => Mathf.Approximately(n.outHigh, AuthoredIslandCeiling));
            if (levels == null)
            {
                Debug.LogError(
                    "[IslandPlateau] No height Levels with the authored ceiling; floor not lowered.");
                return;
            }

            var was = levels.outLow;
            levels.outLow = IslandFloor;
            Debug.Log(
                $"[IslandPlateau] Island floor {was:F2} -> {IslandFloor:F2} "
                + $"({was * 140f:F0} m -> {IslandFloor * 140f:F0} m against a 24 m waterline).");
        }

        /// <summary>Height band the authored graph's own layer masks are moved onto.</summary>
        /// <remarks>
        /// They ship at 0-150 and 0-100, which is above everything the island reaches, so they
        /// never bite -- which is why grass and trees were standing in the water however the
        /// vegetation gate was set. Moving them onto the band the terrain actually occupies is what
        /// makes any height rule in this graph mean something.
        /// </remarks>
        private const float LayerMaskFloor = 15f;

        private const float LayerMaskCeiling = 50f;

        /// <summary>Moves the template's layer height masks onto the band the island occupies.</summary>
        private static void LowerLayerHeightMasks(TerrainGraph graph)
        {
            var moved = 0;
            foreach (var mask in graph.GetNodesOfType<HeightMaskNode>())
            {
                mask.minHeight = LayerMaskFloor;
                mask.maxHeight = LayerMaskCeiling;
                moved++;
            }

            Debug.Log(
                $"[IslandPlateau] Moved {moved} layer height mask(s) to "
                + $"{LayerMaskFloor:F0}-{LayerMaskCeiling:F0} m.");
        }

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
            // The targets are collected before any of them is rewired, because one of them feeds
            // another: the grass Scatter reads the pass-through output of the Detail Density
            // Output. Gating both applied the ramp to the ground cover twice, squared, which is
            // what thinned the meadow near the shore far harder than the 15-22 m band asks for.
            var targets = graph.GetNodes()
                .FindAll(n => n is ScatterNode || n is DetailDensityOutputNode);
            var targetIds = new System.Collections.Generic.HashSet<string>();
            foreach (var node in targets)
            {
                targetIds.Add(node.id);
            }

            var gated = 0;
            var skipped = 0;
            foreach (var output in targets)
            {
                var incoming = graph.GetEdges().Find(e => e.inputSlot.nodeId == output.id);
                if (incoming == null)
                {
                    continue;
                }

                if (targetIds.Contains(incoming.outputSlot.nodeId))
                {
                    skipped++;
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
                + $"{VegetationFloorMetres:F0}-{VegetationFloorTopMetres:F0} m; "
                + $"{skipped} already fed by a gated output.");
        }

        // ---------------------------------------------------------------------------------------
        // Canopy density
        // ---------------------------------------------------------------------------------------

        /// <summary>Input floor the template's canopy Levels ship with - how they are identified.</summary>
        private const float AuthoredCanopyFloor = 0.333f;

        /// <summary>Where the canopy Levels are moved to, so trees keep to the strong green zones.</summary>
        /// <remarks>
        /// The lever that turns a wood into groves. Raising the floor does not thin the canopy
        /// evenly - it deletes the weak tail of the green mask, which is the open meadow, and
        /// leaves the strong cores, which are the drainages. Scatter spacing does the opposite: a
        /// wider grid keeps the same uniform carpet with holes punched in it, and the reference
        /// screenshots are groves against open grass, not a thinner carpet.
        /// </remarks>
        private const float CanopyFloor = 0.46f;

        /// <summary>Second-stage thinning applied on top of the mask, once the groves are shaped.</summary>
        private const float CanopyDensity = 0.6f;

        /// <summary>Raises the canopy masks and thins what survives them.</summary>
        /// <remarks>
        /// Only <c>inLow</c> moves. The template's canopy Levels carry <c>inMid 0.569</c> above
        /// <c>inHigh 0.521</c> - inverted, deliberately, by whoever authored them - and touching
        /// either without knowing why would reshape the mask rather than move its threshold.
        /// </remarks>
        private static void ThinCanopy(TerrainGraph graph)
        {
            var raised = 0;
            foreach (var levels in graph.GetNodesOfType<LevelsNode>())
            {
                if (!Mathf.Approximately(levels.inLow, AuthoredCanopyFloor))
                {
                    continue;
                }

                levels.inLow = CanopyFloor;
                raised++;
            }

            var thinned = 0;
            foreach (var tree in graph.GetNodesOfType<TreeOutputNode>())
            {
                tree.densityMultiplier = CanopyDensity;
                thinned++;
            }

            Debug.Log(
                $"[IslandPlateau] Canopy: {raised} mask floor(s) {AuthoredCanopyFloor:F2} -> "
                + $"{CanopyFloor:F2}, {thinned} tree output(s) at {CanopyDensity:F2} density.");
        }

        // ---------------------------------------------------------------------------------------
        // Geological strata (Phase 1 - topology only)
        // ---------------------------------------------------------------------------------------

        /// <summary>Metres of world relief one unit of the graph's own height is worth.</summary>
        /// <remarks>
        /// <c>LPB_IslandBase</c> sits at sea level with <c>useTransformForHeightBlend</c> and the
        /// final Levels maps onto [0.14, 0.70] of a 140 m biome, so world height is
        /// <c>43.6 + 78.4v</c>. Every metre figure in this stage is converted through this, which is
        /// the only honest way to state an amplitude: the terrace stage stated its step in graph
        /// metres, and what reached the ground was an order of magnitude smaller.
        /// </remarks>
        private const float GraphSpanWorldMetres = 78.4f;

        /// <summary>Typical escarpment rise, in metres of finished world relief.</summary>
        /// <remarks>
        /// <para>
        /// This is the <b>median</b> front. The amplitude field runs from about -7 m to +29 m, so
        /// strong fronts are more than twice this and some strata invert into valleys.
        /// </para>
        /// <para>
        /// 22 m was the target and it does not fit. The island's whole vertical budget is 78.4 m of
        /// world relief and its own landform already uses about 47 m of that; 22 m median fronts
        /// with a spread reaching 2.4x the median needs another 68 m, and the sum clips at both
        /// ends - flat-topped strata over a floored valley, which is worse than a smaller step. The
        /// clean way to buy the missing amplitude is worldHeight on LPB_IslandBase, currently
        /// 140 m, and that is a prefab decision rather than a graph one. Until then the base is
        /// compressed to ~34 m and the geology takes ~12.
        /// </para>
        /// </remarks>
        private const float GeologyAmplitudeWorldMetres = 12f;

        /// <summary>Rise of each front in turn, in metres of finished world relief.</summary>
        /// <remarks>
        /// PHASE 2C.1. The accumulation is not a graph operation - it is authored into the profile
        /// curve, whose keys carry the running total. So a per-stratum amplitude costs nothing at
        /// all: the level after front k is the sum of the first k entries here, and the step the
        /// player climbs at front k is entry k on its own. No node is added and nothing downstream
        /// changes.
        /// <para>
        /// This is worth being exact about, because it also bounds what Phase 2C could ever have
        /// done. One multiplier applied to the curve's output scales every level by the same local
        /// factor, so it thickens or thins the whole sequence together and can never make front 1
        /// strong where front 3 is weak. Independent per-stratum <i>modulation</i> is a different
        /// shape of graph - see the note in the report - but independent per-stratum
        /// <i>amplitude</i> is just these numbers.
        /// </para>
        /// </remarks>
        private static readonly float[] GeologyFrontAmplitudes = { 10f, 12f, 14f };

        /// <summary>Global scale on the strata. Control bakes set it to zero to isolate the stage.</summary>
        private const float GeologyAmplitudeScale = 1f;

        /// <summary>Wavelength of the bed-thickness field, in metres.</summary>
        private const float GeologyModulationScaleMetres = 900f;

        /// <summary>Measured output floor of Perlin01 at these settings.</summary>
        /// <remarks>
        /// Measured, not assumed. Perlin01 is 1 - (perlin * 0.5 + 0.5) and its practical span is
        /// nowhere near 0..1: published as terrain and read back over 402 478 interior cells it
        /// runs 0.216 .. 0.743, median 0.559. Phase 2C sized its multiplier as if the span were
        /// 0..1 and reached the ground with 40 % of the spread it asked for. These two numbers are
        /// that measurement, and they are what makes the remap below exact.
        /// </remarks>
        private const float GeologyNoiseFloor = 0.216f;

        /// <summary>Measured output ceiling of Perlin01 at these settings.</summary>
        private const float GeologyNoiseCeiling = 0.743f;

        /// <summary>Thinnest a bed gets, as a fraction of its authored amplitude.</summary>
        private const float GeologyModulationLow = 0.85f;

        /// <summary>Thickest a bed gets, as a fraction of its authored amplitude.</summary>
        private const float GeologyModulationHigh = 1.15f;

        /// <summary>Down-dip step between the sampling points of successive beds, in metres.</summary>
        /// <remarks>
        /// One field, three sampling points. Same seed, same scale, same warp - only the world
        /// offset differs, so the three beds carry the same regional structure rather than three
        /// unrelated patterns. The offset runs down dip because that is the direction an isopach
        /// actually migrates: each bed was laid down at a different time over a basin whose
        /// depocentre had moved.
        /// <para>
        /// The fronts already sit 308 m apart on the ground, so bed k reads the field at
        /// d + k * (308 + 450) m - about 0.84 of a wavelength between neighbours, past the first
        /// zero of the autocorrelation and short of the next peak. Whether that is far enough to
        /// make the three vary independently is a measurement, not a claim.
        /// </para>
        /// </remarks>
        private const float GeologyModulationOffsetMetres = 450f;

        /// <summary>Number of strata fronts across the biome canvas.</summary>
        /// <remarks>
        /// The canvas is ~2564 m, so three fronts sit roughly 855 m apart - the low end of the
        /// 800-1000 m band the geology is aimed at. They are placed unevenly on purpose: evenly
        /// spaced fronts read as machined terraces, which is exactly the failure this stage
        /// replaces.
        /// </remarks>
        /// <remarks>
        /// Three fronts at 0.12 of the ramp apart, which on the 2564 m canvas is ~308 m - far
        /// enough that each reads as its own escarpment with real terrain between them, close
        /// enough that all three sit on the island rather than running off into the sea.
        /// </remarks>
        private static readonly float[] GeologyFronts = { 0.38f, 0.50f, 0.62f };

        /// <summary>Width of a front as a fraction of the strata ramp.</summary>
        /// <remarks>
        /// 0.006 x 2564 m = <b>15 m</b>, which is three cells of the 5.01 m/px biome canvas - the
        /// finest a front can be before it aliases - and twelve texels of the 2048-wide curve LUT,
        /// so the profile itself is not the limiting factor. A 22 m rise over 15 m of ground is
        /// 56 degrees: a face a character cannot walk up, which is the point.
        /// </remarks>
        private const float GeologyFrontWidth = 0.003f;

        /// <summary>Lateral displacement the warp may apply, as a fraction of the canvas.</summary>
        /// <remarks>
        /// <c>DirectionalWarp</c> displaces in normalized UV: <c>uv -= normal.xz * intensity</c>.
        /// 0.05 of a 2564 m canvas is ~128 m of bend against an 855 m spacing - enough that the
        /// fronts wander and are never parallel, far short of tearing them apart.
        /// </remarks>
        /// <remarks>
        /// PHASE 2B, and the only thing that changed from 2A. DirectionalWarp displaces in
        /// normalized UV - <c>uv -= normal.xz * intensity</c> - so on the 2564 m canvas 0.03 is up
        /// to <b>77 m</b> of lateral bend. That is a quarter of the 308 m spacing between fronts:
        /// enough that no two of them stay parallel, far short of the displacement that would fold
        /// one front over another and tear it into patches. Deliberately the low end; if it is not
        /// enough, the number goes up on its own, not alongside anything else.
        /// </remarks>
        private const float GeologyWarpIntensity = 0.03f;

        /// <summary>Wavelength of the field that bends the strata, in metres.</summary>
        private const float GeologyWarpScaleMetres = 700f;

        /// <summary>Wavelength of the field that decides where strata rise, fade or cut.</summary>
        private const float GeologyAmplitudeScaleMetres = 1100f;

        /// <summary>Floor of the base band. The shaping Levels ships this and it is kept.</summary>
        /// <remarks>
        /// This pair used to read [0.16, 0.60], squeezing the base into 44 % of the graph so the
        /// geological stack would have headroom. It bought the headroom and cost the island its
        /// terrain: measured over the four tiles, the ground between two scarps had a median slope
        /// of 0.0 degrees and a p10-p90 relief of 13.9 m. It was a plain.
        /// <para>
        /// Restored to the authored [0.05, 1.00], that becomes 30.6 m of relief and a median slope
        /// of 5.0 degrees, and it costs the geology nothing: the scarps keep their peak slopes
        /// (46/50/57 deg before, 44/51/56 after) and the deformation field itself differs by a
        /// median of 0.16 m, because the geological branch reads the warped ramp and its own noise
        /// and never reads the terrain. Reachability stayed at 99.7-100 %.
        /// </para>
        /// <para>
        /// The one price is the output clamp: 0.3 % of dry land reached the ceiling, in three
        /// patches, the largest 89 m across. That is a vertical-budget question, not a reason to
        /// squeeze the base again.
        /// </para>
        /// </remarks>
        private const float GeologyBaseFloor = 0.05f;

        /// <summary>Ceiling of the base band, restored to the authored value. See the floor.</summary>
        private const float GeologyBaseCeiling = 1.00f;

        /// <summary>
        /// Deforms the island's own heightfield into tilted geological strata.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Spliced between the shaping Levels and the macro erosion pass, so the two hydraulic
        /// passes integrate the fronts instead of creating them, and everything downstream -
        /// <c>Height Map</c>, <c>ErosionMask</c>, the Weight Blend, the vegetation - reads the
        /// result through the variables it already reads and needs no change at all.
        /// </para>
        /// <para>
        /// <b>This is a deformation of the terrain, not an object placed on it.</b> The previous
        /// attempt at levels used separate Raise-blended biomes: a closed anchor polygon with a
        /// uniform falloff ring, which can only ever produce a rim on every side, sitting on the
        /// terrain as <c>max(island, shelf)</c> rather than being the terrain. Here the terrain is
        /// the same continuous field throughout; the strata only bend it.
        /// </para>
        /// <para>
        /// The asymmetry lives in one place: the profile curve. Applied to a monotonic ramp, a
        /// staircase of long flats and short rises gives every front a gentle back and one steep
        /// face, all facing the same way - a cuesta. Symmetric operators (radial falloff, a Terrace
        /// remap) give mesas, which is the shape we are trying to get away from.
        /// </para>
        /// <para>
        /// The amplitude field is <b>signed</b>. Where it is positive the stratum rises; where it is
        /// negative the same stratum cuts down, and the low ground between two rising fronts becomes
        /// a valley on its own. There is no separate canyon generator, and there should not be one.
        /// </para>
        /// </remarks>
        private static void AddGeologicalStrata(TerrainGraph graph)
        {
            var shaping = graph.GetNodesOfType<LevelsNode>()
                .Find(n => Mathf.Approximately(n.outLow, ShapingFloor)
                           && Mathf.Approximately(n.outHigh, 1f));
            if (shaping == null)
            {
                Debug.LogError("[IslandPlateau] No shaping Levels; geological strata not added.");
                return;
            }

            // Spliced BETWEEN the two hydraulic passes, not ahead of them. Ahead of both is where
            // Vista's Mesa template puts its terrace and it is where this stage went first: a 60 m
            // front measured for this experiment came back invisible on the height map, leaving
            // only flow striations along the new gradient. Our macro pass runs erosionRate 10
            // against depositionRate 0.3, so it scours structure rather than filling around it, and
            // 200 iterations of that removes a 22 m escarpment entirely. The micro pass, at a fifth
            // of the erosion rate, gullies the face and leaves it standing. Measured twice now -
            // once for the terrace stage, once here. See VISTA_TERRAIN_PLAYBOOK 2.12.
            var macro = graph.GetNodesOfType<HydraulicErosionNode2>()
                .Find(n => n.featureSize >= MacroErosionFeatureSize);
            if (macro == null)
            {
                Debug.LogError("[IslandPlateau] No macro erosion pass; strata not added.");
                return;
            }

            var downstream = graph.GetEdges().FindAll(
                e => e.outputSlot.nodeId == macro.id
                     && e.outputSlot.slotId == macro.outputHeightSlot.id);
            if (downstream.Count == 0)
            {
                Debug.LogError("[IslandPlateau] Macro erosion feeds nothing; strata not added.");
                return;
            }

            var amplitude = GeologyAmplitudeWorldMetres / GraphSpanWorldMetres;

            // Trim the base into a band that leaves room both ways: a stratum that cuts needs
            // somewhere below the terrain to cut into, and the shaping Levels ships a floor of 0.05
            // that the island genuinely reaches. Editing that node rather than adding a second one
            // next to it - its whole job is the output range, and it is upstream of the erosion, so
            // the passes see a terrain shaped exactly as before, only re-ranged.
            shaping.outLow = GeologyBaseFloor;
            shaping.outHigh = GeologyBaseCeiling;

            // The dip direction. One bearing for the island, because a coherent direction over
            // kilometres is what separates geology from noise - the warp below is what stops it
            // being a ruled surface.
            var bearing = GeologyBearingRadians;
            var axis = new Vector2(Mathf.Cos(bearing), Mathf.Sin(bearing)) * 0.5f;
            var ramp = new AxialGradientNode
            {
                startPoint = new Vector2(0.5f, 0.5f) - axis,
                endPoint = new Vector2(0.5f, 0.5f) + axis,
            };
            graph.AddNode(ramp);

            var warpField = new NoiseNode
            {
                mode = NoiseMode.Perlin01,
                layerCount = 1,
                scale = GeologyWarpScaleMetres,
                flipSign = false,
                applyRemapPerLayer = false,
                seed = GeologyWarpSeed,
            };
            graph.AddNode(warpField);

            var warped = new DirectionalWarpNode { intensityMultiplier = GeologyWarpIntensity };
            graph.AddNode(warped);
            VistaGraphBuild.Connect(
                graph, ramp.id, ramp.outputSlot.id, warped.id, warped.inputSlot.id);
            VistaGraphBuild.Connect(
                graph, warpField.id, warpField.outputSlot.id, warped.id, warped.directionSlot.id);

            // PHASE 2C.2. The single cumulative curve is gone, and with it the ceiling Phase 2C
            // ran into. That curve's keys carried the running total 0 -> 10 -> 22 -> 36 m, so
            // anything multiplied into its output scaled every level by the same local factor: it
            // could thicken or thin the whole column together and nothing else. What replaces it
            // is one branch per bed -
            //
            //     contribution(k) = step(k, warped ramp) * modulation(k, world position)
            //     total           = sum over k
            //
            // - so bed 2 can be thick where bed 1 is thin. The sum stays monotonic along dip
            // because every step is positive, so the levels never cross and no front can invert.
            //
            // Note what the multiply does downstream of its own front. step(k) holds its value
            // over everything past front k, so a thick patch of bed 1 lifts the whole overlying
            // column there. That is not a side effect to be corrected: it is what an isopach does,
            // and it is why the ground between the scarps keeps its own shape while the sequence
            // above it rides up and down.
            var beds = new System.Collections.Generic.List<CurveNode>();
            var branchNodes = new System.Collections.Generic.List<ImageNodeBase>();
            CombineNode total = null;

            var dip = new Vector2(
                Mathf.Cos(GeologyBearingRadians), Mathf.Sin(GeologyBearingRadians));
            for (var i = 0; i < GeologyFronts.Length; i++)
            {
                var step = new CurveNode { curve = BuildBedProfile(i) };
                graph.AddNode(step);
                VistaGraphBuild.Connect(
                    graph, warped.id, warped.outputSlot.id, step.id, step.inputSlot.id);
                beds.Add(step);
                branchNodes.Add(step);

                // The remap is folded into the noise node itself. Utilities.TextureFromCurve bakes
                // to RGBAFloat and the graph's render targets are RFloat, so a curve that leaves
                // 1.0 is clipped nowhere - which is what makes the separate Math node this would
                // otherwise need unnecessary. Three nodes saved, and one less place for the range
                // arithmetic to be wrong in.
                var modulation = new NoiseNode
                {
                    mode = NoiseMode.Perlin01,
                    layerCount = 1,
                    scale = GeologyModulationScaleMetres,
                    flipSign = false,
                    applyRemapPerLayer = false,
                    warpMode = NoiseNode.WarpMode.Angular,
                    warpIntensity = 0.4f,
                    seed = GeologyAmplitudeSeed,
                    offset = dip * (i * GeologyModulationOffsetMetres),
                    remapCurve = BuildModulationRemap(),
                };
                graph.AddNode(modulation);
                branchNodes.Add(modulation);

                var bed = new CombineNode { mode = CombineNode.Mode.Multiply };
                graph.AddNode(bed);
                VistaGraphBuild.Connect(
                    graph, step.id, step.outputSlot.id, bed.id, bed.backgroundSlot.id);
                VistaGraphBuild.Connect(
                    graph, modulation.id, modulation.outputSlot.id, bed.id, bed.foregroundSlot.id);
                branchNodes.Add(bed);

                if (total == null)
                {
                    total = bed;
                    continue;
                }

                var stack = new CombineNode { mode = CombineNode.Mode.Add };
                graph.AddNode(stack);
                VistaGraphBuild.Connect(
                    graph, total.id, total.outputSlot.id, stack.id, stack.backgroundSlot.id);
                VistaGraphBuild.Connect(
                    graph, bed.id, bed.outputSlot.id, stack.id, stack.foregroundSlot.id);
                branchNodes.Add(stack);
                total = stack;
            }

            var deformed = new CombineNode { mode = CombineNode.Mode.Add };
            graph.AddNode(deformed);
            VistaGraphBuild.Connect(
                graph, macro.id, macro.outputHeightSlot.id, deformed.id, deformed.backgroundSlot.id);
            VistaGraphBuild.Connect(
                graph, total.id, total.outputSlot.id, deformed.id, deformed.foregroundSlot.id);

            // The stack legitimately reaches 1.0281 where a high base meets all three beds, and
            // everything downstream forces the graph value back into [0, 1] - the ClampNode here,
            // and a saturate() inside the shared Levels shader after it. Left alone that removes
            // 0.30 % of the island's dry land into three flat tops, the largest 89 m across.
            //
            // Absorbed locally instead, with two stock nodes and no package change:
            //
            //     Add -> Math(x 1/1.0285) -> Curve(knee) -> Clamp(0, 1)
            //
            // The Math exists because CurveNode's texture is sampled with wrapMode Clamp over
            // [0, 1]: fed 1.0281 directly it would return curve(1.0) and flatten the overrange,
            // which is the very thing being fixed. Scaling first puts the whole range inside the
            // curve's domain, and the curve undoes the scaling below the knee.
            //
            // Below 0.98 this is identity to float precision, not approximately: the curve bakes
            // to 2048 samples with Bilinear filtering, and linear interpolation reproduces a
            // straight line exactly. Above the knee the profile is C1 at both ends - slope exactly
            // 1 where it leaves the identity line, so no contour appears at the threshold - and
            // monotonic, so no level inverts.
            var fit = new MathNode();
            fit.maths.Clear();
            fit.maths.Add(new MathNode.MathConfig
            {
                enabled = true,
                number = 1f / OverrangeCeiling,
                ops = MathNode.Operator.Multiply,
            });
            graph.AddNode(fit);
            VistaGraphBuild.Connect(
                graph, deformed.id, deformed.outputSlot.id, fit.id, fit.inputSlot.id);

            var knee = new CurveNode { curve = BuildOverrangeKnee() };
            graph.AddNode(knee);
            VistaGraphBuild.Connect(
                graph, fit.id, fit.outputSlot.id, knee.id, knee.inputSlot.id);

            var safe = new ClampNode { min = 0f, max = 1f };
            graph.AddNode(safe);
            VistaGraphBuild.Connect(
                graph, knee.id, knee.outputSlot.id, safe.id, safe.inputSlot.id);

            // Re-point what the shaping stage fed - the macro erosion pass. The nodes above are not
            // in this snapshot, so the Add still reads the un-deformed base, which is the point.
            foreach (var edge in downstream)
            {
                var target = edge.inputSlot;
                graph.RemoveEdge(edge.id);
                VistaGraphBuild.Connect(
                    graph, safe.id, safe.outputSlot.id, target.nodeId, target.slotId);
            }

            // DIAGNOSTIC: a hardness band over the front, and nothing else.
            //
            // HydraulicErosion2 reads its Hardness input as erosionMask = 1 - hardness, and an
            // unconnected slot binds Texture2D.blackTexture, so today every cell erodes at full
            // rate. Measured: the micro pass takes the front from 12.0 m / 10.9 m wide / 53 deg
            // down to 2.4 m / 32.8 m / 27 deg - it removes four fifths of the amplitude.
            //
            // The band comes from the same warped ramp the profile does, so it tracks the front
            // exactly wherever the warp puts it, and it is deliberately narrow: full protection
            // across ~21 m centred on the 7.7 m face, with ~8 m shoulders either side. Everything
            // beyond that keeps hardness 0 and erodes normally, which is the point of the test -
            // protect the escarpment, not the plateau.
            var hardness = new CurveNode { curve = BuildFrontProtectionProfile() };
            graph.AddNode(hardness);
            VistaGraphBuild.Connect(
                graph, warped.id, warped.outputSlot.id, hardness.id, hardness.inputSlot.id);

            foreach (var erosion in graph.GetNodesOfType<HydraulicErosionNode2>())
            {
                if (erosion.featureSize >= MacroErosionFeatureSize)
                {
                    continue;
                }

                VistaGraphBuild.Connect(
                    graph, hardness.id, hardness.outputSlot.id, erosion.id, erosion.hardnessSlot.id);
                VistaExecutionProbe.Watched[erosion.id] = "micro erosion";

                // The scour branch is silenced on the micro pass. Measured, not guessed: it is a
                // step detector. On smooth ground it does almost nothing - turning it off moves the
                // base terrain by a median of 0.00 m and costs 2.6 % of the micro-relief, because
                // deposition and sediment transport carry that detail. At a sharp face it fires
                // hard, and 200 iterations of erosionAmount = 0.05 * (capacity - g) removed a 12 m
                // escarpment in full: measured 13.4 m of deformation surviving at rate 0 against
                // exactly 0 cells above 6 m at rate 5.
                //
                // Nothing else about the pass changes - deposition stays 0.1, iterations, feature
                // size and resolution are untouched, and the macro pass is not affected.
                erosion.erosionRate = 0f;
            }

            VistaExecutionProbe.Watched[hardness.id] = "hardness curve";
            for (var i = 0; i < beds.Count; i++)
            {
                VistaExecutionProbe.Watched[beds[i].id] = $"bed {i + 1} step";
            }
            VistaExecutionProbe.Watched[ramp.id] = "axial ramp";

            // PHASE 1B. The biome canvas is 2564 m at 512 px = 5.01 m/px, so a 7.7 m front is one
            // and a half cells - built from two samples and then upsampled to the tile, which
            // softens it before it is ever terrain. resolutionMultiplier is clamped to [0.1, 2]
            // and CalculateResolution applies RES_MAX only to resolutionAbsolute, so RelativeToGraph
            // x2 legally gives these nodes 1024 px = 2.50 m/px, and the same front becomes three
            // cells. That is the sharpness budget the 15 m front had, at half the width.
            var supersampled = new System.Collections.Generic.List<ImageNodeBase>
            {
                ramp, warped, hardness, deformed, safe,
            };
            supersampled.AddRange(branchNodes);
            supersampled.Add(fit);
            supersampled.Add(knee);
            foreach (var node in supersampled)
            {
                node.resolutionOverride = ResolutionOverrideOptions.RelativeToGraph;
                node.resolutionMultiplier = GeologyResolutionMultiplier;
            }

            // Nodes default to RelativeToMainInput, so both hydraulic passes are pinned explicitly
            // rather than left to inherit whatever the Clamp above hands them.
            //
            // The micro pass gets 2x and the macro pass does not, because only the micro pass runs
            // downstream of the deformation. HydraulicErosionNode2 caps its simulation at
            // min(2048, outputResolution) - the node's own resolution, not the biome's - and at 512
            // it reported "Feature Size 2 m needs a 1088 px sim canvas, above the 512 px budget.
            // Baking at 512 px (effective feature size 4.24 m per pixel)". A 7.7 m front is 1.8
            // cells of a 4.24 m grid, and 200 iterations of flow read that as a spike and level it:
            // the measured front came out 4.3 m at 15 deg instead of 12 m at 57. At 1024 the sim
            // runs at 2.50 m/px and the same front is 3.1 cells.
            //
            // No erosion parameter is touched - not featureSize, not the iteration count, not the
            // rates. Only the grid the simulation runs on.
            foreach (var erosion in graph.GetNodesOfType<HydraulicErosionNode2>())
            {
                erosion.resolutionOverride = ResolutionOverrideOptions.RelativeToGraph;
                erosion.resolutionMultiplier =
                    erosion.featureSize >= MacroErosionFeatureSize ? 1f : GeologyResolutionMultiplier;
            }

            Debug.Log(
                $"[IslandPlateau] Geological strata: {GeologyFronts.Length} front(s) at "
                + $"{string.Join(", ", System.Array.ConvertAll(GeologyFronts, f => $"{f * 2564f:F0} m"))}"
                + $", rises "
                + $"{string.Join("/", System.Array.ConvertAll(GeologyFrontAmplitudes, a => $"{a * GeologyAmplitudeScale:F0}"))}"
                + $" m over {GeologyFrontWidth * 2564f:F0} m "
                + $"({Mathf.Atan2(GeologyFrontAmplitudes[0] * GeologyAmplitudeScale, GeologyFrontWidth * 2564f) * Mathf.Rad2Deg:F0}"
                + $"-{Mathf.Atan2(GeologyFrontAmplitudes[GeologyFrontAmplitudes.Length - 1] * GeologyAmplitudeScale, GeologyFrontWidth * 2564f) * Mathf.Rad2Deg:F0} deg), "
                + $"bearing {GeologyBearingRadians * Mathf.Rad2Deg:F0} deg, geology at "
                + $"{512 * GeologyResolutionMultiplier:F0} px "
                + $"({2564f / (512 * GeologyResolutionMultiplier):F2} m/px), warp "
                + $"{GeologyWarpIntensity * 2564f:F0} m, per-bed thickness "
                + $"{GeologyModulationLow:F2}-{GeologyModulationHigh:F2}x from one "
                + $"{GeologyModulationScaleMetres:F0} m field sampled "
                + $"{GeologyModulationOffsetMetres:F0} m apart down dip, hardness band "
                + $"{(2f * FrontProtectionCore + GeologyFrontWidth) * 2564f:F0} m core + "
                + $"{FrontProtectionShoulder * 2564f:F0} m shoulders, base band "
                + $"[{GeologyBaseFloor:F2}, {GeologyBaseCeiling:F2}], "
                + $"{downstream.Count} downstream edge(s) moved.");
        }

        /// <summary>Feature size that tells the template's macro pass from its micro pass, in metres.</summary>
        /// <remarks>Macro ships at 8 m and micro at 2 m; anything at or above this is the macro pass.</remarks>
        private const int MacroErosionFeatureSize = 4;

        /// <summary>Working resolution of the geology nodes, relative to the graph request.</summary>
        /// <remarks>
        /// Clamped by <c>ImageNodeBase.resolutionMultiplier</c> to at most 2, which on a 512 px
        /// graph is 1024 px and 2.50 m/px across the island canvas. The finished terrain cannot go
        /// below 2.73 m/px - <c>VistaManager.heightMapResolution</c> is already at
        /// <c>Constants.HM_RES_MAX</c> = 513 - so 2x removes the canvas as the bottleneck and there
        /// is nothing to gain past it.
        /// </remarks>
        private const float GeologyResolutionMultiplier = 2f;

        /// <summary>Bearing of the dip direction. Fixed: the strike is the island's character.</summary>
        private const float GeologyBearingRadians = 0.62f;

        private const int GeologyWarpSeed = 5507;

        private const int GeologyAmplitudeSeed = 8821;

        /// <summary>How much of the profile range one front climbs.</summary>
        private const float StrataFrontRise = 0.78f;

        /// <summary>Level the dip slope has fallen back to by the next front.</summary>
        private const float StrataTroughLevel = 0.16f;

        /// <summary>
        /// The cuesta profile: a short steep scarp, then a long gentle dip slope back down.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This one curve carries every shape requirement of the stage. Applied to a monotonic ramp
        /// it gives each front a single steep face - all facing the same way, because the rise is
        /// always the short segment - and a back that returns to the surrounding terrain over
        /// hundreds of metres. That is the asymmetry; nothing else here is asymmetric.
        /// </para>
        /// <para>
        /// It deliberately does <b>not</b> accumulate. A monotonic staircase adds a step per front
        /// and the total climb is their sum, which blows the height budget and builds the endless
        /// stair the relief rules forbid. Falling back between fronts holds the range at 0..1
        /// however many fronts there are, and the low ground between two scarps becomes a valley
        /// without anything extra being asked for.
        /// </para>
        /// </remarks>
        /// <summary>Half-width of the fully protected band, as a fraction of the ramp.</summary>
        private const float FrontProtectionCore = 0.0025f;

        /// <summary>Width of the shoulder either side of the protected band.</summary>
        private const float FrontProtectionShoulder = 0.003f;

        /// <summary>Hardness 1 over the front, 0 everywhere else.</summary>
        /// <remarks>
        /// Built on the same coordinate as the strata profile, so the band follows the front rather
        /// than approximating where it ought to be. The shoulders are not decoration: a hard edge in
        /// the hardness map is itself a discontinuity the simulation will chase, and the sediment it
        /// carries has to be allowed to settle somewhere.
        /// </remarks>
        private static AnimationCurve BuildFrontProtectionProfile()
        {
            // Inert: hardness gates only the scour branch, and that branch is now off. Kept wired
            // and kept correct so the two do not silently disagree if scour is ever restored.
            var keys = new System.Collections.Generic.List<Keyframe>
            {
                new Keyframe(0f, 0f, 0f, 0f),
            };

            foreach (var front in GeologyFronts)
            {
                var lo = front - FrontProtectionCore;
                var hi = front + GeologyFrontWidth + FrontProtectionCore;
                keys.Add(new Keyframe(lo - FrontProtectionShoulder, 0f, 0f, 0f));
                keys.Add(new Keyframe(lo, 1f, 0f, 0f));
                keys.Add(new Keyframe(hi, 1f, 0f, 0f));
                keys.Add(new Keyframe(hi + FrontProtectionShoulder, 0f, 0f, 0f));
            }

            keys.Add(new Keyframe(1f, 0f, 0f, 0f));
            return new AnimationCurve(keys.ToArray());
        }

        /// <summary>The contribution of one bed on its own: flat, its scarp, then flat again.</summary>
        /// <remarks>
        /// Deliberately NOT cumulative. Each bed carries only its own rise, and the levels come
        /// back as the sum of the branches rather than as running totals baked into one curve.
        /// That is the whole architectural difference of this phase, and it is what lets a
        /// modulation reach one bed without touching its neighbours.
        /// </remarks>
        private static AnimationCurve BuildBedProfile(int index)
        {
            var rise = GeologyFrontAmplitudes[index] * GeologyAmplitudeScale / GraphSpanWorldMetres;
            var front = GeologyFronts[index];
            var slope = rise / GeologyFrontWidth;
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(front, 0f, 0f, slope),
                new Keyframe(front + GeologyFrontWidth, rise, slope, 0f),
                new Keyframe(1f, rise, 0f, 0f));
        }

        /// <summary>Maps the noise node's measured output range onto the thickness multiplier.</summary>
        /// <remarks>
        /// Straight through the measured floor and ceiling, so the realised range is the authored
        /// one with nothing clipped and nothing assumed. The flat runs outside only catch a cell
        /// whose noise leaves the range this seed produced on the tiles that were measured.
        /// </remarks>
        /// <summary>Highest graph value the geological stack actually produces.</summary>
        /// <remarks>
        /// Measured, over the four tiles: a high base plus all three beds reaches 1.0281, and
        /// 0.596 % of dry land sits above the knee below. Not a safety margin - if the stack is
        /// ever made taller this has to be re-measured, or the knee will start compressing terrain
        /// that did not need it, or stop catching the top.
        /// </remarks>
        private const float OverrangeCeiling = 1.0285f;

        /// <summary>Where the knee leaves the identity line.</summary>
        /// <remarks>
        /// 0.98 rather than something lower because everything between here and 1.0 is terrain the
        /// clamp was NOT damaging, and bending it is a cost with no benefit. At 0.98 the worst case
        /// inside the untouched-by-clipping region is a 0.49 m drop, at v = 0.99 it is 0.13 m, and
        /// 99.4 % of the island is not touched at all.
        /// </remarks>
        private const float OverrangeKnee = 0.98f;

        /// <summary>Slope left at the very top, as a fraction of the identity slope.</summary>
        /// <remarks>
        /// Zero would be simpler and would absorb slightly more, but it flattens the summit: the
        /// highest cells would all land on one value, which is a smaller version of the artifact
        /// this whole stage exists to remove.
        /// </remarks>
        private const float OverrangeTopSlope = 0.3f;

        /// <summary>Identity below the knee, a smooth monotonic squeeze above it.</summary>
        /// <remarks>
        /// Authored in the curve's own domain, which is the graph value divided by
        /// <see cref="OverrangeCeiling"/>. A cubic Hermite whose start slope is chosen so the
        /// bend leaves the identity line with slope exactly 1 - that continuity is what keeps a
        /// visible contour from appearing along the threshold.
        /// </remarks>
        private static AnimationCurve BuildOverrangeKnee()
        {
            var v = OverrangeCeiling;
            var k = OverrangeKnee;
            var m1 = OverrangeTopSlope;
            var r = (v - k) / (1f - k);
            var c3 = r - 2f + m1;
            var c2 = -2f * r + 3f - m1;

            // The identity part needs two keys and no more: bilinear sampling of the baked curve
            // reproduces a straight line exactly, so more keys would only add rounding.
            var keys = new System.Collections.Generic.List<Keyframe>
            {
                new Keyframe(0f, 0f, v, v),
                new Keyframe(k / v, k, v, v),
            };

            const int Steps = 48;
            for (var i = 1; i <= Steps; i++)
            {
                var u = i / (float)Steps;
                var value = k + (1f - k) * (((c3 * u + c2) * u + r) * u);
                var slope = (1f - k) / (v - k) * ((3f * c3 * u + 2f * c2) * u + r) * v;
                var t = (k + (v - k) * u) / v;
                keys.Add(new Keyframe(t, value, slope, slope));
            }

            return new AnimationCurve(keys.ToArray());
        }

        private static AnimationCurve BuildModulationRemap()
        {
            var span = GeologyNoiseCeiling - GeologyNoiseFloor;
            var slope = (GeologyModulationHigh - GeologyModulationLow) / span;
            return new AnimationCurve(
                new Keyframe(0f, GeologyModulationLow, 0f, 0f),
                new Keyframe(GeologyNoiseFloor, GeologyModulationLow, 0f, slope),
                new Keyframe(GeologyNoiseCeiling, GeologyModulationHigh, slope, 0f),
                new Keyframe(1f, GeologyModulationHigh, 0f, 0f));
        }

        private static void RetargetVegetation(TerrainGraph graph)
        {
            LowerIslandFloor(graph);
            LowerLayerHeightMasks(graph);
            VistaGraphBuild.ReplaceSampleGrassWithCanonical(graph);
            AddGeologicalStrata(graph);
            GateVegetationByHeight(graph);
            ThinCanopy(graph);
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
