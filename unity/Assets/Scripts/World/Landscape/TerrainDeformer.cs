using System.Collections.Generic;
using GameDev.World.Roads;
using UnityEngine;

namespace GameDev.World.Landscape
{
    /// <summary>
    /// Applies planned landscape deformations: road corridors only (no inland lake dig).
    /// </summary>
    public static class TerrainDeformer
    {
        public static void Apply(
            UnityEngine.Terrain terrain,
            IReadOnlyList<RoadDefinition> roads,
            TerrainLayer roadCenterLayer = null,
            TerrainLayer roadGravelLayer = null,
            TerrainLayer roadEdgeLayer = null)
        {
            if (terrain == null)
                return;
            if (roads != null && roads.Count > 0)
            {
                RoadCorridorDeformer.Apply(
                    terrain, roads, roadCenterLayer, roadGravelLayer, roadEdgeLayer);
            }
        }
    }
}
