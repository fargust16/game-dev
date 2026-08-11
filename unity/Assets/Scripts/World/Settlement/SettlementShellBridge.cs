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
                // Footprint is the building pad (PropertySpec.BuildingPad when from block layout).
                BindOne(result, plotSpec.Footprint, plotSpec.Street, plotSpec.DistrictId, plotSpec.PlotIndex, worldSeed, ws);
            }

            return result;
        }

        /// <summary>
        /// Binds shells from property building pads. Same result as <see cref="BindAll"/> when
        /// <paramref name="layout"/> projects pads into <see cref="ISettlementLayout.Plots"/>.
        /// </summary>
        public List<ShellInstance> BindProperties(ISettlementPropertyLayout layout, int worldSeed)
        {
            var result = new List<ShellInstance>(layout.Properties.Count);
            var ws = new WorldSeed(worldSeed);
            for (var i = 0; i < layout.Properties.Count; i++)
            {
                var prop = layout.Properties[i];
                if (prop.IsOpenSpace)
                    continue;
                // Property boundaries own street fences — suppress Shell FrontGardenDress fence duplicate.
                var suppressShellFence = HasFrontBoundary(prop);
                BindOne(
                    result,
                    prop.BuildingPad,
                    prop.Street,
                    prop.DistrictId,
                    prop.PlotIndex,
                    worldSeed,
                    ws,
                    suppressShellFence);
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

        public GameObject SpawnProperties(ISettlementPropertyLayout layout, int worldSeed, Transform parent)
        {
            var shells = BindProperties(layout, worldSeed);
            var root = new GameObject($"SettlementProperties_{layout.SettlementId}");
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

        private void BindOne(
            List<ShellInstance> result,
            PlotFootprint buildingPad,
            StreetContext street,
            string districtId,
            int plotIndex,
            int worldSeed,
            WorldSeed ws,
            bool suppressShellFrontFence = false)
        {
            var district = FindDistrict(districtId);
            var archRng = ws.Fork($"settlement_arch_{plotIndex}");
            var archetype = _selector.Select(buildingPad, district, archRng);
            if (archetype == null)
                return;
            var dnaRng = ws.Fork($"settlement_dna_{plotIndex}");
            var dna = ShellDnaSampler.Sample(
                archetype,
                district,
                buildingPad,
                street,
                worldSeed,
                plotIndex,
                dnaRng);
            if (suppressShellFrontFence)
                dna.FrontGardenDress = false;
            result.Add(_binder.Bind(dna, buildingPad, street, archetype, district));
        }

        private static bool HasFrontBoundary(PropertySpec prop)
        {
            if (prop?.Boundaries == null)
                return false;
            for (var i = 0; i < prop.Boundaries.Count; i++)
            {
                if (prop.Boundaries[i].Edge == PlotFace.Front)
                    return true;
            }

            return false;
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
