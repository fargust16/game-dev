using System.Collections.Generic;
using UnityEngine;
using WorldScale = GameDev.World.WorldScale;

namespace GameDev.World.Roads
{
    /// <summary>
    /// Road corridor definition: centerline + vertical profile + blend widths.
    /// Terrain conforms to the road; the road does not canyon-cut the terrain.
    /// </summary>
    public sealed class RoadDefinition
    {
        public IReadOnlyList<Vector2> CenterlineXZ { get; }
        public IReadOnlyList<float> ProfileY { get; }
        public float RoadWidthMeters { get; }
        public float ShoulderMeters { get; }
        public float BlendMeters { get; }
        public RoadTier Tier { get; }

        public RoadDefinition(
            IReadOnlyList<Vector2> centerlineXZ,
            IReadOnlyList<float> profileY,
            float roadWidthMeters,
            float shoulderMeters,
            float blendMeters,
            RoadTier tier)
        {
            CenterlineXZ = centerlineXZ;
            ProfileY = profileY;
            RoadWidthMeters = Mathf.Max(1f, roadWidthMeters);
            ShoulderMeters = Mathf.Max(0f, shoulderMeters);
            BlendMeters = Mathf.Max(0.5f, blendMeters);
            Tier = tier;
        }

        public float HalfCorridorMeters =>
            RoadWidthMeters * 0.5f + ShoulderMeters + BlendMeters;
    }

    /// <summary>
    /// Smooths a sampled terrain height polyline into a driveable vertical profile
    /// (max grade + max grade-change).
    /// </summary>
    public static class RoadVerticalProfile
    {
        public static List<float> Build(
            IReadOnlyList<Vector2> centerlineXZ,
            System.Func<float, float, float> sampleTerrainY,
            float maxGrade = -1f,
            float maxGradeChange = -1f)
        {
            var maxG = maxGrade > 0f ? maxGrade : WorldScale.RoadMaxGrade;
            var maxDg = maxGradeChange > 0f ? maxGradeChange : WorldScale.RoadMaxGradeChange;
            var n = centerlineXZ != null ? centerlineXZ.Count : 0;
            var y = new List<float>(n);
            if (n == 0)
            {
                return y;
            }

            for (var i = 0; i < n; i++)
            {
                var p = centerlineXZ[i];
                y.Add(sampleTerrainY(p.x, p.y));
            }

            if (n < 2)
            {
                return y;
            }

            // Forward pass — clamp grade.
            for (var i = 1; i < n; i++)
            {
                var dist = Mathf.Max(Vector2.Distance(centerlineXZ[i - 1], centerlineXZ[i]), 0.01f);
                var maxDy = maxG * dist;
                var dy = y[i] - y[i - 1];
                if (dy > maxDy)
                {
                    y[i] = y[i - 1] + maxDy;
                }
                else if (dy < -maxDy)
                {
                    y[i] = y[i - 1] - maxDy;
                }
            }

            // Backward pass — keep ends consistent.
            for (var i = n - 2; i >= 0; i--)
            {
                var dist = Mathf.Max(Vector2.Distance(centerlineXZ[i], centerlineXZ[i + 1]), 0.01f);
                var maxDy = maxG * dist;
                var dy = y[i] - y[i + 1];
                if (dy > maxDy)
                {
                    y[i] = y[i + 1] + maxDy;
                }
                else if (dy < -maxDy)
                {
                    y[i] = y[i + 1] - maxDy;
                }
            }

            // Limit grade change (vertical curvature).
            if (n >= 3)
            {
                for (var pass = 0; pass < 2; pass++)
                {
                    for (var i = 1; i < n - 1; i++)
                    {
                        var d0 = Mathf.Max(Vector2.Distance(centerlineXZ[i - 1], centerlineXZ[i]), 0.01f);
                        var d1 = Mathf.Max(Vector2.Distance(centerlineXZ[i], centerlineXZ[i + 1]), 0.01f);
                        var g0 = (y[i] - y[i - 1]) / d0;
                        var g1 = (y[i + 1] - y[i]) / d1;
                        var dg = g1 - g0;
                        if (Mathf.Abs(dg) <= maxDg)
                        {
                            continue;
                        }

                        var targetDg = Mathf.Clamp(dg, -maxDg, maxDg);
                        var mid = (g0 + g1) * 0.5f;
                        var g0n = mid - targetDg * 0.5f;
                        var g1n = mid + targetDg * 0.5f;
                        y[i] = 0.5f * ((y[i - 1] + g0n * d0) + (y[i + 1] - g1n * d1));
                    }
                }
            }

            return y;
        }

        /// <summary>True if profile stays within cut/fill caps vs sampled terrain.</summary>
        public static bool RespectsEarthworkCaps(
            IReadOnlyList<Vector2> centerlineXZ,
            IReadOnlyList<float> profileY,
            System.Func<float, float, float> sampleTerrainY,
            float maxCut = -1f,
            float maxFill = -1f)
        {
            var cutCap = maxCut > 0f ? maxCut : WorldScale.RoadMaxCutDepthMeters;
            var fillCap = maxFill > 0f ? maxFill : WorldScale.RoadMaxFillHeightMeters;
            var n = Mathf.Min(centerlineXZ.Count, profileY.Count);
            for (var i = 0; i < n; i++)
            {
                var terrain = sampleTerrainY(centerlineXZ[i].x, centerlineXZ[i].y);
                var cut = Mathf.Max(0f, terrain - profileY[i]);
                var fill = Mathf.Max(0f, profileY[i] - terrain);
                if (cut > cutCap + 0.05f || fill > fillCap + 0.05f)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
