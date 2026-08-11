using GameDev.World.Buildings.Shell;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// Immutable fabric recipe: lot size, setbacks, and which property zones exist.
    /// Shell silhouette still comes from <see cref="DistrictProfileDef"/> + building archetypes.
    /// </summary>
    [CreateAssetMenu(fileName = "PROP_", menuName = "GameDev/Settlement/Property Archetype")]
    public sealed class PropertyArchetypeDef : ScriptableObject
    {
        public string Id;
        public DistrictKind DistrictKind;

        [Header("Lot (bays)")]
        public int MinFrontageBays = 2;
        public int MaxFrontageBays = 3;
        public int MinDepthBays = 2;
        public int MaxDepthBays = 3;

        [Header("Building pad (bays)")]
        public int MinBuildingWidthBays = 2;
        public int MaxBuildingWidthBays = 3;
        public int MinBuildingDepthBays = 2;
        public int MaxBuildingDepthBays = 3;

        [Header("Setbacks / yards (bays)")]
        [Tooltip("Depth of front garden / setback before the building pad.")]
        public int FrontSetbackBays;
        [Tooltip("Minimum rear yard depth; remainder of lot after pad may be larger.")]
        public int MinRearYardBays;
        [Tooltip("If > 0, carve this many bays from the rear as Service.")]
        public int ServiceDepthBays;

        [Header("Zoning flags")]
        public bool HasFrontGarden;
        public bool HasRearYard;
        public bool HasService;
        public bool FlushToStreet = true;
        public bool BuildingFullFrontage = true;

        [Header("Boundary (S2)")]
        public BoundaryStyle BoundaryStyle = BoundaryStyle.None;
        public bool BoundaryFront = true;
        public bool BoundarySides = true;
        public bool BoundaryBack = true;
        public bool HasFrontGate;

        [Header("Elite / drive (S2)")]
        public bool HasDrive;
        public int DriveWidthBays = 2;
        public bool HasSecondaryBuilding;
        public int SecondaryWidthBays = 3;
        public int SecondaryDepthBays = 2;

        [Header("Street manners")]
        [Tooltip("When true, StreetContext.Back uses Alley (shops / service rear).")]
        public bool RearIsAlley;

        [Header("Spacing")]
        public float GapMeters = 0.6f;

        public int RollFrontageBays(Random rng)
        {
            var max = Mathf.Max(MinFrontageBays, MaxFrontageBays);
            return rng.Next(MinFrontageBays, max + 1);
        }

        public int RollDepthBays(Random rng)
        {
            var max = Mathf.Max(MinDepthBays, MaxDepthBays);
            return rng.Next(MinDepthBays, max + 1);
        }

        public int RollBuildingWidthBays(Random rng)
        {
            var max = Mathf.Max(MinBuildingWidthBays, MaxBuildingWidthBays);
            return rng.Next(MinBuildingWidthBays, max + 1);
        }

        public int RollBuildingDepthBays(Random rng)
        {
            var max = Mathf.Max(MinBuildingDepthBays, MaxBuildingDepthBays);
            return rng.Next(MinBuildingDepthBays, max + 1);
        }
    }
}
