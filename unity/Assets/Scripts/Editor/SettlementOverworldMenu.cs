#if UNITY_EDITOR
using System.Collections.Generic;
using Den.Tools;
using GameDev.World.Buildings.Shell;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.Settlement;
using GameDev.World.Streaming;
using GameDev.World.Terrain;
using GameDev.World.Water;
using MapMagic.Core;
using MapMagic.Terrains;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// S6: place Settlement Street Spine fabric at the TownApproach POI on Overworld terrain.
    /// </summary>
    public static class SettlementOverworldMenu
    {
        private const string RootObjectName = "OverworldSettlement";

        [MenuItem("GameDev/Overworld/Place Settlement At TownApproach")]
        public static void PlaceSettlementAtTownApproach()
        {
            var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
            if (mapMagic == null || mapMagic.graph == null)
            {
                return;
            }

            var tileCoord = new Coord(0, 0);
            var tile = mapMagic.tiles[tileCoord];
            var terrain = tile?.main?.terrain;
            var holder = terrain != null ? terrain.GetComponent<DirectMatricesHolder>() : null;
            if (holder == null)
            {
                return;
            }

            var sampler = new MapMagicBiomeSampler(holder);
            var worldSeed = new WorldSeed(OverworldTerrainMenu.LastSeed);
            var chunks = OverworldTileChunks.CoveringTile(tileCoord, mapMagic.tileSize, OverworldTileChunks.DefaultMarginMeters);
            var placer = new StubPoiPlacer(worldSeed, sampler, chunks, PoiDemoScenario.BuildRequests());

            if (!SettlementApproachBinder.TryFindTownApproach(placer.AllMarkers(), out var approach))
            {
                return;
            }

            var lakeZones = Object.FindFirstObjectByType<OverworldWaterZones>();
            var riverZones = Object.FindFirstObjectByType<OverworldRiverZones>();
            IWaterSampler water = new CompositeWaterSampler(lakeZones, riverZones);
            // Must clear before height probes — otherwise Raycast hits prior shells/cylinder and
            // SnapFabric lifts the whole town into the air.
            ClearSettlement();
            var height = new RaycastWorldHeightSampler();

            if (!SettlementApproachBinder.TryCreateDryFabricNearApproach(
                    approach,
                    OverworldTerrainMenu.LastSeed,
                    height,
                    water,
                    out var fabric,
                    out var placedAt,
                    DistrictKind.Worker,
                    blockCount: 2,
                    plotsPerSide: 3))
            {
                return;
            }

            var root = new GameObject(RootObjectName);
            root.transform.position = Vector3.zero;

            // RoadNetworkMenu reads this to route to where the fabric actually landed (the dry-
            // ground search above can move it well away from the raw POI marker position) instead
            // of the original TownApproach coordinate.
            var anchor = root.AddComponent<OverworldSettlementAnchor>();
            anchor.Set(approach.Id.Value, placedAt);

            var archetypes = ShellCatalogFactory.CreateDefaultArchetypes();
            var districts = ShellCatalogFactory.CreateDefaultDistricts();
            var binder = new BuildingShellBinder();
            var bridge = new SettlementShellBridge(binder, archetypes, districts);
            var shells = bridge.SpawnProperties(fabric, fabric.Pose.SettlementSeed, root.transform);
            shells.name = "SettlementShells";

            var features = PropertyFeatureSpawner.SpawnAll(fabric, root.transform);
            features.name = "SettlementFeatures";

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = $"TownApproach_{approach.Id}";
            marker.transform.SetParent(root.transform, false);
            marker.transform.position = fabric.Pose.Position + Vector3.up * 6f;
            marker.transform.localScale = new Vector3(4f, 6f, 4f);
            var mr = marker.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.9f, 0.85f, 0.1f);
            mr.sharedMaterial = mat;

            Selection.activeGameObject = root;
        }

        [MenuItem("GameDev/Overworld/Clear Overworld Settlement")]
        public static void ClearSettlement()
        {
            var root = GameObject.Find(RootObjectName);
            if (root != null)
                Object.DestroyImmediate(root);
        }

        /// <summary>Editor height probe — prefer TerrainCollider, else nearest ground hit.</summary>
        private sealed class RaycastWorldHeightSampler : IWorldHeightSampler
        {
            private const float RayStartY = 4000f;

            public bool TrySampleWorldY(float worldX, float worldZ, out float worldY)
            {
                var origin = new Vector3(worldX, RayStartY, worldZ);
                var hits = Physics.RaycastAll(origin, Vector3.down, RayStartY * 2f);
                var bestDist = float.PositiveInfinity;
                var found = false;
                worldY = 0f;

                // Prefer real terrain first (ignores floating props / leftover markers).
                for (var i = 0; i < hits.Length; i++)
                {
                    if (!(hits[i].collider is TerrainCollider))
                        continue;
                    if (hits[i].distance >= bestDist)
                        continue;
                    bestDist = hits[i].distance;
                    worldY = hits[i].point.y;
                    found = true;
                }

                if (found)
                    return true;

                bestDist = float.PositiveInfinity;
                for (var i = 0; i < hits.Length; i++)
                {
                    var go = hits[i].collider != null ? hits[i].collider.gameObject : null;
                    if (go != null && ShouldIgnore(go))
                        continue;
                    if (hits[i].distance >= bestDist)
                        continue;
                    bestDist = hits[i].distance;
                    worldY = hits[i].point.y;
                    found = true;
                }

                return found;
            }

            private static bool ShouldIgnore(GameObject go)
            {
                var t = go.transform;
                while (t != null)
                {
                    var n = t.name;
                    if (n.StartsWith("OverworldWater")
                        || n.StartsWith("WaterZone_")
                        || n.StartsWith(RootObjectName)
                        || n.StartsWith("OverworldPoi")
                        || n.StartsWith("OverworldVegetation"))
                        return true;
                    t = t.parent;
                }

                return false;
            }
        }
    }
}
#endif
