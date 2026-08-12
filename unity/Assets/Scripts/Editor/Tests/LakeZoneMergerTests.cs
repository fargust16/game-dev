using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Water.Tests
{
    public sealed class LakeZoneMergerTests
    {
        private static WaterZone MakeBlob(
            string id, float originX, float originZ, int size, int x0, int z0, int x1, int z1, float waterY)
        {
            var flooded = new bool[size, size];
            var heights = new float[size, size];
            for (var x = x0; x <= x1; x++)
            {
                for (var z = z0; z <= z1; z++)
                {
                    flooded[x, z] = true;
                    heights[x, z] = waterY - 2f;
                }
            }

            return new WaterZone(id, waterY, originX, originZ, 1f, flooded, heights);
        }

        [Test]
        public void MergeNearby_TouchingFloodCells_BecomeOneLake()
        {
            var a = MakeBlob("a", 0, 0, 20, 2, 2, 4, 4, 10f);
            var b = MakeBlob("b", 0, 0, 20, 5, 2, 7, 4, 11f); // 1 cell gap
            var merged = LakeZoneMerger.MergeNearby(new[] { a, b }, gapMeters: 2f);
            Assert.AreEqual(1, merged.Length);
            Assert.AreEqual("lake_0", merged[0].Id);
        }

        [Test]
        public void MergeNearby_FarApart_StaySeparate()
        {
            var a = MakeBlob("a", 0, 0, 30, 2, 2, 4, 4, 10f);
            var b = MakeBlob("b", 0, 0, 30, 20, 20, 22, 22, 10f);
            var merged = LakeZoneMerger.MergeNearby(new[] { a, b }, gapMeters: 2f);
            Assert.AreEqual(2, merged.Length);
        }

        [Test]
        public void MergeNearby_OverlappingAabbButDistantFloods_StaySeparate()
        {
            // Two thin strips at opposite ends — AABB of each is small, gap large; must NOT merge
            // via inflated AABB alone (that previously glued whole-tile mega-lakes).
            var a = MakeBlob("a", 0, 0, 40, 1, 1, 3, 3, 10f);
            var b = MakeBlob("b", 0, 0, 40, 35, 35, 38, 38, 10f);
            var merged = LakeZoneMerger.MergeNearby(new[] { a, b }, gapMeters: 12f);
            Assert.AreEqual(2, merged.Length);
        }

        [Test]
        public void CullOverlapping_CentroidNeighbors_KeepsLargestOnly()
        {
            // Side-by-side lobes of one MapMagic pit: little exact cell overlap, close centers.
            var a = MakeBlob("a", 0, 0, 80, 10, 10, 40, 40, 10f);
            var b = MakeBlob("b", 0, 0, 80, 28, 28, 55, 55, 10f);
            var culled = LakeZoneMerger.CullOverlapping(new[] { a, b }, maxOverlapFraction: 0.12f);
            Assert.AreEqual(1, culled.Length);
        }

        [Test]
        public void CullOverlapping_ChainAcrossPlains_DoesNotCollapseToOne()
        {
            // Three well-separated lakes — must NOT transitively glue into one.
            var a = MakeBlob("a", 0, 0, 120, 5, 5, 20, 20, 10f);
            var b = MakeBlob("b", 0, 0, 120, 50, 50, 65, 65, 10f);
            var c = MakeBlob("c", 0, 0, 120, 95, 95, 110, 110, 10f);
            var culled = LakeZoneMerger.CullOverlapping(new[] { a, b, c }, maxOverlapFraction: 0.12f);
            Assert.AreEqual(3, culled.Length);
        }

        [Test]
        public void EnsureMinDistinct_PadsUpToMinCount()
        {
            var kept = new[] { MakeBlob("k", 0, 0, 100, 5, 5, 15, 15, 10f) };
            var pool = new[]
            {
                kept[0],
                MakeBlob("p1", 0, 0, 100, 40, 40, 50, 50, 10f),
                MakeBlob("p2", 0, 0, 100, 70, 10, 80, 20, 10f),
                MakeBlob("p3", 0, 0, 100, 10, 70, 20, 80, 10f),
            };
            var padded = LakeZoneMerger.EnsureMinDistinct(kept, pool, minCount: 4, minCenterSeparationMeters: 25f);
            Assert.AreEqual(4, padded.Length);
        }

        [Test]
        public void CullOverlapping_NestedFloods_KeepsLargestOnly()
        {
            // Large blob contains a smaller nested flood — merge may refuse if over cell cap;
            // cull must keep one plane.
            var large = MakeBlob("large", 0, 0, 40, 5, 5, 25, 25, 10f);
            var nested = MakeBlob("nested", 0, 0, 40, 10, 10, 15, 15, 10f);
            var culled = LakeZoneMerger.CullOverlapping(new[] { nested, large }, maxOverlapFraction: 0.12f);
            Assert.AreEqual(1, culled.Length);
            Assert.AreEqual("lake_0", culled[0].Id);
            Assert.Greater(culled[0].FloodedCellCount, nested.FloodedCellCount);
        }

        [Test]
        public void CullOverlapping_FarApart_StaySeparate()
        {
            var a = MakeBlob("a", 0, 0, 40, 2, 2, 6, 6, 10f);
            var b = MakeBlob("b", 0, 0, 40, 30, 30, 34, 34, 10f);
            var culled = LakeZoneMerger.CullOverlapping(new[] { a, b }, maxOverlapFraction: 0.12f);
            Assert.AreEqual(2, culled.Length);
        }

        [Test]
        public void FillEnclosedHoles_FillsInteriorDryCells()
        {
            var flooded = new bool[7, 7];
            for (var x = 1; x <= 5; x++)
            {
                for (var z = 1; z <= 5; z++)
                {
                    flooded[x, z] = true;
                }
            }

            flooded[3, 3] = false;
            LakeZoneMerger.FillEnclosedHoles(flooded, 7, 7);
            Assert.IsTrue(flooded[3, 3]);
        }
    }
}
