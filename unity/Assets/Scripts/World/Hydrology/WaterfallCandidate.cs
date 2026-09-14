using UnityEngine;

namespace GameDev.World.Hydrology
{
    /// <summary>
    /// A place on a reach where the bed falls steeply enough to read as a waterfall.
    /// </summary>
    /// <remarks>
    /// Derived, never placed: the detector walks each reach's elevation profile and reports where a
    /// drop exceeds the configured thresholds. That is the whole point of the chain
    /// <c>river segment -> elevation profile -> significant local drop -> candidate</c> — a
    /// waterfall is a property the geometry already has, not an object scattered on top of it.
    /// <para>
    /// The MVP produces candidates and renders nothing. Rendering later needs no new analysis.
    /// </para>
    /// </remarks>
    public readonly struct WaterfallCandidate
    {
        public ReachId Reach { get; }

        /// <summary>Index of the upstream node of the falling span, within the reach's node list.</summary>
        public int NodeIndex { get; }

        /// <summary>World XZ at the lip of the drop.</summary>
        public Vector2 PositionXZ { get; }

        /// <summary>Vertical fall across the span, in metres.</summary>
        public float DropMeters { get; }

        /// <summary>Horizontal distance the fall takes, in metres.</summary>
        public float RunMeters { get; }

        public WaterfallCandidate(
            ReachId reach, int nodeIndex, Vector2 positionXZ, float dropMeters, float runMeters)
        {
            Reach = reach;
            NodeIndex = nodeIndex;
            PositionXZ = positionXZ;
            DropMeters = dropMeters;
            RunMeters = runMeters;
        }

        /// <summary>Fall over run. Guarded so a zero-length span cannot produce infinity.</summary>
        public float Slope => RunMeters > 0.001f ? DropMeters / RunMeters : 0f;
    }
}
