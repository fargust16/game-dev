using System;
using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Water.Tests
{
    public sealed class FlowNetworkTests
    {
        private static Func<float, float, float> Sampler(float[,] heights)
        {
            return (worldX, worldZ) => heights[Mathf.FloorToInt(worldX), Mathf.FloorToInt(worldZ)];
        }

        // Simple ramp: height decreases as x increases, constant along z - every cell should flow
        // in the -x direction, and accumulation should grow monotonically as x decreases (more of
        // the ramp drains through low-x cells than high-x ones).
        private static float[,] MakeRamp(int size)
        {
            var heights = new float[size, size];
            for (var x = 0; x < size; x++)
            {
                for (var z = 0; z < size; z++)
                {
                    heights[x, z] = size - x;
                }
            }

            return heights;
        }

        [Test]
        public void Compute_Ramp_EveryCellFlowsToStrictlyLowerNeighbor()
        {
            var heights = MakeRamp(10);
            var grid = FlowNetwork.Compute(Sampler(heights), 0, 0, 10, 10, 1);

            for (var x = 0; x < grid.Width; x++)
            {
                for (var z = 0; z < grid.Depth; z++)
                {
                    if (!grid.TryGetDownstream(x, z, out var nx, out var nz))
                    {
                        continue; // sink - only expected at the low-x edge of the ramp
                    }

                    Assert.Less(grid.HeightAt(nx, nz), grid.HeightAt(x, z),
                        $"({x},{z}) flows to ({nx},{nz}) which is not lower");
                }
            }
        }

        [Test]
        public void Compute_Ramp_AccumulationGrowsDownstream()
        {
            var heights = MakeRamp(10);
            var grid = FlowNetwork.Compute(Sampler(heights), 0, 0, 10, 10, 1);

            // Every column at a given x should have accumulated at least as much as a column further
            // upstream (higher x, since height decreases with x) - the ramp is flat along z, so mass
            // only ever moves toward -x.
            for (var z = 0; z < grid.Depth; z++)
            {
                var previous = 0;
                for (var x = grid.Width - 1; x >= 0; x--)
                {
                    var accumulation = grid.AccumulationAt(x, z);
                    Assert.GreaterOrEqual(accumulation, previous,
                        $"accumulation should not decrease moving downstream at z={z}, x={x}");
                    previous = accumulation;
                }
            }

            // The lowest column should have accumulated the whole grid's worth of cells.
            var total = 0;
            for (var z = 0; z < grid.Depth; z++)
            {
                total += grid.AccumulationAt(0, z);
            }

            Assert.AreEqual(grid.Width * grid.Depth, total,
                "total accumulation at the ramp's low edge should equal every sampled cell");
        }

        [Test]
        public void Compute_SingleBowl_AllFlowConvergesOnTheMinimum()
        {
            var heights = new float[10, 10];
            for (var x = 0; x < 10; x++)
            {
                for (var z = 0; z < 10; z++)
                {
                    heights[x, z] = Mathf.Abs(x - 5) + Mathf.Abs(z - 5);
                }
            }

            var grid = FlowNetwork.Compute(Sampler(heights), 0, 0, 10, 10, 1);

            Assert.AreEqual(100, grid.AccumulationAt(5, 5),
                "the bowl's global minimum should accumulate every cell in the grid");
            Assert.IsFalse(grid.TryGetDownstream(5, 5, out _, out _),
                "the global minimum has no lower neighbour and must be a sink");
        }

        [Test]
        public void Compute_FlatTerrain_EveryCellIsASink()
        {
            var heights = new float[5, 5];
            for (var x = 0; x < 5; x++)
            {
                for (var z = 0; z < 5; z++)
                {
                    heights[x, z] = 3f;
                }
            }

            var grid = FlowNetwork.Compute(Sampler(heights), 0, 0, 5, 5, 1);

            for (var x = 0; x < grid.Width; x++)
            {
                for (var z = 0; z < grid.Depth; z++)
                {
                    Assert.IsFalse(grid.TryGetDownstream(x, z, out _, out _),
                        $"flat ground has no lower neighbour anywhere, but ({x},{z}) reported one");
                    Assert.AreEqual(1, grid.AccumulationAt(x, z),
                        "with no flow at all, every cell only accounts for itself");
                }
            }
        }

        [Test]
        public void Compute_SameInputsTwice_AreDeterministic()
        {
            var heights = MakeRamp(10);

            var first = FlowNetwork.Compute(Sampler(heights), 0, 0, 10, 10, 1);
            var second = FlowNetwork.Compute(Sampler(heights), 0, 0, 10, 10, 1);

            for (var x = 0; x < first.Width; x++)
            {
                for (var z = 0; z < first.Depth; z++)
                {
                    Assert.AreEqual(first.AccumulationAt(x, z), second.AccumulationAt(x, z));

                    var firstHas = first.TryGetDownstream(x, z, out var fnx, out var fnz);
                    var secondHas = second.TryGetDownstream(x, z, out var snx, out var snz);
                    Assert.AreEqual(firstHas, secondHas);
                    if (firstHas)
                    {
                        Assert.AreEqual((fnx, fnz), (snx, snz));
                    }
                }
            }
        }
    }
}
