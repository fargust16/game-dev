using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Roads
{
    /// <summary>
    /// Resamples a road centerline at a fixed step interval along its polyline. The pathfinding
    /// grid's own points are ~25-35m apart (RoadGridSpec.CellSize and its diagonal), which is fine
    /// for finding a route but too coarse to render: a straight ribbon between two points that far
    /// apart cuts through any dip or rise the terrain has in between, leaving the road floating over
    /// hollows and clipping through bumps. Densifying first means the caller can re-sample real
    /// ground height at every dense point (RoadNetworkMenu does this via raycast), so the rendered
    /// road actually follows the terrain's undulation instead of the sparse waypoints' straight-line
    /// shortcuts between them.
    /// </summary>
    public static class RoadPathDensifier
    {
        public static List<Vector2> Densify(IReadOnlyList<Vector2> coarsePoints, float stepMeters)
        {
            var result = new List<Vector2>();
            if (coarsePoints.Count == 0)
            {
                return result;
            }

            result.Add(coarsePoints[0]);
            if (coarsePoints.Count == 1)
            {
                return result;
            }

            var step = Mathf.Max(stepMeters, 0.01f);

            for (var i = 1; i < coarsePoints.Count; i++)
            {
                var segmentStart = coarsePoints[i - 1];
                var segmentEnd = coarsePoints[i];
                var segmentLength = Vector2.Distance(segmentStart, segmentEnd);
                if (segmentLength < 0.0001f)
                {
                    continue;
                }

                var stepsInSegment = Mathf.Max(1, Mathf.RoundToInt(segmentLength / step));
                for (var s = 1; s <= stepsInSegment; s++)
                {
                    var t = (float)s / stepsInSegment;
                    result.Add(Vector2.Lerp(segmentStart, segmentEnd, t));
                }
            }

            return result;
        }
    }
}
