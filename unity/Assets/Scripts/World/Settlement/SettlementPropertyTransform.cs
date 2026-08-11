using System.Collections.Generic;
using GameDev.World.Buildings.Shell;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>World-space translate for property specs (fabric composition).</summary>
    public static class SettlementPropertyTransform
    {
        public static PropertySpec Translate(PropertySpec src, Vector3 delta, int newPlotIndex)
        {
            if (src == null)
                return null;

            var parcel = src.Parcel;
            parcel.Origin += delta;

            var pad = src.BuildingPad;
            pad.Origin += delta;

            var secondary = src.SecondaryBuildingPad;
            if (src.HasSecondaryBuilding)
                secondary.Origin += delta;

            var zones = new List<PropertyZone>(src.Zones.Count);
            for (var i = 0; i < src.Zones.Count; i++)
            {
                var z = src.Zones[i];
                z.Origin += delta;
                zones.Add(z);
            }

            var boundaries = new List<PropertyBoundarySegment>(src.Boundaries.Count);
            for (var i = 0; i < src.Boundaries.Count; i++)
            {
                var b = src.Boundaries[i];
                b.StartWorld += delta;
                b.EndWorld += delta;
                boundaries.Add(b);
            }

            var props = new List<PropertyPropSpec>(src.Props.Count);
            for (var i = 0; i < src.Props.Count; i++)
            {
                var p = src.Props[i];
                p.Position += delta;
                props.Add(p);
            }

            return new PropertySpec
            {
                PlotIndex = newPlotIndex,
                DistrictId = src.DistrictId,
                ArchetypeId = src.ArchetypeId,
                Parcel = parcel,
                Zones = zones,
                Boundaries = boundaries,
                Props = props,
                BuildingPad = pad,
                Street = src.Street,
                HasSecondaryBuilding = src.HasSecondaryBuilding,
                SecondaryBuildingPad = secondary
            };
        }

        public static SettlementLaneSpec TranslateLane(SettlementLaneSpec lane, Vector3 delta)
        {
            lane.Origin += delta;
            return lane;
        }

        public static void MeasureFrontageAndDepth(
            SettlementBlockLayout block,
            out float frontageMeters,
            out float southFrontZ,
            out float northFrontZ)
        {
            frontageMeters = 0f;
            southFrontZ = float.PositiveInfinity;
            northFrontZ = float.NegativeInfinity;
            if (block == null || block.Properties.Count == 0)
            {
                southFrontZ = 0f;
                northFrontZ = 0f;
                return;
            }

            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            for (var i = 0; i < block.Properties.Count; i++)
            {
                var parcel = block.Properties[i].Parcel;
                var yaw = Quaternion.Euler(0f, parcel.YawDegrees, 0f);
                var fl = parcel.Origin;
                var fr = parcel.Origin + yaw * new Vector3(parcel.WidthMeters, 0f, 0f);
                minX = Mathf.Min(minX, Mathf.Min(fl.x, fr.x));
                maxX = Mathf.Max(maxX, Mathf.Max(fl.x, fr.x));
                southFrontZ = Mathf.Min(southFrontZ, Mathf.Min(fl.z, fr.z));
                northFrontZ = Mathf.Max(northFrontZ, Mathf.Max(fl.z, fr.z));
            }

            frontageMeters = Mathf.Max(0f, maxX - minX);
            if (float.IsInfinity(southFrontZ))
                southFrontZ = 0f;
            if (float.IsInfinity(northFrontZ))
                northFrontZ = 0f;
        }
    }
}
