using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Town
{
    /// <summary>Functional classification of a road, ordered coarsest → finest.</summary>
    public enum RoadClass
    {
        /// <summary>Main arterial — connects the town centre to portals (~8–12 m wide).</summary>
        Arterial,

        /// <summary>Secondary street — district connector (~4–6 m wide).</summary>
        Secondary,

        /// <summary>Tertiary street — local block access (~3–4 m wide).</summary>
        Tertiary,

        /// <summary>Alley — narrow pedestrian/cart passage (~1.5–3 m wide).</summary>
        Alley,

        /// <summary>Service lane — rear access, loading bays (~1.5–2 m wide).</summary>
        Service
    }

    /// <summary>
    /// A node in the planar road graph.  Stores a world-space XZ position
    /// and indices into <see cref="TownModel.RoadEdges"/> for adjacency.
    /// Use index references (not pointers) to keep the graph serialisable and
    /// allocation-conscious.
    /// </summary>
    public sealed class RoadNode
    {
        /// <summary>Position in world-space XZ (metres).</summary>
        public Vector2 Pos;

        /// <summary>
        /// Indices into <see cref="TownModel.RoadEdges"/> of all edges incident to
        /// this node.  Stored in insertion order (deterministic).
        /// </summary>
        public List<int> Edges;

        public RoadNode()
        {
            Edges = new List<int>(4);
        }

        public RoadNode(Vector2 pos)
        {
            Pos   = pos;
            Edges = new List<int>(4);
        }
    }

    /// <summary>
    /// A directed edge in the planar road graph represented as a polyline
    /// (for curved / jittered roads).
    /// </summary>
    public sealed class RoadEdge
    {
        /// <summary>Index of the start node in <see cref="TownModel.RoadNodes"/>.</summary>
        public int NodeA;

        /// <summary>Index of the end node in <see cref="TownModel.RoadNodes"/>.</summary>
        public int NodeB;

        /// <summary>
        /// The smoothed centreline of the road (world-space XZ, metres).
        /// First and last points coincide with <see cref="NodeA"/> and <see cref="NodeB"/>
        /// positions.
        /// </summary>
        public List<Vector2> Polyline;

        /// <summary>Functional classification (determines default width range).</summary>
        public RoadClass Class;

        /// <summary>Full corridor width in metres (metres across the centreline).</summary>
        public float Width;

        /// <summary>True if this edge terminates at a dead end rather than another node.</summary>
        public bool DeadEnd;

        /// <summary>True if this edge is an informal shortcut cutting through a block.</summary>
        public bool Shortcut;

        public RoadEdge()
        {
            Polyline = new List<Vector2>(8);
        }
    }
}
