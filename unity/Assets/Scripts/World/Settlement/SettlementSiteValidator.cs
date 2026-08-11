using GameDev.World.Water;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// Dry-land checks for settlement placement (S6). A lot is wet if terrain Y sits at or
    /// below the water surface (+ freeboard).
    /// </summary>
    public static class SettlementSiteValidator
    {
        public const float DefaultFreeboardMeters = 1.5f;

        public static bool IsPointDry(
            float worldX,
            float worldZ,
            float groundY,
            IWaterSampler water,
            float freeboardMeters = DefaultFreeboardMeters)
        {
            if (water == null)
                return true;
            if (!water.TryGetWaterLevel(worldX, worldZ, out var waterY))
                return true;
            return groundY > waterY + freeboardMeters;
        }

        public static bool IsFabricDry(
            SettlementFabricLayout fabric,
            IWaterSampler water,
            float freeboardMeters = DefaultFreeboardMeters)
        {
            if (fabric == null)
                return false;
            if (water == null)
                return true;

            for (var i = 0; i < fabric.Properties.Count; i++)
            {
                if (!IsParcelDry(fabric.Properties[i].Parcel, water, freeboardMeters))
                    return false;
            }

            return true;
        }

        public static bool IsParcelDry(
            PlotParcel parcel,
            IWaterSampler water,
            float freeboardMeters = DefaultFreeboardMeters)
        {
            if (water == null)
                return true;

            var yaw = Quaternion.Euler(0f, parcel.YawDegrees, 0f);
            var o = parcel.Origin;
            var w = parcel.WidthMeters;
            var d = parcel.DepthMeters;
            var corners = new[]
            {
                o,
                o + yaw * new Vector3(w, 0f, 0f),
                o + yaw * new Vector3(0f, 0f, d),
                o + yaw * new Vector3(w, 0f, d)
            };

            for (var i = 0; i < corners.Length; i++)
            {
                var c = corners[i];
                if (!IsPointDry(c.x, c.z, parcel.Origin.y, water, freeboardMeters))
                    return false;
            }

            return true;
        }
    }
}
