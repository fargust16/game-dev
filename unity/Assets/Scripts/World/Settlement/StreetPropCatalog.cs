using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>Default street-dress tables for Primary / Cross ribbons (S8).</summary>
    public static class StreetPropCatalog
    {
        public static IReadOnlyList<StreetPropRule> AllRules()
        {
            return new[]
            {
                StreetPropRule.Create(
                    StreetPropKind.StreetLamp, PropSemantic.Civic, SettlementStreetKind.Primary,
                    spacingMeters: 10f, curbOffsetMeters: 0.7f,
                    scale: new Vector3(0.28f, 2.2f, 0.28f),
                    tint: new Color(0.78f, 0.72f, 0.45f)),
                StreetPropRule.Create(
                    StreetPropKind.PavingMarker, PropSemantic.Civic, SettlementStreetKind.Primary,
                    spacingMeters: 6f, curbOffsetMeters: 0f,
                    scale: new Vector3(0.9f, 0.06f, 0.9f),
                    tint: new Color(0.45f, 0.45f, 0.48f),
                    alternateSides: false),
                StreetPropRule.Create(
                    StreetPropKind.CurbBench, PropSemantic.Civic, SettlementStreetKind.Primary,
                    spacingMeters: 18f, curbOffsetMeters: 1.1f,
                    scale: new Vector3(1.3f, 0.45f, 0.4f),
                    tint: new Color(0.4f, 0.32f, 0.22f)),
                StreetPropRule.Create(
                    StreetPropKind.StreetLamp, PropSemantic.Civic, SettlementStreetKind.Cross,
                    spacingMeters: 12f, curbOffsetMeters: 0.55f,
                    scale: new Vector3(0.26f, 2.0f, 0.26f),
                    tint: new Color(0.7f, 0.65f, 0.4f)),
                StreetPropRule.Create(
                    StreetPropKind.Bollard, PropSemantic.Service, SettlementStreetKind.Cross,
                    spacingMeters: 8f, curbOffsetMeters: 0.45f,
                    scale: new Vector3(0.22f, 0.7f, 0.22f),
                    tint: new Color(0.35f, 0.35f, 0.38f)),
            };
        }
    }
}
