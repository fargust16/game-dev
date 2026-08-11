using System.Collections.Generic;
using GameDev.World.Buildings.Shell;

namespace GameDev.World.Settlement
{
    public interface ISettlementLayout
    {
        string SettlementId { get; }
        IReadOnlyList<SettlementPlotSpec> Plots { get; }
    }

    /// <summary>
    /// Settlement layout that also exposes land parcels and property zones.
    /// <see cref="ISettlementLayout.Plots"/> carries building pads for the Shell bridge.
    /// </summary>
    public interface ISettlementPropertyLayout : ISettlementLayout
    {
        IReadOnlyList<PropertySpec> Properties { get; }
    }

    public struct SettlementPlotSpec
    {
        public int PlotIndex;
        /// <summary>Building pad for Exterior Shell Grammar (not the full land lot).</summary>
        public PlotFootprint Footprint;
        public StreetContext Street;
        public string DistrictId;
    }
}
