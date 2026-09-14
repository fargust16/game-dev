using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>What a stretch of a candidate's boundary is made of.</summary>
    /// <remarks>
    /// Every segment of an enclosure has to say where it came from, so that "the perimeter is ridge
    /// structure for a majority of its length" can be measured rather than assumed. There is no
    /// <c>Divide</c> member: the inter-massif divide only bounds an intermontane basin, and that path
    /// is not built. A member nothing produces would be a decision made before it was taken.
    /// </remarks>
    public enum BoundarySource
    {
        /// <summary>The massif's own crest, the depth-0 divide.</summary>
        Crest = 0,

        /// <summary>A primary spur, the depth-1 divide.</summary>
        Spur = 1,

        /// <summary>The gap between two spur toes. The one stretch with no ridge beneath it.</summary>
        Outlet = 2,
    }

    /// <summary>One run of a candidate's boundary, and the structure it was read off.</summary>
    public readonly struct BoundarySegment
    {
        public BoundarySource Source { get; }

        /// <summary>The spur id for <see cref="BoundarySource.Spur"/>, the massif for a crest, else -1.</summary>
        public int SourceId { get; }

        /// <summary>Where this run starts in the candidate's boundary ring.</summary>
        public int FirstIndex { get; }

        /// <summary>How many points of the ring this run covers.</summary>
        public int PointCount { get; }

        public float LengthMeters { get; }

        /// <summary>Whether this run has ridge structure under it.</summary>
        public bool IsRidge => Source != BoundarySource.Outlet;

        public BoundarySegment(
            BoundarySource source, int sourceId, int firstIndex, int pointCount, float lengthMeters)
        {
            Source = source;
            SourceId = sourceId;
            FirstIndex = firstIndex;
            PointCount = pointCount;
            LengthMeters = lengthMeters;
        }
    }

    /// <summary>
    /// A cirque enclosure that exists structurally. Not yet a basin, and deliberately not called one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It carries what the enclosure <em>is</em> and nothing about whether it holds water or ground:
    /// no floor elevation, no outlet elevation, no outlet corridor relation, no inlets, no verdict.
    /// Those belong to validation, and validation is blocked on a threshold the design never states
    /// and on a semantic question about what the outlet's elevation even means. Calling this a basin
    /// would be answering both by accident.
    /// </para>
    /// <para>
    /// Its enclosure is exactly one primary corridor's own frame: the crest between two adjacent spur
    /// attachments, those two spurs, and the gap between their toes. Every one of the four runs is read
    /// off frozen primary geometry; there is no edge here that was drawn to close a shape.
    /// </para>
    /// </remarks>
    public sealed class CirqueCandidate
    {
        /// <summary>Enumeration order, which is corridor order, which is stable across builds.</summary>
        public int Id { get; }

        public int MassifIndex { get; }

        public int FlankSign { get; }

        /// <summary>The primary corridor this enclosure is the frame of.</summary>
        public int CorridorId { get; }

        public int LowerSpurId { get; }

        public int UpperSpurId { get; }

        /// <summary>The corridor's crest interval, normalised arc.</summary>
        public Vector2 CrestInterval { get; }

        /// <summary>The closed ring, first point not repeated at the end.</summary>
        public Vector2[] Boundary { get; }

        /// <summary>The four runs, in boundary order.</summary>
        public BoundarySegment[] Segments { get; }

        public Vector2 LowerToe { get; }

        public Vector2 UpperToe { get; }

        /// <summary>The straight distance between the two toes, metres.</summary>
        public float OutletGapMeters { get; }

        /// <summary>The crest run between the two attachments, metres.</summary>
        public float CrestSpanMeters { get; }

        /// <summary>
        /// Gap over crest span: under one means the spurs close, over one means they splay.
        /// </summary>
        /// <remarks>
        /// Recorded, not judged. A cirque needs the concave side of a bent axis and this is the number
        /// that says whether the flank is concave here, but turning it into a verdict is E2's job and
        /// E2 is blocked.
        /// </remarks>
        public float ConvergenceRatio { get; }

        public float AreaSqm { get; }

        public float PerimeterMeters { get; }

        /// <summary>
        /// The fraction of the perimeter with ridge structure under it.
        /// </summary>
        /// <remarks>
        /// The measurement the design's first validity condition needs. The threshold it must be
        /// compared against is not stated anywhere in the design, so nothing here compares it.
        /// </remarks>
        public float RidgePerimeterFraction { get; }

        public CirqueCandidate(
            int id,
            int massifIndex,
            int flankSign,
            int corridorId,
            int lowerSpurId,
            int upperSpurId,
            Vector2 crestInterval,
            Vector2[] boundary,
            BoundarySegment[] segments,
            Vector2 lowerToe,
            Vector2 upperToe,
            float outletGapMeters,
            float crestSpanMeters,
            float areaSqm,
            float perimeterMeters,
            float ridgePerimeterFraction)
        {
            Id = id;
            MassifIndex = massifIndex;
            FlankSign = flankSign >= 0 ? 1 : -1;
            CorridorId = corridorId;
            LowerSpurId = lowerSpurId;
            UpperSpurId = upperSpurId;
            CrestInterval = crestInterval;
            Boundary = boundary;
            Segments = segments;
            LowerToe = lowerToe;
            UpperToe = upperToe;
            OutletGapMeters = outletGapMeters;
            CrestSpanMeters = crestSpanMeters;
            AreaSqm = areaSqm;
            PerimeterMeters = perimeterMeters;
            RidgePerimeterFraction = ridgePerimeterFraction;
            ConvergenceRatio = crestSpanMeters > 1e-3f ? outletGapMeters / crestSpanMeters : 0f;
        }

        /// <summary>The points of one run, in boundary order.</summary>
        public Vector2[] PointsOf(int segment)
        {
            var run = Segments[segment];
            var points = new Vector2[run.PointCount];
            for (var i = 0; i < run.PointCount; i++)
            {
                points[i] = Boundary[(run.FirstIndex + i) % Boundary.Length];
            }

            return points;
        }
    }

    /// <summary>
    /// Stage 3E slice E1: the cirque enclosures the frozen structure already contains.
    /// </summary>
    /// <remarks>
    /// Enumeration only. Nothing here decides whether an enclosure is a basin, and nothing here reads
    /// a height field: candidates are found by walking the ridge structure, which is what the design
    /// asks for and the opposite of looking for low ground.
    /// </remarks>
    public sealed class CirqueCandidateSet
    {
        public int Seed { get; }

        public RidgeSettings Settings { get; }

        /// <summary>The frozen depth-1 skeleton. Read-only to everything here.</summary>
        public RidgeSkeleton Primary { get; }

        public IReadOnlyList<CirqueCandidate> Candidates { get; }

        public CirqueCandidateStats Stats { get; }

        public CirqueCandidateSet(
            int seed,
            RidgeSettings settings,
            RidgeSkeleton primary,
            IReadOnlyList<CirqueCandidate> candidates,
            CirqueCandidateStats stats)
        {
            Seed = seed;
            Settings = settings;
            Primary = primary;
            Candidates = candidates;
            Stats = stats;
        }

        /// <summary>The candidate framed by one corridor, or null.</summary>
        public CirqueCandidate ForCorridor(int corridorId)
        {
            for (var i = 0; i < Candidates.Count; i++)
            {
                if (Candidates[i].CorridorId == corridorId)
                {
                    return Candidates[i];
                }
            }

            return null;
        }
    }

    public sealed class CirqueCandidateStats
    {
        public int Seed;
        public int CorridorCount;
        public int CandidateCount;

        /// <summary>Corridors with an open side, which cannot enclose anything.</summary>
        public int SkippedOuter;

        public int RejectedForMissingSpur;
        public int RejectedForCrestInterval;
        public int RejectedForArea;
        public int RejectedForSelfIntersection;
        public int RejectedForDomain;

        public float MinAreaSqm = float.PositiveInfinity;
        public float MaxAreaSqm;
        public float MinConvergenceRatio = float.PositiveInfinity;
        public float MaxConvergenceRatio;
        public float MinRidgePerimeterFraction = float.PositiveInfinity;
        public float MaxRidgePerimeterFraction;

        /// <summary>How many candidates have their toes closer together than their crest span.</summary>
        public int ConvergentCandidates;

        public long BuildMs;

        public int RejectedCount =>
            RejectedForMissingSpur + RejectedForCrestInterval + RejectedForArea
            + RejectedForSelfIntersection + RejectedForDomain;

        public string ToLine() =>
            $"seed {Seed}: corridors={CorridorCount} candidates={CandidateCount}"
            + $" outer={SkippedOuter} rejected={RejectedCount} convergent={ConvergentCandidates}"
            + $" build={BuildMs}ms";

        public string RejectionLine() =>
            $"rejections: spur={RejectedForMissingSpur} crest={RejectedForCrestInterval}"
            + $" area={RejectedForArea} self={RejectedForSelfIntersection} domain={RejectedForDomain}";

        public string ShapeLine() =>
            $"shape: area=[{(MinAreaSqm == float.PositiveInfinity ? -1f : MinAreaSqm):F0},{MaxAreaSqm:F0}]m2"
            + $" convergence=[{(MinConvergenceRatio == float.PositiveInfinity ? -1f : MinConvergenceRatio):F2},"
            + $"{MaxConvergenceRatio:F2}]"
            + $" ridgePerimeter=[{(MinRidgePerimeterFraction == float.PositiveInfinity ? -1f : MinRidgePerimeterFraction):P0},"
            + $"{MaxRidgePerimeterFraction:P0}]";
    }
}
