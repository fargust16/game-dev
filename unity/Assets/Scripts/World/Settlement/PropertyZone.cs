using System;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>Axis-aligned zone inside a property lot (world XZ; yaw from parcel).</summary>
    [Serializable]
    public struct PropertyZone
    {
        public PropertyZoneKind Kind;
        public Vector3 Origin;
        public float WidthMeters;
        public float DepthMeters;
        public float YawDegrees;

        public static PropertyZone Create(
            PropertyZoneKind kind,
            Vector3 origin,
            float widthMeters,
            float depthMeters,
            float yawDegrees)
        {
            return new PropertyZone
            {
                Kind = kind,
                Origin = origin,
                WidthMeters = Mathf.Max(0f, widthMeters),
                DepthMeters = Mathf.Max(0f, depthMeters),
                YawDegrees = yawDegrees
            };
        }
    }
}
