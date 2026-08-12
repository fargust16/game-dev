using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// Soft exclusion around gameplay clearings (roads, POIs, settlements). Returns a 0..1
    /// multiplier — 0 at the cleared core, 1 in undisturbed wild — instead of a hard boolean cut.
    /// </summary>
    public sealed class VegetationClearance
    {
        public readonly struct Disk
        {
            public readonly Vector2 Center;
            public readonly float ClearRadius;
            public readonly float Falloff;

            public Disk(Vector2 center, float clearRadius, float falloff)
            {
                Center = center;
                ClearRadius = Mathf.Max(0f, clearRadius);
                Falloff = Mathf.Max(0.01f, falloff);
            }
        }

        private readonly IReadOnlyList<Disk> disks;
        private readonly IReadOnlyList<Vector2> roadSamples;
        private readonly float roadClearRadius;
        private readonly float roadFalloff;

        public VegetationClearance(
            IReadOnlyList<Disk> disks,
            IReadOnlyList<Vector2> roadSamples,
            float roadClearRadius,
            float roadFalloff)
        {
            this.disks = disks ?? System.Array.Empty<Disk>();
            this.roadSamples = roadSamples ?? System.Array.Empty<Vector2>();
            this.roadClearRadius = Mathf.Max(0f, roadClearRadius);
            this.roadFalloff = Mathf.Max(0.01f, roadFalloff);
        }

        public static VegetationClearance None { get; } =
            new VegetationClearance(System.Array.Empty<Disk>(), System.Array.Empty<Vector2>(), 0f, 1f);

        public float Multiplier(float worldX, float worldZ)
        {
            var mul = 1f;
            var p = new Vector2(worldX, worldZ);

            for (var i = 0; i < disks.Count; i++)
            {
                var d = disks[i];
                var dist = Vector2.Distance(p, d.Center);
                mul = Mathf.Min(mul, FalloffMultiplier(dist, d.ClearRadius, d.Falloff));
                if (mul <= 0f)
                {
                    return 0f;
                }
            }

            if (roadSamples.Count > 0 && roadClearRadius > 0f)
            {
                var nearest = NearestRoadDistanceSq(p);
                var dist = Mathf.Sqrt(nearest);
                mul = Mathf.Min(mul, FalloffMultiplier(dist, roadClearRadius, roadFalloff));
            }

            return mul;
        }

        private float NearestRoadDistanceSq(Vector2 p)
        {
            var best = float.PositiveInfinity;
            for (var i = 0; i < roadSamples.Count; i++)
            {
                var d = (roadSamples[i] - p).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                }
            }

            return best;
        }

        public static float FalloffMultiplier(float distance, float clearRadius, float falloff)
        {
            if (distance <= clearRadius)
            {
                return 0f;
            }

            var t = (distance - clearRadius) / falloff;
            if (t >= 1f)
            {
                return 1f;
            }

            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
