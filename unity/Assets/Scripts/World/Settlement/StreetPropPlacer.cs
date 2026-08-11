using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Settlement
{
    /// <summary>Places street props along SettlementStreet centerlines (S8).</summary>
    public static class StreetPropPlacer
    {
        public static List<StreetPropSpec> Place(
            IReadOnlyList<SettlementStreetSpec> streets,
            Random rng)
        {
            var result = new List<StreetPropSpec>(32);
            if (streets == null || streets.Count == 0 || rng == null)
                return result;

            var rules = StreetPropCatalog.AllRules();
            for (var s = 0; s < streets.Count; s++)
            {
                var street = streets[s];
                if (street.LengthMeters < 4f || street.WidthMeters < 2f)
                    continue;

                for (var r = 0; r < rules.Count; r++)
                {
                    var rule = rules[r];
                    if (rule.StreetKind != street.Kind)
                        continue;
                    PlaceAlongStreet(street, rule, rng, result);
                }
            }

            return result;
        }

        private static void PlaceAlongStreet(
            SettlementStreetSpec street,
            StreetPropRule rule,
            Random rng,
            List<StreetPropSpec> result)
        {
            var start = street.CenterlineStart;
            var end = street.CenterlineEnd;
            var along = end - start;
            var length = along.magnitude;
            if (length < rule.SpacingMeters * 0.5f)
                return;

            var dir = along / length;
            var yaw = Quaternion.Euler(0f, street.YawDegrees, 0f);
            // Local +Z is across the ribbon; curb offset uses that axis.
            var sideAxis = yaw * Vector3.forward;
            var side = 1f;
            var t = rule.SpacingMeters * 0.5f;
            var index = 0;
            while (t < length - rule.SpacingMeters * 0.25f)
            {
                var basePos = start + dir * t;
                var offset = rule.CurbOffsetMeters;
                if (rule.AlternateSides)
                {
                    offset *= side;
                    side = -side;
                }
                else if (offset > 0.01f)
                {
                    // Center-line dress: small jitter so markers don't stack perfectly.
                    offset = (float)(rng.NextDouble() * 0.2f - 0.1f);
                }

                var pos = basePos + sideAxis * offset;
                pos.y = basePos.y + rule.Scale.y * 0.5f;

                var yawDeg = street.YawDegrees;
                if (rule.Kind == StreetPropKind.CurbBench)
                    yawDeg += side > 0f ? 0f : 180f;

                result.Add(new StreetPropSpec
                {
                    Kind = rule.Kind,
                    Semantic = rule.Semantic,
                    StreetKind = street.Kind,
                    Position = pos,
                    Scale = rule.Scale,
                    YawDegrees = yawDeg,
                    Tint = rule.Tint
                });

                t += rule.SpacingMeters;
                index++;
                if (index > 64)
                    break;
            }
        }
    }
}
