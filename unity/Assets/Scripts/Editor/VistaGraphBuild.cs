#if UNITY_EDITOR
using System.IO;
using Pinwheel.Vista.Graph;
using UnityEditor;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Shared plumbing for the C#-authored Vista graphs. Deliberately thin: it owns edge creation,
    /// the <c>Set Variable</c>/<c>Get Variable</c> interface used between graph stages, and folder
    /// creation — nothing that belongs in the graph itself.
    /// </summary>
    /// <remarks>
    /// Vista's own biome graphs wire logical stages through named mask variables rather than long
    /// node chains (see <c>Mountain_TerrainGraphTemplate</c>). <see cref="VarHeightMap"/>,
    /// <see cref="VarErosionMask"/> and <see cref="VarGreenAreaMask"/> reproduce that interface, so
    /// derived maps are computed once and read many times.
    /// </remarks>
    public static class VistaGraphBuild
    {
        /// <summary>Height after erosion. Matches the reference graph's variable name verbatim.</summary>
        public const string VarHeightMap = "Height Map";

        /// <summary>Hydraulic erosion flow/soil channel — drives soil, peak and vegetation masks.</summary>
        public const string VarErosionMask = "ErosionMask";

        /// <summary>Where the biome reads as green. Consumed by the tree and detail stages.</summary>
        public const string VarGreenAreaMask = "GreenAreaMask";

        public static void Connect(
            TerrainGraph graph,
            string outNodeId,
            int outSlotId,
            string inNodeId,
            int inSlotId)
        {
            graph.AddEdge(new Edge(new SlotRef(outNodeId, outSlotId), new SlotRef(inNodeId, inSlotId)));
        }

        /// <summary>Where the biome actually covers the terrain, published for later stages.</summary>
        public const string VarBiomeMask = "BiomeMask";

        /// <summary>Where the canopy stands are, so the understory can follow them.</summary>
        public const string VarCanopyMask = "CanopyMask";

        /// <summary>
        /// The range Vista's noise actually occupies, for contrast-stretching it back to [0,1].
        /// </summary>
        /// <remarks>
        /// <c>Noise.shader</c> computes <c>1 - (perlin * 0.5 + 0.5)</c>. Perlin's theoretical range
        /// is [-1,1] but it practically never reaches the ends, and stacking octaves narrows the
        /// distribution further, so the output is a bell around 0.5. Any node that maps noise onto
        /// an output band has to stretch this range first or it will only traverse the middle of
        /// that band.
        /// </remarks>
        private const float NoiseWorkingLow = 0.35f;
        private const float NoiseWorkingHigh = 0.65f;

        /// <summary>
        /// Reads Vista's own biome coverage mask into the graph and publishes it as a variable.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The mask is already supplied to every terrain graph as an input named
        /// <c>GraphConstants.BIOME_MASK_INPUT_NAME</c> (see <c>LPBInputProvider.SetInput</c>); an
        /// <c>InputNode</c> with that name picks it up. This is the missing piece the graph needs to
        /// reason about its own edge.
        /// </para>
        /// <para>
        /// It matters because the terrain the player sees is <c>mask × graphHeight</c> blended over
        /// whatever is underneath. Inside the Outer falloff ring the mask falls off and the surface
        /// sinks toward the seabed — crossing the waterline partway through the ring. The graph's own
        /// height knows nothing about that, so anything keyed on height alone (a beach band, a tree
        /// mask) spills into the water. Gating on the mask is what keeps vegetation on dry land.
        /// </para>
        /// </remarks>
        public static void PublishBiomeMask(TerrainGraph graph)
        {
            var input = new InputNode();
            input.inputName = GraphConstants.BIOME_MASK_INPUT_NAME;
            input.SetSlotType(typeof(MaskSlot));
            graph.AddNode(input);

            var setMask = AddSetVar(graph, VarBiomeMask);
            Connect(graph, input.id, input.outputSlot.id, setMask.id, setMask.inputSlot.id);
        }

        /// <summary>
        /// The part of the biome that is safely inland of the falloff ring.
        /// </summary>
        /// <remarks>
        /// The mask runs 1 in the solid interior and 0 outside the falloff. The waterline sits
        /// somewhere in between — for the island's numbers, around 0.55 — so anything below
        /// <see cref="DryLandMaskFloor"/> is shoreline or sea and must stay bare.
        /// </remarks>
        public static LevelsNode AddDryLandGate(TerrainGraph graph)
        {
            var node = new LevelsNode
            {
                inLow = DryLandMaskFloor,
                inMid = 0.5f,
                inHigh = DryLandMaskFloor + 0.12f,
            };
            graph.AddNode(node);
            return node;
        }

        /// <summary>Biome-mask value below which the blended surface dips under sea level.</summary>
        private const float DryLandMaskFloor = 0.68f;

        /// <summary>Publishes a mask under <paramref name="varName"/> for later stages to read.</summary>
        public static SetVariableNode AddSetVar(TerrainGraph graph, string varName)
        {
            var node = new SetVariableNode { varName = varName };
            graph.AddNode(node);
            return node;
        }

        /// <summary>Reads a mask published by <see cref="AddSetVar"/> without a physical connection.</summary>
        public static GetVariableNode AddGetVar(TerrainGraph graph, string varName)
        {
            var node = new GetVariableNode { varName = varName };
            graph.AddNode(node);
            return node;
        }

        /// <summary>
        /// Creates one <c>TextureOutputNode</c> per canonical surface layer, indexed by
        /// <c>(int)SurfaceLayer</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Every graph must emit the <b>full</b> canonical set, even for surfaces it does not use.
        /// Vista builds each tile's <c>terrainLayers</c> array from the distinct layers the biomes
        /// covering that tile produced, so a graph that skips a layer makes tiles disagree about what
        /// alphamap channel <i>n</i> means. An unused output is simply left unconnected and writes a
        /// black weight, which costs one cleared render target and keeps every tile on one stack.
        /// </para>
        /// <para>
        /// <c>order</c> is the canonical index because Vista sorts texture outputs by it
        /// (<c>TerrainGraphUtilities</c>) and merges biome layers by <c>TerrainLayer</c> asset
        /// identity — both graphs therefore have to agree on the assets and on the order.
        /// </para>
        /// </remarks>
        public static TextureOutputNode[] AddCanonicalTextureOutputs(TerrainGraph graph)
        {
            var outputs = new TextureOutputNode[OverworldTerrainLayers.CanonicalLayerCount];
            for (var i = 0; i < outputs.Length; i++)
            {
                var layer = (OverworldTerrainLayers.SurfaceLayer)i;
                var node = new TextureOutputNode
                {
                    order = i,
                    terrainLayer = OverworldTerrainLayers.GetCanonicalLayer(layer),
                };
                graph.AddNode(node);
                outputs[i] = node;
            }

            return outputs;
        }

        /// <summary>
        /// A curve that is fully on up to <paramref name="fullUntil"/> and fully off from
        /// <paramref name="zeroAt"/>, over the normalized 0..1 height map.
        /// </summary>
        /// <remarks>
        /// Altitude bands are expressed in normalized height, not metres, so they keep their meaning
        /// when the biome's <c>Set Biome Height On Spawn</c> value changes in the Inspector. A
        /// <c>HeightMaskNode</c> would compare against absolute metres and silently drift instead.
        /// </remarks>
        public static UnityEngine.AnimationCurve LowAltitudeCurve(float fullUntil, float zeroAt)
        {
            var full = UnityEngine.Mathf.Clamp01(fullUntil);
            var zero = UnityEngine.Mathf.Clamp01(zeroAt);
            var keys = new System.Collections.Generic.List<UnityEngine.Keyframe>
            {
                new UnityEngine.Keyframe(0f, 1f, 0f, 0f),
                new UnityEngine.Keyframe(full, 1f, 0f, 0f),
                new UnityEngine.Keyframe(zero, 0f, 0f, 0f),
            };

            // A duplicate time makes AnimationCurve evaluation undefined, so only extend the tail
            // when the fade actually ends before the top of the range.
            if (zero < 1f)
            {
                keys.Add(new UnityEngine.Keyframe(1f, 0f, 0f, 0f));
            }

            return new UnityEngine.AnimationCurve(keys.ToArray());
        }

        /// <summary>
        /// A flat 1.0 transition, which turns a <c>SlopeMaskNode</c> into a plain gate.
        /// </summary>
        /// <remarks>
        /// <c>SlopeMask.shader</c> computes <c>(angle &gt;= min) * (angle &lt;= max) * curve(t)</c>, so the
        /// band edges are already hard. <c>Utilities.EaseInOutCurve</c> is a plateau that reaches 0 at
        /// both ends of the range — fine for a band floating inside the slope domain, but it zeroes a
        /// mask that starts at 0°. A flat-ground gate needs this curve, exactly as Vista's reference
        /// biome does for its <c>SlopeMask(0..3°)</c>.
        /// </remarks>
        public static UnityEngine.AnimationCurve ConstantCurve()
        {
            return new UnityEngine.AnimationCurve(
                new UnityEngine.Keyframe(0f, 1f, 0f, 0f),
                new UnityEngine.Keyframe(1f, 1f, 0f, 0f));
        }

        /// <summary>
        /// Fades in over the first <paramref name="fadeInEnd"/> of the range and then stays on.
        /// </summary>
        /// <remarks>
        /// For a band whose upper bound is a physical limit rather than a design choice — a cliff mask
        /// running to 90°, say. `EaseInOutCurve` would fade back out at the top and leave the sheerest
        /// faces unpainted.
        /// </remarks>
        public static UnityEngine.AnimationCurve RisingBandCurve(float fadeInEnd)
        {
            return new UnityEngine.AnimationCurve(
                new UnityEngine.Keyframe(0f, 0f, 0f, 0f),
                new UnityEngine.Keyframe(UnityEngine.Mathf.Clamp01(fadeInEnd), 1f, 0f, 0f),
                new UnityEngine.Keyframe(1f, 1f, 0f, 0f));
        }

        /// <summary>
        /// The reference biome's fractal noise, with only scale and seed left to the caller.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>NoiseNode</c>'s own defaults already are the reference's mountain noise — 8 octaves,
        /// lacunarity 2.25, persistence 0.335, <c>flipSign</c>, and <c>applyRemapPerLayer</c> with a
        /// remap curve. That last pair is what gives the reference its ridged, eroded-rock character:
        /// each octave is remapped before the sum, which a plain fBm cannot reproduce. Overriding the
        /// octave count down to 3 or 4, as this project did, produces smooth blobs no amount of
        /// downstream curve work recovers.
        /// </para>
        /// <para>
        /// Only <paramref name="scale"/> is genuinely per-biome: it is the size of the largest
        /// feature in metres, so a 1880 m island and a 1050 m massif want different values.
        /// </para>
        /// </remarks>
        public static NoiseNode AddDetailNoise(TerrainGraph graph, float scale, int seed)
        {
            var node = new NoiseNode
            {
                scale = scale,
                seed = seed,
                // The reference's two deliberate departures from the node defaults.
                persistence = 0.382f,
                remapCurve = new UnityEngine.AnimationCurve(
                    new UnityEngine.Keyframe(0.1f, 0f, 0f, 0f),
                    new UnityEngine.Keyframe(1f, 1f, 0f, 0f)),
            };
            ApplyNoiseDiagnosticOverrides(node);
            graph.AddNode(node);
            return node;
        }

        /// <summary>
        /// The reference's single broad lobe: one octave, deciding where the landform has mass.
        /// </summary>
        public static NoiseNode AddMassNoise(TerrainGraph graph, float scale, int seed)
        {
            var node = new NoiseNode
            {
                scale = scale,
                seed = seed,
                layerCount = 1,
            };
            ApplyNoiseDiagnosticOverrides(node);
            graph.AddNode(node);
            return node;
        }

        /// <summary>Applies whatever diagnostic noise overrides are set, and nothing otherwise.</summary>
        private static void ApplyNoiseDiagnosticOverrides(NoiseNode node)
        {
            if (NoiseDerivativeOverride.HasValue)
            {
                node.layerDerivativeMode = NoiseDerivativeOverride.Value;
            }
        }

        /// <summary>
        /// The reference's post-noise <c>Levels</c>: a gentle floor lift, not a contrast stretch.
        /// </summary>
        /// <remarks>
        /// <c>outLow 0.05</c> keeps the lowest ground just off zero. The previous
        /// <c>inHigh = 0.55</c> stretch clipped the whole upper half of the distribution to 1, which
        /// is what flattened the plateau top and the massif crown into mush.
        /// </remarks>
        public static LevelsNode AddReferenceLevels(TerrainGraph graph)
        {
            var node = new LevelsNode
            {
                inLow = 0f,
                inMid = 0.5f,
                inHigh = 1f,
                outLow = 0.05f,
                outHigh = 1f,
            };
            graph.AddNode(node);
            return node;
        }

        /// <summary>
        /// Iterations of the macro erosion pass. The reference's value; see
        /// <see cref="AddMacroErosion"/> for why raising it was tried and rejected.
        /// </summary>
        public const int MacroErosionIterations = 200;

        /// <summary>
        /// Diagnostic only: author the erosion nodes bypassed, so a rebuild produces the shaping
        /// stage alone.
        /// </summary>
        /// <remarks>
        /// The one experiment that separates "the noise made this structure" from "erosion made
        /// it" — Vista's default <c>Bypass</c> forwards the first input slot to the first output
        /// slot, and on <c>HydraulicErosionNode2</c> those are Height in and Height out, so the
        /// shaping passes through untouched. The soil output goes empty, which blanks
        /// <see cref="VarErosionMask"/> and therefore the surface weights: this flag is for
        /// measuring <em>geometry</em>, never for a bake anyone looks at.
        /// <para>
        /// It is deliberately not persisted anywhere. Set it from the batch harness for one run and
        /// restore the graph assets afterwards — leaving a graph authored with erosion bypassed
        /// would be indistinguishable from a broken bake.
        /// </para>
        /// </remarks>
        public static bool BypassErosionForDiagnostics { get; set; }

        /// <summary>
        /// Diagnostic only: author the noise nodes with a non-default octave falloff.
        /// </summary>
        /// <remarks>
        /// <c>Linear</c> is the node default and a standard fBm — octave <c>i</c> has scale
        /// <c>scale / lacunarity^i</c> and amplitude <c>persistence^i</c>. <c>Pow</c> divides by
        /// <c>lacunarity^i</c> *each step*, so scale and amplitude collapse super-exponentially and
        /// only the first two or three octaves contribute anything. Null leaves the default alone.
        /// <para>
        /// Set from the batch harness for one run, and restore the graph assets afterwards.
        /// </para>
        /// </remarks>
        public static NoiseNode.LayerDerivativeMode? NoiseDerivativeOverride { get; set; }

        /// <summary>
        /// The reference's first erosion pass: broad drainage carved into the raw noise.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The node's own Macro preset, with the two values the reference changes. Everything else —
        /// rain 10, erosion 10, featureSize 8, ramps 5/100 — is the default and stays the default.
        /// </para>
        /// <para>
        /// <b>Raising the iteration count does not fix the banding — measured, not assumed.</b> The
        /// standing theory was that our 320 px sim canvas (the biome is 2566 m wide and Vista
        /// Personal caps the canvas at 512, where the reference had ~1000 px) left too few
        /// iterations for neighbouring channels to capture each other, so the flow only scratched
        /// parallel rills down the slope. An A/B at 200 vs 500 iterations, island-only, seed 110201,
        /// moved every measured number by less than 0.02: curvature ratio 0.59/0.71/0.57/0.67 became
        /// 0.59/0.70/0.59/0.66, the axis-aligned share was unchanged on all four tiles, and the peak
        /// height was identical at 115 m. Iterations are not the lever — do not spend bake time on
        /// them again without a new reason.
        /// </para>
        /// </remarks>
        public static HydraulicErosionNode2 AddMacroErosion(TerrainGraph graph)
        {
            var node = new HydraulicErosionNode2
            {
                iterationCount = MacroErosionIterations,
                sedimentCapacity = 50f,
                isBypassed = BypassErosionForDiagnostics,
            };
            graph.AddNode(node);
            return node;
        }

        /// <summary>
        /// The reference's second erosion pass: fine detail, and the source of the erosion mask.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This pass exists in Vista's own Mountain biome and we had been skipping it, on an
        /// inherited claim that two instances share temporary render-target names and produce a
        /// "fangs" artifact. That claim was never verified here, and the shipped reference biome runs
        /// two passes, so it cannot be generally true.
        /// </para>
        /// <para>
        /// Skipping it mattered for more than detail. <c>depositRamp</c> and <c>drainageRamp</c> are
        /// documented on the node as "cm of settled sediment / scoured rock that reads as fully bare
        /// soil". The macro pass leaves them at 5 and 100; this pass raises them to 10 and 300, so
        /// far less of the terrain reads as bare soil. Reading the erosion mask off the macro pass
        /// instead — as this project did — feeds the soil layer a saturated signal, and the layer
        /// dump measured the result: soil covering 45–75 % of the island.
        /// </para>
        /// <para>
        /// <b>Feature size is ours, not the reference's.</b> The reference authors its graph at a
        /// 1024 px canvas and can afford <c>featureSize 2</c>; Vista Personal caps the biome canvas
        /// at 512 px, so on a ~2 km biome that request needs a 1000 px sim and gets clamped — the
        /// node says so in the console. A clamped flow simulation carves grid-aligned parallel rills
        /// instead of drainage, which is the corduroy striping across the island. See
        /// <see cref="MinFeatureSizeMeters"/>.
        /// </para>
        /// </remarks>
        public static HydraulicErosionNode2 AddMicroErosion(TerrainGraph graph)
        {
            var node = new HydraulicErosionNode2
            {
                // Halved against the reference, because the canvas is half the reference's. Rill
                // instability grows with iteration count on a coarse grid.
                iterationCount = 100,
                sedimentCapacity = 50f,
                erosionRate = 5f,
                depositionRate = 0.1f,
                featureSize = MinFeatureSizeMeters,
                depositRamp = 10f,
                drainageRamp = 300f,
                isBypassed = BypassErosionForDiagnostics,
            };
            graph.AddNode(node);
            return node;
        }

        /// <summary>
        /// Smallest erosion feature size that still fits Vista Personal's 512 px biome canvas.
        /// </summary>
        /// <remarks>
        /// The sim canvas Vista allocates is <c>biomeWidthMeters / featureSize</c>, capped at the
        /// biome's canvas resolution. Personal caps that at 512 (1K/2K/4K are Indie/Pro only), so on
        /// a biome up to ~3 km across this is the finest request that is not silently clamped. Raise
        /// it if the island area grows further — the console warning names the exact px it wanted.
        /// </remarks>
        public const int MinFeatureSizeMeters = 6;

        /// <summary>
        /// Publishes the erosion pass's soil channel as <see cref="VarErosionMask"/>, unshaped.
        /// </summary>
        /// <remarks>
        /// The reference's curve here is effectively a pass-through (two keys, slope ~1, and its
        /// weights are ignored because the keyframes are not in weighted mode). Boosting the mid
        /// tones instead — as this project did — turns a sparse drainage signal into a dense one, and
        /// then <c>Levels</c> on the soil layer stretches it to 1 over most of the biome. That single
        /// substitution is what painted three quarters of the island in sediment.
        /// </remarks>
        public static void PublishErosionMask(TerrainGraph graph, HydraulicErosionNode2 erosion)
        {
            var curve = new CurveNode { curve = UnityEngine.AnimationCurve.Linear(0f, 0f, 1f, 1f) };
            var setErosion = AddSetVar(graph, VarErosionMask);
            graph.AddNode(curve);
            Connect(graph, erosion.id, erosion.outputSoilSlot.id, curve.id, curve.inputSlot.id);
            Connect(graph, curve.id, curve.outputSlot.id, setErosion.id, setErosion.inputSlot.id);
        }

        /// <summary>The reference's soil <c>Levels</c> over the erosion mask.</summary>
        public static LevelsNode AddSoilLevels(TerrainGraph graph)
        {
            var node = new LevelsNode { inLow = 0.153f, inMid = 0.5f, inHigh = 0.448f };
            graph.AddNode(node);
            return node;
        }

        /// <summary>
        /// Near-step curve: any erosion activity at all reads as 1.
        /// </summary>
        /// <remarks>
        /// Copied from the reference biome, where this curve reaches 1 by t = 0.01. It only makes
        /// sense in front of <see cref="AddInvert"/>: the inverted result is then a <em>thin</em>
        /// mask covering only untouched ridges. A gradual ramp here inverts into a broad mask that,
        /// as the top Weight Blend layer, paints the entire landform one colour.
        /// </remarks>
        public static UnityEngine.AnimationCurve ErosionPresenceCurve()
        {
            return new UnityEngine.AnimationCurve(
                new UnityEngine.Keyframe(0f, 0f, 0f, 0f),
                new UnityEngine.Keyframe(0.01f, 1f, 0f, 0f),
                new UnityEngine.Keyframe(1f, 1f, 0f, 0f));
        }

        /// <summary>One-minus, the reference graph's way of turning an erosion mask into ridges.</summary>
        public static MathNode AddInvert(TerrainGraph graph)
        {
            var node = new MathNode();
            node.maths.Add(new MathNode.MathConfig
            {
                enabled = true,
                number = 1f,
                ops = MathNode.Operator.OneMinus,
            });
            graph.AddNode(node);
            return node;
        }

        /// <summary>
        /// The vegetation stage from Vista's reference biome: publish where the surface reads green,
        /// then place trees and grass from that variable instead of from a second procedural system.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>greenSource</c> is the graph's green texture weight — the biome paints a surface, and
        /// vegetation follows the paint. Trees additionally pass a <c>GeometryMask</c> so they stay
        /// off peaks and off cliff faces.
        /// </para>
        /// <para>
        /// The Polaris branch of the reference (<c>DetailInstanceOutputNode</c> behind another
        /// <c>Scatter</c>) is deliberately not built: <c>TerrainTile</c> implements
        /// <c>IDetailDensityPopulator</c> but not a detail-instance populator, so on Unity terrain
        /// those two nodes would compute a buffer nothing ever reads.
        /// </para>
        /// </remarks>
        /// <param name="includeGroundCover">
        /// Whether this biome emits the meadow detail layer. Two graphs outputting the <em>same</em>
        /// Detail Template give Vista two density maps per tile for one template, which sends it down
        /// its merge path instead of the direct one — and merging is where the map stopped reaching
        /// the terrain. Alpine scree has no business growing meadow grass anyway, so the massif opts
        /// out and the island keeps it.
        /// </param>
        public static void AddVegetationStage(
            TerrainGraph graph,
            ImageNodeBase greenSource,
            int greenSourceOutputSlotId,
            float treeMaxSlopeDegrees,
            int seed,
            bool includeGroundCover = true)
        {
            // Everything downstream reads GreenAreaMask, so gating it here keeps every vegetation
            // layer — canopy, understory, rocks, grass — off the drowned part of the falloff ring.
            PublishBiomeMask(graph);
            var dryLand = AddDryLandGate(graph);
            var maskForVegetation = AddGetVar(graph, VarBiomeMask);
            var greenOnDryLand = new CombineNode { mode = CombineNode.Mode.Multiply };
            graph.AddNode(greenOnDryLand);

            var setGreen = AddSetVar(graph, VarGreenAreaMask);
            Connect(graph, maskForVegetation.id, maskForVegetation.outputSlot.id, dryLand.id, dryLand.inputSlot.id);
            Connect(graph, greenSource.id, greenSourceOutputSlotId, greenOnDryLand.id, greenOnDryLand.backgroundSlot.id);
            Connect(graph, dryLand.id, dryLand.outputSlot.id, greenOnDryLand.id, greenOnDryLand.foregroundSlot.id);
            Connect(graph, greenOnDryLand.id, greenOnDryLand.outputSlot.id, setGreen.id, setGreen.inputSlot.id);

            // Three canopy species, each confined to its own patches by its own cluster noise, so the
            // forest reads as stands of one kind meeting stands of another instead of an even
            // three-way mix. Rising thresholds make each successive species rarer.
            // Scale figures are multiples of the source prefab, and the prefabs are not the same
            // size: the Polytope pine measures 9.2 m and the fruit tree 7.8 m at scale 1. The
            // comment on each line is the resulting world height band, which is the number worth
            // reviewing — a forest reads as majestic when the canopy is well above head height and
            // the trunks are tall enough to walk under.
            // scaleVariation is wide on purpose: it is what separates saplings from old growth, and
            // a forest of one size reads as wallpaper however good the trees are.
            //
            // Species are grouped into **forest types**, not scattered independently. Five species
            // each rolling their own cluster noise is what produced the "everything mixes, and the
            // island is bare between patches" look: five independent masks agree nowhere, so no
            // stand reads as a kind of wood, and five high thresholds multiply into large empty
            // gaps.
            //
            // Instead, the members of one forest type share a cluster field (same scale, same
            // seed) and differ only in threshold, so the dominant species covers the stand and the
            // second fills in inside it — a pine wood with paler pines through it, a broadleaf wood
            // with darker crowns through it. The two types use different fields, so conifer and
            // broadleaf occupy different parts of the island and meet at an edge.
            //
            // Thresholds are also lower than before. They are compared against noise stretched to
            // [0,1], so 0.30 keeps roughly the upper 70 % of each field rather than the upper 40 %,
            // and the island gets continuous woodland with clearings instead of isolated islands of
            // trees. Age comes from scaleVariation, which is why it is wide: one stand carries
            // saplings and old growth together.
            const float coniferCluster = 640f;
            const int coniferSeed = 11;
            const float broadleafCluster = 520f;
            const int broadleafSeed = 37;

            var stands = new System.Collections.Generic.List<LevelsNode>
            {
                // --- Coniferous wood: dominant pine, paler pine threaded through the same stand ---
                AddCanopySpecies(
                    graph, VistaVegetationTemplates.Species.Pine,
                    clusterScale: coniferCluster, clusterSeed: seed + coniferSeed, threshold: 0.30f,
                    treeMaxSlopeDegrees, spacing: 16f, seed: seed + 101,
                    maxScale: 2.6f, scaleVariation: 0.55f), // 9.2 m prefab -> 10.8…23.8 m
                AddCanopySpecies(
                    graph, VistaVegetationTemplates.Species.PinePale,
                    clusterScale: coniferCluster, clusterSeed: seed + coniferSeed, threshold: 0.46f,
                    treeMaxSlopeDegrees, spacing: 22f, seed: seed + 104,
                    maxScale: 2.3f, scaleVariation: 0.5f),

                // --- Broadleaf wood: dominant fruit tree, deeper-green crowns inside the stand ---
                AddCanopySpecies(
                    graph, VistaVegetationTemplates.Species.Fruit,
                    clusterScale: broadleafCluster, clusterSeed: seed + broadleafSeed, threshold: 0.32f,
                    treeMaxSlopeDegrees, spacing: 20f, seed: seed + 102,
                    maxScale: 2.45f, scaleVariation: 0.5f), // 7.8 m prefab -> 9.6…19.1 m
                AddCanopySpecies(
                    graph, VistaVegetationTemplates.Species.FruitDeep,
                    clusterScale: broadleafCluster, clusterSeed: seed + broadleafSeed, threshold: 0.48f,
                    treeMaxSlopeDegrees, spacing: 24f, seed: seed + 105,
                    maxScale: 2.2f, scaleVariation: 0.45f),

                // --- Orchard: deliberately the rare one, its own small field, pruned and even-sized ---
                AddCanopySpecies(
                    graph, VistaVegetationTemplates.Species.Orchard,
                    clusterScale: 240f, clusterSeed: seed + 73, threshold: 0.58f,
                    treeMaxSlopeDegrees, spacing: 26f, seed: seed + 103,
                    maxScale: 1.6f, scaleVariation: 0.3f), // 7.8 m prefab -> 8.7…12.5 m, pruned orchard
            };

            PublishCanopyMask(graph, stands);
            AddUnderstory(graph, treeMaxSlopeDegrees, seed + 201);
            AddRockDressing(graph, seed + 301);

            // --- Ground cover ---
            // Grass used to be the one vegetation layer with **no floor worth the name and no slope
            // rule at all**: a 0.06 green floor against the canopy's 0.333, and no GeometryMask.
            // Those two omissions produced every "grass where it should not be" report at once —
            // grass on the beach sand, grass past the waterline in the falloff ring, and grass on
            // vertical mountain faces — because 0.06 green is reached by ground that is mostly sand,
            // mostly rock, or mostly drowned, and nothing was checking the angle it sat on.
            //
            // The floor now sits just under the canopy's, so meadow reaches a little further than
            // forest and stops well before bare ground. The slope rule is the same idea as the
            // canopy's, a few degrees more generous: grass holds on a hillside, not on a cliff.
            if (!includeGroundCover)
            {
                return;
            }

            var greenForGrass = AddGetVar(graph, VarGreenAreaMask);
            var grassDensity = new LevelsNode
            {
                inLow = GrassGreenFloor,
                inMid = 0.5f,
                inHigh = 0.55f,
            };

            var heightForGrass = AddGetVar(graph, VarHeightMap);
            var grassTerrain = new GeometryMaskNode
            {
                blendMode = GeometryMaskNode.BlendMode.Multiply,
                enableHeightMask = false,
                enableSlopeMask = true,
                minAngle = 0f,
                maxAngle = GrassMaxSlopeDegrees,
                slopeTransition = ConstantCurve(),
                enableDirectionMask = false,
            };

            var grassOnSlope = new CombineNode { mode = CombineNode.Mode.Multiply };
            var grassOut = new DetailDensityOutputNode
            {
                detailTemplate = VistaVegetationTemplates.LoadOrCreateMeadowGrass(),
            };

            graph.AddNode(grassDensity);
            graph.AddNode(grassTerrain);
            graph.AddNode(grassOnSlope);
            graph.AddNode(grassOut);

            Connect(graph, greenForGrass.id, greenForGrass.outputSlot.id, grassDensity.id, grassDensity.inputSlot.id);
            Connect(graph, heightForGrass.id, heightForGrass.outputSlot.id, grassTerrain.id, grassTerrain.inputSlot.id);
            Connect(
                graph, grassDensity.id, grassDensity.outputSlot.id,
                grassOnSlope.id, grassOnSlope.backgroundSlot.id);
            Connect(
                graph, grassTerrain.id, grassTerrain.outputSlot.id,
                grassOnSlope.id, grassOnSlope.foregroundSlot.id);
            Connect(graph, grassOnSlope.id, grassOnSlope.outputSlot.id, grassOut.id, grassOut.inputSlot.id);
        }

        /// <summary>
        /// Green weight below which ground cover is not placed.
        /// </summary>
        /// <remarks>
        /// Just under <c>AddCanopySpecies</c>'s 0.333, so meadow reaches slightly further than
        /// forest and still stops before sand, rock and the drowned falloff ring — all three of
        /// which sit well under a third green. The old 0.06 is what put grass on the beach and in
        /// the water.
        /// </remarks>
        private const float GrassGreenFloor = 0.30f;

        /// <summary>
        /// Steepest ground that still grows grass. A few degrees above the canopy's limit, so a
        /// meadow runs a little higher up a hillside than the trees do, and nothing grows on a
        /// mountain face.
        /// </summary>
        private const float GrassMaxSlopeDegrees = 32f;

        /// <summary>
        /// Publishes where the canopy stands are, feathered outwards, as <see cref="VarCanopyMask"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The species masks are combined with <c>Max</c> rather than added: a pixel is "under
        /// canopy" if <em>any</em> species stands there, and adding would push overlaps past 1 and
        /// make the seams between stands denser than the stands themselves.
        /// </para>
        /// <para>
        /// The blur is the point of the whole node. A hard canopy mask would put scrub strictly
        /// under the trees and stop dead at the last trunk; feathering it lets the understory spill
        /// past the stand and thin out with distance, which is what a real wood edge does. The
        /// radius is in pixels of the biome canvas — at 512 px across 2240 m, 8 px is roughly 35 m.
        /// </para>
        /// </remarks>
        private static void PublishCanopyMask(
            TerrainGraph graph, System.Collections.Generic.List<LevelsNode> allStands)
        {
            var stands = allStands.FindAll(stand => stand != null);
            if (stands.Count == 0)
            {
                return;
            }

            ImageNodeBase merged = stands[0];
            var mergedSlotId = stands[0].outputSlot.id;
            for (var i = 1; i < stands.Count; i++)
            {
                var union = new CombineNode { mode = CombineNode.Mode.Max };
                graph.AddNode(union);
                Connect(graph, merged.id, mergedSlotId, union.id, union.backgroundSlot.id);
                Connect(graph, stands[i].id, stands[i].outputSlot.id, union.id, union.foregroundSlot.id);
                merged = union;
                mergedSlotId = union.outputSlot.id;
            }

            var feather = new BlurNode { radius = 8 };
            graph.AddNode(feather);
            var setCanopy = AddSetVar(graph, VarCanopyMask);
            Connect(graph, merged.id, mergedSlotId, feather.id, feather.inputSlot.id);
            Connect(graph, feather.id, feather.outputSlot.id, setCanopy.id, setCanopy.inputSlot.id);
        }

        /// <summary>
        /// Rocks on the rugged ground, where the green mask is weakest.
        /// </summary>
        /// <remarks>
        /// Driven by the inverse of the green mask rather than by its own noise, so stones appear
        /// exactly where vegetation thins out — the scree above the treeline, the bald patches, the
        /// steep shoulders. That is also what breaks up the empty ground the canopy cannot reach.
        /// </remarks>
        private static void AddRockDressing(TerrainGraph graph, int seed)
        {
            var template = VistaVegetationTemplates.LoadOrCreateRock();
            if (template == null)
            {
                return;
            }

            var green = AddGetVar(graph, VarGreenAreaMask);
            var bare = AddInvert(graph);
            // Only genuinely bare ground, not merely thin grass.
            var bareGate = new LevelsNode { inLow = 0.55f, inMid = 0.5f, inHigh = 0.8f };

            var scatterNoise = new NoiseNode { scale = 120f, seed = seed, layerCount = 3 };
            graph.AddNode(scatterNoise);
            var clumping = new CombineNode { mode = CombineNode.Mode.Multiply };

            var scatter = new ScatterNode
            {
                spacing = new UnityEngine.Vector2(34f, 34f),
                spreadCount = 5,
                spreadDistance = 0.05f,
                keepSourcePoints = true,
                seed = seed,
            };
            // PT_Generic_Rock_01 measures 0.25 m, so at scale 1 the "rocks" are gravel — and an
            // LOD Group that small falls under its own screen-height cull within a few metres, which
            // is why they appeared only when the camera was almost touching them. Scaled to boulders
            // they read as terrain features and stay visible.
            var output = new TreeOutputNode
            {
                treeTemplate = template,
                seed = seed,
                verticalScaleMultiplier = 9f,
                horizontalScaleMultiplier = 9f,
            };

            var sizeNoise = new NoiseNode { scale = 40f, seed = seed + 5, layerCount = 2 };
            graph.AddNode(sizeNoise);
            var sizeBand = new LevelsNode
            {
                inLow = NoiseWorkingLow,
                inMid = 0.5f,
                inHigh = NoiseWorkingHigh,
                outLow = 0.45f,
                outHigh = 1f,
            };

            graph.AddNode(bareGate);
            graph.AddNode(clumping);
            graph.AddNode(sizeBand);
            graph.AddNode(scatter);
            graph.AddNode(output);

            Connect(graph, sizeNoise.id, sizeNoise.outputSlot.id, sizeBand.id, sizeBand.inputSlot.id);
            Connect(graph, sizeBand.id, sizeBand.outputSlot.id, output.id, output.verticalScaleInputSlot.id);
            Connect(graph, sizeBand.id, sizeBand.outputSlot.id, output.id, output.horizontalScaleInputSlot.id);

            Connect(graph, green.id, green.outputSlot.id, bare.id, bare.inputSlot.id);
            Connect(graph, bare.id, bare.outputSlot.id, bareGate.id, bareGate.inputSlot.id);
            Connect(graph, bareGate.id, bareGate.outputSlot.id, clumping.id, clumping.backgroundSlot.id);
            Connect(
                graph, scatterNoise.id, scatterNoise.outputSlot.id, clumping.id, clumping.foregroundSlot.id);
            Connect(graph, clumping.id, clumping.outputSlot.id, scatter.id, scatter.densityInputSlot.id);
            Connect(graph, scatter.id, scatter.outputSlot.id, output.id, output.positionInputSlot.id);
        }

        /// <summary>
        /// One canopy species, confined to its own irregular patches.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The clustering is the point. Scattering every species from the same density field gives
        /// evenly spaced lone trees — the grid-like look — because each point is drawn independently
        /// from one distribution. Multiplying the green mask by a <em>coarse</em> noise thresholded
        /// hard instead produces forest masses with real gaps: dense inside a patch, empty between
        /// patches, and ragged at the edges because the noise is.
        /// </para>
        /// <para>
        /// <paramref name="threshold"/> is what turns a smooth noise into stands: higher means fewer,
        /// smaller, sparser patches, which is how a species reads as occasional rather than dominant.
        /// </para>
        /// <para>
        /// Size is set here rather than on the template because Unity terrain trees ignore
        /// <c>TreeTemplate.baseScale</c> — <c>TerrainTile.ParseTreeInstances</c> copies
        /// <c>TreeInstance.heightScale</c> straight out of the instance buffer, so the only levers
        /// are <c>TreeOutputNode</c>'s scale multipliers and the scale mask inputs beside them.
        /// </para>
        /// </remarks>
        /// <param name="maxScale">
        /// Multiple of the source prefab for the largest instance of this species.
        /// </param>
        /// <param name="scaleVariation">
        /// How far below <paramref name="maxScale"/> the smallest instance drops, as a fraction.
        /// The variation is driven by a coarse noise sampled at each instance, so size correlates
        /// with position: old growth in one part of a stand, saplings in another, which is how a
        /// forest avoids the cloned look of one scale repeated everywhere.
        /// </param>
        /// <returns>
        /// The species' cluster gate, so the caller can union the stands into
        /// <see cref="VarCanopyMask"/>; <see langword="null"/> when the species has no usable
        /// template.
        /// </returns>
        private static LevelsNode AddCanopySpecies(
            TerrainGraph graph,
            VistaVegetationTemplates.Species species,
            float clusterScale,
            int clusterSeed,
            float threshold,
            float treeMaxSlopeDegrees,
            float spacing,
            int seed,
            float maxScale,
            float scaleVariation)
        {
            var template = VistaVegetationTemplates.LoadOrCreateCanopyTree(species);
            if (template == null)
            {
                return null;
            }

            var green = AddGetVar(graph, VarGreenAreaMask);
            // Thins the canopy out of the faint green fringe so forest edges are not a uniform haze.
            var density = new LevelsNode { inLow = 0.333f, inMid = 0.569f, inHigh = 0.521f };

            var cluster = new NoiseNode { scale = clusterScale, seed = clusterSeed, layerCount = 3 };
            graph.AddNode(cluster);

            // Stretched to [0,1] before it is thresholded, for the reason in the NoiseNode note on
            // NoiseWorkingLow. Without this the threshold is compared against a bell that never
            // leaves [0.35, 0.65], so anything above 0.65 gates the species out of the world
            // entirely — that is why the orchard produced zero instances on all four tiles while
            // pine at 0.42 produced thousands.
            var clusterRange = new LevelsNode { inLow = NoiseWorkingLow, inMid = 0.5f, inHigh = NoiseWorkingHigh };
            graph.AddNode(clusterRange);

            // Hard shoulder: below the threshold there is no stand at all, which is what makes the
            // clearings between patches read as clearings.
            var clusterGate = new LevelsNode
            {
                inLow = threshold,
                inMid = 0.5f,
                inHigh = UnityEngine.Mathf.Min(1f, threshold + 0.16f),
            };

            var height = AddGetVar(graph, VarHeightMap);
            var terrain = new GeometryMaskNode
            {
                blendMode = GeometryMaskNode.BlendMode.Multiply,
                enableHeightMask = false,
                enableSlopeMask = true,
                minAngle = 0f,
                maxAngle = treeMaxSlopeDegrees,
                slopeTransition = ConstantCurve(),
                enableDirectionMask = false,
            };

            var greenTimesCluster = new CombineNode { mode = CombineNode.Mode.Multiply };
            var mask = new CombineNode { mode = CombineNode.Mode.Multiply };
            var scatter = new ScatterNode
            {
                spacing = new UnityEngine.Vector2(spacing, spacing),
                spreadCount = 7,
                spreadDistance = 0.05f,
                keepSourcePoints = true,
                seed = seed,
            };
            var output = new TreeOutputNode
            {
                treeTemplate = template,
                seed = seed,
                verticalScaleMultiplier = maxScale,
                horizontalScaleMultiplier = maxScale,
            };

            // The scale mask multiplies the multiplier, so it has to stay inside [0,1] — hence a
            // band from 1-variation to 1 rather than a curve around 1. One mask feeds both axes so
            // instances scale uniformly; splitting them would stretch trunks.
            var sizeNoise = new NoiseNode { scale = 55f, seed = seed + 7, layerCount = 2 };
            graph.AddNode(sizeNoise);

            // The input range has to be stretched before the band is applied. Vista's noise is
            // `1 - (perlin * 0.5 + 0.5)` (Noise.shader), and Perlin almost never reaches ±1, so the
            // raw output is a bell roughly inside [0.35, 0.65] rather than a sweep across [0,1].
            // Mapping that straight onto the size band traverses only the middle sixth of it: with
            // outLow 0.67 every tree came out between 0.78 and 0.88 of full size, which is the
            // "all the trees are the same height" look. Stretching [0.35,0.65] to [0,1] first is
            // what makes the band mean what it says.
            var sizeBand = new LevelsNode
            {
                inLow = NoiseWorkingLow,
                inMid = 0.5f,
                inHigh = NoiseWorkingHigh,
                outLow = UnityEngine.Mathf.Clamp01(1f - scaleVariation),
                outHigh = 1f,
            };

            graph.AddNode(density);
            graph.AddNode(clusterGate);
            graph.AddNode(terrain);
            graph.AddNode(greenTimesCluster);
            graph.AddNode(mask);
            graph.AddNode(sizeBand);
            graph.AddNode(scatter);
            graph.AddNode(output);

            Connect(graph, sizeNoise.id, sizeNoise.outputSlot.id, sizeBand.id, sizeBand.inputSlot.id);
            Connect(
                graph, sizeBand.id, sizeBand.outputSlot.id,
                output.id, output.verticalScaleInputSlot.id);
            Connect(
                graph, sizeBand.id, sizeBand.outputSlot.id,
                output.id, output.horizontalScaleInputSlot.id);

            Connect(graph, green.id, green.outputSlot.id, density.id, density.inputSlot.id);
            Connect(graph, cluster.id, cluster.outputSlot.id, clusterRange.id, clusterRange.inputSlot.id);
            Connect(graph, clusterRange.id, clusterRange.outputSlot.id, clusterGate.id, clusterGate.inputSlot.id);
            Connect(
                graph, density.id, density.outputSlot.id,
                greenTimesCluster.id, greenTimesCluster.backgroundSlot.id);
            Connect(
                graph, clusterGate.id, clusterGate.outputSlot.id,
                greenTimesCluster.id, greenTimesCluster.foregroundSlot.id);

            Connect(graph, height.id, height.outputSlot.id, terrain.id, terrain.inputSlot.id);
            Connect(
                graph, greenTimesCluster.id, greenTimesCluster.outputSlot.id,
                mask.id, mask.backgroundSlot.id);
            Connect(graph, terrain.id, terrain.outputSlot.id, mask.id, mask.foregroundSlot.id);
            Connect(graph, mask.id, mask.outputSlot.id, scatter.id, scatter.densityInputSlot.id);
            Connect(graph, scatter.id, scatter.outputSlot.id, output.id, output.positionInputSlot.id);

            return clusterGate;
        }

        /// <summary>
        /// Bushes around and under the canopy stands.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Scrub belongs to the trees. Driving it from its own patch noise — worse, from the
        /// <em>inverse</em> of one, which is what this did — spreads bushes evenly across open
        /// ground and puts most of them where there are no trees at all. That is both the wrong
        /// place and far too many of them: at 7 m spacing over open meadow the island ends up
        /// carpeted.
        /// </para>
        /// <para>
        /// Reading <see cref="VarCanopyMask"/> instead ties the understory to the stands, and
        /// because that mask is blurred the density falls off outwards, so bushes thicken under the
        /// trees and thin into the clearing rather than stopping at a hard line.
        /// </para>
        /// </remarks>
        private static void AddUnderstory(TerrainGraph graph, float treeMaxSlopeDegrees, int seed)
        {
            var template = VistaVegetationTemplates.LoadOrCreateUndergrowthBush();
            if (template == null)
            {
                return;
            }

            var green = AddGetVar(graph, VarGreenAreaMask);
            var density = new LevelsNode { inLow = 0.2f, inMid = 0.5f, inHigh = 0.6f };

            // Breaks the ring of scrub into clumps so it does not read as a hedge around every
            // stand. Coarse enough that a clump covers several bushes.
            var patchNoise = new NoiseNode { scale = 60f, seed = seed, layerCount = 2 };
            graph.AddNode(patchNoise);
            // outLow well above zero so the patches thin the scrub rather than erase it. Three
            // masks multiply into this density — green, patches, canopy — and a factor that reaches
            // zero anywhere wins outright, which is how the understory went from carpeting the
            // island to being hard to find.
            var patches = new LevelsNode
            {
                inLow = NoiseWorkingLow,
                inMid = 0.5f,
                inHigh = NoiseWorkingHigh,
                outLow = 0.4f,
                outHigh = 1f,
            };
            graph.AddNode(patches);

            var canopy = AddGetVar(graph, VarCanopyMask);
            var nearCanopy = new CombineNode { mode = CombineNode.Mode.Multiply };
            graph.AddNode(nearCanopy);

            var height = AddGetVar(graph, VarHeightMap);
            var terrain = new GeometryMaskNode
            {
                blendMode = GeometryMaskNode.BlendMode.Multiply,
                enableHeightMask = false,
                enableSlopeMask = true,
                minAngle = 0f,
                maxAngle = treeMaxSlopeDegrees + 8f,
                slopeTransition = ConstantCurve(),
                enableDirectionMask = false,
            };

            var greenTimesPatches = new CombineNode { mode = CombineNode.Mode.Multiply };
            var mask = new CombineNode { mode = CombineNode.Mode.Multiply };
            // 9 m rather than the original 7 m. Spacing is the point grid the density mask thins, so
            // it sets the ceiling on how many bushes can exist. 7 m carpeted the island only because
            // the mask let scrub grow everywhere; now that the canopy mask confines it to the stands,
            // the ceiling can come back up or there is nothing under the trees.
            var scatter = new ScatterNode
            {
                spacing = new UnityEngine.Vector2(9f, 9f),
                spreadCount = 7,
                spreadDistance = 0.05f,
                keepSourcePoints = true,
                seed = seed,
            };
            var output = new TreeOutputNode { treeTemplate = template, seed = seed };

            graph.AddNode(density);
            graph.AddNode(terrain);
            graph.AddNode(greenTimesPatches);
            graph.AddNode(mask);
            graph.AddNode(scatter);
            graph.AddNode(output);

            Connect(graph, green.id, green.outputSlot.id, density.id, density.inputSlot.id);
            Connect(graph, patchNoise.id, patchNoise.outputSlot.id, patches.id, patches.inputSlot.id);
            Connect(
                graph, density.id, density.outputSlot.id,
                greenTimesPatches.id, greenTimesPatches.backgroundSlot.id);
            Connect(
                graph, patches.id, patches.outputSlot.id,
                greenTimesPatches.id, greenTimesPatches.foregroundSlot.id);

            // The canopy mask goes in last so nothing downstream can dilute it: wherever there is no
            // stand nearby, the product is zero and no bush is placed at all.
            Connect(
                graph, greenTimesPatches.id, greenTimesPatches.outputSlot.id,
                nearCanopy.id, nearCanopy.backgroundSlot.id);
            Connect(graph, canopy.id, canopy.outputSlot.id, nearCanopy.id, nearCanopy.foregroundSlot.id);

            Connect(graph, height.id, height.outputSlot.id, terrain.id, terrain.inputSlot.id);
            Connect(
                graph, nearCanopy.id, nearCanopy.outputSlot.id,
                mask.id, mask.backgroundSlot.id);
            Connect(graph, terrain.id, terrain.outputSlot.id, mask.id, mask.foregroundSlot.id);
            Connect(graph, mask.id, mask.outputSlot.id, scatter.id, scatter.densityInputSlot.id);
            Connect(graph, scatter.id, scatter.outputSlot.id, output.id, output.positionInputSlot.id);
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        /// <summary>
        /// Writes a freshly built graph to <paramref name="assetPath"/> <b>without changing the
        /// asset's GUID</b>, and returns the asset that everything else references.
        /// </summary>
        /// <remarks>
        /// Delete-then-create looks equivalent and is not: <c>AssetDatabase.DeleteAsset</c> takes the
        /// <c>.meta</c> with it, so the re-created asset gets a new GUID and every serialized
        /// reference to it — above all the <c>terrainGraph</c> field on the biome template prefabs —
        /// silently becomes "Missing". <c>EditorUtility.CopySerialized</c> overwrites the existing
        /// object's contents in place instead, which is what keeps those references alive.
        /// </remarks>
        public static TAsset SaveGraph<TAsset>(TAsset built, string assetPath)
            where TAsset : UnityEngine.ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<TAsset>(assetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(built, assetPath);
                EditorUtility.SetDirty(built);
                AssetDatabase.SaveAssets();
                return built;
            }

            EditorUtility.CopySerialized(built, existing);
            UnityEngine.Object.DestroyImmediate(built);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            return existing;
        }
    }
}
#endif
