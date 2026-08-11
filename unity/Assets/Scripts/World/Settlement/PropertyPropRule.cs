using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>Immutable placement rule: which prop, which zone, how many.</summary>
    public struct PropertyPropRule
    {
        public PropertyPropKind Kind;
        public PropSemantic Semantic;
        public PropertyZoneKind Zone;
        public int MinCount;
        public int MaxCount;
        public Vector3 Scale;
        public Color Tint;

        public static PropertyPropRule Create(
            PropertyPropKind kind,
            PropSemantic semantic,
            PropertyZoneKind zone,
            int minCount,
            int maxCount,
            Vector3 scale,
            Color tint)
        {
            return new PropertyPropRule
            {
                Kind = kind,
                Semantic = semantic,
                Zone = zone,
                MinCount = Mathf.Max(0, minCount),
                MaxCount = Mathf.Max(minCount, maxCount),
                Scale = scale,
                Tint = tint
            };
        }
    }
}
