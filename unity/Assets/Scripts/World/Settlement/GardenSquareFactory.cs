using System.Collections.Generic;
using GameDev.World.Buildings.Shell;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Settlement
{
    /// <summary>Public green open space without a building shell (S7).</summary>
    public static class GardenSquareFactory
    {
        public const string ArchetypeId = "GardenSquare";
        public const float DefaultWidthMeters = 20f;

        public static PropertySpec Create(
            int plotIndex,
            Vector3 origin,
            float widthMeters,
            float depthMeters,
            float yawDegrees = 0f)
        {
            widthMeters = Mathf.Max(ModularGrid.BayMeters * 2f, widthMeters);
            depthMeters = Mathf.Max(ModularGrid.BayMeters * 2f, depthMeters);
            var widthBays = Mathf.Max(2, Mathf.RoundToInt(widthMeters / ModularGrid.BayMeters));
            var depthBays = Mathf.Max(2, Mathf.RoundToInt(depthMeters / ModularGrid.BayMeters));

            var parcel = new PlotParcel
            {
                WidthBays = widthBays,
                DepthBays = depthBays,
                Origin = origin,
                YawDegrees = yawDegrees
            };

            var zones = new List<PropertyZone>
            {
                PropertyZone.Create(
                    PropertyZoneKind.OpenSpace,
                    parcel.Origin,
                    parcel.WidthMeters,
                    parcel.DepthMeters,
                    yawDegrees)
            };

            var property = new PropertySpec
            {
                PlotIndex = plotIndex,
                DistrictId = "civic",
                ArchetypeId = ArchetypeId,
                Parcel = parcel,
                Zones = zones,
                Boundaries = new List<PropertyBoundarySegment>(),
                BuildingPad = default,
                Street = StreetContext.StreetFrontYardBack(),
                HasSecondaryBuilding = false,
                SecondaryBuildingPad = default,
                IsOpenSpace = true
            };

            property.Props = PropertyPropPlacer.Place(
                property,
                ArchetypeId,
                new Random(unchecked(110201 * 397 ^ plotIndex ^ origin.x.GetHashCode())));
            return property;
        }
    }
}
