using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Buildings.Shell
{
    /// <summary>
    /// Floor-stack exterior assembly: storey 0 → N bottom-up.
    /// Door only on floor 0; step faces above lower roofs use attic windows;
    /// chimneys sit flush on the primary top roof.
    /// Authority: SHELL_NON_OVERLAP_RULE.md
    /// </summary>
    public sealed class ShellGrammarAssembler
    {
        public ShellInstance Assemble(
            BuildingDna dna,
            PlotFootprint plot,
            StreetContext street,
            BuildingArchetypeDef archetype,
            DistrictProfileDef district,
            Random rng)
        {
            var shell = new ShellInstance
            {
                Dna = dna,
                Plot = plot,
                Street = street
            };

            if (dna.LandmarkClass == LandmarkClass.Landmark)
            {
                AddLandmarkMass(shell, plot, dna, district);
                FinalizeBounds(shell, plot, dna);
                return shell;
            }

            var stack = ShellMassingBuilder.BuildFloorStack(plot, dna);

            // One foundation under the ground storey footprint (single slab).
            var ground = stack.SlabsByFloor[0];
            for (var i = 0; i < ground.Count; i++)
                AddFoundation(shell, ground[i], district);

            for (var floor = 0; floor < stack.FloorCount; floor++)
            {
                var slabs = stack.SlabsByFloor[floor];
                for (var i = 0; i < slabs.Count; i++)
                    AssembleStorey(shell, stack, slabs[i], floor, street, dna, archetype, district);

                // Roof vacated strips now; on the top storey roof the remaining body.
                var toRoof = stack.VacatedRegions(floor);
                for (var i = 0; i < toRoof.Count; i++)
                    AddRoof(shell, toRoof[i], floor, dna, district);
            }

            AddChimneysOnPrimaryRoof(shell, stack, dna, district);

            if (dna.FrontGardenDress)
                AddFrontGardenDress(shell, plot, dna, district);

            if (dna.InteriorPolicy == InteriorPolicy.Portal)
            {
                shell.HasPortal = true;
                shell.Portal = new PortalSocket
                {
                    LocalPosition = new Vector3(plot.WidthMeters * 0.5f, ModularGrid.FoundationHeight, 0.05f),
                    YawDegrees = 0f,
                    InteriorKey = $"{dna.ArchetypeId}_{dna.PlotIndex}_{dna.Seed}"
                };
            }

            FinalizeBounds(shell, plot, dna);

            var report = ShellOverlapGuard.Validate(shell, stack);
            if (!report.Ok)
                throw new InvalidOperationException($"Shell non-overlap violated: {report.Error}");

            var openings = CountOpenings(shell);
            if (openings == 0)
            {
                Debug.LogWarning(
                    $"[ShellOpenings] ZERO openings on {shell.Dna.ArchetypeId} "
                    + $"massing={shell.Dna.Massing} floors={shell.Dna.FloorCount} — facade bug");
            }

            return shell;
        }

        private static int CountOpenings(ShellInstance shell)
        {
            var n = 0;
            for (var i = 0; i < shell.Pieces.Count; i++)
            {
                var c = shell.Pieces[i].Category;
                if (c == ExteriorPieceCategory.Window
                    || c == ExteriorPieceCategory.Door
                    || c == ExteriorPieceCategory.Shopfront
                    || c == ExteriorPieceCategory.AtticWindow)
                    n++;
            }

            return n;
        }

        private static void AssembleStorey(
            ShellInstance shell,
            FloorStackMassing stack,
            FloorSlab slab,
            int floor,
            StreetContext street,
            BuildingDna dna,
            BuildingArchetypeDef archetype,
            DistrictProfileDef district)
        {
            var y = ModularGrid.FoundationHeight + floor * ModularGrid.StoreyMeters + ModularGrid.StoreyMeters * 0.5f;
            PlaceFace(shell, stack, slab, floor, PlotFace.Front, y, street, dna, archetype, district);
            PlaceFace(shell, stack, slab, floor, PlotFace.Back, y, street, dna, archetype, district);
            PlaceFace(shell, stack, slab, floor, PlotFace.Left, y, street, dna, archetype, district);
            PlaceFace(shell, stack, slab, floor, PlotFace.Right, y, street, dna, archetype, district);
            // Outer corners only, wall-coloured (no white grid seams between bays).
            PlaceCorners(shell, stack, slab, floor, y, district, dna.Wealth);
        }

        private static void PlaceFace(
            ShellInstance shell,
            FloorStackMassing stack,
            FloorSlab slab,
            int floor,
            PlotFace face,
            float y,
            StreetContext street,
            BuildingDna dna,
            BuildingArchetypeDef archetype,
            DistrictProfileDef district)
        {
            var t = ModularGrid.WallThickness;
            FaceAlongRange(slab, face, stack, floor, t, out var alongStart, out var alongEnd);
            var usable = alongEnd - alongStart;
            if (usable < t * 0.5f)
                return;

            // Exact bay tiling from usable length — openings must stay inside bayStep.
            var bayCount = Mathf.Max(1, Mathf.RoundToInt(usable / ModularGrid.BayMeters));
            if (bayCount < 1) bayCount = 1;
            var bayStep = usable / bayCount;
            var euler = FaceEuler(face);
            var faceIndex = (int)face;
            var preferShop = archetype != null && archetype.PreferShopfront;
            var windowDensity = district != null ? district.WindowDensity : 0.7f;
            var doorBay = -1;
            if (floor == 0 && slab.IsPrimaryStreet && slab.DoorBay >= 0 && face == PlotFace.Front)
                doorBay = Mathf.Clamp(slab.DoorBay, 0, bayCount - 1);
            var kind = ResolveFaceKind(face, slab, street);

            for (var bay = 0; bay < bayCount; bay++)
            {
                var alongCenter = alongStart + (bay + 0.5f) * bayStep;
                var sample = OutsideSample(slab, face, alongCenter);
                if (stack.CoveredOnFloor(sample.x, sample.z, floor))
                    continue; // party against another slab on this storey

                var attic = stack.CoveredBelow(sample.x, sample.z, floor);
                var wallPos = BayCenterInwardAt(slab, face, alongCenter, y, t);
                var wallScale = new Vector3(bayStep, ModularGrid.StoreyMeters, t);

                shell.AddPiece(
                    ExteriorPieceCategory.Wall,
                    $"F{floor}_WALL_{face}_B{bay}",
                    wallPos,
                    euler,
                    wallScale,
                    TintFor(ExteriorPieceCategory.Wall, dna.Wealth, district),
                    slab.Id,
                    faceIndex,
                    bay,
                    floor);

                var opening = ChooseOpening(kind, floor, bay, preferShop, windowDensity, doorBay, attic, face, slab);
                if (!opening.HasValue)
                    continue;

                var openCat = opening.Value;
                var openScale = OpeningScale(openCat, bayStep, t);
                // Thin plaque clearly outside the wall cube (was buried → invisible).
                var openPos = BayCenterOutwardAt(slab, face, alongCenter, wallPos.y, proud: 0.12f);
                if (openCat == ExteriorPieceCategory.Door || openCat == ExteriorPieceCategory.Shopfront)
                {
                    openPos.y = ModularGrid.FoundationHeight
                                + floor * ModularGrid.StoreyMeters
                                + openScale.y * 0.5f;
                }
                else if (openCat == ExteriorPieceCategory.AtticWindow)
                {
                    openPos.y = ModularGrid.FoundationHeight
                                + floor * ModularGrid.StoreyMeters
                                + ModularGrid.StoreyMeters * 0.62f;
                }
                else
                {
                    openPos.y = ModularGrid.FoundationHeight
                                + floor * ModularGrid.StoreyMeters
                                + ModularGrid.StoreyMeters * 0.55f;
                }

                var variant = openCat == ExteriorPieceCategory.Window
                    ? WindowVariantId(bay, floor, dna)
                    : openCat == ExteriorPieceCategory.AtticWindow ? "ATTIC" : "A";

                var openTint = TintFor(openCat, dna.Wealth, district);
                if (openCat == ExteriorPieceCategory.Window)
                    openTint = WindowVariantTint(openTint, variant);

                shell.AddPiece(
                    openCat,
                    $"F{floor}_{openCat}_{variant}_{face}_B{bay}",
                    openPos,
                    euler,
                    openScale,
                    openTint,
                    slab.Id,
                    faceIndex,
                    bay,
                    floor);
            }
        }

        private static void FaceAlongRange(
            FloorSlab slab,
            PlotFace face,
            FloorStackMassing stack,
            int floor,
            float wallThickness,
            out float alongStart,
            out float alongEnd)
        {
            var full = face == PlotFace.Front || face == PlotFace.Back
                ? slab.WidthMeters
                : slab.DepthMeters;
            alongStart = 0f;
            alongEnd = full;

            // Leave corner columns when both meeting faces will emit (not fully party).
            if (face == PlotFace.Front || face == PlotFace.Back)
            {
                if (FaceHasAnyExteriorBay(slab, PlotFace.Left, stack, floor))
                    alongStart += wallThickness;
                if (FaceHasAnyExteriorBay(slab, PlotFace.Right, stack, floor))
                    alongEnd -= wallThickness;
            }
            else
            {
                if (FaceHasAnyExteriorBay(slab, PlotFace.Front, stack, floor))
                    alongStart += wallThickness;
                if (FaceHasAnyExteriorBay(slab, PlotFace.Back, stack, floor))
                    alongEnd -= wallThickness;
            }

            if (alongEnd - alongStart < wallThickness)
            {
                alongStart = 0f;
                alongEnd = full;
            }
        }

        private static bool FaceHasAnyExteriorBay(
            FloorSlab slab, PlotFace face, FloorStackMassing stack, int floor)
        {
            var mid = face == PlotFace.Front || face == PlotFace.Back
                ? slab.WidthMeters * 0.5f
                : slab.DepthMeters * 0.5f;
            var sample = OutsideSample(slab, face, mid);
            return !stack.CoveredOnFloor(sample.x, sample.z, floor);
        }

        private static Vector3 OutsideSample(FloorSlab slab, PlotFace face, float alongCenter)
        {
            const float d = 0.05f;
            switch (face)
            {
                case PlotFace.Front:
                    return new Vector3(slab.LocalX + alongCenter, 0f, slab.LocalZ - d);
                case PlotFace.Back:
                    return new Vector3(slab.LocalX + alongCenter, 0f, slab.MaxZ + d);
                case PlotFace.Left:
                    return new Vector3(slab.LocalX - d, 0f, slab.LocalZ + alongCenter);
                case PlotFace.Right:
                    return new Vector3(slab.MaxX + d, 0f, slab.LocalZ + alongCenter);
                default:
                    return Vector3.zero;
            }
        }

        private static ExteriorPieceCategory? ChooseOpening(
            FaceKind kind,
            int floor,
            int bay,
            bool preferShop,
            float windowDensity,
            int doorBay,
            bool attic,
            PlotFace face,
            FloorSlab slab)
        {
            if (attic)
            {
                // Distinct attic lights on step faces — not standard storey windows.
                if (bay % 2 != 0)
                    return null;
                return ExteriorPieceCategory.AtticWindow;
            }

            // Ground street door / shopfront.
            if (floor == 0 && face == PlotFace.Front && slab.IsPrimaryStreet
                && (kind == FaceKind.Street || kind == FaceKind.Square))
            {
                if (doorBay >= 0 && bay == doorBay)
                    return preferShop ? ExteriorPieceCategory.Shopfront : ExteriorPieceCategory.Door;
                if (preferShop && doorBay >= 0 && Mathf.Abs(bay - doorBay) == 1)
                    return ExteriorPieceCategory.Shopfront;
                return ExteriorPieceCategory.Window;
            }

            // Every exterior bay gets a window unless density culls a minority.
            if (windowDensity < 0.35f && bay % 2 != 0)
                return null;
            if (windowDensity < 0.55f && bay % 3 == 2)
                return null;
            return ExteriorPieceCategory.Window;
        }

        private static FaceKind ResolveFaceKind(PlotFace face, FloorSlab slab, StreetContext street)
        {
            if (face == PlotFace.Front && slab.IsPrimaryStreet && slab.LocalZ <= 0.01f)
                return FaceKind.Street;
            if (face == PlotFace.Front)
                return FaceKind.Yard;
            if (face == PlotFace.Back)
                return FaceKind.Yard;
            return street.Get(face) == FaceKind.Street ? FaceKind.Street : FaceKind.Yard;
        }

        private static Vector3 OpeningScale(ExteriorPieceCategory category, float bayStep, float wallThickness)
        {
            float along;
            float height;
            float depth;
            switch (category)
            {
                case ExteriorPieceCategory.Door:
                    along = bayStep * 0.42f;
                    height = ModularGrid.StoreyMeters * 0.72f;
                    depth = 0.08f;
                    break;
                case ExteriorPieceCategory.Shopfront:
                    along = bayStep * 0.78f;
                    height = ModularGrid.StoreyMeters * 0.78f;
                    depth = 0.08f;
                    break;
                case ExteriorPieceCategory.AtticWindow:
                    along = bayStep * 0.38f;
                    height = ModularGrid.StoreyMeters * 0.28f;
                    depth = 0.07f;
                    break;
                default:
                    along = bayStep * 0.5f;
                    height = ModularGrid.StoreyMeters * 0.45f;
                    depth = 0.07f;
                    break;
            }

            // Stay inside bay along-face; thin plaque on the facade (not buried in wall).
            along = Mathf.Min(along, bayStep * 0.85f);
            height = Mathf.Min(height, ModularGrid.StoreyMeters * 0.85f);
            depth = Mathf.Max(0.05f, depth);
            return new Vector3(along, height, depth);
        }

        private static void PlaceCorners(
            ShellInstance shell,
            FloorStackMassing stack,
            FloorSlab slab,
            int floor,
            float y,
            DistrictProfileDef district,
            int wealth)
        {
            var t = ModularGrid.WallThickness;
            var tint = WallColor(district, wealth); // same as wall — no white seam grid
            TryCorner(shell, stack, slab, floor, 0f, 0f, y, t, tint, PlotFace.Front, PlotFace.Left, "FL");
            TryCorner(shell, stack, slab, floor, slab.WidthMeters, 0f, y, t, tint, PlotFace.Front, PlotFace.Right, "FR");
            TryCorner(shell, stack, slab, floor, 0f, slab.DepthMeters, y, t, tint, PlotFace.Back, PlotFace.Left, "BL");
            TryCorner(shell, stack, slab, floor, slab.WidthMeters, slab.DepthMeters, y, t, tint, PlotFace.Back, PlotFace.Right, "BR");
        }

        private static void TryCorner(
            ShellInstance shell,
            FloorStackMassing stack,
            FloorSlab slab,
            int floor,
            float localX,
            float localZ,
            float y,
            float t,
            Color tint,
            PlotFace a,
            PlotFace b,
            string id)
        {
            if (!FaceHasAnyExteriorBay(slab, a, stack, floor))
                return;
            if (!FaceHasAnyExteriorBay(slab, b, stack, floor))
                return;

            // Corner column fully inside the slab (fills the gap left by shortened walls).
            var cx = localX <= 0.01f ? t * 0.5f : slab.WidthMeters - t * 0.5f;
            var cz = localZ <= 0.01f ? t * 0.5f : slab.DepthMeters - t * 0.5f;
            shell.AddPiece(
                ExteriorPieceCategory.Corner,
                $"F{floor}_CORNER_{id}",
                new Vector3(slab.LocalX + cx, y, slab.LocalZ + cz),
                Vector3.zero,
                new Vector3(t, ModularGrid.StoreyMeters, t),
                tint,
                slab.Id,
                floorIndex: floor);
        }

        private static void AddFoundation(ShellInstance shell, FloorSlab slab, DistrictProfileDef district)
        {
            var wall = WallColor(district, 2);
            var tint = new Color(wall.r * 0.7f, wall.g * 0.7f, wall.b * 0.7f);
            var inset = ModularGrid.WallThickness * 0.5f;
            var w = Mathf.Max(0.2f, slab.WidthMeters - inset);
            var d = Mathf.Max(0.2f, slab.DepthMeters - inset);
            shell.AddPiece(
                ExteriorPieceCategory.Foundation,
                $"FOUNDATION_{slab.Id}",
                new Vector3(
                    slab.LocalX + slab.WidthMeters * 0.5f,
                    ModularGrid.FoundationHeight * 0.5f,
                    slab.LocalZ + slab.DepthMeters * 0.5f),
                Vector3.zero,
                new Vector3(w, ModularGrid.FoundationHeight, d),
                tint,
                slab.Id);
        }

        private static void AddRoof(
            ShellInstance shell,
            FloorSlab slab,
            int floor,
            BuildingDna dna,
            DistrictProfileDef district)
        {
            var yBase = ModularGrid.FoundationHeight + (floor + 1) * ModularGrid.StoreyMeters;
            var roof = RoofColor(district);
            var edge = ModularGrid.WallThickness;
            var w = Mathf.Max(0.2f, slab.WidthMeters - edge * 2f);
            var d = Mathf.Max(0.2f, slab.DepthMeters - edge * 2f);
            var cx = slab.LocalX + slab.WidthMeters * 0.5f;
            var cz = slab.LocalZ + slab.DepthMeters * 0.5f;

            // Roofs always owned by "body" for containment (vacated strips are still on the plot body).
            const string owner = "body";
            if (dna.RoofType == RoofType.Flat)
            {
                shell.AddPiece(
                    ExteriorPieceCategory.RoofFlat,
                    $"F{floor}_ROOF_FLAT_{slab.Id}",
                    new Vector3(cx, yBase + 0.12f, cz),
                    Vector3.zero,
                    new Vector3(w, 0.24f, d),
                    roof,
                    owner,
                    floorIndex: floor);
                return;
            }

            var peak = GablePeakHeight(w);
            shell.AddPiece(
                ExteriorPieceCategory.RoofGable,
                $"F{floor}_ROOF_GABLE_{slab.Id}",
                new Vector3(cx, yBase + peak * 0.5f, cz),
                Vector3.zero,
                new Vector3(w, peak, d),
                roof,
                owner,
                floorIndex: floor);
        }

        private static float GablePeakHeight(float roofWidthMeters)
        {
            return roofWidthMeters <= ModularGrid.BayMeters * 2.2f ? 1.5f : 1.25f;
        }

        private static void AddChimneysOnPrimaryRoof(
            ShellInstance shell,
            FloorStackMassing stack,
            BuildingDna dna,
            DistrictProfileDef district)
        {
            if (dna.ChimneyCount <= 0)
                return;

            var primary = stack.FindPrimaryTop();
            if (!primary.HasValue)
                return;

            var slab = primary.Value;
            var topFloor = stack.FloorCount - 1;
            // Find the storey index where this primary slab last appears (has its roof).
            for (var f = stack.FloorCount - 1; f >= 0; f--)
            {
                var slabs = stack.SlabsByFloor[f];
                for (var i = 0; i < slabs.Count; i++)
                {
                    if (slabs[i].Id == slab.Id)
                    {
                        topFloor = f;
                        slab = slabs[i];
                        f = -1;
                        break;
                    }
                }
            }

            var yBase = ModularGrid.FoundationHeight + (topFloor + 1) * ModularGrid.StoreyMeters;
            // Must match AddRoof inset width — peak height is keyed off roof piece width, not raw slab.
            var edge = ModularGrid.WallThickness;
            var roofW = Mathf.Max(0.2f, slab.WidthMeters - edge * 2f);
            var roofTop = dna.RoofType == RoofType.Flat
                ? yBase + 0.24f
                : yBase + GablePeakHeight(roofW);

            var chimneyH = 1.1f;
            var chimneyY = roofTop + chimneyH * 0.5f; // flush: bottom sits on roof top
            var tint = WallColor(district, dna.Wealth) * 0.85f;
            tint.a = 1f;
            var margin = ModularGrid.WallThickness + 0.3f;
            var usableW = Mathf.Max(0.2f, slab.WidthMeters - margin * 2f);
            var z = Mathf.Clamp(
                slab.LocalZ + slab.DepthMeters * 0.55f,
                slab.LocalZ + margin,
                slab.MaxZ - margin);

            for (var i = 0; i < dna.ChimneyCount; i++)
            {
                var u = dna.ChimneyCount == 1 ? 0.5f : (0.3f + 0.4f * i / Math.Max(1, dna.ChimneyCount - 1));
                var x = slab.LocalX + margin + usableW * u;
                shell.AddPiece(
                    ExteriorPieceCategory.Chimney,
                    $"CHIMNEY_{i}",
                    new Vector3(x, chimneyY, z),
                    Vector3.zero,
                    new Vector3(0.35f, chimneyH, 0.35f),
                    tint,
                    slab.Id,
                    floorIndex: topFloor);
            }
        }

        private static void AddStringCourse(
            ShellInstance shell,
            FloorSlab slab,
            int floor,
            DistrictProfileDef district,
            int wealth)
        {
            var t = ModularGrid.WallThickness;
            var y = ModularGrid.FoundationHeight + floor * ModularGrid.StoreyMeters;
            shell.AddPiece(
                ExteriorPieceCategory.Trim,
                $"STRING_{slab.Id}_{floor}",
                new Vector3(slab.LocalX + slab.WidthMeters * 0.5f, y, slab.LocalZ + t * 0.5f),
                Vector3.zero,
                new Vector3(Mathf.Max(0.2f, slab.WidthMeters - 2f * t), 0.08f, t * 0.85f),
                TrimColor(district, wealth),
                slab.Id,
                floorIndex: floor);
        }

        private static void AddFrontGardenDress(
            ShellInstance shell, PlotFootprint plot, BuildingDna dna, DistrictProfileDef district)
        {
            var trim = TrimColor(district, dna.Wealth);
            shell.AddPiece(
                ExteriorPieceCategory.Fence,
                "FENCE_FRONT",
                new Vector3(plot.WidthMeters * 0.5f, 0.45f, -1.1f),
                Vector3.zero,
                new Vector3(plot.WidthMeters * 0.95f, 0.9f, 0.12f),
                trim);

            // Sit fully in front of the door plaque (proud ≈ 0.12, depth ≈ 0.08) — abut, don't swallow it.
            shell.AddPiece(
                ExteriorPieceCategory.Stoop,
                "STOOP",
                new Vector3(plot.WidthMeters * 0.5f, 0.2f, -0.55f),
                Vector3.zero,
                new Vector3(ModularGrid.BayMeters * 0.7f, 0.4f, 0.55f),
                new Color(trim.r * 0.85f, trim.g * 0.85f, trim.b * 0.85f));
        }

        private static void AddLandmarkMass(
            ShellInstance shell, PlotFootprint plot, BuildingDna dna, DistrictProfileDef district)
        {
            var h = dna.FloorCount * ModularGrid.StoreyMeters + ModularGrid.FoundationHeight;
            shell.AddPiece(
                ExteriorPieceCategory.Wall,
                "LANDMARK_MASS",
                new Vector3(plot.WidthMeters * 0.5f, h * 0.5f, plot.DepthMeters * 0.5f),
                Vector3.zero,
                new Vector3(plot.WidthMeters, h, plot.DepthMeters),
                WallColor(district, dna.Wealth),
                "landmark");
        }

        private static string WindowVariantId(int bay, int floor, BuildingDna dna)
        {
            var h = unchecked(dna.Seed * 397 ^ dna.PlotIndex * 31 ^ bay * 17 ^ floor * 13);
            var v = Mathf.Abs(h) % 3;
            if (v == 0) return "A";
            if (v == 1) return "B";
            return "C";
        }

        private static Color WindowVariantTint(Color baseTint, string variant)
        {
            if (variant == "B")
                return new Color(
                    Mathf.Clamp01(baseTint.r * 0.9f),
                    Mathf.Clamp01(baseTint.g * 1.05f),
                    Mathf.Clamp01(baseTint.b * 1.1f));
            if (variant == "C")
                return new Color(
                    Mathf.Clamp01(baseTint.r * 1.05f),
                    Mathf.Clamp01(baseTint.g * 0.95f),
                    Mathf.Clamp01(baseTint.b * 0.9f));
            return baseTint;
        }

        private static Vector3 FaceInwardLocal(PlotFace face)
        {
            switch (face)
            {
                case PlotFace.Front: return new Vector3(0f, 0f, 1f);
                case PlotFace.Back: return new Vector3(0f, 0f, -1f);
                case PlotFace.Left: return new Vector3(1f, 0f, 0f);
                case PlotFace.Right: return new Vector3(-1f, 0f, 0f);
                default: return Vector3.zero;
            }
        }

        private static Vector3 BayCenterInwardAt(
            FloorSlab slab, PlotFace face, float alongCenter, float y, float wallThickness)
        {
            var halfT = wallThickness * 0.5f;
            switch (face)
            {
                case PlotFace.Front:
                    return new Vector3(slab.LocalX + alongCenter, y, slab.LocalZ + halfT);
                case PlotFace.Back:
                    return new Vector3(slab.LocalX + alongCenter, y, slab.MaxZ - halfT);
                case PlotFace.Left:
                    return new Vector3(slab.LocalX + halfT, y, slab.LocalZ + alongCenter);
                case PlotFace.Right:
                    return new Vector3(slab.MaxX - halfT, y, slab.LocalZ + alongCenter);
                default:
                    return Vector3.zero;
            }
        }

        /// <summary>
        /// Opening centre on the outer face, slightly proud so it reads in front of the wall cube.
        /// </summary>
        private static Vector3 BayCenterOutwardAt(
            FloorSlab slab, PlotFace face, float alongCenter, float y, float proud)
        {
            switch (face)
            {
                case PlotFace.Front:
                    return new Vector3(slab.LocalX + alongCenter, y, slab.LocalZ - proud);
                case PlotFace.Back:
                    return new Vector3(slab.LocalX + alongCenter, y, slab.MaxZ + proud);
                case PlotFace.Left:
                    return new Vector3(slab.LocalX - proud, y, slab.LocalZ + alongCenter);
                case PlotFace.Right:
                    return new Vector3(slab.MaxX + proud, y, slab.LocalZ + alongCenter);
                default:
                    return Vector3.zero;
            }
        }

        private static Vector3 FaceEuler(PlotFace face)
        {
            switch (face)
            {
                case PlotFace.Front: return Vector3.zero;
                case PlotFace.Back: return new Vector3(0f, 180f, 0f);
                case PlotFace.Left: return new Vector3(0f, 270f, 0f);
                case PlotFace.Right: return new Vector3(0f, 90f, 0f);
                default: return Vector3.zero;
            }
        }

        private static Color TintFor(ExteriorPieceCategory category, int wealth, DistrictProfileDef district)
        {
            switch (category)
            {
                case ExteriorPieceCategory.Door:
                    return district != null ? district.DoorTint : new Color(0.35f, 0.22f, 0.15f);
                case ExteriorPieceCategory.Shopfront:
                    return district != null ? district.ShopTint : new Color(0.2f, 0.35f, 0.45f);
                case ExteriorPieceCategory.Window:
                    return district != null ? district.WindowTint : new Color(0.5f, 0.62f, 0.75f);
                case ExteriorPieceCategory.AtticWindow:
                    var w = district != null ? district.WindowTint : new Color(0.5f, 0.62f, 0.75f);
                    return new Color(
                        Mathf.Clamp01(w.r * 0.85f),
                        Mathf.Clamp01(w.g * 0.9f),
                        Mathf.Clamp01(w.b * 1.05f));
                case ExteriorPieceCategory.Blank:
                case ExteriorPieceCategory.Wall:
                case ExteriorPieceCategory.Corner:
                    return WallColor(district, wealth);
                default:
                    return WallColor(district, wealth);
            }
        }

        private static Color WallColor(DistrictProfileDef district, int wealth)
        {
            if (district == null)
                return wealth >= 3
                    ? new Color(0.65f, 0.58f, 0.5f)
                    : new Color(0.58f, 0.55f, 0.5f);
            var c = district.WallTint;
            var lift = (wealth - district.WealthBias) * 0.03f;
            return new Color(
                Mathf.Clamp01(c.r + lift),
                Mathf.Clamp01(c.g + lift),
                Mathf.Clamp01(c.b + lift));
        }

        private static Color RoofColor(DistrictProfileDef district)
        {
            return district != null ? district.RoofTint : new Color(0.28f, 0.22f, 0.2f);
        }

        private static Color TrimColor(DistrictProfileDef district, int wealth)
        {
            if (district == null)
                return wealth >= 4
                    ? new Color(0.75f, 0.7f, 0.55f)
                    : new Color(0.6f, 0.58f, 0.5f);
            return district.TrimTint;
        }

        private static void FinalizeBounds(ShellInstance shell, PlotFootprint plot, BuildingDna dna)
        {
            var roofExtra = dna.RoofType == RoofType.Gable ? 1.8f : 0.5f;
            var h = ModularGrid.FoundationHeight + dna.FloorCount * ModularGrid.StoreyMeters + roofExtra
                    + (dna.ChimneyCount > 0 ? 1.2f : 0f);
            shell.LocalBounds = new Bounds(
                new Vector3(plot.WidthMeters * 0.5f, h * 0.5f, plot.DepthMeters * 0.5f),
                new Vector3(plot.WidthMeters, h, plot.DepthMeters));
        }
    }
}
