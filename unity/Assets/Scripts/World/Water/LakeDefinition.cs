using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Single source of truth for one lake: analytic shore SDF drives basin dig, water mesh,
    /// shoreline paint, road lake-penalty, and vegetation exclusion. Water shape is the silhouette
    /// expanded by <see cref="WaterOverlapMeters"/>; basin is expanded by <see cref="BasinExpandMeters"/>.
    /// </summary>
    public sealed class LakeDefinition
    {
        public string Id { get; }
        public LakeSilhouette Silhouette { get; }
        public float WaterLevelY { get; private set; }
        public float TargetDepthMeters { get; }
        public float BasinExpandMeters { get; }
        public float WaterOverlapMeters { get; }

        /// <summary>Legacy name — negative of overlap (shrink). Prefer <see cref="WaterOverlapMeters"/>.</summary>
        public float WaterInsetMeters => -WaterOverlapMeters;

        /// <summary>Legacy/runtime zone sharing the same silhouette (flood mask for rivers/flow).</summary>
        public WaterZone Zone { get; }

        public LakeDefinition(
            WaterZone zone,
            float targetDepthMeters,
            float basinExpandMeters = -1f,
            float waterOverlapMeters = -1f)
        {
            Zone = zone ?? throw new System.ArgumentNullException(nameof(zone));
            Id = zone.Id;
            Silhouette = zone.Silhouette;
            WaterLevelY = zone.WaterLevelY;
            TargetDepthMeters = Mathf.Max(1f, targetDepthMeters);
            BasinExpandMeters = basinExpandMeters >= 0f
                ? basinExpandMeters
                : WorldScale.LakeBasinExpandMeters;
            // Callers historically passed inset (>=0 shrink). Negative or -1 → use WorldScale overlap.
            if (waterOverlapMeters < 0f)
            {
                WaterOverlapMeters = WorldScale.WaterOverlapMeters;
            }
            else
            {
                // Positive value from old APIs meant inset; convert to overlap only when explicitly
                // using the new WorldScale.WaterOverlapMeters path (constructor default -1).
                WaterOverlapMeters = waterOverlapMeters;
            }
        }

        public void SetWaterLevel(float waterLevelY)
        {
            WaterLevelY = waterLevelY;
            Zone.SetWaterLevel(waterLevelY);
        }

        /// <summary>Water shore SDF: positive inside analytic shore iso.</summary>
        public float WaterShoreSignedDistance(float worldX, float worldZ) =>
            Zone.ShoreSignedDistance(worldX, worldZ);

        /// <summary>
        /// Basin SDF: positive inside the excavated bowl. Expanded so water always sits inside.
        /// </summary>
        public float BasinSignedDistance(float worldX, float worldZ) =>
            WaterShoreSignedDistance(worldX, worldZ) + BasinExpandMeters;

        /// <summary>True inside the water sheet (shore expanded by overlap).</summary>
        public bool IsInsideWater(float worldX, float worldZ) =>
            WaterShoreSignedDistance(worldX, worldZ) >= -WaterOverlapMeters;

        public bool IsInsideBasin(float worldX, float worldZ) =>
            BasinSignedDistance(worldX, worldZ) >= 0f;

        public Rect GetBasinAabbXZ()
        {
            var r = Zone.GetFloodAabbXZ();
            var pad = BasinExpandMeters + 2f;
            return Rect.MinMaxRect(r.xMin - pad, r.yMin - pad, r.xMax + pad, r.yMax + pad);
        }

        public static LakeDefinition[] FromZones(
            System.Collections.Generic.IReadOnlyList<WaterZone> zones,
            float targetDepthMeters,
            float basinExpandMeters = -1f,
            float waterOverlapMeters = -1f)
        {
            if (zones == null || zones.Count == 0)
            {
                return System.Array.Empty<LakeDefinition>();
            }

            var defs = new LakeDefinition[zones.Count];
            for (var i = 0; i < zones.Count; i++)
            {
                var depth = LakeTerrainCarver.DepthForLakeSize(zones[i], targetDepthMeters);
                defs[i] = new LakeDefinition(zones[i], depth, basinExpandMeters, waterOverlapMeters);
            }

            return defs;
        }
    }
}
