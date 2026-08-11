using GameDev.World.Terrain;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// Snaps a placed fabric to terrain: each property becomes a flat terrace at the
    /// highest sampled parcel corner (avoids burying shells into uphill ground).
    /// Does not carve the heightmap — V0 lot pads only.
    /// </summary>
    public static class SettlementTerrainConform
    {
        public static void SnapFabric(SettlementFabricLayout fabric, IWorldHeightSampler height)
        {
            if (fabric == null || height == null)
                return;

            for (var i = 0; i < fabric.Properties.Count; i++)
                SnapProperty(fabric.Properties[i], height);

            fabric.ConformStreetsAndLanes(height);
            // Street props follow snapped ribbon Y (S8).
            fabric.RebuildStreetProps(fabric.LayoutSeed != 0 ? fabric.LayoutSeed : 110201);
        }

        public static void SnapProperty(PropertySpec prop, IWorldHeightSampler height)
        {
            if (prop == null || height == null)
                return;

            if (!TryMaxParcelCornerY(prop.Parcel, height, out var targetY))
                return;

            var deltaY = targetY - prop.Parcel.Origin.y;
            if (Mathf.Abs(deltaY) < 0.001f)
                return;

            LiftProperty(prop, deltaY);
        }

        public static bool TryMaxParcelCornerY(
            PlotParcel parcel,
            IWorldHeightSampler height,
            out float maxY)
        {
            maxY = float.NegativeInfinity;
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

            var any = false;
            for (var i = 0; i < corners.Length; i++)
            {
                if (!height.TrySampleWorldY(corners[i].x, corners[i].z, out var y))
                    continue;
                any = true;
                if (y > maxY)
                    maxY = y;
            }

            if (!any)
            {
                maxY = 0f;
                return false;
            }

            return true;
        }

        private static void LiftProperty(PropertySpec prop, float deltaY)
        {
            var lift = Vector3.up * deltaY;

            var parcel = prop.Parcel;
            parcel.Origin += lift;
            prop.Parcel = parcel;

            var pad = prop.BuildingPad;
            pad.Origin += lift;
            prop.BuildingPad = pad;

            if (prop.HasSecondaryBuilding)
            {
                var sec = prop.SecondaryBuildingPad;
                sec.Origin += lift;
                prop.SecondaryBuildingPad = sec;
            }

            for (var i = 0; i < prop.Zones.Count; i++)
            {
                var z = prop.Zones[i];
                z.Origin += lift;
                prop.Zones[i] = z;
            }

            for (var i = 0; i < prop.Boundaries.Count; i++)
            {
                var b = prop.Boundaries[i];
                b.StartWorld += lift;
                b.EndWorld += lift;
                prop.Boundaries[i] = b;
            }

            for (var i = 0; i < prop.Props.Count; i++)
            {
                var p = prop.Props[i];
                p.Position += lift;
                prop.Props[i] = p;
            }
        }
    }
}
