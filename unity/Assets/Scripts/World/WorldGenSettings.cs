using UnityEngine;

namespace GameDev.World
{
    /// <summary>
    /// Tunable knobs for one overworld generate. Plain C# data — not a ScriptableObject.
    /// World Generator UI and pipeline menus share this struct.
    /// </summary>
    [System.Serializable]
    public struct WorldGenSettings
    {
        // --- Lakes ---
        public int LakeCount;
        public float LakeSize;
        public float ShapeIrregularity;
        public float TerrainConformity;
        public float WaterInsetMeters;
        public float WaterDepthOffsetMeters;
        public float MaxDepthMeters;
        public float MaxLakeTerrainModificationMeters;

        // --- Rivers ---
        public float RiverSourceWidthMeters;
        public float RiverMouthWidthMeters;
        public float RiverBankWidthMeters;

        // --- Roads ---
        public float PrimaryRoadCostWeight;
        public float SecondaryRoadCostWeight;
        public float MaxGrade;
        public float MaxCutDepthMeters;
        public float MaxFillHeightMeters;
        public float TurnRadiusMeters;
        public float ParallelRoadPenalty;

        // --- POI ---
        public int TownCount;
        public int ShrineCount;
        public int SmallCampCount;
        public int LandmarkCount;
        public float MinimumPoiDistanceMeters;

        public static WorldGenSettings Default => new WorldGenSettings
        {
            LakeCount = 3,
            LakeSize = 0.6f,
            ShapeIrregularity = 0.35f,
            TerrainConformity = 0.75f,
            WaterInsetMeters = WorldScale.WaterOverlapMeters,
            WaterDepthOffsetMeters = WorldScale.WaterDepthOffsetMeters,
            MaxDepthMeters = 22f,
            MaxLakeTerrainModificationMeters = WorldScale.LakeMaxTerrainModificationMeters,

            RiverSourceWidthMeters = WorldScale.RiverSourceWidthMeters,
            RiverMouthWidthMeters = WorldScale.RiverMouthWidthMeters,
            RiverBankWidthMeters = WorldScale.RiverBankWidthMeters,

            PrimaryRoadCostWeight = 1f,
            SecondaryRoadCostWeight = 1f,
            MaxGrade = WorldScale.RoadMaxGrade,
            MaxCutDepthMeters = WorldScale.RoadMaxCutDepthMeters,
            MaxFillHeightMeters = WorldScale.RoadMaxFillHeightMeters,
            TurnRadiusMeters = 18f,
            ParallelRoadPenalty = 1f / Mathf.Max(0.01f, WorldScale.RoadExistingCorridorDiscount),

            TownCount = 5,
            ShrineCount = 9,
            SmallCampCount = 9,
            LandmarkCount = 9,
            MinimumPoiDistanceMeters = 64f,
        };

        public void Clamp()
        {
            LakeCount = Mathf.Clamp(LakeCount, WorldScale.LakeMinCount, 6);
            LakeSize = Mathf.Clamp01(LakeSize);
            ShapeIrregularity = Mathf.Clamp01(ShapeIrregularity);
            TerrainConformity = Mathf.Clamp01(TerrainConformity);
            WaterInsetMeters = Mathf.Clamp(WaterInsetMeters, 1.0f, 2.5f);
            WaterDepthOffsetMeters = Mathf.Clamp(WaterDepthOffsetMeters, 0.75f, 2.5f);
            MaxDepthMeters = Mathf.Clamp(MaxDepthMeters, 8f, 48f);
            MaxLakeTerrainModificationMeters = Mathf.Clamp(MaxLakeTerrainModificationMeters, 4f, 48f);

            RiverSourceWidthMeters = Mathf.Clamp(RiverSourceWidthMeters, 1f, 20f);
            RiverMouthWidthMeters = Mathf.Clamp(RiverMouthWidthMeters, RiverSourceWidthMeters, 60f);
            RiverBankWidthMeters = Mathf.Clamp(RiverBankWidthMeters, 2f, 40f);

            PrimaryRoadCostWeight = Mathf.Max(0.1f, PrimaryRoadCostWeight);
            SecondaryRoadCostWeight = Mathf.Max(0.1f, SecondaryRoadCostWeight);
            MaxGrade = Mathf.Clamp(MaxGrade, 0.02f, 0.35f);
            MaxCutDepthMeters = Mathf.Clamp(MaxCutDepthMeters, 0.25f, 8f);
            MaxFillHeightMeters = Mathf.Clamp(MaxFillHeightMeters, 0.25f, 8f);
            TurnRadiusMeters = Mathf.Clamp(TurnRadiusMeters, 4f, 80f);
            ParallelRoadPenalty = Mathf.Max(0.1f, ParallelRoadPenalty);

            TownCount = Mathf.Clamp(TownCount, 1, 12);
            ShrineCount = Mathf.Clamp(ShrineCount, 0, 20);
            SmallCampCount = Mathf.Clamp(SmallCampCount, 0, 40);
            LandmarkCount = Mathf.Clamp(LandmarkCount, 0, 20);
            MinimumPoiDistanceMeters = Mathf.Clamp(MinimumPoiDistanceMeters, 16f, 400f);
        }
    }
}
