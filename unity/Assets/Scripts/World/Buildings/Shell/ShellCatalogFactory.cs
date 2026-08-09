using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Buildings.Shell
{
    public static class ShellCatalogFactory
    {
        public static List<BuildingArchetypeDef> CreateDefaultArchetypes()
        {
            return new List<BuildingArchetypeDef>(5)
            {
                MakeArchetype(
                    "PoorHouse", "Residential",
                    minW: 2, maxW: 2, minD: 2, maxD: 2,
                    minF: 1, maxF: 2, minWealth: 1, maxWealth: 2,
                    shop: false, chimney: true, roof: RoofType.Gable, interior: InteriorPolicy.None,
                    poor: 1.4f, worker: 0.35f, affluent: 0.05f, rich: 0f, commercial: 0.05f, elite: 0f),
                MakeArchetype(
                    "WorkerHouse", "Residential",
                    minW: 2, maxW: 3, minD: 2, maxD: 3,
                    minF: 1, maxF: 3, minWealth: 1, maxWealth: 3,
                    shop: false, chimney: true, roof: RoofType.Gable, interior: InteriorPolicy.None,
                    poor: 0.4f, worker: 1.3f, affluent: 0.25f, rich: 0.05f, commercial: 0.1f, elite: 0f),
                MakeArchetype(
                    "Townhouse", "Residential",
                    minW: 2, maxW: 4, minD: 2, maxD: 4,
                    minF: 2, maxF: 4, minWealth: 2, maxWealth: 4,
                    shop: false, chimney: true, roof: RoofType.Gable, interior: InteriorPolicy.None,
                    poor: 0.05f, worker: 0.35f, affluent: 1.3f, rich: 0.55f, commercial: 0.2f, elite: 0.25f),
                MakeArchetype(
                    "NobleHouse", "Residential",
                    minW: 3, maxW: 6, minD: 3, maxD: 5,
                    minF: 2, maxF: 4, minWealth: 4, maxWealth: 5,
                    shop: false, chimney: true, roof: RoofType.Gable, interior: InteriorPolicy.None,
                    poor: 0f, worker: 0.05f, affluent: 0.45f, rich: 1.4f, commercial: 0.1f, elite: 1.2f),
                MakeArchetype(
                    "Shop", "Commercial",
                    minW: 2, maxW: 4, minD: 2, maxD: 3,
                    minF: 1, maxF: 3, minWealth: 2, maxWealth: 4,
                    shop: true, chimney: false, roof: RoofType.Flat, interior: InteriorPolicy.Portal,
                    poor: 0.15f, worker: 0.35f, affluent: 0.4f, rich: 0.2f, commercial: 1.5f, elite: 0.05f)
            };
        }

        public static List<DistrictProfileDef> CreateDefaultDistricts()
        {
            return new List<DistrictProfileDef>
            {
                MakePoor(),
                MakeWorker(),
                MakeAffluent(),
                MakeRich(),
                MakeElite(),
                MakeCommercial()
            };
        }

        public static DistrictProfileDef CreateDistrict(DistrictKind kind)
        {
            switch (kind)
            {
                case DistrictKind.Poor: return MakePoor();
                case DistrictKind.Worker: return MakeWorker();
                case DistrictKind.Affluent: return MakeAffluent();
                case DistrictKind.Rich: return MakeRich();
                case DistrictKind.Elite: return MakeElite();
                case DistrictKind.Commercial: return MakeCommercial();
                default: return MakeWorker();
            }
        }

        private static DistrictProfileDef MakePoor()
        {
            var d = Base(DistrictIds.Poor, DistrictKind.Poor);
            d.WealthBias = 1;
            d.MinWealth = 1;
            d.MaxWealth = 2;
            d.MinFloors = 1;
            d.MaxFloors = 2;
            d.MinWidthBays = 2;
            d.MaxWidthBays = 2;
            d.MinDepthBays = 2;
            d.MaxDepthBays = 2;
            d.GapMeters = 0.35f;
            d.Density = 0.95f;
            d.ShopChance = 0.08f;
            d.ChimneyChance = 0.45f;
            d.GableRoofChance = 0.9f;
            d.WindowDensity = 0.45f;
            d.MaxTrimLevel = 0;
            d.CornerEveryN = 10;
            d.MassingBox = 1f;
            d.MassingLShape = 0f;
            d.MassingStepped = 0f;
            d.MassingRearWing = 0f;
            d.WallTint = new Color(0.42f, 0.4f, 0.36f);
            d.RoofTint = new Color(0.22f, 0.2f, 0.18f);
            d.WindowTint = new Color(0.35f, 0.42f, 0.48f);
            d.DoorTint = new Color(0.28f, 0.18f, 0.12f);
            return d;
        }

        private static DistrictProfileDef MakeWorker()
        {
            var d = Base(DistrictIds.Worker, DistrictKind.Worker);
            d.WealthBias = 2;
            d.MinWealth = 1;
            d.MaxWealth = 3;
            d.MinFloors = 1;
            d.MaxFloors = 3;
            d.MinWidthBays = 2;
            d.MaxWidthBays = 3;
            d.MinDepthBays = 2;
            d.MaxDepthBays = 3;
            d.GapMeters = 0.5f;
            d.Density = 0.9f;
            d.ShopChance = 0.12f;
            d.ChimneyChance = 0.7f;
            d.ExtraChimneyChance = 0.1f;
            d.GableRoofChance = 0.8f;
            d.WindowDensity = 0.65f;
            d.MaxTrimLevel = 1;
            d.CornerEveryN = 8;
            d.MassingBox = 0.7f;
            d.MassingLShape = 0.15f;
            d.MassingStepped = 0.15f;
            d.MassingRearWing = 0f;
            d.WallTint = new Color(0.55f, 0.48f, 0.42f);
            d.RoofTint = new Color(0.3f, 0.22f, 0.2f);
            d.TrimTint = new Color(0.58f, 0.52f, 0.45f);
            d.WindowTint = new Color(0.45f, 0.58f, 0.7f);
            d.DoorTint = new Color(0.35f, 0.22f, 0.14f);
            return d;
        }

        private static DistrictProfileDef MakeAffluent()
        {
            var d = Base(DistrictIds.Affluent, DistrictKind.Affluent);
            d.WealthBias = 3;
            d.MinWealth = 2;
            d.MaxWealth = 4;
            d.MinFloors = 2;
            d.MaxFloors = 4;
            d.MinWidthBays = 2;
            d.MaxWidthBays = 4;
            d.MinDepthBays = 2;
            d.MaxDepthBays = 4;
            d.GapMeters = 1.0f;
            d.Density = 0.7f;
            d.ShopChance = 0.1f;
            d.ChimneyChance = 0.8f;
            d.ExtraChimneyChance = 0.25f;
            d.GableRoofChance = 0.8f;
            d.WindowDensity = 0.8f;
            d.MinTrimLevel = 1;
            d.MaxTrimLevel = 2;
            d.CornerEveryN = 6;
            d.FrontGardenDress = true;
            d.MassingBox = 0.2f;
            d.MassingLShape = 0.35f;
            d.MassingStepped = 0.25f;
            d.MassingRearWing = 0.2f;
            d.WallTint = new Color(0.72f, 0.68f, 0.6f);
            d.RoofTint = new Color(0.25f, 0.28f, 0.32f);
            d.TrimTint = new Color(0.85f, 0.8f, 0.68f);
            d.WindowTint = new Color(0.55f, 0.72f, 0.88f);
            d.DoorTint = new Color(0.25f, 0.2f, 0.35f);
            return d;
        }

        private static DistrictProfileDef MakeRich()
        {
            var d = Base(DistrictIds.Rich, DistrictKind.Rich);
            d.WealthBias = 5;
            d.MinWealth = 4;
            d.MaxWealth = 5;
            d.MinFloors = 2;
            d.MaxFloors = 4;
            d.MinWidthBays = 3;
            d.MaxWidthBays = 5;
            d.MinDepthBays = 3;
            d.MaxDepthBays = 5;
            d.GapMeters = 1.8f;
            d.Density = 0.45f;
            d.ShopChance = 0.02f;
            d.ChimneyChance = 0.85f;
            d.ExtraChimneyChance = 0.45f;
            d.GableRoofChance = 0.85f;
            d.WindowDensity = 0.88f;
            d.MinTrimLevel = 2;
            d.MaxTrimLevel = 3;
            d.CornerEveryN = 5;
            d.FrontGardenDress = true;
            d.MassingBox = 0.1f;
            d.MassingLShape = 0.35f;
            d.MassingStepped = 0.25f;
            d.MassingRearWing = 0.3f;
            d.WallTint = new Color(0.72f, 0.28f, 0.24f); // warm red brick
            d.RoofTint = new Color(0.2f, 0.24f, 0.3f);
            d.TrimTint = new Color(0.92f, 0.9f, 0.86f); // white stone
            d.WindowTint = new Color(0.65f, 0.8f, 0.92f);
            d.DoorTint = new Color(0.12f, 0.1f, 0.12f);
            return d;
        }

        private static DistrictProfileDef MakeElite()
        {
            var d = Base(DistrictIds.Elite, DistrictKind.Elite);
            d.WealthBias = 5;
            d.MinWealth = 5;
            d.MaxWealth = 5;
            d.MinFloors = 3;
            d.MaxFloors = 4;
            d.MinWidthBays = 4;
            d.MaxWidthBays = 6;
            d.MinDepthBays = 3;
            d.MaxDepthBays = 5;
            d.GapMeters = 2.4f;
            d.Density = 0.35f;
            d.ShopChance = 0f;
            d.ChimneyChance = 0.95f;
            d.ExtraChimneyChance = 0.65f;
            d.GableRoofChance = 0.95f;
            d.WindowDensity = 0.92f;
            d.MinTrimLevel = 3;
            d.MaxTrimLevel = 3;
            d.CornerEveryN = 4;
            d.FrontGardenDress = true;
            // Mansions / angular villas — almost never a plain box.
            d.MassingBox = 0.05f;
            d.MassingLShape = 0.4f;
            d.MassingStepped = 0.2f;
            d.MassingRearWing = 0.35f;
            d.WallTint = new Color(0.7f, 0.22f, 0.2f);
            d.RoofTint = new Color(0.18f, 0.22f, 0.28f);
            d.TrimTint = new Color(0.95f, 0.93f, 0.9f);
            d.WindowTint = new Color(0.7f, 0.84f, 0.95f);
            d.DoorTint = new Color(0.08f, 0.08f, 0.1f);
            return d;
        }

        private static DistrictProfileDef MakeCommercial()
        {
            var d = Base(DistrictIds.Commercial, DistrictKind.Commercial);
            d.WealthBias = 3;
            d.MinWealth = 2;
            d.MaxWealth = 4;
            d.MinFloors = 1;
            d.MaxFloors = 3;
            d.MinWidthBays = 2;
            d.MaxWidthBays = 4;
            d.MinDepthBays = 2;
            d.MaxDepthBays = 3;
            d.GapMeters = 0.45f;
            d.Density = 0.88f;
            d.ShopChance = 0.75f;
            d.ChimneyChance = 0.25f;
            d.GableRoofChance = 0.35f;
            d.WindowDensity = 0.7f;
            d.PortalShopChance = 0.55f;
            d.MaxTrimLevel = 2;
            d.CornerEveryN = 5;
            d.MassingBox = 0.55f;
            d.MassingLShape = 0.15f;
            d.MassingStepped = 0.3f;
            d.MassingRearWing = 0f;
            d.WallTint = new Color(0.5f, 0.46f, 0.42f);
            d.RoofTint = new Color(0.24f, 0.24f, 0.26f);
            d.TrimTint = new Color(0.7f, 0.62f, 0.45f);
            d.WindowTint = new Color(0.5f, 0.65f, 0.78f);
            d.DoorTint = new Color(0.3f, 0.2f, 0.12f);
            d.ShopTint = new Color(0.18f, 0.38f, 0.48f);
            return d;
        }

        private static DistrictProfileDef Base(string id, DistrictKind kind)
        {
            var d = ScriptableObject.CreateInstance<DistrictProfileDef>();
            d.name = id;
            d.Id = id;
            d.Kind = kind;
            return d;
        }

        private static BuildingArchetypeDef MakeArchetype(
            string id,
            string category,
            int minW, int maxW, int minD, int maxD,
            int minF, int maxF, int minWealth, int maxWealth,
            bool shop, bool chimney, RoofType roof, InteriorPolicy interior,
            float poor, float worker, float affluent, float rich, float commercial, float elite)
        {
            var a = ScriptableObject.CreateInstance<BuildingArchetypeDef>();
            a.name = id;
            a.Id = id;
            a.Category = category;
            a.MinWidthBays = minW;
            a.MaxWidthBays = maxW;
            a.MinDepthBays = minD;
            a.MaxDepthBays = maxD;
            a.MinFloors = minF;
            a.MaxFloors = maxF;
            a.MinWealth = minWealth;
            a.MaxWealth = maxWealth;
            a.PreferShopfront = shop;
            a.AllowChimney = chimney;
            a.PreferredRoof = roof;
            a.InteriorPolicy = interior;
            a.LandmarkClass = LandmarkClass.Background;
            a.WeightPoor = poor;
            a.WeightWorker = worker;
            a.WeightAffluent = affluent;
            a.WeightRich = rich;
            a.WeightCommercial = commercial;
            a.WeightElite = elite;
            return a;
        }
    }
}
