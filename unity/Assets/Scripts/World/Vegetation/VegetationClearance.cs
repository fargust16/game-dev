using System;
using System.Collections.Generic;
using GameDev.World.Roads;
using UnityEngine;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// Soft exclusion around gameplay clearings (roads, POIs, settlements).
    /// Returns a 0..1 multiplier — near-0 at cleared cores, 1 in undisturbed wild.
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
        private readonly float roadMinAtCore;

        public VegetationClearance(
            IReadOnlyList<Disk> disks,
            IReadOnlyList<Vector2> roadSamples,
            float roadClearRadius,
            float roadFalloff,
            float roadMinAtCore = 0f)
        {
            this.disks = disks ?? Array.Empty<Disk>();
            this.roadSamples = roadSamples ?? Array.Empty<Vector2>();
            this.roadClearRadius = Mathf.Max(0f, roadClearRadius);
            this.roadFalloff = Mathf.Max(0.01f, roadFalloff);
            this.roadMinAtCore = Mathf.Clamp01(roadMinAtCore);
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
                    return 0f;
            }

            if (RoadLawnMask.HasCpuBake)
            {
                mul = Mathf.Min(mul, RoadLawnMask.SampleBakedLawn01(worldX, worldZ));
                if (mul <= 0f)
                    return 0f;
            }
            else if (roadSamples.Count > 0 && roadClearRadius > 0f)
            {
                var nearest = NearestRoadDistanceSq(p);
                var dist = Mathf.Sqrt(nearest);
                mul = Mathf.Min(
                    mul,
                    FalloffMultiplier(dist, roadClearRadius, roadFalloff, roadMinAtCore));
                if (mul <= 0f)
                    return 0f;
            }

            return mul;
        }

        private float NearestRoadDistanceSq(Vector2 p)
        {
            var dist = RoadSampleDistance.NearestMeters(p, roadSamples);
            return dist * dist;
        }

        public static float FalloffMultiplier(float distance, float clearRadius, float falloff)
        {
            return FalloffMultiplier(distance, clearRadius, falloff, minAtCore: 0f);
        }

        public static float FalloffMultiplier(float distance, float clearRadius, float falloff, float minAtCore)
        {
            minAtCore = Mathf.Clamp01(minAtCore);
            if (distance <= clearRadius)
                return minAtCore;
            var span = Mathf.Max(falloff, 0.01f);
            var t = (distance - clearRadius) / span;
            if (t >= 1f)
                return 1f;
            t = Mathf.Clamp01(t);
            t = t * t * (3f - 2f * t);
            return Mathf.Lerp(minAtCore, 1f, t);
        }
    }
}
