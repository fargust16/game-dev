using System.Collections.Generic;
using GameDev.World.Town.Geometry;
using UnityEngine;

namespace GameDev.World.Town
{
    /// <summary>
    /// Terrain features that may influence town layout: rivers/coast as polylines
    /// and an optional ridge line (hill crest).
    /// </summary>
    public sealed class TownTerrain
    {
        /// <summary>
        /// Zero or more river or coastline polylines (world-space XZ, metres).
        /// Arterials that cross a river become Bridge portals.
        /// </summary>
        public List<List<Vector2>> RiverPolylines;

        /// <summary>
        /// Optional ridge (hill crest) polyline (world-space XZ, metres).
        /// Rich districts are biased toward the ridge; industrial toward lower ground.
        /// Null when <see cref="TownProfile.HasRidge"/> is false.
        /// </summary>
        public List<Vector2> Ridge;

        public TownTerrain()
        {
            RiverPolylines = new List<List<Vector2>>();
        }
    }

    /// <summary>
    /// Root aggregate for a generated town.  All stage outputs are collected
    /// here; the struct is fully deterministic given the same seed and
    /// <see cref="TownProfile"/>.
    ///
    /// Index-based cross-references (e.g. <see cref="Block.DistrictIndex"/>) are
    /// used throughout to keep the model serialisable and allocation-conscious.
    /// </summary>
    public sealed class TownModel
    {
        // ------------------------------------------------------------------ identity

        /// <summary>Seed used to generate this model.</summary>
        public int Seed;

        // ------------------------------------------------------------------ geometry roots

        /// <summary>Outer boundary polygon of the town (world-space XZ, metres).</summary>
        public Polygon2 Bounds;

        /// <summary>Town centre / origin of the road network (world-space XZ, metres).</summary>
        public Vector2 Center;

        // ------------------------------------------------------------------ terrain

        /// <summary>Optional rivers, coast, and ridge affecting layout.</summary>
        public TownTerrain Terrain;

        // ------------------------------------------------------------------ landmark

        /// <summary>The dominant central landmark (market, church, castle, or town hall).</summary>
        public Landmark Landmark;

        // ------------------------------------------------------------------ road network

        /// <summary>All portals on the town boundary (one per arterial exit).</summary>
        public List<Portal> Portals;

        /// <summary>Nodes of the planar road graph.</summary>
        public List<RoadNode> RoadNodes;

        /// <summary>Edges of the planar road graph.</summary>
        public List<RoadEdge> RoadEdges;

        // ------------------------------------------------------------------ spatial hierarchy

        /// <summary>Districts partitioned by arterials / ring roads.</summary>
        public List<District> Districts;

        /// <summary>Blocks produced by recursive OBB subdivision of districts.</summary>
        public List<Block> Blocks;

        /// <summary>Plots produced by recursive frontage subdivision of blocks.</summary>
        public List<Plot> Plots;

        /// <summary>Buildings placed on plots.</summary>
        public List<Building> Buildings;

        /// <summary>Plazas, wells, gardens, and courtyards.</summary>
        public List<OpenSpace> OpenSpaces;

        // ------------------------------------------------------------------ ctor

        public TownModel()
        {
            Bounds     = new Polygon2(32);
            Terrain    = new TownTerrain();
            Portals    = new List<Portal>(8);
            RoadNodes  = new List<RoadNode>(256);
            RoadEdges  = new List<RoadEdge>(256);
            Districts  = new List<District>(16);
            Blocks     = new List<Block>(256);
            Plots      = new List<Plot>(1024);
            Buildings  = new List<Building>(1024);
            OpenSpaces = new List<OpenSpace>(64);
        }

        // ------------------------------------------------------------------ convenience

        /// <summary>
        /// Resets all collections and geometry roots so the same TownModel
        /// instance can be reused for a new generation without heap pressure.
        /// </summary>
        public void Reset(int seed)
        {
            Seed = seed;
            Bounds.Clear();
            Center    = Vector2.zero;
            Terrain   = new TownTerrain();
            Landmark  = null;
            Portals.Clear();
            RoadNodes.Clear();
            RoadEdges.Clear();
            Districts.Clear();
            Blocks.Clear();
            Plots.Clear();
            Buildings.Clear();
            OpenSpaces.Clear();
        }
    }
}
