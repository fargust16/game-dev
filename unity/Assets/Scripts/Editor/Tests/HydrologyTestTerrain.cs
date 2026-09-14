using GameDev.World.Hydrology;
using UnityEngine;

namespace GameDev.World.Hydrology.Tests
{
    /// <summary>
    /// Synthetic height fields the hydrology tests reason about.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built from plain functions rather than from a baked terrain, following the house pattern in
    /// <c>LandscapeCostFieldTests</c>. The whole analysis is pure C#, so none of these tests needs a
    /// scene, a terrain bake or an asset — which is what keeps them fast enough to be worth running.
    /// </para>
    /// <para>
    /// The shapes are chosen for what they prove, and two of them are less obvious than they look.
    /// A depression only dams water if it is deeper than the fall across it, so the ramp is gentle;
    /// and a bare cone has no drainage network at all, because flow diverges everywhere and no cell
    /// ever accumulates enough area to become a channel — so the island has valleys cut into it.
    /// </para>
    /// </remarks>
    internal static class HydrologyTestTerrain
    {
        public const float SeaLevel = 24f;

        /// <summary>
        /// A gentle plane rising toward +X out of the sea along the west edge.
        /// </summary>
        /// <remarks>
        /// The slope is deliberately shallow. At a steep gradient the fall across one cell exceeds
        /// any pit dug into it, so the "pit" is an open groove and nothing is ever dammed — which is
        /// how the first version of these tests quietly measured nothing.
        /// </remarks>
        public static HydrologyGrid Ramp(int size = 64, float cellSize = 10f, float slope = 0.05f)
        {
            return HydrologyGrid.FromFunction(
                0f, 0f, size, size, cellSize, SeaLevel,
                (x, _) => SeaLevel - 12f + x * slope);
        }

        /// <summary>A ramp with a square basin dug into its upper half.</summary>
        /// <param name="depth">How far below the surrounding surface the basin floor sits.</param>
        public static HydrologyGrid RampWithPit(
            int size = 64,
            float cellSize = 10f,
            float slope = 0.05f,
            float depth = 8f,
            int pitMin = 45,
            int pitMax = 55)
        {
            var loX = pitMin * cellSize;
            var hiX = pitMax * cellSize;
            return HydrologyGrid.FromFunction(
                0f, 0f, size, size, cellSize, SeaLevel,
                (x, z) =>
                {
                    var h = SeaLevel - 12f + x * slope;
                    var inPit = x >= loX && x <= hiX && z >= loX && z <= hiX;
                    return inPit ? h - depth : h;
                });
        }

        /// <summary>
        /// A cone rising out of the sea with a main valley and two tributary valleys cut into it.
        /// </summary>
        /// <remarks>
        /// The tributaries meet the main valley part way down, so the network has real confluences
        /// and therefore a Strahler order above one. Without cut valleys a cone drains radially and
        /// produces a fringe of unrelated rills instead of a river system.
        /// </remarks>
        public static HydrologyGrid ValleyIsland(int size = 96, float cellSize = 12f)
        {
            var span = (size - 1) * cellSize;
            var centre = span * 0.5f;
            var radius = centre * 0.9f;

            var junction = new Vector2(centre + radius * 0.35f, centre);
            var mouth = new Vector2(centre + radius * 1.05f, centre);
            var headA = new Vector2(centre - radius * 0.45f, centre + radius * 0.5f);
            var headB = new Vector2(centre - radius * 0.45f, centre - radius * 0.5f);

            var grooveWidth = cellSize * 2.6f;

            return HydrologyGrid.FromFunction(
                0f, 0f, size, size, cellSize, SeaLevel,
                (x, z) =>
                {
                    var p = new Vector2(x, z);
                    var r = Vector2.Distance(p, new Vector2(centre, centre));
                    var h = SeaLevel + 95f * Mathf.Clamp01(1f - r / radius);

                    h -= Groove(p, junction, mouth, grooveWidth, 30f);
                    h -= Groove(p, headA, junction, grooveWidth, 26f);
                    h -= Groove(p, headB, junction, grooveWidth, 26f);
                    return h;
                });
        }

        /// <summary>A grid entirely above sea level, with no coast anywhere.</summary>
        public static HydrologyGrid LandlockedPlateau(int size = 32, float cellSize = 10f) =>
            HydrologyGrid.FromFunction(
                0f, 0f, size, size, cellSize, SeaLevel, (_, _) => SeaLevel + 60f);

        private static float Groove(Vector2 p, Vector2 a, Vector2 b, float width, float depth)
        {
            var ab = b - a;
            var lengthSq = ab.sqrMagnitude;
            var t = lengthSq < 1e-4f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
            var distance = Vector2.Distance(p, a + ab * t);
            return depth * Mathf.Exp(-distance / width);
        }

        /// <summary>Builds a reach from bare (position, bed) triples, for topology-only tests.</summary>
        public static RiverReach Reach(
            int id,
            int downstreamId,
            ReachTermination termination,
            params (float x, float z, float bed)[] points)
        {
            var nodes = new RiverNode[points.Length];
            var travelled = 0f;
            for (var i = 0; i < points.Length; i++)
            {
                var position = new Vector2(points[i].x, points[i].z);
                if (i > 0)
                {
                    travelled += Vector2.Distance(nodes[i - 1].PositionXZ, position);
                }

                nodes[i] = new RiverNode(
                    position, points[i].bed, points[i].bed + 0.5f, travelled, 50_000f);
            }

            return new RiverReach(
                new ReachId(id),
                downstreamId >= 0 ? new ReachId(downstreamId) : ReachId.None,
                LakeId.None,
                nodes,
                1,
                50_000f,
                9f,
                termination,
                true);
        }

        /// <summary>Counts the land cells that have no downhill neighbour on the filled surface.</summary>
        public static int CountLandSinks(HydrologyGrid grid, byte[] directions, bool[] oceanMask)
        {
            var sinks = 0;
            for (var i = 0; i < grid.CellCount; i++)
            {
                if (!oceanMask[i] && directions[i] == FlowRouting.NoDirection)
                {
                    sinks++;
                }
            }

            return sinks;
        }
    }
}
