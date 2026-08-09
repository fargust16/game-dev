using System;

namespace GameDev.World.Buildings.Shell
{
    [Serializable]
    public struct BuildingDna
    {
        public string ArchetypeId;
        public string DistrictId;
        public int Wealth;
        public string StyleId;
        public int FloorCount;
        public int SecondaryFloors;
        public MassingType Massing;
        public LandmarkClass LandmarkClass;
        public InteriorPolicy InteriorPolicy;
        public RoofType RoofType;
        public int ChimneyCount;
        public int TrimLevel;
        public bool FrontGardenDress;
        public int Seed;
        public int PlotIndex;

        public static BuildingDna CreateDefault(string archetypeId, int seed, int plotIndex)
        {
            return new BuildingDna
            {
                ArchetypeId = archetypeId,
                DistrictId = DistrictIds.Worker,
                Wealth = 2,
                StyleId = "VictIndustrial",
                FloorCount = 2,
                SecondaryFloors = 1,
                Massing = MassingType.Box,
                LandmarkClass = LandmarkClass.Background,
                InteriorPolicy = InteriorPolicy.None,
                RoofType = RoofType.Gable,
                ChimneyCount = 0,
                TrimLevel = 0,
                FrontGardenDress = false,
                Seed = seed,
                PlotIndex = plotIndex
            };
        }
    }
}
