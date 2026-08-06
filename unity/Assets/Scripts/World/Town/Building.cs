using GameDev.World.Town.Geometry;
using UnityEngine;

namespace GameDev.World.Town
{
    /// <summary>Broad architectural/functional category of a building.</summary>
    public enum BuildingKind
    {
        Residential,
        Commercial,
        Civic,
        Industrial,
        Religious,
        Landmark
    }

    /// <summary>
    /// A building instance: a footprint polygon, floor count, and back-reference
    /// to the plot it occupies. Optional D1 template placement for enterable interiors.
    /// </summary>
    public sealed class Building
    {
        /// <summary>Ground-floor footprint polygon (world-space XZ, metres).</summary>
        public Polygon2 Footprint;

        /// <summary>Number of above-ground floors (may be fractional for rendering).</summary>
        public float Floors;

        /// <summary>Index into <see cref="TownModel.Plots"/>.</summary>
        public int PlotIndex;

        /// <summary>Architectural/functional category.</summary>
        public BuildingKind Kind;

        /// <summary>Assigned interior schematic, or <see cref="BuildingTemplateId.None"/>.</summary>
        public BuildingTemplateId TemplateId;

        /// <summary>World XZ of local cell (0,0) corner (door side = local −Z).</summary>
        public Vector2 TemplateOrigin;

        /// <summary>Yaw degrees; local +Z aligns with depth into the building.</summary>
        public float TemplateYawDeg;

        public bool HasTemplate => TemplateId != BuildingTemplateId.None;

        public Building()
        {
            Footprint = new Polygon2();
            TemplateId = BuildingTemplateId.None;
        }

        public void ClearTemplate()
        {
            TemplateId = BuildingTemplateId.None;
            TemplateOrigin = Vector2.zero;
            TemplateYawDeg = 0f;
        }
    }
}
