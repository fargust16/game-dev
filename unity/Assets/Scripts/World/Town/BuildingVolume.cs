using System.Collections.Generic;
using GameDev.World.Town.Geometry;
using UnityEngine;

namespace GameDev.World.Town
{
    /// <summary>
    /// Axis-aligned-in-Y prism over a building footprint. Used by D2 occlusion
    /// and reusable later for audio zones / interior triggers without GridMap.
    /// </summary>
    public readonly struct BuildingVolume
    {
        public const float DefaultStoreyMetres = 2f;

        public readonly int BuildingIndex;
        public readonly IReadOnlyList<Vector2> FootprintXZ;
        public readonly float MinY;
        public readonly float MaxY;

        public BuildingVolume(int buildingIndex, IReadOnlyList<Vector2> footprintXZ, float minY, float maxY)
        {
            BuildingIndex = buildingIndex;
            FootprintXZ = footprintXZ;
            MinY = minY;
            MaxY = maxY;
        }

        public static BuildingVolume FromBuilding(int buildingIndex, Building building, float storeyMetres = DefaultStoreyMetres)
        {
            if (building == null || building.Footprint == null)
            {
                return new BuildingVolume(buildingIndex, System.Array.Empty<Vector2>(), 0f, 0f);
            }

            var floors = Mathf.Max(1f, building.Floors);
            var maxY = floors * Mathf.Max(0.5f, storeyMetres);
            return new BuildingVolume(buildingIndex, building.Footprint.Vertices, -0.25f, maxY);
        }

        public bool IsValid =>
            FootprintXZ != null && FootprintXZ.Count >= 3 && MaxY > MinY;

        public bool Contains(Vector3 worldPos)
        {
            if (!IsValid)
            {
                return false;
            }

            if (worldPos.y < MinY || worldPos.y > MaxY)
            {
                return false;
            }

            return Geo2.PointInPolygon(new Vector2(worldPos.x, worldPos.z), FootprintXZ);
        }

        public bool ContainsXZ(Vector2 worldXZ)
        {
            if (!IsValid)
            {
                return false;
            }

            return Geo2.PointInPolygon(worldXZ, FootprintXZ);
        }
    }
}
