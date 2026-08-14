using System.Collections.Generic;
using GameDev.World.Roads;
using GameDev.World.Water;
using UnityEngine;

namespace GameDev.World.Landscape
{
    /// <summary>
    /// Applies all planned landscape deformations in one heightmap pass order:
    /// analytic lake basins, then road corridors. Callers own definitions.
    /// </summary>
    public static class TerrainDeformer
    {
        public static void Apply(
            UnityEngine.Terrain terrain,
            IReadOnlyList<LakeDefinition> lakes,
            IReadOnlyList<RoadDefinition> roads,
            TerrainLayer lakeBankLayer = null,
            TerrainLayer roadDirtLayer = null)
        {
            if (terrain == null)
            {
                return;
            }

            if (lakes != null && lakes.Count > 0)
            {
                LakeBasinDeformer.Apply(terrain, lakes, lakeBankLayer);
                var targets = new float[lakes.Count];
                for (var i = 0; i < lakes.Count; i++)
                {
                    targets[i] = lakes[i].TargetDepthMeters;
                }

                LakeTerrainCarver.RecordDigStats(
                    LakeBasinDeformer.LastPerLakeMaxDigMeters,
                    LakeBasinDeformer.LastMaxDigMeters,
                    LakeBasinDeformer.LastHeadroomShiftMeters,
                    targets);
            }

            if (roads != null && roads.Count > 0)
            {
                RoadCorridorDeformer.Apply(terrain, roads, roadDirtLayer, lakes);
            }
        }
    }
}
