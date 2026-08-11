using System;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// Axis-aligned street ribbon (S5). Local +X along length, local +Z across width.
    /// E–W primary: yaw 0. N–S cross: yaw −90 (Unity +90 would point south).
    /// Centerline is the mid-line of the ribbon — not a graph edge.
    /// </summary>
    [Serializable]
    public struct SettlementStreetSpec
    {
        public SettlementStreetKind Kind;
        public Vector3 Origin;
        public float LengthMeters;
        public float WidthMeters;
        public float YawDegrees;

        public static SettlementStreetSpec Create(
            SettlementStreetKind kind,
            Vector3 origin,
            float lengthMeters,
            float widthMeters,
            float yawDegrees = 0f)
        {
            return new SettlementStreetSpec
            {
                Kind = kind,
                Origin = origin,
                LengthMeters = Mathf.Max(0f, lengthMeters),
                WidthMeters = Mathf.Max(0f, widthMeters),
                YawDegrees = yawDegrees
            };
        }

        public Vector3 CenterlineStart
        {
            get
            {
                var yaw = Quaternion.Euler(0f, YawDegrees, 0f);
                return Origin + yaw * new Vector3(0f, 0f, WidthMeters * 0.5f);
            }
        }

        public Vector3 CenterlineEnd
        {
            get
            {
                var yaw = Quaternion.Euler(0f, YawDegrees, 0f);
                return Origin + yaw * new Vector3(LengthMeters, 0f, WidthMeters * 0.5f);
            }
        }
    }
}
