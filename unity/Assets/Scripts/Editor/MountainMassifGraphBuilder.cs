#if UNITY_EDITOR
using System.IO;
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// One mountain massif as a <b>height delta</b> for Raise blend: foothills near 0,
    /// ridge/peaks in the core — not a dome shaped by Outer falloff alone.
    /// <para>
    /// Relief is hierarchical and deliberately irregular: macro asymmetric mass → masked major
    /// ridges → Voronoi spurs/benches → medium breakup → hydraulic erosion driven by a
    /// non-uniform hardness field → thermal talus → weak micro breakup. Nothing here repeats at a
    /// fixed spacing, which is the whole point (see <see cref="Rebuild"/> notes).
    /// </para>
    /// </summary>
    public static class MountainMassifGraphBuilder
    {
        public const string GraphAssetPath = "Assets/Settings/Vista/MountainMassif_TerrainGraph.asset";
        public const int ExpectedTextureOutputs = 4;
        /// <summary>Bump to force LoadOrRebuild when peak curve / maxHeight changes.</summary>
        public const float ExpectedBodyMaxHeight = 1f;

        /// <summary>
        /// Angular-warp intensity is in <b>normalized biome space</b>: the shader offsets the sample by
        /// <c>float2(cos,sin) * warpIntensity * 0.001</c> in 0..1 local coords, so world metres of warp
        /// ≈ <c>warpIntensity * 0.001 * biomeWidth</c>. At the ~2500 m massif footprint one unit is only
        /// ~2.5 m — the 1..2.5 values used elsewhere in this project are effectively no-ops. Tens are
        /// required here for a visible organic bend. Do not "tidy" these back down to single digits.
        /// </summary>
        private const float RidgeWarpIntensity = 28f;
        private const float SpurWarpIntensity = 18f;
        private const float MediumWarpIntensity = 9f;

        private const string GrassLayerPath = "Assets/Settings/TerrainLayers/Grass_Fields.terrainlayer";
        private const string SoilLayerPath = "Assets/Settings/TerrainLayers/Dirt_Scaled.terrainlayer";
        private const string RockLayerPath = "Assets/Settings/TerrainLayers/MicroSplat/MS_Rock_Cliff.terrainlayer";
        private const string CliffLayerPath = "Assets/Settings/TerrainLayers/Cliff_Rock.terrainlayer";

        [MenuItem("GameDev/Overworld/Vista/Rebuild Mountain Massif Graph")]
        public static void RebuildMenu()
        {
            var graph = Rebuild();
            if (graph == null)
                return;
            Selection.activeObject = graph;
            EditorGUIUtility.PingObject(graph);
            Debug.Log($"[MountainMassif] Rebuilt multi-scale irregular relief graph at {GraphAssetPath}");
        }

        public static TerrainGraph LoadOrRebuild()
        {
            var graph = AssetDatabase.LoadAssetAtPath<TerrainGraph>(GraphAssetPath);
            if (graph != null &&
                graph.GetNode<HeightOutputNode>() != null &&
                graph.GetNode<GenericMountainNode>() != null &&
                graph.GetNodesOfType<TextureOutputNode>().Count >= ExpectedTextureOutputs &&
                IsCurrentGraphShape(graph))
            {
                var body = graph.GetNode<GenericMountainNode>() as GenericMountainNode;
                if (body != null && Mathf.Abs(body.maxHeight - ExpectedBodyMaxHeight) < 0.01f)
                    return graph;
            }

            return Rebuild();
        }

        /// <summary>
        /// Structural signature of the current chain. maxHeight is clamped to 0..1 so it cannot carry a
        /// version number — a checked-in asset from the old RidgedMountain chain has to be detected by
        /// shape instead, or LoadOrRebuild would keep serving it forever.
        /// </summary>
        private static bool IsCurrentGraphShape(TerrainGraph graph)
        {
            return graph.GetNodesOfType<RidgedMountainNode>().Count == 0 &&
                   graph.GetNodesOfType<ThermalErosionNode3>().Count == 1 &&
                   graph.GetNodesOfType<VoronoiNode>().Count == 1;
        }

        public static TerrainGraph Rebuild()
        {
            EnsureFolder("Assets/Settings");
            EnsureFolder("Assets/Settings/Vista");
            if (AssetDatabase.LoadAssetAtPath<TerrainGraph>(GraphAssetPath) != null)
                AssetDatabase.DeleteAsset(GraphAssetPath);

            var graph = ScriptableObject.CreateInstance<TerrainGraph>();
            graph.name = "MountainMassif_TerrainGraph";

            // ---- A. Macro mass -------------------------------------------------------------------
            // Periphery low so Raise does not stamp a plateau; core reaches ~1.
            var body = new GenericMountainNode();
            body.mountainScale = 1500f;
            body.peakVariationScale = 2100f;
            body.minHeight = 0f;
            body.maxHeight = 1f;
            body.seed = 17;

            // Sub-cycle gradient across the whole massif — one flank ends up taller/broader than the
            // other. Single octave keeps the output strictly 0..1 so it is safe as a Multiply factor
            // (multi-octave Perlin01 alternates sign via flipSign and can go negative).
            var macroTilt = new NoiseNode();
            macroTilt.scale = 2200f;
            macroTilt.layerCount = 1;
            macroTilt.persistence = 0.5f;
            macroTilt.lacunarity = 2f;
            macroTilt.mode = NoiseMode.Perlin01;
            macroTilt.warpMode = NoiseNode.WarpMode.None;
            macroTilt.applyRemapPerLayer = false;
            // Suppresses one flank to ~52%: off-centre summit, one long ramp and one headwall. The
            // mass lost here is restored by the RemapNode further down, so this only shapes, never shrinks.
            macroTilt.remapCurve = AnimationCurve.Linear(0f, 0.52f, 1f, 1f);
            macroTilt.seed = 613;

            var massTilt = new CombineNode();
            massTilt.mode = CombineNode.Mode.Multiply;
            massTilt.backgroundMultiplier = 1f;
            massTilt.foregroundMultiplier = 1f;

            // ---- B. Irregular major ridges / valleys ---------------------------------------------
            // Ridged noise creases along zero crossings; Angular warp takes its offset ANGLE from noise
            // (unlike RidgedMountainNode's hardcoded Directional warp at a fixed 0°, which combs the
            // field into parallel streaks — that node is why the old massif looked pleated).
            // Non-integer lacunarity keeps octaves from stacking into a lattice.
            var majorRidges = new NoiseNode();
            majorRidges.scale = 700f;
            majorRidges.layerCount = 3;
            majorRidges.lacunarity = 2.07f;
            majorRidges.persistence = 0.42f;
            majorRidges.mode = NoiseMode.Ridged;
            // flipSign defaults to TRUE, which alternates octave signs. That is right for Perlin01 but
            // wrong for Ridged — the octaves cancel instead of stacking into fractal crests.
            majorRidges.flipSign = false;
            majorRidges.warpMode = NoiseNode.WarpMode.Angular;
            majorRidges.warpAngleMin = -360f;
            majorRidges.warpAngleMax = 360f;
            majorRidges.warpIntensity = RidgeWarpIntensity;
            majorRidges.applyRemapPerLayer = true;
            // Amplitude budget: with flipSign off the octaves sum, and Noise.shader always runs the
            // summed value through a clamped 0..1 remap texture — anything over 1.0 comes out
            // flat-topped. 0.60 × (1 + 0.42 + 0.176) = 0.958.
            majorRidges.remapCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.60f);
            majorRidges.seed = 91;

            // Spatially varying ridge strength — the main lever for "different parts of the mountain
            // look different". CombineNode does lerp(bg, blend, mask), so some flanks stay smooth
            // whole slopes while others are heavily ridged, with the boundary set by noise, not spacing.
            var ridgeStrengthMask = new NoiseNode();
            ridgeStrengthMask.scale = 1600f;
            ridgeStrengthMask.layerCount = 1;
            ridgeStrengthMask.persistence = 0.5f;
            ridgeStrengthMask.mode = NoiseMode.Perlin01;
            ridgeStrengthMask.warpMode = NoiseNode.WarpMode.None;
            ridgeStrengthMask.applyRemapPerLayer = false;
            ridgeStrengthMask.remapCurve = AnimationCurve.Linear(0f, 0.20f, 1f, 1f);
            ridgeStrengthMask.seed = 2207;

            var addRidges = new CombineNode();
            addRidges.mode = CombineNode.Mode.Add;
            addRidges.backgroundMultiplier = 1f;
            addRidges.foregroundMultiplier = 0.42f;
            addRidges.maskMultiplier = 1f;

            // ---- C. Secondary spurs and benches ---------------------------------------------------
            // Cells (not outlines): broad irregular plateaus read as shelves/benches. Outlines would
            // draw a web of lines, which is the regularity we are removing.
            var spurs = new VoronoiNode();
            spurs.scale = 520f;
            spurs.layerCount = 2;
            spurs.lacunarity = 2f;
            spurs.persistence = 0.4f;
            spurs.amplitudeExponent = 1f;
            spurs.inverse = false;
            spurs.warpMode = VoronoiNode.WarpMode.Angular;
            spurs.warpAngleMin = -360f;
            spurs.warpAngleMax = 360f;
            spurs.warpIntensity = SpurWarpIntensity;
            spurs.remapCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.7f);
            spurs.seed = 733;

            var addSpurs = new CombineNode();
            addSpurs.mode = CombineNode.Mode.Add;
            addSpurs.backgroundMultiplier = 1f;
            addSpurs.foregroundMultiplier = 0.16f;

            // ---- D. Medium-scale breakup ----------------------------------------------------------
            // Signed (flipSign default) so it both adds and subtracts — keeps flanks from sharing one
            // uniform surface character.
            var mediumBreak = new NoiseNode();
            mediumBreak.scale = 230f;
            mediumBreak.layerCount = 4;
            mediumBreak.lacunarity = 2.13f;
            mediumBreak.persistence = 0.42f;
            mediumBreak.mode = NoiseMode.Perlin01;
            mediumBreak.warpMode = NoiseNode.WarpMode.Angular;
            mediumBreak.warpAngleMin = -360f;
            mediumBreak.warpAngleMax = 360f;
            mediumBreak.warpIntensity = MediumWarpIntensity;
            // Budget: flipSign alternates octaves, so the positive peak is 1 + 0.176 = 1.176 and would
            // clip against the shader's 0..1 remap texture. 0.84 × 1.176 = 0.988.
            mediumBreak.applyRemapPerLayer = true;
            mediumBreak.remapCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.84f);
            mediumBreak.seed = 1289;

            var addMedium = new CombineNode();
            addMedium.mode = CombineNode.Mode.Add;
            addMedium.backgroundMultiplier = 1f;
            addMedium.foregroundMultiplier = 0.12f;

            // RemapNode GPU-scans the field's real min/max and stretches it to exactly 0..1
            // (VistaLib.Remap). Two jobs: the periphery is guaranteed to reach 0 so Raise blends into
            // the island instead of stamping a step, and the additive stack can never overshoot into
            // the CurveNode's clamp and flatten the summit into a mesa. Costs one GPU→CPU sync.
            var normalize = new RemapNode();
            normalize.min = 0f;
            normalize.max = 1f;

            // ---- E. Profile -----------------------------------------------------------------------
            // Foothills stay low at the rim; mid→peak rises hard so the Raise delta shows. Mid-band
            // gain is ~1.4 (was ~1.8): a uniformly steepened cone is exactly what lets erosion cut
            // evenly spaced radial rills.
            var foothillCurve = new CurveNode();
            foothillCurve.curve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0.35f),
                new Keyframe(0.22f, 0.09f, 0.55f, 0.55f),
                new Keyframe(0.45f, 0.30f, 0.95f, 0.95f),
                new Keyframe(0.68f, 0.62f, 1.15f, 1.15f),
                new Keyframe(0.86f, 0.90f, 1f, 1f),
                new Keyframe(1f, 1f, 0.5f, 0f));

            // ---- F. Hydraulic erosion with a non-uniform hardness field ---------------------------
            // HydraulicErosion2.compute: erosionMask = 1 - hardness, so 1 = resists, 0 = fully erodible
            // (an unconnected slot is black = fully erodible, the old behaviour). Feeding smooth
            // geological bands here is the strongest anti-regularity lever available: soft bands are
            // cut into ravines while hard bands stand as buttresses and rock walls, so drainage stops
            // being a radial comb and becomes a dendritic network.
            var hardnessField = new NoiseNode();
            hardnessField.scale = 900f;
            hardnessField.layerCount = 1;
            hardnessField.persistence = 0.45f;
            hardnessField.lacunarity = 2f;
            hardnessField.mode = NoiseMode.Perlin01;
            hardnessField.warpMode = NoiseNode.WarpMode.None;
            hardnessField.applyRemapPerLayer = false;
            hardnessField.seed = 4177;

            var hardnessLevels = new LevelsNode();
            hardnessLevels.inLow = 0.28f;
            hardnessLevels.inMid = 0.5f;
            hardnessLevels.inHigh = 0.74f;
            hardnessLevels.outLow = 0.12f;
            hardnessLevels.outHigh = 0.88f;

            // Single HydraulicErosion2 — dual instances share static temp RT names and look like "fangs".
            // featureSize is METRES PER SIM PIXEL: sim canvas = clamp(worldWidth / featureSize, 32,
            // min(2048, baseResolution)). At the ~2500 m massif and baseResolution 1024 this asks for
            // ~424 px (was 192 px at featureSize 12 / baseResolution 512).
            var erosion = new HydraulicErosionNode2();
            erosion.featureSize = 6;
            erosion.iterationCount = 320;
            erosion.rainRate = 8f;
            erosion.erosionRate = 9f;
            // Was 0.3 — far too low. Without deposition every channel keeps the same depth and the
            // slope never varies; alluvial fill is what makes valleys read as valleys.
            erosion.depositionRate = 2.5f;
            erosion.evaporationRate = 12f;
            erosion.sedimentCapacity = 100f;
            erosion.depositRamp = 5f;
            erosion.drainageRamp = 100f;
            erosion.useBorderFade = true;
            erosion.useAutoIterationPerFrame = true;

            // ---- G. Thermal talus -----------------------------------------------------------------
            // Scree aprons and broken slopes; also relaxes leftover rills. Distinct temp RT names from
            // the hydraulic node (~ThermalErosion3_* vs ~HydraulicErosion2_*), so chaining is safe.
            // Runs at full output resolution, not a downsampled canvas — keep iterations modest, the
            // node default of 1000 is far too expensive at 1024².
            var talus = new ThermalErosionNode3();
            talus.talusAngle = 42f;
            talus.erosionRate = 0.8f;
            talus.iterationCount = 240;
            talus.useMultiResolution = true;
            talus.useBorderFade = true;
            talus.useAutoIterationPerFrame = true;

            // ---- H. Weak high-frequency breakup ---------------------------------------------------
            // After erosion, so erosion cannot amplify it into more rills. Masked so it is not a
            // uniform sprinkle of the same noise everywhere.
            var microBreak = new NoiseNode();
            microBreak.scale = 65f;
            microBreak.layerCount = 3;
            microBreak.lacunarity = 2.19f;
            microBreak.persistence = 0.4f;
            microBreak.mode = NoiseMode.Perlin01;
            microBreak.warpMode = NoiseNode.WarpMode.None;
            // Budget: positive peak 1 + 0.16 = 1.16 → 0.85 × 1.16 = 0.986.
            microBreak.applyRemapPerLayer = true;
            microBreak.remapCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.85f);
            microBreak.seed = 5501;

            var microMask = new NoiseNode();
            microMask.scale = 1100f;
            microMask.layerCount = 1;
            microMask.persistence = 0.5f;
            microMask.mode = NoiseMode.Perlin01;
            microMask.warpMode = NoiseNode.WarpMode.None;
            microMask.applyRemapPerLayer = false;
            microMask.remapCurve = AnimationCurve.Linear(0f, 0.15f, 1f, 1f);
            microMask.seed = 6113;

            var addMicro = new CombineNode();
            addMicro.mode = CombineNode.Mode.Add;
            addMicro.backgroundMultiplier = 1f;
            addMicro.foregroundMultiplier = 0.055f;
            addMicro.maskMultiplier = 1f;

            var heightOut = new HeightOutputNode();

            // ---- Texture branch -------------------------------------------------------------------
            // NOT what renders. OverworldMicroSplatSetup.PaintUnifiedIslandSplats replaces
            // terrainLayers and overwrites every weight after the bake, so this branch is dead weight
            // kept valid for the day Vista texture outputs become source of truth. It uses
            // GeometryMaskNode (slope AND height in one node, same cost as the old SlopeMaskNode) plus
            // a low-frequency noise multiply so the bands are not pure slope contours. Curvature is
            // deliberately NOT computed here — CurvatureNode does several blur passes on every bake for
            // output nobody reads; the painter derives local relief from the heightmap instead.
            var texVariation = new NoiseNode();
            texVariation.scale = 620f;
            texVariation.layerCount = 1;
            texVariation.persistence = 0.5f;
            texVariation.mode = NoiseMode.Perlin01;
            texVariation.warpMode = NoiseNode.WarpMode.None;
            texVariation.applyRemapPerLayer = false;
            texVariation.remapCurve = AnimationCurve.Linear(0f, 0.55f, 1f, 1f);
            texVariation.seed = 7229;

            var soilGeo = new GeometryMaskNode();
            soilGeo.blendMode = GeometryMaskNode.BlendMode.Multiply;
            soilGeo.enableSlopeMask = true;
            soilGeo.minAngle = 8f;
            soilGeo.maxAngle = 32f;
            soilGeo.slopeTransition = Utilities.EaseInOutCurve();

            var rockGeo = new GeometryMaskNode();
            rockGeo.blendMode = GeometryMaskNode.BlendMode.Multiply;
            rockGeo.enableSlopeMask = true;
            rockGeo.minAngle = 20f;
            rockGeo.maxAngle = 48f;
            rockGeo.slopeTransition = Utilities.EaseInOutCurve();
            rockGeo.enableHeightMask = true;
            rockGeo.minHeight = 90f;
            rockGeo.maxHeight = 520f;
            rockGeo.heightTransition = Utilities.EaseInOutCurve();

            var cliffGeo = new GeometryMaskNode();
            cliffGeo.blendMode = GeometryMaskNode.BlendMode.Multiply;
            cliffGeo.enableSlopeMask = true;
            cliffGeo.minAngle = 38f;
            cliffGeo.maxAngle = 85f;
            cliffGeo.slopeTransition = Utilities.EaseInOutCurve();

            var rockVaried = new CombineNode();
            rockVaried.mode = CombineNode.Mode.Multiply;
            rockVaried.backgroundMultiplier = 1f;
            rockVaried.foregroundMultiplier = 1f;

            var cliffVaried = new CombineNode();
            cliffVaried.mode = CombineNode.Mode.Multiply;
            cliffVaried.backgroundMultiplier = 1f;
            cliffVaried.foregroundMultiplier = 1f;

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

            var cliffOut = new TextureOutputNode();
            cliffOut.order = 3;
            cliffOut.terrainLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(CliffLayerPath);

            graph.AddNode(body);
            graph.AddNode(macroTilt);
            graph.AddNode(massTilt);
            graph.AddNode(majorRidges);
            graph.AddNode(ridgeStrengthMask);
            graph.AddNode(addRidges);
            graph.AddNode(spurs);
            graph.AddNode(addSpurs);
            graph.AddNode(mediumBreak);
            graph.AddNode(addMedium);
            graph.AddNode(normalize);
            graph.AddNode(foothillCurve);
            graph.AddNode(hardnessField);
            graph.AddNode(hardnessLevels);
            graph.AddNode(erosion);
            graph.AddNode(talus);
            graph.AddNode(microBreak);
            graph.AddNode(microMask);
            graph.AddNode(addMicro);
            graph.AddNode(heightOut);
            graph.AddNode(texVariation);
            graph.AddNode(soilGeo);
            graph.AddNode(rockGeo);
            graph.AddNode(cliffGeo);
            graph.AddNode(rockVaried);
            graph.AddNode(cliffVaried);
            graph.AddNode(weightBlend);
            graph.AddNode(grassOut);
            graph.AddNode(soilOut);
            graph.AddNode(rockOut);
            graph.AddNode(cliffOut);

            // Height: mass → ridges → spurs → medium → profile → hydraulic → thermal → micro → out.
            Connect(graph, body.id, body.outputSlot.id, massTilt.id, massTilt.backgroundSlot.id);
            Connect(graph, macroTilt.id, macroTilt.outputSlot.id, massTilt.id, massTilt.foregroundSlot.id);

            Connect(graph, massTilt.id, massTilt.outputSlot.id, addRidges.id, addRidges.backgroundSlot.id);
            Connect(graph, majorRidges.id, majorRidges.outputSlot.id, addRidges.id, addRidges.foregroundSlot.id);
            Connect(graph, ridgeStrengthMask.id, ridgeStrengthMask.outputSlot.id, addRidges.id, addRidges.maskSlot.id);

            Connect(graph, addRidges.id, addRidges.outputSlot.id, addSpurs.id, addSpurs.backgroundSlot.id);
            Connect(graph, spurs.id, spurs.cellsSlot.id, addSpurs.id, addSpurs.foregroundSlot.id);

            Connect(graph, addSpurs.id, addSpurs.outputSlot.id, addMedium.id, addMedium.backgroundSlot.id);
            Connect(graph, mediumBreak.id, mediumBreak.outputSlot.id, addMedium.id, addMedium.foregroundSlot.id);

            Connect(graph, addMedium.id, addMedium.outputSlot.id, normalize.id, normalize.inputSlot.id);
            Connect(graph, normalize.id, normalize.outputSlot.id, foothillCurve.id, foothillCurve.inputSlot.id);

            Connect(graph, hardnessField.id, hardnessField.outputSlot.id, hardnessLevels.id, hardnessLevels.inputSlot.id);
            Connect(graph, foothillCurve.id, foothillCurve.outputSlot.id, erosion.id, erosion.inputHeightSlot.id);
            Connect(graph, hardnessLevels.id, hardnessLevels.outputSlot.id, erosion.id, erosion.hardnessSlot.id);

            Connect(graph, erosion.id, erosion.outputHeightSlot.id, talus.id, talus.inputHeightSlot.id);
            Connect(graph, hardnessLevels.id, hardnessLevels.outputSlot.id, talus.id, talus.hardnessSlot.id);

            Connect(graph, talus.id, talus.outputHeightSlot.id, addMicro.id, addMicro.backgroundSlot.id);
            Connect(graph, microBreak.id, microBreak.outputSlot.id, addMicro.id, addMicro.foregroundSlot.id);
            Connect(graph, microMask.id, microMask.outputSlot.id, addMicro.id, addMicro.maskSlot.id);

            Connect(graph, addMicro.id, addMicro.outputSlot.id, heightOut.id, heightOut.inputSlot.id);

            // Textures read the final height so masks match what ships.
            Connect(graph, addMicro.id, addMicro.outputSlot.id, soilGeo.id, soilGeo.inputSlot.id);
            Connect(graph, addMicro.id, addMicro.outputSlot.id, rockGeo.id, rockGeo.inputSlot.id);
            Connect(graph, addMicro.id, addMicro.outputSlot.id, cliffGeo.id, cliffGeo.inputSlot.id);

            Connect(graph, rockGeo.id, rockGeo.outputSlot.id, rockVaried.id, rockVaried.backgroundSlot.id);
            Connect(graph, texVariation.id, texVariation.outputSlot.id, rockVaried.id, rockVaried.foregroundSlot.id);
            Connect(graph, cliffGeo.id, cliffGeo.outputSlot.id, cliffVaried.id, cliffVaried.backgroundSlot.id);
            Connect(graph, texVariation.id, texVariation.outputSlot.id, cliffVaried.id, cliffVaried.foregroundSlot.id);

            // WeightBlend: L0 grass (default white), L1 soil, L2 rock, L3 cliff.
            Connect(graph, soilGeo.id, soilGeo.outputSlot.id, weightBlend.id, weightBlend.inputSlots[1].id);
            Connect(graph, rockVaried.id, rockVaried.outputSlot.id, weightBlend.id, weightBlend.inputSlots[2].id);
            Connect(graph, cliffVaried.id, cliffVaried.outputSlot.id, weightBlend.id, weightBlend.inputSlots[3].id);

            Connect(graph, weightBlend.id, weightBlend.outputSlots[0].id, grassOut.id, grassOut.inputSlot.id);
            Connect(graph, weightBlend.id, weightBlend.outputSlots[1].id, soilOut.id, soilOut.inputSlot.id);
            Connect(graph, weightBlend.id, weightBlend.outputSlots[2].id, rockOut.id, rockOut.inputSlot.id);
            Connect(graph, weightBlend.id, weightBlend.outputSlots[3].id, cliffOut.id, cliffOut.inputSlot.id);

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
