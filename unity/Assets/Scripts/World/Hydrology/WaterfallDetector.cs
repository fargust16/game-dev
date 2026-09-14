using System.Collections.Generic;

namespace GameDev.World.Hydrology
{
    /// <summary>
    /// Reads waterfalls out of the river geometry that already exists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The chain is <c>river segment -&gt; elevation profile -&gt; significant local drop -&gt;
    /// candidate</c>, and that is the entire mechanism. Nothing is scattered, nothing is placed and
    /// no separate pass decides where a waterfall "should" be — if the bed falls far enough over a
    /// short enough run, there is a waterfall there, and if it does not, there is not.
    /// </para>
    /// <para>
    /// The profile read is <see cref="RiverNode.BedElevation"/>, not the pre-carve ground: the bed is
    /// what the water will actually fall down once the terrain is carved. The carve depth varies
    /// smoothly with contributing area, so it shifts the profile without inventing or erasing steps.
    /// </para>
    /// <para>
    /// The MVP derives candidates and renders nothing. Rendering later needs no new analysis, which
    /// is the point of doing this now rather than leaving it to a future decorative system.
    /// </para>
    /// </remarks>
    public static class WaterfallDetector
    {
        public static List<WaterfallCandidate> Detect(
            IReadOnlyList<RiverReach> reaches, HydrologySettings settings)
        {
            var candidates = new List<WaterfallCandidate>();
            if (reaches == null)
            {
                return candidates;
            }

            for (var r = 0; r < reaches.Count; r++)
            {
                DetectInReach(reaches[r], settings, candidates);
            }

            return candidates;
        }

        private static void DetectInReach(
            RiverReach reach, HydrologySettings settings, List<WaterfallCandidate> candidates)
        {
            var nodes = reach?.Nodes;
            if (nodes == null || nodes.Count < 2)
            {
                return;
            }

            var i = 0;
            while (i < nodes.Count - 1)
            {
                var start = nodes[i];
                var bestDrop = 0f;
                var bestRun = 0f;
                var bestSlope = 0f;
                var bestEnd = -1;

                // Grow the window while it still counts as "local", and keep the <b>steepest</b> one
                // rather than the one with the largest fall. Taking the largest fall would swallow
                // the gentle run below a drop into the same candidate and report a 17 m cascade
                // where the terrain has a 16 m cliff followed by a slope.
                for (var j = i + 1; j < nodes.Count; j++)
                {
                    var run = nodes[j].CumulativeDistance - start.CumulativeDistance;
                    if (run > settings.WaterfallMaxRunMeters)
                    {
                        break;
                    }

                    var drop = start.BedElevation - nodes[j].BedElevation;
                    if (drop < settings.WaterfallMinDropMeters || run <= 0.001f)
                    {
                        continue;
                    }

                    // On a tie the longer window wins, so a uniform cascade is reported as one
                    // waterfall instead of one per step down it.
                    var slope = drop / run;
                    if (slope > bestSlope + 1e-4f
                        || (slope > bestSlope - 1e-4f && drop > bestDrop))
                    {
                        bestSlope = slope;
                        bestDrop = drop;
                        bestRun = run;
                        bestEnd = j;
                    }
                }

                if (bestEnd < 0)
                {
                    i++;
                    continue;
                }

                candidates.Add(new WaterfallCandidate(
                    reach.Id, i, start.PositionXZ, bestDrop, bestRun));

                // Resume past the fall, so one drop cannot be reported again from inside itself.
                i = bestEnd;
            }
        }
    }
}
