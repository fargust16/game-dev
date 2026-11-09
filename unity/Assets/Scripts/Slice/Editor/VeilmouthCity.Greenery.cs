using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The city's greenery and the roadside (VEILMOUTH_CITY_PLAN.md §25, owner 2026-11-09, §26, the owner's fifth play
    /// test, and §27): street trees in square pits on the carriageway by the kerb, in the lamps' line, on both kerbs of a
    /// wide street and one of a narrow one, Veilmouth's own thin trees made in Blender (VeilmouthTrees), the Stylized
    /// Trees Pack's bushes, clipped hedges round beds and a parterre,
    /// grass on the gardens' earth, and Roadside Tales' walls, fences and posts by the roads and farms outside the walls.
    /// Every number is an authoring default unless marked.
    /// </summary>
    public static partial class VeilmouthCity
    {
        // §26, the owner's choice: the pavements back as they were before §25.2 widened them, m, since the trees and
        // lamps now stand on the carriageway.
        private const float HighStreetWalk = 4f, ParadeWalk = 3f, ByelawWalk = 3f;

        // §26: a lamp's post 0.3 m out from the kerb onto the carriageway; a tree's pit flush on the kerb.
        private const float LampFromKerb = 0.3f, TreeFromKerb = VeilmouthPavements.PitSize / 2f;

        // §25.3: a street tree about every 11 m, at least 4 m from a lamp, never within 2.5 m along of a door that
        // stands up to 12 m across, nor within 6 m of an alley's or a lane's mouth.
        private const float TreeSpacing = 11f, TreeFromLamp = 4f, TreeDoorAlong = 2.5f, TreeDoorAcross = 12f, LaneMouth = 6f;

        // §25.4: one grass clump in each 2 × 2 m of a garden's earth.
        private const int LawnStep = 2;

        // §25.5: farm yards 10 m deep with a 4 m way in, a vegetable bed of 6 × 4 m beside each, dry-stone walls 6 m
        // off the roads' middles with a 3 m gap every 33 m, and a post on a verge every 40 m.
        private const int YardDepth = 10, WallGapEvery = 33, PostEvery = 40;

        // §27 (owner, 2026-11-09): Veilmouth's own thin trees from Blender in the fruit tree's place: one kind along a
        // street, its two variants taking turns down the row; the garden trees and both street kinds in the gardens.
        private static readonly string[][] StreetKinds =
        {
            new[] { "CityTree_StreetHornbeamA", "CityTree_StreetHornbeamB" },
            new[] { "CityTree_StreetBeechA", "CityTree_StreetBeechB" },
        };

        private static readonly string[] GardenTrees =
            { "CityTree_GardenBeechA", "CityTree_GardenHornbeamB", "CityTree_GardenBeechA", "CityTree_StreetBeechB", "CityTree_StreetHornbeamA" };

        // §27 (owner, 2026-11-09): trees on both kerbs only where the carriageway between them is at least this wide, m;
        // a narrower street has one row, on the kerbs of alternate sides street by street. Authoring default: the High
        // Street's 10 m keeps both rows, the byelaw streets' 6 m keep one, so 4.8 m of carriageway stays clear by a pit.
        private const float TwoRowsFrom = 8f;

        private static readonly string[] GardenBushes = { "CityBush_006", "CityBush_011", "CityBush_015" };
        private static readonly string[] Hedges = { "CityHedge_019", "CityHedge_020" };
        private static readonly string[] FieldFences = { "FieldFenceA", "FieldFenceB", "FieldFenceC" };
        private static readonly string[] PlankFences = { "PlankFenceA", "PlankFenceB", "PlankFenceC" };
        private static readonly string[] BedFences = { "BedFenceA", "BedFenceB" };

        /// <summary>The farms outside the walls (§3.8): the plan point of each, and the way its front faces.</summary>
        private static readonly (float Px, float Py, Facing F)[] Farms =
            { (150f, 640f, Facing.S), (200f, 700f, Facing.N), (110f, 705f, Facing.N), (230f, 600f, Facing.E) };

        /// <summary>A street's tree: one kind along a street, so it reads as one planting, its variants in turn.</summary>
        private static string RowTree(int street, int i) => StreetKinds[Math.Abs(street) % StreetKinds.Length][Math.Abs(i) % 2];

        /// <summary>A square's row (Crown Avenue's mall, the palace's corners): the beech, its variants in turn.</summary>
        private static string RowTree(int i) => RowTree(1, i);

        private static string GardenTree(Random rng) => GardenTrees[rng.Next(GardenTrees.Length)];

        private static string GardenBush(Random rng) => GardenBushes[rng.Next(GardenBushes.Length)];

        // ------------------------------------------------------------------------------------------- street trees

        /// <summary>
        /// The street trees (§25.3, §26, §27), after the lamps and the shop goods: each in a square pit on the carriageway,
        /// flush on the kerb, in the lamps' line; both kerbs of a wide street, one kerb of a narrow one. A street is its
        /// kerbs' plan lines, the near one's carriageway toward +, the far one's toward − (none where its other side has
        /// no row, as the Parade's villas).
        /// </summary>
        private static void StreetTrees()
        {
            var planted = 0;
            var street = 0;
            var oneSided = 0;
            foreach (var (alongX, near, far, a0, a1) in new (bool, float, float?, int, int)[]
                     {
                         (true, 726f + HighStreetWalk, 744f - HighStreetWalk, 480, 778),
                         (true, 668f + ParadeWalk, null, 480, 752),
                         (false, 516f + ByelawWalk, 528f - ByelawWalk, 680, 726),
                         (true, 772f + ByelawWalk, 784f - ByelawWalk, 480, 550),
                         (true, 772f + ByelawWalk, 784f - ByelawWalk, 618, 701),
                         (false, 516f + ByelawWalk, 528f - ByelawWalk, 744, 812),
                         (false, 606f + ByelawWalk, 618f - ByelawWalk, 744, 812),
                         (false, 701f + ByelawWalk, 713f - ByelawWalk, 744, 812),
                     })
            {
                var both = far.HasValue && far.Value - near >= TwoRowsFrom;
                var nearSide = !far.HasValue || both || street % 2 == 0;
                oneSided += far.HasValue && !both ? 1 : 0;
                if (nearSide)
                {
                    planted += TreeRow(alongX, near, 1, a0, a1, street);
                }

                if (far.HasValue && (both || !nearSide))
                {
                    planted += TreeRow(alongX, far.Value, -1, a0, a1, street);
                }

                street++;
            }

            Debug.Log($"[VeilmouthCity] street trees (§25.3, §26, §27): {planted}, each in its pit by the kerb; {oneSided} narrow streets planted on one side.");
        }

        private static int TreeRow(bool alongX, float line, int side, int a0, int a1, int street)
        {
            var last = float.MinValue;
            var planted = 0;
            var across = line + (side * TreeFromKerb);
            for (var at = a0 + 1; at < a1 - 1; at++)
            {
                var along = at + 0.5f;
                if (along - last < TreeSpacing)
                {
                    continue;
                }

                var c = alongX ? new Vector2(along, across) : new Vector2(across, along);
                if (!KerbRuns(alongX, line, side, along, TreeFromKerb + 0.1f) ||
                    _lampsAt.Any(l => (l - c).sqrMagnitude < TreeFromLamp * TreeFromLamp) ||
                    _doors.Any(d => Mathf.Abs((alongX ? d.At.y : d.At.x) - across) < TreeDoorAcross &&
                                    Mathf.Abs((alongX ? d.At.x : d.At.y) - along) < TreeDoorAlong) ||
                    _lanes.Any(l => l.Use == Use.Road && !l.Carriage && !l.PaintOnly && Overlap(l.Quad, Rect(c.x - LaneMouth, c.y - LaneMouth, c.x + LaneMouth, c.y + LaneMouth))))
                {
                    continue;
                }

                if (OnCarriageway(RowTree(street, planted), c, VeilmouthPavements.PitSize) != null)
                {
                    _pavements.Pit(c);
                    last = along;
                    planted++;
                }
            }

            return planted;
        }

        // ------------------------------------------------------------------------------------------- hedges and beds

        /// <summary>
        /// A clipped hedge on the grid line <paramref name="line"/> from <paramref name="a0"/> to <paramref name="a1"/>, a
        /// metre a block, leaving the metres <paramref name="skip"/> names. Forced, it crosses only a house's margin, as a
        /// railing does; otherwise it leaves any metre already taken.
        /// </summary>
        private static int HedgeLine(bool alongX, float line, int a0, int a1, Random rng, Func<int, bool> skip = null, bool force = false)
        {
            var placed = 0;
            for (var a = a0; a < a1; a++)
            {
                if (skip != null && skip(a))
                {
                    continue;
                }

                var key = Hedges[rng.Next(Hedges.Length)];
                var go = alongX ? Prop(key, Facing.N, a, line - 0.1f, force: force) : Prop(key, Facing.E, line - 0.1f, a, force: force);
                placed += go != null ? 1 : 0;
            }

            return placed;
        }

        /// <summary>A bed: a hedge round the plan rectangle's edge lines, grass and a bush or a few flowers inside.</summary>
        private static void HedgeBed(int x0, int y0, int x1, int y1, Random rng)
        {
            HedgeLine(true, y0, x0, x1, rng, force: true);
            HedgeLine(true, y1, x0, x1, rng, force: true);
            HedgeLine(false, x0, y0, y1, rng, force: true);
            HedgeLine(false, x1, y0, y1, rng, force: true);
            Prop(rng.Next(3) == 0 ? "Poppy" : GardenBush(rng), Facing.N, x0 + 1 + rng.Next(Mathf.Max(1, x1 - x0 - 2)), y0 + 1 + rng.Next(Mathf.Max(1, y1 - y0 - 2)));
            Lawn(Rect(x0 + 0.4f, y0 + 0.4f, x1 - 0.4f, y1 - 0.4f), rng);
        }

        /// <summary>
        /// Wardley Garden (§25.4): four hedged beds round a cross of paths, north to south between the gates (px 554–562)
        /// and east to west between the benches (py 692–703); each bed holds one of the corner trees, the middle one gives
        /// its place to the angel.
        /// </summary>
        private static void WardleyBeds(Random rng)
        {
            foreach (var (x0, y0) in new[] { (549, 687), (562, 687), (549, 703), (562, 703) })
            {
                HedgeBed(x0, y0, x0 + 5, y0 + 5, rng);
            }
        }

        /// <summary>
        /// The palace forecourt's parterre (§25.4): either side of the Crown axis's way a hedged bed of 8 × 10 m quartered
        /// by a cross of hedge, clear of the lions (city plan §23), the corner trees and the gate's lamps.
        /// </summary>
        private static void Parterre(Random rng)
        {
            foreach (var x0 in new[] { 590, 626 })
            {
                const int y0 = 654, y1 = 664;
                var x1 = x0 + 8;
                HedgeLine(true, y0, x0, x1, rng, force: true);
                HedgeLine(true, y1, x0, x1, rng, force: true);
                HedgeLine(false, x0, y0, y1, rng, force: true);
                HedgeLine(false, x1, y0, y1, rng, force: true);
                HedgeLine(false, x0 + 4, y0, y1, rng, force: true);
                HedgeLine(true, y0 + 5, x0, x1, rng, force: true);
                foreach (var (qx, qy) in new[] { (x0, y0), (x0 + 4, y0), (x0, y0 + 5), (x0 + 4, y0 + 5) })
                {
                    Lawn(Rect(qx + 0.4f, qy + 0.4f, qx + 3.6f, qy + 4.6f), rng);
                }
            }
        }

        /// <summary>
        /// Grass on a garden's earth (§25.4): one clump in each 2 × 2 m square, at a cell of it the seed picks, where
        /// nothing stands, and never on a door's steps or its path.
        /// </summary>
        private static void Lawn(Vector2[] area, Random rng)
        {
            var lo = new Vector2(Mathf.Floor(area.Min(p => p.x)), Mathf.Floor(area.Min(p => p.y)));
            var hi = new Vector2(area.Max(p => p.x), area.Max(p => p.y));
            for (var x = lo.x; x < hi.x; x += LawnStep)
            {
                for (var y = lo.y; y < hi.y; y += LawnStep)
                {
                    var px = x + rng.Next(LawnStep);
                    var py = y + rng.Next(LawnStep);
                    var c = new Vector2(px + 0.5f, py + 0.5f);
                    if (!Inside(area, c) || _stoops.Any(s => Overlap(s, Rect(px - 0.3f, py - 0.3f, px + 1.3f, py + 1.3f))) || _doors.Any(d =>
                        {
                            var off = c - d.At;
                            var outside = Vector2.Dot(off, d.Out);
                            return outside > -0.5f && outside < 8f && Mathf.Abs(Vector2.Dot(off, new Vector2(-d.Out.y, d.Out.x))) < 2f;
                        }))
                    {
                        continue;
                    }

                    Prop("Grass", Facing.N, px, py);
                }
            }
        }

        // ------------------------------------------------------------------------------------------- outside the walls

        /// <summary>Roadside Tales by meaning (§25.5): the farms' yards and beds, dry-stone walls and posts by the roads.</summary>
        private static void Roadside()
        {
            var rng = new Random(2525); // authoring layout only, fixed; never the world seed
            foreach (var (px, py, f) in Farms)
            {
                FarmYard(px, py, f, rng);
            }

            // The West road (py 735, its soil 4 m either side) by the farms' fields, and the North road (px 747) across
            // the open land between the forest and the North Gate; a gap toward the flax field.
            var walls = 0;
            walls += Wall(true, 729f, 70, 250, rng, a => false);
            walls += Wall(true, 741f, 90, 262, rng, a => a + 3 > 175 && a < 181);
            walls += Wall(false, 741f, 470, 602, rng, a => false);
            walls += Wall(false, 753f, 470, 602, rng, a => false);

            var posts = 0;
            for (var px = 70; px < 462; px += PostEvery)
            {
                posts += Prop("RoadPost", Facing.N, px, 739f) != null ? 1 : 0;
            }

            for (var py = 80; py < 606; py += PostEvery)
            {
                posts += Prop("RoadPost", Facing.N, 742f, py) != null ? 1 : 0;
            }

            Debug.Log($"[VeilmouthCity] roadside (§25.5): {walls} wall pieces, {posts} posts, {Farms.Length} farm yards.");
        }

        /// <summary>
        /// A dry-stone wall on a grid line, a piece every 3 m, with a 3 m gap every <see cref="WallGapEvery"/> m and where
        /// <paramref name="gap"/> says.
        /// </summary>
        private static int Wall(bool alongX, float line, int a0, int a1, Random rng, Func<int, bool> gap)
        {
            var placed = 0;
            for (var a = a0; a + 3 <= a1; a += 3)
            {
                if ((a - a0) % WallGapEvery >= WallGapEvery - 3 || gap(a))
                {
                    continue;
                }

                var go = alongX ? Prop("RoadWall", Facing.N, a, line - 0.1f, force: true) : Prop("RoadWall", Facing.E, line - 0.1f, a, force: true);
                placed += go != null ? 1 : 0;
            }

            return placed;
        }

        /// <summary>
        /// A farm's yard before its front, fenced with planks on three sides with a way in of 4 m before the door, and a
        /// vegetable bed fenced beside it (§25.5).
        /// </summary>
        private static void FarmYard(float px, float py, Facing f, Random rng)
        {
            var e = _kit["House_Farm"];
            var alongX = f == Facing.N || f == Facing.S;
            var front = alongX ? py : px;
            var a = (int)(alongX ? px : py);
            var s = (int)(Outward(f).x + Outward(f).y);
            var doorAlong = e.DoorX > 0f ? (f == Facing.N || f == Facing.E ? a + e.DoorX : a + e.Width - e.DoorX) : a + (e.Width / 2f);
            var a0 = a - 2;
            var a1 = a + e.Width + 2;
            a1 += (a1 - a0) % 2;
            var far = front + (s * YardDepth);
            var (near0, near1) = ((int)Mathf.Min(front, far), (int)Mathf.Max(front, far));
            var plank = 0;
            void Planks(bool along, float line, int from, int to, Func<int, bool> skip)
            {
                for (var at = from; at + 2 <= to; at += 2)
                {
                    if (skip(at))
                    {
                        continue;
                    }

                    var key = PlankFences[plank++ % PlankFences.Length];
                    if (along)
                    {
                        Prop(key, Facing.N, at, line - 0.1f, force: true);
                    }
                    else
                    {
                        Prop(key, Facing.E, line - 0.1f, at, force: true);
                    }
                }
            }

            Planks(!alongX, a0, near0, near1, _ => false);
            Planks(!alongX, a1, near0, near1, _ => false);
            Planks(alongX, far, a0, a1, at => Mathf.Abs(at + 1f - doorAlong) < 2f);

            // The bed: 6 m along beyond the yard's side, 4 m out, 2 m from the house's front line.
            var b0 = a1 + 2;
            var b1 = b0 + 6;
            var o0 = front + (s * 2);
            var o1 = front + (s * 6);
            var (out0, out1) = ((int)Mathf.Min(o0, o1), (int)Mathf.Max(o0, o1));
            var bed = 0;
            void Bed(bool along, float line, int from, int to)
            {
                for (var at = from; at + 2 <= to; at += 2)
                {
                    var key = BedFences[bed++ % BedFences.Length];
                    if (along)
                    {
                        Prop(key, Facing.N, at, line - 0.1f, force: true);
                    }
                    else
                    {
                        Prop(key, Facing.E, line - 0.1f, at, force: true);
                    }
                }
            }

            Bed(alongX, out0, b0, b1);
            Bed(alongX, out1, b0, b1);
            Bed(!alongX, b0, out0, out1);
            Bed(!alongX, b1, out0, out1);
            Lawn(alongX ? Rect(b0 + 0.4f, out0 + 0.4f, b1 - 0.4f, out1 - 0.4f) : Rect(out0 + 0.4f, b0 + 0.4f, out1 - 0.4f, b1 - 0.4f), rng);
        }

        // ------------------------------------------------------------------------------------------- the rebuild

        /// <summary>
        /// Batch only (§25.6): the city built again on its present ground with its greenery, the passes beside its root,
        /// then POI A's and POI B's stages and the next day's residue placed again, since they stood among the old
        /// walkers and on the old kerb line, and the greenery's views. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthCity.RebuildGreenery -lookLabel green
        /// </summary>
        public static void RebuildGreenery()
        {
            RebuildStreets();
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            var stale = new[] { SlicePoiStages.PoiAStage, SlicePoiStages.PoiBStage, SliceResidue.SealedName, SliceResidue.PairsName, SliceResidue.FearName };
            foreach (var old in scene.GetRootGameObjects().Where(g => stale.Contains(g.name)).ToList())
            {
                Object.DestroyImmediate(old);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            SlicePoiStages.StagePoiA();
            SlicePoiStages.StagePoiB();
            SliceResidue.PlaceAll();
            VeilmouthRenders.RenderGreenery();
        }
    }
}
