using System;
using System.Collections.Generic;
using GameDev.World.Water;
using UnityEngine;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// Soft exclusion around gameplay clearings (roads, POIs, settlements, planned lakes).
    /// Returns a 0..1 multiplier — 0 at the cleared core, 1 in undisturbed wild.
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
        private readonly IReadOnlyList<LakeDefinition> lakes;
        private readonly float lakeShoreFalloff;

        public VegetationClearance(
            IReadOnlyList<Disk> disks,
            IReadOnlyList<Vector2> roadSamples,
            float roadClearRadius,
            float roadFalloff,
            IReadOnlyList<LakeDefinition> lakes = null,
            float lakeShoreFalloffMeters = 6f)
        {
            this.disks = disks ?? Array.Empty<Disk>();
            this.roadSamples = roadSamples ?? Array.Empty<Vector2>();
            this.roadClearRadius = Mathf.Max(0f, roadClearRadius);
            this.roadFalloff = Mathf.Max(0.01f, roadFalloff);
            this.lakes = lakes ?? Array.Empty<LakeDefinition>();
            this.lakeShoreFalloff = Mathf.Max(0.01f, lakeShoreFalloffMeters);
        }

        public static VegetationClearance None { get; } =
            new VegetationClearance(Array.Empty<Disk>(), Array.Empty<Vector2>(), 0f, 1f);

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
                if (mul <= 0f)
                {
                    return 0f;
                }
            }

            for (var i = 0; i < lakes.Count; i++)
            {
                // Positive inside basin → fully excluded; negative outside with shore falloff.
                var sd = lakes[i].BasinSignedDistance(worldX, worldZ);
                if (sd >= 0f)
                {
                    return 0f;
                }

                mul = Mathf.Min(mul, FalloffMultiplier(-sd, 0f, lakeShoreFalloff));
                if (mul <= 0f)
                {
                    return 0f;
                }
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
