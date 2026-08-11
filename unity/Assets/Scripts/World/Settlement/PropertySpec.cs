using System;
using System.Collections.Generic;
using GameDev.World.Buildings.Shell;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// One property: land parcel + zones + building pad for Shell.
    /// </summary>
    [Serializable]
    public sealed class PropertySpec
    {
        public int PlotIndex;
        public string DistrictId;
        public string ArchetypeId;
        public PlotParcel Parcel;
        public List<PropertyZone> Zones = new List<PropertyZone>();
        public List<PropertyBoundarySegment> Boundaries = new List<PropertyBoundarySegment>();
        public List<PropertyPropSpec> Props = new List<PropertyPropSpec>();
        public PlotFootprint BuildingPad;
        public StreetContext Street;
        public bool HasSecondaryBuilding;
        public PlotFootprint SecondaryBuildingPad;

        public SettlementPlotSpec ToPlotSpec()
        {
            return new SettlementPlotSpec
            {
                PlotIndex = PlotIndex,
                Footprint = BuildingPad,
                Street = Street,
                DistrictId = DistrictId
            };
        }
    }
}
