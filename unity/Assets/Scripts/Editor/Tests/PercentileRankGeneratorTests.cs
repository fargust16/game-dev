using Den.Tools;
using Den.Tools.Matrices;
using GameDev.World.Terrain;
using NUnit.Framework;

namespace GameDev.World.Terrain.Tests
{
    public sealed class PercentileRankGeneratorTests
    {
        [Test]
        public void RankNormalize_MapsValuesToTheirOwnPercentile()
        {
            var values = new[] { 5f, 1f, 3f, 4f, 2f };
            var src = new Matrix(new CoordRect(0, 0, values.Length, 1), values);
            var dst = new Matrix(new CoordRect(0, 0, values.Length, 1), new float[values.Length]);

            PercentileRankGenerator.RankNormalize(src, dst);

            Assert.AreEqual(1f, dst.arr[0], 1e-5f);
            Assert.AreEqual(0f, dst.arr[1], 1e-5f);
            Assert.AreEqual(0.5f, dst.arr[2], 1e-5f);
            Assert.AreEqual(0.75f, dst.arr[3], 1e-5f);
            Assert.AreEqual(0.25f, dst.arr[4], 1e-5f);
        }

        [Test]
        public void RankNormalize_GuaranteesAreaFraction_RegardlessOfInputDistribution()
        {
            // A heavily skewed input (80 values clustered near 0.9, 20 clustered near 0.1) mirrors
            // the seed-dependent "noise field's realized range covers a wildly different fraction
            // of the map than the threshold assumed" bug a raw fixed threshold couldn't avoid -
            // rank normalization must still put exactly the lowest 20 below rank 0.2 regardless.
            const int count = 100;
            var values = new float[count];
            for (var i = 0; i < count; i++)
            {
                values[i] = i < 80 ? 0.9f + i * 0.0001f : 0.1f + i * 0.0001f;
            }

            var src = new Matrix(new CoordRect(0, 0, count, 1), values);
            var dst = new Matrix(new CoordRect(0, 0, count, 1), new float[count]);

            PercentileRankGenerator.RankNormalize(src, dst);

            var below20Percent = 0;
            for (var i = 0; i < count; i++)
            {
                if (dst.arr[i] < 0.2f)
                {
                    below20Percent++;
                }
            }

            Assert.AreEqual(20, below20Percent,
                "exactly the lowest 20 values, whatever their raw magnitude, should land below rank 0.2");
        }

        [Test]
        public void RankNormalize_PreservesOrdering()
        {
            var values = new[] { 10f, -3f, 0f, 100f, 2.5f };
            var src = new Matrix(new CoordRect(0, 0, values.Length, 1), values);
            var dst = new Matrix(new CoordRect(0, 0, values.Length, 1), new float[values.Length]);

            PercentileRankGenerator.RankNormalize(src, dst);

            for (var i = 0; i < values.Length; i++)
            {
                for (var j = 0; j < values.Length; j++)
                {
                    if (values[i] < values[j])
                    {
                        Assert.Less(dst.arr[i], dst.arr[j],
                            $"index {i} (raw {values[i]}) should still rank below index {j} (raw {values[j]})");
                    }
                }
            }
        }
    }
}
