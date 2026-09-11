using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Turns a macro plate partition into candidate mountain axes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The chain is grown along convergent plate boundaries, rounded into an arc-chain with a named
    /// radius, clipped to the land Stage 1 produced, and only then judged. Nothing here reads the fine
    /// crustal boundaries: an axis is the bisector of two points a kilometre apart, filleted, and the
    /// only thing Stage 1 contributes is where the water is.
    /// </para>
    /// <para>
    /// Discovery is separated from acceptance for the same reason it is in
    /// <see cref="UpliftBeltBuilder"/>: growing greedily from the first eligible boundary makes the
    /// result depend on which boundary happened to be first, and the longest chain in the world is
    /// rarely the one that starts there.
    /// </para>
    /// </remarks>
    public static class MacroAxisBuilder
    {
        /// <summary>Half-widths the corridor probe reports on, metres.</summary>
        public static readonly float[] ProbeWidths = { 280f, 240f, 200f, 160f, 120f, 80f, 40f, 0f };

        /// <summary>What the pass did, for the diagnostic report.</summary>
        public sealed class Report
        {
            public int ConvergentBoundaries;
            public int OnLandBoundaries;
            public int ChainsDiscovered;
            public int ChainsAccepted;

            /// <summary>Chains that certified at least one run, so are worth owning boundaries.</summary>
            public int ChainsCertified;

            /// <summary>Chains that certified nothing. These now consume no boundaries.</summary>
            public int ChainsWithoutCertifiedRun;

            /// <summary>Certified chains a better-certifying chain had already claimed a boundary of.</summary>
            public int CertifiedChainsDiscardedByOverlap;
            public int FilletRelaxations;
            public int FilletSplits;
            public int SegmentsBeforeLandClip;
            public int SegmentsAfterLandClip;

            /// <summary>Stretches of curve that reached land far enough to be worth certifying.</summary>
            public int BaseRunsConsidered;

            /// <summary>Stretches that had no room for a massif of any permitted width.</summary>
            public int RunsWithoutRoom;

            /// <summary>
            /// Longest run of axis clearing <c>gate + W</c> for each probed half-width, metres.
            /// </summary>
            /// <remarks>
            /// The corridor an island actually offers, measured rather than assumed. Certification is
            /// a decision taken from this table, and when it certifies nothing this is what says
            /// whether the axis was badly placed or the island simply has no room.
            /// </remarks>
            public readonly float[] CorridorAtWidth = new float[ProbeWidths.Length];
            public float LongestChainMeters;
            public float MeanBoundaryLengthMeters;

            public override string ToString() =>
                $"convergent={ConvergentBoundaries} onLand={OnLandBoundaries} chains={ChainsDiscovered}"
                + $" accepted={ChainsAccepted} certified={ChainsCertified}"
                + $" uncertified={ChainsWithoutCertifiedRun}"
                + $" overlapped={CertifiedChainsDiscardedByOverlap}"
                + $" relaxed={FilletRelaxations} split={FilletSplits}"
                + $" pieces={SegmentsBeforeLandClip}->{SegmentsAfterLandClip}"
                + $" runs={BaseRunsConsidered} noRoom={RunsWithoutRoom}"
                + $" longestChain={LongestChainMeters:F0}m meanBoundary={MeanBoundaryLengthMeters:F0}m";
        }

        public static List<MacroAxis> Build(
            MacroPlates plates,
            MacroStructureSettings settings,
            CoastQuery coast,
            out Report report)
        {
            settings.Clamp();
            report = new Report
            {
                MeanBoundaryLengthMeters = plates.MeanInteriorBoundaryLength(),
            };

            var convergent = new bool[plates.Boundaries.Length];
            for (var e = 0; e < plates.Boundaries.Length; e++)
            {
                if (plates.Boundaries[e].Convergence < settings.ConvergenceThreshold)
                {
                    continue;
                }

                report.ConvergentBoundaries++;

                // A boundary out at sea is convergent all the same, and chaining along it spends the
                // chain budget on geometry the land clip throws away. Candidacy is convergence plus
                // somewhere to stand.
                if (!TouchesLand(plates.Boundaries[e], coast, settings.CoastSetbackMeters))
                {
                    continue;
                }

                convergent[e] = true;
                report.OnLandBoundaries++;
            }

            var chains = DiscoverChains(plates, settings, convergent);
            report.ChainsDiscovered = chains.Count;

            // Evaluation order, not selection order. Certification writes counters into the report and
            // a stable order keeps those reproducible; which chain wins is decided afterwards, on what
            // certification found.
            chains.Sort((a, b) =>
            {
                var c = b.LengthMeters.CompareTo(a.LengthMeters);
                return c != 0 ? c : a.Boundaries[0].CompareTo(b.Boundaries[0]);
            });

            // Every chain is certified before any of them owns anything. The old order - rank by raw
            // length, take exclusive ownership, then certify the survivors - let a long chain that
            // certified nothing consume the boundaries a shorter certifiable chain needed, and the
            // shorter chain was gone before certification ever looked at it. Measured over 29 worlds,
            // 83 of 123 discovered chains were discarded that way, and on two of them it cost the
            // world its mountain outright while a convergent boundary with 456 m and 793 m of usable
            // core sat in a chain nobody evaluated.
            var evaluated = new List<EvaluatedChain>(chains.Count);
            for (var i = 0; i < chains.Count; i++)
            {
                var evaluation = EvaluateChain(chains[i], coast, settings, report);
                if (evaluation == null)
                {
                    report.ChainsWithoutCertifiedRun++;
                    continue;
                }

                report.ChainsCertified++;
                evaluated.Add(evaluation);
            }

            // Ranked on what each chain certified rather than on how long it happens to be. The key is
            // the high ground the world actually gets - certified half-width times certified core -
            // because both are the quantities certification established and their product is the
            // footprint of the massif that would stand there.
            evaluated.Sort(CompareEvaluatedChains);

            var used = new bool[plates.Boundaries.Length];
            var candidates = new List<MacroAxis>();

            for (var i = 0; i < evaluated.Count; i++)
            {
                var evaluation = evaluated[i];
                var chain = evaluation.Chain;

                var free = true;
                for (var k = 0; k < chain.Boundaries.Count; k++)
                {
                    if (used[chain.Boundaries[k]])
                    {
                        free = false;
                        break;
                    }
                }

                if (!free)
                {
                    report.CertifiedChainsDiscardedByOverlap++;
                    continue;
                }

                for (var k = 0; k < chain.Boundaries.Count; k++)
                {
                    used[chain.Boundaries[k]] = true;
                }

                report.ChainsAccepted++;
                report.LongestChainMeters = Mathf.Max(report.LongestChainMeters, chain.LengthMeters);
                candidates.AddRange(evaluation.Candidates);
            }

            // Best certified massif first, so the index a candidate reports is stable and whichever
            // one Judge takes is the strongest the world offered.
            candidates.Sort(CompareCandidates);

            for (var i = 0; i < candidates.Count; i++)
            {
                candidates[i].Index = i;
            }

            Judge(candidates, settings);
            return candidates;
        }

        /// <summary>One chain and the certified axes it turned out to be able to carry.</summary>
        private sealed class EvaluatedChain
        {
            public Chain Chain;
            public List<MacroAxis> Candidates;

            /// <summary>Half-width times core length of its best candidate, square metres.</summary>
            public float BestFootprintSqm;

            public float BestLengthMeters;
        }

        /// <summary>
        /// Fillets a chain, certifies every piece of it, and returns what it can carry, or null.
        /// </summary>
        /// <remarks>
        /// Returning null is the whole point: a chain that certifies nothing is not merely unusable,
        /// it must also be unable to take boundaries away from a chain that certifies something.
        /// </remarks>
        private static EvaluatedChain EvaluateChain(
            Chain chain, CoastQuery coast, MacroStructureSettings settings, Report report)
        {
            var pieces = FilletChain(chain.Points, settings, report);
            report.SegmentsBeforeLandClip += pieces.Count;

            List<MacroAxis> candidates = null;
            var bestFootprint = 0f;
            var bestLength = 0f;

            for (var p = 0; p < pieces.Count; p++)
            {
                var certified = Certify(pieces[p].curve, coast, settings, report);
                report.SegmentsAfterLandClip += certified.Count;

                for (var r = 0; r < certified.Count; r++)
                {
                    var run = certified[r];
                    var axis = new MacroAxis(
                        run.Points,
                        chain.Points.ToArray(),
                        chain.PlatePairs.ToArray(),
                        pieces[p].radiusMeters,
                        chain.MeanConvergence,
                        run.HalfWidthMeters)
                    {
                        CoastClearanceMeters = run.ClearanceMeters,
                        CertifiedCore = run.Core,
                    };

                    candidates ??= new List<MacroAxis>(2);
                    candidates.Add(axis);

                    bestFootprint = Mathf.Max(
                        bestFootprint, axis.UsableHalfWidthMeters * axis.CoreLengthMeters);
                    bestLength = Mathf.Max(bestLength, axis.LengthMeters);
                }
            }

            if (candidates == null)
            {
                return null;
            }

            return new EvaluatedChain
            {
                Chain = chain,
                Candidates = candidates,
                BestFootprintSqm = bestFootprint,
                BestLengthMeters = bestLength,
            };
        }

        /// <summary>
        /// Orders chains by the massif each certified, breaking every tie deterministically.
        /// </summary>
        private static int CompareEvaluatedChains(EvaluatedChain a, EvaluatedChain b)
        {
            var c = b.BestFootprintSqm.CompareTo(a.BestFootprintSqm);
            if (c != 0)
            {
                return c;
            }

            c = b.BestLengthMeters.CompareTo(a.BestLengthMeters);
            if (c != 0)
            {
                return c;
            }

            // Geological strength decides between two chains that would build the same massif.
            c = b.Chain.MeanConvergence.CompareTo(a.Chain.MeanConvergence);
            if (c != 0)
            {
                return c;
            }

            return a.Chain.Boundaries[0].CompareTo(b.Chain.Boundaries[0]);
        }

        /// <summary>Orders candidates by the massif they certify, breaking every tie deterministically.</summary>
        private static int CompareCandidates(MacroAxis a, MacroAxis b)
        {
            var c = (b.UsableHalfWidthMeters * b.CoreLengthMeters)
                .CompareTo(a.UsableHalfWidthMeters * a.CoreLengthMeters);
            if (c != 0)
            {
                return c;
            }

            c = b.LengthMeters.CompareTo(a.LengthMeters);
            if (c != 0)
            {
                return c;
            }

            c = b.MeanConvergence.CompareTo(a.MeanConvergence);
            if (c != 0)
            {
                return c;
            }

            c = a.Points[0].x.CompareTo(b.Points[0].x);
            return c != 0 ? c : a.Points[0].y.CompareTo(b.Points[0].y);
        }

        /// <summary>Whether any part of a boundary lies far enough inland to build on.</summary>
        private static bool TouchesLand(
            MacroPlates.Boundary boundary, CoastQuery coast, float setbackMeters)
        {
            var steps = Mathf.Max(2, Mathf.CeilToInt(boundary.Length / 40f));
            for (var i = 0; i <= steps; i++)
            {
                var p = Vector2.Lerp(boundary.A, boundary.B, i / (float)steps);
                if (coast.SignedDistance(p.x, p.y) >= setbackMeters)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Closest approach between two axes, metres.</summary>
        public static float Separation(MacroAxis a, MacroAxis b)
        {
            var best = float.PositiveInfinity;
            for (var i = 0; i < a.Points.Length; i++)
            {
                best = Mathf.Min(best, b.DistanceTo(a.Points[i].x, a.Points[i].y));
            }

            return best;
        }

        private static void Judge(List<MacroAxis> candidates, MacroStructureSettings settings)
        {
            var kept = new List<MacroAxis>();
            for (var i = 0; i < candidates.Count; i++)
            {
                var axis = candidates[i];

                if (axis.LengthMeters < settings.MinAxisLengthMeters)
                {
                    axis.RejectReason =
                        $"short ({axis.LengthMeters:F0} m < {settings.MinAxisLengthMeters:F0} m)";
                    continue;
                }

                if (axis.SelfIntersects)
                {
                    axis.RejectReason = "self-intersecting";
                    continue;
                }

                if (axis.UsableHalfWidthMeters < settings.MinUsableHalfWidthMeters)
                {
                    axis.RejectReason =
                        $"narrow (usable {axis.UsableHalfWidthMeters:F0} m"
                        + $" < {settings.MinUsableHalfWidthMeters:F0} m)";
                    continue;
                }

                if (kept.Count >= settings.MaxSystems)
                {
                    axis.RejectReason = $"budget ({settings.MaxSystems} system(s) already accepted)";
                    continue;
                }

                var crowded = false;
                for (var k = 0; k < kept.Count; k++)
                {
                    var gap = Separation(axis, kept[k]);
                    if (gap < settings.MinSystemSeparationMeters)
                    {
                        axis.RejectReason =
                            $"crowds axis {kept[k].Index} ({gap:F0} m"
                            + $" < {settings.MinSystemSeparationMeters:F0} m)";
                        crowded = true;
                        break;
                    }
                }

                if (crowded)
                {
                    continue;
                }

                axis.Accepted = true;
                kept.Add(axis);
            }
        }

        // ---------- chains ----------

        private sealed class Chain
        {
            public List<int> Boundaries = new List<int>();
            public List<Vector2> Points = new List<Vector2>();
            public List<int> PlatePairs = new List<int>();
            public float LengthMeters;
            public float MeanConvergence;
            public float RadiusMeters;
        }

        private static List<Chain> DiscoverChains(
            MacroPlates plates, MacroStructureSettings settings, bool[] convergent)
        {
            var chains = new List<Chain>();
            var scratch = new bool[plates.Boundaries.Length];

            for (var seed = 0; seed < plates.Boundaries.Length; seed++)
            {
                if (!convergent[seed])
                {
                    continue;
                }

                System.Array.Clear(scratch, 0, scratch.Length);
                scratch[seed] = true;

                var startJunction = plates.EndpointJunction[seed * 2];
                var endJunction = plates.EndpointJunction[seed * 2 + 1];

                var forward = new List<int>();
                Grow(plates, settings, convergent, scratch, seed, endJunction, forward);

                var backward = new List<int>();
                Grow(plates, settings, convergent, scratch, seed, startJunction, backward);

                var ordered = new List<int>(backward.Count + 1 + forward.Count);
                for (var i = backward.Count - 1; i >= 0; i--)
                {
                    ordered.Add(backward[i]);
                }

                ordered.Add(seed);
                ordered.AddRange(forward);

                var chain = Materialise(plates, ordered);
                if (chain != null)
                {
                    chain.RadiusMeters = settings.FilletRadiusMeters;
                    chains.Add(chain);
                }
            }

            return chains;
        }

        private static void Grow(
            MacroPlates plates,
            MacroStructureSettings settings,
            bool[] convergent,
            bool[] used,
            int fromBoundary,
            int atJunction,
            List<int> grown)
        {
            var current = fromBoundary;
            var junction = atJunction;

            while (grown.Count + 1 < settings.MaxChainEdges)
            {
                var incoming = DirectionInto(plates, current, junction);
                var best = -1;
                var bestTurn = float.PositiveInfinity;

                var options = plates.JunctionBoundaries[junction];
                for (var k = 0; k < options.Length; k++)
                {
                    var next = options[k];
                    if (next == current || used[next] || !convergent[next])
                    {
                        continue;
                    }

                    var outgoing = DirectionOutOf(plates, next, junction);
                    var turn = Vector2.Angle(incoming, outgoing);
                    if (turn > settings.MaxTurnDegrees)
                    {
                        continue;
                    }

                    if (turn < bestTurn - 1e-4f || (Mathf.Abs(turn - bestTurn) <= 1e-4f && next < best))
                    {
                        bestTurn = turn;
                        best = next;
                    }
                }

                if (best < 0)
                {
                    return;
                }

                used[best] = true;
                grown.Add(best);

                var a = plates.EndpointJunction[best * 2];
                var b = plates.EndpointJunction[best * 2 + 1];
                junction = a == junction ? b : a;
                current = best;
            }
        }

        private static Vector2 DirectionInto(MacroPlates plates, int boundary, int junction)
        {
            var e = plates.Boundaries[boundary];
            var atA = plates.EndpointJunction[boundary * 2] == junction;
            return atA ? (e.A - e.B).normalized : (e.B - e.A).normalized;
        }

        private static Vector2 DirectionOutOf(MacroPlates plates, int boundary, int junction)
        {
            var e = plates.Boundaries[boundary];
            var atA = plates.EndpointJunction[boundary * 2] == junction;
            return atA ? (e.B - e.A).normalized : (e.A - e.B).normalized;
        }

        private static Chain Materialise(MacroPlates plates, List<int> ordered)
        {
            if (ordered.Count == 0)
            {
                return null;
            }

            var chain = new Chain();
            chain.Boundaries.AddRange(ordered);

            if (ordered.Count == 1)
            {
                var only = plates.Boundaries[ordered[0]];
                chain.Points.Add(only.A);
                chain.Points.Add(only.B);
            }
            else
            {
                // Walk the shared junctions so the point sequence follows the chain rather than the
                // arbitrary A/B ordering each boundary was built with.
                var first = ordered[0];
                var second = ordered[1];
                var shared = SharedJunction(plates, first, second);
                if (shared < 0)
                {
                    return null;
                }

                var startAtA = plates.EndpointJunction[first * 2] != shared;
                chain.Points.Add(startAtA ? plates.Boundaries[first].A : plates.Boundaries[first].B);

                var junction = shared;
                for (var i = 1; i < ordered.Count; i++)
                {
                    chain.Points.Add(plates.Junctions[junction]);
                    var e = ordered[i];
                    var a = plates.EndpointJunction[e * 2];
                    var b = plates.EndpointJunction[e * 2 + 1];
                    if (a != junction && b != junction)
                    {
                        return null;
                    }

                    junction = a == junction ? b : a;
                }

                chain.Points.Add(plates.Junctions[junction]);
            }

            var convergenceSum = 0f;
            for (var i = 0; i < ordered.Count; i++)
            {
                var e = plates.Boundaries[ordered[i]];
                convergenceSum += e.Convergence;
                chain.PlatePairs.Add(e.PlateA);
                chain.PlatePairs.Add(e.PlateB);
            }

            chain.MeanConvergence = convergenceSum / ordered.Count;

            for (var i = 0; i + 1 < chain.Points.Count; i++)
            {
                chain.LengthMeters += (chain.Points[i + 1] - chain.Points[i]).magnitude;
            }

            return chain;
        }

        private static int SharedJunction(MacroPlates plates, int first, int second)
        {
            var a0 = plates.EndpointJunction[first * 2];
            var a1 = plates.EndpointJunction[first * 2 + 1];
            var b0 = plates.EndpointJunction[second * 2];
            var b1 = plates.EndpointJunction[second * 2 + 1];

            if (a0 == b0 || a0 == b1)
            {
                return a0;
            }

            if (a1 == b0 || a1 == b1)
            {
                return a1;
            }

            return -1;
        }

        // ---------- rounding ----------

        /// <summary>
        /// Rounds a chain, relaxing the radius once and splitting the chain if that still does not fit.
        /// </summary>
        private static List<(Vector2[] curve, float radiusMeters)> FilletChain(
            List<Vector2> control, MacroStructureSettings settings, Report report)
        {
            var result = new List<(Vector2[], float)>();
            var pending = new Queue<List<Vector2>>();
            pending.Enqueue(control);

            var guard = 0;
            while (pending.Count > 0 && guard++ < 64)
            {
                var polyline = pending.Dequeue();
                if (polyline.Count < 2)
                {
                    continue;
                }

                // Shrink before cutting. A narrower belt still reads as a range; a chain cut in two
                // reads as two stubs, and on seed 20260905 that turned a 1.4 km spine into three
                // fragments none of which cleared the length floor.
                var radius = settings.FilletRadiusMeters;
                List<int> failures = null;
                Vector2[] curve = null;
                var relaxed = false;

                while (radius >= settings.MinFilletRadiusMeters - 1e-3f)
                {
                    curve = MacroAxis.Fillet(
                        polyline, radius, settings.SampleSpacingMeters, out failures);
                    if (curve.Length >= 2)
                    {
                        break;
                    }

                    relaxed = true;
                    radius = radius <= settings.MinFilletRadiusMeters
                        ? settings.MinFilletRadiusMeters - 1f
                        : Mathf.Max(settings.MinFilletRadiusMeters, radius * 0.8f);
                }

                if (relaxed)
                {
                    report.FilletRelaxations++;
                }

                if (curve != null && curve.Length >= 2)
                {
                    result.Add((curve, radius));
                    continue;
                }

                if (failures == null || failures.Count == 0)
                {
                    continue;
                }

                // Cut at the corner that could not be rounded. A sharp corner is the one shape a belt
                // provably cannot follow, so the chain ends there rather than carrying it.
                report.FilletSplits++;
                var cut = failures[0];
                pending.Enqueue(polyline.GetRange(0, cut + 1));
                pending.Enqueue(polyline.GetRange(cut, polyline.Count - cut));
            }

            var typed = new List<(Vector2[] curve, float radiusMeters)>(result.Count);
            for (var i = 0; i < result.Count; i++)
            {
                typed.Add((result[i].Item1, result[i].Item2));
            }

            return typed;
        }

        // ---------- land clipping ----------

        /// <summary>One stretch of a curve certified as able to carry a massif of a given width.</summary>
        private readonly struct CertifiedRun
        {
            public Vector2[] Points { get; }
            public float HalfWidthMeters { get; }
            public float ClearanceMeters { get; }

            /// <summary>Normalised arc interval of the run whose whole section clears the coast.</summary>
            public Vector2 Core { get; }

            public CertifiedRun(
                Vector2[] points, float halfWidthMeters, float clearanceMeters, Vector2 core)
            {
                Points = points;
                HalfWidthMeters = halfWidthMeters;
                ClearanceMeters = clearanceMeters;
                Core = core;
            }
        }

        /// <summary>
        /// Cuts a curve down to the stretches that have room for the body they will be asked to carry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the contract. The old rule clipped an axis to wherever it was sixty metres inland
        /// and set its width from its length, which certified geometry that did not exist: a body of
        /// half-width W faded in over a coastal gate needs the gate to be open at the edges of its
        /// cross-section, and sixty metres of clearance at the axis says nothing about that. Seed 1337
        /// was the consequence - a legal axis, a legal width, and a quarter of the high ground standing
        /// where its section could not be built.
        /// </para>
        /// <para>
        /// The test is made where the failure happens: at the two edges of the section, not at the
        /// axis. Clearance at the axis is a far stronger condition than it looks - it assumes the coast
        /// runs square across the section - and demanding it certified almost nothing on four of the
        /// five canonical worlds while those worlds were in fact sound. What matters is only that both
        /// flanks of the body stand where the gate is open.
        /// </para>
        /// <para>
        /// The width is not assumed and then checked; it is found. Each stretch is asked how wide a
        /// body it has room for, from the target downwards, and the first width whose run is long
        /// enough for the length and length-to-width rules is certified. A stretch with room for
        /// nothing certifies nothing, and the axis is not offered at all.
        /// </para>
        /// </remarks>
        private static List<CertifiedRun> Certify(
            Vector2[] curve, CoastQuery coast, MacroStructureSettings settings, Report report)
        {
            var certified = new List<CertifiedRun>();
            if (curve == null || curve.Length < 2)
            {
                return certified;
            }

            var normal = new Vector2[curve.Length];
            for (var i = 0; i < curve.Length; i++)
            {
                var direction = curve[Mathf.Min(curve.Length - 1, i + 1)] - curve[Mathf.Max(0, i - 1)];
                normal[i] = direction.sqrMagnitude < 1e-10f
                    ? Vector2.up
                    : new Vector2(-direction.normalized.y, direction.normalized.x);
            }

            for (var w = 0; w < ProbeWidths.Length; w++)
            {
                report.CorridorAtWidth[w] = Mathf.Max(
                    report.CorridorAtWidth[w],
                    LongestRun(curve, normal, coast, settings, ProbeWidths[w], 0, curve.Length - 1,
                        out _, out _));
            }

            // The axis itself only has to be on land; it is the high ground that needs the room.
            var onLand = ClipToLand(curve, coast, settings.CoastSetbackMeters);
            for (var r = 0; r < onLand.Count; r++)
            {
                report.BaseRunsConsidered++;
                if (TryCertify(curve, normal, coast, settings, onLand[r].from, onLand[r].to, out var run))
                {
                    certified.Add(run);
                }
                else
                {
                    report.RunsWithoutRoom++;
                }
            }

            return certified;
        }

        /// <summary>Index ranges of a curve that stay a given distance inland.</summary>
        private static List<(int from, int to)> ClipToLand(
            Vector2[] curve, CoastQuery coast, float setbackMeters)
        {
            var runs = new List<(int, int)>();
            var start = -1;

            for (var i = 0; i <= curve.Length; i++)
            {
                var inland = i < curve.Length
                             && coast.SignedDistance(curve[i].x, curve[i].y) >= setbackMeters;

                if (inland)
                {
                    if (start < 0)
                    {
                        start = i;
                    }

                    continue;
                }

                if (start >= 0 && i - 1 > start)
                {
                    runs.Add((start, i - 1));
                }

                start = -1;
            }

            return runs;
        }

        /// <summary>The widest body a stretch has room for, or nothing if it has room for none.</summary>
        /// <remarks>
        /// Two rules, both on the same stretch. The axis must be long enough to read as a range, and
        /// the part of it that will carry high ground must have room for the whole cross-section and
        /// be longer than the body is wide. Certifying only the first is what shipped a mesa;
        /// certifying the second over the whole axis instead of over its core rejects worlds that are
        /// perfectly sound, because a massif's ends are allowed to run down to the water.
        /// </remarks>
        private static bool TryCertify(
            Vector2[] curve,
            Vector2[] normal,
            CoastQuery coast,
            MacroStructureSettings settings,
            int from,
            int to,
            out CertifiedRun run)
        {
            run = default;

            var length = 0f;
            for (var i = from; i < to; i++)
            {
                length += (curve[i + 1] - curve[i]).magnitude;
            }

            if (length < settings.MinAxisLengthMeters)
            {
                return false;
            }

            var step = Mathf.Max(1f, settings.CertificationWidthStepMeters);
            for (var width = settings.TargetHalfWidthMeters;
                 width >= settings.MinUsableHalfWidthMeters - 1e-3f;
                 width -= step)
            {
                width = Mathf.Max(width, settings.MinUsableHalfWidthMeters);

                if (length >= width * Mathf.Max(1f, settings.MinLengthToWidthRatio))
                {
                    var core = LongestRun(
                        curve, normal, coast, settings, width, from, to, out var start, out var end);

                    if (core >= width * Mathf.Max(0.5f, settings.MinCoreLengthPerHalfWidth))
                    {
                        var points = new Vector2[to - from + 1];
                        System.Array.Copy(curve, from, points, 0, points.Length);

                        var worst = float.PositiveInfinity;
                        for (var i = from; i <= to; i++)
                        {
                            worst = Mathf.Min(worst, coast.SignedDistance(curve[i].x, curve[i].y));
                        }

                        run = new CertifiedRun(
                            points, width, worst, NormalisedCore(curve, from, to, start, end));
                        return true;
                    }
                }

                if (width <= settings.MinUsableHalfWidthMeters)
                {
                    break;
                }
            }

            return false;
        }

        /// <summary>The core's index range expressed as a fraction of the run's arc length.</summary>
        private static Vector2 NormalisedCore(Vector2[] curve, int from, int to, int start, int end)
        {
            if (start < 0 || end <= start)
            {
                return new Vector2(0f, 1f);
            }

            var total = 0f;
            var beforeStart = 0f;
            var beforeEnd = 0f;

            for (var i = from; i < to; i++)
            {
                var segment = (curve[i + 1] - curve[i]).magnitude;
                if (i < start)
                {
                    beforeStart += segment;
                }

                if (i < end)
                {
                    beforeEnd += segment;
                }

                total += segment;
            }

            if (total <= 1e-4f)
            {
                return new Vector2(0f, 1f);
            }

            return new Vector2(
                Mathf.Clamp01(beforeStart / total), Mathf.Clamp01(beforeEnd / total));
        }

        /// <summary>
        /// Longest stretch inside a range whose whole cross-section stands clear of the coast.
        /// </summary>
        private static float LongestRun(
            Vector2[] curve,
            Vector2[] normal,
            CoastQuery coast,
            MacroStructureSettings settings,
            float halfWidth,
            int from,
            int to,
            out int bestStart,
            out int bestEnd)
        {
            bestStart = -1;
            bestEnd = -1;
            var best = 0f;

            var clearance = settings.MassifCoastClearanceMeters;
            var start = -1;

            for (var i = from; i <= to + 1; i++)
            {
                var inside = false;
                if (i <= to)
                {
                    var left = curve[i] + normal[i] * halfWidth;
                    var right = curve[i] - normal[i] * halfWidth;
                    inside = coast.SignedDistance(left.x, left.y) >= clearance
                             && coast.SignedDistance(right.x, right.y) >= clearance;
                }

                if (inside)
                {
                    if (start < 0)
                    {
                        start = i;
                    }

                    continue;
                }

                if (start >= 0 && i - 1 > start)
                {
                    var length = 0f;
                    for (var k = start; k < i - 1; k++)
                    {
                        length += (curve[k + 1] - curve[k]).magnitude;
                    }

                    if (length > best)
                    {
                        best = length;
                        bestStart = start;
                        bestEnd = i - 1;
                    }
                }

                start = -1;
            }

            return best;
        }
    }
}
