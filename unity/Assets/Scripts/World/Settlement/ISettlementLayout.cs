using System.Collections.Generic;
using GameDev.World.Buildings.Shell;

namespace GameDev.World.Settlement
{
    public interface ISettlementLayout
    {
        string SettlementId { get; }
        IReadOnlyList<SettlementPlotSpec> Plots { get; }
    }

    public struct SettlementPlotSpec
    {
        public int PlotIndex;
        public PlotFootprint Footprint;
        public StreetContext Street;
        public string DistrictId;
    }
}
