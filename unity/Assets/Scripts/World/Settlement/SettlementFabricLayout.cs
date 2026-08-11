using System.Collections.Generic;
using GameDev.World.Buildings.Shell;
using GameDev.World.Terrain;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// Settlement Street Spine V0: primary + cross street ribbons and one or more
    /// double-frontage blocks along the spine. Not a TownRoadGraph revival.
    /// </summary>
    public sealed class SettlementFabricLayout : ISettlementPropertyLayout
    {
        public const float DefaultStreetWidthMeters = 8f;
        public const float DefaultCrossStreetWidthMeters = 6f;

        private readonly List<PropertySpec> _properties = new List<PropertySpec>();
        private readonly List<SettlementPlotSpec> _plots = new List<SettlementPlotSpec>();
        private readonly List<SettlementStreetSpec> _streets = new List<SettlementStreetSpec>();
        private readonly List<SettlementLaneSpec> _lanes = new List<SettlementLaneSpec>();
        private readonly List<StreetPropSpec> _streetProps = new List<StreetPropSpec>();

        public string SettlementId { get; private set; }
        public DistrictKind DistrictKind { get; private set; }
        public int BlockCount { get; private set; }
        public int LayoutSeed { get; private set; }
        public SettlementPose Pose { get; private set; }
        public bool HasWorldPose { get; private set; }
        public IReadOnlyList<PropertySpec> Properties => _properties;
        public IReadOnlyList<SettlementPlotSpec> Plots => _plots;
        public IReadOnlyList<SettlementStreetSpec> Streets => _streets;
        public IReadOnlyList<SettlementLaneSpec> Lanes => _lanes;
        public IReadOnlyList<StreetPropSpec> StreetProps => _streetProps;

        /// <summary>Rotate/translate this local fabric onto a world <see cref="SettlementPose"/> (S6).</summary>
        public SettlementFabricLayout AtPose(SettlementPose pose)
        {
            var placed = new SettlementFabricLayout
            {
                SettlementId = SettlementId,
                DistrictKind = DistrictKind,
                BlockCount = BlockCount,
                LayoutSeed = LayoutSeed,
                Pose = pose,
                HasWorldPose = true
            };

            for (var i = 0; i < _properties.Count; i++)
            {
                var prop = SettlementPropertyTransform.ApplyPose(_properties[i], pose, _properties[i].PlotIndex);
                placed._properties.Add(prop);
                if (!prop.IsOpenSpace)
                    placed._plots.Add(prop.ToPlotSpec());
            }

            for (var i = 0; i < _streets.Count; i++)
                placed._streets.Add(SettlementPropertyTransform.ApplyPoseStreet(_streets[i], pose));

            for (var i = 0; i < _lanes.Count; i++)
                placed._lanes.Add(SettlementPropertyTransform.ApplyPoseLane(_lanes[i], pose));

            // Rebuild from posed streets so curb math stays on world ribbons.
            placed.RebuildStreetProps(LayoutSeed);
            return placed;
        }

        /// <summary>Re-place S8 street props from current street ribbons (after pose / terrain snap).</summary>
        public void RebuildStreetProps(int seed)
        {
            _streetProps.Clear();
            var rng = new Random(unchecked(seed * 9176 ^ 110201));
            _streetProps.AddRange(StreetPropPlacer.Place(_streets, rng));
        }

        /// <summary>Lift street/lane ribbons to terrain after properties are posed (S6 conform).</summary>
        public void ConformStreetsAndLanes(IWorldHeightSampler height)
        {
            if (height == null)
                return;

            for (var i = 0; i < _streets.Count; i++)
            {
                var street = _streets[i];
                if (!SnapRibbon(
                        street.Origin,
                        street.LengthMeters,
                        street.WidthMeters,
                        street.YawDegrees,
                        height,
                        out var origin))
                    continue;
                street.Origin = origin;
                _streets[i] = street;
            }

            for (var i = 0; i < _lanes.Count; i++)
            {
                var lane = _lanes[i];
                if (!SnapRibbon(
                        lane.Origin,
                        lane.LengthMeters,
                        lane.WidthMeters,
                        lane.YawDegrees,
                        height,
                        out var origin))
                    continue;
                lane.Origin = origin;
                _lanes[i] = lane;
            }
        }

        private static bool SnapRibbon(
            Vector3 origin,
            float lengthMeters,
            float widthMeters,
            float yawDegrees,
            IWorldHeightSampler height,
            out Vector3 snappedOrigin)
        {
            snappedOrigin = origin;
            var yaw = Quaternion.Euler(0f, yawDegrees, 0f);
            var corners = new[]
            {
                origin,
                origin + yaw * new Vector3(lengthMeters, 0f, 0f),
                origin + yaw * new Vector3(0f, 0f, widthMeters),
                origin + yaw * new Vector3(lengthMeters, 0f, widthMeters)
            };

            var maxY = float.NegativeInfinity;
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
                return false;

            snappedOrigin = new Vector3(origin.x, maxY, origin.z);
            return true;
        }

        /// <summary>
        /// E–W primary street with a west/east cross loop, optional mid crosses, and
        /// <paramref name="blockCount"/> double-frontage blocks north of the primary.
        /// </summary>
        public static SettlementFabricLayout CreateStreetSpine(
            int seed,
            DistrictKind districtKind,
            int blockCount = 2,
            int plotsPerSide = 4,
            float streetWidthMeters = DefaultStreetWidthMeters,
            float crossStreetWidthMeters = DefaultCrossStreetWidthMeters,
            float laneWidthMeters = SettlementBlockLayout.DefaultLaneWidthMeters,
            PropertyArchetypeDef archetype = null)
        {
            return CreateStreetSpineInternal(
                seed,
                districtKind,
                blockCount,
                plotsPerSide,
                streetWidthMeters,
                crossStreetWidthMeters,
                laneWidthMeters,
                archetype,
                blockDistricts: null,
                plazaWidthMeters: 0f);
        }

        /// <summary>
        /// S7: Commercial block + GardenSquare plaza + Worker block along one street spine.
        /// </summary>
        public static SettlementFabricLayout CreateDistrictMix(
            int seed,
            int plotsPerSide = 3,
            float streetWidthMeters = DefaultStreetWidthMeters,
            float crossStreetWidthMeters = DefaultCrossStreetWidthMeters,
            float laneWidthMeters = SettlementBlockLayout.DefaultLaneWidthMeters,
            float plazaWidthMeters = GardenSquareFactory.DefaultWidthMeters)
        {
            return CreateStreetSpineInternal(
                seed,
                DistrictKind.Worker,
                blockCount: 2,
                plotsPerSide,
                streetWidthMeters,
                crossStreetWidthMeters,
                laneWidthMeters,
                archetype: null,
                blockDistricts: new[] { DistrictKind.Commercial, DistrictKind.Worker },
                plazaWidthMeters: Mathf.Max(12f, plazaWidthMeters));
        }

        private static SettlementFabricLayout CreateStreetSpineInternal(
            int seed,
            DistrictKind districtKind,
            int blockCount,
            int plotsPerSide,
            float streetWidthMeters,
            float crossStreetWidthMeters,
            float laneWidthMeters,
            PropertyArchetypeDef archetype,
            DistrictKind[] blockDistricts,
            float plazaWidthMeters)
        {
            blockCount = Mathf.Clamp(blockCount, 1, 4);
            if (blockDistricts != null && blockDistricts.Length > 0)
                blockCount = Mathf.Clamp(blockDistricts.Length, 1, 4);
            plotsPerSide = Mathf.Max(1, plotsPerSide);
            var streetW = Mathf.Max(4f, streetWidthMeters);
            var crossW = Mathf.Max(3f, crossStreetWidthMeters);
            var usePlaza = plazaWidthMeters >= 12f && blockCount >= 2;

            var mixId = blockDistricts != null ? "mix" : districtKind.ToString();
            var fabric = new SettlementFabricLayout
            {
                SettlementId = usePlaza ? $"mix_{seed}" : $"spine_{mixId}_{seed}",
                DistrictKind = districtKind,
                BlockCount = blockCount,
                LayoutSeed = seed
            };

            var xCursor = crossW;
            var plotIndex = 0;
            var maxNorthFrontWorldZ = streetW;
            var blockSpans = new List<BlockSpan>(blockCount);

            for (var b = 0; b < blockCount; b++)
            {
                var blockDistrict = blockDistricts != null && b < blockDistricts.Length
                    ? blockDistricts[b]
                    : districtKind;
                var blockSeed = unchecked(seed + (b + 1) * 10007);
                var blockArch = archetype != null && blockDistricts == null
                    ? archetype
                    : null;
                var block = SettlementBlockLayout.CreateDoubleFrontage(
                    blockSeed,
                    blockDistrict,
                    plotsPerSide,
                    laneWidthMeters,
                    blockArch);

                SettlementPropertyTransform.MeasureFrontageAndDepth(
                    block,
                    out var frontage,
                    out var southFrontLocalZ,
                    out var northFrontLocalZ);

                // Sit south fronts on the north edge of the primary street.
                var delta = new Vector3(xCursor - MinParcelOriginX(block), 0f, streetW - southFrontLocalZ);

                for (var i = 0; i < block.Properties.Count; i++)
                {
                    var prop = SettlementPropertyTransform.Translate(
                        block.Properties[i],
                        delta,
                        plotIndex++);
                    fabric._properties.Add(prop);
                    if (!prop.IsOpenSpace)
                        fabric._plots.Add(prop.ToPlotSpec());
                }

                if (block.HasRearLane)
                {
                    fabric._lanes.Add(
                        SettlementPropertyTransform.TranslateLane(block.Lane, delta));
                }

                var northFrontWorldZ = northFrontLocalZ + delta.z;
                maxNorthFrontWorldZ = Mathf.Max(maxNorthFrontWorldZ, northFrontWorldZ);

                blockSpans.Add(new BlockSpan
                {
                    X = xCursor,
                    Width = frontage,
                    NorthFrontZ = northFrontWorldZ
                });

                xCursor += frontage;
                if (b >= blockCount - 1)
                    continue;

                if (usePlaza)
                {
                    var plazaDepth = Mathf.Max(8f, northFrontWorldZ - streetW);
                    var plaza = GardenSquareFactory.Create(
                        plotIndex++,
                        new Vector3(xCursor, 0f, streetW),
                        plazaWidthMeters,
                        plazaDepth,
                        0f);
                    fabric._properties.Add(plaza);
                    xCursor += plazaWidthMeters;
                }
                else
                {
                    xCursor += crossW;
                }
            }

            var eastCrossX = xCursor;
            var totalLength = eastCrossX + crossW;
            var northStreetZ = maxNorthFrontWorldZ;
            var loopDepth = northStreetZ + streetW;

            fabric._streets.Add(SettlementStreetSpec.Create(
                SettlementStreetKind.Primary,
                new Vector3(0f, 0f, 0f),
                totalLength,
                streetW,
                0f));

            fabric._streets.Add(SettlementStreetSpec.Create(
                SettlementStreetKind.Primary,
                new Vector3(0f, 0f, northStreetZ),
                totalLength,
                streetW,
                0f));

            fabric._streets.Add(MakeCrossStreet(0f, crossW, loopDepth));
            fabric._streets.Add(MakeCrossStreet(eastCrossX, crossW, loopDepth));

            // Mid crosses only when gaps are streets (not GardenSquare plazas).
            if (!usePlaza)
            {
                for (var b = 0; b < blockSpans.Count - 1; b++)
                {
                    var gapX = blockSpans[b].X + blockSpans[b].Width;
                    fabric._streets.Add(MakeCrossStreet(gapX, crossW, loopDepth));
                }
            }

            fabric.RebuildStreetProps(seed);
            return fabric;
        }

        private static SettlementStreetSpec MakeCrossStreet(float originX, float crossWidth, float loopDepth)
        {
            // Unity yaw −90: local +X → world +Z (north), local +Z → world +X (east).
            // Origin at SW corner → ribbon fills [originX, originX+crossWidth] × [0, loopDepth].
            // (yaw +90 would send length south into empty space — do not use.)
            return SettlementStreetSpec.Create(
                SettlementStreetKind.Cross,
                new Vector3(originX, 0f, 0f),
                loopDepth,
                crossWidth,
                -90f);
        }

        private static float MinParcelOriginX(SettlementBlockLayout block)
        {
            var min = 0f;
            if (block.Properties.Count == 0)
                return min;
            min = block.Properties[0].Parcel.Origin.x;
            for (var i = 1; i < block.Properties.Count; i++)
                min = Mathf.Min(min, block.Properties[i].Parcel.Origin.x);
            return min;
        }

        private struct BlockSpan
        {
            public float X;
            public float Width;
            public float NorthFrontZ;
        }
    }
}
