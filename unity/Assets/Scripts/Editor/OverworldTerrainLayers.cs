#if UNITY_EDITOR
using System.Collections.Generic;
using GameDev.World.Terrain;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// The canonical overworld <see cref="TerrainLayer"/> stack every tile is baked with.
    /// </summary>
    /// <remarks>
    /// This class owns the layer <em>assets</em> and nothing else. The bake assigns them to
    /// <c>terrainData.terrainLayers</c> in <see cref="TerrainSurfaceLayer"/> order and
    /// <see cref="TerrainSurfaceClassifier"/> writes weights in the same order, so a layer's index is
    /// its identity on every tile.
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

        /// <summary>Project-owned surface textures the layer assets reference.</summary>
        private const string SurfaceTextures = "Assets/Art/Terrain/SurfaceLayers";

        /// <summary>
        /// UV tiling applied when a layer asset is first created. Editable in the Inspector
        /// afterwards — this value is a starting point, not an authority.
        /// </summary>
        private const float InitialTileMeters = 12f;

        /// <summary>Number of entries in <see cref="TerrainSurfaceLayer"/>.</summary>
        public const int CanonicalLayerCount = TerrainSurfaceClassifier.LayerCount;

        private static TerrainLayer[] s_canonicalLayers;

        [MenuItem("GameDev/Overworld/Terrain/Rebuild Canonical Terrain Layers")]
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
        /// Loads (creating on first use) the canonical layer assets in <see cref="TerrainSurfaceLayer"/> order.
        /// </summary>
        /// <remarks>
        /// Existing assets are never re-stamped: only a missing asset is created and populated, so
        /// Inspector edits survive.
        /// </remarks>
        public static TerrainLayer[] EnsureCanonicalLayers()
        {
            EnsureFolder();
            var layers = new[]
            {
                LoadOrCreateLayer(
                    "Overworld_Grass", $"{SurfaceTextures}/Grass/grass_dark_diff.jpg",
                    $"{SurfaceTextures}/Grass/grass_dark_nor_gl.exr"),
                LoadOrCreateLayer(
                    "Overworld_Sand", $"{SurfaceTextures}/Sand/sand_diff.png",
                    $"{SurfaceTextures}/Sand/sand_nor_gl.png"),
                LoadOrCreateLayer(
                    "Overworld_Soil", $"{SurfaceTextures}/Soil/sediment_diff.jpg",
                    $"{SurfaceTextures}/Soil/sediment_nor_gl.exr"),
                LoadOrCreateLayer(
                    "Overworld_Cliff", $"{SurfaceTextures}/Cliff/rock_face_diff_1k.jpg",
                    $"{SurfaceTextures}/Cliff/rock_face_nor_gl_1k.exr"),
                LoadOrCreateLayer(
                    "Overworld_Rock", $"{SurfaceTextures}/Rock/rock_base_diff.jpg",
                    $"{SurfaceTextures}/Rock/rock_base_nor_gl.exr"),
            };

            AssetDatabase.SaveAssets();
            return layers;
        }

        /// <summary>Single canonical layer.</summary>
        public static TerrainLayer GetCanonicalLayer(TerrainSurfaceLayer layer)
        {
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
                    $"[Overworld] Canonical layer '{layer}' out of range ({s_canonicalLayers.Length} layers).");
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
