using System.Collections.Generic;
using GameDev.World.Terrain;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// City/outskirts Dust splat on settlement streets (strong) and pads (softer).
    /// Not a BiomeId — fabric dressing only. Soft edges so fabric never reads as hard rectangles.
    /// Spec: OVERWORLD_VISUAL_DIRECTION.md.
    /// </summary>
    public static class SettlementDustPaint
    {
        public const float StreetCoverage = 0.82f;
        public const float PadCoverage = 0.48f;
        public const float GardenCoverage = 0.18f;

        /// <summary>World-space soft edge so dust fades into biome splat (not a painted box).</summary>
        public const float EdgeFalloffMeters = 2.25f;

        private readonly struct RectZone
        {
            public readonly Vector3 Origin;
            public readonly float WidthMeters;
            public readonly float DepthMeters;
            public readonly float YawDegrees;

            public RectZone(Vector3 origin, float widthMeters, float depthMeters, float yawDegrees)
            {
                Origin = origin;
                WidthMeters = Mathf.Max(0f, widthMeters);
                DepthMeters = Mathf.Max(0f, depthMeters);
                YawDegrees = yawDegrees;
            }
        }

        public static void Paint(
            UnityEngine.Terrain terrain,
            SettlementFabricLayout fabric,
            TerrainLayer dustLayer)
        {
            if (terrain == null || terrain.terrainData == null || fabric == null || dustLayer == null)
            {
                return;
            }

            var data = terrain.terrainData;
            var dustIndex = EnsureLayerIndex(data, dustLayer);
            data = terrain.terrainData;

            var resolution = data.alphamapResolution;
            var alphamaps = data.GetAlphamaps(0, 0, resolution, resolution);
            var layerCount = alphamaps.GetLength(2);
            var terrainPos = terrain.transform.position;
            var size = data.size;
            var worldPerPixelX = size.x / resolution;
            var worldPerPixelZ = size.z / resolution;

            PaintRectZones(
                alphamaps, layerCount, dustIndex, terrainPos, worldPerPixelX, worldPerPixelZ,
                CollectStreetZones(fabric), StreetCoverage);
            PaintRectZones(
                alphamaps, layerCount, dustIndex, terrainPos, worldPerPixelX, worldPerPixelZ,
                CollectPadZones(fabric), PadCoverage);
            PaintRectZones(
                alphamaps, layerCount, dustIndex, terrainPos, worldPerPixelX, worldPerPixelZ,
                CollectGardenZones(fabric), GardenCoverage);

            data.SetAlphamaps(0, 0, alphamaps);
        }

        private static List<RectZone> CollectStreetZones(SettlementFabricLayout fabric)
        {
            var list = new List<RectZone>(32);
            var streets = fabric.Streets;
            for (var i = 0; i < streets.Count; i++)
            {
                var s = streets[i];
                list.Add(new RectZone(s.Origin, s.LengthMeters, s.WidthMeters, s.YawDegrees));
            }

            var lanes = fabric.Lanes;
            for (var i = 0; i < lanes.Count; i++)
            {
                var lane = lanes[i];
                list.Add(new RectZone(lane.Origin, lane.LengthMeters, lane.WidthMeters, lane.YawDegrees));
            }

            return list;
        }

        private static List<RectZone> CollectPadZones(SettlementFabricLayout fabric)
        {
            var list = new List<RectZone>(64);
            var props = fabric.Properties;
            for (var i = 0; i < props.Count; i++)
            {
                var prop = props[i];
                if (prop?.Zones == null)
                {
                    continue;
                }

                for (var z = 0; z < prop.Zones.Count; z++)
                {
                    var zone = prop.Zones[z];
                    if (zone.Kind == PropertyZoneKind.BuildingPad
                        || zone.Kind == PropertyZoneKind.SecondaryPad
                        || zone.Kind == PropertyZoneKind.Drive
                        || zone.Kind == PropertyZoneKind.Service)
                    {
                        list.Add(new RectZone(zone.Origin, zone.WidthMeters, zone.DepthMeters, zone.YawDegrees));
                    }
                }
            }

            return list;
        }

        private static List<RectZone> CollectGardenZones(SettlementFabricLayout fabric)
        {
            var list = new List<RectZone>(32);
            var props = fabric.Properties;
            for (var i = 0; i < props.Count; i++)
            {
                var prop = props[i];
                if (prop?.Zones == null)
                {
                    continue;
                }

                for (var z = 0; z < prop.Zones.Count; z++)
                {
                    var zone = prop.Zones[z];
                    if (zone.Kind == PropertyZoneKind.FrontGarden
                        || zone.Kind == PropertyZoneKind.RearYard
                        || zone.Kind == PropertyZoneKind.OpenSpace)
                    {
                        list.Add(new RectZone(zone.Origin, zone.WidthMeters, zone.DepthMeters, zone.YawDegrees));
                    }
                }
            }

            return list;
        }

        private static void PaintRectZones(
            float[,,] alphamaps,
            int layerCount,
            int dustIndex,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            List<RectZone> zones,
            float coverage)
        {
            var resolution = alphamaps.GetLength(0);
            var pad = EdgeFalloffMeters + Mathf.Max(worldPerPixelX, worldPerPixelZ);

            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone.WidthMeters <= 0.1f || zone.DepthMeters <= 0.1f)
                {
                    continue;
                }

                var yaw = Quaternion.Euler(0f, zone.YawDegrees, 0f);
                var corners = new Vector3[4];
                corners[0] = zone.Origin;
                corners[1] = zone.Origin + yaw * new Vector3(zone.WidthMeters, 0f, 0f);
                corners[2] = zone.Origin + yaw * new Vector3(0f, 0f, zone.DepthMeters);
                corners[3] = zone.Origin + yaw * new Vector3(zone.WidthMeters, 0f, zone.DepthMeters);

                var minX = corners[0].x;
                var maxX = corners[0].x;
                var minZ = corners[0].z;
                var maxZ = corners[0].z;
                for (var c = 1; c < 4; c++)
                {
                    minX = Mathf.Min(minX, corners[c].x);
                    maxX = Mathf.Max(maxX, corners[c].x);
                    minZ = Mathf.Min(minZ, corners[c].z);
                    maxZ = Mathf.Max(maxZ, corners[c].z);
                }

                var px0 = Mathf.Clamp(
                    Mathf.FloorToInt((minX - pad - terrainPos.x) / worldPerPixelX), 0, resolution - 1);
                var px1 = Mathf.Clamp(
                    Mathf.CeilToInt((maxX + pad - terrainPos.x) / worldPerPixelX), 0, resolution - 1);
                var pz0 = Mathf.Clamp(
                    Mathf.FloorToInt((minZ - pad - terrainPos.z) / worldPerPixelZ), 0, resolution - 1);
                var pz1 = Mathf.Clamp(
                    Mathf.CeilToInt((maxZ + pad - terrainPos.z) / worldPerPixelZ), 0, resolution - 1);

                var invYaw = Quaternion.Inverse(yaw);
                for (var pz = pz0; pz <= pz1; pz++)
                {
                    for (var px = px0; px <= px1; px++)
                    {
                        var worldX = terrainPos.x + (px + 0.5f) * worldPerPixelX;
                        var worldZ = terrainPos.z + (pz + 0.5f) * worldPerPixelZ;
                        var local = invYaw * (new Vector3(worldX, 0f, worldZ) - zone.Origin);
                        var cover = SoftRectCoverage(
                            local.x, local.z, zone.WidthMeters, zone.DepthMeters, coverage);
                        if (cover <= 0.01f)
                        {
                            continue;
                        }

                        WriteDustPixel(alphamaps, pz, px, layerCount, dustIndex, cover);
                    }
                }
            }
        }

        /// <summary>
        /// Inside core → full coverage; outside within EdgeFalloffMeters → smoothstep fade.
        /// </summary>
        public static float SoftRectCoverage(
            float localX,
            float localZ,
            float widthMeters,
            float depthMeters,
            float maxCoverage)
        {
            var dx = OutsideDistance(localX, widthMeters);
            var dz = OutsideDistance(localZ, depthMeters);
            var outside = Mathf.Max(dx, dz);
            if (outside > EdgeFalloffMeters)
            {
                return 0f;
            }

            var edge = 1f;
            if (outside > 0f)
            {
                var t = 1f - outside / EdgeFalloffMeters;
                t = t * t * (3f - 2f * t);
                edge = t;
            }

            // Mild noise breaks rectangular silhouette without extra splat layers.
            var n = Mathf.PerlinNoise(localX * 0.35f + 11.3f, localZ * 0.35f + 7.1f);
            edge *= Mathf.Lerp(0.85f, 1.05f, n);
            return Mathf.Clamp01(maxCoverage * edge);
        }

        private static float OutsideDistance(float v, float size)
        {
            if (v < 0f)
            {
                return -v;
            }

            if (v > size)
            {
                return v - size;
            }

            return 0f;
        }

        private static void WriteDustPixel(
            float[,,] alphamaps,
            int z,
            int x,
            int layerCount,
            int dustIndex,
            float coverage)
        {
            coverage = Mathf.Clamp01(coverage);
            // Keep strongest dust when pads/streets overlap.
            coverage = Mathf.Max(coverage, alphamaps[z, x, dustIndex]);
            var keep = 1f - coverage;
            var sumOther = 0f;
            for (var l = 0; l < layerCount; l++)
            {
                if (l == dustIndex)
                {
                    continue;
                }

                sumOther += alphamaps[z, x, l];
            }

            if (sumOther > 1e-5f)
            {
                var scale = keep / sumOther;
                for (var l = 0; l < layerCount; l++)
                {
                    if (l == dustIndex)
                    {
                        continue;
                    }

                    alphamaps[z, x, l] *= scale;
                }
            }

            alphamaps[z, x, dustIndex] = coverage;
        }

        private static int EnsureLayerIndex(TerrainData data, TerrainLayer layer)
        {
            var layers = data.terrainLayers;
            for (var i = 0; i < layers.Length; i++)
            {
                if (layers[i] == layer)
                {
                    return i;
                }
            }

            var expanded = new TerrainLayer[layers.Length + 1];
            for (var i = 0; i < layers.Length; i++)
            {
                expanded[i] = layers[i];
            }

            expanded[layers.Length] = layer;
            data.terrainLayers = expanded;
            return layers.Length;
        }
    }
}
