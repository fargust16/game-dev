using System;
using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Water.Tests
{
    public sealed class LakeBasinFinderTests
    {
        private static float[,] MakeSingleBowl(int size, int centerX, int centerZ, int radius, float highGround = 1000f)
        {
            var heights = new float[size, size];
            for (var x = 0; x < size; x++)
            {
                for (var z = 0; z < size; z++)
                {
                    heights[x, z] = highGround;
                }
            }

            for (var x = 0; x < size; x++)
            {
                for (var z = 0; z < size; z++)
                {
                    var d = Mathf.Abs(x - centerX) + Mathf.Abs(z - centerZ);
                    if (d <= radius)
                    {
                        heights[x, z] = Mathf.Min(heights[x, z], d);
                    }
                }
            }

            return heights;
        }

        private static Func<float, float, float> Sampler(float[,] heights)
        {
            return (worldX, worldZ) => heights[Mathf.FloorToInt(worldX), Mathf.FloorToInt(worldZ)];
        }

        [Test]
        public void FindZones_SingleBowl_EveryFloodedCellAtOrBelowItsOwnWaterLevel()
        {
            var heights = MakeSingleBowl(10, 5, 5, 4);
            var zones = LakeBasinFinder.FindZones(Sampler(heights), 0, 0, 10, 10, 1, 10f, 1000, 1, 1);

            Assert.AreEqual(1, zones.Length);
            var zone = zones[0];

            for (var x = 0; x < 10; x++)
            {
                for (var z = 0; z < 10; z++)
                {
                    if (zone.IsFlooded(x, z))
                    {
                        Assert.LessOrEqual(heights[x, z], zone.WaterLevelY,
                            $"flooded cell ({x},{z}) sits above its own zone's water level - water would pass through ground");
                    }
                }
            }

            Assert.IsTrue(zone.IsFlooded(5, 5), "the basin's global minimum should always end up flooded");
        }

        [Test]
        public void FindZones_MaxDepth_StopsAtBasinRimNotSurroundingHighGround()
        {
            var heights = MakeSingleBowl(10, 5, 5, 4);
            // Cell count cap is deliberately huge relative to the bowl (100 cells) so the depth
            // cap - not the cell count - is what has to stop the flood, proving a generous count
            // cap alone would otherwise swallow the whole surrounding plain.
            var zones = LakeBasinFinder.FindZones(Sampler(heights), 0, 0, 10, 10, 1, 10f, 100, 1, 1);

            Assert.AreEqual(1, zones.Length);
            var zone = zones[0];

            for (var x = 0; x < 10; x++)
            {
                for (var z = 0; z < 10; z++)
                {
                    if (Mathf.Abs(x - 5) + Mathf.Abs(z - 5) > 4)
                    {
                        Assert.IsFalse(zone.IsFlooded(x, z),
                            $"cell ({x},{z}) is outside the bowl but got flooded - depth cap failed to stop at the rim");
                    }
                }
            }
        }

        [Test]
        public void FindZones_FlatTerrain_ProducesNoZones()
        {
            var heights = new float[10, 10];
            for (var x = 0; x < 10; x++)
            {
                for (var z = 0; z < 10; z++)
                {
                    heights[x, z] = 5f;
                }
            }

            var zones = LakeBasinFinder.FindZones(Sampler(heights), 0, 0, 10, 10, 1, 10f, 1000, 1, 1);

            Assert.AreEqual(0, zones.Length, "flat ground has no local minimum and should never spawn a lake");
        }

        [Test]
        public void FindZones_TwoBowlsAtDifferentElevations_EachKeepsItsOwnWaterLevel()
        {
            var heights = new float[20, 20];
            for (var x = 0; x < 20; x++)
            {
                for (var z = 0; z < 20; z++)
                {
                    heights[x, z] = 1000f;
                }
            }

            for (var x = 0; x < 20; x++)
            {
                for (var z = 0; z < 20; z++)
                {
                    var dLow = Mathf.Abs(x - 5) + Mathf.Abs(z - 5);
                    if (dLow <= 4)
                    {
                        heights[x, z] = Mathf.Min(heights[x, z], dLow);
                    }

                    var dHigh = Mathf.Abs(x - 15) + Mathf.Abs(z - 15);
                    if (dHigh <= 4)
                    {
                        heights[x, z] = Mathf.Min(heights[x, z], 50f + dHigh);
                    }
                }
            }

            var zones = LakeBasinFinder.FindZones(Sampler(heights), 0, 0, 20, 20, 1, 10f, 1000, 1, 2);

            Assert.AreEqual(2, zones.Length);
            Assert.IsTrue(Array.Exists(zones, z => z.WaterLevelY < 10f),
                "expected a low-elevation (valley) zone");
            Assert.IsTrue(Array.Exists(zones, z => z.WaterLevelY >= 50f),
                "expected a high-elevation (mountain) zone - a single global water Y could never produce this");
        }

        [Test]
        public void FindZones_SameInputsTwice_AreDeterministic()
        {
            var heights = MakeSingleBowl(10, 5, 5, 4);

            var first = LakeBasinFinder.FindZones(Sampler(heights), 0, 0, 10, 10, 1, 10f, 1000, 1, 1);
            var second = LakeBasinFinder.FindZones(Sampler(heights), 0, 0, 10, 10, 1, 10f, 1000, 1, 1);

            Assert.AreEqual(first.Length, second.Length);
            Assert.AreEqual(first[0].WaterLevelY, second[0].WaterLevelY);
        }

        [Test]
        public void FindZones_MinCellsPerZone_RejectsTooSmallDips()
        {
            var heights = MakeSingleBowl(10, 5, 5, 1); // tiny dip: at most 5 cells

            var zones = LakeBasinFinder.FindZones(Sampler(heights), 0, 0, 10, 10, 1, 10f, 1000, 100, 1);

            Assert.AreEqual(0, zones.Length, "a dip smaller than minCellsPerZone should not become a lake");
        }
    }
}
