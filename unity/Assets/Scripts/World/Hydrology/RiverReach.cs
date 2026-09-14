using System.Collections.Generic;

namespace GameDev.World.Hydrology
{
    /// <summary>What a reach runs into at its downstream end.</summary>
    public enum ReachTermination
    {
        /// <summary>Meets another reach at a confluence. <see cref="RiverReach.DownstreamId"/> is set.</summary>
        Confluence = 0,

        /// <summary>Enters a lake. <see cref="RiverReach.DownstreamLake"/> is set.</summary>
        Lake = 1,

        /// <summary>Reaches the sea. This is the only termination allowed to have no downstream reach.</summary>
        Ocean = 2,
    }

    /// <summary>
    /// One river segment between two topological events — a source, a confluence, a lake shore or
    /// the coast.
    /// </summary>
    /// <remarks>
    /// Reaches are not generated independently. They are cut out of a single shared drainage network
    /// (<c>HYDROLOGY_GENERATION_PLAN.md</c> §10), which is what makes tributaries fall out of the
    /// terrain rather than being placed. A reach whose <see cref="Termination"/> is not
    /// <see cref="ReachTermination.Ocean"/> must have a downstream link, and following those links
    /// must always end at the sea — the invariant <c>HydrologyValidator</c> checks explicitly.
    /// </remarks>
    public sealed class RiverReach
    {
        public ReachId Id { get; }

        /// <summary>
        /// The reach this one flows into, or <see cref="ReachId.None"/> for an ocean outlet.
        /// </summary>
        public ReachId DownstreamId { get; }

        /// <summary>The lake this reach empties into, or <see cref="LakeId.None"/>.</summary>
        public LakeId DownstreamLake { get; }

        /// <summary>Centreline samples, upstream first.</summary>
        public IReadOnlyList<RiverNode> Nodes { get; }

        /// <summary>Strahler order, derived from the drainage tree. Headwaters are 1.</summary>
        public int StrahlerOrder { get; }

        /// <summary>Contributing area at the downstream end, in square metres.</summary>
        public float ContributingAreaSqm { get; }

        /// <summary>Surface width at the downstream end, in metres.</summary>
        public float WidthMeters { get; }

        public ReachTermination Termination { get; }

        /// <summary>True when nothing else drains into this reach — i.e. it starts at a source.</summary>
        public bool IsSource { get; }

        public RiverReach(
            ReachId id,
            ReachId downstreamId,
            LakeId downstreamLake,
            IReadOnlyList<RiverNode> nodes,
            int strahlerOrder,
            float contributingAreaSqm,
            float widthMeters,
            ReachTermination termination,
            bool isSource)
        {
            Id = id;
            DownstreamId = downstreamId;
            DownstreamLake = downstreamLake;
            Nodes = nodes;
            StrahlerOrder = strahlerOrder;
            ContributingAreaSqm = contributingAreaSqm;
            WidthMeters = widthMeters;
            Termination = termination;
            IsSource = isSource;
        }

        /// <summary>Centreline length in metres.</summary>
        public float LengthMeters =>
            Nodes == null || Nodes.Count == 0 ? 0f : Nodes[Nodes.Count - 1].CumulativeDistance;

        /// <summary>Total fall from the upstream to the downstream end, in metres.</summary>
        public float DropMeters =>
            Nodes == null || Nodes.Count < 2
                ? 0f
                : Nodes[0].BedElevation - Nodes[Nodes.Count - 1].BedElevation;
    }
}
