#if UNITY_EDITOR
using System.Collections.Generic;
using System.Diagnostics;
using GameDev.World;
using GameDev.World.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace GameDev.EditorTools
{
    /// <summary>
    /// The overworld terrain bake: seed → TerrainGen relief → Unity <c>TerrainData</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One production path, in order:
    /// </para>
    /// <list type="number">
    /// <item>clear the previous terrain grid and any scene object left orphaned by deleted scripts;</item>
    /// <item><see cref="OverworldTerrainGrid"/> creates the tiles with the canonical layers and material,
    /// and their <c>TerrainData</c> becomes assets before anything is written into it;</item>
    /// <item>tree and detail prototypes are bound to every tile;</item>
    /// <item><see cref="TerrainAuthorityField"/> evaluates TerrainGen's relief onto
    /// <see cref="OverworldTerrainField"/>'s world lattice;</item>
    /// <item>a pending hydrology plan, if any, lowers the lattice (<see cref="HydrologyBake"/>);</item>
    /// <item><see cref="OverworldTerrainBuilder"/> writes heights, surface weights, trees and ground cover;</item>
    /// <item>the tile data is saved and view distances are applied.</item>
    /// </list>
    /// <para>
    /// Everything is deterministic in the seed. Nothing is read back from <c>TerrainData</c> into the
    /// relief, and there is no second or optional path.
    /// </para>
    /// </remarks>
    public static class OverworldTerrainBake
    {
        /// <summary>Generated terrain assets. Refilled by every bake; not authored.</summary>
        public const string DataFolder = "Assets/Settings/Terrain";

        public static string TileDataPath(int x, int z) => $"{DataFolder}/OverworldTerrainData_{x}_{z}.asset";

        /// <summary>The last bake's stage timings and counts, for diagnostics and tests.</summary>
        public static OverworldTerrainBuilder.Report LastReport { get; private set; }

        /// <summary>Milliseconds spent evaluating TerrainGen's relief in the last bake.</summary>
        public static long LastAuthorityMs { get; private set; }

        public static UnityEngine.Terrain[] Bake(int seed, TerrainBakeFlags flags)
        {
            var total = Stopwatch.StartNew();
            ClearTerrain();

            var spec = OverworldTerrainGrid.Spec.Default;
            var layers = OverworldTerrainLayers.EnsureCanonicalLayers();
            var material = OverworldTerrainMenu.EnsureOverworldTerrainMaterial();
            var holder = new GameObject(OverworldTerrainAccess.TerrainsRootName);
            holder.transform.position = Vector3.zero;
            var tiles = OverworldTerrainGrid.Create(holder.transform, spec, layers, material);

            // Persisted before anything is written into them. A TerrainData made an asset after its
            // alphamaps are painted comes back with fresh splat textures, and the painted weights are
            // silently replaced by the base layer.
            PersistTileData(tiles, spec, layers);

            var trees = OverworldVegetationAssets.TreePrototypes(out var speciesPrototype);
            var details = OverworldVegetationAssets.DetailPrototypes(out var grassPrototype);
            foreach (var tile in tiles)
            {
                tile.terrainData.treePrototypes = trees;
                tile.terrainData.detailPrototypes = details;
            }

            var authorityWatch = Stopwatch.StartNew();
            var includeMassifs = (flags & TerrainBakeFlags.IncludeMountains) != 0;
            var authority = new TerrainAuthorityField(seed, includeMassifs);
            var field = OverworldTerrainField.FromAuthority(authority, spec);
            LastAuthorityMs = authorityWatch.ElapsedMilliseconds;
            OverworldGenTiming.Log("terrain.authority", LastAuthorityMs, $"massifs={authority.MassifCount}");

            HydrologyBake.ApplyPendingCarve(field);

            var report = OverworldTerrainBuilder.Build(
                tiles, field, seed, TerrainSurfaceRules.Default, TerrainVegetationRules.Default,
                speciesPrototype, grassPrototype);
            LastReport = report;

            OverworldGenTiming.Log("terrain.heights", report.HeightMs);
            OverworldGenTiming.Log("terrain.derived", report.DerivedMs);
            OverworldGenTiming.Log("terrain.alphamaps", report.AlphamapMs);
            OverworldGenTiming.Log("terrain.trees", report.TreeMs, $"trees={report.Trees}");
            OverworldGenTiming.Log("terrain.details", report.DetailMs, $"instances={report.DetailInstances}");

            if (report.ClampedHeightSamples > 0)
            {
                Debug.LogWarning(
                    $"[Terrain] {report.ClampedHeightSamples} lattice sample(s) fell outside 0..{spec.HeightBudgetMeters:F0} m " +
                    "and were clamped by the tile height budget.");
            }

            foreach (var tile in tiles)
            {
                EditorUtility.SetDirty(tile.terrainData);
            }

            AssetDatabase.SaveAssets();
            ApplyRenderDistances(tiles);

            Debug.Log(
                $"[Terrain] Bake done: seed={seed} flags={flags} tiles={tiles.Length} massifs={authority.MassifCount} " +
                $"trees={report.Trees} detailInstances={report.DetailInstances} total={total.ElapsedMilliseconds}ms");
            return tiles;
        }

        /// <summary>
        /// Removes the terrain grid, every Unity Terrain, and root objects left with nothing but
        /// missing scripts.
        /// </summary>
        public static void ClearTerrain()
        {
            var root = GameObject.Find(OverworldTerrainAccess.TerrainsRootName);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            foreach (var terrain in Object.FindObjectsByType<UnityEngine.Terrain>())
            {
                if (terrain != null)
                {
                    Object.DestroyImmediate(terrain.gameObject);
                }
            }

            RemoveOrphanedRootObjects(SceneManager.GetActiveScene());
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// Deletes root objects whose entire hierarchy carries no component except transforms and
        /// scripts that no longer exist.
        /// </summary>
        /// <remarks>
        /// Such an object can do nothing and can never be repaired from the scene; it is what a deleted
        /// script leaves behind. Anything with a single live component anywhere in its hierarchy is kept.
        /// </remarks>
        /// <returns>Names of the removed objects.</returns>
        public static List<string> RemoveOrphanedRootObjects(Scene scene)
        {
            var removed = new List<string>();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return removed;
            }

            foreach (var go in scene.GetRootGameObjects())
            {
                if (go != null && IsOrphaned(go))
                {
                    removed.Add(go.name);
                    Object.DestroyImmediate(go);
                }
            }

            if (removed.Count > 0)
            {
                Debug.Log($"[Terrain] Removed {removed.Count} orphaned scene object(s) with only missing scripts: {string.Join(", ", removed)}");
            }

            return removed;
        }

        private static bool IsOrphaned(GameObject root)
        {
            var missing = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                foreach (var component in t.GetComponents<Component>())
                {
                    if (component != null && !(component is Transform))
                    {
                        return false;
                    }
                }
            }

            return missing > 0;
        }

        /// <remarks>
        /// An existing tile asset is refilled in place rather than replaced. A replacement gets a new
        /// GUID, which leaves a saved scene pointing at nothing after any later bake and rewrites the
        /// scene's references on every generation.
        /// </remarks>
        private static void PersistTileData(
            IReadOnlyList<UnityEngine.Terrain> tiles, OverworldTerrainGrid.Spec spec, TerrainLayer[] layers)
        {
            OverworldVegetationAssets.EnsureFolder(DataFolder);
            for (var z = 0; z < spec.TileCountZ; z++)
            {
                for (var x = 0; x < spec.TileCountX; x++)
                {
                    var tile = tiles[x + z * spec.TileCountX];
                    var path = TileDataPath(x, z);
                    var asset = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                    if (asset != null)
                    {
                        OverworldTerrainGrid.Configure(asset, spec, layers);
                        var fresh = tile.terrainData;
                        tile.terrainData = asset;
                        Object.DestroyImmediate(fresh);
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(tile.terrainData, path);
                    }

                    var collider = tile.GetComponent<TerrainCollider>();
                    if (collider != null)
                    {
                        collider.terrainData = tile.terrainData;
                    }
                }
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// View settings on the Terrain component — what is drawn, never what was generated.
        /// </summary>
        /// <remarks>
        /// Unity draws details within <c>detailObjectDistance</c> only (80 m by default, 250 m maximum),
        /// so grass exists in the data and is simply not rendered from an aerial view. Geometry LOD and
        /// the basemap are distance-driven and band across a large gentle surface, so pixel error is
        /// held at 1 and the basemap distance pushed past the world.
        /// </remarks>
        private static void ApplyRenderDistances(IReadOnlyList<UnityEngine.Terrain> tiles)
        {
            foreach (var terrain in tiles)
            {
                if (terrain == null)
                {
                    continue;
                }

                terrain.detailObjectDistance = 250f;
                terrain.detailObjectDensity = 1f;
                terrain.treeDistance = 5000f;
                terrain.treeBillboardDistance = 200f;
                terrain.drawTreesAndFoliage = true;
                terrain.heightmapPixelError = 1f;
                terrain.basemapDistance = 20000f;
            }
        }
    }
}
#endif
