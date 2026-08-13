using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Console diagnostics for river trench + water ribbon (prefix <c>[River]</c>).
    /// Use after carve → sync → mesh to see why the surface looks interrupted.
    /// </summary>
    public static class RiverDiagnostics
    {
        public const float BuriedEpsilonMeters = 0.05f;
        public const float BedSpikeMeters = 1.25f;
        public const float SurfaceJumpMeters = 1.5f;

        /// <summary>After trench carve + <see cref="RiverPath.SyncWaterSurfaceAfterCarve"/>.</summary>
        public static void LogAfterCarveSync(
            IReadOnlyList<RiverPath> rivers,
            System.Func<float, float, float> sampleTerrainY,
            float waterFillFraction)
        {
            if (rivers == null || rivers.Count == 0)
            {
                Debug.Log("[River] none");
                return;
            }

            Debug.Log(
                $"[River] === carve+sync rivers={rivers.Count} fill={waterFillFraction:F2} " +
                $"carveDensifyStep={RiverTerrainCarver.DensifyStepMeters:F2}m ===");

            for (var i = 0; i < rivers.Count; i++)
            {
                var river = rivers[i];
                if (river == null || river.Centerline.Count == 0)
                {
                    Debug.LogWarning("[River] empty path");
                    continue;
                }

                LogPathOverview(river);
                LogBedProfile(river, sampleTerrainY);
                LogSyncedSurface(river, sampleTerrainY);
            }
        }

        /// <summary>After <see cref="RiverMeshBuilder.Build"/> (Chaikin + terrain resample).</summary>
        public static void LogAfterMesh(
            IReadOnlyList<RiverPath> rivers,
            System.Func<float, float, float> sampleTerrainY)
        {
            if (rivers == null || rivers.Count == 0)
            {
                return;
            }

            Debug.Log(
                $"[River] === post-mesh rivers={rivers.Count} " +
                $"meshFreeboard={RiverMeshBuilder.MeshFreeboardMeters:F2}m " +
                $"meshDensifyStep={RiverMeshBuilder.DensifyStepMeters:F2}m ===");

            for (var i = 0; i < rivers.Count; i++)
            {
                var river = rivers[i];
                if (river == null || river.Centerline.Count == 0)
                {
                    continue;
                }

                LogMeshRibbon(river, sampleTerrainY);
            }
        }

        private static void LogPathOverview(RiverPath river)
        {
            var n = river.Centerline.Count;
            var len = 0f;
            var wMin = float.PositiveInfinity;
            var wMax = 0f;
            var dMin = float.PositiveInfinity;
            var dMax = 0f;
            for (var i = 0; i < n; i++)
            {
                wMin = Mathf.Min(wMin, river.Widths[i]);
                wMax = Mathf.Max(wMax, river.Widths[i]);
                dMin = Mathf.Min(dMin, river.CarveDepths[i]);
                dMax = Mathf.Max(dMax, river.CarveDepths[i]);
                if (i > 0)
                {
                    var a = river.Centerline[i - 1];
                    var b = river.Centerline[i];
                    len += Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                }
            }

            RiverTerrainCarver.DensifyPath(
                river, RiverTerrainCarver.DensifyStepMeters,
                out var densified, out _, out _);

            var start = river.Centerline[0];
            var end = river.Centerline[n - 1];
            Debug.Log(
                $"[River] {river.Id} path points={n} densifyPts={densified.Count} lenXZ={len:F0}m " +
                $"width={wMin:F1}–{wMax:F1}m carveDepth={dMin:F1}–{dMax:F1}m " +
                $"start=({start.x:F0},{start.z:F0}) yPre={start.y:F1} " +
                $"end=({end.x:F0},{end.z:F0}) yPre={end.y:F1} → {river.Termination}");
        }

        private static void LogBedProfile(
            RiverPath river, System.Func<float, float, float> sampleTerrainY)
        {
            if (sampleTerrainY == null)
            {
                Debug.LogWarning($"[River] {river.Id} bed: no terrain sampler");
                return;
            }

            RiverTerrainCarver.DensifyPath(
                river, RiverTerrainCarver.DensifyStepMeters,
                out var pts, out var halfW, out var carveD);

            var bedMin = float.PositiveInfinity;
            var bedMax = float.NegativeInfinity;
            var worstRise = 0f;
            var riseIndex = -1;
            var spikeCount = 0;
            var prevBed = 0f;
            var sb = new StringBuilder(256);
            sb.Append($"[River] {river.Id} bed samples (every ~{Mathf.Max(1, pts.Count / 8)} pts): ");

            var step = Mathf.Max(1, pts.Count / 8);
            for (var i = 0; i < pts.Count; i++)
            {
                var bed = SampleBed(sampleTerrainY, pts[i].x, pts[i].y, halfW[i] * 0.5f);
                bedMin = Mathf.Min(bedMin, bed);
                bedMax = Mathf.Max(bedMax, bed);
                if (i > 0)
                {
                    var rise = bed - prevBed;
                    if (rise > worstRise)
                    {
                        worstRise = rise;
                        riseIndex = i;
                    }

                    // Downstream bed should not jump up hard — those ridges bury a flat ribbon.
                    if (rise > BedSpikeMeters)
                    {
                        spikeCount++;
                    }
                }

                prevBed = bed;

                if (i % step == 0 || i == pts.Count - 1)
                {
                    if (i > 0 && i % step == 0)
                    {
                        sb.Append(" | ");
                    }

                    sb.Append($"i={i} bed={bed:F1} halfW={halfW[i]:F1} carve={carveD[i]:F1}");
                }
            }

            Debug.Log(
                $"[River] {river.Id} bed: min={bedMin:F1} max={bedMax:F1} span={bedMax - bedMin:F1}m " +
                $"uphillSpikes(>{BedSpikeMeters:F1}m)={spikeCount} " +
                $"worstRise={worstRise:F1}m@(i={riseIndex}) " +
                $"(spikes → ribbon dips under bank / looks interrupted)");
            Debug.Log(sb.ToString());
        }

        private static void LogSyncedSurface(
            RiverPath river, System.Func<float, float, float> sampleTerrainY)
        {
            var n = river.Centerline.Count;
            var surfMin = float.PositiveInfinity;
            var surfMax = float.NegativeInfinity;
            var freeMin = float.PositiveInfinity;
            var freeMax = float.NegativeInfinity;
            var buried = 0;
            var negFreeboard = 0;
            var bigJumps = 0;
            var worstBurial = 0f;
            var worstJump = 0f;
            var jumpAt = -1;

            for (var i = 0; i < n; i++)
            {
                var p = river.Centerline[i];
                var surf = river.WaterSurfaceY[i];
                surfMin = Mathf.Min(surfMin, surf);
                surfMax = Mathf.Max(surfMax, surf);

                var bed = sampleTerrainY != null
                    ? SampleBed(sampleTerrainY, p.x, p.z, river.Widths[i] * 0.25f)
                    : p.y;
                var free = surf - bed;
                freeMin = Mathf.Min(freeMin, free);
                freeMax = Mathf.Max(freeMax, free);
                if (free < BuriedEpsilonMeters)
                {
                    negFreeboard++;
                }

                if (surf + BuriedEpsilonMeters < bed)
                {
                    buried++;
                    worstBurial = Mathf.Max(worstBurial, bed - surf);
                }

                if (i > 0)
                {
                    var jump = Mathf.Abs(surf - river.WaterSurfaceY[i - 1]);
                    if (jump > SurfaceJumpMeters)
                    {
                        bigJumps++;
                        if (jump > worstJump)
                        {
                            worstJump = jump;
                            jumpAt = i;
                        }
                    }
                }
            }

            Debug.Log(
                $"[River] {river.Id} syncSurface: Y={surfMin:F1}–{surfMax:F1} " +
                $"freeboard={freeMin:F2}–{freeMax:F2}m " +
                $"buriedPts={buried}/{n} worstBurial={worstBurial:F2}m " +
                $"negFreeboard={negFreeboard} " +
                $"absJumps(>{SurfaceJumpMeters:F1}m)={bigJumps} worstJump={worstJump:F1}m@(i={jumpAt})");
        }

        private static void LogMeshRibbon(
            RiverPath river, System.Func<float, float, float> sampleTerrainY)
        {
            RiverMeshBuilder.BuildPolylines(river, out var centerline, out var widths, out var surfaceY);
            RiverMeshBuilder.ResampleSurfaceToTerrain(centerline, widths, surfaceY, sampleTerrainY);

            var buriedCenter = 0;
            var buriedEdge = 0;
            var worst = 0f;
            var worstKind = "";
            var worstI = -1;
            var interruptRuns = 0;
            var inRun = false;
            var maxRun = 0;
            var runLen = 0;

            for (var i = 0; i < centerline.Count; i++)
            {
                var half = widths[i] * 0.5f * RiverMeshBuilder.WidthScale + RiverMeshBuilder.WidthPadMeters;
                var offset = RiverMeshBuilder.MiterOffset(centerline, i, half);
                var cx = centerline[i].x;
                var cz = centerline[i].z;
                var sy = surfaceY[i];

                var buriedHere = false;
                if (sampleTerrainY != null)
                {
                    var bedC = sampleTerrainY(cx, cz);
                    CheckBurial(sy, bedC, i, "center", ref buriedCenter, ref worst, ref worstKind, ref worstI, ref buriedHere);

                    // Edge samples: only count as buried if bank is above water AND still below
                    // a shallow threshold (deep banks are expected; crumple was from lifting edges).
                    var bedL = sampleTerrainY(cx - offset.x, cz - offset.z);
                    var bedR = sampleTerrainY(cx + offset.x, cz + offset.z);
                    if (bedL > sy + BuriedEpsilonMeters && bedL < sy + 2.5f)
                    {
                        CheckBurial(sy, bedL, i, "left", ref buriedEdge, ref worst, ref worstKind, ref worstI, ref buriedHere);
                    }

                    if (bedR > sy + BuriedEpsilonMeters && bedR < sy + 2.5f)
                    {
                        CheckBurial(sy, bedR, i, "right", ref buriedEdge, ref worst, ref worstKind, ref worstI, ref buriedHere);
                    }
                }

                if (buriedHere)
                {
                    if (!inRun)
                    {
                        interruptRuns++;
                        inRun = true;
                        runLen = 1;
                    }
                    else
                    {
                        runLen++;
                    }

                    maxRun = Mathf.Max(maxRun, runLen);
                }
                else
                {
                    inRun = false;
                    runLen = 0;
                }
            }

            var verts = centerline.Count * 2;
            Debug.Log(
                $"[River] {river.Id} meshRibbon: polyPts={centerline.Count} verts≈{verts} " +
                $"buriedCenter={buriedCenter} buriedEdgeHits={buriedEdge} " +
                $"interruptRuns={interruptRuns} longestBuriedRun={maxRun} " +
                $"worstBurial={worst:F2}m@{worstKind}[i={worstI}] " +
                $"(interruptRuns>0 ⇒ teal ribbon vanishes into trench floor/banks)");

            // Compact strip of freeboard along mesh polyline for quick scan.
            if (sampleTerrainY != null && centerline.Count > 0)
            {
                var sb = new StringBuilder(200);
                sb.Append($"[River] {river.Id} meshFreeboard strip: ");
                var step = Mathf.Max(1, centerline.Count / 10);
                for (var i = 0; i < centerline.Count; i += step)
                {
                    var bed = sampleTerrainY(centerline[i].x, centerline[i].z);
                    var free = surfaceY[i] - bed;
                    if (i > 0)
                    {
                        sb.Append(" | ");
                    }

                    sb.Append($"i={i} fb={free:F2}");
                    if (free < BuriedEpsilonMeters)
                    {
                        sb.Append(" BURY");
                    }
                }

                var last = centerline.Count - 1;
                if (last % step != 0)
                {
                    var bed = sampleTerrainY(centerline[last].x, centerline[last].z);
                    var free = surfaceY[last] - bed;
                    sb.Append($" | i={last} fb={free:F2}");
                    if (free < BuriedEpsilonMeters)
                    {
                        sb.Append(" BURY");
                    }
                }

                Debug.Log(sb.ToString());
            }
        }

        private static void CheckBurial(
            float surfaceY,
            float bedY,
            int index,
            string kind,
            ref int counter,
            ref float worst,
            ref string worstKind,
            ref int worstI,
            ref bool buriedHere)
        {
            var burial = bedY - surfaceY;
            if (burial > BuriedEpsilonMeters)
            {
                counter++;
                buriedHere = true;
                if (burial > worst)
                {
                    worst = burial;
                    worstKind = kind;
                    worstI = index;
                }
            }
        }

        private static float SampleBed(
            System.Func<float, float, float> sample, float x, float z, float half)
        {
            var w = Mathf.Max(half, 0.5f);
            var bed = sample(x, z);
            bed = Mathf.Min(bed, sample(x + w, z));
            bed = Mathf.Min(bed, sample(x - w, z));
            bed = Mathf.Min(bed, sample(x, z + w));
            bed = Mathf.Min(bed, sample(x, z - w));
            return bed;
        }
    }
}
