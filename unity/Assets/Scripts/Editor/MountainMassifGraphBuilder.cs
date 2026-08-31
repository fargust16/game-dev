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
                Patch);
        }

        /// <summary>
        /// Weight Blend priority slot -> canonical layer, lowest slot first.
        /// <c>null</c> keeps the template's own layer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The template paints five Vista sample layers: Rock Base, Sediment, Grass Dark,
        /// Grass Light, Rock Convex. Three of them duplicate rock and soil the canonical stack
        /// already carries, and every distinct layer costs a slot in
        /// <c>terrainData.terrainLayers</c>, which Vista rewrites per tile from the union of the
        /// covering biomes. Sea floor, island and massif were emitting ten between them, against
        /// URP TerrainLit's ceiling of eight.
        /// </para>
        /// <para>
        /// The two greens are deliberately left alone. They are the same two assets the island
        /// paints, so they are already shared and cost nothing extra - and collapsing both onto one
        /// canonical grass would put two Texture Outputs on a single layer, where the second would
        /// contend with the first rather than add to it.
        /// </para>
        /// </remarks>
        private static readonly SurfaceLayer?[] CanonicalBlendLayers =
        {
            SurfaceLayer.Rock,   // Rock Base   - the unconnected white base
            SurfaceLayer.Soil,   // Sediment    - settles in the drainages
            null,                // Grass Dark  - shared with the island
            null,                // Grass Light - shared with the island
            SurfaceLayer.Cliff,  // Rock Convex - the sheer faces
        };

        private static void Patch(TerrainGraph graph)
        {
            // No ground cover: the massif is rock and scree above the treeline, and grass on it
            // was Vista's, not ours.
            VistaGraphBuild.RetargetTemplateVegetation(
                graph,
                new[] { VistaVegetationTemplates.Species.Pine },
                includeGroundCover: false);
            VistaGraphBuild.RetargetTextureOutputsToCanonical(graph, CanonicalBlendLayers);
            VistaGraphBuild.ReplaceSampleGrassWithCanonical(graph);
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
