using System.Collections.Generic;
using GameDev.World.Buildings.Shell;
using UnityEngine;

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

        public string SettlementId { get; private set; }
        public DistrictKind DistrictKind { get; private set; }
        public int BlockCount { get; private set; }
        public IReadOnlyList<PropertySpec> Properties => _properties;
        public IReadOnlyList<SettlementPlotSpec> Plots => _plots;
        public IReadOnlyList<SettlementStreetSpec> Streets => _streets;
        public IReadOnlyList<SettlementLaneSpec> Lanes => _lanes;

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
            blockCount = Mathf.Clamp(blockCount, 1, 4);
            plotsPerSide = Mathf.Max(1, plotsPerSide);
            var streetW = Mathf.Max(4f, streetWidthMeters);
            var crossW = Mathf.Max(3f, crossStreetWidthMeters);

            var fabric = new SettlementFabricLayout
            {
                SettlementId = $"spine_{districtKind}_{seed}",
                DistrictKind = districtKind,
                BlockCount = blockCount
            };

            var xCursor = crossW;
            var plotIndex = 0;
            var maxNorthFrontWorldZ = streetW;
            var blockSpans = new List<BlockSpan>(blockCount);

            for (var b = 0; b < blockCount; b++)
            {
                var blockSeed = unchecked(seed + (b + 1) * 10007);
                var block = SettlementBlockLayout.CreateDoubleFrontage(
                    blockSeed,
                    districtKind,
                    plotsPerSide,
                    laneWidthMeters,
                    archetype);

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
                if (b < blockCount - 1)
                    xCursor += crossW;
            }

            var eastCrossX = xCursor;
            var totalLength = eastCrossX + crossW;
            var northStreetZ = maxNorthFrontWorldZ;
            var loopDepth = northStreetZ + streetW;

            // Primary (south) street — full spine length.
            fabric._streets.Add(SettlementStreetSpec.Create(
                SettlementStreetKind.Primary,
                new Vector3(0f, 0f, 0f),
                totalLength,
                streetW,
                0f));

            // North street — closes the loop above the north fronts.
            fabric._streets.Add(SettlementStreetSpec.Create(
                SettlementStreetKind.Primary,
                new Vector3(0f, 0f, northStreetZ),
                totalLength,
                streetW,
                0f));

            // West + east + mid crosses — N–S ribbons closing the block loop.
            fabric._streets.Add(MakeCrossStreet(0f, crossW, loopDepth));
            fabric._streets.Add(MakeCrossStreet(eastCrossX, crossW, loopDepth));
            for (var b = 0; b < blockSpans.Count - 1; b++)
            {
                var gapX = blockSpans[b].X + blockSpans[b].Width;
                fabric._streets.Add(MakeCrossStreet(gapX, crossW, loopDepth));
            }

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
