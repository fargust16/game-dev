#if UNITY_EDITOR
using Den.Tools;
using Den.Tools.Tasks;
using GameDev.World.Water;
using MapMagic.Core;
using MapMagic.Terrains;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Builds the overworld's <see cref="WaterZone"/> set from the terrain MapMagic just
    /// generated (WORLD_GENERATION_PIPELINE.md Iteration 05, replacing the earlier flat-plane-at-
    /// fixed-Y slice): samples the real "height" channel <see cref="OverworldTerrainMenu.BuildGraph"/>
    /// already exposes via DirectMatricesOutput200, finds basins with <see cref="LakeBasinFinder"/>,
    /// and drops one mesh per zone under "OverworldWater" carrying <see cref="OverworldWaterZones"/>
    /// so vegetation/POI placement can query real per-position water instead of a single global Y.
    /// </summary>
    public static class WaterZoneMenu
    {
        private const string RootObjectName = "OverworldWater";
        private const float CellSize = 6f;
        private const int MinCellsPerZone = 40;
        private const int MaxZones = 8;

        // Max plausible lake depth in metres, above a basin's own lowest point - this is what
        // actually shapes each zone (see LakeBasinFinder's type doc for why depth, not cell count,
        // is the right stopping signal). Deliberately an absolute value, NOT a fraction of
        // mapMagic.globals.height: that scale is set by the single tall mountain mass in
        // OverworldTerrainMenu.BuildGraph, which has nothing to do with how deep a lake should be -
        // scaling off it (an earlier version of this constant did, at 0.25 = 62.5m) let the flood
        // bridge across the plains' own gentle ~15m undulation (plateauLevels' outMin/outMax band
        // is intentionally that thin) and swallow almost the entire tile. This is the one number
        // worth re-tuning against what you actually see: raise it if a wide, gently-sloped basin
        // still gets cut off before reaching its real rim; lower it if a lake bleeds into ground
        // that should read as separate high ground.
        private const float MaxLakeDepthMeters = 12f;

        // Defensive only - see LakeBasinFinder.FindZones' cellCountSafetyCap doc. At CellSize=6m
        // this is roughly a 950m-diameter circle, far larger than any basin the current graph
        // produces; the depth cap above is what normally stops a flood, not this.
        private const int CellCountSafetyCap = 20000;

        [MenuItem("GameDev/Overworld/Generate Water Zones")]
        public static void GenerateWaterZonesMenuItem()
        {
            var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
            if (mapMagic == null || mapMagic.graph == null)
            {
                Debug.LogWarning("[Water] No generated terrain - run GameDev/Overworld/Generate Terrain From Seed first.");
                return;
            }

            GenerateForTile(mapMagic, new Coord(0, 0));
        }

        public static void GenerateForTile(MapMagicObject mapMagic, Coord tileCoord)
        {
            ClearWaterZones();

            var tile = mapMagic.tiles[tileCoord];
            if (tile == null)
            {
                Debug.LogWarning("[Water] No tile at the requested coord - skipping water zones.");
                return;
            }

            if (!WaitForTileReady(tile))
            {
                Debug.LogWarning("[Water] Tile did not finish generating within the timeout - water " +
                                  "zones may not match the final terrain. Re-run " +
                                  "GameDev/Overworld/Generate Water Zones once generation settles.");
            }

            var terrain = tile.main?.terrain;
            var holder = terrain != null ? terrain.GetComponent<DirectMatricesHolder>() : null;
            if (holder == null)
            {
                Debug.LogWarning("[Water] No DirectMatricesHolder on the terrain - skipping water zones.");
                return;
            }

            var worldHeight = mapMagic.globals.height;
            float HeightSampler(float x, float z) =>
                holder.ContainsPosition(x, z)
                    ? holder.ValueAtPositionInterpolated("height", x, z) * worldHeight
                    : float.PositiveInfinity;

            // Inset from the raw tile edge so every sampled cell - and its neighbours - stays
            // inside the holder's bounds; keeps HeightSampler's out-of-bounds fallback out of
            // normal operation instead of quietly biasing basins near the tile border.
            var inset = CellSize * 2f;
            var minX = (float)(tileCoord.x * mapMagic.tileSize.x) + inset;
            var minZ = (float)(tileCoord.z * mapMagic.tileSize.z) + inset;
            var sizeX = (float)mapMagic.tileSize.x - 2f * inset;
            var sizeZ = (float)mapMagic.tileSize.z - 2f * inset;

            var zones = LakeBasinFinder.FindZones(
                HeightSampler, minX, minZ, sizeX, sizeZ, CellSize,
                MaxLakeDepthMeters, CellCountSafetyCap, MinCellsPerZone, MaxZones);

            var root = new GameObject(RootObjectName);
            var holderComponent = root.AddComponent<OverworldWaterZones>();
            holderComponent.SetZones(zones);

            var material = CreateWaterMaterial();
            foreach (var zone in zones)
            {
                var zoneObject = new GameObject($"WaterZone_{zone.Id}");
                zoneObject.transform.SetParent(root.transform, worldPositionStays: true);
                var meshFilter = zoneObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = WaterMeshBuilder.Build(zone, zones);
                var renderer = zoneObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
            }

            Debug.Log($"[Water] Generated {zones.Length} water zone(s) from sampled terrain " +
                      $"(levels: {string.Join(", ", System.Array.ConvertAll(zones, z => z.WaterLevelY.ToString("F1")))}).");
        }

        /// <summary>
        /// MapMagic's Apply step (which populates DirectMatricesHolder - see
        /// DirectMatricesOutput200.ApplyData.Apply in TexturesOut.cs) runs through
        /// Den.Tools.Tasks.CoroutineManager, normally pumped once per frame by
        /// MapMagicObject.EditorUpdate() via EditorApplication.update. That event never fires
        /// while this method is still on the call stack, so without manually pumping it here,
        /// sampling right after Refresh()/Pin() can read a still-incomplete or stale (previous
        /// generation's) height matrix - the cause of water zones that didn't match the visible
        /// terrain on the first generate. Background generation itself (Den.Tools.Tasks.ThreadManager)
        /// keeps progressing on its own real threads regardless; only the main-thread Apply queue
        /// needs help here.
        /// </summary>
        private static bool WaitForTileReady(TerrainTile tile, float timeoutSeconds = 30f)
        {
            var start = System.DateTime.UtcNow;
            while (tile.main == null || !tile.Ready)
            {
                CoroutineManager.Update();
                System.Threading.Thread.Sleep(1);

                if ((System.DateTime.UtcNow - start).TotalSeconds > timeoutSeconds)
                {
                    return false;
                }
            }

            return true;
        }

        public static void ClearWaterZones()
        {
            var root = GameObject.Find(RootObjectName);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Material CreateWaterMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = "OverworldWater (generated)" };
            material.color = new Color(0.15f, 0.35f, 0.55f, 0.75f);

            // Skirt quads face outward on four different sides per cell - rather than reasoning
            // about per-quad winding for each direction, render both faces so skirts and the top
            // surface are never backface-culled from any angle (also reads correctly from a low
            // camera near the shore).
            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            }

            return material;
        }
    }
}
#endif
