using System;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Buildings.Shell
{
    public static class ShellDnaSampler
    {
        public static BuildingDna Sample(
            BuildingArchetypeDef archetype,
            DistrictProfileDef district,
            PlotFootprint plot,
            StreetContext street,
            int worldSeed,
            int plotIndex,
            Random rng)
        {
            var districtId = district != null ? district.Id : DistrictIds.Worker;
            var wealthBias = district != null ? district.WealthBias : 2;
            var minWealth = district != null ? district.MinWealth : 1;
            var maxWealth = district != null ? district.MaxWealth : 4;
            if (archetype != null)
            {
                minWealth = Mathf.Max(minWealth, archetype.MinWealth);
                maxWealth = Mathf.Min(maxWealth, archetype.MaxWealth);
                if (maxWealth < minWealth)
                    maxWealth = minWealth;
            }

            var wealth = Mathf.Clamp(wealthBias + rng.Next(-1, 2), minWealth, maxWealth);

            var minFloors = archetype != null ? archetype.MinFloors : 1;
            var maxFloors = archetype != null ? archetype.MaxFloors : 3;
            if (district != null)
            {
                minFloors = Mathf.Max(minFloors, district.MinFloors);
                maxFloors = Mathf.Min(maxFloors, district.MaxFloors);
                if (maxFloors < minFloors)
                    maxFloors = minFloors;
            }

            var floors = rng.Next(minFloors, maxFloors + 1);
            if (wealth >= 4 && floors < maxFloors && rng.NextDouble() < 0.4)
                floors++;
            if (wealth <= 1 && floors > minFloors && rng.NextDouble() < 0.5)
                floors--;

            var gableChance = district != null ? district.GableRoofChance : 0.7f;
            var preferGable = archetype == null || archetype.PreferredRoof == RoofType.Gable;
            RoofType roof;
            if (archetype != null && archetype.PreferShopfront)
                roof = rng.NextDouble() < (1f - gableChance) ? RoofType.Flat : RoofType.Gable;
            else if (preferGable)
                roof = rng.NextDouble() < gableChance ? RoofType.Gable : RoofType.Flat;
            else
                roof = rng.NextDouble() < gableChance ? RoofType.Gable : RoofType.Flat;

            var chimney = 0;
            var allowChimney = archetype == null || archetype.AllowChimney;
            if (allowChimney && district != null)
            {
                if (rng.NextDouble() < district.ChimneyChance)
                    chimney = 1;
                if (chimney > 0 && rng.NextDouble() < district.ExtraChimneyChance)
                    chimney = 2;
            }
            else if (allowChimney && wealth >= 2 && rng.NextDouble() < 0.6)
            {
                chimney = 1;
            }

            var minTrim = district != null ? district.MinTrimLevel : 0;
            var maxTrim = district != null ? district.MaxTrimLevel : 1;
            var trim = minTrim;
            if (maxTrim > minTrim)
                trim = rng.Next(minTrim, maxTrim + 1);
            // Wealth can push trim up one step inside district cap.
            if (wealth >= 4 && trim < maxTrim)
                trim++;

            var landmark = archetype != null ? archetype.LandmarkClass : LandmarkClass.Background;
            var interior = archetype != null ? archetype.InteriorPolicy : InteriorPolicy.None;
            var portalChance = district != null ? district.PortalShopChance : 0.35f;
            if (interior == InteriorPolicy.None
                && archetype != null
                && archetype.PreferShopfront
                && street.IsPublicFront(street.Front)
                && rng.NextDouble() < portalChance)
            {
                interior = InteriorPolicy.Portal;
            }

            var massing = district != null ? district.RollMassing(rng) : MassingType.Box;
            // Shops stay readable: box or simple stepped front.
            if (archetype != null && archetype.PreferShopfront
                && (massing == MassingType.LShape || massing == MassingType.RearWing))
            {
                massing = rng.NextDouble() < 0.5f ? MassingType.Box : MassingType.Stepped;
            }

            // Poor stays boxes — variety is floor count / width only.
            if (district != null && district.Kind == DistrictKind.Poor)
                massing = MassingType.Box;

            var secondary = Mathf.Max(1, floors - 1);
            if ((massing == MassingType.LShape || massing == MassingType.RearWing)
                && floors >= 3
                && rng.NextDouble() < 0.4)
            {
                secondary = floors - 2;
            }

            var frontGarden = district != null && district.FrontGardenDress && wealth >= 3;

            return new BuildingDna
            {
                ArchetypeId = archetype != null ? archetype.Id : "WorkerHouse",
                DistrictId = districtId,
                Wealth = wealth,
                StyleId = "VictIndustrial",
                FloorCount = Mathf.Clamp(floors, 1, 4),
                SecondaryFloors = secondary,
                Massing = massing,
                LandmarkClass = landmark,
                InteriorPolicy = interior,
                RoofType = roof,
                ChimneyCount = chimney,
                TrimLevel = Mathf.Clamp(trim, 0, 3),
                FrontGardenDress = frontGarden,
                Seed = worldSeed,
                PlotIndex = plotIndex
            };
        }
    }
}

