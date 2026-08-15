using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Roads
{
    /// <summary>
    /// Distance to ordered road clearance polylines. Point-stamps create scalloped edges when
    /// sample spacing is large vs road width; segment distance keeps soft continuous shoulders.
    /// </summary>
    public static class RoadSampleDistance
    {
        /// <summary>
        /// Max gap between consecutive samples still treated as one segment.
        /// Larger jumps start a new run (multi-road clearance lists).
        /// </summary>
        public const float MaxSegmentGapMeters = 12f;

        public static float NearestMeters(Vector2 point, IReadOnlyList<Vector2> samples)
        {
            if (samples == null || samples.Count == 0)
            {
                return float.PositiveInfinity;
            }

            if (samples.Count == 1)
            {
                return Vector2.Distance(point, samples[0]);
            }

            var best = float.PositiveInfinity;
            for (var i = 0; i < samples.Count - 1; i++)
            {
                var a = samples[i];
                var b = samples[i + 1];
                var gap = Vector2.Distance(a, b);
                if (gap > MaxSegmentGapMeters || gap < 1e-4f)
                {
                    best = Mathf.Min(best, Vector2.Distance(point, a));
                    continue;
                }

                best = Mathf.Min(best, PointToSegmentMeters(point, a, b));
            }

            best = Mathf.Min(best, Vector2.Distance(point, samples[samples.Count - 1]));
            return best;
        }

        public static float PointToSegmentMeters(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var lenSq = ab.sqrMagnitude;
            if (lenSq < 1e-8f)
            {
                return Vector2.Distance(p, a);
            }

            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
            var closest = a + ab * t;
            return Vector2.Distance(p, closest);
        }
    }
}
