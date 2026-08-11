using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>Spacing rule for dressing a street kind (S8).</summary>
    public struct StreetPropRule
    {
        public StreetPropKind Kind;
        public PropSemantic Semantic;
        public SettlementStreetKind StreetKind;
        public float SpacingMeters;
        public float CurbOffsetMeters;
        public Vector3 Scale;
        public Color Tint;
        public bool AlternateSides;

        public static StreetPropRule Create(
            StreetPropKind kind,
            PropSemantic semantic,
            SettlementStreetKind streetKind,
            float spacingMeters,
            float curbOffsetMeters,
            Vector3 scale,
            Color tint,
            bool alternateSides = true)
        {
            return new StreetPropRule
            {
                Kind = kind,
                Semantic = semantic,
                StreetKind = streetKind,
                SpacingMeters = Mathf.Max(2f, spacingMeters),
                CurbOffsetMeters = curbOffsetMeters,
                Scale = scale,
                Tint = tint,
                AlternateSides = alternateSides
            };
        }
    }
}
