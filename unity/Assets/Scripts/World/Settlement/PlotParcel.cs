using System;
using GameDev.World.Buildings.Shell;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// Geometric land lot (frontage × depth). Distinct from Shell
    /// <see cref="PlotFootprint"/>, which is only the building pad inside the lot.
    /// Street front is the local −Z edge; depth extends along local +Z.
    /// </summary>
    [Serializable]
    public struct PlotParcel
    {
        public int WidthBays;
        public int DepthBays;
        public Vector3 Origin;
        public float YawDegrees;

        public float WidthMeters => WidthBays * ModularGrid.BayMeters;
        public float DepthMeters => DepthBays * ModularGrid.BayMeters;

        public static PlotParcel Rect(int widthBays, int depthBays, Vector3 origin, float yawDegrees = 0f)
        {
            return new PlotParcel
            {
                WidthBays = Mathf.Max(1, widthBays),
                DepthBays = Mathf.Max(1, depthBays),
                Origin = origin,
                YawDegrees = yawDegrees
            };
        }

        public Vector3 LocalToWorld(float localX, float localZ)
        {
            var yaw = Quaternion.Euler(0f, YawDegrees, 0f);
            return Origin + yaw * new Vector3(localX, 0f, localZ);
        }

        public bool ContainsPad(PlotFootprint pad, float epsilon = 0.01f)
        {
            var yaw = Quaternion.Euler(0f, YawDegrees, 0f);
            var inv = Quaternion.Inverse(yaw);
            var local = inv * (pad.Origin - Origin);
            var padW = pad.WidthMeters;
            var padD = pad.DepthMeters;
            if (local.x < -epsilon || local.z < -epsilon)
                return false;
            if (local.x + padW > WidthMeters + epsilon)
                return false;
            if (local.z + padD > DepthMeters + epsilon)
                return false;
            return true;
        }
    }
}
