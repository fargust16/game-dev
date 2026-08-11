using System;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>Service / rear lane strip between block frontages (S4).</summary>
    [Serializable]
    public struct SettlementLaneSpec
    {
        public Vector3 Origin;
        public float WidthMeters;
        public float LengthMeters;
        public float YawDegrees;

        public static SettlementLaneSpec Create(Vector3 origin, float lengthMeters, float widthMeters, float yawDegrees = 0f)
        {
            return new SettlementLaneSpec
            {
                Origin = origin,
                LengthMeters = Mathf.Max(0f, lengthMeters),
                WidthMeters = Mathf.Max(0f, widthMeters),
                YawDegrees = yawDegrees
            };
        }
    }
}
