using System.Collections.Generic;
using GameDev.World.Buildings.Shell;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>In-memory default <see cref="PropertyArchetypeDef"/> catalog for lab / tests.</summary>
    public static class PropertyArchetypeFactory
    {
        public static List<PropertyArchetypeDef> CreateDefaults()
        {
            return new List<PropertyArchetypeDef>
            {
                MakePoorRow(),
                MakeWorkerYard(),
                MakeAffluentGarden(),
                MakeRichTownhouse(),
                MakeShopFront(),
                MakeEliteEstate()
            };
        }

        public static PropertyArchetypeDef ForDistrict(DistrictKind kind)
        {
            switch (kind)
            {
                case DistrictKind.Poor: return MakePoorRow();
                case DistrictKind.Worker: return MakeWorkerYard();
                case DistrictKind.Affluent: return MakeAffluentGarden();
                case DistrictKind.Rich: return MakeRichTownhouse();
                case DistrictKind.Elite: return MakeEliteEstate();
                case DistrictKind.Commercial: return MakeShopFront();
                default: return MakeWorkerYard();
            }
        }

        private static PropertyArchetypeDef MakePoorRow()
        {
            var a = Base("PoorRow", DistrictKind.Poor);
            a.MinFrontageBays = 2;
            a.MaxFrontageBays = 3;
            a.MinDepthBays = 2;
            a.MaxDepthBays = 2;
            a.MinBuildingWidthBays = 2;
            a.MaxBuildingWidthBays = 3;
            a.MinBuildingDepthBays = 2;
            a.MaxBuildingDepthBays = 2;
            a.FrontSetbackBays = 0;
            a.MinRearYardBays = 0;
            a.HasFrontGarden = false;
            a.HasRearYard = false;
            a.HasService = false;
            a.FlushToStreet = true;
            a.BuildingFullFrontage = true;
            a.GapMeters = 0.35f;
            a.BoundaryStyle = BoundaryStyle.None;
            a.BoundaryFront = false;
            a.BoundarySides = false;
            a.BoundaryBack = false;
            return a;
        }

        private static PropertyArchetypeDef MakeWorkerYard()
        {
            var a = Base("WorkerYard", DistrictKind.Worker);
            a.MinFrontageBays = 2;
            a.MaxFrontageBays = 3;
            a.MinDepthBays = 3;
            a.MaxDepthBays = 5;
            a.MinBuildingWidthBays = 2;
            a.MaxBuildingWidthBays = 3;
            a.MinBuildingDepthBays = 2;
            a.MaxBuildingDepthBays = 3;
            a.FrontSetbackBays = 0;
            a.MinRearYardBays = 1;
            a.HasFrontGarden = false;
            a.HasRearYard = true;
            a.HasService = false;
            a.FlushToStreet = true;
            a.BuildingFullFrontage = true;
            a.GapMeters = 0.5f;
            a.BoundaryStyle = BoundaryStyle.LowFence;
            a.BoundaryFront = false;
            a.BoundarySides = true;
            a.BoundaryBack = true;
            a.HasFrontGate = false;
            return a;
        }

        private static PropertyArchetypeDef MakeAffluentGarden()
        {
            var a = Base("AffluentGarden", DistrictKind.Affluent);
            a.MinFrontageBays = 3;
            a.MaxFrontageBays = 5;
            a.MinDepthBays = 4;
            a.MaxDepthBays = 7;
            a.MinBuildingWidthBays = 2;
            a.MaxBuildingWidthBays = 4;
            a.MinBuildingDepthBays = 2;
            a.MaxBuildingDepthBays = 4;
            a.FrontSetbackBays = 1;
            a.MinRearYardBays = 1;
            a.HasFrontGarden = true;
            a.HasRearYard = true;
            a.HasService = false;
            a.FlushToStreet = false;
            a.BuildingFullFrontage = true;
            a.GapMeters = 0.8f;
            a.BoundaryStyle = BoundaryStyle.IronFence;
            a.BoundaryFront = true;
            a.BoundarySides = true;
            a.BoundaryBack = true;
            a.HasFrontGate = true;
            return a;
        }

        private static PropertyArchetypeDef MakeRichTownhouse()
        {
            var a = Base("RichTownhouse", DistrictKind.Rich);
            a.MinFrontageBays = 4;
            a.MaxFrontageBays = 6;
            a.MinDepthBays = 6;
            a.MaxDepthBays = 10;
            a.MinBuildingWidthBays = 3;
            a.MaxBuildingWidthBays = 5;
            a.MinBuildingDepthBays = 3;
            a.MaxBuildingDepthBays = 5;
            a.FrontSetbackBays = 2;
            a.MinRearYardBays = 2;
            a.ServiceDepthBays = 1;
            a.HasFrontGarden = true;
            a.HasRearYard = true;
            a.HasService = true;
            a.FlushToStreet = false;
            a.BuildingFullFrontage = true;
            a.GapMeters = 1.2f;
            a.BoundaryStyle = BoundaryStyle.IronFence;
            a.BoundaryFront = true;
            a.BoundarySides = true;
            a.BoundaryBack = true;
            a.HasFrontGate = true;
            return a;
        }

        private static PropertyArchetypeDef MakeShopFront()
        {
            var a = Base("ShopFront", DistrictKind.Commercial);
            a.MinFrontageBays = 3;
            a.MaxFrontageBays = 5;
            a.MinDepthBays = 4;
            a.MaxDepthBays = 6;
            a.MinBuildingWidthBays = 3;
            a.MaxBuildingWidthBays = 5;
            a.MinBuildingDepthBays = 3;
            a.MaxBuildingDepthBays = 4;
            a.FrontSetbackBays = 0;
            a.MinRearYardBays = 0;
            a.ServiceDepthBays = 2;
            a.HasFrontGarden = false;
            a.HasRearYard = false;
            a.HasService = true;
            a.FlushToStreet = true;
            a.BuildingFullFrontage = true;
            a.GapMeters = 0.25f;
            a.RearIsAlley = true;
            a.BoundaryStyle = BoundaryStyle.None;
            a.BoundaryFront = false;
            a.BoundarySides = false;
            a.BoundaryBack = false;
            a.HasFrontGate = false;
            return a;
        }

        private static PropertyArchetypeDef MakeEliteEstate()
        {
            var a = Base("EliteEstate", DistrictKind.Elite);
            a.MinFrontageBays = 8;
            a.MaxFrontageBays = 12;
            a.MinDepthBays = 12;
            a.MaxDepthBays = 16;
            a.MinBuildingWidthBays = 4;
            a.MaxBuildingWidthBays = 6;
            a.MinBuildingDepthBays = 4;
            a.MaxBuildingDepthBays = 6;
            a.FrontSetbackBays = 3;
            a.MinRearYardBays = 3;
            a.ServiceDepthBays = 3;
            a.HasFrontGarden = true;
            a.HasRearYard = true;
            a.HasService = true;
            a.FlushToStreet = false;
            a.BuildingFullFrontage = false;
            a.GapMeters = 2f;
            a.BoundaryStyle = BoundaryStyle.Wall;
            a.BoundaryFront = true;
            a.BoundarySides = true;
            a.BoundaryBack = true;
            a.HasFrontGate = true;
            a.HasDrive = true;
            a.DriveWidthBays = 2;
            a.HasSecondaryBuilding = true;
            a.SecondaryWidthBays = 3;
            a.SecondaryDepthBays = 2;
            return a;
        }

        private static PropertyArchetypeDef Base(string id, DistrictKind kind)
        {
            var a = ScriptableObject.CreateInstance<PropertyArchetypeDef>();
            a.name = id;
            a.Id = id;
            a.DistrictKind = kind;
            return a;
        }
    }
}
