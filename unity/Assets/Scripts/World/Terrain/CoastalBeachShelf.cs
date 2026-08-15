using GameDev.World;
using GameDev.World.Seed;
using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// Coastal height transition: gentle ≤12° sea→land ramp on most of the rim.
    /// Cliff azimuth may stay steeper for mountain coasts. Never a flat terrace ring.
    /// </summary>
    public static class CoastalBeachShelf
    {
        public const float TargetBeachSlopeDegrees = 12f;
        public const float CoastTransitionMaxMeters = 480f;
        public const float MinBeachWidthMeters = 200f;

        /// <summary>Soft prior half-width (degrees). Not a hard carve gate.</summary>
        public const float BeachArcHalfDegrees = 130f;

        public const float MaxCoastInfluence = 0.98f;

        /// <summary>Above this ASL near the water, cliff-arc cells may keep steep rock.</summary>
        public const float CliffPreserveAboveSeaMeters = 45f;

        private const string CliffYawStream = "terrain.coast.cliff";
        private const string WidthStream = "terrain.coast.width";
        private const float FlattenHeightEpsilonMeters = 0.04f;

        public static float SampleCliffCoastYawRadians(int worldSeed)
        {
            return (float)new WorldSeed(worldSeed).Fork(CliffYawStream).NextDouble() * Mathf.PI * 2f;
        }

        public static float BeachYawDegreesFromCliffRadians(float cliffYawRadians)
        {
            return Mathf.DeltaAngle(0f, cliffYawRadians * Mathf.Rad2Deg + 180f);
        }

        public static float SampleBeachYawDegrees(int worldSeed)
        {
            return BeachYawDegreesFromCliffRadians(SampleCliffCoastYawRadians(worldSeed));
        }

        /// <summary>1 at beach azimuth center → 0 outside arc. Always in [0,1].</summary>
        public static float BeachArcWeight(float yawDegrees, float beachYawDegrees)
        {
            var arcDelta = Mathf.Abs(Mathf.DeltaAngle(yawDegrees, beachYawDegrees));
            if (arcDelta >= BeachArcHalfDegrees)
                return 0f;
            var fadeStart = BeachArcHalfDegrees * 0.45f;
            var w = 1f - Mathf.SmoothStep(fadeStart, BeachArcHalfDegrees, arcDelta);
            return Mathf.Clamp01(w);
        }

        public static float SmootherStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        public static void ApplyAll(UnityEngine.Terrain[] terrains, int worldSeed)
        {
            if (terrains == null || terrains.Length == 0)
                return;

            var cliffYaw = SampleCliffCoastYawRadians(worldSeed);
            var beachYawDeg = BeachYawDegreesFromCliffRadians(cliffYaw);
            var widthRng = new WorldSeed(worldSeed).Fork(WidthStream);

            var cells = 0;
            var nearSea = 0;
            var nearSeaInArc = 0;
            var beachCells = 0;
            var cliffCells = 0;
            var drySand = 0;
            var widthSum = 0f;
            var slopeSum = 0f;
            var slopeMax = 0f;
            var slopeSamples = 0;
            var flattenedCells = 0;
            var constantHeightRuns = 0;

            const int gridN = 8;
            var widthGrid = new float[gridN, gridN];
            for (var gz = 0; gz < gridN; gz++)
            {
                for (var gx = 0; gx < gridN; gx++)
                    widthGrid[gz, gx] = 0.8f + 0.4f * (float)widthRng.NextDouble();
            }

            for (var i = 0; i < terrains.Length; i++)
            {
                if (terrains[i] == null)
                    continue;
                cells += ApplyTile(
                    terrains[i],
                    beachYawDeg,
                    widthGrid,
                    ref nearSea,
                    ref nearSeaInArc,
                    ref beachCells,
                    ref cliffCells,
                    ref drySand,
                    ref widthSum,
                    ref slopeSum,
                    ref slopeMax,
                    ref slopeSamples,
                    ref flattenedCells,
                    ref constantHeightRuns);
            }

            var avgWidth = beachCells > 0 ? widthSum / beachCells : 0f;
            var avgSlope = slopeSamples > 0 ? slopeSum / slopeSamples : 0f;
            Debug.Log(
                $"[BeachShelf] DirectedCoast beachYaw={beachYawDeg:F0}° " +
                $"cliffYaw={cliffYaw * Mathf.Rad2Deg:F0}° maxDist={CoastTransitionMaxMeters:F0}m " +
                $"targetSlope≤{TargetBeachSlopeDegrees:F0}° plateauSoft sea={WorldScale.SeaLevelMeters:F0} " +
                $"nearSea={nearSea} nearSeaInArc={nearSeaInArc} cells={cells} " +
                $"drySand={drySand} beachCells={beachCells} cliffSkip={cliffCells} " +
                $"averageBeachWidth={avgWidth:F1} averageBeachSlope={avgSlope:F1} " +
                $"maxBeachSlope={slopeMax:F1} " +
                $"numberOfFlattenedCells={flattenedCells} numberOfConstantHeightRuns={constantHeightRuns}");
        }

        private static int ApplyTile(
            UnityEngine.Terrain terrain,
            float beachYawDegrees,
            float[,] widthGrid,
            ref int nearSeaTotal,
            ref int nearSeaInArcTotal,
            ref int beachCellsTotal,
            ref int cliffCellsTotal,
            ref int drySandTotal,
            ref float widthSum,
            ref float slopeSum,
            ref float slopeMax,
            ref int slopeSamples,
            ref int flattenedTotal,
            ref int constantRunsTotal)
        {
            var data = terrain.terrainData;
            if (data == null)
                return 0;

            var res = data.heightmapResolution;
            var heights = data.GetHeights(0, 0, res, res);
            var original = (float[,])heights.Clone();
            var size = data.size;
            var origin = terrain.GetPosition();
            var sizeY = Mathf.Max(0.01f, size.y);
            var terrainBaseY = origin.y;
            var sea01 = Mathf.Clamp01((WorldScale.SeaLevelMeters - terrainBaseY) / sizeY);
            var cx = WorldScale.WorldExtentXMeters * 0.5f;
            var cz = WorldScale.WorldExtentZMeters * 0.5f;
            var cellMeters = Mathf.Max(0.25f, 0.5f * (size.x + size.z) / Mathf.Max(1, res - 1));
            var maxDistCells = Mathf.Max(4, Mathf.CeilToInt(CoastTransitionMaxMeters / cellMeters) + 2);
            var tanTarget = Mathf.Tan(TargetBeachSlopeDegrees * Mathf.Deg2Rad);

            var dist = BuildFloodDist(heights, res, maxDistCells, h => h <= sea01);

            var carved = 0;
            var nearSea = 0;
            var nearSeaInArc = 0;
            var beachCells = 0;
            var cliffCells = 0;
            var drySand = 0;
            var loggedSample = false;
            var gridN = widthGrid.GetLength(0);

            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    var dCells = dist[z, x];
                    if (dCells < 1 || dCells > maxDistCells)
                        continue;

                    var h = heights[z, x];
                    if (h <= sea01)
                        continue;

                    var dMeters = dCells * cellMeters;
                    if (dMeters > CoastTransitionMaxMeters)
                        continue;

                    nearSea++;

                    var worldX = origin.x + (x / (float)(res - 1)) * size.x;
                    var worldZ = origin.z + (z / (float)(res - 1)) * size.z;
                    var yawDeg = Mathf.Atan2(worldZ - cz, worldX - cx) * Mathf.Rad2Deg;
                    var arcW = BeachArcWeight(yawDeg, beachYawDegrees);
                    if (arcW > 0.02f)
                        nearSeaInArc++;

                    var heightAboveSea = (h - sea01) * sizeY;
                    if (heightAboveSea < 0.12f)
                        continue;

                    // Soft prior: beach side full strength; opposite side still softens low shelves.
                    // Only skip true mountain cliff: high + weak beach prior.
                    var isCliffSkip = arcW < 0.12f && heightAboveSea > CliffPreserveAboveSeaMeters;
                    if (isCliffSkip)
                    {
                        cliffCells++;
                        continue;
                    }

                    var widthForSlope = heightAboveSea / Mathf.Max(1e-4f, tanTarget);
                    var widthMul = SampleWidthField(widthGrid, gridN, worldX, worldZ);
                    var localWidth = Mathf.Max(MinBeachWidthMeters, widthForSlope) * widthMul;
                    localWidth = Mathf.Clamp(localWidth, MinBeachWidthMeters, CoastTransitionMaxMeters);

                    if (dMeters > localWidth)
                        continue;

                    var t = SmootherStep(dMeters / Mathf.Max(1f, localWidth));
                    var rampAsl = WorldScale.SeaLevelMeters + dMeters * tanTarget;
                    var ramp01 = Mathf.Clamp01((rampAsl - terrainBaseY) / sizeY);
                    var coastProfile = Mathf.Lerp(ramp01, h, t);
                    var target = Mathf.Min(h, coastProfile);

                    // Floor strength so opposite-azimuth low shelves still get a gentle ramp.
                    var strength = Mathf.Lerp(0.55f, 1f, arcW);
                    var influence = strength * MaxCoastInfluence * (1f - 0.3f * t);
                    target = Mathf.Lerp(h, target, influence);

                    if (target > h)
                        target = h;
                    if (Mathf.Abs(target - h) * sizeY < FlattenHeightEpsilonMeters)
                        continue;

                    heights[z, x] = target;
                    carved++;
                    beachCells++;
                    widthSum += localWidth;

                    var slopeDeg = Mathf.Atan2(Mathf.Max(0.01f, (target - sea01) * sizeY), dMeters) *
                                   Mathf.Rad2Deg;
                    slopeSum += slopeDeg;
                    slopeSamples++;
                    if (slopeDeg > slopeMax)
                        slopeMax = slopeDeg;

                    var asl = terrainBaseY + target * sizeY;
                    if (asl <= WorldScale.SeaLevelMeters + 8f && asl > WorldScale.SeaLevelMeters)
                        drySand++;

                    if (!loggedSample)
                    {
                        loggedSample = true;
                        Debug.Log(
                            $"[BeachShelf] write sample tile={terrain.name} " +
                            $"asl {terrainBaseY + h * sizeY:F1}m → {asl:F1}m " +
                            $"d={dMeters:F0}m width={localWidth:F0}m arcW={arcW:F2} " +
                            $"slope~{slopeDeg:F1}° yaw={yawDeg:F0}°");
                    }
                }
            }

            CountFlattenArtifacts(
                heights, original, dist, res, sea01, out var flatCells, out var runs);

            // Integer flood rings create terrace steps → sawtooth rock faces.
            // Soften only the coast band (not mountain interiors).
            if (carved > 0)
                SmoothCoastBand(heights, dist, res, sea01, maxDistCells, passes: 4);

            nearSeaTotal += nearSea;
            nearSeaInArcTotal += nearSeaInArc;
            beachCellsTotal += beachCells;
            cliffCellsTotal += cliffCells;
            drySandTotal += drySand;
            flattenedTotal += flatCells;
            constantRunsTotal += runs;

            if (carved > 0)
            {
                data.SetHeights(0, 0, heights);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(data);
#endif
                var collider = terrain.GetComponent<TerrainCollider>();
                if (collider != null)
                    collider.terrainData = data;
            }

            return carved;
        }

        /// <summary>
        /// Box-smooth heights in the sea-adjacent band to erase flood-ring terraces.
        /// </summary>
        private static void SmoothCoastBand(
            float[,] heights,
            int[,] dist,
            int res,
            float sea01,
            int maxDistCells,
            int passes)
        {
            var tmp = new float[res, res];
            for (var p = 0; p < passes; p++)
            {
                for (var z = 0; z < res; z++)
                {
                    for (var x = 0; x < res; x++)
                    {
                        var d = dist[z, x];
                        if (d < 1 || d > maxDistCells || heights[z, x] <= sea01)
                        {
                            tmp[z, x] = heights[z, x];
                            continue;
                        }

                        var acc = 0f;
                        var w = 0f;
                        for (var dz = -1; dz <= 1; dz++)
                        {
                            for (var dx = -1; dx <= 1; dx++)
                            {
                                var nz = z + dz;
                                var nx = x + dx;
                                if (nz < 0 || nx < 0 || nz >= res || nx >= res)
                                    continue;
                                var hN = heights[nz, nx];
                                // Don't pull coast into deep ocean zeros.
                                if (hN <= sea01 * 0.5f && (dx != 0 || dz != 0))
                                    continue;
                                var k = (dx == 0 && dz == 0) ? 2.5f : 1f;
                                acc += hN * k;
                                w += k;
                            }
                        }

                        tmp[z, x] = w > 1e-5f ? acc / w : heights[z, x];
                    }
                }

                for (var z = 0; z < res; z++)
                {
                    for (var x = 0; x < res; x++)
                        heights[z, x] = tmp[z, x];
                }
            }
        }

        private static float SampleWidthField(float[,] grid, int n, float worldX, float worldZ)
        {
            var u = Mathf.Clamp01(worldX / Mathf.Max(1f, WorldScale.WorldExtentXMeters));
            var v = Mathf.Clamp01(worldZ / Mathf.Max(1f, WorldScale.WorldExtentZMeters));
            var fx = u * (n - 1);
            var fz = v * (n - 1);
            var x0 = Mathf.FloorToInt(fx);
            var z0 = Mathf.FloorToInt(fz);
            var x1 = Mathf.Min(n - 1, x0 + 1);
            var z1 = Mathf.Min(n - 1, z0 + 1);
            var tx = fx - x0;
            var tz = fz - z0;
            var a = Mathf.Lerp(grid[z0, x0], grid[z0, x1], tx);
            var b = Mathf.Lerp(grid[z1, x0], grid[z1, x1], tx);
            return Mathf.Lerp(a, b, tz);
        }

        private static void CountFlattenArtifacts(
            float[,] heights,
            float[,] original,
            int[,] dist,
            int res,
            float sea01,
            out int flattenedCells,
            out int constantHeightRuns)
        {
            flattenedCells = 0;
            constantHeightRuns = 0;
            const float eps = 0.00012f;

            for (var z = 0; z < res; z++)
            {
                var run = 0;
                var runH = -1f;
                for (var x = 0; x < res; x++)
                {
                    var d = dist[z, x];
                    if (d < 1 || heights[z, x] <= sea01)
                    {
                        if (run >= 16)
                            constantHeightRuns++;
                        run = 0;
                        runH = -1f;
                        continue;
                    }

                    var h = heights[z, x];
                    if (Mathf.Abs(h - original[z, x]) > eps)
                    {
                        var flatN = 0;
                        for (var dz = -1; dz <= 1; dz++)
                        {
                            for (var dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dz == 0)
                                    continue;
                                var nz = z + dz;
                                var nx = x + dx;
                                if (nz < 0 || nx < 0 || nz >= res || nx >= res)
                                    continue;
                                if (Mathf.Abs(heights[nz, nx] - h) < eps)
                                    flatN++;
                            }
                        }

                        if (flatN >= 6)
                            flattenedCells++;
                    }

                    if (runH < 0f || Mathf.Abs(h - runH) < eps)
                    {
                        run++;
                        runH = h;
                    }
                    else
                    {
                        if (run >= 16)
                            constantHeightRuns++;
                        run = 1;
                        runH = h;
                    }
                }

                if (run >= 16)
                    constantHeightRuns++;
            }
        }

        private static int[,] BuildFloodDist(
            float[,] heights,
            int res,
            int maxDistCells,
            System.Func<float, bool> isSeed)
        {
            var dist = new int[res, res];
            var queue = new int[res * res];
            var head = 0;
            var tail = 0;
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    if (isSeed(heights[z, x]))
                    {
                        dist[z, x] = 0;
                        queue[tail++] = (z << 16) | x;
                    }
                    else
                        dist[z, x] = -1;
                }
            }

            while (head < tail)
            {
                var packed = queue[head++];
                var z = packed >> 16;
                var x = packed & 0xFFFF;
                var d = dist[z, x];
                if (d >= maxDistCells)
                    continue;
                Enqueue(z - 1, x, d + 1);
                Enqueue(z + 1, x, d + 1);
                Enqueue(z, x - 1, d + 1);
                Enqueue(z, x + 1, d + 1);
            }

            void Enqueue(int nz, int nx, int d)
            {
                if (nz < 0 || nx < 0 || nz >= res || nx >= res)
                    return;
                if (dist[nz, nx] != -1)
                    return;
                dist[nz, nx] = d;
                queue[tail++] = (nz << 16) | nx;
            }

            return dist;
        }
    }
}
