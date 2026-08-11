using System;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// World placement for a settlement fabric (S6). Local fabric XZ is rotated by
    /// <see cref="YawDegrees"/> then translated to <see cref="Position"/> (Y = terrain).
    /// </summary>
    [Serializable]
    public struct SettlementPose
    {
        public Vector3 Position;
        public float YawDegrees;
        public int SettlementSeed;
        public string SourcePoiId;

        public static SettlementPose Create(
            Vector3 position,
            float yawDegrees,
            int settlementSeed,
            string sourcePoiId = null)
        {
            return new SettlementPose
            {
                Position = position,
                YawDegrees = yawDegrees,
                SettlementSeed = settlementSeed,
                SourcePoiId = sourcePoiId ?? string.Empty
            };
        }
    }
}
