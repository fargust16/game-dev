using System.Collections.Generic;
using GameDev.World.Buildings.Shell;
using GameDev.World.Seed;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// Contract demo: deterministic fake settlement plots for shell binder tests.
    /// </summary>
    public sealed class SettlementLayoutStub : ISettlementLayout
    {
        private readonly List<SettlementPlotSpec> _plots = new List<SettlementPlotSpec>();

        public string SettlementId { get; private set; }
        public IReadOnlyList<SettlementPlotSpec> Plots => _plots;

        public static SettlementLayoutStub CreateDemo(int seed, int plotCount = 8)
        {
            var stub = new SettlementLayoutStub
            {
                SettlementId = $"stub_{seed}"
            };
            var ws = new WorldSeed(seed);
            var district = ShellCatalogFactory.CreateDistrict(DistrictKind.Worker);
            var x = 0f;
            for (var i = 0; i < plotCount; i++)
            {
                var rng = ws.Fork($"settlement_plot_{i}");
                var w = district.RollWidthBays(rng);
                var d = district.RollDepthBays(rng);
                var corner = i == plotCount - 1;
                var origin = new Vector3(x, 0f, 0f);
                var footprint = corner
                    ? PlotFootprint.Corner(w, d, origin)
                    : PlotFootprint.Rect(w, d, origin);
                var street = corner
                    ? StreetContext.CornerFrontLeft()
                    : StreetContext.StreetFrontYardBack();
                var districtId = i < plotCount / 2 ? DistrictIds.Worker : DistrictIds.Commercial;
                stub._plots.Add(new SettlementPlotSpec
                {
                    PlotIndex = i,
                    Footprint = footprint,
                    Street = street,
                    DistrictId = districtId
                });
                x += footprint.WidthMeters + 1f;
            }

            return stub;
        }
    }
}
