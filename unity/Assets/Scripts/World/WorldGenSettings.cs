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
        public float PrimaryRoadCostWeight;
        public float SecondaryRoadCostWeight;
        public float MaxGrade;
        public float MaxCutDepthMeters;
        public float MaxFillHeightMeters;
        public float TurnRadiusMeters;
        public float ParallelRoadPenalty;
        public int TownCount;
        public int ShrineCount;
        public int SmallCampCount;
        public int LandmarkCount;
        public float MinimumPoiDistanceMeters;

        public bool RunSurface;
        public bool RunMountains;
        public bool RunMarkers;
        public bool RunSettlements;
        public bool RunRoads;
        public bool RunPlayer;

        public static WorldGenSettings Default => new WorldGenSettings
        {
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
            RunMountains = true,
            RunMarkers = true,
            RunSettlements = true,
            RunRoads = true,
            RunPlayer = true,
        };

        public void Clamp()
        {
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
                case OverworldPipelineStepId.Mountains: return RunMountains;
                case OverworldPipelineStepId.Markers: return RunMarkers;
                case OverworldPipelineStepId.Settlements: return RunSettlements;
                case OverworldPipelineStepId.Roads: return RunRoads;
                case OverworldPipelineStepId.Player: return RunPlayer;
                default: return true;
            }
        }
    }

    /// <summary>Editor pipeline step ids (WORLD_GENERATION_PIPELINE.md §5).</summary>
    public enum OverworldPipelineStepId
    {
        Surface,
        Mountains,
        Markers,
        Settlements,
        Roads,
        Player,
    }
}
