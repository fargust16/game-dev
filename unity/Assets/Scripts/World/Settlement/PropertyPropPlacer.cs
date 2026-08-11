using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// Places property props inside existing zones from <see cref="PropertyPropCatalog"/> rules.
    /// </summary>
    public static class PropertyPropPlacer
    {
        private const float MarginMeters = 0.35f;

        public static List<PropertyPropSpec> Place(
            PropertySpec property,
            string archetypeId,
            Random rng)
        {
            var result = new List<PropertyPropSpec>(8);
            if (property == null || rng == null)
                return result;

            var rules = PropertyPropCatalog.RulesFor(archetypeId);
            for (var r = 0; r < rules.Count; r++)
            {
                var rule = rules[r];
                if (!TryFindZone(property, rule.Zone, out var zone))
                    continue;

                var count = rule.MinCount;
                if (rule.MaxCount > rule.MinCount)
                    count = rng.Next(rule.MinCount, rule.MaxCount + 1);
                if (count <= 0)
                    continue;

                for (var i = 0; i < count; i++)
                {
                    if (!TrySamplePoint(zone, rule.Scale, rng, out var pos))
                        break;

                    var yaw = (float)(rng.NextDouble() * 360.0);
                    // Path markers face street (yaw 0); lamps slight jitter only.
                    if (rule.Kind == PropertyPropKind.PathMarker)
                        yaw = zone.YawDegrees;
                    else if (rule.Kind == PropertyPropKind.GardenLamp || rule.Kind == PropertyPropKind.WardMarker)
                        yaw = zone.YawDegrees + (float)(rng.NextDouble() * 20.0 - 10.0);

                    result.Add(new PropertyPropSpec
                    {
                        Kind = rule.Kind,
                        Semantic = rule.Semantic,
                        Zone = rule.Zone,
                        Position = pos,
                        Scale = rule.Scale,
                        YawDegrees = yaw,
                        Tint = rule.Tint
                    });
                }
            }

            return result;
        }

        private static bool TryFindZone(PropertySpec property, PropertyZoneKind kind, out PropertyZone zone)
        {
            for (var i = 0; i < property.Zones.Count; i++)
            {
                if (property.Zones[i].Kind == kind
                    && property.Zones[i].WidthMeters > MarginMeters * 2f
                    && property.Zones[i].DepthMeters > MarginMeters * 2f)
                {
                    zone = property.Zones[i];
                    return true;
                }
            }

            zone = default;
            return false;
        }

        private static bool TrySamplePoint(PropertyZone zone, Vector3 scale, Random rng, out Vector3 world)
        {
            var usableW = zone.WidthMeters - MarginMeters * 2f - scale.x;
            var usableD = zone.DepthMeters - MarginMeters * 2f - scale.z;
            if (usableW < 0.05f || usableD < 0.05f)
            {
                // Fall back to zone center when the prop barely fits.
                var yaw = Quaternion.Euler(0f, zone.YawDegrees, 0f);
                world = zone.Origin + yaw * new Vector3(zone.WidthMeters * 0.5f, 0f, zone.DepthMeters * 0.5f);
                world.y = scale.y * 0.5f;
                return zone.WidthMeters > 0.2f && zone.DepthMeters > 0.2f;
            }

            var lx = MarginMeters + scale.x * 0.5f + (float)rng.NextDouble() * usableW;
            var lz = MarginMeters + scale.z * 0.5f + (float)rng.NextDouble() * usableD;
            var rot = Quaternion.Euler(0f, zone.YawDegrees, 0f);
            world = zone.Origin + rot * new Vector3(lx, 0f, lz);
            world.y = scale.y * 0.5f;
            return true;
        }
    }
}
