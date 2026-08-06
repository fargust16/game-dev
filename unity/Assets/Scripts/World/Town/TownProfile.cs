using System;
using UnityEngine;

namespace GameDev.World.Town
{
    // ======================================================================
    //  Helper serialisable types embedded in TownProfile
    // ======================================================================

    /// <summary>
    /// Width range and organic road character settings for a single
    /// <see cref="RoadClass"/>.
    /// </summary>
    [Serializable]
    public sealed class RoadClassSettings
    {
        [Tooltip("Minimum road width in metres (edge-to-edge).")]
        public float WidthMin = 4f;

        [Tooltip("Maximum road width in metres.")]
        public float WidthMax = 6f;

        [Tooltip("Maximum lateral deviation of the road centreline from a straight line (metres). " +
                 "Controls how much the road bends.")]
        public float CurvatureAmplitude = 2f;

        [Tooltip("Per-control-point random lateral jitter in metres. " +
                 "Controls small-scale wobble independent of overall curvature.")]
        public float JitterAmplitude = 0.5f;
    }

    /// <summary>
    /// Bundles a <see cref="DistrictProfile"/> (the generation parameters) with a
    /// <see cref="WealthClass"/> label so TownProfile can expose one entry per class.
    /// </summary>
    [Serializable]
    public sealed class WealthClassSettings
    {
        [Tooltip("Generation parameters for districts of this wealth class.")]
        public DistrictProfile Profile = new DistrictProfile();
    }

    // ======================================================================
    //  Relative weights for enumerated choices (portal types, landmark types)
    // ======================================================================

    /// <summary>Relative spawn weights for each <see cref="PortalType"/>.</summary>
    [Serializable]
    public sealed class PortalTypeWeights
    {
        [Tooltip("Relative weight for Gate portals (used when walls are enabled).")]
        public float Gate = 3f;

        [Tooltip("Relative weight for Bridge portals (chosen automatically when an arterial meets a river).")]
        public float Bridge = 1f;

        [Tooltip("Relative weight for Farm Road portals.")]
        public float FarmRoad = 2f;

        [Tooltip("Relative weight for Settlement Road portals.")]
        public float SettlementRoad = 2f;

        /// <summary>
        /// Returns the weight array in <see cref="PortalType"/> enum order:
        /// [Gate, Bridge, FarmRoad, SettlementRoad].
        /// </summary>
        public float[] ToArray() => new[] { Gate, Bridge, FarmRoad, SettlementRoad };
    }

    /// <summary>Relative spawn weights for each <see cref="LandmarkKind"/>.</summary>
    [Serializable]
    public sealed class LandmarkKindWeights
    {
        [Tooltip("Relative weight for Market Square landmark.")]
        public float MarketSquare = 3f;

        [Tooltip("Relative weight for Church landmark.")]
        public float Church = 3f;

        [Tooltip("Relative weight for Castle landmark.")]
        public float Castle = 1f;

        [Tooltip("Relative weight for Town Hall landmark.")]
        public float TownHall = 2f;

        /// <summary>
        /// Returns the weight array in <see cref="LandmarkKind"/> enum order:
        /// [MarketSquare, Church, Castle, TownHall].
        /// </summary>
        public float[] ToArray() => new[] { MarketSquare, Church, Castle, TownHall };
    }

    // ======================================================================
    //  TownProfile ScriptableObject
    // ======================================================================

    /// <summary>
    /// Authorable ScriptableObject that drives all tunable parameters of the
    /// OrganicTown v2 generator.  Create via
    /// <c>Assets → Create → GameDev → Town → Town Profile</c>.
    ///
    /// All distances are in metres (world-space, human scale).
    /// </summary>
    [CreateAssetMenu(menuName = "GameDev/Town/Town Profile", fileName = "TownProfile")]
    public sealed class TownProfile : ScriptableObject
    {
        // ------------------------------------------------------------------
        //  Town shape
        // ------------------------------------------------------------------

        [Header("Town Shape")]
        [Tooltip("Minimum town radius from the central landmark to the boundary, in metres.")]
        public float TownRadiusMin = 200f;

        [Tooltip("Maximum town radius from the central landmark to the boundary, in metres.")]
        public float TownRadiusMax = 320f;

        [Tooltip("Amplitude of random radial perturbation applied to the boundary polygon vertices (metres). " +
                 "Higher = more irregular / organic silhouette.")]
        public float BoundaryIrregularity = 30f;

        [Tooltip("When true, a town wall is generated along the boundary and portals are always Gates " +
                 "unless a river forces a Bridge.")]
        public bool HasWall = false;

        // ------------------------------------------------------------------
        //  Terrain features
        // ------------------------------------------------------------------

        [Header("Terrain Features")]
        [Tooltip("Probability (0–1) that the town is bordered by a river or coastline.")]
        [Range(0f, 1f)]
        public float RiverCoastProbability = 0.35f;

        [Tooltip("When true, a ridge (hill crest) polyline is generated and influences " +
                 "district wealth biasing (rich districts favour high ground).")]
        public bool HasRidge = false;

        // ------------------------------------------------------------------
        //  Portals
        // ------------------------------------------------------------------

        [Header("Portals")]
        [Tooltip("Minimum number of portals (arterial exit points) on the boundary.")]
        public int PortalCountMin = 3;

        [Tooltip("Maximum number of portals on the boundary.")]
        public int PortalCountMax = 6;

        [Tooltip("Relative weights for portal type selection.")]
        public PortalTypeWeights PortalWeights = new PortalTypeWeights();

        // ------------------------------------------------------------------
        //  Road class settings
        // ------------------------------------------------------------------

        [Header("Road Classes — Arterial (~8–12 m)")]
        public RoadClassSettings Arterial = new RoadClassSettings
        {
            WidthMin           = 8f,
            WidthMax           = 12f,
            CurvatureAmplitude = 6f,
            JitterAmplitude    = 1f
        };

        [Header("Road Classes — Secondary (~4–6 m)")]
        public RoadClassSettings Secondary = new RoadClassSettings
        {
            WidthMin           = 4f,
            WidthMax           = 6f,
            CurvatureAmplitude = 4f,
            JitterAmplitude    = 0.8f
        };

        [Header("Road Classes — Tertiary (~3–4 m)")]
        public RoadClassSettings Tertiary = new RoadClassSettings
        {
            WidthMin           = 3f,
            WidthMax           = 4f,
            CurvatureAmplitude = 3f,
            JitterAmplitude    = 0.6f
        };

        [Header("Road Classes — Alley (~1.5–3 m)")]
        public RoadClassSettings Alley = new RoadClassSettings
        {
            WidthMin           = 1.5f,
            WidthMax           = 3f,
            CurvatureAmplitude = 2f,
            JitterAmplitude    = 0.5f
        };

        [Header("Road Classes — Service (~1.5–2 m)")]
        public RoadClassSettings Service = new RoadClassSettings
        {
            WidthMin           = 1.5f,
            WidthMax           = 2f,
            CurvatureAmplitude = 1f,
            JitterAmplitude    = 0.3f
        };

        // ------------------------------------------------------------------
        //  District profiles per wealth class
        // ------------------------------------------------------------------

        [Header("District Profile — Rich")]
        [Tooltip("Generation parameters for wealthy districts: large blocks, wide streets, gardens.")]
        public WealthClassSettings RichDistrict = new WealthClassSettings
        {
            Profile = new DistrictProfile
            {
                BlockSize          = 60f,
                PlotFrontageMin    = 10f,
                PlotFrontageMax    = 22f,
                PlotDepthMin       = 20f,
                PlotDepthMax       = 40f,
                StreetDensity      = 0.3f,
                Setback            = 3f,
                FloorsMin          = 2,
                FloorsMax          = 4,
                CourtyardProbability = 0.05f,
                GardenProbability    = 0.30f,
                DeadEndProbability   = 0.02f,
                ShortcutProbability  = 0.02f
            }
        };

        [Header("District Profile — Mixed")]
        [Tooltip("Generation parameters for mixed districts: medium block sizes and variety.")]
        public WealthClassSettings MixedDistrict = new WealthClassSettings
        {
            Profile = new DistrictProfile
            {
                BlockSize          = 40f,
                PlotFrontageMin    = 6f,
                PlotFrontageMax    = 14f,
                PlotDepthMin       = 12f,
                PlotDepthMax       = 28f,
                StreetDensity      = 0.5f,
                Setback            = 1.5f,
                FloorsMin          = 2,
                FloorsMax          = 5,
                CourtyardProbability = 0.15f,
                GardenProbability    = 0.15f,
                DeadEndProbability   = 0.05f,
                ShortcutProbability  = 0.05f
            }
        };

        [Header("District Profile — Poor")]
        [Tooltip("Generation parameters for poor districts: dense small blocks, alleys, courtyards.")]
        public WealthClassSettings PoorDistrict = new WealthClassSettings
        {
            Profile = new DistrictProfile
            {
                BlockSize          = 25f,
                PlotFrontageMin    = 4f,
                PlotFrontageMax    = 8f,
                PlotDepthMin       = 8f,
                PlotDepthMax       = 18f,
                StreetDensity      = 0.75f,
                Setback            = 0.5f,
                FloorsMin          = 3,
                FloorsMax          = 6,
                CourtyardProbability = 0.35f,
                GardenProbability    = 0.05f,
                DeadEndProbability   = 0.15f,
                ShortcutProbability  = 0.12f
            }
        };

        // ------------------------------------------------------------------
        //  Landmark
        // ------------------------------------------------------------------

        [Header("Landmark")]
        [Tooltip("Relative weights for landmark type selection.")]
        public LandmarkKindWeights LandmarkWeights = new LandmarkKindWeights();

        // ------------------------------------------------------------------
        //  Open spaces
        // ------------------------------------------------------------------

        [Header("Open Spaces")]
        [Tooltip("Minimum radius of junction plazas in metres.")]
        public float PlazaSizeMin = 8f;

        [Tooltip("Maximum radius of junction plazas in metres.")]
        public float PlazaSizeMax = 20f;

        [Tooltip("Average number of wells per square kilometre of town area.")]
        public float WellDensityPerKm2 = 4f;

        // ======================================================================
        //  Convenience accessors
        // ======================================================================

        /// <summary>
        /// Returns the <see cref="RoadClassSettings"/> for the given <see cref="RoadClass"/>.
        /// </summary>
        public RoadClassSettings GetRoadClassSettings(RoadClass cls)
        {
            switch (cls)
            {
                case RoadClass.Arterial:  return Arterial;
                case RoadClass.Secondary: return Secondary;
                case RoadClass.Tertiary:  return Tertiary;
                case RoadClass.Alley:     return Alley;
                case RoadClass.Service:   return Service;
                default:                  return Secondary;
            }
        }

        /// <summary>
        /// Returns the default <see cref="DistrictProfile"/> for the given
        /// <see cref="WealthClass"/>.  The caller should copy the profile
        /// (use the copy constructor) rather than mutating the shared reference.
        /// </summary>
        public DistrictProfile GetDefaultDistrictProfile(WealthClass wealth)
        {
            switch (wealth)
            {
                case WealthClass.Rich:  return RichDistrict.Profile;
                case WealthClass.Mixed: return MixedDistrict.Profile;
                case WealthClass.Poor:  return PoorDistrict.Profile;
                default:                return MixedDistrict.Profile;
            }
        }
    }
}
