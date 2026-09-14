using System;
using UnityEngine;

namespace GameDev.World.Hydrology
{
    /// <summary>
    /// Turns the river network into the depth field the terrain bake subtracts from the relief.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the bridge between "where the water is" and "what the ground looks like". The output
    /// is metres of carve per pixel over one rectangle of the world; the terrain bake subtracts it
    /// from its lattice. No terrain data is touched here and no heightmap is written.
    /// </para>
    /// <para>
    /// The centre depth comes from each node's own <see cref="RiverNode.BedElevation"/>, not from a
    /// formula re-derived here. That is what keeps the carved ground, the planned bed and the water
    /// surface describing the same river: change how the bed is decided and the carve follows,
    /// with no second definition to keep in step.
    /// </para>
    /// </remarks>
    public static class HydrologyCarveField
    {
        /// <summary>
        /// Rasterises the carve over <paramref name="worldRect"/> at <paramref name="resolution"/>.
        /// </summary>
        /// <param name="groundAt">
        /// World-space ground height before the carve, used to turn a bed elevation into a depth.
        /// </param>
        /// <returns>Carve depth in metres, row-major, one value per pixel.</returns>
        public static float[] Rasterize(
            HydrologyDefinition definition,
            Rect worldRect,
            int resolution,
            Func<float, float, float> groundAt)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (groundAt == null)
            {
                throw new ArgumentNullException(nameof(groundAt));
            }

            if (resolution < 2)
            {
                throw new ArgumentException($"Resolution must be at least 2, got {resolution}.");
            }

            var carve = new float[resolution * resolution];
            var pixelSizeX = worldRect.width / resolution;
            var pixelSizeZ = worldRect.height / resolution;
            var settings = definition.Settings;

            for (var r = 0; r < definition.Reaches.Count; r++)
            {
                RasterizeReach(
                    definition.Reaches[r], carve, worldRect, resolution,
                    pixelSizeX, pixelSizeZ, settings, groundAt);
            }

            SuppressInsideLakes(definition, carve, worldRect, resolution, pixelSizeX, pixelSizeZ);

            var ceiling = settings.MaxCarveDepthMeters;
            for (var i = 0; i < carve.Length; i++)
            {
                if (carve[i] > ceiling)
                {
                    carve[i] = ceiling;
                }
            }

            return carve;
        }

        /// <summary>
        /// Draws one reach, segment by segment, into the pixels its valley can actually reach.
        /// </summary>
        /// <remarks>
        /// Per-segment bounding boxes rather than a distance transform over the whole tile: a river
        /// covers a tiny fraction of the world, and iterating every pixel against every segment would
        /// cost billions of operations for a field that is zero almost everywhere.
        /// </remarks>
        private static void RasterizeReach(
            RiverReach reach,
            float[] carve,
            Rect worldRect,
            int resolution,
            float pixelSizeX,
            float pixelSizeZ,
            HydrologySettings settings,
            Func<float, float, float> groundAt)
        {
            var nodes = reach.Nodes;
            for (var i = 1; i < nodes.Count; i++)
            {
                var a = nodes[i - 1];
                var b = nodes[i];

                var area = Mathf.Max(a.ContributingAreaSqm, b.ContributingAreaSqm);
                var valleyHalfWidth = settings.ValleyHalfWidth(area);
                var channelHalfWidth = settings.ChannelHalfWidth(area);
                var channelDepth = settings.ChannelDepth(area);

                var depthA = Mathf.Max(0f, groundAt(a.PositionXZ.x, a.PositionXZ.y) - a.BedElevation);
                var depthB = Mathf.Max(0f, groundAt(b.PositionXZ.x, b.PositionXZ.y) - b.BedElevation);
                if (depthA <= 0f && depthB <= 0f)
                {
                    continue;
                }

                var minX = Mathf.Min(a.PositionXZ.x, b.PositionXZ.x) - valleyHalfWidth;
                var maxX = Mathf.Max(a.PositionXZ.x, b.PositionXZ.x) + valleyHalfWidth;
                var minZ = Mathf.Min(a.PositionXZ.y, b.PositionXZ.y) - valleyHalfWidth;
                var maxZ = Mathf.Max(a.PositionXZ.y, b.PositionXZ.y) + valleyHalfWidth;

                var px0 = Mathf.Max(0, Mathf.FloorToInt((minX - worldRect.xMin) / pixelSizeX));
                var px1 = Mathf.Min(resolution - 1, Mathf.CeilToInt((maxX - worldRect.xMin) / pixelSizeX));
                var pz0 = Mathf.Max(0, Mathf.FloorToInt((minZ - worldRect.yMin) / pixelSizeZ));
                var pz1 = Mathf.Min(resolution - 1, Mathf.CeilToInt((maxZ - worldRect.yMin) / pixelSizeZ));
                if (px0 > px1 || pz0 > pz1)
                {
                    continue;
                }

                for (var pz = pz0; pz <= pz1; pz++)
                {
                    var worldZ = worldRect.yMin + (pz + 0.5f) * pixelSizeZ;
                    var row = pz * resolution;
                    for (var px = px0; px <= px1; px++)
                    {
                        var worldX = worldRect.xMin + (px + 0.5f) * pixelSizeX;
                        var point = new Vector2(worldX, worldZ);

                        var t = ProjectOnSegment(point, a.PositionXZ, b.PositionXZ);
                        var distance = Vector2.Distance(
                            point, Vector2.Lerp(a.PositionXZ, b.PositionXZ, t));
                        if (distance >= valleyHalfWidth)
                        {
                            continue;
                        }

                        var centreDepth = Mathf.Lerp(depthA, depthB, t);
                        if (centreDepth <= 0f)
                        {
                            continue;
                        }

                        // Split so the cross-section keeps its two scales: a broad valley the eye
                        // reads as landform, with a narrow bed cut into it. Scaling one profile by
                        // the total depth would give a single smooth trough and lose the channel.
                        var channelPart = Mathf.Min(centreDepth, channelDepth);
                        var valleyPart = centreDepth - channelPart;

                        var depth = valleyPart * ValleyProfile(distance / valleyHalfWidth)
                            + channelPart * ChannelProfile(distance / channelHalfWidth);

                        var index = row + px;
                        if (depth > carve[index])
                        {
                            // Deepest wins, never the sum. Two valleys crossing at a confluence make
                            // one valley of the deeper profile; adding them would gouge a pit exactly
                            // where the river network is most visible.
                            carve[index] = depth;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Zeroes the carve inside lake outlines.
        /// </summary>
        /// <remarks>
        /// A lake is a depression the terrain already has, and its water level was measured from that
        /// terrain. Letting a valley shoulder reach in and deepen or breach the basin would move the
        /// surface out from under the water plane the same analysis produced.
        /// </remarks>
        private static void SuppressInsideLakes(
            HydrologyDefinition definition,
            float[] carve,
            Rect worldRect,
            int resolution,
            float pixelSizeX,
            float pixelSizeZ)
        {
            for (var l = 0; l < definition.Lakes.Count; l++)
            {
                var boundary = definition.Lakes[l].Boundary;
                if (boundary == null || boundary.Count < 3)
                {
                    continue;
                }

                var minX = float.MaxValue;
                var maxX = float.MinValue;
                var minZ = float.MaxValue;
                var maxZ = float.MinValue;
                for (var v = 0; v < boundary.Count; v++)
                {
                    minX = Mathf.Min(minX, boundary[v].x);
                    maxX = Mathf.Max(maxX, boundary[v].x);
                    minZ = Mathf.Min(minZ, boundary[v].y);
                    maxZ = Mathf.Max(maxZ, boundary[v].y);
                }

                var px0 = Mathf.Max(0, Mathf.FloorToInt((minX - worldRect.xMin) / pixelSizeX));
                var px1 = Mathf.Min(resolution - 1, Mathf.CeilToInt((maxX - worldRect.xMin) / pixelSizeX));
                var pz0 = Mathf.Max(0, Mathf.FloorToInt((minZ - worldRect.yMin) / pixelSizeZ));
                var pz1 = Mathf.Min(resolution - 1, Mathf.CeilToInt((maxZ - worldRect.yMin) / pixelSizeZ));

                for (var pz = pz0; pz <= pz1; pz++)
                {
                    var worldZ = worldRect.yMin + (pz + 0.5f) * pixelSizeZ;
                    var row = pz * resolution;
                    for (var px = px0; px <= px1; px++)
                    {
                        var worldX = worldRect.xMin + (px + 0.5f) * pixelSizeX;
                        if (ContainsPoint(boundary, worldX, worldZ))
                        {
                            carve[row + px] = 0f;
                        }
                    }
                }
            }
        }

        /// <summary>Even-odd ray crossing test against a closed ring.</summary>
        private static bool ContainsPoint(
            System.Collections.Generic.IReadOnlyList<Vector2> polygon, float x, float z)
        {
            var inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if (a.y > z != b.y > z
                    && x < (b.x - a.x) * (z - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static float ProjectOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var lengthSq = ab.sqrMagnitude;
            return lengthSq < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSq);
        }

        /// <summary>Broad trough that meets the surrounding ground with zero slope.</summary>
        /// <remarks>
        /// The zero slope at the rim is the point: a linear taper leaves a visible crease all the way
        /// along both sides of every river, which reads as a trench cut into the landscape rather
        /// than as a valley the landscape grew.
        /// </remarks>
        private static float ValleyProfile(float t)
        {
            var d = 1f - Mathf.Clamp01(t);
            return d * d;
        }

        /// <summary>Flat-bottomed bed with a definite bank at its edge.</summary>
        private static float ChannelProfile(float t)
        {
            var c = Mathf.Clamp01(t);
            return 1f - c * c;
        }
    }
}
