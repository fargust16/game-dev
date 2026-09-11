using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Stage 2B: the broad mountain massif raised on the accepted macro axis.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stage 2A proved an axis exists that can carry a body of a given width without folding it.
    /// This stage builds that body and nothing else - no ridges, no spurs, no valleys, no drainage,
    /// no detail. What comes out is the parent structure a later stage subdivides.
    /// </para>
    /// <para>
    /// Stage 1 is run once and read, never rewritten. Its coastline decides where the massif may
    /// stand and its signed-distance field drives the coast gate, so the shoreline cannot move: the
    /// gate is exactly zero at the shore, which makes every ocean sample keep its Stage 1 height to
    /// the bit rather than merely close to it.
    /// </para>
    /// </remarks>
    public sealed class Stage2BResult
    {
        public int Seed { get; }
        public TerrainGenSettings TerrainSettings { get; }
        public MacroStructureSettings MacroSettings { get; }
        public MassifSettings MassifSettings { get; }

        public Stage1Result Stage1 { get; }
        public MacroPlates Plates { get; }

        /// <summary>Every Stage 2A candidate, accepted and rejected alike.</summary>
        public IReadOnlyList<MacroAxis> Candidates { get; }

        /// <summary>One massif per accepted axis.</summary>
        public IReadOnlyList<MountainMassif> Massifs { get; }

        /// <summary>Stage 1 height plus the massif contribution.</summary>
        public WorldHeightField Field { get; }

        /// <summary>The massif contribution alone, metres.</summary>
        public WorldHeightField UpliftField { get; }

        /// <summary>Per-massif measurements taken from the field this run produced.</summary>
        public IReadOnlyList<MassifMetrics> Metrics { get; }

        /// <summary>Longitudinal stations per massif, in the same order as <see cref="Massifs"/>.</summary>
        public IReadOnlyList<MassifStation[]> Stations { get; }

        public Stage2BStats Stats { get; }

        public Stage2BResult(
            int seed,
            TerrainGenSettings terrainSettings,
            MacroStructureSettings macroSettings,
            MassifSettings massifSettings,
            Stage1Result stage1,
            MacroPlates plates,
            IReadOnlyList<MacroAxis> candidates,
            IReadOnlyList<MountainMassif> massifs,
            WorldHeightField field,
            WorldHeightField upliftField,
            IReadOnlyList<MassifMetrics> metrics,
            IReadOnlyList<MassifStation[]> stations,
            Stage2BStats stats)
        {
            Seed = seed;
            TerrainSettings = terrainSettings;
            MacroSettings = macroSettings;
            MassifSettings = massifSettings;
            Stage1 = stage1;
            Plates = plates;
            Candidates = candidates;
            Massifs = massifs;
            Field = field;
            UpliftField = upliftField;
            Metrics = metrics;
            Stations = stations;
            Stats = stats;
        }
    }

    /// <summary>Per-massif measurements, taken from the field rather than from the parameters.</summary>
    public struct MassifMetrics
    {
        public int Index;
        public float LengthMeters;
        public float NominalHalfWidthMeters;
        public float ReachLimitedHalfWidthMeters;
        public bool ReachBound;
        public float CrestHeightMeters;

        /// <summary>Mean full width where the uplift is at least half the local crest, metres.</summary>
        public float MeasuredHalfHeightWidthMeters;

        public float MinHalfHeightWidthMeters;
        public float MaxHalfHeightWidthMeters;

        /// <summary>Mean full width where the uplift is at least a tenth of the local crest, metres.</summary>
        public float MeasuredToeWidthMeters;

        /// <summary>Lowest saddle between culminations, as a fraction of the peak.</summary>
        public float MinSaddleFraction;

        /// <summary>Culminations placed on the axis.</summary>
        public int CulminationCount;

        /// <summary>Highest uplift found anywhere, and how far that sample is from the axis.</summary>
        public float PeakUpliftMeters;

        public float PeakOffsetMeters;

        /// <summary>Footprint area where uplift clears a metre, square metres.</summary>
        public float FootprintAreaSqm;

        /// <summary>Length over nominal half-width.</summary>
        public float LengthToWidthRatio;
    }

    /// <summary>One longitudinal station through a massif, measured from the field.</summary>
    /// <remarks>
    /// The instrument the width model is judged with. Intended half-width is what the geology asked
    /// for; the half-height and toe widths are what the lattice actually carries. A model whose
    /// footprint is a property of the massif keeps those in a fixed ratio all along the body; one whose
    /// footprint is decided by something else - curvature, or the coastline - does not.
    /// </remarks>
    public struct MassifStation
    {
        public float S;
        public Vector2 Position;

        /// <summary>Axis curvature here, 1/metres.</summary>
        public float Curvature;

        /// <summary>Half-width the model asks for here, metres.</summary>
        public float IntendedHalfWidthMeters;

        /// <summary>Crest the model asks for here, metres.</summary>
        public float IntendedCrestMeters;

        /// <summary>Highest contribution on this cross-section, metres.</summary>
        public float SectionPeakMeters;

        /// <summary>Full width above half this section's own peak, metres.</summary>
        public float HalfHeightWidthMeters;

        /// <summary>Full width above a tenth of this section's own peak, metres.</summary>
        public float ToeWidthMeters;

        /// <summary>How much the coast gate holds this section below its intended crest, 0 to 1.</summary>
        public float GateFactor;
    }

    /// <summary>Everything a Stage 2B run can be judged by without opening a scene.</summary>
    public sealed class Stage2BStats
    {
        public int Seed;
        public int GeneratorVersion;

        public int AcceptedAxes;
        public int MassifCount;

        /// <summary>
        /// Worst fraction of a massif's high ground standing in a gate too shallow for its section.
        /// </summary>
        /// <remarks>
        /// Zero on a world whose axis clears the coast by the body's own reach plus the gate width.
        /// Above zero it names precisely how much of the massif the coastline is deforming, and the
        /// number belongs in the report rather than in a silent correction.
        /// </remarks>
        public float ShearedHighFraction;

        public float UpliftMinMeters;
        public float UpliftMaxMeters;
        public float UpliftMeanMeters;
        public float UpliftMeanOverLandMeters;
        public int UpliftClampedSamples;
        public int NegativeUpliftSamples;

        /// <summary>Samples above a metre of uplift, and how many connected groups they form.</summary>
        public int UpliftSamples;

        public int UpliftComponents;

        public int OceanSamplesChanged;

        public float Stage1MinHeightMeters;
        public float Stage1MaxHeightMeters;
        public float Stage1MeanHeightMeters;
        public string Stage1Hash;

        public float MinHeightMeters;
        public float MaxHeightMeters;
        public float MeanHeightMeters;
        public string FieldHash;
        public bool HasNonFinite;

        public float WorstMeasuredWidthMeters;
        public float WorstLengthToWidthRatio;
        public float MinSaddleFraction;
        public float WorstPeakOffsetMeters;
        public bool AnyReachBound;

        /// <summary>
        /// Largest second difference of the contribution over the lattice, metres.
        /// </summary>
        /// <remarks>
        /// The continuity measure. A smooth flank has a non-zero second difference of its own - the
        /// curvature of the cross-section - so this is judged against that scale rather than against
        /// zero: the flank drops the crest height over the half-width, which on this lattice is a few
        /// centimetres per sample. Anything of that order is the surface's own curvature; anything far
        /// above it is a crease.
        /// </remarks>
        public float MaxSecondDifferenceMeters;

        /// <summary>Second difference a smooth flank of this crest and width would produce, metres.</summary>
        public float SmoothSecondDifferenceMeters;

        /// <summary>
        /// Largest step in Stage 1's signed distance between neighbouring samples, metres.
        /// </summary>
        /// <remarks>
        /// A distance field is 1-Lipschitz, so this cannot exceed the lattice spacing on an exact one.
        /// Measured because the coast gate is a function of it: whatever this field does, the massif's
        /// flank does too, multiplied by the crest over the gate width.
        /// </remarks>
        public float MaxSignedDistanceStepMeters;

        /// <summary>The lattice spacing, which bounds the step above on an exact distance field.</summary>
        public float LatticeSpacingMeters;

        /// <summary>
        /// Worst second difference the same model produces on a straight axis, metres.
        /// </summary>
        /// <remarks>
        /// The continuity decomposition's middle term. Profiles alone give
        /// <see cref="SmoothSecondDifferenceMeters"/>; the same profiles sampled on a lattice with no
        /// bending give this; the real massif gives <see cref="MaxUngatedSecondDifferenceMeters"/>; and
        /// the gated field gives <see cref="MaxSecondDifferenceMeters"/>. Each step names one source.
        /// </remarks>
        public float StraightAxisSecondDifferenceMeters;

        /// <summary>Largest axis curvature under any massif, 1/metres.</summary>
        public float MaxAxisCurvature;

        /// <summary>Worst ratio of measured half-height width to the model's own full width.</summary>
        public float MinWidthFidelity;

        /// <summary>Best ratio of measured half-height width to the model's own full width.</summary>
        public float MaxWidthFidelity;

        /// <summary>Lowest coast-gate factor over the measured stations, 0 to 1.</summary>
        public float MinStationGate;

        /// <summary>
        /// Worst second difference of the contribution before the coast gate is applied, metres.
        /// </summary>
        /// <remarks>
        /// The gate is a smoothstep of Stage 1's distance to the coast. That distance is 1-Lipschitz
        /// but its gradient turns through the coastline's medial axis, so the gate has a crease there
        /// even though the distance does not - and the massif multiplies it. Measuring the ungated
        /// body separates a defect in this stage's own shape from one it inherits by multiplication.
        /// </remarks>
        public float MaxUngatedSecondDifferenceMeters;

        public long Stage1Ms;
        public long PlatesMs;
        public long AxesMs;
        public long MassifMs;
        public long FieldMs;
        public long MetricsMs;
        public long TotalMs;

        public string ToLine()
        {
            var c = CultureInfo.InvariantCulture;
            return string.Join(" ", new[]
            {
                $"seed={Seed}",
                $"massifs={MassifCount}/{AcceptedAxes}",
                $"sheared={ShearedHighFraction.ToString("F2", c)}",
                $"uplift=[{UpliftMinMeters.ToString("F1", c)}..{UpliftMaxMeters.ToString("F1", c)}]",
                $"upliftLandMean={UpliftMeanOverLandMeters.ToString("F1", c)}",
                $"h=[{MinHeightMeters.ToString("F1", c)}..{MaxHeightMeters.ToString("F1", c)}]",
                $"width={WorstMeasuredWidthMeters.ToString("F0", c)}m",
                $"L/W={WorstLengthToWidthRatio.ToString("F2", c)}",
                $"saddle={MinSaddleFraction.ToString("F2", c)}",
                $"peakOff={WorstPeakOffsetMeters.ToString("F0", c)}m",
                $"components={UpliftComponents}",
                $"d2={MaxSecondDifferenceMeters.ToString("F3", c)}m",
                $"d2smooth={SmoothSecondDifferenceMeters.ToString("F3", c)}m",
                $"d2ungated={MaxUngatedSecondDifferenceMeters.ToString("F3", c)}m",
                $"d2straight={StraightAxisSecondDifferenceMeters.ToString("F3", c)}m",
                $"kappaMax={MaxAxisCurvature.ToString("F5", c)}",
                $"widthFidelity=[{MinWidthFidelity.ToString("F2", c)}..{MaxWidthFidelity.ToString("F2", c)}]",
                $"minGate={MinStationGate.ToString("F2", c)}",
                $"sdStep={MaxSignedDistanceStepMeters.ToString("F2", c)}m/{LatticeSpacingMeters.ToString("F2", c)}m",
                $"clamped={UpliftClampedSamples}",
                $"negative={NegativeUpliftSamples}",
                $"oceanChanged={OceanSamplesChanged}",
                $"reachBound={AnyReachBound}",
                $"stage1={Stage1Hash}",
                $"hash={FieldHash}",
                $"ms={TotalMs}",
            });
        }
    }

    public static class Stage2BMassifGenerator
    {
        /// <summary>Uplift a sample needs before it counts as part of the massif footprint, metres.</summary>
        public const float FootprintThresholdMeters = 1f;

        public static Stage2BResult Generate(
            int seed,
            TerrainGenSettings terrainSettings,
            MacroStructureSettings macroSettings,
            MassifSettings massifSettings) =>
            Generate(new Seed.WorldSeed(seed), terrainSettings, macroSettings, massifSettings);

        public static Stage2BResult Generate(int seed) =>
            Generate(
                seed,
                TerrainGenSettings.Default,
                MacroStructureSettings.Default,
                MassifSettings.Default);

        public static Stage2BResult Generate(
            Seed.IWorldSeed worldSeed,
            TerrainGenSettings terrainSettings,
            MacroStructureSettings macroSettings,
            MassifSettings massifSettings)
        {
            terrainSettings.Clamp();
            macroSettings.Clamp();
            massifSettings.Clamp();

            var total = Stopwatch.StartNew();
            var stats = new Stage2BStats
            {
                Seed = worldSeed.Value,
                GeneratorVersion = terrainSettings.GeneratorVersion,
            };

            var sw = Stopwatch.StartNew();
            var stage1 = Stage1LandmassGenerator.Generate(worldSeed, terrainSettings);
            stats.Stage1Ms = sw.ElapsedMilliseconds;

            // The macro layer is rebuilt from Stage 1's own partition and coastline rather than from a
            // second Stage2A run, which would repeat the 110 ms partition build for an identical answer.
            sw.Restart();
            var plates = MacroPlates.Build(
                worldSeed, macroSettings, stage1.Structure.Partition, stage1.Landmass.LandSites);
            stats.PlatesMs = sw.ElapsedMilliseconds;

            sw.Restart();
            var candidates = MacroAxisBuilder.Build(plates, macroSettings, stage1.Coast, out _);
            stats.AxesMs = sw.ElapsedMilliseconds;

            sw.Restart();
            var massifs = new List<MountainMassif>();
            var acceptedAxes = 0;
            for (var i = 0; i < candidates.Count; i++)
            {
                if (!candidates[i].Accepted)
                {
                    continue;
                }

                acceptedAxes++;

                // Everything this stage used to work out for itself - whether the axis was buildable,
                // where its high ground was allowed to stand, how wide a body it could carry - is
                // certified upstream now. Stage 2A does not hand over an axis it has not proved has
                // room, so there is nothing left here to decide and nothing left to go wrong: the
                // massif is built on what it was given.
                var axis = candidates[i];
                var massif = MountainMassif.Build(
                    worldSeed, massifs.Count, axis, terrainSettings, massifSettings, axis.CertifiedCore);

                // Verification, not correction. Under the contract this is zero; if it is ever not,
                // certification and the massif have stopped agreeing about the same geometry.
                stats.ShearedHighFraction = Mathf.Max(
                    stats.ShearedHighFraction, ShearedHighFraction(massif, stage1, massifSettings));

                massifs.Add(massif);
            }

            stats.MassifMs = sw.ElapsedMilliseconds;
            stats.AcceptedAxes = acceptedAxes;
            stats.MassifCount = massifs.Count;

            var upliftModel = new MassifUpliftModel(massifs, massifSettings);

            sw.Restart();
            EvaluateFields(stage1, upliftModel, out var field, out var upliftField);
            stats.FieldMs = sw.ElapsedMilliseconds;

            sw.Restart();
            var stations = MeasureStations(massifs, stage1, upliftField, massifSettings);
            var metrics = Measure(massifs, upliftField);
            FillStats(stats, stage1, field, upliftField, massifs, metrics, stations, massifSettings);
            stats.MetricsMs = sw.ElapsedMilliseconds;
            stats.TotalMs = total.ElapsedMilliseconds;

            return new Stage2BResult(
                worldSeed.Value, terrainSettings, macroSettings, massifSettings,
                stage1, plates, candidates, massifs, field, upliftField, metrics, stations, stats);
        }

        /// <summary>
        /// Adds the massif contribution to the Stage 1 field, producing the field and the uplift alone.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Only the samples inside a massif's support rectangle are visited. The massif is a small
        /// structure on a 2800 m world - its support covers a few per cent of the lattice - so
        /// sweeping the whole field to ask 97 % of it a question whose answer is zero would be the
        /// bulk of the cost for none of the result.
        /// </para>
        /// <para>
        /// Stage 1's signed distances are reused rather than recomputed. That field is the expensive
        /// quantity in this generator and it cannot have changed.
        /// </para>
        /// </remarks>
        public static void EvaluateFields(
            Stage1Result stage1,
            MassifUpliftModel uplift,
            out WorldHeightField field,
            out WorldHeightField upliftField)
        {
            var baseField = stage1.Field;
            field = WorldHeightField.ForWorld(baseField.Width);
            upliftField = WorldHeightField.ForWorld(baseField.Width);

            System.Array.Copy(baseField.Heights, field.Heights, baseField.Heights.Length);

            if (uplift == null || uplift.MassifCount == 0)
            {
                return;
            }

            var cell = baseField.CellSize;
            var support = uplift.Massifs[0].Support;
            for (var m = 1; m < uplift.MassifCount; m++)
            {
                var other = uplift.Massifs[m].Support;
                support = Rect.MinMaxRect(
                    Mathf.Min(support.xMin, other.xMin),
                    Mathf.Min(support.yMin, other.yMin),
                    Mathf.Max(support.xMax, other.xMax),
                    Mathf.Max(support.yMax, other.yMax));
            }

            var xMin = Mathf.Max(0, Mathf.FloorToInt(support.xMin / cell));
            var xMax = Mathf.Min(baseField.Width - 1, Mathf.CeilToInt(support.xMax / cell));
            var zMin = Mathf.Max(0, Mathf.FloorToInt(support.yMin / cell));
            var zMax = Mathf.Min(baseField.Height - 1, Mathf.CeilToInt(support.yMax / cell));

            for (var z = zMin; z <= zMax; z++)
            {
                var worldZ = baseField.WorldZ(z);
                var row = z * baseField.Width;

                for (var x = xMin; x <= xMax; x++)
                {
                    var index = row + x;
                    var contribution = uplift.UpliftAt(
                        baseField.WorldX(x), worldZ, stage1.SignedDistance[index]);
                    if (contribution == 0f)
                    {
                        continue;
                    }

                    upliftField.Heights[index] = contribution;
                    field.Heights[index] = baseField.Heights[index] + contribution;
                }
            }
        }

        // ---------- measurement ----------

        /// <summary>Stations a massif is measured at along its length.</summary>
        public const int StationCount = 21;

        /// <summary>
        /// Measures each massif at evenly spaced stations along its whole length.
        /// </summary>
        /// <remarks>
        /// The whole length, ends included, unlike the body average reported per massif. A width model
        /// is only trustworthy if it can be read station by station, and the ends are exactly where a
        /// taper and a truncation look alike in a single averaged number.
        /// </remarks>
        private static List<MassifStation[]> MeasureStations(
            IReadOnlyList<MountainMassif> massifs,
            Stage1Result stage1,
            WorldHeightField uplift,
            MassifSettings settings)
        {
            var all = new List<MassifStation[]>(massifs.Count);
            var gateMeters = Mathf.Max(1f, settings.CoastGateMeters);

            for (var m = 0; m < massifs.Count; m++)
            {
                var massif = massifs[m];
                var stations = new MassifStation[StationCount];

                for (var i = 0; i < StationCount; i++)
                {
                    var s = i / (StationCount - 1f);
                    var centre = massif.PointAt(s);
                    var ahead = massif.PointAt(Mathf.Min(1f, s + 0.01f));
                    var behind = massif.PointAt(Mathf.Max(0f, s - 0.01f));
                    var axis = ahead - behind;
                    var normal = axis.sqrMagnitude < 1e-8f
                        ? Vector2.up
                        : new Vector2(-axis.normalized.y, axis.normalized.x);

                    var peak = SectionPeak(uplift, centre, normal, 5f);

                    stations[i] = new MassifStation
                    {
                        S = s,
                        Position = centre,
                        Curvature = massif.CurvatureAt(s),
                        IntendedHalfWidthMeters = massif.HalfWidthAt(s),
                        IntendedCrestMeters = massif.CrestAt(s),
                        SectionPeakMeters = peak,
                        HalfHeightWidthMeters = peak > 1f
                            ? WidthAt(uplift, centre, normal, peak * 0.5f, 5f)
                            : 0f,
                        ToeWidthMeters = peak > 1f
                            ? WidthAt(uplift, centre, normal, peak * 0.1f, 5f)
                            : 0f,
                        GateFactor = GateAt(stage1, centre, gateMeters),
                    };
                }

                all.Add(stations);
            }

            return all;
        }

        /// <summary>
        /// How much of a massif's high ground stands where the coast gate cannot carry its full section.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A part-open gate does not lower a cross-section evenly: the flank nearer the water is cut
        /// while the inland one is not, so the body is sheared and its width stops being a property of
        /// the massif. Under the ends that costs nothing, because the massif is tapering to zero
        /// anyway; under the crest it is the difference between a range and a mesa, which is why this
        /// is measured on the high ground alone.
        /// </para>
        /// <para>
        /// Reported, not corrected. Four ways of correcting it inside this stage were tried and each
        /// traded one wrong shape for another - a shorter massif, a narrower one, a sheared one - and
        /// the arithmetic says why: the gate needs about 200 m to open and the body reaches 280 m past
        /// its axis, so an axis has to clear roughly 480 m of coastline to carry a full massif, while
        /// Stage 2A only guarantees 60 m. That gap is an upstream one and belongs to a Stage 2A pass.
        /// </para>
        /// </remarks>
        public static float ShearedHighFraction(
            MountainMassif massif, Stage1Result stage1, MassifSettings settings)
        {
            const int samples = 80;

            var gateMeters = Mathf.Max(1f, settings.CoastGateMeters);
            var highThreshold = massif.CrestHeightMeters * 0.5f;

            var high = 0;
            var sheared = 0;

            for (var i = 0; i <= samples; i++)
            {
                var s = i / (float)samples;
                if (massif.CrestAt(s) < highThreshold)
                {
                    continue;
                }

                high++;

                var half = massif.HalfWidthAt(s);
                var centre = massif.PointAt(s);
                var ahead = massif.PointAt(Mathf.Min(1f, s + 0.01f));
                var behind = massif.PointAt(Mathf.Max(0f, s - 0.01f));
                var direction = ahead - behind;
                var normal = direction.sqrMagnitude < 1e-8f
                    ? Vector2.up
                    : new Vector2(-direction.normalized.y, direction.normalized.x);

                var whole = GateAt(stage1, centre + normal * half, gateMeters)
                                >= settings.MinCulminationGate
                            && GateAt(stage1, centre - normal * half, gateMeters)
                                >= settings.MinCulminationGate;

                if (!whole)
                {
                    sheared++;
                }
            }

            return high > 0 ? sheared / (float)high : 0f;
        }

        /// <summary>The coast gate at a world point, read from Stage 1 signed distance.</summary>
        private static float GateAt(Stage1Result stage1, Vector2 p, float gateMeters)
        {
            var field = stage1.Field;
            var x = Mathf.Clamp(Mathf.RoundToInt(p.x / field.CellSize), 0, field.Width - 1);
            var z = Mathf.Clamp(Mathf.RoundToInt(p.y / field.CellSize), 0, field.Height - 1);
            var signed = stage1.SignedDistance[z * field.Width + x];
            if (signed <= 0f)
            {
                return 0f;
            }

            var g = Mathf.Clamp01(signed / gateMeters);
            return g * g * (3f - 2f * g);
        }

        /// <summary>Worst second difference the same massifs produce laid on straight axes.</summary>
        private static float StraightAxisSecondDifference(
            IReadOnlyList<MountainMassif> massifs, WorldHeightField frame, MassifSettings settings)
        {
            var worst = 0f;
            for (var m = 0; m < massifs.Count; m++)
            {
                var origin = new Vector2(
                    (WorldScale.WorldExtentXMeters - massifs[m].LengthMeters) * 0.5f,
                    WorldScale.WorldExtentZMeters * 0.5f);
                var straight = massifs[m].OnAStraightAxis(origin, settings);
                worst = Mathf.Max(worst, BodySecondDifference(straight, frame));
            }

            return worst;
        }

        /// <summary>Worst second difference of one massif ungated body on a lattice.</summary>
        private static float BodySecondDifference(MountainMassif massif, WorldHeightField frame)
        {
            var body = WorldHeightField.ForWorld(frame.Width);
            var support = massif.Support;
            var cell = frame.CellSize;
            var xMin = Mathf.Max(1, Mathf.FloorToInt(support.xMin / cell));
            var xMax = Mathf.Min(frame.Width - 2, Mathf.CeilToInt(support.xMax / cell));
            var zMin = Mathf.Max(1, Mathf.FloorToInt(support.yMin / cell));
            var zMax = Mathf.Min(frame.Height - 2, Mathf.CeilToInt(support.yMax / cell));

            for (var z = zMin - 1; z <= zMax + 1; z++)
            {
                if (z < 0 || z >= frame.Height)
                {
                    continue;
                }

                var worldZ = frame.WorldZ(z);
                for (var x = xMin - 1; x <= xMax + 1; x++)
                {
                    if (x < 0 || x >= frame.Width)
                    {
                        continue;
                    }

                    body.Heights[z * frame.Width + x] = massif.Evaluate(frame.WorldX(x), worldZ);
                }
            }

            var worst = 0f;
            for (var z = zMin; z <= zMax; z++)
            {
                var row = z * frame.Width;
                for (var x = xMin; x <= xMax; x++)
                {
                    var index = row + x;
                    if (body.Heights[index] < FootprintThresholdMeters)
                    {
                        continue;
                    }

                    var dx = body.Heights[index + 1] - 2f * body.Heights[index] + body.Heights[index - 1];
                    var dz = body.Heights[index + frame.Width] - 2f * body.Heights[index]
                             + body.Heights[index - frame.Width];
                    worst = Mathf.Max(worst, Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)));
                }
            }

            return worst;
        }

        /// <summary>
        /// Measures each massif from the field it produced rather than from the numbers it was given.
        /// </summary>
        /// <remarks>
        /// Width is sampled by walking outwards from the axis at a set of stations until the uplift
        /// drops below a fraction of the local crest. That is the number the acceptance question turns
        /// on - "is this broad or is it tubular" - and reading it off the settings would answer a
        /// different question.
        /// </remarks>
        private static List<MassifMetrics> Measure(
            IReadOnlyList<MountainMassif> massifs, WorldHeightField uplift)
        {
            const int stations = 24;
            const float step = 5f;

            var metrics = new List<MassifMetrics>(massifs.Count);

            for (var m = 0; m < massifs.Count; m++)
            {
                var massif = massifs[m];
                var entry = new MassifMetrics
                {
                    Index = massif.Index,
                    LengthMeters = massif.LengthMeters,
                    NominalHalfWidthMeters = massif.HalfWidthMeters,
                    ReachLimitedHalfWidthMeters = massif.ReachLimitedHalfWidthMeters,
                    ReachBound = massif.ReachBound,
                    CrestHeightMeters = massif.CrestHeightMeters,
                    CulminationCount = massif.CulminationT.Length,
                    LengthToWidthRatio = massif.LengthToWidthRatio,
                    MinHalfHeightWidthMeters = float.PositiveInfinity,
                };

                var halfSum = 0f;
                var toeSum = 0f;
                var counted = 0;

                for (var i = 0; i < stations; i++)
                {
                    // Skip the tapering ends: their width is supposed to shrink, and averaging it in
                    // would report the body as narrower than it is over the part that matters.
                    var s = Mathf.Lerp(0.2f, 0.8f, i / (float)(stations - 1));

                    var centre = massif.PointAt(s);
                    var ahead = massif.PointAt(Mathf.Min(1f, s + 0.01f));
                    var behind = massif.PointAt(Mathf.Max(0f, s - 0.01f));
                    var axis = (ahead - behind);
                    if (axis.sqrMagnitude < 1e-8f)
                    {
                        continue;
                    }

                    axis.Normalize();
                    var normal = new Vector2(-axis.y, axis.x);

                    // Measured against what this cross-section actually reaches, not against the
                    // model's crest. Near the coast the gate holds the whole section below the crest,
                    // and comparing to the crest there reports a width of zero for a body that is
                    // plainly there.
                    var crest = SectionPeak(uplift, centre, normal, step);
                    if (crest <= 1f)
                    {
                        continue;
                    }

                    var half = WidthAt(uplift, centre, normal, crest * 0.5f, step);
                    var toe = WidthAt(uplift, centre, normal, crest * 0.1f, step);

                    halfSum += half;
                    toeSum += toe;
                    counted++;

                    entry.MinHalfHeightWidthMeters = Mathf.Min(entry.MinHalfHeightWidthMeters, half);
                    entry.MaxHalfHeightWidthMeters = Mathf.Max(entry.MaxHalfHeightWidthMeters, half);
                }

                if (counted > 0)
                {
                    entry.MeasuredHalfHeightWidthMeters = halfSum / counted;
                    entry.MeasuredToeWidthMeters = toeSum / counted;
                }
                else
                {
                    entry.MinHalfHeightWidthMeters = 0f;
                }

                entry.MinSaddleFraction = MinSaddleFraction(massif);
                MeasurePeak(massif, uplift, out entry.PeakUpliftMeters, out entry.PeakOffsetMeters);
                entry.FootprintAreaSqm = FootprintArea(massif, uplift);

                metrics.Add(entry);
            }

            return metrics;
        }

        /// <summary>Highest uplift on one cross-section through the axis, metres.</summary>
        private static float SectionPeak(
            WorldHeightField uplift, Vector2 centre, Vector2 normal, float step)
        {
            var peak = SampleAt(uplift, centre.x, centre.y);
            for (var d = -600f; d <= 600f; d += step)
            {
                var p = centre + normal * d;
                peak = Mathf.Max(peak, SampleAt(uplift, p.x, p.y));
            }

            return peak;
        }

        /// <summary>Full width across the axis where the uplift stays above a level, metres.</summary>
        private static float WidthAt(
            WorldHeightField uplift, Vector2 centre, Vector2 normal, float level, float step)
        {
            var width = 0f;
            for (var side = -1; side <= 1; side += 2)
            {
                var walked = 0f;
                while (walked < 1200f)
                {
                    walked += step;
                    var p = centre + normal * (side * walked);
                    if (SampleAt(uplift, p.x, p.y) < level)
                    {
                        break;
                    }
                }

                width += walked - step;
            }

            return width;
        }

        private static float SampleAt(WorldHeightField field, float worldX, float worldZ)
        {
            var x = Mathf.RoundToInt(worldX / field.CellSize);
            var z = Mathf.RoundToInt(worldZ / field.CellSize);
            if (x < 0 || z < 0 || x >= field.Width || z >= field.Height)
            {
                return 0f;
            }

            return field.Heights[z * field.Width + x];
        }

        /// <summary>Lowest point of the longitudinal profile between its culminations, as a fraction.</summary>
        private static float MinSaddleFraction(MountainMassif massif)
        {
            if (massif.CulminationT.Length < 2)
            {
                return 1f;
            }

            var lowest = 1f;
            for (var i = 0; i + 1 < massif.CulminationT.Length; i++)
            {
                var a = massif.CulminationT[i];
                var b = massif.CulminationT[i + 1];
                var floor = 1f;
                for (var k = 0; k <= 40; k++)
                {
                    floor = Mathf.Min(floor, massif.Longitudinal.Evaluate(Mathf.Lerp(a, b, k / 40f)));
                }

                var peak = Mathf.Max(
                    massif.Longitudinal.Evaluate(a), massif.Longitudinal.Evaluate(b));
                lowest = Mathf.Min(lowest, peak > 1e-4f ? floor / peak : 0f);
            }

            return lowest;
        }

        /// <summary>Highest uplift in the massif's support, and how far off the axis it sits.</summary>
        private static void MeasurePeak(
            MountainMassif massif, WorldHeightField uplift, out float peak, out float offset)
        {
            peak = 0f;
            offset = 0f;

            var cell = uplift.CellSize;
            var support = massif.Support;
            var xMin = Mathf.Max(0, Mathf.FloorToInt(support.xMin / cell));
            var xMax = Mathf.Min(uplift.Width - 1, Mathf.CeilToInt(support.xMax / cell));
            var zMin = Mathf.Max(0, Mathf.FloorToInt(support.yMin / cell));
            var zMax = Mathf.Min(uplift.Height - 1, Mathf.CeilToInt(support.yMax / cell));

            var bestX = 0f;
            var bestZ = 0f;
            for (var z = zMin; z <= zMax; z++)
            {
                var row = z * uplift.Width;
                for (var x = xMin; x <= xMax; x++)
                {
                    var value = uplift.Heights[row + x];
                    if (value <= peak)
                    {
                        continue;
                    }

                    peak = value;
                    bestX = uplift.WorldX(x);
                    bestZ = uplift.WorldZ(z);
                }
            }

            if (peak > 0f)
            {
                massif.Project(bestX, bestZ, out _, out var signed);
                offset = Mathf.Abs(signed);
            }
        }

        private static float FootprintArea(MountainMassif massif, WorldHeightField uplift)
        {
            var cell = uplift.CellSize;
            var support = massif.Support;
            var xMin = Mathf.Max(0, Mathf.FloorToInt(support.xMin / cell));
            var xMax = Mathf.Min(uplift.Width - 1, Mathf.CeilToInt(support.xMax / cell));
            var zMin = Mathf.Max(0, Mathf.FloorToInt(support.yMin / cell));
            var zMax = Mathf.Min(uplift.Height - 1, Mathf.CeilToInt(support.yMax / cell));

            var count = 0;
            for (var z = zMin; z <= zMax; z++)
            {
                var row = z * uplift.Width;
                for (var x = xMin; x <= xMax; x++)
                {
                    if (uplift.Heights[row + x] >= FootprintThresholdMeters)
                    {
                        count++;
                    }
                }
            }

            return count * cell * cell;
        }

        // ---------- statistics ----------

        private static void FillStats(
            Stage2BStats stats,
            Stage1Result stage1,
            WorldHeightField field,
            WorldHeightField uplift,
            IReadOnlyList<MountainMassif> massifs,
            IReadOnlyList<MassifMetrics> metrics,
            IReadOnlyList<MassifStation[]> stations,
            MassifSettings settings)
        {
            var baseField = stage1.Field;
            var count = field.Heights.Length;

            stats.Stage1Hash = stage1.Stats.FieldHash;
            stats.Stage1MinHeightMeters = stage1.Stats.MinHeightMeters;
            stats.Stage1MaxHeightMeters = stage1.Stats.MaxHeightMeters;
            stats.Stage1MeanHeightMeters = stage1.Stats.MeanHeightMeters;

            stats.UpliftMinMeters = float.PositiveInfinity;
            stats.MinHeightMeters = float.PositiveInfinity;
            stats.MaxHeightMeters = float.NegativeInfinity;

            var upliftSum = 0.0;
            var heightSum = 0.0;
            var landUpliftSum = 0.0;
            var landSamples = 0;

            for (var i = 0; i < count; i++)
            {
                var contribution = uplift.Heights[i];
                var height = field.Heights[i];

                if (float.IsNaN(height) || float.IsInfinity(height)
                    || float.IsNaN(contribution) || float.IsInfinity(contribution))
                {
                    stats.HasNonFinite = true;
                }

                if (contribution < 0f)
                {
                    stats.NegativeUpliftSamples++;
                }

                if (contribution >= settings.MaxUpliftMeters - 1e-3f)
                {
                    stats.UpliftClampedSamples++;
                }

                if (contribution >= FootprintThresholdMeters)
                {
                    stats.UpliftSamples++;
                }

                stats.UpliftMinMeters = Mathf.Min(stats.UpliftMinMeters, contribution);
                stats.UpliftMaxMeters = Mathf.Max(stats.UpliftMaxMeters, contribution);
                stats.MinHeightMeters = Mathf.Min(stats.MinHeightMeters, height);
                stats.MaxHeightMeters = Mathf.Max(stats.MaxHeightMeters, height);

                upliftSum += contribution;
                heightSum += height;

                if (stage1.SignedDistance[i] > 0f)
                {
                    landUpliftSum += contribution;
                    landSamples++;
                }
                else if (height != baseField.Heights[i])
                {
                    stats.OceanSamplesChanged++;
                }
            }

            stats.UpliftMeanMeters = (float)(upliftSum / count);
            stats.MeanHeightMeters = (float)(heightSum / count);
            stats.UpliftMeanOverLandMeters =
                landSamples > 0 ? (float)(landUpliftSum / landSamples) : 0f;

            stats.LatticeSpacingMeters = uplift.CellSize;
            stats.MaxSignedDistanceStepMeters = MaxNeighbourStep(stage1.SignedDistance, uplift.Width, uplift.Height);
            stats.UpliftComponents = CountComponents(uplift);
            stats.MaxSecondDifferenceMeters = MaxSecondDifference(uplift);
            stats.MaxUngatedSecondDifferenceMeters = MaxUngatedSecondDifference(massifs, uplift);
            stats.StraightAxisSecondDifferenceMeters =
                StraightAxisSecondDifference(massifs, uplift, settings);

            stats.MinWidthFidelity = float.PositiveInfinity;
            stats.MinStationGate = 1f;
            for (var m = 0; m < stations.Count; m++)
            {
                stats.MaxAxisCurvature = Mathf.Max(stats.MaxAxisCurvature, massifs[m].MaxCurvature());
                for (var i = 0; i < stations[m].Length; i++)
                {
                    var station = stations[m][i];

                    // The ends are meant to narrow, so fidelity is judged over the body only.
                    if (station.S < 0.2f || station.S > 0.8f || station.SectionPeakMeters <= 1f)
                    {
                        continue;
                    }

                    var fidelity = station.HalfHeightWidthMeters
                                   / Mathf.Max(1f, station.IntendedHalfWidthMeters * 2f);
                    stats.MinWidthFidelity = Mathf.Min(stats.MinWidthFidelity, fidelity);
                    stats.MaxWidthFidelity = Mathf.Max(stats.MaxWidthFidelity, fidelity);
                    stats.MinStationGate = Mathf.Min(stats.MinStationGate, station.GateFactor);
                }
            }

            if (float.IsPositiveInfinity(stats.MinWidthFidelity))
            {
                stats.MinWidthFidelity = 0f;
            }
            stats.FieldHash = field.Hash();

            for (var i = 0; i < massifs.Count; i++)
            {
                stats.SmoothSecondDifferenceMeters = Mathf.Max(
                    stats.SmoothSecondDifferenceMeters,
                    massifs[i].ProfileSecondDifferenceMeters(uplift.CellSize));
            }

            stats.WorstMeasuredWidthMeters = float.PositiveInfinity;
            stats.WorstLengthToWidthRatio = float.PositiveInfinity;
            stats.MinSaddleFraction = 1f;

            for (var i = 0; i < metrics.Count; i++)
            {
                stats.WorstMeasuredWidthMeters = Mathf.Min(
                    stats.WorstMeasuredWidthMeters, metrics[i].MeasuredHalfHeightWidthMeters);
                stats.WorstLengthToWidthRatio = Mathf.Min(
                    stats.WorstLengthToWidthRatio, metrics[i].LengthToWidthRatio);
                stats.MinSaddleFraction = Mathf.Min(
                    stats.MinSaddleFraction, metrics[i].MinSaddleFraction);
                stats.WorstPeakOffsetMeters = Mathf.Max(
                    stats.WorstPeakOffsetMeters, metrics[i].PeakOffsetMeters);
                stats.AnyReachBound |= metrics[i].ReachBound;
            }

            if (metrics.Count == 0)
            {
                stats.WorstMeasuredWidthMeters = 0f;
                stats.WorstLengthToWidthRatio = 0f;
            }

            if (massifs.Count == 0)
            {
                stats.UpliftMinMeters = 0f;
                stats.UpliftMaxMeters = 0f;
            }
        }

        /// <summary>Worst second difference of the massif bodies with the coast gate left off.</summary>
        private static float MaxUngatedSecondDifference(
            IReadOnlyList<MountainMassif> massifs, WorldHeightField frame)
        {
            if (massifs.Count == 0)
            {
                return 0f;
            }

            var ungated = WorldHeightField.ForWorld(frame.Width);
            for (var m = 0; m < massifs.Count; m++)
            {
                var massif = massifs[m];
                var support = massif.Support;
                var cell = frame.CellSize;
                var xMin = Mathf.Max(0, Mathf.FloorToInt(support.xMin / cell));
                var xMax = Mathf.Min(frame.Width - 1, Mathf.CeilToInt(support.xMax / cell));
                var zMin = Mathf.Max(0, Mathf.FloorToInt(support.yMin / cell));
                var zMax = Mathf.Min(frame.Height - 1, Mathf.CeilToInt(support.yMax / cell));

                for (var z = zMin; z <= zMax; z++)
                {
                    var worldZ = frame.WorldZ(z);
                    for (var x = xMin; x <= xMax; x++)
                    {
                        var index = z * frame.Width + x;
                        ungated.Heights[index] = Mathf.Max(
                            ungated.Heights[index], massif.Evaluate(frame.WorldX(x), worldZ));
                    }
                }
            }

            var worst = 0f;
            for (var z = 1; z + 1 < ungated.Height; z++)
            {
                var row = z * ungated.Width;
                for (var x = 1; x + 1 < ungated.Width; x++)
                {
                    var index = row + x;
                    if (ungated.Heights[index] < FootprintThresholdMeters)
                    {
                        continue;
                    }

                    var dx = ungated.Heights[index + 1] - 2f * ungated.Heights[index]
                             + ungated.Heights[index - 1];
                    var dz = ungated.Heights[index + ungated.Width] - 2f * ungated.Heights[index]
                             + ungated.Heights[index - ungated.Width];
                    worst = Mathf.Max(worst, Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)));
                }
            }

            return worst;
        }

        /// <summary>Largest change between four-connected neighbours of a lattice field.</summary>
        public static float MaxNeighbourStep(float[] values, int width, int height)
        {
            var worst = 0f;
            for (var z = 0; z < height; z++)
            {
                var row = z * width;
                for (var x = 0; x < width; x++)
                {
                    var here = values[row + x];
                    if (x + 1 < width)
                    {
                        worst = Mathf.Max(worst, Mathf.Abs(values[row + x + 1] - here));
                    }

                    if (z + 1 < height)
                    {
                        worst = Mathf.Max(worst, Mathf.Abs(values[row + width + x] - here));
                    }
                }
            }

            return worst;
        }

        /// <summary>Largest second difference of a field, over samples that carry contribution.</summary>
        public static float MaxSecondDifference(WorldHeightField field)
        {
            var worst = 0f;
            for (var z = 1; z + 1 < field.Height; z++)
            {
                var row = z * field.Width;
                for (var x = 1; x + 1 < field.Width; x++)
                {
                    var index = row + x;
                    if (field.Heights[index] < FootprintThresholdMeters)
                    {
                        continue;
                    }

                    var dx = field.Heights[index + 1] - 2f * field.Heights[index]
                             + field.Heights[index - 1];
                    var dz = field.Heights[index + field.Width] - 2f * field.Heights[index]
                             + field.Heights[index - field.Width];

                    worst = Mathf.Max(worst, Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)));
                }
            }

            return worst;
        }

        /// <summary>
        /// Connected groups of samples carrying uplift, four-connected.
        /// </summary>
        /// <remarks>
        /// The blob detector. One massif has to be one component; a count above the massif count means
        /// the contribution broke into islands somewhere, which is the failure mode a smooth-looking
        /// profile can still hide.
        /// </remarks>
        public static int CountComponents(WorldHeightField uplift)
        {
            var width = uplift.Width;
            var height = uplift.Height;
            var seen = new bool[width * height];
            var stack = new Stack<int>(1024);
            var components = 0;

            for (var start = 0; start < seen.Length; start++)
            {
                if (seen[start] || uplift.Heights[start] < FootprintThresholdMeters)
                {
                    continue;
                }

                components++;
                seen[start] = true;
                stack.Push(start);

                while (stack.Count > 0)
                {
                    var index = stack.Pop();
                    var x = index % width;
                    var z = index / width;

                    if (x > 0)
                    {
                        Visit(index - 1);
                    }

                    if (x + 1 < width)
                    {
                        Visit(index + 1);
                    }

                    if (z > 0)
                    {
                        Visit(index - width);
                    }

                    if (z + 1 < height)
                    {
                        Visit(index + width);
                    }
                }
            }

            return components;

            void Visit(int neighbour)
            {
                if (seen[neighbour] || uplift.Heights[neighbour] < FootprintThresholdMeters)
                {
                    return;
                }

                seen[neighbour] = true;
                stack.Push(neighbour);
            }
        }

        // ---------- reporting ----------

        /// <summary>Formats a metre value that is allowed to be unbounded.</summary>
        /// <remarks>
        /// An unbent axis has no measurable reach, so the width its curvature permits is unbounded.
        /// Printing the finite number the arithmetic happened to produce for that - six million metres
        /// on seed 1337 - measured nothing, and a diagnostic that prints absurd geometry teaches the
        /// reader to stop trusting the ones next to it.
        /// </remarks>
        private static string Unbounded(float meters, CultureInfo culture) =>
            float.IsPositiveInfinity(meters) ? "inf" : meters.ToString("F0", culture);

        public static string Describe(Stage2BResult result)
        {
            var c = CultureInfo.InvariantCulture;
            var s = result.Stats;
            var metrics = result.Metrics;
            var sb = new StringBuilder();

            sb.AppendLine($"seed {result.Seed}  (generator version {s.GeneratorVersion})");
            sb.AppendLine(
                $"  massifs    {s.MassifCount} on {s.AcceptedAxes} accepted axis/axes, "
                + $"{s.UpliftComponents} connected uplift component(s); "
                + $"{(s.ShearedHighFraction * 100f).ToString("F0", c)} % of the high ground stands in "
                + "a gate too shallow for its own section (0 under the contract)");
            sb.AppendLine(
                $"  uplift     [{s.UpliftMinMeters.ToString("F1", c)} .. "
                + $"{s.UpliftMaxMeters.ToString("F1", c)}] m, "
                + $"mean over land {s.UpliftMeanOverLandMeters.ToString("F1", c)} m, "
                + $"clamped {s.UpliftClampedSamples}, negative {s.NegativeUpliftSamples}");
            sb.AppendLine(
                $"  stage1->2B h [{s.Stage1MinHeightMeters.ToString("F1", c)} .. "
                + $"{s.Stage1MaxHeightMeters.ToString("F1", c)}] -> "
                + $"[{s.MinHeightMeters.ToString("F1", c)} .. {s.MaxHeightMeters.ToString("F1", c)}] m, "
                + $"mean {s.Stage1MeanHeightMeters.ToString("F1", c)} -> "
                + $"{s.MeanHeightMeters.ToString("F1", c)}");
            sb.AppendLine(
                $"  invariants ocean samples changed {s.OceanSamplesChanged} (must be 0), "
                + $"non-finite {s.HasNonFinite}, reach clamp bound {s.AnyReachBound} (must be False)");
            sb.AppendLine(
                $"  continuity worst second difference {s.MaxSecondDifferenceMeters.ToString("F3", c)} m "
                + $"against {s.SmoothSecondDifferenceMeters.ToString("F3", c)} m for a smooth flank "
                + $"for the profiles themselves");
            sb.AppendLine(
                $"             decomposition: profiles {s.SmoothSecondDifferenceMeters.ToString("F3", c)}"
                + $" -> straight axis {s.StraightAxisSecondDifferenceMeters.ToString("F3", c)}"
                + $" -> curved axis {s.MaxUngatedSecondDifferenceMeters.ToString("F3", c)}"
                + $" -> gated {s.MaxSecondDifferenceMeters.ToString("F3", c)} m; "
                + $"max curvature {s.MaxAxisCurvature.ToString("F5", c)} 1/m "
                + $"(radius {(s.MaxAxisCurvature > 1e-6f ? 1f / s.MaxAxisCurvature : 0f).ToString("F0", c)} m)");
            sb.AppendLine(
                $"  width      fidelity over the body "
                + $"[{s.MinWidthFidelity.ToString("F2", c)} .. {s.MaxWidthFidelity.ToString("F2", c)}], "
                + $"lowest station gate {s.MinStationGate.ToString("F2", c)}");
            sb.AppendLine($"  hashes     stage1 {s.Stage1Hash}   stage2B {s.FieldHash}");
            sb.AppendLine(
                $"  timing     stage1 {s.Stage1Ms} ms, plates {s.PlatesMs} ms, axes {s.AxesMs} ms, "
                + $"massif {s.MassifMs} ms, field {s.FieldMs} ms, metrics {s.MetricsMs} ms, "
                + $"total {s.TotalMs} ms");

            sb.AppendLine("  massif table:");
            sb.AppendLine(
                "    idx  len_m  nomW_m  reachW_m  crest_m  culms  halfWidth_m  toeWidth_m  "
                + "saddle  peak_m  peakOff_m  L/W  area_km2");

            for (var i = 0; i < metrics.Count; i++)
            {
                var m = metrics[i];
                sb.AppendLine(
                    $"    {m.Index,3}  {m.LengthMeters.ToString("F0", c),5}  "
                    + $"{m.NominalHalfWidthMeters.ToString("F0", c),6}  "
                    + $"{Unbounded(m.ReachLimitedHalfWidthMeters, c),8}  "
                    + $"{m.CrestHeightMeters.ToString("F0", c),7}  {m.CulminationCount,5}  "
                    + $"{m.MeasuredHalfHeightWidthMeters.ToString("F0", c),11}  "
                    + $"{m.MeasuredToeWidthMeters.ToString("F0", c),10}  "
                    + $"{m.MinSaddleFraction.ToString("F2", c),6}  "
                    + $"{m.PeakUpliftMeters.ToString("F0", c),6}  "
                    + $"{m.PeakOffsetMeters.ToString("F0", c),9}  "
                    + $"{m.LengthToWidthRatio.ToString("F2", c),4}  "
                    + $"{(m.FootprintAreaSqm / 1e6f).ToString("F2", c),8}");
                sb.AppendLine(
                    $"         half-height width range [{m.MinHalfHeightWidthMeters.ToString("F0", c)} .. "
                    + $"{m.MaxHalfHeightWidthMeters.ToString("F0", c)}] m");
            }

            for (var m = 0; m < result.Stations.Count; m++)
            {
                sb.AppendLine($"  massif {m} stations:");
                sb.AppendLine(
                    "      s   x      z     kappa_1/m  intendW_m  intendH_m  peak_m  halfW_m  toeW_m  gate  fidelity");

                var rows = result.Stations[m];
                for (var i = 0; i < rows.Length; i++)
                {
                    var r = rows[i];
                    var fidelity = r.IntendedHalfWidthMeters > 1f
                        ? r.HalfHeightWidthMeters / (r.IntendedHalfWidthMeters * 2f)
                        : 0f;
                    sb.AppendLine(
                        $"   {r.S.ToString("F2", c),4}  {r.Position.x.ToString("F0", c),5}  "
                        + $"{r.Position.y.ToString("F0", c),5}  {r.Curvature.ToString("F5", c),9}  "
                        + $"{r.IntendedHalfWidthMeters.ToString("F0", c),9}  "
                        + $"{r.IntendedCrestMeters.ToString("F0", c),9}  "
                        + $"{r.SectionPeakMeters.ToString("F0", c),6}  "
                        + $"{r.HalfHeightWidthMeters.ToString("F0", c),7}  "
                        + $"{r.ToeWidthMeters.ToString("F0", c),6}  "
                        + $"{r.GateFactor.ToString("F2", c),4}  {fidelity.ToString("F2", c),8}");
                }
            }

            return sb.ToString();
        }
    }
}
