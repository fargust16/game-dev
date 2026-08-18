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
                VistaGraphBuild.MountainTemplateGraphPath, GraphAssetPath);
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
            var probe = NoiseProbeForDiagnostics;
            var detailScale = DetailNoiseScaleForDiagnostics ?? (reference ? 750f : 1200f);
            var detail = probe == NoiseProbe.ReferenceDetail || ReferenceDetailNoiseForDiagnostics
                ? VistaGraphBuild.AddSampleGraphDetailNoise(graph, detailScale, seed: 110)
                : VistaGraphBuild.AddDetailNoise(graph, scale: detailScale, seed: 110);
            // The detail signal the rest of the chain sees. Normally the noise node itself; with
            // the post-stack candidate, the noise passed through normalise -> curve.
            ImageNodeBase detailSrc = detail;
            var detailSlot = detail.outputSlot.id;
            if (PostStackNormaliseForDiagnostics)
            {
                // Sum of the amplitude series the shader accumulates: 1, p, p^2 ... p^(n-1). With
                // flipSign off every octave adds, so this is the true ceiling of the raw sum.
                var amplitudeSum = 0f;
                var amplitude = 1f;
                for (var i = 0; i < detail.layerCount; i++)
                {
                    amplitudeSum += amplitude;
                    amplitude *= detail.persistence;
                }

                // Remap maps [0,1] onto [min,max], so max = 1/sum divides the sum back into [0,1].
                var normalise = new RemapNode { min = 0f, max = 1f / amplitudeSum };
                graph.AddNode(normalise);

                VistaGraphBuild.Connect(
                    graph, detail.id, detail.outputSlot.id, normalise.id, normalise.inputSlot.id);
                detailSrc = normalise;
                detailSlot = normalise.outputSlot.id;

                if (PostStackCurveForDiagnostics != PostStackCurve.None)
                {
                    var shapeCurve = new CurveNode
                    {
                        curve = PostStackCurveForDiagnostics == PostStackCurve.Shelf
                            ? new AnimationCurve(
                                new Keyframe(0.1f, 0f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f))
                            : new AnimationCurve(
                                new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f)),
                    };
                    graph.AddNode(shapeCurve);
                    VistaGraphBuild.Connect(
                        graph, normalise.id, normalise.outputSlot.id,
                        shapeCurve.id, shapeCurve.inputSlot.id);
                    detailSrc = shapeCurve;
                    detailSlot = shapeCurve.outputSlot.id;
                }
                Debug.LogWarning(
                    $"[IslandPlateau] Post-stack normalise ON: sum={amplitudeSum:F4} over "
                    + $"{detail.layerCount} octaves, curve={PostStackCurveForDiagnostics}.");
            }

            var mass = VistaGraphBuild.AddMassNoise(
                graph, scale: reference ? 1000f : 1900f, seed: 277);

            if (NoiseOffsetForDiagnostics.HasValue)
            {
                detail.offset = NoiseOffsetForDiagnostics.Value;
                mass.offset = NoiseOffsetForDiagnostics.Value;
            }
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

            // --- Noise probe: publish the raw signal and stop. Nothing below runs. ---
            var isEarlyProbe = probe == NoiseProbe.Detail || probe == NoiseProbe.Mass ||
                probe == NoiseProbe.Product || probe == NoiseProbe.ReferenceDetail;
            if (isEarlyProbe)
            {
                var probeSet = VistaGraphBuild.AddSetVar(graph, VistaGraphBuild.VarHeightMap);
                var probeOut = new HeightOutputNode();
                graph.AddNode(probeOut);

                if (probe == NoiseProbe.Product)
                {
                    VistaGraphBuild.Connect(
                        graph, detailSrc.id, detailSlot, shape.id, shape.backgroundSlot.id);
                    VistaGraphBuild.Connect(
                        graph, mass.id, mass.outputSlot.id, shape.id, shape.foregroundSlot.id);
                    VistaGraphBuild.Connect(
                        graph, shape.id, shape.outputSlot.id, probeSet.id, probeSet.inputSlot.id);
                }
                else
                {
                    var src = probe == NoiseProbe.Mass ? (ImageNodeBase)mass : detail;
                    VistaGraphBuild.Connect(
                        graph, src.id,
                        probe == NoiseProbe.Mass ? mass.outputSlot.id : detail.outputSlot.id,
                        probeSet.id, probeSet.inputSlot.id);
                }

                VistaGraphBuild.Connect(
                    graph, probeSet.id, probeSet.outputSlot.id, probeOut.id, probeOut.inputSlot.id);
                Debug.LogWarning(
                    $"[IslandPlateau] NOISE PROBE {probe}: raw noise is the height map. Levels, " +
                    "ShelfCurve and erosion are all bypassed; surface and vegetation output is " +
                    "meaningless.");
                return;
            }

            // Two passes, as the reference has — see the note in MountainMassifGraphBuilder.
            var macroErosion = VistaGraphBuild.AddMacroErosion(graph);
            var microErosion = VistaGraphBuild.AddMicroErosion(graph);

            var setHeight = VistaGraphBuild.AddSetVar(graph, VistaGraphBuild.VarHeightMap);
            var heightOut = new HeightOutputNode();
            graph.AddNode(heightOut);

            VistaGraphBuild.Connect(graph, detailSrc.id, detailSlot, shape.id, shape.backgroundSlot.id);
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

            // --- Stage probe: publish an intermediate instead of the finished height. ---
            // The whole chain is still built and still executes; only what feeds Height Map
            // changes, so each stage is measured through the identical downstream machinery.
            ImageNodeBase stageSrc = microErosion;
            var stageSlot = microErosion.outputHeightSlot.id;
            if (probe == NoiseProbe.AfterLevels)
            {
                stageSrc = levels; stageSlot = levels.outputSlot.id;
            }
            else if (probe == NoiseProbe.AfterShelf && shelf != null)
            {
                stageSrc = shelf; stageSlot = shelf.outputSlot.id;
            }
            else if (probe == NoiseProbe.AfterMacroErosion)
            {
                stageSrc = macroErosion; stageSlot = macroErosion.outputHeightSlot.id;
            }

            if (probe != NoiseProbe.Off)
            {
                Debug.LogWarning($"[IslandPlateau] STAGE PROBE {probe}: Height Map is an "
                    + "intermediate, not the finished terrain.");
            }

            VistaGraphBuild.Connect(
                graph, stageSrc.id, stageSlot, setHeight.id, setHeight.inputSlot.id);
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
