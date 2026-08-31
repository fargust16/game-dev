#if UNITY_EDITOR
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;
using SurfaceLayer = GameDev.EditorTools.OverworldTerrainLayers.SurfaceLayer;

namespace GameDev.EditorTools
{
    /// <summary>
    /// One stepped plateau: flat top, near-vertical wall, grass and trees on the top only.
    /// Spawned many times over the island as <c>LPB_Shelf_*</c>, blended with <b>Raise</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The relief tiers and the rules this graph implements are in
    /// <c>Assets/Plans/WORLD_RELIEF_PRINCIPLES.md</c>. In short: a plateau is caprock over softer
    /// rock, so it is a flat top plus a wall that stays steep, and it must be climbable from at
    /// least one side.
    /// </para>
    /// <para>
    /// <b>The wall is made by the biome mask, not by this graph's noise.</b> That is the whole
    /// reason shelves are separate biomes. A shelf 300 m across with a 45 m falloff resolves at
    /// 390 m / 512 px = <b>0.76 m/px</b>, against the island biome's 5.0 m/px, so a 26 m drop can
    /// be delivered over ~14 m of ground — 62 degrees, which reads as a cliff and stops a character
    /// dead. Inside the island's own graph the same intent is smeared over 5 m cells and comes out
    /// as a slope. An earlier attempt to terrace the island graph produced 1.7 m corduroy over the
    /// whole meadow; see VISTA_TERRAIN_PLAYBOOK 2.12.
    /// </para>
    /// <para>
    /// The ramp is not in this graph either — it is the tapering spur on the spawned silhouette
    /// (<c>OverworldLpbBake.BuildShelfSilhouette</c>). Where the polygon narrows to less than twice
    /// the falloff width the mask never reaches 1, so the plateau runs out as a tongue instead of a
    /// wall. That keeps the ramp direction per-instance without the bake writing any biome
    /// parameter it is not allowed to touch.
    /// </para>
    /// </remarks>
    public static class ShelfPlateauGraphBuilder
    {
        public const string GraphAssetPath = "Assets/Settings/Vista/ShelfPlateau_TerrainGraph.asset";

        /// <summary>Biome-mask value where the wall's foot sits.</summary>
        /// <remarks>
        /// The mask falls from 1 inside the anchor polygon to 0 at the outer edge of the falloff
        /// ring. Squeezing the height transition into the <see cref="MaskCliffLow"/>..
        /// <see cref="MaskCliffHigh"/> slice of that ring is what turns a 45 m ramp into a 14 m
        /// wall — the falloff stays wide enough to blend the biome cleanly, while the rock face is
        /// only as wide as the canvas can resolve.
        /// </remarks>
        private const float MaskCliffLow = 0.30f;

        /// <summary>Biome-mask value where the plateau top begins.</summary>
        private const float MaskCliffHigh = 0.62f;

        /// <summary>Wavelength of the top's unevenness, in metres.</summary>
        /// <remarks>
        /// A dead-flat top is one of the artificiality tells the terrain guides list, next to
        /// radially symmetric mountains. This is small relief, not a second landform: the top has
        /// to stay a tier apart from the wall or the two read as one muddy shape.
        /// </remarks>
        private const float TopReliefScaleMetres = 70f;

        /// <summary>Fraction of the shelf's height the top's own relief is allowed to use.</summary>
        private const float TopReliefAmount = 0.10f;

        /// <summary>Slope at which the wall stops being ground and starts being rock, in degrees.</summary>
        private const float CliffOnsetDegrees = 30f;

        /// <summary>
        /// Where in the 30..90 degree window the rock paint reaches full strength, as a fraction.
        /// </summary>
        /// <remarks>
        /// <see cref="SlopeMaskNode"/> returns <b>0 outside</b> its band, not the clamped end of the
        /// curve, so the ceiling has to be 90 or the steepest faces come out unpainted. The curve
        /// rises and then holds for the same reason — Vista's default <c>EaseInOutCurve</c> falls
        /// back to 0 at the top of the range.
        /// </remarks>
        private const float CliffFullAt = 0.25f;

        /// <summary>Wavelength of the canopy's patchiness on the top, in metres.</summary>
        private const float CanopyPatchScaleMetres = 90f;

        /// <summary>Metres between scattered trees on a plateau top.</summary>
        private const float TreeSpacingMetres = 24f;

        [MenuItem("GameDev/Overworld/Vista/Rebuild Shelf Plateau Graph (destructive)")]
        public static void RebuildMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild the shelf plateau terrain graph?",
                    "This re-authors ShelfPlateau_TerrainGraph from code, in place. Every edit made "
                    + "in the Vista graph editor is lost; references to the asset are kept.",
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
            Debug.Log($"[ShelfPlateau] Rebuilt graph at {GraphAssetPath}");
        }

        /// <summary>Loads the authored graph. Never rebuilds — the asset is the source of truth.</summary>
        public static TerrainGraph Load()
        {
            var graph = AssetDatabase.LoadAssetAtPath<TerrainGraph>(GraphAssetPath);
            if (graph == null)
            {
                Debug.LogError(
                    $"[ShelfPlateau] No terrain graph at {GraphAssetPath}. "
                    + "Run GameDev/Overworld/Vista/Rebuild Shelf Plateau Graph (destructive) once.");
            }

            return graph;
        }

        /// <summary>Re-authors the asset from code, discarding any Inspector edit.</summary>
        public static TerrainGraph Rebuild()
        {
            VistaGraphBuild.EnsureFolder("Assets/Settings/Vista");

            var graph = ScriptableObject.CreateInstance<TerrainGraph>();
            graph.name = "ShelfPlateau_TerrainGraph";

            var height = BuildTerrainStage(graph);
            var cliff = BuildSurfaceStage(graph, height);
            BuildVegetationStage(graph, cliff);

            graph.Validate();
            return VistaGraphBuild.SaveGraph(graph, GraphAssetPath);
        }

        /// <summary>Flat top with a steep wall, both derived from the biome mask.</summary>
        /// <returns>The <c>Set Variable</c> node carrying <c>Height Map</c>.</returns>
        private static SetVariableNode BuildTerrainStage(TerrainGraph graph)
        {
            var maskInput = new InputNode { inputName = GraphConstants.BIOME_MASK_INPUT_NAME };
            maskInput.SetSlotType(typeof(MaskSlot));
            graph.AddNode(maskInput);

            // inMid is the midpoint on purpose. LevelsNode is not a plain window: it maps the two
            // halves symmetrically about inMid, as inverseLerp(v, inLow, 2*inMid - inLow) below and
            // inverseLerp(v, 2*inMid - inHigh, inHigh) above. Only when inMid sits exactly halfway
            // do the two agree and the node behave like the linear window everyone assumes it is.
            var profile = new LevelsNode
            {
                inLow = MaskCliffLow,
                inMid = (MaskCliffLow + MaskCliffHigh) * 0.5f,
                inHigh = MaskCliffHigh,
                outLow = 0f,
                outHigh = 1f,
            };
            graph.AddNode(profile);
            VistaGraphBuild.Connect(
                graph, maskInput.id, maskInput.outputSlot.id, profile.id, profile.inputSlot.id);

            var relief = new NoiseNode
            {
                mode = NoiseMode.Perlin01,

                // One octave. Summed octaves run past 1 and are clamped by the shader's closing
                // remap, which leaves the field squeezed into a narrow band and any Levels window
                // on it either saturated or dead. A single octave is a bell on 0..1 centred at 0.5.
                layerCount = 1,
                scale = TopReliefScaleMetres,
                flipSign = false,
                applyRemapPerLayer = false,
                warpMode = NoiseNode.WarpMode.Angular,
                warpIntensity = 0.5f,
                seed = 4211,
            };
            graph.AddNode(relief);

            var reliefBand = new LevelsNode
            {
                inLow = 0f,
                inMid = 0.5f,
                inHigh = 1f,
                outLow = 1f - TopReliefAmount,
                outHigh = 1f,
            };
            graph.AddNode(reliefBand);
            VistaGraphBuild.Connect(
                graph, relief.id, relief.outputSlot.id, reliefBand.id, reliefBand.inputSlot.id);

            var shaped = new CombineNode { mode = CombineNode.Mode.Multiply };
            graph.AddNode(shaped);
            VistaGraphBuild.Connect(
                graph, profile.id, profile.outputSlot.id, shaped.id, shaped.backgroundSlot.id);
            VistaGraphBuild.Connect(
                graph, reliefBand.id, reliefBand.outputSlot.id, shaped.id, shaped.foregroundSlot.id);

            var setHeight = VistaGraphBuild.AddSetVar(graph, VistaGraphBuild.VarHeightMap);
            VistaGraphBuild.Connect(
                graph, shaped.id, shaped.outputSlot.id, setHeight.id, setHeight.inputSlot.id);

            var output = new HeightOutputNode();
            graph.AddNode(output);
            VistaGraphBuild.Connect(
                graph, shaped.id, shaped.outputSlot.id, output.id, output.inputSlot.id);

            return setHeight;
        }

        /// <summary>Grass on the top, rock on the wall. Two layers, both canonical.</summary>
        /// <returns>The cliff mask, reused by the vegetation stage.</returns>
        /// <remarks>
        /// Only two Weight Blend layers, and layer 0 is left unconnected as the contract requires —
        /// Vista fills it white, which is what makes the weights sum to 1. Both layers bind assets
        /// the tile already carries, so a shelf costs nothing against URP TerrainLit's eight-layer
        /// ceiling.
        /// </remarks>
        private static SlopeMaskNode BuildSurfaceStage(TerrainGraph graph, SetVariableNode height)
        {
            var heightVar = VistaGraphBuild.AddGetVar(graph, VistaGraphBuild.VarHeightMap);

            var cliff = new SlopeMaskNode
            {
                // The ceiling is raised before the floor because the setters clamp against each
                // other and would otherwise pin minAngle under the default 45.
                maxAngle = 90f,
                minAngle = CliffOnsetDegrees,
                transition = new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(CliffFullAt, 1f),
                    new Keyframe(1f, 1f)),
            };
            graph.AddNode(cliff);
            VistaGraphBuild.Connect(
                graph, heightVar.id, heightVar.outputSlot.id, cliff.id, cliff.inputSlot.id);

            var blend = new WeightBlendNode { layerCount = 2 };
            graph.AddNode(blend);
            VistaGraphBuild.Connect(
                graph, cliff.id, cliff.outputSlot.id, blend.id, blend.inputSlots[1].id);

            var grassOut = new TextureOutputNode
            {
                terrainLayer = OverworldTerrainLayers.GetCanonicalLayer(SurfaceLayer.Grass),
                order = (int)SurfaceLayer.Grass,
            };
            graph.AddNode(grassOut);
            VistaGraphBuild.Connect(
                graph, blend.id, blend.outputSlots[0].id, grassOut.id, grassOut.inputSlot.id);

            var cliffOut = new TextureOutputNode
            {
                terrainLayer = OverworldTerrainLayers.GetCanonicalLayer(SurfaceLayer.Cliff),
                order = (int)SurfaceLayer.Cliff,
            };
            graph.AddNode(cliffOut);
            VistaGraphBuild.Connect(
                graph, blend.id, blend.outputSlots[1].id, cliffOut.id, cliffOut.inputSlot.id);

            return cliff;
        }

        /// <summary>Meadow and a few trees, on the top only.</summary>
        /// <remarks>
        /// A plateau covered in bare rock is not what the reference shows: the tops are grass with
        /// trees, and only the walls are stone. The gate is <c>1 - cliff</c>, so it follows the same
        /// mask that paints the rock and cannot drift away from it.
        /// </remarks>
        private static void BuildVegetationStage(TerrainGraph graph, SlopeMaskNode cliff)
        {
            var flat = new MathNode();
            flat.maths[0] = new MathNode.MathConfig
            {
                enabled = true,
                number = 1f,
                ops = MathNode.Operator.OneMinus,
            };
            graph.AddNode(flat);
            VistaGraphBuild.Connect(
                graph, cliff.id, cliff.outputSlot.id, flat.id, flat.inputSlot.id);

            var patches = new NoiseNode
            {
                mode = NoiseMode.Perlin01,
                layerCount = 1,
                scale = CanopyPatchScaleMetres,
                flipSign = false,
                applyRemapPerLayer = false,
                warpMode = NoiseNode.WarpMode.Angular,
                warpIntensity = 0.5f,
                seed = 9137,
            };
            graph.AddNode(patches);

            var green = new CombineNode { mode = CombineNode.Mode.Multiply };
            graph.AddNode(green);
            VistaGraphBuild.Connect(
                graph, flat.id, flat.outputSlot.id, green.id, green.backgroundSlot.id);
            VistaGraphBuild.Connect(
                graph, patches.id, patches.outputSlot.id, green.id, green.foregroundSlot.id);

            var density = new DetailDensityOutputNode
            {
                detailTemplate = VistaVegetationTemplates.LoadOrCreateMeadowGrass(),
            };
            graph.AddNode(density);
            VistaGraphBuild.Connect(
                graph, green.id, green.outputSlot.id, density.id, density.inputSlot.id);

            // Groves, not a carpet: the window keeps only the strong half of the patch field, so a
            // top has clumps of trees and open grass rather than an even sprinkle.
            var canopy = new LevelsNode
            {
                inLow = 0.52f,
                inMid = 0.66f,
                inHigh = 0.80f,
                outLow = 0f,
                outHigh = 1f,
            };
            graph.AddNode(canopy);
            VistaGraphBuild.Connect(
                graph, green.id, green.outputSlot.id, canopy.id, canopy.inputSlot.id);

            var scatter = new ScatterNode
            {
                spacing = new Vector2(TreeSpacingMetres, TreeSpacingMetres),
            };
            graph.AddNode(scatter);
            VistaGraphBuild.Connect(
                graph, canopy.id, canopy.outputSlot.id, scatter.id, scatter.densityInputSlot.id);

            var trees = new TreeOutputNode
            {
                treeTemplate = VistaVegetationTemplates.LoadOrCreateCanopyTree(
                    VistaVegetationTemplates.Species.Pine),
            };
            graph.AddNode(trees);
            VistaGraphBuild.Connect(
                graph, scatter.id, scatter.outputSlot.id, trees.id, trees.positionInputSlot.id);
        }
    }
}
#endif
