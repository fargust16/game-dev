using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Buildings.Shell
{
    [CreateAssetMenu(fileName = "DIST_", menuName = "GameDev/Buildings/District Profile")]
    public sealed class DistrictProfileDef : ScriptableObject
    {
        public string Id;
        public DistrictKind Kind;

        [Header("Wealth / height")]
        [Range(1, 5)] public int WealthBias = 2;
        [Range(1, 5)] public int MinWealth = 1;
        [Range(1, 5)] public int MaxWealth = 4;
        public int MinFloors = 1;
        public int MaxFloors = 3;

        [Header("Plots / density")]
        public int MinWidthBays = 2;
        public int MaxWidthBays = 3;
        public int MinDepthBays = 2;
        public int MaxDepthBays = 3;
        public float GapMeters = 0.6f;
        [Range(0.2f, 1f)] public float Density = 0.85f;

        [Header("Grammar biases (0–1)")]
        [Range(0f, 1f)] public float ShopChance = 0.05f;
        [Range(0f, 1f)] public float ChimneyChance = 0.55f;
        [Range(0f, 1f)] public float ExtraChimneyChance = 0.1f;
        [Range(0f, 1f)] public float GableRoofChance = 0.75f;
        [Range(0f, 1f)] public float WindowDensity = 0.7f;
        [Range(0f, 1f)] public float PortalShopChance = 0.35f;
        public int MinTrimLevel;
        public int MaxTrimLevel = 1;
        public int CornerEveryN = 8;
        public bool FrontGardenDress;

        [Header("Massing weights — non-overlapping partitions only")]
        public float MassingBox = 1f;
        public float MassingLShape;
        public float MassingStepped;
        public float MassingRearWing;

        [Header("Placeholder palette")]
        public Color WallTint = new Color(0.58f, 0.55f, 0.5f);
        public Color RoofTint = new Color(0.28f, 0.22f, 0.2f);
        public Color TrimTint = new Color(0.65f, 0.6f, 0.5f);
        public Color WindowTint = new Color(0.5f, 0.62f, 0.75f);
        public Color DoorTint = new Color(0.35f, 0.22f, 0.15f);
        public Color ShopTint = new Color(0.25f, 0.4f, 0.48f);

        public int RollWidthBays(Random rng)
        {
            var max = Mathf.Max(MinWidthBays, MaxWidthBays);
            return rng.Next(MinWidthBays, max + 1);
        }

        public int RollDepthBays(Random rng)
        {
            var max = Mathf.Max(MinDepthBays, MaxDepthBays);
            return rng.Next(MinDepthBays, max + 1);
        }

        public float EffectiveGapMeters()
        {
            var t = Mathf.Clamp01(Density);
            return Mathf.Lerp(GapMeters * 2.5f, GapMeters, t);
        }

        public MassingType RollMassing(Random rng)
        {
            var total = MassingBox + MassingLShape + MassingStepped + MassingRearWing;
            if (total <= 0f)
                return MassingType.Box;

            var roll = (float)rng.NextDouble() * total;
            var acc = MassingBox;
            if (roll <= acc) return MassingType.Box;
            acc += MassingLShape;
            if (roll <= acc) return MassingType.LShape;
            acc += MassingStepped;
            if (roll <= acc) return MassingType.Stepped;
            return MassingType.RearWing;
        }
    }
}
