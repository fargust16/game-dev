using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Veilmouth on the island, what lies between the districts (Assets/Plans/ISLAND_CITY_REBUILD_PLAN.md §4, IC5): the
    /// town's walls with their gates, the quays with their stairs to the water, the retaining walls, the bridges and
    /// the pier, and the two streets that run between districts.
    /// <para>
    /// Built in place, in the world's own metres, from the island's layout and the terrain as it lies: a wall follows
    /// the ground in 4 m pieces, and what stands along a district's side is read from the ground either side of it,
    /// piece by piece, never from a list of heights. All of it is plain stone (<see cref="Works"/>), by the owner's
    /// fifth principle: nothing is modelled where a box carries it.
    /// </para>
    /// Every size here is an authoring default.
    /// </summary>
    public static partial class VeilmouthCity
    {
        [Serializable]
        private sealed class IslandLine
        {
            public string name, kind;
            public float height;
            public VeilmouthLayout.Point[] line;
        }

        [Serializable]
        private sealed class IslandSpan
        {
            public string name;
            public float ax, ay, bx, by, width;
        }

        [Serializable]
        private sealed class IslandPlace
        {
            public string id, key, name;
            public float x, y;
        }

        /// <summary>Sea Level, m (AGENTS.md, Overworld terrain stack): the island's tide stands at it.</summary>
        private const float IslandSea = 24f;

        // The town wall: its thickness; a gate's half opening, its towers' side and the clear height under its lintel, m.
        private const float TownWallThick = 1.8f, GateHalf = 3.5f, GateTower = 4.6f, GateClear = 4.6f;

        // A coping course of lighter stone on the wall's and its towers' tops: its overhang and its height, m.
        private const float CopeOver = 0.15f, CopeHigh = 0.3f;

        // A quay wall reaches QuayIn m into the district's ground (its coping, as C5's) and QuayOut m out over the
        // ground's fall; a stair down it is StairWide m wide, of risers and runs under the kit standard's limits (§3).
        private const float QuayIn = 0.5f, QuayOut = 1.8f, StairWide = 2f, StairRise = 0.17f, StairRun = 0.32f;

        // A quay's coping stands a kerb's height over the district's ground at its edge, m: under the walker's step.
        private const float QuayKerb = 0.25f;

        // A district's side gets a wall where the ground either side differs by more than this, m; a bank under it.
        private const float WallOver = 0.7f;

        private static float IslandGround(Vector2 p) => _terrain.SampleHeight(World(p.x, p.y)) + _terrain.transform.position.y;

        /// <summary>
        /// A stone box along the plan line from <paramref name="a"/> in the direction <paramref name="d"/>: from
        /// <paramref name="s0"/> to <paramref name="s1"/> m along it and from <paramref name="c0"/> to
        /// <paramref name="c1"/> m to the right of it.
        /// </summary>
        private static void Along(Works w, Vector2 a, Vector2 d, float s0, float s1, float c0, float c1, float y0, float y1, string material = null)
        {
            if (s1 - s0 < 0.01f || c1 - c0 < 0.01f || y1 - y0 < 0.01f)
            {
                return;
            }

            var n = new Vector2(-d.y, d.x);
            var mid = a + (d * ((s0 + s1) / 2f)) + (n * ((c0 + c1) / 2f));
            w.Box(World(mid.x, mid.y, (y0 + y1) / 2f), new Vector3(s1 - s0, y1 - y0, c1 - c0), Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, true, material);
        }

        private static void IslandTissue(IslandFile file, Transform city, float[,,] maps, (int Grass, int Earth, int Setts, int Cobbles) paving, StringBuilder report)
        {
            _worksFolder = $"{IslandWorksFolder}/tissue";
            Transform Owner(string name)
            {
                var t = new GameObject(name).transform;
                t.SetParent(city, false);
                return t;
            }

            void Part(string name, Action<Works, Transform> build)
            {
                try
                {
                    var owner = Owner(name);
                    var works = new Works(owner, true);
                    build(works, owner);
                    works.Finish(WallStone);
                }
                catch (Exception e)
                {
                    report.AppendLine($"{name} FAILED ({e.GetType().Name}: {e.Message.Split('\n')[0]})");
                    Debug.LogException(e);
                }
            }

            Part("Town Walls", (w, _) => IslandWalls(file, w, report));
            Part("Waterside", (w, _) => IslandEdges(file, w, report));
            Part("Bridges", (w, owner) => IslandBridges(file, w, owner, report));
            IslandStreets(file, maps, paving, report);
        }

        // ------------------------------------------------------------------------------------------- the town's walls

        private static void IslandWalls(IslandFile file, Works w, StringBuilder report)
        {
            var gates = file.gates ?? Array.Empty<IslandPlace>();
            var hung = new HashSet<string>();
            var metres = 0f;
            var towers = 0;
            foreach (var wall in file.walls ?? Array.Empty<IslandLine>())
            {
                var pts = wall.line.Select(p => new Vector2(p.x, p.y)).ToArray();
                for (var k = 0; k + 1 < pts.Length; k++)
                {
                    var a = pts[k];
                    var length = (pts[k + 1] - a).magnitude;
                    var d = (pts[k + 1] - a) / length;
                    var gaps = new List<(float A, float B)>();
                    foreach (var g in gates)
                    {
                        var p = new Vector2(g.x, g.y) - a;
                        var t = Vector2.Dot(p, d);
                        if (Mathf.Abs(Vector2.Dot(p, new Vector2(-d.y, d.x))) > 3f || t < GateHalf + GateTower + 2f || t > length - GateHalf - GateTower - 2f)
                        {
                            continue;
                        }

                        // The wall's pieces end inside the gate's towers, so no two faces lie in one plane.
                        gaps.Add((t - GateHalf - 0.2f, t + GateHalf + 0.2f));
                        IslandGate(w, a, d, t, wall.height);
                        hung.Add(g.name);
                    }

                    for (var s = 0f; s < length - 0.01f; s += 4f)
                    {
                        foreach (var (s0, s1) in Runs(s, Mathf.Min(s + 4f, length), gaps))
                        {
                            var g0 = IslandGround(a + (d * s0));
                            var g1 = IslandGround(a + (d * s1));
                            var gm = IslandGround(a + (d * ((s0 + s1) / 2f)));
                            var hi = Mathf.Max(g0, Mathf.Max(g1, gm));
                            if (hi < IslandSea - 0.6f)
                            {
                                continue; // the wall ends at the water
                            }

                            Along(w, a, d, s0, s1, -TownWallThick / 2f, TownWallThick / 2f, Mathf.Min(g0, Mathf.Min(g1, gm)) - 1f, hi + wall.height);
                            Along(w, a, d, s0, s1, (-TownWallThick / 2f) - CopeOver, (TownWallThick / 2f) + CopeOver, hi + wall.height, hi + wall.height + CopeHigh, KerbStone);
                            metres += s1 - s0;
                        }
                    }
                }

                // A square tower at every turn and end of the wall, on dry ground: it closes the corner two runs leave.
                for (var k = 0; k < pts.Length; k++)
                {
                    var g = IslandGround(pts[k]);
                    if (g < IslandSea - 0.3f)
                    {
                        continue;
                    }

                    var d = (k + 1 < pts.Length ? pts[k + 1] - pts[k] : pts[k] - pts[k - 1]).normalized;
                    Along(w, pts[k], d, -2.3f, 2.3f, -2.3f, 2.3f, g - 1.5f, g + wall.height + 1.6f);
                    Along(w, pts[k], d, -2.3f - CopeOver, 2.3f + CopeOver, -2.3f - CopeOver, 2.3f + CopeOver, g + wall.height + 1.6f, g + wall.height + 1.6f + CopeHigh, KerbStone);
                    towers++;
                }
            }

            report.AppendLine($"town walls: {metres:0} m in {(file.walls ?? Array.Empty<IslandLine>()).Length} runs, {towers} towers, {hung.Count} gates" +
                              (hung.Count < gates.Length ? "; NOT ON A WALL: " + string.Join(", ", gates.Where(g => !hung.Contains(g.name)).Select(g => g.name)) : ""));
        }

        /// <summary>A gate at <paramref name="t"/> m along a wall: two square towers and the lintel between them.</summary>
        private static void IslandGate(Works w, Vector2 a, Vector2 d, float t, float height)
        {
            var g = Mathf.Max(IslandGround(a + (d * t)), Mathf.Max(IslandGround(a + (d * (t - GateHalf))), IslandGround(a + (d * (t + GateHalf)))));
            var top = g + height;
            foreach (var side in new[] { -1f, 1f })
            {
                var s = t + (side * (GateHalf + (GateTower / 2f)));
                Along(w, a, d, s - (GateTower / 2f), s + (GateTower / 2f), -GateTower / 2f, GateTower / 2f,
                    Mathf.Min(g, IslandGround(a + (d * s))) - 1.5f, top + 2.5f);
                Along(w, a, d, s - (GateTower / 2f) - CopeOver, s + (GateTower / 2f) + CopeOver, (-GateTower / 2f) - CopeOver, (GateTower / 2f) + CopeOver,
                    top + 2.5f, top + 2.5f + CopeHigh, KerbStone);
            }

            Along(w, a, d, t - GateHalf - 0.2f, t + GateHalf + 0.2f, -TownWallThick / 2f, TownWallThick / 2f, g + GateClear, top + 0.8f);
        }

        // ------------------------------------------------------------------------------------------- a district's sides

        private enum SideKind { Bank, Quay, Hold, Cut }

        /// <summary>
        /// What stands along each district's four sides, read from the ground 1 m inside the side and 3 m outside it,
        /// in 4 m pieces: a quay wall where the outside is the water, a retaining wall where the town stands over the
        /// land (it gets a parapet over 1.2 m) or lies under it, and nothing where a bank carries the difference. A
        /// stair goes down a quay where a street of the district comes out on it.
        /// </summary>
        private static void IslandEdges(IslandFile file, Works w, StringBuilder report)
        {
            float quay = 0f, hold = 0f, cut = 0f;
            var stairs = 0;
            foreach (var f in file.frames.Where(f => f.streets != null && f.streets.Length > 0))
            {
                var yaw = KitLayoutCheck.NearestMeasuredTurn(f.angle) * Mathf.Deg2Rad;
                var (ex, ey) = (new Vector2(Mathf.Cos(yaw), Mathf.Sin(yaw)), new Vector2(-Mathf.Sin(yaw), Mathf.Cos(yaw)));
                Vector2 P(float u, float v) => new Vector2(f.origin.x, f.origin.y) + (ex * u) + (ey * v);

                // A side: its ends, the way out of the district, and where a street of the other axis meets it.
                var sides = new (Vector2 A, Vector2 B, Vector2 Out, bool AlongU, Func<float, float> AtOf)[]
                {
                    (P(0f, 0f), P(f.U, 0f), -ey, true, u => u), (P(f.U, 0f), P(f.U, f.V), ex, false, v => v),
                    (P(f.U, f.V), P(0f, f.V), ey, true, u => f.U - u), (P(0f, f.V), P(0f, 0f), -ex, false, v => f.V - v),
                };
                foreach (var (a, b, outward, alongU, atOf) in sides)
                {
                    var length = (b - a).magnitude;
                    var d = (b - a) / length;
                    var sign = Vector2.Dot(new Vector2(-d.y, d.x), outward) > 0f ? 1f : -1f;
                    // A band across the side, in metres outward of it.
                    (float C0, float C1) Band(float o0, float o1) => sign > 0f ? (o0, o1) : (-o1, -o0);
                    float Inside(float s) => IslandGround(a + (d * s) - outward);
                    float Outside(float s) => IslandGround(a + (d * s) + (outward * 3f));

                    var count = Mathf.CeilToInt(length / 4f);
                    var kinds = new SideKind[count];
                    var tops = new float[count];
                    for (var k = 0; k < count; k++)
                    {
                        var (s0, s1) = (k * 4f, Mathf.Min((k + 1) * 4f, length));
                        var sm = (s0 + s1) / 2f;
                        var gi = Mathf.Max(Inside(s0), Mathf.Max(Inside(sm), Inside(s1)));
                        var go = Mathf.Min(Outside(s0), Mathf.Min(Outside(sm), Outside(s1)));
                        kinds[k] = go < IslandSea + 0.3f ? SideKind.Quay : gi - go > WallOver ? SideKind.Hold : go - gi > WallOver ? SideKind.Cut : SideKind.Bank;
                        tops[k] = gi;
                        // The first and the last piece run on round the corner, by the widest wall's reach.
                        var (r0, r1) = (k == 0 ? s0 - QuayOut : s0, k == count - 1 ? s1 + QuayOut : s1);
                        switch (kinds[k])
                        {
                            case SideKind.Quay:
                            {
                                var (c0, c1) = Band(-QuayIn, QuayOut);
                                Along(w, a, d, r0, r1, c0, c1, Mathf.Min(go, IslandSea) - 1.5f, gi + QuayKerb);
                                quay += s1 - s0;
                                break;
                            }

                            case SideKind.Hold:
                            {
                                var (c0, c1) = Band(-0.3f, 0.6f);
                                Along(w, a, d, r0, r1, c0, c1, go - 1f, gi + (gi - go > 1.2f ? 0.9f : 0.15f));
                                hold += s1 - s0;
                                break;
                            }

                            case SideKind.Cut:
                            {
                                var (c0, c1) = Band(0f, 0.9f);
                                Along(w, a, d, r0, r1, c0, c1, gi - 0.8f, Mathf.Max(Outside(s0), Mathf.Max(Outside(sm), Outside(s1))) + 0.4f);
                                cut += s1 - s0;
                                break;
                            }
                        }
                    }

                    // A stair where a street of the other axis comes out on a quay: it needs 9 m of quay one way or the other.
                    foreach (var street in f.streets.Where(st => st.AlongX != alongU).Take(4))
                    {
                        var at = atOf((street.c0 + street.c1) / 2f);
                        bool Quay(float s0, float s1) =>
                            s0 >= 0f && s1 <= length && Enumerable.Range(Mathf.FloorToInt(s0 / 4f), Mathf.FloorToInt(s1 / 4f) - Mathf.FloorToInt(s0 / 4f) + 1)
                                .All(k => k < count && kinds[k] == SideKind.Quay);
                        var dir = Quay(at - 1.5f, at + 9f) ? 1f : Quay(at - 9f, at + 1.5f) ? -1f : 0f;
                        if (dir == 0f)
                        {
                            continue;
                        }

                        var (c0, c1) = Band(QuayOut, QuayOut + StairWide);
                        var top = tops[Mathf.Clamp(Mathf.FloorToInt(at / 4f), 0, count - 1)] + QuayKerb;
                        var bed = IslandSea - 3f;
                        void Tread(float from, float to, float level) => Along(w, a, d, Mathf.Min(from, to), Mathf.Max(from, to), c0, c1, bed, level, StepStone);
                        Tread(at - dir, at + dir, top);
                        var s = at + dir;
                        var y = top;
                        while (y - StairRise > IslandSea + 0.1f)
                        {
                            y -= StairRise;
                            Tread(s, s + (dir * StairRun), y);
                            s += dir * StairRun;
                        }

                        Tread(s, s + (dir * 1.8f), y); // the landing at the tide
                        stairs++;
                    }
                }
            }

            report.AppendLine($"districts' sides: {quay:0} m of quay wall with {stairs} stairs to the water, {hold:0} m of wall holding the town, {cut:0} m holding the hill");
        }

        // ------------------------------------------------------------------------------------------- bridges and the pier

        /// <summary>
        /// A stone bridge on the plan line of its two ends: its deck runs on to the land at each end at the ground's own
        /// height there and rises to a level crown over the water, high enough for the tide to pass under; abutments
        /// under its ends, piers every 12 m or so between them.
        /// </summary>
        private static void IslandBridges(IslandFile file, Works w, Transform owner, StringBuilder report)
        {
            foreach (var b in file.bridges ?? Array.Empty<IslandSpan>())
            {
                var a = new Vector2(b.ax, b.ay);
                var length = (new Vector2(b.bx, b.by) - a).magnitude;
                var d = (new Vector2(b.bx, b.by) - a) / length;
                // What it spans: the water, or a brook's bed 0.7 m and more under the lower of its two ends.
                var brim = Mathf.Min(IslandGround(a), IslandGround(a + (d * length))) - 0.7f;
                float w0 = -1f, w1 = -1f;
                for (var s = 0f; s <= length; s += 1f)
                {
                    var under = IslandGround(a + (d * s));
                    if (under < IslandSea + 0.2f || under < brim)
                    {
                        w0 = w0 < 0f ? s : w0;
                        w1 = s;
                    }
                }

                if (w0 < 0f)
                {
                    report.AppendLine($"bridge {b.name}: the ground is level under its line, so its street runs on it and no bridge is built");
                    continue;
                }

                var foot = b.width < 5f;
                var depth = foot ? 0.5f : 1f;
                var run = foot ? 2f : 4f;
                var (s0, s1) = (-run, length + run);
                var (y0, y1) = (IslandGround(a + (d * s0)) + 0.05f, IslandGround(a + (d * s1)) + 0.05f);
                var crown = Mathf.Max(Mathf.Max(y0, y1) + (foot ? 0.4f : 0.8f), IslandSea + depth + 1.2f);
                var (m0, m1) = (Mathf.Lerp(w0, w1, 0.25f), Mathf.Lerp(w0, w1, 0.75f));
                var profile = new[] { (S: s0, Y: y0), (S: m0, Y: crown), (S: m1, Y: crown), (S: s1, Y: y1) };
                float Top(float s)
                {
                    for (var k = 0; k + 1 < profile.Length; k++)
                    {
                        if (s <= profile[k + 1].S || k + 2 == profile.Length)
                        {
                            return Mathf.Lerp(profile[k].Y, profile[k + 1].Y, Mathf.InverseLerp(profile[k].S, profile[k + 1].S, s));
                        }
                    }

                    return crown;
                }

                Vector3 At(float s, float y) => World(a.x + (d.x * s), a.y + (d.y * s), y);
                IslandDeck(w, owner, profile.Select(p => At(p.S, p.Y)).ToList(), b.width, depth, foot ? 0.25f : Parapet, ParapetHeight);

                // Under the deck: a box's top lies just under the deck's top at its lower end, so it never shows through.
                void Under(float from, float to, float half, float lower)
                {
                    for (var s = from; s < to - 0.01f; s += 2f)
                    {
                        var e = Mathf.Min(s + 2f, to);
                        var floor = Mathf.Min(IslandGround(a + (d * s)), IslandGround(a + (d * e))) - 1.5f;
                        Along(w, a, d, s, e, -half, half, floor, Mathf.Min(Top(s), Top(e)) - lower);
                    }
                }

                Under(s0, w0 + 2f, (b.width / 2f) - 0.03f, 0.03f);
                Under(w1 - 2f, s1, (b.width / 2f) - 0.03f, 0.03f);
                var spans = Mathf.Max(1, Mathf.RoundToInt((w1 - w0 - 4f) / 12f));
                for (var k = 1; k < spans; k++)
                {
                    var at = Mathf.Lerp(w0 + 2f, w1 - 2f, (float)k / spans);
                    Under(at - 1f, at + 1f, (b.width / 2f) + 0.5f, depth * 0.6f);
                }

                report.AppendLine($"bridge {b.name}: {length:0} m, {w1 - w0:0} m over the water or the bed, {b.width:0} m wide; its ends at {y0 - IslandSea:0.0} and {y1 - IslandSea:0.0} m over the tide, its crown at {crown - IslandSea:0.0} m, {spans - 1} piers");
            }

            foreach (var p in file.piers ?? Array.Empty<IslandSpan>())
            {
                var a = new Vector2(p.ax, p.ay);
                var length = (new Vector2(p.bx, p.by) - a).magnitude;
                var d = (new Vector2(p.bx, p.by) - a) / length;
                // Its root: the pier's line is followed back to the quay it leaves, and its deck lies at the quay's top.
                var root = 0f;
                while (root > -12f && IslandGround(a + (d * root)) < IslandSea + 1f)
                {
                    root -= 1f;
                }

                if (root <= -12f)
                {
                    report.AppendLine("pier: NO QUAY within 12 m of its landward end, not built");
                    continue;
                }

                root -= 2f;
                var y = IslandGround(a + (d * root)) + QuayKerb;
                IslandDeck(w, owner, new[] { World(a.x + (d.x * root), a.y + (d.y * root), y), World(p.bx, p.by, y) }, p.width, 0.5f, 0f, 0f);
                for (var s = 2f; s < length - 0.4f; s += 6f)
                {
                    foreach (var c in new[] { (-p.width / 2f) + 0.9f, (p.width / 2f) - 0.9f })
                    {
                        Along(w, a, d, s - 0.4f, s + 0.4f, c - 0.4f, c + 0.4f, Mathf.Min(IslandGround(a + (d * s)), IslandSea) - 2f, y - 0.4f);
                    }
                }

                report.AppendLine($"pier: {length:0} m, its deck {y - IslandSea:0.0} m over the tide");
            }
        }

        /// <summary>
        /// A deck along the world points of its top's centre line, each length sloped as its ends ask: its top in the
        /// streets' setts, its sides and soffit in stone, a parapet each side where one is asked, and a box collider
        /// laid to each length's slope, so a walker meets no step.
        /// </summary>
        private static void IslandDeck(Works w, Transform owner, IReadOnlyList<Vector3> line, float width, float depth, float parapet, float parapetHeight)
        {
            for (var k = 0; k + 1 < line.Count; k++)
            {
                var (p, q) = (line[k], line[k + 1]);
                var flat = new Vector3(q.x - p.x, 0f, q.z - p.z).normalized;
                var right = Vector3.Cross(Vector3.up, flat);
                var h = right * (width / 2f);
                var down = Vector3.down * depth;
                w.Poly(new[] { p - h, p + h, q + h, q - h }, Vector3.up, DeckStone);
                w.Poly(new[] { p - h + down, p + h + down, q + h + down, q - h + down }, Vector3.down);
                w.Poly(new[] { p + h, q + h, q + h + down, p + h + down }, right);
                w.Poly(new[] { p - h, q - h, q - h + down, p - h + down }, -right);
                var rotation = Quaternion.LookRotation(q - p, Vector3.up);
                void Solid(Vector3 centre, Vector3 size)
                {
                    var body = new GameObject("Deck").transform;
                    body.SetParent(owner, false);
                    body.SetPositionAndRotation((p + q) / 2f, rotation);
                    body.gameObject.layer = owner.gameObject.layer;
                    var box = body.gameObject.AddComponent<BoxCollider>();
                    box.center = centre;
                    box.size = size;
                }

                Solid(new Vector3(0f, -depth / 2f, 0f), new Vector3(width, depth, (q - p).magnitude));
                if (parapet <= 0f)
                {
                    continue;
                }

                var up = Vector3.up * parapetHeight;
                foreach (var side in new[] { -1f, 1f })
                {
                    var outer = h * side;
                    var inner = outer - (right * (side * parapet));
                    w.Poly(new[] { p + outer, q + outer, q + outer + up, p + outer + up }, right * side);
                    w.Poly(new[] { p + inner, q + inner, q + inner + up, p + inner + up }, right * -side);
                    w.Poly(new[] { p + inner + up, q + inner + up, q + outer + up, p + outer + up }, Vector3.up);
                    if (k == 0)
                    {
                        w.Poly(new[] { p + inner, p + outer, p + outer + up, p + inner + up }, -flat);
                    }

                    if (k + 2 == line.Count)
                    {
                        w.Poly(new[] { q + inner, q + outer, q + outer + up, q + inner + up }, flat);
                    }

                    Solid(new Vector3(side * ((width / 2f) - (parapet / 2f)), parapetHeight / 2f, 0f), new Vector3(parapet, parapetHeight, (q - p).magnitude));
                }
            }
        }

        // ------------------------------------------------------------------------------------------- the town's edge

        // A garden plot beyond a district's last street: it begins GardenNear m off the district's side, is 9 to 16 m
        // wide and 6 to GardenDeep m deep, behind a stone wall GardenWall m high; one place in seven is left open.
        private const float GardenWall = 1.1f;
        private const int GardenNear = 2, GardenDeep = 12;

        /// <summary>
        /// The town's edge on the land (the owner's first and second principles): beyond a district's last street lie
        /// walled garden plots of uneven width and depth, kitchen gardens of bare earth, orchards on lush turf and
        /// rough grass, so a district's side is no ruled line between paving and turf. A plot is left out where the
        /// ground falls to the water or climbs, where a road or a bridge leaves the district, and where the town's
        /// wall stands close. Built on the slip, in the district's own metres, its walls in the district's stone
        /// works: no new mesh. Few trees, by the frame budget: the island's woodland brings the cheap ones.
        /// </summary>
        private static void EdgeGardens(IslandFrame f, float[,,] maps, (int Grass, int Earth, int Setts, int Cobbles) paving)
        {
            var rng = new System.Random(4100 + (int)f.origin.x);
            var lush = Array.FindIndex(_terrain.terrainData.terrainLayers, l => l != null && l.name.Contains("leafy_grass"));
            Vector2 Plan(Vector2 local)
            {
                var world = _frameWorld(local.x, local.y);
                return new Vector2(world.x + 500f, 500f - world.z);
            }

            // What a plot keeps clear of, each with its distance, m: the town's walls, the roads, the bridges.
            var lines = new List<(Vector2 A, Vector2 B, float Keep)>();
            foreach (var wall in _island.walls ?? Array.Empty<IslandLine>())
            {
                for (var k = 0; k + 1 < wall.line.Length; k++)
                {
                    lines.Add((new Vector2(wall.line[k].x, wall.line[k].y), new Vector2(wall.line[k + 1].x, wall.line[k + 1].y), 3.5f));
                }
            }

            foreach (var road in _island.roads ?? Array.Empty<IslandLine>())
            {
                for (var k = 0; k + 1 < road.line.Length; k++)
                {
                    lines.Add((new Vector2(road.line[k].x, road.line[k].y), new Vector2(road.line[k + 1].x, road.line[k + 1].y), 8f));
                }
            }

            foreach (var b in _island.bridges ?? Array.Empty<IslandSpan>())
            {
                lines.Add((new Vector2(b.ax, b.ay), new Vector2(b.bx, b.by), 6f + (b.width / 2f)));
            }

            bool Clear(Vector2 p) => lines.All(l =>
            {
                var ab = l.B - l.A;
                var t = Mathf.Clamp01(Vector2.Dot(p - l.A, ab) / ab.sqrMagnitude);
                return (p - l.A - (ab * t)).magnitude > l.Keep;
            });

            int plots = 0, trees = 0;
            // A side: whether it lies along x, its line, the way out of the district across it, and its length.
            foreach (var (alongX, line, sign, length) in new[] { (true, 0f, -1, f.U), (true, f.V, 1, f.U), (false, 0f, -1, f.V), (false, f.U, 1, f.V) })
            {
                // The side's own street: an embankment and a quay look on the water, and nothing stands between.
                var along = f.streets.Where(s => s.AlongX == alongX).ToList();
                var last = along.Count == 0 ? null : sign < 0 ? along.OrderBy(s => s.c0).First() : along.OrderByDescending(s => s.c1).First();
                if (last != null && (last.kind == "embankment" || last.kind == "quay"))
                {
                    continue;
                }

                // A point of the side's own frame: t m along the side, c m out of the district.
                Vector2 Local(float t, float c) => alongX ? new Vector2(t, line + (sign * c)) : new Vector2(line + (sign * c), t);
                // A wall across the side, at t; and one along it, at c, with a way in if one is given.
                void Across(int t, int c0, int c1)
                {
                    var (p, q) = (Local(t, c0), Local(t, c1));
                    if (alongX)
                    {
                        WallRun(true, t - 0.2f, t + 0.2f, (int)Mathf.Min(p.y, q.y), (int)Mathf.Max(p.y, q.y), GardenWall);
                    }
                    else
                    {
                        WallRun(false, t - 0.2f, t + 0.2f, (int)Mathf.Min(p.x, q.x), (int)Mathf.Max(p.x, q.x), GardenWall);
                    }
                }

                void Lengthwise(int c, int t0, int t1, params (int, int)[] way)
                {
                    var at = alongX ? Local(t0, c).y : Local(t0, c).x;
                    WallRun(!alongX, at - 0.2f, at + 0.2f, t0, t1, GardenWall, way);
                }

                var from = 3;
                var (lastEnd, lastFar) = (-1, 0);
                while (from < length - 12f)
                {
                    var to = Mathf.Min(from + 9 + rng.Next(8), (int)length - 3);
                    var far = GardenNear + 6 + rng.Next(GardenDeep - 5);
                    var kind = rng.Next(7);
                    var mid = (from + to) / 2f;
                    var edge = IslandGround(Plan(Local(mid, -1f)));
                    var fits = to - from >= 7 && kind != 6 &&
                               new[] { (from, GardenNear), (to, GardenNear), (from, far), (to, far), (mid, far), (mid, (GardenNear + far) / 2f) }.All(c =>
                               {
                                   var p = Plan(Local(c.Item1, c.Item2));
                                   var g = IslandGround(p);
                                   return g > IslandSea + 0.8f && Mathf.Abs(g - edge) < 2.2f && Clear(p);
                               });
                    if (!fits)
                    {
                        from = to;
                        continue;
                    }

                    // The wall round it; the side it shares with the plot before it is built once, by the deeper of the two.
                    var way = from + 2 + rng.Next(Mathf.Max(1, to - from - 5));
                    Lengthwise(GardenNear, from, to, (way, way + 2));
                    Lengthwise(far, from, to);
                    Across(to, GardenNear, far);
                    if (lastEnd != from)
                    {
                        Across(from, GardenNear, far);
                    }
                    else if (far > lastFar)
                    {
                        Across(from, lastFar, far);
                    }

                    // Its ground: a kitchen garden's bare earth, an orchard's lush turf, or the land as it lies.
                    var (a, b) = (Local(from, GardenNear), Local(to, far));
                    var (x0, y0, x1, y1) = (Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                    var layer = kind <= 1 ? paving.Earth : kind <= 3 ? lush : -1;
                    if (layer >= 0)
                    {
                        PaintLocal(maps, x0 + 0.3f, y0 + 0.3f, x1 - 0.3f, y1 - 0.3f, layer);
                    }

                    if (kind == 2 || kind == 3)
                    {
                        var count = to - from >= 14 ? 3 : 2;
                        for (var k = 0; k < count; k++)
                        {
                            var p = Local(Mathf.Round(Mathf.Lerp(from + 2f, to - 3f, (k + 0.5f) / count)), GardenNear + 2 + rng.Next(Mathf.Max(1, far - GardenNear - 5)));
                            trees += Prop("Tree_Fruit", Facing.N, p.x, p.y) != null ? 1 : 0;
                        }
                    }

                    plots++;
                    (lastEnd, lastFar) = (to, far);
                    from = to;
                }
            }

            Debug.Log($"[VeilmouthCity] {f.name}: {plots} garden plots beyond its last streets, {trees} fruit trees.");
        }

        /// <summary>The terrain's texels inside a rectangle of the district's own metres take one layer, where the district will lie.</summary>
        private static void PaintLocal(float[,,] maps, float x0, float y0, float x1, float y1, int layer)
        {
            var size = maps.GetLength(0);
            var layers = maps.GetLength(2);
            var cell = VeilmouthScene.Extent / size;
            var quad = new[] { _frameWorld(x0, y0), _frameWorld(x1, y0), _frameWorld(x1, y1), _frameWorld(x0, y1) }
                .Select(w => new Vector2(w.x + 500f, 500f - w.z)).ToArray();
            var ix0 = Mathf.Max(0, Mathf.FloorToInt(quad.Min(p => p.x) / cell) - 1);
            var ix1 = Mathf.Min(size - 1, Mathf.CeilToInt(quad.Max(p => p.x) / cell) + 1);
            var iz0 = Mathf.Max(0, Mathf.FloorToInt((VeilmouthScene.Extent - quad.Max(p => p.y)) / cell) - 1);
            var iz1 = Mathf.Min(size - 1, Mathf.CeilToInt((VeilmouthScene.Extent - quad.Min(p => p.y)) / cell) + 1);
            for (var z = iz0; z <= iz1; z++)
            {
                for (var x = ix0; x <= ix1; x++)
                {
                    if (!Inside(quad, new Vector2((x + 0.5f) * cell, VeilmouthScene.Extent - ((z + 0.5f) * cell))))
                    {
                        continue;
                    }

                    for (var l = 0; l < layers; l++)
                    {
                        maps[z, x, l] = l == layer ? 1f : 0f;
                    }
                }
            }
        }

        // ------------------------------------------------------------------------------------------- streets between districts

        /// <summary>
        /// The streets that run from one district to the next, laid in setts on the ground as it lies: the terrain's
        /// texels within half the street's width of its line. Country roads keep the earth the island gave them.
        /// </summary>
        private static void IslandStreets(IslandFile file, float[,,] maps, (int Grass, int Earth, int Setts, int Cobbles) paving, StringBuilder report)
        {
            var size = maps.GetLength(0);
            var layers = maps.GetLength(2);
            var cell = VeilmouthScene.Extent / size;
            var laid = new List<string>();
            foreach (var road in (file.roads ?? Array.Empty<IslandLine>()).Where(r => r.kind == "main" || r.kind == "side"))
            {
                var half = road.kind == "main" ? 5f : 4f;
                var pts = road.line.Select(p => new Vector2(p.x, p.y)).ToArray();
                var x0 = Mathf.Max(0, Mathf.FloorToInt((pts.Min(p => p.x) - half) / cell));
                var x1 = Mathf.Min(size - 1, Mathf.CeilToInt((pts.Max(p => p.x) + half) / cell));
                var z0 = Mathf.Max(0, Mathf.FloorToInt((VeilmouthScene.Extent - pts.Max(p => p.y) - half) / cell));
                var z1 = Mathf.Min(size - 1, Mathf.CeilToInt((VeilmouthScene.Extent - pts.Min(p => p.y) + half) / cell));
                for (var z = z0; z <= z1; z++)
                {
                    for (var x = x0; x <= x1; x++)
                    {
                        var p = new Vector2((x + 0.5f) * cell, VeilmouthScene.Extent - ((z + 0.5f) * cell));
                        var near = float.MaxValue;
                        for (var k = 0; k + 1 < pts.Length; k++)
                        {
                            var ab = pts[k + 1] - pts[k];
                            var t = Mathf.Clamp01(Vector2.Dot(p - pts[k], ab) / ab.sqrMagnitude);
                            near = Mathf.Min(near, (p - pts[k] - (ab * t)).magnitude);
                        }

                        if (near > half || IslandGround(p) < IslandSea + 0.2f)
                        {
                            continue;
                        }

                        for (var l = 0; l < layers; l++)
                        {
                            maps[z, x, l] = l == paving.Setts ? 1f : 0f;
                        }
                    }
                }

                laid.Add($"{road.name} ({half * 2f:0} m)");
            }

            report.AppendLine("streets between districts, in setts: " + string.Join(", ", laid));
        }
    }
}
