using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Buildings.Shell
{
    /// <summary>
    /// True floor-stack: <b>one rectangle per storey</b>.
    /// Lower storeys use the full plot; upper storeys shrink; vacated strips get roofs.
    /// Never two side-by-side full-height annexes.
    /// Authority: SHELL_NON_OVERLAP_RULE.md
    /// </summary>
    public static class ShellMassingBuilder
    {
        public static FloorStackMassing BuildFloorStack(PlotFootprint plot, BuildingDna dna)
        {
            switch (dna.Massing)
            {
                case MassingType.LShape:
                    return BuildWidthStep(plot, dna, tallOnLeft: true, split: 0.62f);
                case MassingType.Stepped:
                    return BuildWidthStep(
                        plot,
                        dna,
                        tallOnLeft: dna.PlotIndex % 2 == 0,
                        split: dna.PlotIndex % 2 == 0 ? 0.48f : 0.52f);
                case MassingType.RearWing:
                    return BuildDepthStep(plot, dna);
                default:
                    return BuildBox(plot, dna);
            }
        }

        private static FloorStackMassing BuildBox(PlotFootprint plot, BuildingDna dna)
        {
            var floors = Mathf.Max(1, dna.FloorCount);
            var stack = new FloorStackMassing(floors) { PrimarySlabId = "body" };
            var doorBay = Mathf.Clamp(plot.WidthBays / 2, 0, Mathf.Max(0, plot.WidthBays - 1));
            for (var f = 0; f < floors; f++)
            {
                stack.SlabsByFloor[f].Add(MakeBody(
                    0f, 0f, plot.WidthMeters, plot.DepthMeters, doorBay: f == 0 ? doorBay : -1));
            }

            return stack;
        }

        /// <summary>
        /// Floors 0..low-1: full plot. Floors low..tall-1: left or right portion only.
        /// </summary>
        private static FloorStackMassing BuildWidthStep(
            PlotFootprint plot, BuildingDna dna, bool tallOnLeft, float split)
        {
            var tall = Mathf.Max(2, dna.FloorCount);
            var low = Mathf.Clamp(
                dna.SecondaryFloors > 0 ? dna.SecondaryFloors : tall - 1,
                1,
                tall - 1);

            var tallW = Mathf.Max(ModularGrid.BayMeters * 2f, plot.WidthMeters * split);
            var shortW = plot.WidthMeters - tallW;
            if (shortW < ModularGrid.BayMeters)
                return BuildBox(plot, dna);

            var tallX = tallOnLeft ? 0f : shortW;
            var stack = new FloorStackMassing(tall) { PrimarySlabId = "body" };
            var doorBay = Mathf.Clamp(plot.WidthBays / 2, 0, Mathf.Max(0, plot.WidthBays - 1));

            for (var f = 0; f < tall; f++)
            {
                if (f < low)
                {
                    stack.SlabsByFloor[f].Add(MakeBody(
                        0f, 0f, plot.WidthMeters, plot.DepthMeters, doorBay: f == 0 ? doorBay : -1));
                }
                else
                {
                    // Upper storeys: only the continuing tall strip (one slab).
                    var upperBays = Mathf.Max(1, Mathf.CeilToInt(tallW / ModularGrid.BayMeters - 0.001f));
                    stack.SlabsByFloor[f].Add(MakeBody(
                        tallX, 0f, tallW, plot.DepthMeters, doorBay: -1));
                    _ = upperBays;
                }
            }

            return stack;
        }

        /// <summary>
        /// Floors 0..low-1: full plot. Floors low..tall-1: front portion only; rear strip roofs off.
        /// </summary>
        private static FloorStackMassing BuildDepthStep(PlotFootprint plot, BuildingDna dna)
        {
            var tall = Mathf.Max(2, dna.FloorCount);
            var low = Mathf.Clamp(
                dna.SecondaryFloors > 0 ? dna.SecondaryFloors : tall - 1,
                1,
                tall - 1);

            var frontD = Mathf.Max(ModularGrid.BayMeters * 2f, plot.DepthMeters * 0.58f);
            var rearD = plot.DepthMeters - frontD;
            if (rearD < ModularGrid.BayMeters)
                return BuildBox(plot, dna);

            var stack = new FloorStackMassing(tall) { PrimarySlabId = "body" };
            var doorBay = Mathf.Clamp(plot.WidthBays / 2, 0, Mathf.Max(0, plot.WidthBays - 1));

            for (var f = 0; f < tall; f++)
            {
                if (f < low)
                {
                    stack.SlabsByFloor[f].Add(MakeBody(
                        0f, 0f, plot.WidthMeters, plot.DepthMeters, doorBay: f == 0 ? doorBay : -1));
                }
                else
                {
                    stack.SlabsByFloor[f].Add(MakeBody(
                        0f, 0f, plot.WidthMeters, frontD, doorBay: -1));
                }
            }

            return stack;
        }

        private static FloorSlab MakeBody(
            float x, float z, float w, float d, int doorBay)
        {
            return new FloorSlab
            {
                Id = "body",
                LocalX = x,
                LocalZ = z,
                WidthMeters = w,
                DepthMeters = d,
                IsPrimaryStreet = true,
                DoorBay = doorBay
            };
        }
    }
}
