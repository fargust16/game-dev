using System.Collections.Generic;
using GameDev.World.Buildings.Shell;
using GameDev.World.Seed;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// Hooks <see cref="IBuildingShellBinder"/> to an <see cref="ISettlementLayout"/>.
    /// </summary>
    public sealed class SettlementShellBridge
    {
        private readonly IBuildingShellBinder _binder;
        private readonly ShellArchetypeSelector _selector;
        private readonly IReadOnlyList<DistrictProfileDef> _districts;

        public SettlementShellBridge(
            IBuildingShellBinder binder,
            IReadOnlyList<BuildingArchetypeDef> archetypes,
            IReadOnlyList<DistrictProfileDef> districts)
        {
            _binder = binder;
            _districts = districts;
            _selector = new ShellArchetypeSelector(archetypes);
        }

        public List<ShellInstance> BindAll(ISettlementLayout layout, int worldSeed)
        {
            var result = new List<ShellInstance>(layout.Plots.Count);
            var ws = new WorldSeed(worldSeed);
            for (var i = 0; i < layout.Plots.Count; i++)
            {
                var plotSpec = layout.Plots[i];
                var district = FindDistrict(plotSpec.DistrictId);
                var archRng = ws.Fork($"settlement_arch_{plotSpec.PlotIndex}");
                var archetype = _selector.Select(plotSpec.Footprint, district, archRng);
                if (archetype == null)
                    continue;
                var dnaRng = ws.Fork($"settlement_dna_{plotSpec.PlotIndex}");
                var dna = ShellDnaSampler.Sample(
                    archetype,
                    district,
                    plotSpec.Footprint,
                    plotSpec.Street,
                    worldSeed,
                    plotSpec.PlotIndex,
                    dnaRng);
                result.Add(_binder.Bind(dna, plotSpec.Footprint, plotSpec.Street, archetype, district));
            }

            return result;
        }

        public GameObject SpawnAll(ISettlementLayout layout, int worldSeed, Transform parent)
        {
            var shells = BindAll(layout, worldSeed);
            var root = new GameObject($"SettlementShells_{layout.SettlementId}");
            if (parent != null)
                root.transform.SetParent(parent, false);
            for (var i = 0; i < shells.Count; i++)
            {
                var s = shells[i];
                ShellPlaceholderFactory.SpawnShellRoot(
                    s,
                    root.transform,
                    $"Shell_{i:00}_{s.Dna.ArchetypeId}");
            }

            return root;
        }

        private DistrictProfileDef FindDistrict(string id)
        {
            for (var i = 0; i < _districts.Count; i++)
            {
                if (_districts[i] != null && _districts[i].Id == id)
                    return _districts[i];
            }

            return _districts.Count > 0 ? _districts[0] : null;
        }
    }
}
