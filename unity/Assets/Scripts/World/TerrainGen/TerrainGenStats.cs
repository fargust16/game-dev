using System.Globalization;
using System.Text;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Everything a Stage 1 run can be judged by without opening a scene.
    /// </summary>
    /// <remarks>
    /// Numbers, not screenshots. A generator whose only report is an image can only be argued about;
    /// the invariants in the design document are all expressed against fields on this object, and
    /// the dump tool prints it verbatim so two runs can be diffed as text.
    /// </remarks>
    public sealed class TerrainGenStats
    {
        public int Seed;
        public int GeneratorVersion;

        // --- crustal partition ---
        public int SiteCount;
        public int SitesInWorld;
        public int EligibleSites;

        // --- landmass ---
        public int LandSiteCount;
        public int IsletCount;
        public int ComponentCount;
        public float TargetLandFraction;
        public float RasterLandFraction;
        public float PolygonLandFraction;
        public float FieldLandFraction;
        public int FieldLandComponents;
        public int EnclosedSeaFilled;
        public bool LandBudgetShort;

        // --- coastline ---
        public int CoastRingCount;
        public int CoastVertexCount;
        public float CoastLengthMeters;

        /// <summary>
        /// Mainland coastline length over its convex-hull perimeter.
        /// </summary>
        /// <remarks>
        /// The design's open question about coastal variety, made measurable. 1.0 is a convex blob;
        /// a real coast with bays and peninsulas runs well above it.
        /// </remarks>
        public float CoastCrenulation;

        public int MaxRingDisplaceAttempts;
        public int RingsFallenBack;

        // --- height field ---
        public float MinHeightMeters;
        public float MaxHeightMeters;
        public float MeanHeightMeters;
        public bool HasNonFinite;
        public string FieldHash;

        // --- mountain uplift (Stage 2; zero on a Stage 1 world) ---
        public int CrustalEdgeCount;
        public int MountainSystemCount;
        public int BranchCount;
        public float LongestSpineMeters;
        public float TotalSpineMeters;
        public float TallestCrestMeters;
        public float UpliftMinMeters;
        public float UpliftMaxMeters;
        public float UpliftMeanMeters;

        /// <summary>Worst length-to-half-width ratio across the belts. Under about 3 reads as a hill.</summary>
        public float MinLengthToWidthRatio;

        /// <summary>
        /// Worst principal-spread ratio of any belt's own relief. About 1 is a dome.
        /// </summary>
        /// <remarks>
        /// The dome detector. Measured on the relief the belt produces rather than on its spine,
        /// because the failure this exists to catch was a long spine carrying a row of circles.
        /// </remarks>
        public float MinReliefElongation;

        /// <summary>Worst fraction of a belt whose width was set by the curvature cap.</summary>
        public float MaxCurvatureLimitedFraction;

        /// <summary>Lowest saddle between major summits, as a fraction of the crest peak.</summary>
        public float MinSaddleFraction;

        /// <summary>Mean uplift over dry land only, which is the only place it can be non-zero.</summary>
        public float UpliftMeanOverLandMeters;

        /// <summary>Samples where the hard uplift ceiling actually bound. Should be zero.</summary>
        public int UpliftClampedSamples;

        /// <summary>Ocean samples whose height differs from the Stage 1 field. Must be zero.</summary>
        public int OceanSamplesChanged;

        /// <summary>Stage 1 height range, for comparison against the Stage 2 range above.</summary>
        public float Stage1MinHeightMeters;

        public float Stage1MaxHeightMeters;
        public float Stage1MeanHeightMeters;
        public float Stage1LandFraction;

        // --- timing ---
        public long PartitionMs;
        public long LandmassMs;
        public long FieldMs;
        public long EdgesMs;
        public long BeltsMs;
        public long UpliftFieldMs;
        public long TotalMs;

        public string ToLine()
        {
            var c = CultureInfo.InvariantCulture;
            return string.Join(" ", new[]
            {
                $"seed={Seed}",
                $"ver={GeneratorVersion}",
                $"sites={SitesInWorld}/{SiteCount}",
                $"eligible={EligibleSites}",
                $"landCells={LandSiteCount}",
                $"islets={IsletCount}",
                $"rings={CoastRingCount}",
                $"verts={CoastVertexCount}",
                $"landTarget={TargetLandFraction.ToString("F3", c)}",
                $"landRaster={RasterLandFraction.ToString("F3", c)}",
                $"landPoly={PolygonLandFraction.ToString("F3", c)}",
                $"landField={FieldLandFraction.ToString("F3", c)}",
                $"fieldComponents={FieldLandComponents}",
                $"coastKm={(CoastLengthMeters / 1000f).ToString("F2", c)}",
                $"crenulation={CoastCrenulation.ToString("F2", c)}",
                $"h=[{MinHeightMeters.ToString("F1", c)}..{MaxHeightMeters.ToString("F1", c)}]",
                $"hMean={MeanHeightMeters.ToString("F1", c)}",
                $"ranges={MountainSystemCount}",
                $"branches={BranchCount}",
                $"spineKm={(TotalSpineMeters / 1000f).ToString("F2", c)}",
                $"crest={TallestCrestMeters.ToString("F0", c)}",
                $"uplift=[{UpliftMinMeters.ToString("F1", c)}..{UpliftMaxMeters.ToString("F1", c)}]",
                $"upliftLandMean={UpliftMeanOverLandMeters.ToString("F1", c)}",
                $"lenWidth={MinLengthToWidthRatio.ToString("F2", c)}",
                $"elongation={MinReliefElongation.ToString("F2", c)}",
                $"saddle={MinSaddleFraction.ToString("F2", c)}",
                $"oceanChanged={OceanSamplesChanged}",
                $"nonFinite={HasNonFinite}",
                $"hash={FieldHash}",
                $"ms={TotalMs}",
            });
        }

        /// <summary>The Stage 2 half of the report, appended to <see cref="ToReport"/>.</summary>
        public string ToStage2Report()
        {
            var c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine($"  crustEdges {CrustalEdgeCount} land-land boundaries available as belt candidates");
            sb.AppendLine($"  mountains  {MountainSystemCount} system(s) + {BranchCount} branch(es), "
                          + $"longest spine {LongestSpineMeters.ToString("F0", c)} m, "
                          + $"total spine {(TotalSpineMeters / 1000f).ToString("F2", c)} km, "
                          + $"tallest crest {TallestCrestMeters.ToString("F0", c)} m");
            sb.AppendLine($"  uplift     [{UpliftMinMeters.ToString("F1", c)} .. {UpliftMaxMeters.ToString("F1", c)}] m, "
                          + $"mean {UpliftMeanMeters.ToString("F2", c)} m over the world, "
                          + $"{UpliftMeanOverLandMeters.ToString("F1", c)} m over land, "
                          + $"clamped {UpliftClampedSamples}");
            sb.AppendLine($"  stage1->2  h [{Stage1MinHeightMeters.ToString("F1", c)} .. {Stage1MaxHeightMeters.ToString("F1", c)}]"
                          + $" -> [{MinHeightMeters.ToString("F1", c)} .. {MaxHeightMeters.ToString("F1", c)}] m, "
                          + $"mean {Stage1MeanHeightMeters.ToString("F1", c)} -> {MeanHeightMeters.ToString("F1", c)}, "
                          + $"land {Stage1LandFraction.ToString("F3", c)} -> {FieldLandFraction.ToString("F3", c)}");
            sb.AppendLine($"  geometry   worst length/half-width {MinLengthToWidthRatio.ToString("F2", c)}, "
                          + $"worst relief elongation {MinReliefElongation.ToString("F2", c)} "
                          + $"(1.0 is a dome), lowest saddle {MinSaddleFraction.ToString("F2", c)} of peak, "
                          + $"curvature-capped {(MaxCurvatureLimitedFraction * 100f).ToString("F0", c)} % at worst");
            sb.AppendLine($"  invariants ocean samples changed {OceanSamplesChanged} (must be 0)");
            sb.AppendLine($"  timing     edges {EdgesMs} ms, belts {BeltsMs} ms, uplift field {UpliftFieldMs} ms");
            return sb.ToString();
        }

        public string ToReport()
        {
            var c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine($"seed {Seed}  (generator version {GeneratorVersion})");
            sb.AppendLine($"  crust      sites {SitesInWorld} in world / {SiteCount} total, {EligibleSites} eligible for land");
            sb.AppendLine($"  landmass   {LandSiteCount} cells, {IsletCount} islet(s), {ComponentCount} component(s)"
                          + (LandBudgetShort ? "   [BUDGET SHORT]" : string.Empty));
            sb.AppendLine($"  land       target {TargetLandFraction.ToString("F3", c)}"
                          + $"  raster {RasterLandFraction.ToString("F3", c)}"
                          + $"  polygon {PolygonLandFraction.ToString("F3", c)}"
                          + $"  field {FieldLandFraction.ToString("F3", c)}");
            sb.AppendLine($"  coast      {CoastRingCount} ring(s), {CoastVertexCount} vertices, "
                          + $"{(CoastLengthMeters / 1000f).ToString("F2", c)} km, "
                          + $"crenulation {CoastCrenulation.ToString("F2", c)}");
            sb.AppendLine($"  outline    enclosed-sea cells filled {EnclosedSeaFilled}, "
                          + $"max displace attempts {MaxRingDisplaceAttempts}, rings fallen back {RingsFallenBack}");
            sb.AppendLine($"  field      h [{MinHeightMeters.ToString("F1", c)} .. {MaxHeightMeters.ToString("F1", c)}] m, "
                          + $"mean {MeanHeightMeters.ToString("F1", c)} m, "
                          + $"land components {FieldLandComponents}, non-finite {HasNonFinite}");
            sb.AppendLine($"  hash       {FieldHash}");
            sb.AppendLine($"  timing     partition {PartitionMs} ms, landmass {LandmassMs} ms, "
                          + $"field {FieldMs} ms, total {TotalMs} ms");
            return sb.ToString();
        }
    }
}
