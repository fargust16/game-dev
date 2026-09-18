using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// The valley geometry <see cref="CarveField"/> cuts along: one curve, in its own arc frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stage 3C carries a valley twice — as the <see cref="ValleyCorridor"/> the spur builder writes,
    /// and as the <see cref="FinalValleyTrajectory"/> Slice C settled on, one per corridor with the
    /// same <c>Id</c>. The carve model is the same either way; only the curve differs. This is the
    /// surface the model reads, so the geometry source is a parameter rather than a second algorithm.
    /// </para>
    /// <para>
    /// Everything here is world-space, deterministic, and evaluated per point with no allocation, so a
    /// carve built on it stays tile-independent and point-queryable.
    /// </para>
    /// </remarks>
    public interface IValleyCarveCurve
    {
        /// <summary>Corridor identity. A trajectory keeps the id of the corridor it came from.</summary>
        int Id { get; }

        int MassifIndex { get; }

        /// <summary>Whether there is a curve to cut along at all.</summary>
        bool HasCarveGeometry { get; }

        /// <summary>Arc length of the whole curve, metres.</summary>
        float LengthMeters { get; }

        /// <summary>Arc length at the massif foot, metres.</summary>
        float ToeArcMeters { get; }

        /// <summary>Half-width of the corridor, metres, carried constant along the curve.</summary>
        float HalfWidthMeters { get; }

        /// <summary>The massif arc parameter the corridor heads from; the crest reads from it.</summary>
        float HeadS { get; }

        ValleyTermination Termination { get; }

        /// <summary>
        /// A world point in the curve's own frame: arc along it, distance across it.
        /// </summary>
        /// <remarks>
        /// A local coordinate inside the curve's own support, not a global nearest-point field. The two
        /// ends where a nearest-point projection would go radial are exactly the two ends where the
        /// carve is zero.
        /// </remarks>
        bool Project(Vector2 p, out float arcMeters, out float acrossMeters);

        /// <summary>Axis-aligned bounds of the curve, inflated by a pad.</summary>
        Rect Bounds(float padMeters);
    }
}
