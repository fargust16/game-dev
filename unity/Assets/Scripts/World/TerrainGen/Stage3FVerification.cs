using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>What a local low along a valley profile turns out to be.</summary>
    public enum DrainageState
    {
        /// <summary>Not deep enough to qualify, or the geometry cannot answer.</summary>
        Ambiguous = 0,

        /// <summary>Nothing downstream stands above it: water leaves along the valley.</summary>
        Drained,

        /// <summary>The centreline is blocked but the valley floor across its width is not.</summary>
        DrainableWithinTrajectory,

        /// <summary>Blocked on the centreline and across the whole valley floor.</summary>
        Closed,

        /// <summary>At the trajectory's own end; there is no declared downstream to reach.</summary>
        Terminal,

        /// <summary>Inside a Stage 3E declared basin, which is entitled to hold a hollow.</summary>
        BasinOwned,
    }

    /// <summary>One local low on a trajectory's finished-ground profile.</summary>
    public readonly struct DrainageFinding
    {
        public int TrajectoryId { get; }

        /// <summary>Arc length of the low, metres.</summary>
        public float ArcMeters { get; }

        public Vector2 Position { get; }

        /// <summary>Finished ground at the low, metres.</summary>
        public float FloorMeters { get; }

        /// <summary>
        /// The spill point: the highest finished ground that has to be crossed to reach anywhere lower.
        /// </summary>
        public float BarrierMeters { get; }

        /// <summary>Whether the profile reaches ground below this low at all, barrier or not.</summary>
        public bool ReachesLowerGround { get; }

        /// <summary>Arc length of that barrier, metres.</summary>
        public float BarrierArcMeters { get; }

        /// <summary>How far the barrier stands above the low, metres. Zero when drained.</summary>
        public float DepthMeters { get; }

        /// <summary>Lowest finished ground across the valley at the barrier, metres.</summary>
        public float BarrierFloorAcrossMeters { get; }

        public DrainageState State { get; }

        /// <summary>Declared basin the low falls inside, or -1.</summary>
        public int BasinId { get; }

        /// <summary>
        /// True when the low sits past the massif foot, on the island floor the trough continues across.
        /// </summary>
        /// <remarks>
        /// Worth separating: a low above the toe is on the massif flank, where the carve's mountain term
        /// and Stage 4A's apron both swing hardest, and it is a different landform question from one out
        /// on the floor.
        /// </remarks>
        public bool BelowToe { get; }

        public DrainageFinding(
            int trajectoryId, float arcMeters, Vector2 position, float floorMeters, float barrierMeters,
            float barrierArcMeters, float depthMeters, float barrierFloorAcrossMeters, DrainageState state,
            int basinId, bool belowToe, bool reachesLowerGround)
        {
            BelowToe = belowToe;
            ReachesLowerGround = reachesLowerGround;
            TrajectoryId = trajectoryId;
            ArcMeters = arcMeters;
            Position = position;
            FloorMeters = floorMeters;
            BarrierMeters = barrierMeters;
            BarrierArcMeters = barrierArcMeters;
            DepthMeters = depthMeters;
            BarrierFloorAcrossMeters = barrierFloorAcrossMeters;
            State = state;
            BasinId = basinId;
        }
    }

    /// <summary>One trajectory's finished-ground profile, and what it says about drainage.</summary>
    public sealed class TrajectoryProfile
    {
        public int TrajectoryId { get; internal set; }

        public ValleyTermination Termination { get; internal set; }

        public float LengthMeters { get; internal set; }

        public float HalfWidthMeters { get; internal set; }

        /// <summary>Arc length of each sample, metres.</summary>
        public float[] Arc { get; internal set; }

        public Vector2[] Position { get; internal set; }

        /// <summary>Stage 2 structural ground, <c>H2 = H1 + U2</c>.</summary>
        public float[] H2 { get; internal set; }

        /// <summary>Stage 4A ground, <c>H4A</c>.</summary>
        public float[] H4A { get; internal set; }

        /// <summary>Finished ground under test, <c>H3F</c>.</summary>
        public float[] H3F { get; internal set; }

        /// <summary>Lowest finished ground across the valley floor at each sample, metres.</summary>
        public float[] AcrossMin { get; internal set; }

        /// <summary>
        /// <c>H4A − H3F</c>, the carve the finished surface actually shows. Derived from the two
        /// surfaces, never read from the carve model.
        /// </summary>
        public float[] Carve { get; internal set; }

        /// <summary>Stage 4A's own relief, <c>R4A = H4A − H2</c>.</summary>
        public float[] Relief { get; internal set; }

        /// <summary>`max(H3F − running min H3F)`, metres. The finished ground's adverse rise.</summary>
        public float AdverseRiseFinished { get; internal set; }

        /// <summary>Arc length where that rise is worst, metres.</summary>
        public float AdverseRiseArcMeters { get; internal set; }

        /// <summary>`max(H4A − running min H4A)`, metres. The same measure before the carve.</summary>
        public float AdverseRiseIsland { get; internal set; }

        /// <summary>
        /// `max(R4A − running min R4A)` over <b>toe → end</b>, metres — Stage 4A's S11-T reproduced on
        /// these samples, so the three quantities can be compared rather than argued about.
        /// </summary>
        /// <remarks>
        /// The range matters: S11-T starts at <c>ToeIndex</c>, not at the head. Measured over the whole
        /// arc the same expression reaches 22–39 m on the canonical seeds, because the head sits on the
        /// massif crest where the apron and plate terms swing hardest — that is
        /// <see cref="ReliefRiseFullArc"/>, and it is not what S11-T bounds.
        /// </remarks>
        public float S11TClimb { get; internal set; }

        /// <summary>`max(R4A − running min R4A)` over the whole arc. Not S11-T; reported for contrast.</summary>
        public float ReliefRiseFullArc { get; internal set; }

        /// <summary>The finished ground's adverse rise over toe → end, metres.</summary>
        public float AdverseRiseFinishedFromToe { get; internal set; }

        /// <summary>Arc length at the massif foot, metres.</summary>
        public float ToeArcMeters { get; internal set; }

        public int LocalMinimaCount { get; internal set; }

        public int LocalMaximaCount { get; internal set; }

        public float MaxCarveMeters { get; internal set; }

        /// <summary>Samples where the finished ground stood above the island it was cut from.</summary>
        public int CompositionViolations { get; internal set; }

        public List<DrainageFinding> Findings { get; } = new List<DrainageFinding>();
    }

    /// <summary>
    /// Stage 3F F3: an independent read of what the finished ground does along the declared valleys.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Independent on purpose.</b> Nothing here consults <see cref="CarveField"/>, its profiles, its
    /// budget or its bucket index. It samples two surfaces — <see cref="IslandElevation"/> for the
    /// island and whatever the caller hands it for the finished ground — and derives the carve as the
    /// difference between them. If the carve model were wrong, this would still describe the ground
    /// correctly, which is the whole point of a verification pass: it has to be able to fail the thing
    /// it is checking. The one primitive it shares is
    /// <see cref="FinalValleyTrajectory.Project"/>, because the question is about the declared valley
    /// and that is what declares it.
    /// </para>
    /// <para>
    /// <b>It measures, it does not judge.</b> A local low is reported with its barrier, its cross-valley
    /// escape and its basin ownership; the classification says what the geometry shows and says
    /// <see cref="DrainageState.Ambiguous"/> where it does not show enough.
    /// </para>
    /// </remarks>
    public static class Stage3FVerification
    {
        /// <summary>
        /// Arc-length sampling step, metres. Stage 3B's own spur sampling, so the profile is read at
        /// the spacing the geometry was built at rather than at a number invented here.
        /// </summary>
        public static float StepMeters(RidgeSettings settings) =>
            Mathf.Max(1f, settings.SpurSampleSpacingMeters);

        /// <summary>
        /// A low counts when its barrier stands this far above it, metres.
        /// </summary>
        /// <remarks>
        /// Borrowed from Stage 4A's S8 depression definition rather than chosen here, so the two
        /// measures can be read side by side. S8 also requires 100 m² of area, which a one-dimensional
        /// profile cannot answer; the areal count stays with the field scan.
        /// </remarks>
        public const float QualifyingDepthMeters = 0.25f;

        /// <summary>Offsets across the valley, as fractions of the half-width.</summary>
        public static readonly float[] CrossSectionFractions = { -1f, -0.5f, -0.25f, 0f, 0.25f, 0.5f, 1f };

        /// <summary>A low within this distance of the trajectory end is terminal, metres.</summary>
        public const float TerminalMarginMeters = 30f;

        /// <summary>Height at a world point, in metres.</summary>
        public delegate float Surface(float worldX, float worldZ);

        /// <summary>
        /// Reads one trajectory's finished-ground profile and classifies every local low on it.
        /// </summary>
        /// <param name="finished">The surface under test, <c>H3F</c>.</param>
        /// <param name="basins">Declared Stage 3E basins, or null.</param>
        public static TrajectoryProfile Profile(
            FinalValleyTrajectory trajectory,
            Stage1Result stage1,
            MassifUpliftModel uplift,
            IslandRelief relief,
            Surface finished,
            IReadOnlyList<StructuralBasin> basins,
            RidgeSettings settings)
        {
            var profile = new TrajectoryProfile
            {
                TrajectoryId = trajectory.Id,
                Termination = trajectory.Termination,
                LengthMeters = trajectory.LengthMeters,
                HalfWidthMeters = trajectory.HalfWidthMeters,
            };

            if (!trajectory.HasGeometry || trajectory.LengthMeters <= 1e-3f)
            {
                profile.Arc = System.Array.Empty<float>();
                profile.Position = System.Array.Empty<Vector2>();
                profile.H2 = profile.H4A = profile.H3F = profile.Carve = profile.Relief =
                    profile.AcrossMin = System.Array.Empty<float>();
                return profile;
            }

            var step = StepMeters(settings);
            var count = Mathf.Max(2, Mathf.CeilToInt(trajectory.LengthMeters / step) + 1);
            profile.Arc = new float[count];
            profile.Position = new Vector2[count];
            profile.H2 = new float[count];
            profile.H4A = new float[count];
            profile.H3F = new float[count];
            profile.Carve = new float[count];
            profile.Relief = new float[count];
            profile.AcrossMin = new float[count];

            for (var i = 0; i < count; i++)
            {
                var s = Mathf.Min(trajectory.LengthMeters, i * step);
                var p = PointAtArc(trajectory, s, out var tangent);
                profile.Arc[i] = s;
                profile.Position[i] = p;

                var h2 = StructuralElevation.At(stage1, uplift, p.x, p.y);
                var h4a = IslandElevation.At(stage1, uplift, relief, p.x, p.y);
                var h3f = finished(p.x, p.y);

                profile.H2[i] = h2;
                profile.H4A[i] = h4a;
                profile.H3F[i] = h3f;
                profile.Relief[i] = h4a - h2;
                profile.Carve[i] = h4a - h3f;
                if (h3f > h4a + 1e-3f)
                {
                    profile.CompositionViolations++;
                }

                profile.MaxCarveMeters = Mathf.Max(profile.MaxCarveMeters, h4a - h3f);
                profile.AcrossMin[i] = AcrossMinimum(p, tangent, trajectory.HalfWidthMeters, finished);
            }

            Running(profile.H3F, 0, out var riseFinished, out var riseIndex);
            Running(profile.H4A, 0, out var riseIsland, out _);
            Running(profile.Relief, 0, out var fullArcClimb, out _);
            profile.AdverseRiseFinished = riseFinished;
            profile.AdverseRiseArcMeters = profile.Arc[riseIndex];
            profile.AdverseRiseIsland = riseIsland;
            profile.ReliefRiseFullArc = fullArcClimb;

            // S11-T starts at the toe, not the head. Reproduced over the same range so the number here
            // can be checked against Stage 4A's own rather than resembling it.
            profile.ToeArcMeters = trajectory.ToeArcMeters;
            var toe = 0;
            while (toe < profile.Arc.Length - 1 && profile.Arc[toe] < trajectory.ToeArcMeters)
            {
                toe++;
            }

            Running(profile.Relief, toe, out var climbFromToe, out _);
            Running(profile.H3F, toe, out var finishedFromToe, out _);
            profile.S11TClimb = climbFromToe;
            profile.AdverseRiseFinishedFromToe = finishedFromToe;

            Classify(profile, trajectory, basins);
            return profile;
        }

        /// <summary>World point at an arc length, with the direction of travel there.</summary>
        private static Vector2 PointAtArc(FinalValleyTrajectory trajectory, float arc, out Vector2 tangent)
        {
            var points = trajectory.Points;
            var cumulative = trajectory.Cumulative;
            var last = points.Length - 1;
            if (arc <= 0f)
            {
                tangent = Direction(points[Mathf.Min(1, last)] - points[0]);
                return points[0];
            }

            if (arc >= cumulative[last])
            {
                tangent = Direction(points[last] - points[Mathf.Max(0, last - 1)]);
                return points[last];
            }

            var i = 0;
            while (i + 1 < points.Length && cumulative[i + 1] < arc)
            {
                i++;
            }

            var span = cumulative[i + 1] - cumulative[i];
            var t = span > 1e-6f ? (arc - cumulative[i]) / span : 0f;
            tangent = Direction(points[i + 1] - points[i]);
            return Vector2.Lerp(points[i], points[i + 1], t);
        }

        private static Vector2 Direction(Vector2 v) => v.sqrMagnitude > 1e-10f ? v.normalized : Vector2.right;

        /// <summary>Lowest finished ground across the valley floor at one station.</summary>
        private static float AcrossMinimum(Vector2 p, Vector2 tangent, float halfWidth, Surface finished)
        {
            var normal = new Vector2(-tangent.y, tangent.x);
            var best = float.PositiveInfinity;
            for (var k = 0; k < CrossSectionFractions.Length; k++)
            {
                var q = p + normal * (CrossSectionFractions[k] * halfWidth);
                best = Mathf.Min(best, finished(q.x, q.y));
            }

            return best;
        }

        /// <summary>`max(v − running min v)` from a starting sample, and where it happens.</summary>
        private static void Running(float[] v, int from, out float rise, out int index)
        {
            rise = 0f;
            index = Mathf.Clamp(from, 0, Mathf.Max(0, v.Length - 1));
            if (v.Length == 0 || from >= v.Length)
            {
                return;
            }

            var min = v[from];
            for (var i = from + 1; i < v.Length; i++)
            {
                min = Mathf.Min(min, v[i]);
                var r = v[i] - min;
                if (r > rise)
                {
                    rise = r;
                    index = i;
                }
            }
        }

        /// <summary>
        /// Every local low, with the barrier standing between it and the rest of the valley.
        /// </summary>
        /// <remarks>
        /// The barrier is the highest finished ground downstream of the low before the profile falls
        /// back below it. A low the profile never gets back under is barred by the highest ground
        /// remaining, which is what makes the end of a trajectory a different case from the middle.
        /// </remarks>
        private static void Classify(
            TrajectoryProfile profile, FinalValleyTrajectory trajectory, IReadOnlyList<StructuralBasin> basins)
        {
            var h = profile.H3F;
            for (var i = 1; i < h.Length - 1; i++)
            {
                if (h[i] > h[i - 1] || h[i] > h[i + 1])
                {
                    if (h[i] > h[i - 1] && h[i] > h[i + 1])
                    {
                        profile.LocalMaximaCount++;
                    }

                    continue;
                }

                if (h[i] == h[i - 1] && h[i] == h[i + 1])
                {
                    continue;
                }

                profile.LocalMinimaCount++;

                // The spill point is the highest ground that has to be crossed to reach anywhere lower.
                // Lower ground existing further downstream is not drainage on its own: water at the low
                // has to get over whatever stands between, so the barrier is tracked up to the first
                // sample that actually falls below the low, and it is the barrier that decides.
                var barrier = h[i];
                var barrierIndex = i;
                var reachesLowerGround = false;
                for (var j = i + 1; j < h.Length; j++)
                {
                    if (h[j] < h[i] - 1e-4f)
                    {
                        reachesLowerGround = true;
                        break;
                    }

                    if (h[j] > barrier)
                    {
                        barrier = h[j];
                        barrierIndex = j;
                    }
                }

                var depth = barrier - h[i];
                var basinId = BasinAt(profile.Position[i], basins);
                var acrossAtBarrier = profile.AcrossMin[barrierIndex];

                DrainageState state;
                if (basinId >= 0)
                {
                    state = DrainageState.BasinOwned;
                }
                else if (depth < QualifyingDepthMeters)
                {
                    // Nothing worth crossing stands in the way, so the valley runs downhill from here
                    // whether or not it ever reaches lower ground inside this trajectory.
                    state = reachesLowerGround || profile.LengthMeters - profile.Arc[i] > TerminalMarginMeters
                        ? DrainageState.Drained
                        : DrainageState.Terminal;
                }
                else if (profile.LengthMeters - profile.Arc[i] <= TerminalMarginMeters)
                {
                    state = DrainageState.Terminal;
                }
                else if (acrossAtBarrier <= h[i] + 1e-4f)
                {
                    state = DrainageState.DrainableWithinTrajectory;
                }
                else
                {
                    state = DrainageState.Closed;
                }

                profile.Findings.Add(new DrainageFinding(
                    trajectory.Id, profile.Arc[i], profile.Position[i], h[i], barrier,
                    profile.Arc[barrierIndex], depth, acrossAtBarrier, state, basinId,
                    profile.Arc[i] >= profile.ToeArcMeters, reachesLowerGround));
            }
        }

        /// <summary>The declared basin containing a point, or -1.</summary>
        public static int BasinAt(Vector2 p, IReadOnlyList<StructuralBasin> basins)
        {
            if (basins == null)
            {
                return -1;
            }

            for (var i = 0; i < basins.Count; i++)
            {
                var boundary = basins[i].Boundary;
                if (boundary != null && boundary.Length >= 3 && Town.Geometry.Geo2.PointInPolygon(p, boundary))
                {
                    return basins[i].Id;
                }
            }

            return -1;
        }
    }
}
