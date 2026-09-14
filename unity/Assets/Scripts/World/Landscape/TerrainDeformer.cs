using System.Collections.Generic;
using GameDev.World.Roads;
using UnityEngine;

namespace GameDev.World.Landscape
{
    /// <summary>
    /// Applies planned landscape deformations. Road corridors are the only kind: terrain lerps
    /// toward the road profile across road / shoulder / outer blend zones, and nothing else in the
    /// pipeline is allowed to rewrite the baked heightmap after the bake.
    /// </summary>
    public static class TerrainDeformer
    {
        /// <summary>
        /// Applies the road corridors to every tile they cross.
        /// </summary>
        /// <remarks>
        /// The world is a tile grid and a road does not stop at a tile edge, so this has to run per
        /// tile: deforming only one of them left three quarters of the world with planned roads and
        /// no road surface. Each pass clips itself — <c>RoadCorridorDeformer</c> derives its pixel
        /// range from world coordinates and clamps it to the tile, so a corridor outside a given
        /// tile costs an empty loop and a corridor crossing an edge is handled by both neighbours.
        /// </remarks>
        public static void Apply(
            IReadOnlyList<UnityEngine.Terrain> terrains,
            IReadOnlyList<RoadDefinition> roads,
            TerrainLayer roadCenterLayer = null,
            TerrainLayer roadGravelLayer = null,
            TerrainLayer roadEdgeLayer = null)
        {
            if (terrains == null || roads == null || roads.Count == 0)
                return;

            for (var i = 0; i < terrains.Count; i++)
            {
                var terrain = terrains[i];
                if (terrain == null)
                    continue;

                RoadCorridorDeformer.Apply(
                    terrain, roads, roadCenterLayer, roadGravelLayer, roadEdgeLayer);
            }
        }

        /// <summary>Single-tile overload, for callers that genuinely mean one tile.</summary>
        public static void Apply(
            UnityEngine.Terrain terrain,
            IReadOnlyList<RoadDefinition> roads,
            TerrainLayer roadCenterLayer = null,
            TerrainLayer roadGravelLayer = null,
            TerrainLayer roadEdgeLayer = null)
        {
            Apply(new[] { terrain }, roads, roadCenterLayer, roadGravelLayer, roadEdgeLayer);
        }
    }
}
