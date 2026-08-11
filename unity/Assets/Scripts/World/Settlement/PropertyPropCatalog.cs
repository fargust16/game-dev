using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>Per-archetype prop tables for Property Props V0.</summary>
    public static class PropertyPropCatalog
    {
        public static IReadOnlyList<PropertyPropRule> RulesFor(string archetypeId)
        {
            switch (archetypeId)
            {
                case "WorkerYard": return Worker();
                case "AffluentGarden": return Affluent();
                case "RichTownhouse": return Rich();
                case "ShopFront": return Shop();
                case "EliteEstate": return Elite();
                case GardenSquareFactory.ArchetypeId: return GardenSquare();
                default: return System.Array.Empty<PropertyPropRule>();
            }
        }

        private static PropertyPropRule[] GardenSquare()
        {
            return new[]
            {
                PropertyPropRule.Create(
                    PropertyPropKind.Planter, PropSemantic.Civic, PropertyZoneKind.OpenSpace,
                    2, 4, new Vector3(0.7f, 0.5f, 0.7f), new Color(0.25f, 0.45f, 0.28f)),
                PropertyPropRule.Create(
                    PropertyPropKind.Bench, PropSemantic.Civic, PropertyZoneKind.OpenSpace,
                    1, 2, new Vector3(1.2f, 0.45f, 0.4f), new Color(0.35f, 0.28f, 0.2f)),
                PropertyPropRule.Create(
                    PropertyPropKind.GardenLamp, PropSemantic.Civic, PropertyZoneKind.OpenSpace,
                    1, 2, new Vector3(0.25f, 1.6f, 0.25f), new Color(0.75f, 0.7f, 0.45f)),
                PropertyPropRule.Create(
                    PropertyPropKind.PathMarker, PropSemantic.Civic, PropertyZoneKind.OpenSpace,
                    1, 3, new Vector3(0.5f, 0.08f, 0.5f), new Color(0.55f, 0.55f, 0.5f)),
            };
        }

        private static PropertyPropRule[] Worker()
        {
            return new[]
            {
                PropertyPropRule.Create(
                    PropertyPropKind.Crate, PropSemantic.Service, PropertyZoneKind.RearYard,
                    1, 2, new Vector3(0.55f, 0.45f, 0.55f), new Color(0.45f, 0.32f, 0.2f)),
                PropertyPropRule.Create(
                    PropertyPropKind.Firewood, PropSemantic.Domestic, PropertyZoneKind.RearYard,
                    0, 1, new Vector3(0.7f, 0.35f, 0.4f), new Color(0.4f, 0.28f, 0.18f)),
                PropertyPropRule.Create(
                    PropertyPropKind.Barrel, PropSemantic.Service, PropertyZoneKind.RearYard,
                    0, 1, new Vector3(0.45f, 0.55f, 0.45f), new Color(0.35f, 0.3f, 0.22f))
            };
        }

        private static PropertyPropRule[] Affluent()
        {
            return new[]
            {
                PropertyPropRule.Create(
                    PropertyPropKind.Planter, PropSemantic.Domestic, PropertyZoneKind.FrontGarden,
                    1, 2, new Vector3(0.5f, 0.4f, 0.5f), new Color(0.35f, 0.45f, 0.28f)),
                PropertyPropRule.Create(
                    PropertyPropKind.GardenLamp, PropSemantic.Domestic, PropertyZoneKind.FrontGarden,
                    1, 1, new Vector3(0.2f, 1.1f, 0.2f), new Color(0.55f, 0.5f, 0.35f)),
                PropertyPropRule.Create(
                    PropertyPropKind.PathMarker, PropSemantic.Domestic, PropertyZoneKind.FrontGarden,
                    1, 2, new Vector3(0.45f, 0.08f, 0.35f), new Color(0.5f, 0.48f, 0.42f)),
                PropertyPropRule.Create(
                    PropertyPropKind.Bench, PropSemantic.Domestic, PropertyZoneKind.RearYard,
                    0, 1, new Vector3(1.0f, 0.45f, 0.35f), new Color(0.4f, 0.3f, 0.2f)),
                PropertyPropRule.Create(
                    PropertyPropKind.HedgeOrb, PropSemantic.Domestic, PropertyZoneKind.RearYard,
                    0, 2, new Vector3(0.55f, 0.55f, 0.55f), new Color(0.25f, 0.4f, 0.22f))
            };
        }

        private static PropertyPropRule[] Rich()
        {
            return new[]
            {
                PropertyPropRule.Create(
                    PropertyPropKind.Planter, PropSemantic.Domestic, PropertyZoneKind.FrontGarden,
                    2, 3, new Vector3(0.55f, 0.45f, 0.55f), new Color(0.32f, 0.42f, 0.26f)),
                PropertyPropRule.Create(
                    PropertyPropKind.GardenLamp, PropSemantic.Domestic, PropertyZoneKind.FrontGarden,
                    1, 2, new Vector3(0.22f, 1.2f, 0.22f), new Color(0.5f, 0.48f, 0.4f)),
                PropertyPropRule.Create(
                    PropertyPropKind.PathMarker, PropSemantic.Domestic, PropertyZoneKind.FrontGarden,
                    2, 3, new Vector3(0.5f, 0.08f, 0.4f), new Color(0.48f, 0.46f, 0.4f)),
                PropertyPropRule.Create(
                    PropertyPropKind.Crate, PropSemantic.Service, PropertyZoneKind.Service,
                    1, 2, new Vector3(0.5f, 0.4f, 0.5f), new Color(0.42f, 0.3f, 0.18f)),
                PropertyPropRule.Create(
                    PropertyPropKind.Barrel, PropSemantic.Service, PropertyZoneKind.Service,
                    0, 1, new Vector3(0.45f, 0.55f, 0.45f), new Color(0.32f, 0.28f, 0.2f))
            };
        }

        private static PropertyPropRule[] Shop()
        {
            return new[]
            {
                PropertyPropRule.Create(
                    PropertyPropKind.Crate, PropSemantic.Industrial, PropertyZoneKind.Service,
                    2, 4, new Vector3(0.55f, 0.45f, 0.55f), new Color(0.4f, 0.3f, 0.18f)),
                PropertyPropRule.Create(
                    PropertyPropKind.Barrel, PropSemantic.Industrial, PropertyZoneKind.Service,
                    1, 2, new Vector3(0.5f, 0.6f, 0.5f), new Color(0.3f, 0.28f, 0.22f)),
                PropertyPropRule.Create(
                    PropertyPropKind.Cart, PropSemantic.Industrial, PropertyZoneKind.Service,
                    0, 1, new Vector3(1.2f, 0.7f, 0.7f), new Color(0.35f, 0.28f, 0.2f))
            };
        }

        private static PropertyPropRule[] Elite()
        {
            return new[]
            {
                PropertyPropRule.Create(
                    PropertyPropKind.GardenLamp, PropSemantic.Civic, PropertyZoneKind.Drive,
                    2, 3, new Vector3(0.25f, 1.4f, 0.25f), new Color(0.55f, 0.52f, 0.42f)),
                PropertyPropRule.Create(
                    PropertyPropKind.Planter, PropSemantic.Domestic, PropertyZoneKind.FrontGarden,
                    2, 3, new Vector3(0.6f, 0.5f, 0.6f), new Color(0.3f, 0.42f, 0.25f)),
                PropertyPropRule.Create(
                    PropertyPropKind.Statue, PropSemantic.Civic, PropertyZoneKind.FrontGarden,
                    0, 1, new Vector3(0.5f, 1.3f, 0.5f), new Color(0.65f, 0.62f, 0.58f)),
                PropertyPropRule.Create(
                    PropertyPropKind.WardMarker, PropSemantic.Occult, PropertyZoneKind.FrontGarden,
                    0, 1, new Vector3(0.25f, 0.9f, 0.25f), new Color(0.35f, 0.25f, 0.45f)),
                PropertyPropRule.Create(
                    PropertyPropKind.Cart, PropSemantic.Service, PropertyZoneKind.Service,
                    0, 1, new Vector3(1.3f, 0.75f, 0.75f), new Color(0.32f, 0.26f, 0.18f)),
                PropertyPropRule.Create(
                    PropertyPropKind.Crate, PropSemantic.Service, PropertyZoneKind.Service,
                    1, 2, new Vector3(0.55f, 0.45f, 0.55f), new Color(0.4f, 0.3f, 0.18f))
            };
        }
    }
}
