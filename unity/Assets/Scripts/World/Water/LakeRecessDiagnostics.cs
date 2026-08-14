using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Console diagnostics for lake recess / floating edges (prefix <c>[LakeRecess]</c>).
    /// Samples terrain vs waterY at shore iso and at mesh outer edge (shore − overlap).
    /// Orange selection outline visible ⇒ FLOATING or EXPOSED edges dominate.
    /// </summary>
    public static class LakeRecessDiagnostics
    {
        public const float FloatEpsilonMeters = 0.05f;
        public const int AngleSamples = 24;

        /// <summary>Call immediately after basin dig (before mesh).</summary>
        public static void LogAfterCarve(
            IReadOnlyList<WaterZone> zones,
            System.Func<float, float, float> sampleTerrainY,
            bool carveRan,
            float overlapMeters)
        {
            if (zones == null || zones.Count == 0)
            {
                Debug.Log("[LakeRecess] none");
                return;
            }

            var inset = WorldScale.WaterInsetDepthMeters;
            var depthOff = WorldScale.WaterDepthOffsetMeters;
            Debug.Log(
                $"[LakeRecess] === after-carve lakes={zones.Count} carveRan={carveRan} " +
                $"overlap={overlapMeters:F2}m insetDepth={inset:F2}m depthOffset={depthOff:F2}m " +
                $"basinExpand={WorldScale.LakeBasinExpandMeters:F2}m " +
                $"headroomShift={LakeTerrainCarver.LastHeadroomShiftMeters:F2}m " +
                $"maxDig={LakeTerrainCarver.LastMaxDigMeters:F2}m ===");

            if (!carveRan)
            {
                Debug.Log(
                    "[LakeRecess] carveRan=false — basins already applied via TerrainDeformer " +
                    "(expected on full pipeline). Sampling post-dig terrain vs waterY.");
            }

            for (var i = 0; i < zones.Count; i++)
            {
                LogOneLake(zones[i], sampleTerrainY, overlapMeters, phase: "carve", lakeIndex: i);
            }
        }

        /// <summary>Call after water meshes are built (same samples + verdict).</summary>
        public static void LogAfterMesh(
            IReadOnlyList<WaterZone> zones,
            System.Func<float, float, float> sampleTerrainY,
            float overlapMeters)
        {
            if (zones == null || zones.Count == 0)
            {
                return;
            }

            Debug.Log(
                $"[LakeRecess] === after-mesh lakes={zones.Count} " +
                $"meshY=zone.WaterLevelY (flat sheet) overlap={overlapMeters:F2}m ===");

            for (var i = 0; i < zones.Count; i++)
            {
                LogOneLake(zones[i], sampleTerrainY, overlapMeters, phase: "mesh", lakeIndex: i);
            }
        }

        /// <summary>Internal spill snapshot from <see cref="Landscape.LakeBasinDeformer"/>.</summary>
        public static void LogSpillCompute(
            string lakeId,
            float avgRimY,
            int rimCount,
            float waterYBefore,
            float waterYAfter,
            float offsetUsed)
        {
            Debug.Log(
                $"[LakeRecess] {lakeId} spill: rimN={rimCount} avgRim={avgRimY:F2} " +
                $"offset={offsetUsed:F2} → waterY {waterYBefore:F2}→{waterYAfter:F2} " +
                $"(recess vs avgRim={(avgRimY - waterYAfter):F2}m)");
        }

        private static void LogOneLake(
            WaterZone zone,
            System.Func<float, float, float> sampleTerrainY,
            float overlapMeters,
            string phase,
            int lakeIndex)
        {
            if (zone == null)
            {
                return;
            }

            var dig = lakeIndex < LakeTerrainCarver.LastPerLakeMaxDigMeters.Count
                ? LakeTerrainCarver.LastPerLakeMaxDigMeters[lakeIndex]
                : -1f;
            var target = lakeIndex < LakeTerrainCarver.LastPerLakeTargetDepthMeters.Count
                ? LakeTerrainCarver.LastPerLakeTargetDepthMeters[lakeIndex]
                : -1f;

            var waterY = zone.WaterLevelY;
            var c = zone.GetFloodCentroidXZ();
            Debug.Log(
                $"[LakeRecess] {zone.Id} [{phase}] waterY={waterY:F2} " +
                $"center=({c.x:F0},{c.z:F0}) r≈{zone.EquivalentRadiusMeters:F0}m " +
                $"sil={(zone.Silhouette != null ? "yes" : "NO")} " +
                $"targetDig={target:F1} maxDig={dig:F1}");

            if (sampleTerrainY == null)
            {
                Debug.LogWarning($"[LakeRecess] {zone.Id} no terrain sampler");
                return;
            }

            var centerTerrain = sampleTerrainY(c.x, c.z);
            var centerClearance = waterY - centerTerrain;
            Debug.Log(
                $"[LakeRecess] {zone.Id} center: terrainY={centerTerrain:F2} " +
                $"water−terrain={centerClearance:F2}m " +
                $"{(centerClearance > 0.15f ? "WET_OK" : "BED_TOO_HIGH_or_NO_DIG")}");

            SampleRing(zone, sampleTerrainY, ringOffsetMeters: 0f, waterY, overlapMeters,
                out var shoreRecessed, out var shoreExposed, out var shoreFloating,
                out var shoreMinClear, out var shoreMaxClear, out var shoreWorst,
                out var shoreStrip);

            SampleRing(zone, sampleTerrainY, ringOffsetMeters: overlapMeters, waterY, overlapMeters,
                out var edgeRecessed, out var edgeExposed, out var edgeFloating,
                out var edgeMinClear, out var edgeMaxClear, out var edgeWorst,
                out var edgeStrip);

            // clearance = terrainY − waterY: positive ⇒ bank above water (good for hiding edge)
            Debug.Log(
                $"[LakeRecess] {zone.Id} shore(iso=0): " +
                $"recessed={shoreRecessed} exposed={shoreExposed} FLOATING={shoreFloating}/{AngleSamples} " +
                $"clearance(terrain−water)={shoreMinClear:F2}…{shoreMaxClear:F2}m " +
                $"worstFloat={shoreWorst:F2}m");
            Debug.Log(shoreStrip);

            Debug.Log(
                $"[LakeRecess] {zone.Id} meshEdge(iso−overlap={overlapMeters:F2}m): " +
                $"recessed={edgeRecessed} exposed={edgeExposed} FLOATING={edgeFloating}/{AngleSamples} " +
                $"clearance={edgeMinClear:F2}…{edgeMaxClear:F2}m " +
                $"worstFloat={edgeWorst:F2}m " +
                $"(FLOATING>0 ⇒ orange outline in air)");
            Debug.Log(edgeStrip);

            var verdict = edgeFloating > AngleSamples / 4
                ? "FAIL_FLOATING_EDGE"
                : edgeExposed + edgeFloating > AngleSamples / 2
                    ? "WARN_EDGE_VISIBLE"
                    : shoreFloating > 0
                        ? "WARN_SHORE_GAPS"
                        : "OK_RECESSED";

            Debug.Log(
                $"[LakeRecess] {zone.Id} VERDICT={verdict} " +
                $"(need terrain≥water+{WorldScale.WaterInsetDepthMeters:F1}m at mesh edge)");
        }

        /// <summary>
        /// Ring at shore radius + outward offset (positive offset = outside lake toward land for
        /// mesh edge when using silhouette radius; for SDF we walk angle and push along radial).
        /// </summary>
        private static void SampleRing(
            WaterZone zone,
            System.Func<float, float, float> sampleTerrainY,
            float ringOffsetMeters,
            float waterY,
            float overlapMeters,
            out int recessed,
            out int exposed,
            out int floating,
            out float minClearance,
            out float maxClearance,
            out float worstFloat,
            out string strip)
        {
            recessed = 0;
            exposed = 0;
            floating = 0;
            minClearance = float.PositiveInfinity;
            maxClearance = float.NegativeInfinity;
            worstFloat = 0f;

            var sb = new StringBuilder(320);
            var label = ringOffsetMeters <= 0.01f ? "shore" : "edge";
            sb.Append($"[LakeRecess] {zone.Id} {label} strip: ");

            var cx = zone.Silhouette != null
                ? zone.Silhouette.CenterX
                : zone.GetFloodCentroidXZ().x;
            var cz = zone.Silhouette != null
                ? zone.Silhouette.CenterZ
                : zone.GetFloodCentroidXZ().z;

            var insetNeed = WorldScale.WaterInsetDepthMeters;
            for (var i = 0; i < AngleSamples; i++)
            {
                var ang = (Mathf.PI * 2f * i) / AngleSamples;
                float wx;
                float wz;
                if (zone.Silhouette != null)
                {
                    // Shore point at iso≈0, then push outward (away from center) by ringOffset.
                    // Mesh expands past shore by +overlap on SDF ⇒ outward by overlapMeters.
                    var r = zone.Silhouette.ShoreRadiusAtAngle(ang) + ringOffsetMeters;
                    wx = cx + Mathf.Cos(ang) * r;
                    wz = cz + Mathf.Sin(ang) * r;
                }
                else
                {
                    // Fallback: walk from center until shore SDF ~ 0, then add offset.
                    if (!TryWalkToShore(zone, cx, cz, ang, out wx, out wz))
                    {
                        wx = cx;
                        wz = cz;
                    }

                    if (ringOffsetMeters > 0f)
                    {
                        wx += Mathf.Cos(ang) * ringOffsetMeters;
                        wz += Mathf.Sin(ang) * ringOffsetMeters;
                    }
                }

                var terrainY = sampleTerrainY(wx, wz);
                var clearance = terrainY - waterY; // >0 bank above water
                minClearance = Mathf.Min(minClearance, clearance);
                maxClearance = Mathf.Max(maxClearance, clearance);

                string tag;
                if (clearance < -FloatEpsilonMeters)
                {
                    floating++;
                    worstFloat = Mathf.Max(worstFloat, -clearance);
                    tag = "FLOAT";
                }
                else if (clearance < insetNeed)
                {
                    exposed++;
                    tag = "EXPOSE";
                }
                else
                {
                    recessed++;
                    tag = "OK";
                }

                if (i > 0)
                {
                    sb.Append(" | ");
                }

                sb.Append($"a={i * (360 / AngleSamples)} Δ={clearance:F2}{tag}");
            }

            if (float.IsInfinity(minClearance))
            {
                minClearance = 0f;
                maxClearance = 0f;
            }

            strip = sb.ToString();
            _ = overlapMeters;
        }

        private static bool TryWalkToShore(
            WaterZone zone, float cx, float cz, float ang, out float wx, out float wz)
        {
            var dirX = Mathf.Cos(ang);
            var dirZ = Mathf.Sin(ang);
            var maxR = Mathf.Max(zone.EquivalentRadiusMeters * 2.5f, 40f);
            var step = Mathf.Max(zone.CellSize * 0.5f, 1f);
            var prevSd = zone.ShoreSignedDistance(cx, cz);
            for (var r = step; r <= maxR; r += step)
            {
                wx = cx + dirX * r;
                wz = cz + dirZ * r;
                var sd = zone.ShoreSignedDistance(wx, wz);
                if (prevSd >= 0f && sd < 0f)
                {
                    return true;
                }

                prevSd = sd;
            }

            wx = cx + dirX * zone.EquivalentRadiusMeters;
            wz = cz + dirZ * zone.EquivalentRadiusMeters;
            return false;
        }
    }
}
