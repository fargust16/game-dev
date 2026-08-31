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
        /// The reference's <b>third</b> noise: fine roughness across the whole landform.
        /// </summary>
        /// <remarks>
        /// <para>
        /// We had two noises where <c>Mountain_TerrainGraphTemplate</c> has three, and the missing
        /// one is why the terrain banded. Amplitude decays as <c>persistence^i</c>, so at the detail
        /// noise's 0.382 the octaves run 1, 0.38, 0.15, 0.056, 0.021 — by the fourth there is
        /// essentially nothing left, and the fourth octave of a 1200 m noise is ~105 m. That is
        /// exactly the period `Dump Terrain Geometry` measured as the dominant banding: with no
        /// content below it, the last octave with any amplitude *is* the terrain's texture, and one
        /// spatial frequency repeated across a landform reads as stripes.
        /// </para>
        /// <para>
        /// This noise fills that gap. <c>persistence 0.728</c> is nearly double the detail noise's,
        /// so its eight octaves stay strong all the way down — 200 m to sub-metre with amplitudes
        /// 1, 0.73, 0.53, 0.39, 0.28 — which gives the surface continuous spectral content instead
        /// of one dominant wavelength. Both values are the reference's, not ours.
        /// </para>
        /// </remarks>
        public static NoiseNode AddFineNoise(TerrainGraph graph, float scale, int seed)
        {
            var node = new NoiseNode
            {
                scale = scale,
                seed = seed,
                persistence = 0.728f,
            };
            ApplyNoiseDiagnosticOverrides(node);
            graph.AddNode(node);
            return node;
        }

        /// <summary>
        /// The noise settings of `Demo_MountainRange_AsIs`, the one graph that measured isotropic.
        /// </summary>
        /// <remarks>
        /// Four octaves, persistence 0.333, and — this is the point — **no** `flipSign` and **no**
        /// `applyRemapPerLayer`, both of which our detail noise sets. Diagnostic only: it exists so
        /// the sample graph's noise can be measured through our pipeline, isolating the settings from
        /// everything else that differs between the two graphs.
        /// </remarks>
        public static NoiseNode AddSampleGraphDetailNoise(TerrainGraph graph, float scale, int seed)
        {
            var node = new NoiseNode
            {
                scale = scale,
                seed = seed,
                layerCount = 4,
                persistence = 0.333f,
                flipSign = false,
                applyRemapPerLayer = false,
            };
            ApplyNoiseDiagnosticOverrides(node);
            graph.AddNode(node);
            return node;
        }

        /// <summary>Scale of <see cref="AddFineNoise"/>, the reference's value.</summary>
        public const float FineNoiseScaleMeters = 200f;

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

            if (DisableNoiseRemapPerLayerForDiagnostics)
            {
                node.applyRemapPerLayer = false;
                node.remapCurve = UnityEngine.AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }

            if (DisableNoiseFlipSignForDiagnostics)
            {
                node.flipSign = false;
            }
            // Only the multi-octave noises; the mass noise is deliberately one octave.
            if (NoiseLayerCountForDiagnostics.HasValue && node.layerCount > 1)
            {
                node.layerCount = NoiseLayerCountForDiagnostics.Value;
            }

            if (NoiseLacunarityForDiagnostics.HasValue && node.layerCount > 1)
            {
                node.lacunarity = NoiseLacunarityForDiagnostics.Value;
            }

            if (NoiseWarpForDiagnostics.HasValue)
            {
                node.warpMode = NoiseWarpForDiagnostics.Value;
                node.warpIntensity = NoiseWarpIntensityForDiagnostics;
            }
        }

        /// <summary>
        /// Diagnostic only: author the noise nodes without the per-octave remap.
        /// </summary>
        /// <remarks>
        /// The prime suspect for the east-west anisotropy. <c>applyRemapPerLayer</c> runs every
        /// octave through the remap curve *before* the sum, and our curve clips the bottom 10 % of
        /// each one — a strongly non-linear per-octave operation, and the most plausible way an
        /// isotropic Perlin field acquires a preferred direction. The one graph that measured
        /// isotropic does not have this field set at all.
        /// </remarks>
        public static bool DisableNoiseRemapPerLayerForDiagnostics { get; set; }

        /// <summary>
        /// Diagnostic only: author the noise nodes without the alternating octave sign.
        /// </summary>
        /// <remarks>
        /// Second suspect, same reasoning: <c>flipSign</c> negates every other octave before the
        /// sum, and it is likewise absent from the graph that measured isotropic.
        /// </remarks>
        public static bool DisableNoiseFlipSignForDiagnostics { get; set; }

        /// <summary>
        /// Diagnostic only: displace the noise sample position by another noise field.
        /// </summary>
        /// <remarks>
        /// The standard remedy for lattice-aligned gradient noise, and the last untested suspect.
        /// Every noise in this project uses <c>WarpMode.None</c>; the playbook rules warp out on
        /// cost grounds, a decision about bake time that was never tested against this defect.
        /// Turning off <c>flipSign</c> only *rotated* the anisotropy, which is what a lattice
        /// artifact does — warp is what breaks the lattice.
        /// </remarks>
        public static NoiseNode.WarpMode? NoiseWarpForDiagnostics { get; set; }

        /// <summary>
        /// Diagnostic only: how many octaves the multi-octave noises stack.
        /// </summary>
        /// <remarks>
        /// The last untested lead, and the strongest signal in the data. The best result of the whole
        /// investigation was <c>layerDerivativeMode = Pow</c>, which collapses scale and amplitude by
        /// <c>lacunarity^i</c> at every step and so leaves only two or three octaves contributing —
        /// an octave-count reduction under another name. The one graph that measured isotropic uses
        /// <c>layerCount = 4</c> against our 8. Two independent routes to the same variable.
        /// <para>
        /// If four octaves reach Pow's ratio while keeping the amplitude Pow throws away, the
        /// mechanism is octaves reinforcing a shared lattice.
        /// </para>
        /// </remarks>
        public static int? NoiseLayerCountForDiagnostics { get; set; }

        /// <summary>
        /// Diagnostic only: the ratio between successive octave scales.
        /// </summary>
        /// <remarks>
        /// The variable the evidence actually points at. Cutting octaves made the banding worse and
        /// <c>Pow</c> made it better, and the difference between them is not how many octaves survive
        /// but how far apart their scales are: Linear steps by a constant 2.25 while Pow diverges
        /// (2.25, 5.1, 11.4, 51). Evenly spaced octaves on one lattice reinforce its axes. Raising
        /// lacunarity separates them the way Pow does **without** collapsing amplitude, which is what
        /// made Pow unusable as a fix rather than a diagnostic.
        /// </remarks>
        public static float? NoiseLacunarityForDiagnostics { get; set; }

        /// <summary>Warp strength used with <see cref="NoiseWarpForDiagnostics"/>.</summary>
        public static float NoiseWarpIntensityForDiagnostics { get; set; } = 1f;

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
        /// <summary>Vista Wizard's Mountains biome — the graph the overworld is rebuilt from.</summary>
        /// <summary>The island graph as authored in the Vista editor, kept as our source.</summary>
        /// <remarks>
        /// Hand-tuned in the graph editor and copied out of the spawn folder, which the bake
        /// overwrites. It carries the whole island recipe: the height Levels at [0.22, 0.70], the
        /// stone slots already painted as grass, the rock-highlight branch removed, and the five
        /// Weight Blend layers split into two green zones -- GreenAreaMask over slots 4 and 3,
        /// DarkGreenAreaMask over 2 and 1 -- so two tree species land on different ground.
        /// Cloning it rather than re-authoring it in C# is the point: transcribing a Vista graph by
        /// hand is what produced two days of wrong answers. See the graph rule in AGENTS.md.
        /// </remarks>
        public const string AuthoredIslandGraphPath =
            "Assets/Settings/Vista/Templates/IslandBase_Authored_TerrainGraph.asset";

        public const string MountainTemplateGraphPath =
            "Assets/PinwheelStudio/Vista/Personal/BiomeTemplates/Mountain/" +
            "Mountain_TerrainGraphTemplate.asset";

        private const string SampleGrassDarkPath =
            "Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Dark/"
            + "Grass_Dark_VistaSample.terrainlayer";

        private const string SampleGrassLightPath =
            "Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Light/"
            + "Grass_Light_VistaSample.terrainlayer";

        private const string TemplateScratchPath =
            "Assets/Settings/Vista/__FromTemplate_Scratch.asset";

        /// <summary>
        /// Clones a Vista template graph onto <paramref name="targetPath"/>, applying
        /// <paramref name="patch"/> to the clone before it is written.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The copy is <see cref="AssetDatabase.CopyAsset"/>, i.e. a file copy, so the clone carries
        /// every node and every wire the template has. That is the entire point: transcribing the
        /// Mountains template into C# by hand reproduced about a third of it — three of its four
        /// Levels, none of its three Curves, neither HeightMask, no Concave, no Smooth — and one
        /// noise ended up in the height path where the template puts it in the surface stage. Two
        /// days of banding experiments were run against that partial copy.
        /// </para>
        /// <para>
        /// The clone lands on a scratch asset first and is then copied into the existing target with
        /// <see cref="EditorUtility.CopySerialized"/>, so the target keeps its GUID and the biome
        /// template prefabs that reference it keep resolving.
        /// </para>
        /// </remarks>
        public static TerrainGraph RebuildFromTemplate(
            string templatePath, string targetPath, System.Action<TerrainGraph> patch = null)
        {
            EnsureFolder("Assets/Settings/Vista");
            if (AssetDatabase.LoadAssetAtPath<TerrainGraph>(templatePath) == null)
            {
                UnityEngine.Debug.LogError($"[VistaGraphBuild] No template graph at {templatePath}.");
                return null;
            }

            AssetDatabase.DeleteAsset(TemplateScratchPath);
            if (!AssetDatabase.CopyAsset(templatePath, TemplateScratchPath))
            {
                UnityEngine.Debug.LogError(
                    $"[VistaGraphBuild] Failed to copy {templatePath} -> {TemplateScratchPath}.");
                return null;
            }

            AssetDatabase.SaveAssets();
            var working = AssetDatabase.LoadAssetAtPath<TerrainGraph>(TemplateScratchPath);
            if (working == null)
            {
                UnityEngine.Debug.LogError($"[VistaGraphBuild] Copy at {TemplateScratchPath} did not load.");
                return null;
            }

            patch?.Invoke(working);
            working.Validate();
            working.name = System.IO.Path.GetFileNameWithoutExtension(targetPath);
            EditorUtility.SetDirty(working);
            AssetDatabase.SaveAssets();

            var existing = AssetDatabase.LoadAssetAtPath<TerrainGraph>(targetPath);
            if (existing == null)
            {
                AssetDatabase.CopyAsset(TemplateScratchPath, targetPath);
                AssetDatabase.DeleteAsset(TemplateScratchPath);
                AssetDatabase.SaveAssets();
                return AssetDatabase.LoadAssetAtPath<TerrainGraph>(targetPath);
            }

            EditorUtility.CopySerialized(working, existing);
            existing.name = System.IO.Path.GetFileNameWithoutExtension(targetPath);
            EditorUtility.SetDirty(existing);
            AssetDatabase.DeleteAsset(TemplateScratchPath);
            AssetDatabase.SaveAssets();
            return existing;
        }

        /// <summary>Points a cloned template's vegetation outputs at our templates.</summary>
        /// <remarks>
        /// <para>
        /// The template already scatters trees and ground cover, and it does it well. All that is
        /// ours about the vegetation is which prefab comes out, so this reassigns the template
        /// references on the output nodes and touches nothing else -- no new nodes, no new masks,
        /// no second placement system running beside the one that ships.
        /// </para>
        /// <para>
        /// It replaces a hand-built stage of some sixty nodes that duplicated exactly this, was far
        /// harder to read than the graph it sat next to, and quietly emitted no ground cover at all.
        /// See the graph rule in AGENTS.md.
        /// </para>
        /// </remarks>
        public static void RetargetTemplateVegetation(
            TerrainGraph graph,
            VistaVegetationTemplates.Species[] canopy,
            bool includeGroundCover = true)
        {
            // One species per Tree Output, in order. The authored island graph has two, fed by two
            // green masks over different Weight Blend layers, which is what puts the species on
            // different ground; a graph with one output simply gets the first species.
            var trees = 0;
            foreach (var node in graph.GetNodesOfType<TreeOutputNode>())
            {
                node.treeTemplate = VistaVegetationTemplates.LoadOrCreateCanopyTree(
                    canopy[trees % canopy.Length]);
                trees++;
            }

            // Removed, not bypassed, when ground cover is unwanted: a bypassed output node still
            // registers its prototype on the terrain, which is how Vista's own GrassDetail kept
            // appearing in the bake report after both density outputs had been retargeted.
            var details = 0;
            foreach (var node in graph.GetNodesOfType<DetailDensityOutputNode>())
            {
                if (!includeGroundCover)
                {
                    graph.RemoveNode(node.id);
                    continue;
                }

                node.detailTemplate = VistaVegetationTemplates.LoadOrCreateMeadowGrass();
                details++;
            }

            // The instance output carries a DetailTemplate too, and it is where Vista's own
            // GrassDetail was still reaching the terrain after the density output had been
            // retargeted -- which is why a bake reported our meadow and drew theirs.
            var props = 0;
            foreach (var node in graph.GetNodesOfType<DetailInstanceOutputNode>())
            {
                if (!includeGroundCover)
                {
                    graph.RemoveNode(node.id);
                    continue;
                }

                node.detailTemplate = VistaVegetationTemplates.LoadOrCreateMeadowGrass();
                props++;
            }

            UnityEngine.Debug.Log(
                $"[VistaGraphBuild] Retargeted {trees} tree, {details} detail and {props} prop "
                + $"output(s) retargeted (canopy {string.Join(", ", canopy)}).");
        }

        /// <summary>
        /// Points a cloned template's Weight Blend texture outputs at our canonical layer assets.
        /// </summary>
        /// <param name="graph">The cloned graph, patched in place.</param>
        /// <param name="byBlendLayer">
        /// One entry per Weight Blend priority slot, lowest first. A <c>null</c> entry leaves that
        /// slot's layer alone, which is how a template layer we already share is kept.
        /// </param>
        /// <remarks>
        /// <para>
        /// URP TerrainLit draws at most eight <c>TerrainLayer</c>s, and Vista rewrites
        /// <c>terrainData.terrainLayers</c> per tile from the distinct layers every covering biome
        /// produced. Sea floor, island and massif between them were emitting <b>ten</b> — the three
        /// biomes each shipped their template's own rock and sediment samples alongside our
        /// canonical set. Retargeting the duplicates, rather than adding an eleventh, is what keeps
        /// a tile under the ceiling.
        /// </para>
        /// <para>
        /// The slot index comes from the edge, not from node order: Weight Blend output slot ids are
        /// <c>WeightBlendNode.MAX_LAYER + i</c>, so the layer a Texture Output paints is whichever
        /// slot feeds it. Node order in the asset is authoring order and means nothing.
        /// </para>
        /// </remarks>
        /// <summary>Points every Vista sample grass output at our canonical grass layer.</summary>
        /// <remarks>
        /// URP TerrainLit renders eight terrain layers: one _Control texture drives Splat0..Splat3
        /// and a single TERRAIN_SPLAT_ADDPASS drives four more. Vista builds each tile's layer
        /// array from the union of every covering biome's outputs, and the roads and settlement
        /// then append four more on top. Measured on a dressed bake that came to eleven layers on
        /// the settlement tile and ten elsewhere, so three road layers were silently not drawn.
        /// <para>
        /// The two sample layers are the cheapest two slots to reclaim: nothing references them by
        /// name, no system consumes their identity, and they carry 0.0-5.6 % of the weight
        /// depending on the tile. What is lost is the massif's two-tone grass, which collapses into
        /// one shade. That is a deliberate trade, not an oversight.
        /// </para>
        /// <para>
        /// Substitution is by layer identity rather than by blend-layer index on purpose. The
        /// island graph's blend semantics are not what the reference document describes any more -
        /// it was replaced wholesale by the authored template - so "wherever the sample grass is
        /// painted, paint the canonical grass" is the only mapping that cannot be wrong. Two
        /// outputs landing on one layer is fine: Vista keys the tile array on distinct layers and
        /// their weights add into the same channel.
        /// </para>
        /// </remarks>
        public static void ReplaceSampleGrassWithCanonical(TerrainGraph graph)
        {
            var dark = AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>(SampleGrassDarkPath);
            var light = AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>(SampleGrassLightPath);
            if (dark == null && light == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[VistaGraphBuild] Neither Vista sample grass layer resolved; nothing replaced.");
                return;
            }

            var canonical =
                OverworldTerrainLayers.GetCanonicalLayer(OverworldTerrainLayers.SurfaceLayer.Grass);
            var replaced = 0;
            foreach (var output in graph.GetNodesOfType<TextureOutputNode>())
            {
                var layer = output.terrainLayer;
                if (layer == null || (layer != dark && layer != light))
                {
                    continue;
                }

                output.terrainLayer = canonical;
                output.order = (int)OverworldTerrainLayers.SurfaceLayer.Grass;
                replaced++;
            }

            UnityEngine.Debug.Log(
                $"[VistaGraphBuild] Replaced {replaced} Vista sample grass output(s) with the "
                + "canonical grass layer.");
        }

        public static void RetargetTextureOutputsToCanonical(
            TerrainGraph graph,
            OverworldTerrainLayers.SurfaceLayer?[] byBlendLayer)
        {
            var weightBlends = graph.GetNodesOfType<WeightBlendNode>();
            var weightBlend = weightBlends.Count > 0 ? weightBlends[0] : null;
            if (weightBlend == null)
            {
                UnityEngine.Debug.LogError(
                    "[VistaGraphBuild] No Weight Blend node; texture outputs not retargeted.");
                return;
            }

            var retargeted = 0;
            foreach (var edge in graph.GetEdges())
            {
                if (edge.outputSlot.nodeId != weightBlend.id)
                {
                    continue;
                }

                if (!(graph.GetNode(edge.inputSlot.nodeId) is TextureOutputNode output))
                {
                    continue;
                }

                var blendLayer = edge.outputSlot.slotId - WeightBlendNode.MAX_LAYER;
                if (blendLayer < 0 || blendLayer >= byBlendLayer.Length)
                {
                    continue;
                }

                var layer = byBlendLayer[blendLayer];
                if (layer == null)
                {
                    continue;
                }

                output.terrainLayer = OverworldTerrainLayers.GetCanonicalLayer(layer.Value);
                output.order = (int)layer.Value;
                retargeted++;
            }

            UnityEngine.Debug.Log(
                $"[VistaGraphBuild] Retargeted {retargeted} texture output(s) to canonical layers.");
        }

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
