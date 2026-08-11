using System.Collections.Generic;
using GameDev.World.Buildings.Shell;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// Builds property fence segments as world-space Start→End lines on the parcel rectangle.
    /// Parcel local: +X = street frontage, +Z = into lot, front edge at local Z=0, back at Z=depth.
    /// </summary>
    public static class PropertyBoundaryBuilder
    {
        public const float DefaultGateWidthMeters = 2f;

        public static List<PropertyBoundarySegment> Build(
            PlotParcel parcel,
            PropertyArchetypeDef arch,
            bool isCorner,
            float gateCenterLocalX = -1f,
            float frontSetbackMeters = 0f,
            float buildingDepthMeters = 0f)
        {
            var list = new List<PropertyBoundarySegment>(12);
            if (arch == null || arch.BoundaryStyle == BoundaryStyle.None)
                return list;

            var style = arch.BoundaryStyle;
            var height = PropertyBoundarySegment.HeightFor(style);
            var thick = PropertyBoundarySegment.ThicknessFor(style);
            var w = parcel.WidthMeters;
            var d = parcel.DepthMeters;

            // Closed lot rectangle corners in local XZ (Y ignored).
            // FL──FR          front z=0 (street)
            // │    │
            // BL──BR          back  z=d
            var fl = Local(parcel, 0f, 0f);
            var fr = Local(parcel, w, 0f);
            var bl = Local(parcel, 0f, d);
            var br = Local(parcel, w, d);

            var fullPerimeter = arch.BoundaryFront && arch.BoundarySides && arch.BoundaryBack;
            if (fullPerimeter)
            {
                // Affluent / Rich / Elite: closed rectangle around the whole plot.
                AddFront(list, parcel, arch, style, height, thick, w, fl, fr, gateCenterLocalX);
                AddSeg(list, PlotFace.Back, style, false, bl, br, height, thick);
                AddSeg(list, PlotFace.Left, style, false, fl, bl, height, thick);
                AddSeg(list, PlotFace.Right, style, false, fr, br, height, thick);
                return list;
            }

            // Worker-style: open street front, fence only the rear yard (U behind the building).
            var frontSetback = Mathf.Clamp(frontSetbackMeters, 0f, d);
            var buildingBack = Mathf.Clamp(frontSetback + Mathf.Max(0f, buildingDepthMeters), 0f, d);

            if (arch.BoundaryBack)
            {
                AddSeg(list, PlotFace.Back, style, false, bl, br, height, thick);
            }

            if (arch.BoundarySides && buildingBack < d - 0.05f)
            {
                var leftStart = Local(parcel, 0f, buildingBack);
                var rightStart = Local(parcel, w, buildingBack);
                AddSeg(list, PlotFace.Left, style, false, leftStart, bl, height, thick);
                AddSeg(list, PlotFace.Right, style, false, rightStart, br, height, thick);
            }

            if (arch.BoundaryFront)
            {
                AddFront(list, parcel, arch, style, height, thick, w, fl, fr, gateCenterLocalX);
            }
            else if (isCorner && arch.BoundarySides && frontSetback > 0.05f)
            {
                var leftEnd = Local(parcel, 0f, frontSetback);
                AddSeg(list, PlotFace.Left, style, false, fl, leftEnd, height, thick);
            }

            return list;
        }

        private static void AddFront(
            List<PropertyBoundarySegment> list,
            PlotParcel parcel,
            PropertyArchetypeDef arch,
            BoundaryStyle style,
            float height,
            float thick,
            float width,
            Vector3 fl,
            Vector3 fr,
            float gateCenterLocalX)
        {
            if (!arch.HasFrontGate || width <= DefaultGateWidthMeters + 0.5f)
            {
                AddSeg(list, PlotFace.Front, style, false, fl, fr, height, thick);
                return;
            }

            var gateW = Mathf.Min(DefaultGateWidthMeters, width * 0.4f);
            var center = gateCenterLocalX >= 0f ? gateCenterLocalX : width * 0.5f;
            var gateStartX = Mathf.Clamp(center - gateW * 0.5f, 0f, width - gateW);
            var gateEndX = gateStartX + gateW;

            var gateL = Local(parcel, gateStartX, 0f);
            var gateR = Local(parcel, gateEndX, 0f);

            if (gateStartX > 0.05f)
                AddSeg(list, PlotFace.Front, style, false, fl, gateL, height, thick);
            if (width - gateEndX > 0.05f)
                AddSeg(list, PlotFace.Front, style, false, gateR, fr, height, thick);

            AddSeg(list, PlotFace.Front, style, true, gateL, gateR, height * 0.85f, thick * 1.15f);
        }

        private static Vector3 Local(PlotParcel parcel, float x, float z)
        {
            return parcel.LocalToWorld(x, z);
        }

        private static void AddSeg(
            List<PropertyBoundarySegment> list,
            PlotFace edge,
            BoundaryStyle style,
            bool isGate,
            Vector3 start,
            Vector3 end,
            float height,
            float thick)
        {
            if (Vector3.Distance(start, end) <= 0.05f || height <= 0.01f)
                return;

            list.Add(new PropertyBoundarySegment
            {
                Edge = edge,
                Style = style,
                IsGate = isGate,
                StartWorld = start,
                EndWorld = end,
                HeightMeters = height,
                ThicknessMeters = thick
            });
        }
    }
}
