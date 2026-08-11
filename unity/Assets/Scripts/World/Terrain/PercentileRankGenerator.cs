using Den.Tools;
using Den.Tools.Matrices;
using MapMagic.Nodes;
using MapMagic.Products;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// Custom MapMagic matrix generator (WORLD_GENERATION_PIPELINE.md biome-ratio guarantee):
    /// remaps every value in the input matrix to its own percentile rank within that matrix's
    /// value distribution, so "the bottom 10% of this map" and "value &lt; 0.10" become the exact
    /// same statement, regardless of the input noise's actual distribution shape or seed. A raw
    /// Perlin field's values cluster near the middle of the 0..1 range (bell-curve-ish), so a fixed
    /// threshold on the raw field covers a wildly different area fraction from one seed to the
    /// next - the reason OverworldTerrainMenu's old mountainNoise cutoff kept needing re-tuning
    /// (0.5 covered roughly half the tile; 0.72 covered almost none; a single constant can't fix
    /// that because the problem is the field's distribution, not the constant). Rank is a strictly
    /// monotonic function of the input value, so topology is fully preserved - a local minimum
    /// stays a local minimum, "lower than" stays "lower than" - which is exactly why hydrology
    /// (LakeBasinFinder's local-minima flood-fill) still works correctly on a rank-normalized
    /// height field: only the spacing between values changes to guarantee area fractions, never
    /// which cell is locally lowest.
    /// <para>
    /// Not part of MapMagic's own node set - no MapMagic version suffix ("...200") in the class
    /// name, this is project-specific. Computed per generated tile: correct while the overworld is
    /// a single V0 stub tile (the whole map IS the matrix being ranked), but each tile would rank
    /// itself independently once real W0.5 chunk streaming lands, which would show as a seam at
    /// tile borders - revisit then (e.g. rank against a shared/precomputed distribution instead of
    /// each tile's own).
    /// </para>
    /// </summary>
    [System.Serializable]
    [GeneratorMenu(menu = "Map/Modifiers", name = "Percentile Rank", iconName = "GeneratorIcons/Levels", disengageable = true)]
    public class PercentileRankGenerator : Generator, IInlet<MatrixWorld>, IOutlet<MatrixWorld>
    {
        public override void Generate(TileData data, StopToken stop)
        {
            if (stop != null && stop.stop)
            {
                return;
            }

            var src = data.ReadInletProduct(this);
            if (src == null)
            {
                return;
            }

            if (!enabled)
            {
                data.StoreProduct(this, src);
                return;
            }

            if (stop != null && stop.stop)
            {
                return;
            }

            var dst = new MatrixWorld(src);
            RankNormalize(src, dst);

            data.StoreProduct(this, dst);
        }

        /// <summary>
        /// Sets each cell in <paramref name="dst"/> to <paramref name="src"/>'s value at that cell,
        /// expressed as its 0..1 rank among every value in <paramref name="src"/> (0 = lowest,
        /// 1 = highest). O(n log n): sorts a copy of the source values once, then binary-searches
        /// each cell's own value in that sorted copy for its rank - simple and exact, and a one-off
        /// Editor-time generation cost, not a per-frame one.
        /// </summary>
        public static void RankNormalize(Matrix src, Matrix dst)
        {
            var count = src.arr.Length;
            if (count <= 1)
            {
                return;
            }

            var sorted = (float[])src.arr.Clone();
            System.Array.Sort(sorted);

            for (var i = 0; i < count; i++)
            {
                var rank = System.Array.BinarySearch(sorted, src.arr[i]);
                if (rank < 0)
                {
                    rank = ~rank;
                }

                dst.arr[i] = (float)rank / (count - 1);
            }
        }
    }
}
