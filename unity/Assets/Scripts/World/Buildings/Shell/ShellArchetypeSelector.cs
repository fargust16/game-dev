using System;
using System.Collections.Generic;
using Random = System.Random;

namespace GameDev.World.Buildings.Shell
{
    public sealed class ShellArchetypeSelector
    {
        private readonly IReadOnlyList<BuildingArchetypeDef> _archetypes;

        public ShellArchetypeSelector(IReadOnlyList<BuildingArchetypeDef> archetypes)
        {
            _archetypes = archetypes ?? Array.Empty<BuildingArchetypeDef>();
        }

        public BuildingArchetypeDef Select(PlotFootprint plot, DistrictProfileDef district, Random rng)
        {
            var districtId = district != null ? district.Id : DistrictIds.Worker;
            var shopChance = district != null ? district.ShopChance : 0.1f;

            // District shop bias: try to place a shopfront building when chance hits.
            if (rng.NextDouble() < shopChance)
            {
                var shop = FindPreferShop(plot);
                if (shop != null)
                    return shop;
            }

            return SelectByWeight(plot, districtId, rng);
        }

        public BuildingArchetypeDef Select(PlotFootprint plot, string districtId, Random rng)
        {
            return SelectByWeight(plot, districtId, rng);
        }

        private BuildingArchetypeDef SelectByWeight(PlotFootprint plot, string districtId, Random rng)
        {
            var total = 0f;
            for (var i = 0; i < _archetypes.Count; i++)
            {
                var a = _archetypes[i];
                if (a == null || !a.FitsPlot(plot))
                    continue;
                var w = a.AffinityFor(districtId);
                if (w > 0f)
                    total += w;
            }

            if (total <= 0f)
                return FirstFittingOrNull(plot);

            var roll = (float)rng.NextDouble() * total;
            var acc = 0f;
            for (var i = 0; i < _archetypes.Count; i++)
            {
                var a = _archetypes[i];
                if (a == null || !a.FitsPlot(plot))
                    continue;
                var w = a.AffinityFor(districtId);
                if (w <= 0f)
                    continue;
                acc += w;
                if (roll <= acc)
                    return a;
            }

            return FirstFittingOrNull(plot);
        }

        private BuildingArchetypeDef FindPreferShop(PlotFootprint plot)
        {
            for (var i = 0; i < _archetypes.Count; i++)
            {
                var a = _archetypes[i];
                if (a != null && a.PreferShopfront && a.FitsPlot(plot))
                    return a;
            }

            return null;
        }

        private BuildingArchetypeDef FirstFittingOrNull(PlotFootprint plot)
        {
            for (var i = 0; i < _archetypes.Count; i++)
            {
                var a = _archetypes[i];
                if (a != null && a.FitsPlot(plot))
                    return a;
            }

            return _archetypes.Count > 0 ? _archetypes[0] : null;
        }
    }
}
