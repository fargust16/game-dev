using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// The geographic skeleton: the explicit world-space features the height field is derived from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what makes "why is this here" answerable. At Stage 1 it holds the coastline and the
    /// crustal partition the coastline is the boundary of; later stages add ranges, spurs, basins
    /// and terraces to the same object.
    /// </para>
    /// <para>
    /// Derived, deterministic and <b>not persisted</b> — rebuildable from seed, settings and
    /// generator version, which is the project's standing rule that generation is not persistence.
    /// Plain data with no behaviour beyond lookups, and no <c>UnityEngine</c> dependency beyond
    /// <c>Vector2</c>, so it is testable without a scene.
    /// </para>
    /// <para>
    /// No downstream system may require it. A world baked by Vista has no structure at all, and
    /// everything has to keep working — that is what keeps the legacy path viable.
    /// </para>
    /// </remarks>
    public sealed class WorldStructure
    {
        /// <summary>World seed this structure was built from.</summary>
        public int Seed { get; }

        /// <summary>Generator version. A seed plus a version identifies a world; a seed alone does not.</summary>
        public int GeneratorVersion { get; }

        /// <summary>
        /// Coastline polygons in world XZ, counter-clockwise, largest first. Ring 0 is the mainland.
        /// </summary>
        public IReadOnlyList<Vector2[]> CoastRings { get; }

        /// <summary>Crustal sites that became land, ascending.</summary>
        public IReadOnlyList<int> LandSites { get; }

        /// <summary>Detached islets among <see cref="CoastRings"/>.</summary>
        public int IsletCount { get; }

        /// <summary>
        /// The partition the landmass was grown from.
        /// </summary>
        /// <remarks>
        /// Kept because it is the provenance of the coastline and, from Stage 2, the source of the
        /// mountain-range spines: ranges follow the interior boundaries of this same partition, so
        /// the coast and the ranges are consequences of one structure rather than two independent
        /// ones.
        /// </remarks>
        public CrustalPartition Partition { get; }

        /// <summary>
        /// Uplift belts, primaries first, each branch after the range it left.
        /// </summary>
        /// <remarks>
        /// Empty for a Stage 1 world. Every range names the crustal site pairs its spine was chained
        /// from, so "why is this mountain here" resolves to a specific set of block boundaries rather
        /// than to a seed.
        /// </remarks>
        public IReadOnlyList<MountainRange> Ranges { get; }

        public WorldStructure(
            int seed,
            int generatorVersion,
            IReadOnlyList<Vector2[]> coastRings,
            IReadOnlyList<int> landSites,
            int isletCount,
            CrustalPartition partition,
            IReadOnlyList<MountainRange> ranges = null)
        {
            Seed = seed;
            GeneratorVersion = generatorVersion;
            CoastRings = coastRings ?? System.Array.Empty<Vector2[]>();
            LandSites = landSites ?? System.Array.Empty<int>();
            IsletCount = isletCount;
            Partition = partition;
            Ranges = ranges ?? System.Array.Empty<MountainRange>();
        }

        /// <summary>Copy carrying uplift belts, leaving the Stage 1 structure untouched.</summary>
        public WorldStructure WithRanges(IReadOnlyList<MountainRange> ranges) =>
            new WorldStructure(
                Seed, GeneratorVersion, CoastRings, LandSites, IsletCount, Partition, ranges);

        /// <summary>Primary belts only, i.e. mountain systems rather than their branches.</summary>
        public int PrimaryRangeCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < Ranges.Count; i++)
                {
                    if (Ranges[i].ParentIndex < 0)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Sum of the absolute polygon areas of every coast ring, square metres.</summary>
        public float PolygonLandAreaSqm()
        {
            var total = 0f;
            for (var i = 0; i < CoastRings.Count; i++)
            {
                total += Mathf.Abs(Town.Geometry.Geo2.PolygonArea(CoastRings[i]));
            }

            return total;
        }

        /// <summary>Total coastline length across every ring, metres.</summary>
        public float CoastLengthMeters()
        {
            var total = 0f;
            for (var i = 0; i < CoastRings.Count; i++)
            {
                total += CoastOutline.Perimeter(CoastRings[i]);
            }

            return total;
        }

        public int CoastVertexCount()
        {
            var total = 0;
            for (var i = 0; i < CoastRings.Count; i++)
            {
                total += CoastRings[i]?.Length ?? 0;
            }

            return total;
        }
    }
}
