using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Buildings.Shell
{
    /// <summary>
    /// Hard non-overlap validator. Authority: SHELL_NON_OVERLAP_RULE.md
    /// </summary>
    public static class ShellOverlapGuard
    {
        public const float Epsilon = 0.002f;

        public readonly struct Report
        {
            public readonly bool Ok;
            public readonly string Error;

            public Report(bool ok, string error)
            {
                Ok = ok;
                Error = error ?? string.Empty;
            }

            public static Report Success() => new Report(true, string.Empty);
            public static Report Fail(string error) => new Report(false, error);
        }

        public static Report Validate(ShellInstance shell, FloorStackMassing stack)
        {
            if (shell == null)
                return Report.Fail("shell is null");
            if (stack == null)
                return Report.Fail("stack is null");

            for (var f = 0; f < stack.FloorCount; f++)
            {
                var slabs = stack.SlabsByFloor[f];
                for (var i = 0; i < slabs.Count; i++)
                {
                    for (var j = i + 1; j < slabs.Count; j++)
                    {
                        if (SlabsOverlapXZ(slabs[i], slabs[j]))
                            return Report.Fail(
                                $"floor {f} slabs overlap XZ: {slabs[i].Id} ∩ {slabs[j].Id}");
                    }
                }
            }

            // Containment against the ground footprint (largest body) — upper shrinks stay inside it.
            var groundFootprint = stack.SlabsByFloor[0].Count > 0
                ? stack.SlabsByFloor[0][0]
                : default;
            var pieces = shell.Pieces;
            var bounds = new Bounds[pieces.Count];
            for (var i = 0; i < pieces.Count; i++)
            {
                bounds[i] = WorldAabb(pieces[i]);
                var contain = ContainmentError(pieces[i], bounds[i], groundFootprint);
                if (contain != null)
                    return Report.Fail(contain);
            }

            for (var i = 0; i < pieces.Count; i++)
            {
                for (var j = i + 1; j < pieces.Count; j++)
                {
                    if (!AabbOverlap(bounds[i], bounds[j]))
                        continue;
                    if (IsAllowedOverlap(pieces[i], pieces[j], bounds[i], bounds[j]))
                        continue;
                    return Report.Fail(
                        $"illegal overlap: {pieces[i].PieceId} ∩ {pieces[j].PieceId}");
                }
            }

            return Report.Success();
        }

        public static Bounds WorldAabb(ShellPieceInstance piece)
        {
            var half = piece.LocalScale * 0.5f;
            var yaw = Mathf.Abs(Mathf.DeltaAngle(0f, piece.LocalEuler.y));
            Vector3 extents;
            if (yaw > 45f && yaw < 135f)
                extents = new Vector3(half.z, half.y, half.x);
            else
                extents = half;

            return new Bounds(piece.LocalPosition, extents * 2f);
        }

        private static string ContainmentError(
            ShellPieceInstance piece, Bounds pieceBounds, FloorSlab ground)
        {
            if (IsPlotDress(piece.Category))
                return null;
            if (string.IsNullOrEmpty(piece.OwnerVolumeId))
                return null;
            if (ground.WidthMeters <= 0f)
                return $"piece {piece.PieceId}: missing ground footprint";

            var min = pieceBounds.min;
            var max = pieceBounds.max;
            // Openings may sit slightly proud of the outer facade (greybox visibility).
            var pad = IsOpening(piece.Category) ? ModularGrid.WallThickness : 0f;
            if (min.x < ground.LocalX - pad - Epsilon
                || max.x > ground.MaxX + pad + Epsilon
                || min.z < ground.LocalZ - pad - Epsilon
                || max.z > ground.MaxZ + pad + Epsilon)
            {
                return $"piece {piece.PieceId} escapes ground footprint "
                       + $"pieceXZ=[{min.x:F3},{max.x:F3}]x[{min.z:F3},{max.z:F3}] "
                       + $"ground=[{ground.LocalX:F3},{ground.MaxX:F3}]x[{ground.LocalZ:F3},{ground.MaxZ:F3}]";
            }

            return null;
        }

        private static bool IsAllowedOverlap(
            ShellPieceInstance a,
            ShellPieceInstance b,
            Bounds ba,
            Bounds bb)
        {
            if (IsOpeningWallStack(a, b, ba, bb) || IsOpeningWallStack(b, a, bb, ba))
                return true;

            // Entry dress: stoop may kiss the ground door / shopfront plaque.
            if (IsEntryDressOverlap(a, b))
                return true;

            if (SameOwner(a, b))
            {
                if (IsFoundation(a.Category) || IsFoundation(b.Category))
                    return true;
                if (IsRoof(a.Category) && b.Category == ExteriorPieceCategory.Chimney)
                    return true;
                if (IsRoof(b.Category) && a.Category == ExteriorPieceCategory.Chimney)
                    return true;
                if (a.Category == ExteriorPieceCategory.Corner || b.Category == ExteriorPieceCategory.Corner)
                {
                    if (IsWall(a.Category) || IsWall(b.Category))
                        return true;
                }

                if (a.Category == ExteriorPieceCategory.Trim || b.Category == ExteriorPieceCategory.Trim)
                {
                    if (IsWall(a.Category) || IsWall(b.Category) || a.Category == ExteriorPieceCategory.Corner
                        || b.Category == ExteriorPieceCategory.Corner)
                        return true;
                }
            }

            return false;
        }

        private static bool IsEntryDressOverlap(ShellPieceInstance a, ShellPieceInstance b)
        {
            var stoopA = a.Category == ExteriorPieceCategory.Stoop;
            var stoopB = b.Category == ExteriorPieceCategory.Stoop;
            if (!stoopA && !stoopB)
                return false;
            var other = stoopA ? b : a;
            return other.Category == ExteriorPieceCategory.Door
                   || other.Category == ExteriorPieceCategory.Shopfront;
        }

        private static bool IsOpeningWallStack(
            ShellPieceInstance wall,
            ShellPieceInstance opening,
            Bounds wallBounds,
            Bounds openBounds)
        {
            if (!IsWall(wall.Category) || !IsOpening(opening.Category))
                return false;
            if (!SameBay(wall, opening))
                return false;
            // Same bay: opening may sit proud of the wall (visible plaque). Must not spill into neighbor bays.
            var alongPad = 0.02f;
            return openBounds.min.y >= wallBounds.min.y - Epsilon
                   && openBounds.max.y <= wallBounds.max.y + Epsilon
                   && BoundsOverlapOnBayFace(wall, wallBounds, openBounds, alongPad);
        }

        private static bool BoundsOverlapOnBayFace(
            ShellPieceInstance wall, Bounds wallBounds, Bounds openBounds, float alongPad)
        {
            // FaceIndex: Front/Back → constrain X; Left/Right → constrain Z.
            var face = (PlotFace)wall.FaceIndex;
            if (face == PlotFace.Front || face == PlotFace.Back)
            {
                return openBounds.min.x >= wallBounds.min.x - alongPad
                       && openBounds.max.x <= wallBounds.max.x + alongPad;
            }

            return openBounds.min.z >= wallBounds.min.z - alongPad
                   && openBounds.max.z <= wallBounds.max.z + alongPad;
        }

        private static bool SameBay(ShellPieceInstance a, ShellPieceInstance b)
        {
            return a.OwnerVolumeId == b.OwnerVolumeId
                   && a.FaceIndex == b.FaceIndex
                   && a.FloorIndex == b.FloorIndex
                   && a.BayIndex == b.BayIndex
                   && a.FaceIndex >= 0
                   && a.BayIndex >= 0;
        }

        private static bool SameOwner(ShellPieceInstance a, ShellPieceInstance b)
        {
            return !string.IsNullOrEmpty(a.OwnerVolumeId)
                   && a.OwnerVolumeId == b.OwnerVolumeId;
        }

        private static bool IsSubset(Bounds inner, Bounds outer, float eps)
        {
            return inner.min.x >= outer.min.x - eps
                   && inner.min.y >= outer.min.y - eps
                   && inner.min.z >= outer.min.z - eps
                   && inner.max.x <= outer.max.x + eps
                   && inner.max.y <= outer.max.y + eps
                   && inner.max.z <= outer.max.z + eps;
        }

        private static bool AabbOverlap(Bounds a, Bounds b)
        {
            return a.min.x < b.max.x - Epsilon
                   && a.max.x > b.min.x + Epsilon
                   && a.min.y < b.max.y - Epsilon
                   && a.max.y > b.min.y + Epsilon
                   && a.min.z < b.max.z - Epsilon
                   && a.max.z > b.min.z + Epsilon;
        }

        private static bool SlabsOverlapXZ(FloorSlab a, FloorSlab b)
        {
            return a.LocalX < b.MaxX - Epsilon
                   && a.MaxX > b.LocalX + Epsilon
                   && a.LocalZ < b.MaxZ - Epsilon
                   && a.MaxZ > b.LocalZ + Epsilon;
        }

        private static bool IsOpening(ExteriorPieceCategory c)
        {
            return c == ExteriorPieceCategory.Window
                   || c == ExteriorPieceCategory.Door
                   || c == ExteriorPieceCategory.Shopfront
                   || c == ExteriorPieceCategory.AtticWindow;
        }

        private static bool IsWall(ExteriorPieceCategory c)
        {
            return c == ExteriorPieceCategory.Wall || c == ExteriorPieceCategory.Blank;
        }

        private static bool IsRoof(ExteriorPieceCategory c)
        {
            return c == ExteriorPieceCategory.RoofFlat || c == ExteriorPieceCategory.RoofGable;
        }

        private static bool IsFoundation(ExteriorPieceCategory c)
        {
            return c == ExteriorPieceCategory.Foundation;
        }

        private static bool IsPlotDress(ExteriorPieceCategory c)
        {
            return c == ExteriorPieceCategory.Fence || c == ExteriorPieceCategory.Stoop;
        }
    }
}
