using System;
using System.Collections.Generic;
using GameDev.World.Roads;
using GameDev.World.Terrain;
using GameDev.World.Water;
using UnityEngine;
using WorldScale = GameDev.World.WorldScale;

namespace GameDev.World.Landscape
{
    /// <summary>
    /// Orchestrates landscape definitions → cost field → road routing → road profiles.
    /// Does not own Unity scene objects; editor menus apply meshes / deformers.
    /// </summary>
    public sealed class LandscapePlanner
    {
        public LandscapeCostField CostField { get; private set; }
        public IReadOnlyList<LakeDefinition> Lakes { get; private set; } =
            Array.Empty<LakeDefinition>();
        public IReadOnlyList<RoadDefinition> Roads { get; private set; } =
            Array.Empty<RoadDefinition>();
        public IReadOnlyList<RoadSegment> Segments { get; private set; } =
            Array.Empty<RoadSegment>();

        public void SetLakes(IReadOnlyList<LakeDefinition> lakes)
        {
            Lakes = lakes ?? Array.Empty<LakeDefinition>();
        }

        public void BuildCostField(
            float originX,
            float originZ,
            float sizeX,
            float sizeZ,
            Func<float, float, float> heightAtWorld,
            Func<float, float, BiomeId> biomeAtWorld,
            float cellSize = -1f)
        {
            var cell = cellSize > 0f ? cellSize : WorldScale.LandscapeCostCellMeters;
            CostField = new LandscapeCostField(
                originX, originZ, sizeX, sizeZ, cell,
                heightAtWorld, biomeAtWorld, Lakes);
        }

        /// <summary>
        /// Hierarchical trade-tree roads on the cost field, then vertical profiles + corridor defs.
        /// Guarantees every POI node has degree ≥ 1 when a route exists.
        /// </summary>
        public void PlanRoads(
            IReadOnlyList<Vector2> nodes,
            IReadOnlyList<bool> isHub,
            RoadGridSpec grid,
            Func<float, float, float> heightMetersAtWorld,
            Func<Vector2, Vector2, bool> canBridgeSpan = null)
        {
            if (CostField == null)
            {
                Roads = Array.Empty<RoadDefinition>();
                Segments = Array.Empty<RoadSegment>();
                return;
            }

            Func<float, float, float> costAtWorld = (x, z) => CostField.EvaluateAtTerrain(x, z);

            var segments = RoadNetworkBuilder.BuildHierarchicalNetwork(
                nodes, isHub, grid, CostField, heightMetersAtWorld, canBridgeSpan);

            RoadNetworkBuilder.EnsureMinimumConnectivity(
                nodes, segments, grid, costAtWorld, heightMetersAtWorld, canBridgeSpan,
                minimumDegree: 1, CostField);

            Segments = segments;
            Roads = BuildRoadDefinitions(
                segments,
                heightMetersAtWorld,
                CostField != null ? CostField.MaxGrade : -1f,
                CostField != null ? CostField.MaxGradeChangeOrDefault() : -1f);
        }

        public static List<RoadDefinition> BuildRoadDefinitions(
            IReadOnlyList<RoadSegment> segments,
            Func<float, float, float> heightMetersAtWorld,
            float maxGrade = -1f,
            float maxGradeChange = -1f)
        {
            var roads = new List<RoadDefinition>();
            if (segments == null)
            {
                return roads;
            }

            for (var i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];
                if (seg.Points == null || seg.Points.Count < 2)
                {
                    continue;
                }

                var profile = RoadVerticalProfile.Build(
                    seg.Points, heightMetersAtWorld, maxGrade, maxGradeChange);
                float width;
                float shoulder;
                float blend;
                switch (seg.Tier)
                {
                    case RoadTier.Local:
                        width = WorldScale.RoadLocalWidthMeters;
                        shoulder = WorldScale.RoadLocalShoulderMeters;
                        blend = WorldScale.RoadLocalBlendMeters;
                        break;
                    case RoadTier.Secondary:
                        width = WorldScale.RoadSecondaryWidthMeters;
                        shoulder = WorldScale.RoadSecondaryShoulderMeters;
                        blend = WorldScale.RoadSecondaryBlendMeters;
                        break;
                    default:
                        width = WorldScale.RoadPrimaryWidthMeters;
                        shoulder = WorldScale.RoadPrimaryShoulderMeters;
                        blend = WorldScale.RoadPrimaryBlendMeters;
                        break;
                }

                roads.Add(new RoadDefinition(
                    seg.Points,
                    profile,
                    width,
                    shoulder,
                    blend,
                    seg.Tier));
            }

            return roads;
        }
    }
}
