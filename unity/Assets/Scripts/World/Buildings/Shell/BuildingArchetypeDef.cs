using UnityEngine;

namespace GameDev.World.Buildings.Shell
{
    [CreateAssetMenu(fileName = "ARCH_", menuName = "GameDev/Buildings/Building Archetype")]
    public sealed class BuildingArchetypeDef : ScriptableObject
    {
        public string Id;
        public string Category = "Residential";
        public int MinWidthBays = 2;
        public int MaxWidthBays = 4;
        public int MinDepthBays = 2;
        public int MaxDepthBays = 3;
        public int MinFloors = 1;
        public int MaxFloors = 3;
        public int MinWealth = 1;
        public int MaxWealth = 4;
        public LandmarkClass LandmarkClass = LandmarkClass.Background;
        public InteriorPolicy InteriorPolicy = InteriorPolicy.None;
        public bool PreferShopfront;
        public bool AllowChimney = true;
        public RoofType PreferredRoof = RoofType.Gable;

        [Header("District affinity weights")]
        public float WeightPoor;
        public float WeightWorker = 1f;
        public float WeightAffluent = 0.4f;
        public float WeightRich = 0.1f;
        public float WeightCommercial = 0.2f;
        public float WeightElite;

        public float AffinityFor(string districtId)
        {
            if (string.IsNullOrEmpty(districtId))
                return 0.5f;
            switch (districtId)
            {
                case DistrictIds.Poor: return WeightPoor;
                case DistrictIds.Worker: return WeightWorker;
                case DistrictIds.Affluent: return WeightAffluent;
                case DistrictIds.Rich: return WeightRich;
                case DistrictIds.Commercial: return WeightCommercial;
                case DistrictIds.Elite: return WeightElite;
                default: return 0.15f;
            }
        }

        public bool FitsPlot(PlotFootprint plot)
        {
            return plot.WidthBays >= MinWidthBays
                   && plot.WidthBays <= MaxWidthBays
                   && plot.DepthBays >= MinDepthBays
                   && plot.DepthBays <= MaxDepthBays;
        }
    }
}
