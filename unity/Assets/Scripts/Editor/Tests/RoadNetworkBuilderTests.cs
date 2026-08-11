using System.Collections.Generic;
using GameDev.World.Roads;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Roads.Tests
{
    public sealed class RoadNetworkBuilderTests
    {
        private static RoadGridSpec MakeGrid() => new RoadGridSpec(0f, 0f, 10, 10, 10f);

        [Test]
        public void BuildNetwork_FewerThanTwoNodes_ReturnsEmpty()
        {
            var grid = MakeGrid();
            Assert.AreEqual(0, RoadNetworkBuilder.BuildNetwork(new List<Vector2>(), grid, (x, z) => 1f).Count);
            Assert.AreEqual(0, RoadNetworkBuilder.BuildNetwork(new List<Vector2> { new Vector2(5f, 5f) }, grid, (x, z) => 1f).Count);
        }

        [Test]
        public void BuildNetwork_FourCornersUniformCost_ConnectsWithSidesNotDiagonals()
        {
            var grid = MakeGrid();
            var nodes = new List<Vector2>
            {
                new Vector2(10f, 10f), // 0: bottom-left
                new Vector2(10f, 90f), // 1: top-left
                new Vector2(90f, 10f), // 2: bottom-right
                new Vector2(90f, 90f), // 3: top-right
            };

            var segments = RoadNetworkBuilder.BuildNetwork(nodes, grid, (x, z) => 1f);

            // A spanning tree over 4 nodes has exactly 3 edges.
            Assert.AreEqual(3, segments.Count);

            var connected = new HashSet<int>();
            foreach (var segment in segments)
            {
                connected.Add(segment.NodeA);
                connected.Add(segment.NodeB);

                var isDiagonal =
                    (segment.NodeA == 0 && segment.NodeB == 3) || (segment.NodeA == 3 && segment.NodeB == 0) ||
                    (segment.NodeA == 1 && segment.NodeB == 2) || (segment.NodeA == 2 && segment.NodeB == 1);
                Assert.IsFalse(isDiagonal, "with uniform cost, the shorter side edges must win over the longer diagonals");
            }

            Assert.AreEqual(4, connected.Count, "every node must be reachable in the resulting tree");
        }

        [Test]
        public void BuildNetwork_NoGapInWall_FindsNoRoute()
        {
            // Band (220m) exceeds RoadCost.MaxBridgeSpanMeters (200m) and spans every z - neither a
            // bridge nor a walk-around detour exists, so BuildNetwork must report that honestly
            // rather than fabricating a segment.
            var grid = new RoadGridSpec(0f, 0f, 35, 10, 10f);
            var nodes = new List<Vector2> { new Vector2(5f, 50f), new Vector2(345f, 50f) };

            float Cost(float x, float z) => x > 50f && x < 270f ? float.PositiveInfinity : 1f;

            var segments = RoadNetworkBuilder.BuildNetwork(nodes, grid, Cost);

            Assert.AreEqual(0, segments.Count);
        }

        [Test]
        public void BuildNetwork_NarrowWaterGap_BridgesAcross()
        {
            var grid = MakeGrid();
            var nodes = new List<Vector2> { new Vector2(10f, 50f), new Vector2(90f, 50f) };

            // 20m gap spanning every z - no walk-around detour exists on this grid, so the only
            // way BuildNetwork can connect these nodes is a bridge.
            float Cost(float x, float z) => x > 40f && x < 60f ? float.PositiveInfinity : 1f;

            var segments = RoadNetworkBuilder.BuildNetwork(nodes, grid, Cost);

            Assert.AreEqual(1, segments.Count, "a 20m gap is well within MaxBridgeSpanMeters and should connect");
        }

        [Test]
        public void BuildNetwork_StraightLineBlockedButDetourExists_StillConnects()
        {
            // Regression for the real bug: two POIs directly across a lake from each other, where
            // the straight line between them is fully submerged but a route around the lake's edge
            // exists. An MST that only estimates edge weight along the straight line (the original,
            // buggy implementation) would have marked this pair as unreachable and produced zero
            // roads even though a real path exists - BuildNetwork now runs actual A* per pair, so it
            // must find the detour.
            var grid = new RoadGridSpec(0f, 0f, 35, 10, 10f);
            var nodes = new List<Vector2> { new Vector2(10f, 50f), new Vector2(340f, 50f) };

            // "Lake" blocks a rectangle 220m wide in x (wider than RoadCost.MaxBridgeSpanMeters, so
            // a direct bridge can't shortcut it) and 60m tall in z - both nodes sit exactly on the
            // blocked z row, so the direct line is blocked end to end and open only above/below.
            float Cost(float x, float z) => x > 50f && x < 270f && z > 20f && z < 80f ? float.PositiveInfinity : 1f;

            var segments = RoadNetworkBuilder.BuildNetwork(nodes, grid, Cost);

            Assert.AreEqual(1, segments.Count, "a detour around the lake exists and must be found");
            foreach (var p in segments[0].Points)
            {
                if (p.x > 50f && p.x < 270f)
                {
                    Assert.IsTrue(p.y <= 20f || p.y >= 80f, "the route must cross the lake band only where it's actually open, not bridge straight across");
                }
            }
        }

        [Test]
        public void BuildNetwork_ManyNodesInALine_AllConnectedDespiteKnnCandidateLimit()
        {
            // Regression for demo-scale POI counts (PoiDemoScenario: 20+ markers): each node's KNN
            // candidate list only covers its 6 nearest neighbours, not every other node - this
            // proves the resulting graph still spans everyone instead of silently stranding nodes
            // outside that local neighbourhood.
            var grid = new RoadGridSpec(0f, 0f, 100, 10, 10f);
            var nodes = new List<Vector2>();
            for (var i = 0; i < 20; i++)
            {
                nodes.Add(new Vector2(20f + i * 45f, 50f));
            }

            var segments = RoadNetworkBuilder.BuildNetwork(nodes, grid, (x, z) => 1f);

            Assert.AreEqual(19, segments.Count, "a spanning tree over 20 nodes has exactly 19 edges");

            var connected = new HashSet<int>();
            foreach (var segment in segments)
            {
                connected.Add(segment.NodeA);
                connected.Add(segment.NodeB);
            }

            Assert.AreEqual(20, connected.Count, "every node must end up reachable despite the KNN candidate limit");
        }
    }
}
