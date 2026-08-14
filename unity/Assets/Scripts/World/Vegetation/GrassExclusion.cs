using System;
using System.Collections.Generic;
using GameDev.World.Settlement;
using UnityEngine;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// Soft settlement exclusion for grass: streets / building pads forbidden,
    /// gardens and open green reduced — not a second biome system.
    /// </summary>
    public sealed class GrassExclusion
    {
        public readonly struct RectZone
        {
            public readonly Vector3 Origin;
            public readonly float WidthMeters;
            public readonly float DepthMeters;
            public readonly float YawDegrees;
            /// <summary>0 = forbidden, (0,1] = density multiplier when inside.</summary>
            public readonly float DensityMul;

            public RectZone(
                Vector3 origin,
                float widthMeters,
                float depthMeters,
                float yawDegrees,
                float densityMul)
            {
                Origin = origin;
                WidthMeters = Mathf.Max(0f, widthMeters);
                DepthMeters = Mathf.Max(0f, depthMeters);
                YawDegrees = yawDegrees;
                DensityMul = Mathf.Clamp01(densityMul);
            }
        }

        public const float GardenDensityMul = 0.45f;
        public const float OpenSpaceDensityMul = 0.55f;

        private readonly IReadOnlyList<RectZone> zones;

        public GrassExclusion(IReadOnlyList<RectZone> zones)
        {
            this.zones = zones ?? Array.Empty<RectZone>();
        }

        public static GrassExclusion None { get; } =
            new GrassExclusion(Array.Empty<RectZone>());

        public static GrassExclusion FromFabric(SettlementFabricLayout fabric)
        {
            if (fabric == null)
            {
                return None;
            }

            var list = new List<RectZone>(64);
            var streets = fabric.Streets;
            for (var i = 0; i < streets.Count; i++)
            {
                var s = streets[i];
                list.Add(new RectZone(s.Origin, s.LengthMeters, s.WidthMeters, s.YawDegrees, 0f));
            }

            var lanes = fabric.Lanes;
            for (var i = 0; i < lanes.Count; i++)
            {
                var lane = lanes[i];
                list.Add(new RectZone(lane.Origin, lane.LengthMeters, lane.WidthMeters, lane.YawDegrees, 0f));
            }

            var props = fabric.Properties;
            for (var i = 0; i < props.Count; i++)
            {
                var prop = props[i];
                if (prop == null || prop.Zones == null)
                {
                    continue;
                }

                for (var z = 0; z < prop.Zones.Count; z++)
                {
                    var zone = prop.Zones[z];
                    var mul = DensityForZoneKind(zone.Kind);
                    list.Add(new RectZone(
                        zone.Origin,
                        zone.WidthMeters,
                        zone.DepthMeters,
                        zone.YawDegrees,
                        mul));
                }
            }

            return new GrassExclusion(list);
        }

        public float Multiplier(float worldX, float worldZ)
        {
            var mul = 1f;
            for (var i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                if (!Contains(z, worldX, worldZ))
                {
                    continue;
                }

                mul = Mathf.Min(mul, z.DensityMul);
                if (mul <= 0f)
                {
                    return 0f;
                }
            }

            return mul;
        }

        public static float DensityForZoneKind(PropertyZoneKind kind)
        {
            switch (kind)
            {
                case PropertyZoneKind.FrontGarden:
                case PropertyZoneKind.RearYard:
                    return GardenDensityMul;
                case PropertyZoneKind.OpenSpace:
                    return OpenSpaceDensityMul;
                case PropertyZoneKind.BuildingPad:
                case PropertyZoneKind.SecondaryPad:
                case PropertyZoneKind.Service:
                case PropertyZoneKind.Drive:
                case PropertyZoneKind.Boundary:
                    return 0f;
                default:
                    return 0f;
            }
        }

        private static bool Contains(RectZone zone, float worldX, float worldZ)
        {
            if (zone.WidthMeters <= 0f || zone.DepthMeters <= 0f)
            {
                return false;
            }

            var yaw = Quaternion.Euler(0f, zone.YawDegrees, 0f);
            var local = Quaternion.Inverse(yaw) * (new Vector3(worldX, 0f, worldZ) - zone.Origin);
            return local.x >= 0f
                   && local.z >= 0f
                   && local.x <= zone.WidthMeters
                   && local.z <= zone.DepthMeters;
        }
    }
}
