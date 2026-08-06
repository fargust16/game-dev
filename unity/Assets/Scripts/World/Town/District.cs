using System;
using GameDev.World.Town.Geometry;
using UnityEngine;

namespace GameDev.World.Town
{
    /// <summary>Socioeconomic character of a district.</summary>
    public enum WealthClass
    {
        Rich,
        Mixed,
        Poor
    }

    /// <summary>Dominant land-use function of a district.</summary>
    public enum DistrictType
    {
        Residential,
        Civic,
        Market,
        Industrial
    }

    /// <summary>
    /// Per-district tunable parameters that control how a district is subdivided
    /// into blocks and plots and how buildings are placed.
    /// Defined as a <see cref="SerializableAttribute"/> class so it can be
    /// embedded in both runtime data (<see cref="District"/>) and the
    /// <see cref="TownProfile"/> ScriptableObject.
    /// </summary>
    [Serializable]
    public sealed class DistrictProfile
    {
        [Header("Block &amp; Plot Sizing")]
        [Tooltip("Target side-length of a block in metres.")]
        public float BlockSize = 40f;

        [Tooltip("Minimum plot frontage (street-facing width) in metres.")]
        public float PlotFrontageMin = 6f;

        [Tooltip("Maximum plot frontage in metres.")]
        public float PlotFrontageMax = 12f;

        [Tooltip("Minimum plot depth (perpendicular to street) in metres.")]
        public float PlotDepthMin = 12f;

        [Tooltip("Maximum plot depth in metres.")]
        public float PlotDepthMax = 25f;

        [Header("Street Character")]
        [Tooltip("Normalised density of minor streets within the district (0 = sparse, 1 = dense).")]
        [Range(0f, 1f)]
        public float StreetDensity = 0.5f;

        [Tooltip("Building setback from the street edge in metres.")]
        public float Setback = 1.5f;

        [Header("Buildings")]
        [Tooltip("Minimum number of above-ground floors.")]
        public int FloorsMin = 2;

        [Tooltip("Maximum number of above-ground floors.")]
        public int FloorsMax = 4;

        [Header("Probabilities")]
        [Tooltip("Probability that the interior of a dense block becomes a courtyard rather than a plot.")]
        [Range(0f, 1f)]
        public float CourtyardProbability = 0.2f;

        [Tooltip("Probability that a plot becomes a private garden instead of a building.")]
        [Range(0f, 1f)]
        public float GardenProbability = 0.1f;

        [Tooltip("Probability that a secondary branch terminates as a dead end.")]
        [Range(0f, 1f)]
        public float DeadEndProbability = 0.05f;

        [Tooltip("Probability of a shortcut passage cutting through a block.")]
        [Range(0f, 1f)]
        public float ShortcutProbability = 0.05f;

        public DistrictProfile() { }

        /// <summary>Deep copy constructor.</summary>
        public DistrictProfile(DistrictProfile src)
        {
            BlockSize          = src.BlockSize;
            PlotFrontageMin    = src.PlotFrontageMin;
            PlotFrontageMax    = src.PlotFrontageMax;
            PlotDepthMin       = src.PlotDepthMin;
            PlotDepthMax       = src.PlotDepthMax;
            StreetDensity      = src.StreetDensity;
            Setback            = src.Setback;
            FloorsMin          = src.FloorsMin;
            FloorsMax          = src.FloorsMax;
            CourtyardProbability = src.CourtyardProbability;
            GardenProbability    = src.GardenProbability;
            DeadEndProbability   = src.DeadEndProbability;
            ShortcutProbability  = src.ShortcutProbability;
        }
    }

    /// <summary>
    /// A district — a named zone within the town boundary with a wealth/type
    /// classification and a <see cref="DistrictProfile"/> that drives all
    /// subsequent block/plot/building decisions inside it.
    /// </summary>
    public sealed class District
    {
        /// <summary>Polygon boundary of the district (world-space XZ, metres).</summary>
        public Polygon2 Area;

        /// <summary>Socioeconomic character.</summary>
        public WealthClass Wealth;

        /// <summary>Dominant land use.</summary>
        public DistrictType Type;

        /// <summary>Generation parameters for this district.</summary>
        public DistrictProfile Profile;

        public District()
        {
            Area    = new Polygon2();
            Profile = new DistrictProfile();
        }
    }
}
