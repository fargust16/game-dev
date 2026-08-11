using System.Collections.Generic;
using GameDev.World.Buildings.Shell;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.Terrain;
using GameDev.World.Water;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// S6: TownApproach POI → SettlementPose → Street Spine fabric in world space.
    /// Height Y is supplied by the caller (raycast / terrain); pure path stays mesh-free.
    /// </summary>
    public static class SettlementApproachBinder
    {
        private const float DrySearchStepMeters = 48f;
        private const int DrySearchRingMax = 4;
        public static bool TryFindTownApproach(IEnumerable<PoiMarker> markers, out PoiMarker approach)
        {
            approach = default;
            if (markers == null)
                return false;

            var found = false;
            foreach (var marker in markers)
            {
                if (marker.Kind != PoiKind.TownApproach)
                    continue;
                if (!found)
                {
                    approach = marker;
                    found = true;
                    continue;
                }

                // Stable pick: lowest PoiId string among TownApproach markers.
                if (string.CompareOrdinal(marker.Id.Value, approach.Id.Value) < 0)
                    approach = marker;
            }

            return found;
        }

        public static SettlementPose BuildPose(PoiMarker approach, float worldY, int worldSeed)
        {
            var ws = new WorldSeed(worldSeed);
            var rng = ws.Fork($"settlement_pose_{approach.Id.Value}");
            var yaw = rng.Next(0, 4) * 90f;
            var settlementSeed = unchecked(worldSeed * 397 ^ (approach.Id.Value != null
                ? approach.Id.Value.GetHashCode()
                : 0));
            if (settlementSeed == int.MinValue)
                settlementSeed = 1;

            return SettlementPose.Create(
                new Vector3(approach.Position.x, worldY, approach.Position.z),
                yaw,
                settlementSeed,
                approach.Id.Value);
        }

        public static SettlementFabricLayout CreateFabricAtApproach(
            PoiMarker approach,
            float worldY,
            int worldSeed,
            DistrictKind districtKind = DistrictKind.Worker,
            int blockCount = 2,
            int plotsPerSide = 3)
        {
            var pose = BuildPose(approach, worldY, worldSeed);
            // S7 default overworld fabric: Commercial + GardenSquare + Worker.
            var local = SettlementFabricLayout.CreateDistrictMix(
                pose.SettlementSeed,
                plotsPerSide);
            return local.AtPose(pose);
        }

        /// <summary>
        /// Build + terrain-snap a fabric on dry land near <paramref name="approach"/>.
        /// Searches a ring of XZ offsets when the approach itself is wet or the footprint floods.
        /// </summary>
        public static bool TryCreateDryFabricNearApproach(
            PoiMarker approach,
            int worldSeed,
            IWorldHeightSampler height,
            IWaterSampler water,
            out SettlementFabricLayout fabric,
            out Vector3 placedAt,
            DistrictKind districtKind = DistrictKind.Worker,
            int blockCount = 2,
            int plotsPerSide = 3,
            float freeboardMeters = SettlementSiteValidator.DefaultFreeboardMeters)
        {
            fabric = null;
            placedAt = default;
            if (height == null)
                return false;

            foreach (var offset in DrySearchOffsets())
            {
                var x = approach.Position.x + offset.x;
                var z = approach.Position.z + offset.y;
                if (!height.TrySampleWorldY(x, z, out var y))
                    continue;
                if (!SettlementSiteValidator.IsPointDry(x, z, y, water, freeboardMeters))
                    continue;

                var probe = new PoiMarker(
                    approach.Id,
                    approach.Kind,
                    new Vector3(x, 0f, z),
                    approach.Biome);
                var candidate = CreateFabricAtApproach(
                    probe, y, worldSeed, districtKind, blockCount, plotsPerSide);
                SettlementTerrainConform.SnapFabric(candidate, height);
                if (!SettlementSiteValidator.IsFabricDry(candidate, water, freeboardMeters))
                    continue;

                fabric = candidate;
                placedAt = candidate.Pose.Position;
                return true;
            }

            return false;
        }

        private static IEnumerable<Vector2> DrySearchOffsets()
        {
            yield return Vector2.zero;
            for (var ring = 1; ring <= DrySearchRingMax; ring++)
            {
                var r = ring * DrySearchStepMeters;
                yield return new Vector2(r, 0f);
                yield return new Vector2(-r, 0f);
                yield return new Vector2(0f, r);
                yield return new Vector2(0f, -r);
                yield return new Vector2(r, r);
                yield return new Vector2(r, -r);
                yield return new Vector2(-r, r);
                yield return new Vector2(-r, -r);
            }
        }
    }
}
