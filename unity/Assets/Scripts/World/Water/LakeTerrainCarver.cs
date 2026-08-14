using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Digs one continuous bathymetric basin under each filled lake area (distance-to-shore field,
    /// not spline grooves / centre cones). Interior is dig-only; the shore-overlap ring may fill a
    /// tight lip so the flat water sheet tucks under banks. Plains often sit near heightmap 0, so
    /// the carver first shifts the whole tile up for dig headroom, then cuts the bed under a locked
    /// spill water plane.
    /// <see cref="LastPerLakeMaxDigMeters"/> is the <b>actual</b> applied dig.
    /// </summary>
    public static class LakeTerrainCarver
    {
        /// <summary>Legacy shore floor constant — bathymetry uses ShoreEpsilon (near 0 at lip).</summary>
        public const float MinShoreDepthMeters = 0.45f;

        /// <summary>Large lakes dig at least this deep — enough for a sunk-city volume.</summary>
        public const float MinLargeBasinDepthMeters = GameDev.World.WorldScale.LakeBasinDepthMinMeters;

        /// <summary>Hard cap — deeper than this fights the heightmap ceiling and is not needed for V0.</summary>
        public const float MaxLargeBasinDepthMeters = GameDev.World.WorldScale.LakeBasinDepthMaxMeters;

        /// <summary>
        /// Shore shelf height above the water plane (vertical InsetDepth). Dry ground at the lip
        /// must clear the water sheet so edges never float or gap.
        /// </summary>
        public const float WaterFreeboardMeters = WorldScale.WaterInsetDepthMeters;

        /// <summary>Legacy spill percentile — basin deformer now uses average shore − depth offset.</summary>
        public const float RimSpillPercentile = 0.18f;

        /// <summary>Exterior dry blend — ~1 heightmap texel only.</summary>
        public const float BankBlendMeters = 4f;

        /// <summary>Shore-strip half-width for blur on both wet and dry sides of the lip.</summary>
        public const float ShoreStripBlurMeters = 12f;

        private const int ShoreBlurPasses = 5;
        private const int WallBandBlurPasses = 5;

        /// <summary>Active bathymetric profile (p, flat start, LF noise). Mutable for tuning/tests.</summary>
        public static LakeBathymetry.Settings Profile { get; set; } = LakeBathymetry.Settings.NaturalDefault;

        /// <summary>Call after code reload if an old Profile still has bed noise enabled.</summary>
        public static void ResetProfileToNaturalDefault() =>
            Profile = LakeBathymetry.Settings.NaturalDefault;

        /// <summary>Target dig depth requested for each lake on the last <see cref="Carve"/>.</summary>
        public static IReadOnlyList<float> LastPerLakeTargetDepthMeters { get; private set; } =
            System.Array.Empty<float>();

        /// <summary>Actual max dig applied (pre−post height), per lake.</summary>
        public static IReadOnlyList<float> LastPerLakeMaxDigMeters { get; private set; } =
            System.Array.Empty<float>();

        /// <summary>Deepest actual dig across all lakes on the last carve.</summary>
        public static float LastMaxDigMeters { get; private set; }

        /// <summary>How far the whole heightmap was lifted to make dig room (metres).</summary>
        public static float LastHeadroomShiftMeters { get; private set; }

        /// <summary>Copy dig diagnostics from <see cref="GameDev.World.Landscape.LakeBasinDeformer"/> after joint deform.</summary>
        public static void RecordDigStats(
            IReadOnlyList<float> perLakeMaxDig,
            float maxDigMeters,
            float headroomShiftMeters,
            IReadOnlyList<float> perLakeTargets = null)
        {
            LastPerLakeMaxDigMeters = perLakeMaxDig ?? System.Array.Empty<float>();
            LastMaxDigMeters = maxDigMeters;
            LastHeadroomShiftMeters = headroomShiftMeters;
            if (perLakeTargets != null)
            {
                LastPerLakeTargetDepthMeters = perLakeTargets;
            }
        }

        public static void Carve(
            UnityEngine.Terrain terrain,
            IEnumerable<WaterZone> zones,
            float targetBasinDepthMeters,
            TerrainLayer bankLayer = null)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                LastPerLakeTargetDepthMeters = System.Array.Empty<float>();
                LastPerLakeMaxDigMeters = System.Array.Empty<float>();
                LastMaxDigMeters = 0f;
                LastHeadroomShiftMeters = 0f;
                return;
            }

            var pinned = new List<WaterZone>(zones);
            if (pinned.Count == 0)
            {
                LastPerLakeTargetDepthMeters = System.Array.Empty<float>();
                LastPerLakeMaxDigMeters = System.Array.Empty<float>();
                LastMaxDigMeters = 0f;
                LastHeadroomShiftMeters = 0f;
                return;
            }

            var worldHeight = Mathf.Max(terrain.terrainData.size.y, 0.01f);
            var maxDepth = Mathf.Clamp(
                targetBasinDepthMeters,
                MinLargeBasinDepthMeters,
                Mathf.Min(MaxLargeBasinDepthMeters, worldHeight * 0.45f));

            Profile = LakeBathymetry.Settings.NaturalDefault;

            var defs = LakeDefinition.FromZones(pinned, maxDepth);
            var targets = new float[defs.Length];
            for (var i = 0; i < defs.Length; i++)
            {
                targets[i] = defs[i].TargetDepthMeters;
            }

            LastPerLakeTargetDepthMeters = targets;

            // Analytic basin from LakeDefinition (expanded bowl + designed floor).
            GameDev.World.Landscape.LakeBasinDeformer.Apply(terrain, defs, bankLayer);

            LastPerLakeMaxDigMeters = GameDev.World.Landscape.LakeBasinDeformer.LastPerLakeMaxDigMeters;
            LastMaxDigMeters = GameDev.World.Landscape.LakeBasinDeformer.LastMaxDigMeters;
            LastHeadroomShiftMeters = GameDev.World.Landscape.LakeBasinDeformer.LastHeadroomShiftMeters;
        }

        /// <summary>
        /// Plains often sit near heightmap 0 — there is nowhere to dig. Lift the whole tile so lake
        /// pads have room for <paramref name="targetDepthMeters"/> (peaks clamp at 1).
        /// </summary>
        private static float EnsureDigHeadroom(
            float[,] heights,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            IReadOnlyList<WaterZone> zones,
            float targetDepthMeters)
        {
            var needNorm = (targetDepthMeters + 6f) / worldHeight;
            var minH = 1f;
            var maxH = 0f;

            for (var pz = 0; pz < resolution; pz++)
            {
                for (var px = 0; px < resolution; px++)
                {
                    var h = heights[pz, px];
                    if (h < minH)
                    {
                        minH = h;
                    }

                    if (h > maxH)
                    {
                        maxH = h;
                    }
                }
            }

            // Prefer measuring under lake pads (plains), not mountain peaks.
            var padMin = 1f;
            var anyPad = false;
            for (var i = 0; i < zones.Count; i++)
            {
                var aabb = zones[i].GetFloodAabbXZ();
                var pad = BankBlendMeters + zones[i].CellSize;
                var minPx = Mathf.Max(0, Mathf.FloorToInt((aabb.xMin - pad - terrainPos.x) / worldPerPixelX));
                var maxPx = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.xMax + pad - terrainPos.x) / worldPerPixelX));
                var minPz = Mathf.Max(0, Mathf.FloorToInt((aabb.yMin - pad - terrainPos.z) / worldPerPixelZ));
                var maxPz = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.yMax + pad - terrainPos.z) / worldPerPixelZ));
                for (var pz = minPz; pz <= maxPz; pz++)
                {
                    for (var px = minPx; px <= maxPx; px++)
                    {
                        anyPad = true;
                        var h = heights[pz, px];
                        if (h < padMin)
                        {
                            padMin = h;
                        }
                    }
                }
            }

            var floorNorm = anyPad ? padMin : minH;
            if (floorNorm >= needNorm)
            {
                return 0f;
            }

            var shift = needNorm - floorNorm;
            // Leave a little headroom under the normalized ceiling so peaks are not totally crushed.
            var maxShift = Mathf.Max(0f, 0.98f - maxH);
            if (shift > maxShift)
            {
                shift = maxShift;
            }

            if (shift <= 0.0001f)
            {
                return 0f;
            }

            for (var pz = 0; pz < resolution; pz++)
            {
                for (var px = 0; px < resolution; px++)
                {
                    heights[pz, px] = Mathf.Clamp01(heights[pz, px] + shift);
                }
            }

            return shift * worldHeight;
        }

        /// <summary>
        /// Small ponds stay shallow; large / compact lakes dig toward the requested depth.
        /// Elongated lakes are slightly shallower (same area ≠ same D<sub>max</sub>).
        /// </summary>
        public static float DepthForLakeSize(WaterZone zone, float requestedBasinDepth)
        {
            return LakeBathymetry.MaxDepthForLake(
                zone.EquivalentRadiusMeters,
                LakeBathymetry.AspectCompactness(zone),
                requestedBasinDepth,
                MinLargeBasinDepthMeters,
                MaxLargeBasinDepthMeters);
        }

        private static void SoftenShoreHeights(
            float[,] heights,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            IReadOnlyList<WaterZone> zones)
        {
            var maskSdfs = new float[zones.Count][,];
            for (var i = 0; i < zones.Count; i++)
            {
                if (zones[i].Silhouette == null)
                {
                    maskSdfs[i] = LakeShoreField.BuildSignedDistance(
                        zones[i], exteriorCapMeters: ShoreStripBlurMeters + BankBlendMeters + 4f);
                }
            }

            // Blur a narrow strip on BOTH sides of the shore iso — smooth lip without tearing the pit.
            for (var pass = 0; pass < ShoreBlurPasses; pass++)
            {
                var copy = (float[,])heights.Clone();
                for (var zi = 0; zi < zones.Count; zi++)
                {
                    var aabb = zones[zi].GetFloodAabbXZ();
                    var pad = ShoreStripBlurMeters + BankBlendMeters;
                    var minPx = Mathf.Max(1, Mathf.FloorToInt((aabb.xMin - pad - terrainPos.x) / worldPerPixelX));
                    var maxPx = Mathf.Min(resolution - 2, Mathf.CeilToInt((aabb.xMax + pad - terrainPos.x) / worldPerPixelX));
                    var minPz = Mathf.Max(1, Mathf.FloorToInt((aabb.yMin - pad - terrainPos.z) / worldPerPixelZ));
                    var maxPz = Mathf.Min(resolution - 2, Mathf.CeilToInt((aabb.yMax + pad - terrainPos.z) / worldPerPixelZ));

                    for (var pz = minPz; pz <= maxPz; pz++)
                    {
                        for (var px = minPx; px <= maxPx; px++)
                        {
                            var worldX = terrainPos.x + px * worldPerPixelX;
                            var worldZ = terrainPos.z + pz * worldPerPixelZ;
                            var signed = zones[zi].ShoreSignedDistance(worldX, worldZ, maskSdfs[zi]);
                            if (Mathf.Abs(signed) > ShoreStripBlurMeters)
                            {
                                continue;
                            }

                            var sum =
                                copy[pz - 1, px - 1] + copy[pz - 1, px] + copy[pz - 1, px + 1] +
                                copy[pz, px - 1] + copy[pz, px] * 4f + copy[pz, px + 1] +
                                copy[pz + 1, px - 1] + copy[pz + 1, px] + copy[pz + 1, px + 1];
                            heights[pz, px] = sum / 12f;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Snap wet pixels to bathymetry from a <b>smoothed</b> interior distance field (softens
        /// clover medial ridges), blur the wall band, pin the shore lip to the raw profile.
        /// Dig-only: never raise above <paramref name="originalHeights"/>.
        /// </summary>
        private static void EnforceSmoothWetFloor(
            float[,] heights,
            float[,] originalHeights,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            IReadOnlyList<WaterZone> zones)
        {
            for (var zi = 0; zi < zones.Count; zi++)
            {
                var zone = zones[zi];
                float[,] maskSdf = null;
                if (zone.Silhouette == null)
                {
                    maskSdf = LakeShoreField.BuildSignedDistance(zone, exteriorCapMeters: BankBlendMeters + 4f);
                }

                var aabb = zone.GetFloodAabbXZ();
                var waterPlaneY = zone.WaterLevelY;
                var floorY = terrainPos.y + 0.25f;
                var requested = zi < LastPerLakeTargetDepthMeters.Count
                    ? LastPerLakeTargetDepthMeters[zi]
                    : MaxLargeBasinDepthMeters;
                var depthHere = DepthForLakeSize(zone, requested);
                var profile = ResolvedProfile();
                var seed = HashString(zone.Id);
                var pad = zone.CellSize * 2f;
                var minPx = Mathf.Max(0, Mathf.FloorToInt((aabb.xMin - pad - terrainPos.x) / worldPerPixelX));
                var maxPx = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.xMax + pad - terrainPos.x) / worldPerPixelX));
                var minPz = Mathf.Max(0, Mathf.FloorToInt((aabb.yMin - pad - terrainPos.z) / worldPerPixelZ));
                var maxPz = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.yMax + pad - terrainPos.z) / worldPerPixelZ));

                var rawD = SampleShoreDistanceField(
                    resolution, terrainPos, worldPerPixelX, worldPerPixelZ,
                    zone, maskSdf, minPx, maxPx, minPz, maxPz);
                var smoothD = (float[,])rawD.Clone();
                BlurInteriorDistanceField(
                    smoothD, minPx, maxPx, minPz, maxPz, profile.DistanceFieldSmoothPasses);
                var dMaxInterior = MaxPositiveInRect(smoothD, minPx, maxPx, minPz, maxPz);
                if (dMaxInterior < 1f)
                {
                    dMaxInterior = LakeBathymetry.MaxInteriorDistanceMeters(
                        zone, (x, z) => zone.ShoreSignedDistance(x, z, maskSdf));
                }

                var wallWidth = LakeBathymetry.WallWidthMeters(dMaxInterior, profile);

                for (var pz = minPz; pz <= maxPz; pz++)
                {
                    for (var px = minPx; px <= maxPx; px++)
                    {
                        var raw = rawD[pz, px];
                        if (raw < 0f)
                        {
                            continue;
                        }

                        var worldX = terrainPos.x + px * worldPerPixelX;
                        var worldZ = terrainPos.z + pz * worldPerPixelZ;
                        var dUse = LakeBathymetry.BlendShoreDistanceForBathymetry(
                            raw, smoothD[pz, px], profile.ShorePinMeters);
                        var dig = LakeBathymetry.DepthMeters(
                            dUse, dMaxInterior, depthHere,
                            worldX, worldZ, zone.EquivalentRadiusMeters, seed, profile);
                        var targetInteriorY = Mathf.Max(floorY, waterPlaneY - dig);
                        WriteWorldYDigOnly(
                            heights, originalHeights, pz, px, terrainPos.y, worldHeight, targetInteriorY);
                    }
                }

                BlurWetInteriorHeights(
                    heights, rawD, minPx, maxPx, minPz, maxPz,
                    profile.InteriorHeightSmoothPasses, profile.ShorePinMeters);

                // Final snap to inset-contour plate (flat floor + arc walls) — no centre funnel.
                ResnapInteriorToSmoothBathymetry(
                    heights, originalHeights, rawD, minPx, maxPx, minPz, maxPz,
                    terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight,
                    waterPlaneY, floorY, dMaxInterior, depthHere, profile);

                // Soften wall band bumps, then re-pin only the narrow lip so blur cannot tear the shore.
                BlurWallBandHeights(
                    heights, rawD, minPx, maxPx, minPz, maxPz, wallWidth, WallBandBlurPasses);

                PinShoreBandHeights(
                    heights, originalHeights, rawD, minPx, maxPx, minPz, maxPz,
                    terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight,
                    waterPlaneY, floorY, dMaxInterior, depthHere, zone, seed, profile);
            }
        }

        private static void ResnapInteriorToSmoothBathymetry(
            float[,] heights,
            float[,] originalHeights,
            float[,] rawDistance,
            int minPx,
            int maxPx,
            int minPz,
            int maxPz,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            float waterPlaneY,
            float floorY,
            float dMaxInterior,
            float maxDepthMeters,
            LakeBathymetry.Settings profile)
        {
            var quiet = profile;
            quiet.NoiseAmplitude = 0f;
            for (var pz = minPz; pz <= maxPz; pz++)
            {
                for (var px = minPx; px <= maxPx; px++)
                {
                    var raw = rawDistance[pz, px];
                    if (raw < 0f)
                    {
                        continue;
                    }

                    var dig = LakeBathymetry.BaseDepthFromShoreDistance(
                        raw, dMaxInterior, maxDepthMeters, quiet);
                    dig = Mathf.Max(dig, quiet.ShoreEpsilonMeters);
                    var targetY = Mathf.Max(floorY, waterPlaneY - dig);
                    WriteWorldYDigOnly(
                        heights, originalHeights, pz, px, terrainPos.y, worldHeight, targetY);
                }
            }
        }

        /// <summary>Blur heights in the wet wall ring (0 &lt; d &lt; wall) to kill stair/bump artifacts.</summary>
        private static void BlurWallBandHeights(
            float[,] heights,
            float[,] rawDistance,
            int minPx,
            int maxPx,
            int minPz,
            int maxPz,
            float wallWidthMeters,
            int passes)
        {
            var n = Mathf.Clamp(passes, 0, 8);
            var wall = Mathf.Max(wallWidthMeters, 2f);
            if (n == 0)
            {
                return;
            }

            var w = maxPx - minPx + 1;
            var h = maxPz - minPz + 1;
            if (w < 3 || h < 3)
            {
                return;
            }

            var scratch = new float[h, w];
            for (var pass = 0; pass < n; pass++)
            {
                for (var pz = minPz; pz <= maxPz; pz++)
                {
                    for (var px = minPx; px <= maxPx; px++)
                    {
                        var d = rawDistance[pz, px];
                        if (d <= 0.15f || d >= wall)
                        {
                            scratch[pz - minPz, px - minPx] = heights[pz, px];
                            continue;
                        }

                        var sum = heights[pz, px] * 4f;
                        var count = 4f;
                        for (var oz = -1; oz <= 1; oz++)
                        {
                            for (var ox = -1; ox <= 1; ox++)
                            {
                                if (ox == 0 && oz == 0)
                                {
                                    continue;
                                }

                                var nx = px + ox;
                                var nz = pz + oz;
                                if (nx < minPx || nx > maxPx || nz < minPz || nz > maxPz)
                                {
                                    continue;
                                }

                                var nd = rawDistance[nz, nx];
                                if (nd < 0f)
                                {
                                    continue;
                                }

                                sum += heights[nz, nx];
                                count += 1f;
                            }
                        }

                        scratch[pz - minPz, px - minPx] = sum / count;
                    }
                }

                for (var pz = minPz; pz <= maxPz; pz++)
                {
                    for (var px = minPx; px <= maxPx; px++)
                    {
                        heights[pz, px] = scratch[pz - minPz, px - minPx];
                    }
                }
            }
        }

        private static LakeBathymetry.Settings ResolvedProfile()
        {
            var s = Profile;
            if (s.ShapeExponent <= 0.01f)
            {
                s = LakeBathymetry.Settings.NaturalDefault;
            }

            // Optional mean-depth constraint → nudge p (hypsographic-lite).
            if (s.MeanToMaxDepthRatio > 0.05f)
            {
                s.ShapeExponent = LakeBathymetry.ShapeExponentForMeanRatio(
                    s.MeanToMaxDepthRatio, s.ShapeExponent);
            }

            return s;
        }

        /// <summary>
        /// Legacy wrapper for inset-contour plate: outer shore → inner floor at
        /// <paramref name="flatBottomScale"/> · R, arc walls between.
        /// </summary>
        public static float FlatBottomBowlDigMeters(
            float signedDistanceInside,
            float halfSpanMeters,
            float maxDepthMeters,
            float flatBottomScale = 0.5f)
        {
            var settings = LakeBathymetry.Settings.NaturalDefault;
            settings.InnerContourScale = Mathf.Clamp(flatBottomScale, 0.2f, 0.85f);
            settings.NoiseAmplitude = 0f;
            return Mathf.Max(
                LakeBathymetry.BaseDepthFromShoreDistance(
                    signedDistanceInside, halfSpanMeters, maxDepthMeters, settings),
                settings.ShoreEpsilonMeters);
        }

        /// <summary>Legacy name — bathymetric bowl via distance field.</summary>
        public static float SineBowlDigMeters(float signedDistanceInside, float halfSpanMeters, float maxDepthMeters) =>
            FlatBottomBowlDigMeters(signedDistanceInside, halfSpanMeters, maxDepthMeters);

        private static float CarveFilledBowl(
            float[,] heights,
            float[,] originalHeights,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            WaterZone zone,
            float targetDepthMeters)
        {
            float[,] maskSdf = null;
            if (zone.Silhouette == null)
            {
                maskSdf = LakeShoreField.BuildSignedDistance(zone, exteriorCapMeters: BankBlendMeters + 4f);
            }

            var aabb = zone.GetFloodAabbXZ();
            var profile = ResolvedProfile();
            var seed = HashString(zone.Id);
            var maxActualDig = 0f;
            var waterPlaneY = zone.WaterLevelY;
            var floorY = terrainPos.y + 0.25f;

            var pad = BankBlendMeters + zone.CellSize;
            var minPx = Mathf.Max(0, Mathf.FloorToInt((aabb.xMin - pad - terrainPos.x) / worldPerPixelX) - 1);
            var maxPx = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.xMax + pad - terrainPos.x) / worldPerPixelX) + 1);
            var minPz = Mathf.Max(0, Mathf.FloorToInt((aabb.yMin - pad - terrainPos.z) / worldPerPixelZ) - 1);
            var maxPz = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.yMax + pad - terrainPos.z) / worldPerPixelZ) + 1);

            var rawD = SampleShoreDistanceField(
                resolution, terrainPos, worldPerPixelX, worldPerPixelZ,
                zone, maskSdf, minPx, maxPx, minPz, maxPz);
            var smoothD = (float[,])rawD.Clone();
            BlurInteriorDistanceField(
                smoothD, minPx, maxPx, minPz, maxPz, profile.DistanceFieldSmoothPasses);
            var dMaxInterior = MaxPositiveInRect(smoothD, minPx, maxPx, minPz, maxPz);
            if (dMaxInterior < 1f)
            {
                dMaxInterior = LakeBathymetry.MaxInteriorDistanceMeters(
                    zone, (x, z) => zone.ShoreSignedDistance(x, z, maskSdf));
            }

            for (var pz = minPz; pz <= maxPz; pz++)
            {
                for (var px = minPx; px <= maxPx; px++)
                {
                    var worldX = terrainPos.x + px * worldPerPixelX;
                    var worldZ = terrainPos.z + pz * worldPerPixelZ;
                    var signed = rawD[pz, px];
                    var currentWorldY = terrainPos.y + heights[pz, px] * worldHeight;

                    if (signed < 0f)
                    {
                        // Tiny exterior blend (~1–2 texels) only when land sits above the plane —
                        // never dig a wide dry moat under the waterline.
                        if (signed > -BankBlendMeters && currentWorldY > waterPlaneY + WaterFreeboardMeters)
                        {
                            var t = 1f + signed / BankBlendMeters;
                            var smooth = t * t * (3f - 2f * t);
                            var shelfY = waterPlaneY + WaterFreeboardMeters;
                            var target = Mathf.Lerp(currentWorldY, shelfY, smooth * 0.35f);
                            var appliedBank = WriteWorldYDigOnly(
                                heights, originalHeights, pz, px, terrainPos.y, worldHeight, target);
                            if (appliedBank > maxActualDig)
                            {
                                maxActualDig = appliedBank;
                            }
                        }

                        continue;
                    }

                    var dUse = LakeBathymetry.BlendShoreDistanceForBathymetry(
                        signed, smoothD[pz, px], profile.ShorePinMeters);
                    var dig = LakeBathymetry.DepthMeters(
                        dUse, dMaxInterior, targetDepthMeters,
                        worldX, worldZ, zone.EquivalentRadiusMeters, seed, profile);

                    var targetInteriorY = Mathf.Max(floorY, waterPlaneY - dig);
                    var applied = WriteWorldYDigOnly(
                        heights, originalHeights, pz, px, terrainPos.y, worldHeight, targetInteriorY);
                    if (applied > maxActualDig)
                    {
                        maxActualDig = applied;
                    }
                }
            }

            return maxActualDig;
        }

        private static float[,] SampleShoreDistanceField(
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            WaterZone zone,
            float[,] maskSdf,
            int minPx,
            int maxPx,
            int minPz,
            int maxPz)
        {
            var field = new float[resolution, resolution];
            for (var pz = 0; pz < resolution; pz++)
            {
                for (var px = 0; px < resolution; px++)
                {
                    field[pz, px] = -1e6f;
                }
            }

            for (var pz = minPz; pz <= maxPz; pz++)
            {
                for (var px = minPx; px <= maxPx; px++)
                {
                    var worldX = terrainPos.x + px * worldPerPixelX;
                    var worldZ = terrainPos.z + pz * worldPerPixelZ;
                    field[pz, px] = zone.ShoreSignedDistance(worldX, worldZ, maskSdf);
                }
            }

            return field;
        }

        /// <summary>
        /// Average interior (d≥0) samples with wet neighbours only — softens medial-axis ridges
        /// on lobed lakes without pulling the shoreline outward.
        /// </summary>
        private static void BlurInteriorDistanceField(
            float[,] field,
            int minPx,
            int maxPx,
            int minPz,
            int maxPz,
            int passes)
        {
            var n = Mathf.Clamp(passes, 0, 24);
            if (n == 0)
            {
                return;
            }

            var w = maxPx - minPx + 1;
            var h = maxPz - minPz + 1;
            if (w < 3 || h < 3)
            {
                return;
            }

            var scratch = new float[h, w];
            for (var pass = 0; pass < n; pass++)
            {
                for (var pz = minPz; pz <= maxPz; pz++)
                {
                    for (var px = minPx; px <= maxPx; px++)
                    {
                        var self = field[pz, px];
                        if (self < 0f)
                        {
                            scratch[pz - minPz, px - minPx] = self;
                            continue;
                        }

                        var sum = self * 4f;
                        var count = 4f;
                        AccumulateWet(field, px - 1, pz, ref sum, ref count);
                        AccumulateWet(field, px + 1, pz, ref sum, ref count);
                        AccumulateWet(field, px, pz - 1, ref sum, ref count);
                        AccumulateWet(field, px, pz + 1, ref sum, ref count);
                        AccumulateWet(field, px - 1, pz - 1, ref sum, ref count);
                        AccumulateWet(field, px + 1, pz - 1, ref sum, ref count);
                        AccumulateWet(field, px - 1, pz + 1, ref sum, ref count);
                        AccumulateWet(field, px + 1, pz + 1, ref sum, ref count);
                        scratch[pz - minPz, px - minPx] = sum / count;
                    }
                }

                for (var pz = minPz; pz <= maxPz; pz++)
                {
                    for (var px = minPx; px <= maxPx; px++)
                    {
                        field[pz, px] = scratch[pz - minPz, px - minPx];
                    }
                }
            }
        }

        private static void AccumulateWet(float[,] field, int px, int pz, ref float sum, ref float count)
        {
            if (pz < 0 || px < 0 || pz >= field.GetLength(0) || px >= field.GetLength(1))
            {
                return;
            }

            var v = field[pz, px];
            if (v < 0f)
            {
                return;
            }

            sum += v;
            count += 1f;
        }

        private static float MaxPositiveInRect(
            float[,] field, int minPx, int maxPx, int minPz, int maxPz)
        {
            var max = 0f;
            for (var pz = minPz; pz <= maxPz; pz++)
            {
                for (var px = minPx; px <= maxPx; px++)
                {
                    var v = field[pz, px];
                    if (v > max)
                    {
                        max = v;
                    }
                }
            }

            return max;
        }

        private static void BlurWetInteriorHeights(
            float[,] heights,
            float[,] rawDistance,
            int minPx,
            int maxPx,
            int minPz,
            int maxPz,
            int passes,
            float shorePinMeters)
        {
            var n = Mathf.Clamp(passes, 0, 16);
            var pin = Mathf.Max(shorePinMeters, 1f);
            if (n == 0)
            {
                return;
            }

            var w = maxPx - minPx + 1;
            var h = maxPz - minPz + 1;
            var scratch = new float[h, w];
            for (var pass = 0; pass < n; pass++)
            {
                for (var pz = minPz; pz <= maxPz; pz++)
                {
                    for (var px = minPx; px <= maxPx; px++)
                    {
                        var d = rawDistance[pz, px];
                        if (d < pin)
                        {
                            scratch[pz - minPz, px - minPx] = heights[pz, px];
                            continue;
                        }

                        var sum = heights[pz, px] * 4f;
                        var count = 4f;
                        for (var oz = -1; oz <= 1; oz++)
                        {
                            for (var ox = -1; ox <= 1; ox++)
                            {
                                if (ox == 0 && oz == 0)
                                {
                                    continue;
                                }

                                var nx = px + ox;
                                var nz = pz + oz;
                                if (nx < minPx || nx > maxPx || nz < minPz || nz > maxPz)
                                {
                                    continue;
                                }

                                if (rawDistance[nz, nx] < 0f)
                                {
                                    continue;
                                }

                                sum += heights[nz, nx];
                                count += 1f;
                            }
                        }

                        scratch[pz - minPz, px - minPx] = sum / count;
                    }
                }

                for (var pz = minPz; pz <= maxPz; pz++)
                {
                    for (var px = minPx; px <= maxPx; px++)
                    {
                        heights[pz, px] = scratch[pz - minPz, px - minPx];
                    }
                }
            }
        }

        private static void PinShoreBandHeights(
            float[,] heights,
            float[,] originalHeights,
            float[,] rawDistance,
            int minPx,
            int maxPx,
            int minPz,
            int maxPz,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            float waterPlaneY,
            float floorY,
            float dMaxInterior,
            float maxDepthMeters,
            WaterZone zone,
            int seed,
            LakeBathymetry.Settings profile)
        {
            var pin = Mathf.Max(profile.ShorePinMeters, 1f);
            for (var pz = minPz; pz <= maxPz; pz++)
            {
                for (var px = minPx; px <= maxPx; px++)
                {
                    var raw = rawDistance[pz, px];
                    if (raw < 0f || raw > pin)
                    {
                        continue;
                    }

                    var worldX = terrainPos.x + px * worldPerPixelX;
                    var worldZ = terrainPos.z + pz * worldPerPixelZ;
                    // Lip uses raw distance only — keep shoreline transition clean.
                    var dig = LakeBathymetry.DepthMeters(
                        raw, dMaxInterior, maxDepthMeters,
                        worldX, worldZ, zone.EquivalentRadiusMeters, seed, profile);
                    var targetY = Mathf.Max(floorY, waterPlaneY - dig);
                    WriteWorldYDigOnly(
                        heights, originalHeights, pz, px, terrainPos.y, worldHeight, targetY);
                }
            }
        }

        /// <summary>Write target Y but never above the pre-carve original (dig-only).</summary>
        private static float WriteWorldYDigOnly(
            float[,] heights,
            float[,] originalHeights,
            int pz,
            int px,
            float terrainPosY,
            float worldHeight,
            float worldY)
        {
            var before = terrainPosY + heights[pz, px] * worldHeight;
            var originalY = terrainPosY + originalHeights[pz, px] * worldHeight;
            var cappedY = Mathf.Min(worldY, originalY);
            var candidate = Mathf.Clamp01((cappedY - terrainPosY) / worldHeight);
            if (candidate >= heights[pz, px])
            {
                return 0f;
            }

            heights[pz, px] = candidate;
            var after = terrainPosY + heights[pz, px] * worldHeight;
            return before - after;
        }

        private static float ComputeWaterLevelFromHeights(
            float[,] heights,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            WaterZone zone)
        {
            var rim = new List<float>(256);
            for (var x = 0; x < zone.Width; x++)
            {
                for (var z = 0; z < zone.Depth; z++)
                {
                    if (!zone.IsFlooded(x, z))
                    {
                        continue;
                    }

                    TryAddDryRimFromHeights(
                        heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight,
                        zone, rim, x - 1, z);
                    TryAddDryRimFromHeights(
                        heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight,
                        zone, rim, x + 1, z);
                    TryAddDryRimFromHeights(
                        heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight,
                        zone, rim, x, z - 1);
                    TryAddDryRimFromHeights(
                        heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight,
                        zone, rim, x, z + 1);
                }
            }

            return SpillWaterLevel(rim, zone.WaterLevelY);
        }

        private static float SpillWaterLevel(List<float> rim, float fallbackWaterY)
        {
            var offset = Mathf.Max(
                WorldScale.WaterDepthOffsetMeters,
                WorldScale.WaterInsetDepthMeters);
            if (rim == null || rim.Count == 0)
            {
                return fallbackWaterY - Mathf.Min(offset, WaterFreeboardMeters);
            }

            // Low-rim spill (same as LakeBasinDeformer): avg sits above downhill shores.
            rim.Sort();
            var lowIdx = Mathf.Clamp(
                Mathf.FloorToInt((rim.Count - 1) * RimSpillPercentile),
                0,
                rim.Count - 1);
            return rim[lowIdx] - offset;
        }

        /// <summary>
        /// Dig-only guard: dry land in the lake pad must never sit above the pre-carve surface
        /// (no berm / crater rim). Wet pixels are already dig-capped during bathymetry writes.
        /// </summary>
        private static void ClampDryToOriginal(
            float[,] heights,
            float[,] originalHeights,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            IReadOnlyList<WaterZone> zones)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                float[,] maskSdf = null;
                if (zone.Silhouette == null)
                {
                    maskSdf = LakeShoreField.BuildSignedDistance(zone, exteriorCapMeters: BankBlendMeters + 4f);
                }

                var aabb = zone.GetFloodAabbXZ();
                var pad = Mathf.Max(BankBlendMeters + zone.CellSize * 2f, 4f);
                var minPx = Mathf.Max(0, Mathf.FloorToInt((aabb.xMin - pad - terrainPos.x) / worldPerPixelX) - 1);
                var maxPx = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.xMax + pad - terrainPos.x) / worldPerPixelX) + 1);
                var minPz = Mathf.Max(0, Mathf.FloorToInt((aabb.yMin - pad - terrainPos.z) / worldPerPixelZ) - 1);
                var maxPz = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.yMax + pad - terrainPos.z) / worldPerPixelZ) + 1);

                for (var pz = minPz; pz <= maxPz; pz++)
                {
                    for (var px = minPx; px <= maxPx; px++)
                    {
                        var worldX = terrainPos.x + px * worldPerPixelX;
                        var worldZ = terrainPos.z + pz * worldPerPixelZ;
                        var signed = zone.ShoreSignedDistance(worldX, worldZ, maskSdf);
                        if (signed >= 0f)
                        {
                            // Wet: also never above original (spikes from blur).
                            if (heights[pz, px] > originalHeights[pz, px])
                            {
                                heights[pz, px] = originalHeights[pz, px];
                            }

                            continue;
                        }

                        if (heights[pz, px] > originalHeights[pz, px])
                        {
                            heights[pz, px] = originalHeights[pz, px];
                        }
                    }
                }
            }
        }

        private static void TryAddDryRimFromHeights(
            float[,] heights,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            WaterZone zone,
            List<float> rim,
            int x,
            int z)
        {
            if (x < 0 || z < 0 || x >= zone.Width || z >= zone.Depth || zone.IsFlooded(x, z))
            {
                return;
            }

            var wx = zone.OriginX + (x + 0.5f) * zone.CellSize;
            var wz = zone.OriginZ + (z + 0.5f) * zone.CellSize;
            var px = Mathf.Clamp(Mathf.RoundToInt((wx - terrainPos.x) / worldPerPixelX), 0, resolution - 1);
            var pz = Mathf.Clamp(Mathf.RoundToInt((wz - terrainPos.z) / worldPerPixelZ), 0, resolution - 1);
            rim.Add(terrainPos.y + heights[pz, px] * worldHeight);
        }

        public static float ComputeAuthoritativeWaterLevel(UnityEngine.Terrain terrain, WaterZone zone)
        {
            var rim = new List<float>(256);
            for (var x = 0; x < zone.Width; x++)
            {
                for (var z = 0; z < zone.Depth; z++)
                {
                    if (!zone.IsFlooded(x, z))
                    {
                        continue;
                    }

                    TryAddDryRim(terrain, zone, rim, x - 1, z);
                    TryAddDryRim(terrain, zone, rim, x + 1, z);
                    TryAddDryRim(terrain, zone, rim, x, z - 1);
                    TryAddDryRim(terrain, zone, rim, x, z + 1);
                }
            }

            return SpillWaterLevel(rim, zone.WaterLevelY);
        }

        private static void TryAddDryRim(
            UnityEngine.Terrain terrain, WaterZone zone, List<float> rim, int x, int z)
        {
            if (x < 0 || z < 0 || x >= zone.Width || z >= zone.Depth || zone.IsFlooded(x, z))
            {
                return;
            }

            var wx = zone.OriginX + (x + 0.5f) * zone.CellSize;
            var wz = zone.OriginZ + (z + 0.5f) * zone.CellSize;
            rim.Add(terrain.SampleHeight(new Vector3(wx, 0f, wz)) + terrain.transform.position.y);
        }

        private static void PaintBanks(
            UnityEngine.Terrain terrain, IReadOnlyList<WaterZone> zones, TerrainLayer bankLayer)
        {
            var data = terrain.terrainData;
            var layerIndex = EnsureLayer(data, bankLayer);
            var resolution = data.alphamapResolution;
            var alphamaps = data.GetAlphamaps(0, 0, resolution, resolution);
            var layerCount = alphamaps.GetLength(2);
            var terrainPos = terrain.transform.position;
            var size = data.size;
            var worldPerPixelX = size.x / resolution;
            var worldPerPixelZ = size.z / resolution;

            foreach (var zone in zones)
            {
                float[,] maskSdf = null;
                if (zone.Silhouette == null)
                {
                    maskSdf = LakeShoreField.BuildSignedDistance(zone, exteriorCapMeters: BankBlendMeters + 4f);
                }

                var aabb = zone.GetFloodAabbXZ();
                var pad = BankBlendMeters + zone.CellSize;
                var minAx = Mathf.Max(0, Mathf.FloorToInt((aabb.xMin - pad - terrainPos.x) / worldPerPixelX));
                var maxAx = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.xMax + pad - terrainPos.x) / worldPerPixelX));
                var minAz = Mathf.Max(0, Mathf.FloorToInt((aabb.yMin - pad - terrainPos.z) / worldPerPixelZ));
                var maxAz = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.yMax + pad - terrainPos.z) / worldPerPixelZ));

                for (var z = minAz; z <= maxAz; z++)
                {
                    for (var x = minAx; x <= maxAx; x++)
                    {
                        var worldX = terrainPos.x + (x + 0.5f) * worldPerPixelX;
                        var worldZ = terrainPos.z + (z + 0.5f) * worldPerPixelZ;
                        var signed = zone.ShoreSignedDistance(worldX, worldZ, maskSdf);
                        if (signed > 5f || signed < -BankBlendMeters)
                        {
                            continue;
                        }

                        var t = signed >= 0f
                            ? 1f - signed / 5f
                            : 1f + signed / BankBlendMeters;
                        t = Mathf.Clamp01(t);
                        var target = t * t * (3f - 2f * t);
                        if (target <= 0.02f)
                        {
                            continue;
                        }

                        var current = alphamaps[z, x, layerIndex];
                        var newWeight = Mathf.Max(current, target);
                        if (newWeight <= current)
                        {
                            continue;
                        }

                        var otherSum = 1f - current;
                        var newOtherSum = 1f - newWeight;
                        var scale = otherSum > 0.0001f ? newOtherSum / otherSum : 0f;
                        for (var l = 0; l < layerCount; l++)
                        {
                            alphamaps[z, x, l] = l == layerIndex ? newWeight : alphamaps[z, x, l] * scale;
                        }
                    }
                }
            }

            data.SetAlphamaps(0, 0, alphamaps);
        }

        private static int EnsureLayer(TerrainData data, TerrainLayer layer)
        {
            var layers = data.terrainLayers;
            for (var i = 0; i < layers.Length; i++)
            {
                if (layers[i] == layer)
                {
                    return i;
                }
            }

            var expanded = new TerrainLayer[layers.Length + 1];
            layers.CopyTo(expanded, 0);
            expanded[layers.Length] = layer;
            data.terrainLayers = expanded;
            return layers.Length;
        }

        private static int HashString(string s)
        {
            unchecked
            {
                var h = 23;
                for (var i = 0; i < s.Length; i++)
                {
                    h = h * 31 + s[i];
                }

                return h & 0x7fffffff;
            }
        }
    }
}
