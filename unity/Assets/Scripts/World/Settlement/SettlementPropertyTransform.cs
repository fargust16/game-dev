using System.Collections.Generic;
using GameDev.World.Buildings.Shell;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>World-space translate / pose for property specs (fabric composition + S6).</summary>
    public static class SettlementPropertyTransform
    {
        public static Vector3 ApplyPosePoint(Vector3 local, SettlementPose pose)
        {
            var yaw = Quaternion.Euler(0f, pose.YawDegrees, 0f);
            var flat = yaw * new Vector3(local.x, 0f, local.z);
            return pose.Position + flat;
        }

        public static PropertySpec ApplyPose(PropertySpec src, SettlementPose pose, int newPlotIndex)
        {
            if (src == null)
                return null;

            var parcel = src.Parcel;
            parcel.Origin = ApplyPosePoint(parcel.Origin, pose);
            parcel.YawDegrees = parcel.YawDegrees + pose.YawDegrees;

            var pad = src.BuildingPad;
            pad.Origin = ApplyPosePoint(pad.Origin, pose);
            pad.YawDegrees = pad.YawDegrees + pose.YawDegrees;

            var secondary = src.SecondaryBuildingPad;
            if (src.HasSecondaryBuilding)
            {
                secondary.Origin = ApplyPosePoint(secondary.Origin, pose);
                secondary.YawDegrees = secondary.YawDegrees + pose.YawDegrees;
            }

            var zones = new List<PropertyZone>(src.Zones.Count);
            for (var i = 0; i < src.Zones.Count; i++)
            {
                var z = src.Zones[i];
                z.Origin = ApplyPosePoint(z.Origin, pose);
                z.YawDegrees = z.YawDegrees + pose.YawDegrees;
                zones.Add(z);
            }

            var boundaries = new List<PropertyBoundarySegment>(src.Boundaries.Count);
            for (var i = 0; i < src.Boundaries.Count; i++)
            {
                var b = src.Boundaries[i];
                b.StartWorld = ApplyPosePoint(b.StartWorld, pose);
                b.EndWorld = ApplyPosePoint(b.EndWorld, pose);
                boundaries.Add(b);
            }

            var props = new List<PropertyPropSpec>(src.Props.Count);
            for (var i = 0; i < src.Props.Count; i++)
            {
                var p = src.Props[i];
                p.Position = ApplyPosePoint(p.Position, pose);
                p.YawDegrees = p.YawDegrees + pose.YawDegrees;
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
                SecondaryBuildingPad = secondary,
                IsOpenSpace = src.IsOpenSpace
            };
        }

        public static SettlementStreetSpec ApplyPoseStreet(SettlementStreetSpec street, SettlementPose pose)
        {
            street.Origin = ApplyPosePoint(street.Origin, pose);
            street.YawDegrees = street.YawDegrees + pose.YawDegrees;
            return street;
        }

        public static SettlementLaneSpec ApplyPoseLane(SettlementLaneSpec lane, SettlementPose pose)
        {
            lane.Origin = ApplyPosePoint(lane.Origin, pose);
            lane.YawDegrees = lane.YawDegrees + pose.YawDegrees;
            return lane;
        }

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
                SecondaryBuildingPad = secondary,
                IsOpenSpace = src.IsOpenSpace
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
