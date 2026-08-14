using UnityEngine;

namespace GameDev.World
{
    /// <summary>
    /// Tunable knobs for one overworld generate. Plain C# data — not a ScriptableObject.
    /// World Generator UI and pipeline menus share this struct.
    /// Step flags are not seeds: same seed + Mountains off → stable map without ridges.
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

        // --- Pipeline step mask (Generate All) ---
        public bool RunSurface;
        public bool RunZones;
        public bool RunMountains;
        public bool RunLakes;
        public bool RunMarkers;
        public bool RunSettlements;
        public bool RunRoads;
        public bool RunWaterMeshes;
        public bool RunTrees;
        public bool RunUndergrowth;
        public bool RunGrass;
        public bool RunPlayer;

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

            RunSurface = true,
            RunZones = true,
            RunMountains = true,
            RunLakes = true,
            RunMarkers = true,
            RunSettlements = true,
            RunRoads = true,
            RunWaterMeshes = true,
            RunTrees = true,
            RunUndergrowth = true,
            RunGrass = true,
            RunPlayer = true,
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

        public bool IsStepEnabled(OverworldPipelineStepId step)
        {
            switch (step)
            {
                case OverworldPipelineStepId.Surface: return RunSurface;
                case OverworldPipelineStepId.Zones: return RunZones;
                case OverworldPipelineStepId.Mountains: return RunMountains;
                case OverworldPipelineStepId.Lakes: return RunLakes;
                case OverworldPipelineStepId.Markers: return RunMarkers;
                case OverworldPipelineStepId.Settlements: return RunSettlements;
                case OverworldPipelineStepId.Roads: return RunRoads;
                case OverworldPipelineStepId.WaterMeshes: return RunWaterMeshes;
                case OverworldPipelineStepId.Trees: return RunTrees;
                case OverworldPipelineStepId.Undergrowth: return RunUndergrowth;
                case OverworldPipelineStepId.Grass: return RunGrass;
                case OverworldPipelineStepId.Player: return RunPlayer;
                default: return true;
            }
        }
    }

    /// <summary>Editor pipeline step ids (WORLD_GENERATION_PIPELINE.md §5).</summary>
    public enum OverworldPipelineStepId
    {
        Surface,
        Zones,
        Mountains,
        Lakes,
        Markers,
        Settlements,
        Roads,
        WaterMeshes,
        Trees,
        Undergrowth,
        Grass,
        Player,
    }
}
