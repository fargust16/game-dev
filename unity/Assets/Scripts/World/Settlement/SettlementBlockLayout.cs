using System.Collections.Generic;
using GameDev.World.Buildings.Shell;
using GameDev.World.Seed;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// Deterministic block parcelizer: single strip, rear-lane strip, or double-frontage around a service lane.
    /// Parcel local: +X along frontage, +Z into lot from the street front.
    /// </summary>
    public sealed class SettlementBlockLayout : ISettlementPropertyLayout
    {
        public const float DefaultLaneWidthMeters = 4f;

        private readonly List<PropertySpec> _properties = new List<PropertySpec>();
        private readonly List<SettlementPlotSpec> _plots = new List<SettlementPlotSpec>();

        public string SettlementId { get; private set; }
        public DistrictKind DistrictKind { get; private set; }
        public bool HasRearLane { get; private set; }
        public SettlementLaneSpec Lane { get; private set; }
        public IReadOnlyList<PropertySpec> Properties => _properties;
        public IReadOnlyList<SettlementPlotSpec> Plots => _plots;

        /// <summary>Single street strip (S1 default).</summary>
        public static SettlementBlockLayout Create(
            int seed,
            DistrictKind districtKind,
            int plotCount = 8,
            PropertyArchetypeDef archetype = null)
        {
            return CreateInternal(seed, districtKind, plotCount, archetype, rearLane: false, doubleFrontage: false);
        }

        /// <summary>Single strip facing a street with a service lane behind the lots (S4).</summary>
        public static SettlementBlockLayout CreateWithRearLane(
            int seed,
            DistrictKind districtKind,
            int plotCount = 8,
            float laneWidthMeters = DefaultLaneWidthMeters,
            PropertyArchetypeDef archetype = null)
        {
            return CreateInternal(
                seed,
                districtKind,
                plotCount,
                archetype,
                rearLane: true,
                doubleFrontage: false,
                laneWidthMeters);
        }

        /// <summary>
        /// Two rows facing outward streets with a shared central service lane (S4).
        /// <paramref name="plotsPerSide"/> lots on the south row and the same count on the north row.
        /// </summary>
        public static SettlementBlockLayout CreateDoubleFrontage(
            int seed,
            DistrictKind districtKind,
            int plotsPerSide = 4,
            float laneWidthMeters = DefaultLaneWidthMeters,
            PropertyArchetypeDef archetype = null)
        {
            return CreateInternal(
                seed,
                districtKind,
                plotsPerSide,
                archetype,
                rearLane: true,
                doubleFrontage: true,
                laneWidthMeters);
        }

        private static SettlementBlockLayout CreateInternal(
            int seed,
            DistrictKind districtKind,
            int plotCount,
            PropertyArchetypeDef archetype,
            bool rearLane,
            bool doubleFrontage,
            float laneWidthMeters = DefaultLaneWidthMeters)
        {
            plotCount = Mathf.Max(1, plotCount);
            var layout = new SettlementBlockLayout
            {
                SettlementId = doubleFrontage
                    ? $"block2_{districtKind}_{seed}"
                    : rearLane
                        ? $"blockLane_{districtKind}_{seed}"
                        : $"block_{districtKind}_{seed}",
                DistrictKind = districtKind,
                HasRearLane = rearLane || doubleFrontage
            };

            var arch = archetype != null ? archetype : PropertyArchetypeFactory.ForDistrict(districtKind);
            var districtId = DistrictIds.FromKind(districtKind);
            var ws = new WorldSeed(seed);
            var gap = Mathf.Max(0f, arch.GapMeters);
            var forceAlleyBack = rearLane || doubleFrontage || arch.RearIsAlley;

            // --- South row (front toward −Z / street at z≈0) ---
            var south = BuildRow(
                ws,
                arch,
                districtId,
                plotCount,
                rowId: "S",
                startIndex: 0,
                originZ: 0f,
                yawDegrees: 0f,
                forceAlleyBack,
                mirrorXForYaw180: false,
                gap,
                mirrorWidths: null);
            for (var i = 0; i < south.Count; i++)
            {
                layout._properties.Add(south[i]);
                layout._plots.Add(south[i].ToPlotSpec());
            }

            var southDepth = MaxParcelDepth(south);
            var rowLength = RowLengthMeters(south, gap);

            if (!rearLane && !doubleFrontage)
                return layout;

            var laneWidth = Mathf.Max(2f, laneWidthMeters);
            var laneOrigin = new Vector3(0f, 0f, southDepth);
            layout.Lane = SettlementLaneSpec.Create(laneOrigin, rowLength, laneWidth, 0f);

            if (!doubleFrontage)
                return layout;

            // Align north lots to south frontages; front-left local sits on the east street corner (yaw=180).
            var mirrorWidths = new float[south.Count];
            for (var i = 0; i < south.Count; i++)
                mirrorWidths[i] = south[i].Parcel.WidthMeters;

            // North street edge: southDepth + lane + mirrored depth budget (south max).
            var northStreetZ = southDepth + laneWidth + southDepth;
            var north = BuildRow(
                ws,
                arch,
                districtId,
                plotCount,
                rowId: "N",
                startIndex: plotCount,
                originZ: northStreetZ,
                yawDegrees: 180f,
                forceAlleyBack,
                mirrorXForYaw180: true,
                gap,
                mirrorWidths);
            for (var i = 0; i < north.Count; i++)
            {
                layout._properties.Add(north[i]);
                layout._plots.Add(north[i].ToPlotSpec());
            }

            return layout;
        }

        private static List<PropertySpec> BuildRow(
            WorldSeed ws,
            PropertyArchetypeDef arch,
            string districtId,
            int plotCount,
            string rowId,
            int startIndex,
            float originZ,
            float yawDegrees,
            bool forceAlleyBack,
            bool mirrorXForYaw180,
            float gap,
            float[] mirrorWidths)
        {
            var list = new List<PropertySpec>(plotCount);
            var x = 0f;

            for (var i = 0; i < plotCount; i++)
            {
                var rng = ws.Fork($"settlement_parcel_{rowId}_{i}");
                var isCornerLeft = i == 0;
                var isCornerRight = i == plotCount - 1;
                var forcedWidth = mirrorWidths != null && i < mirrorWidths.Length
                    ? mirrorWidths[i]
                    : -1f;

                Vector3 origin;
                if (mirrorXForYaw180)
                {
                    // Front-left local sits on the east street corner of this lot.
                    var widthMeters = forcedWidth > 0f
                        ? forcedWidth
                        : arch.RollFrontageBays(rng) * ModularGrid.BayMeters;
                    origin = new Vector3(x + widthMeters, 0f, originZ);
                    list.Add(BuildProperty(
                        startIndex + i,
                        origin,
                        yawDegrees,
                        arch,
                        districtId,
                        rng,
                        isCornerLeft,
                        isCornerRight,
                        forceAlleyBack,
                        forcedFrontageMeters: widthMeters,
                        frontageAlreadyRolled: forcedWidth <= 0f));
                    x += widthMeters + gap;
                }
                else
                {
                    origin = new Vector3(x, 0f, originZ);
                    var prop = BuildProperty(
                        startIndex + i,
                        origin,
                        yawDegrees,
                        arch,
                        districtId,
                        rng,
                        isCornerLeft,
                        isCornerRight,
                        forceAlleyBack);
                    list.Add(prop);
                    x += prop.Parcel.WidthMeters + gap;
                }
            }

            return list;
        }

        private static float MaxParcelDepth(List<PropertySpec> row)
        {
            var max = 0f;
            for (var i = 0; i < row.Count; i++)
                max = Mathf.Max(max, row[i].Parcel.DepthMeters);
            return max;
        }

        private static float RowLengthMeters(List<PropertySpec> row, float gap)
        {
            if (row.Count == 0)
                return 0f;
            var len = 0f;
            for (var i = 0; i < row.Count; i++)
            {
                len += row[i].Parcel.WidthMeters;
                if (i < row.Count - 1)
                    len += gap;
            }

            return len;
        }

        private static PropertySpec BuildProperty(
            int index,
            Vector3 parcelOrigin,
            float yawDegrees,
            PropertyArchetypeDef arch,
            string districtId,
            Random rng,
            bool isCornerLeft,
            bool isCornerRight,
            bool forceAlleyBack,
            float forcedFrontageMeters = -1f,
            bool frontageAlreadyRolled = false)
        {
            int frontageBays;
            if (forcedFrontageMeters > 0f)
            {
                frontageBays = Mathf.Max(1, Mathf.RoundToInt(forcedFrontageMeters / ModularGrid.BayMeters));
                // Consume a frontage roll so depth/building streams stay aligned with Create().
                if (!frontageAlreadyRolled)
                    arch.RollFrontageBays(rng);
            }
            else
            {
                frontageBays = arch.RollFrontageBays(rng);
            }

            var lotDepthBays = arch.RollDepthBays(rng);

            var frontSetbackBays = arch.FlushToStreet ? 0 : Mathf.Max(0, arch.FrontSetbackBays);
            if (arch.HasFrontGarden)
                frontSetbackBays = Mathf.Max(1, frontSetbackBays);

            var serviceBays = arch.HasService ? Mathf.Max(0, arch.ServiceDepthBays) : 0;
            var minRear = arch.HasRearYard ? Mathf.Max(0, arch.MinRearYardBays) : 0;

            var driveBays = 0;
            if (arch.HasDrive)
            {
                driveBays = Mathf.Clamp(arch.DriveWidthBays, 1, Mathf.Max(1, frontageBays - 2));
                if (frontageBays - driveBays < 2)
                    frontageBays = driveBays + 2;
            }

            var maxBuildingDepth = lotDepthBays - frontSetbackBays - minRear - serviceBays;
            if (maxBuildingDepth < 1)
            {
                lotDepthBays += 1 - maxBuildingDepth;
                maxBuildingDepth = 1;
            }

            var buildingDepthBays = Mathf.Clamp(arch.RollBuildingDepthBays(rng), 1, maxBuildingDepth);
            var usedDepth = frontSetbackBays + buildingDepthBays + serviceBays;
            var rearBays = lotDepthBays - usedDepth;
            if (arch.HasRearYard && rearBays < minRear)
            {
                lotDepthBays += minRear - rearBays;
                rearBays = minRear;
            }
            else if (!arch.HasRearYard && rearBays > 0)
            {
                if (arch.HasService)
                    serviceBays += rearBays;
                else
                    buildingDepthBays += rearBays;
                rearBays = 0;
            }

            var usableWidthBays = frontageBays - driveBays;
            var buildingWidthBays = arch.BuildingFullFrontage
                ? usableWidthBays
                : Mathf.Clamp(arch.RollBuildingWidthBays(rng), 1, usableWidthBays);

            var sideOffsetBays = driveBays;
            if (!arch.BuildingFullFrontage)
                sideOffsetBays = driveBays + (usableWidthBays - buildingWidthBays) / 2;

            var parcel = new PlotParcel
            {
                WidthBays = frontageBays,
                DepthBays = lotDepthBays,
                Origin = parcelOrigin,
                YawDegrees = yawDegrees
            };

            var padOrigin = parcel.LocalToWorld(
                sideOffsetBays * ModularGrid.BayMeters,
                frontSetbackBays * ModularGrid.BayMeters);

            var isCorner = isCornerLeft || isCornerRight;
            var pad = isCorner
                ? PlotFootprint.Corner(buildingWidthBays, buildingDepthBays, padOrigin, yawDegrees)
                : PlotFootprint.Rect(buildingWidthBays, buildingDepthBays, padOrigin, yawDegrees);

            var street = BuildStreetContext(arch, isCornerLeft, isCornerRight, forceAlleyBack);

            var zones = new List<PropertyZone>();
            var yaw = parcel.YawDegrees;

            if (driveBays > 0)
            {
                zones.Add(PropertyZone.Create(
                    PropertyZoneKind.Drive,
                    parcel.LocalToWorld(0f, 0f),
                    driveBays * ModularGrid.BayMeters,
                    parcel.DepthMeters,
                    yaw));
            }

            if (frontSetbackBays > 0 && (arch.HasFrontGarden || !arch.FlushToStreet))
            {
                var gardenX = driveBays * ModularGrid.BayMeters;
                var gardenW = parcel.WidthMeters - gardenX;
                if (gardenW > 0.01f)
                {
                    zones.Add(PropertyZone.Create(
                        PropertyZoneKind.FrontGarden,
                        parcel.LocalToWorld(gardenX, 0f),
                        gardenW,
                        frontSetbackBays * ModularGrid.BayMeters,
                        yaw));
                }
            }

            zones.Add(PropertyZone.Create(
                PropertyZoneKind.BuildingPad,
                padOrigin,
                pad.WidthMeters,
                pad.DepthMeters,
                yaw));

            var rearStartZ = (frontSetbackBays + buildingDepthBays) * ModularGrid.BayMeters;
            var serviceZ = rearStartZ;
            if (arch.HasRearYard && rearBays > 0)
            {
                zones.Add(PropertyZone.Create(
                    PropertyZoneKind.RearYard,
                    parcel.LocalToWorld(driveBays > 0 ? driveBays * ModularGrid.BayMeters : 0f, rearStartZ),
                    parcel.WidthMeters - driveBays * ModularGrid.BayMeters,
                    rearBays * ModularGrid.BayMeters,
                    yaw));
                serviceZ = rearStartZ + rearBays * ModularGrid.BayMeters;
            }

            PlotFootprint secondaryPad = default;
            var hasSecondary = false;
            if (arch.HasService && serviceBays > 0)
            {
                zones.Add(PropertyZone.Create(
                    PropertyZoneKind.Service,
                    parcel.LocalToWorld(0f, serviceZ),
                    parcel.WidthMeters,
                    serviceBays * ModularGrid.BayMeters,
                    yaw));

                if (arch.HasSecondaryBuilding)
                {
                    var secW = Mathf.Clamp(arch.SecondaryWidthBays, 1, frontageBays);
                    var secD = Mathf.Clamp(arch.SecondaryDepthBays, 1, serviceBays);
                    var secLocalX = (frontageBays - secW) * ModularGrid.BayMeters;
                    var secOrigin = parcel.LocalToWorld(secLocalX, serviceZ);
                    secondaryPad = PlotFootprint.Rect(secW, secD, secOrigin, yawDegrees);
                    hasSecondary = true;
                    zones.Add(PropertyZone.Create(
                        PropertyZoneKind.SecondaryPad,
                        secOrigin,
                        secondaryPad.WidthMeters,
                        secondaryPad.DepthMeters,
                        yaw));
                }
            }

            var gateCenterX = driveBays > 0
                ? driveBays * ModularGrid.BayMeters * 0.5f
                : parcel.WidthMeters * 0.5f;
            var boundaries = PropertyBoundaryBuilder.Build(
                parcel,
                arch,
                isCorner,
                gateCenterX,
                frontSetbackBays * ModularGrid.BayMeters,
                pad.DepthMeters);

            var property = new PropertySpec
            {
                PlotIndex = index,
                DistrictId = districtId,
                ArchetypeId = arch.Id,
                Parcel = parcel,
                Zones = zones,
                Boundaries = boundaries,
                BuildingPad = pad,
                Street = street,
                HasSecondaryBuilding = hasSecondary,
                SecondaryBuildingPad = secondaryPad
            };

            property.Props = PropertyPropPlacer.Place(
                property,
                arch.Id,
                ForkProps(arch.Id, index, parcel.Origin));
            return property;
        }

        private static Random ForkProps(string archetypeId, int plotIndex, Vector3 parcelOrigin)
        {
            unchecked
            {
                var h = 110201;
                if (!string.IsNullOrEmpty(archetypeId))
                {
                    for (var i = 0; i < archetypeId.Length; i++)
                        h = (h * 397) ^ archetypeId[i];
                }

                h = (h * 397) ^ plotIndex;
                h = (h * 397) ^ parcelOrigin.x.GetHashCode();
                h = (h * 397) ^ parcelOrigin.z.GetHashCode();
                if (h == int.MinValue)
                    h = 1;
                return new Random(h);
            }
        }

        private static StreetContext BuildStreetContext(
            PropertyArchetypeDef arch,
            bool isCornerLeft,
            bool isCornerRight,
            bool forceAlleyBack)
        {
            StreetContext street;
            if (isCornerLeft)
                street = StreetContext.CornerFrontLeft();
            else if (isCornerRight)
                street = StreetContext.CornerFrontRight();
            else if (forceAlleyBack || arch.RearIsAlley)
                street = StreetContext.StreetFrontAlleyBack();
            else
                street = StreetContext.StreetFrontYardBack();

            if (forceAlleyBack || arch.RearIsAlley)
                street = street.WithBack(FaceKind.Alley);

            return street;
        }
    }
}
