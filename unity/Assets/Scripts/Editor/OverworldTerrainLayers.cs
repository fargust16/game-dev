#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// The canonical overworld <see cref="TerrainLayer"/> stack that both Vista terrain graphs bind
    /// their <c>TextureOutputNode</c>s to.
    /// </summary>
    /// <remarks>
    /// This class owns the layer <em>assets</em> and nothing else. It does not paint, does not
    /// assign <c>terrainData.terrainLayers</c>, and does not touch materials.
    /// <para>
    /// Vista authors the per-tile layer array itself: <c>TerrainGraphUtilities</c> collects the
    /// graph's texture outputs sorted by <c>order</c>, <c>BiomeTextureBlend.BlendTextureWeights</c>
    /// concatenates them across biomes, and <c>TerrainTile.PopulateLayerWeights</c> writes
    /// <c>srcLayers.Distinct()</c> into the terrain. Layers are matched by <em>asset identity</em>,
    /// which is why every graph must bind these exact assets and why every graph emits the full
    /// canonical set — that keeps each tile's stack identical regardless of which biomes touched it.
    /// </para>
    /// <para>
    /// Appearance (albedo, normal, UV tiling, smoothness, remap) belongs to the layer asset and is
    /// edited in the Inspector. Do not set those fields from generation code.
    /// </para>
    /// </remarks>
    public static class OverworldTerrainLayers
    {
        private const string SettingsFolder = "Assets/Settings";
        private const string TerrainLayersFolder = SettingsFolder + "/TerrainLayers";
        private const string LayersFolder = TerrainLayersFolder + "/Overworld";
        private const string VistaSampleLayers =
            "Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers";

        /// <summary>
        /// UV tiling applied when a layer asset is first created. Editable in the Inspector
        /// afterwards — this value is a starting point, not an authority.
        /// </summary>
        private const float InitialTileMeters = 12f;

        /// <summary>
        /// The overworld surface layers, in canonical alphamap order.
        /// </summary>
        /// <remarks>
        /// The enum value is the <c>TextureOutputNode.order</c> every graph must use, so all graphs
        /// agree on the stack order. This is the surface <em>identity</em>, not a Weight Blend
        /// priority — each graph picks its own blend priority independently.
        /// </remarks>
        public enum SurfaceLayer
        {
            Grass = 0,
            Sand = 1,
            Soil = 2,
            Cliff = 3,
            Rock = 4,
        }

        /// <summary>Number of entries in <see cref="SurfaceLayer"/>.</summary>
        public const int CanonicalLayerCount = 5;

        private static TerrainLayer[] s_canonicalLayers;

        [MenuItem("GameDev/Overworld/Vista/Rebuild Canonical Terrain Layers")]
        private static void RebuildCanonicalLayersMenu()
        {
            s_canonicalLayers = null;
            var layers = EnsureCanonicalLayers();
            var names = new List<string>(layers.Length);
            for (var i = 0; i < layers.Length; i++)
            {
                names.Add(layers[i] != null ? $"[{i}] {layers[i].name}" : $"[{i}] NULL");
            }

            Debug.Log($"[Overworld] Canonical terrain layers: {string.Join(", ", names)}");
        }

        /// <summary>
        /// Loads (creating on first use) the canonical layer assets in <see cref="SurfaceLayer"/> order.
        /// </summary>
        /// <remarks>
        /// Textures come from Vista's own sample set so the overworld starts from the same material
        /// vocabulary as the reference biomes. Existing assets are never re-stamped: only a missing
        /// asset is created and populated, so Inspector edits survive.
        /// </remarks>
        public static TerrainLayer[] EnsureCanonicalLayers()
        {
            EnsureFolder();
            var layers = new[]
            {
                LoadOrCreateLayer(
                    "Overworld_Grass", $"{VistaSampleLayers}/Grass Dark/grass_dark_diff.jpg",
                    $"{VistaSampleLayers}/Grass Dark/grass_dark_nor_gl.exr"),
                LoadOrCreateLayer(
                    "Overworld_Sand", $"{VistaSampleLayers}/Sand/sand_diff.png",
                    $"{VistaSampleLayers}/Sand/sand_nor_gl.png"),
                LoadOrCreateLayer(
                    "Overworld_Soil", $"{VistaSampleLayers}/Sediment/sediment_diff.jpg",
                    $"{VistaSampleLayers}/Sediment/sediment_nor_gl.exr"),
                LoadOrCreateLayer(
                    "Overworld_Cliff", $"{VistaSampleLayers}/RockFace/rock_face_diff_1k.jpg",
                    $"{VistaSampleLayers}/RockFace/rock_face_nor_gl_1k.exr"),
                LoadOrCreateLayer(
                    "Overworld_Rock", $"{VistaSampleLayers}/Rock Base/rock_base_diff.jpg",
                    $"{VistaSampleLayers}/Rock Base/rock_base_nor_gl.exr"),
            };

            AssetDatabase.SaveAssets();
            return layers;
        }

        /// <summary>Single canonical layer, for a graph builder binding a <c>TextureOutputNode</c>.</summary>
        public static TerrainLayer GetCanonicalLayer(SurfaceLayer layer)
        {
            // Cached so building a graph with five outputs does not run five SaveAssets passes.
            if (s_canonicalLayers == null ||
                s_canonicalLayers.Length < CanonicalLayerCount ||
                System.Array.IndexOf(s_canonicalLayers, null) >= 0)
            {
                s_canonicalLayers = EnsureCanonicalLayers();
            }

            var index = (int)layer;
            if (index < 0 || index >= s_canonicalLayers.Length)
            {
                Debug.LogError(
                    $"[Overworld] Canonical layer '{layer}' out of range " +
                    $"({s_canonicalLayers.Length} layers).");
                return null;
            }

            return s_canonicalLayers[index];
        }

        /// <summary>
        /// Creates the layer asset if it does not exist yet; otherwise returns it untouched.
        /// </summary>
        private static TerrainLayer LoadOrCreateLayer(string name, string albedoPath, string normalPath)
        {
            var path = $"{LayersFolder}/{name}.terrainlayer";
            var existing = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (existing != null)
            {
                return existing;
            }

            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            if (albedo == null)
            {
                Debug.LogError($"[Overworld] TerrainLayer '{name}': missing albedo at '{albedoPath}'");
            }

            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normal == null)
            {
                Debug.LogWarning($"[Overworld] TerrainLayer '{name}': missing normal at '{normalPath}'");
            }

            var layer = new TerrainLayer
            {
                name = name,
                diffuseTexture = albedo,
                normalMapTexture = normal,
                tileSize = new Vector2(InitialTileMeters, InitialTileMeters),
            };

            AssetDatabase.CreateAsset(layer, path);
            return layer;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(SettingsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            if (!AssetDatabase.IsValidFolder(TerrainLayersFolder))
            {
                AssetDatabase.CreateFolder(SettingsFolder, "TerrainLayers");
            }

            if (!AssetDatabase.IsValidFolder(LayersFolder))
            {
                AssetDatabase.CreateFolder(TerrainLayersFolder, "Overworld");
            }
        }
    }
}
#endif
