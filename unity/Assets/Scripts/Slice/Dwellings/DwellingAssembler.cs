using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Interiors;
using GameDev.Slice.Kit;
using GameDev.World.Portals;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.Slice.Dwellings
{
    /// <summary>
    /// Builds a <see cref="DwellingPlan"/> from the kit by the rules the seven interiors follow since VI-2 and VI-3
    /// (VICTORIAN_INTERIORS_PLAN §3–§5, §8; `VeilmouthInteriors`): storey by storey, a floor and a ceiling per module
    /// cell with a camera box under each ceiling, one wall per segment in the main room's finish, a stair well left open,
    /// a shut leaf in the street doorway with the portal back, curtains and a lit pane with its daylight in each window,
    /// fire in each fireplace, and the lights handed to <see cref="InteriorLighting"/>, which follows the clock.
    /// </summary>
    public static class DwellingAssembler
    {
        public const int Module = DwellingPlan.Module;
        public const float Storey = DwellingPlan.Storey;

        /// <summary>The ceiling's underside: the pack's ceiling is 0.23 m thick under the storey's height.</summary>
        public const float CeilingHeight = Storey - 0.23f;

        /// <summary>The camera meets this layer and the Combat Grid does not read it (kit standard §6).</summary>
        public const string CeilingLayer = "Ignore Raycast";

        public const string RoomMarker = "Room - ";
        public const string StashName = "Stash";

        /// <summary>The pack's lamps were lit for baked light; real-time each burns at 2.5 at least, reaching 6–9 m (plan §5).</summary>
        private const float LampIntensity = 2.5f;

        private const string NoGridLayer = "NoGrid";

        private static readonly Vector2Int N = new Vector2Int(0, 1), S = new Vector2Int(0, -1), E = new Vector2Int(1, 0), W = new Vector2Int(-1, 0);

        /// <summary>Each finish's plain wall, doorway and window wall; panelling has neither, and takes the dark ones.</summary>
        private static readonly Dictionary<Finish, (string Plain, string Doorway, string Window)> Finishes = new Dictionary<Finish, (string, string, string)>
        {
            [Finish.Paper] = ("ViPaper", "ViPaperDoorway", "ViPaperWindow"),
            [Finish.Dark] = ("ViDark", "ViDarkDoorway", "ViDarkWindow"),
            [Finish.Panel] = ("ViPanel", "ViDarkDoorway", "ViDarkWindow"),
        };

        /// <summary>Every architectural key the assembler places, so the kit asset holds them all.</summary>
        public static readonly string[] Keys =
        {
            "ViFloor", "ViCeiling", "ViPaper", "ViPaperDoorway", "ViPaperWindow", "ViDark", "ViDarkDoorway", "ViDarkWindow", "ViPanel",
            "ViDoor", "ViCurtain", "PortalLeaf", NewelKey,
        };

        /// <summary>The pack's newel post at a corner of two balustrades (owner, VI-8 remark 4).</summary>
        public const string NewelKey = "ViNewel";

        private sealed class Job
        {
            public DwellingPlan Plan;
            public DwellingKit Kit;
            public Transform Parent;
            public float FloorY;
            public readonly List<Light> Windows = new List<Light>();
            public readonly List<Renderer> Panes = new List<Renderer>();
            public readonly List<InteriorLighting.Hearth> Hearths = new List<InteriorLighting.Hearth>();
        }

        /// <summary>Builds the plan under <paramref name="parent"/> and returns the street door's portal, set to lead back.</summary>
        public static Portal Build(DwellingPlan plan, DwellingKit kit, Transform parent, DwellingKey key, string insidePortalId)
        {
            var b = new Job { Plan = plan, Kit = kit, Parent = Group(parent, "Rooms") };
            var portal = Rooms(b, key, insidePortalId);
            b.Parent = Group(parent, "Furniture");
            foreach (var f in plan.Furniture)
            {
                b.FloorY = f.Level * Storey;
                var go = f.Mode == PlacementMode.Hang ? Hang(b, f.Key, f.Facing, f.Line, f.Along, f.Y) : Put(b, f.Key, f.X, f.Z, f.Turns, f.Y);
                if (go == null)
                {
                    continue;
                }

                if (f.Stash)
                {
                    go.name = StashName;
                }
                else if (f.Name != null)
                {
                    go.name = f.Name;
                }

                if (f.Key == DwellingPlanner.FireplaceKey)
                {
                    Fireplace(b, go, f);
                }
                else if (f.Key == DwellingPlanner.ChandelierKey)
                {
                    Chandelier(go);
                }
            }

            b.FloorY = 0f;
            var newel = kit.Get(NewelKey);
            if (newel?.prefab != null)
            {
                foreach (var (node, rotation) in NewelSpots(parent))
                {
                    var post = Object.Instantiate(newel.prefab, b.Parent);
                    post.name = "Newel";
                    post.transform.SetPositionAndRotation(node, rotation);
                }
            }

            NestOnStairs(parent);
            Lighting(b, parent);
            return portal;
        }

        // ------------------------------------------------------------------------------------------- rooms

        private static Portal Rooms(Job b, DwellingKey key, string insidePortalId)
        {
            var plan = b.Plan;
            var segments = new HashSet<(int X, int Z, bool AlongZ, int Level)>();
            foreach (var r in plan.Rooms)
            {
                b.FloorY = r.Level * Storey;
                var ceiling = new GameObject("Ceiling collider - " + r.Name);
                ceiling.transform.SetParent(b.Parent, false);
                ceiling.layer = LayerMask.NameToLayer(CeilingLayer);
                for (var cx = r.X; cx < r.X + r.Width; cx += Module)
                {
                    for (var cz = r.Z; cz < r.Z + r.Depth; cz += Module)
                    {
                        if (!plan.InWell(cx, cz, r.Level))
                        {
                            At(b, "ViFloor", new Vector3(cx, 0f, cz), Quaternion.identity);
                        }

                        if (plan.InWell(cx, cz, r.Level + 1))
                        {
                            continue;
                        }

                        At(b, "ViCeiling", new Vector3(cx, Storey, cz), Quaternion.identity);
                        // Under the next floor's slab (3.4 m), so the camera stays in the room and nobody upstairs stands
                        // on it: 3.14–3.40 m, one box per module so a stair well stays open (VI-3).
                        var box = ceiling.AddComponent<BoxCollider>();
                        box.center = new Vector3(cx + Module * 0.5f, b.FloorY + CeilingHeight, cz + Module * 0.5f);
                        box.size = new Vector3(Module, 0.26f, Module);
                    }

                    segments.Add((cx, r.Z, false, r.Level));
                    segments.Add((cx, r.Z + r.Depth, false, r.Level));
                }

                for (var cz = r.Z; cz < r.Z + r.Depth; cz += Module)
                {
                    segments.Add((r.X, cz, true, r.Level));
                    segments.Add((r.X + r.Width, cz, true, r.Level));
                }

                var marker = new GameObject(RoomMarker + r.Name).transform;
                marker.SetParent(b.Parent, false);
                marker.localPosition = new Vector3(r.X + r.Width * 0.5f, b.FloorY, r.Z + r.Depth * 0.5f);
                marker.localScale = new Vector3(r.Width, Storey, r.Depth);
            }

            Portal portal = null;
            foreach (var (x, z, alongZ, level) in segments.OrderBy(s => s.Level).ThenBy(s => s.X).ThenBy(s => s.Z).ThenBy(s => s.AlongZ))
            {
                b.FloorY = level * Storey;
                var sides = Sides(plan, x, z, alongZ, level);
                // The main room is the one listed first: its finish takes the wall, which the pack draws on both sides.
                var main = sides.OrderBy(s => s.Index).First();
                var (plain, doorway, window) = Finishes[main.Finish];
                switch (plan.OpeningAt(x, z, alongZ, level))
                {
                    case Opening.Wall:
                        Segment(b, plain, x, z, alongZ, main.Facing);
                        break;
                    case Opening.Door:
                        Segment(b, doorway, x, z, alongZ, main.Facing);
                        break;
                    case Opening.Street:
                        var door = Segment(b, doorway, x, z, alongZ, main.Facing);
                        var leaf = Segment(b, "ViDoor", x, z, alongZ, main.Facing);
                        if (door != null && leaf != null)
                        {
                            leaf.transform.SetParent(door.transform, true); // the shut leaf, nested in its doorway
                            leaf.name = "Street door";
                        }

                        var portalLeaf = Put(b, "PortalLeaf", x, z, alongZ ? 3 : 0);
                        portal = portalLeaf != null ? portalLeaf.GetComponent<Portal>() : null;
                        if (portal != null)
                        {
                            portal.Configure(insidePortalId, key.StreetScene, key.StreetPortalId, true);
                            portal.gameObject.name = $"Portal - {insidePortalId}";
                        }

                        break;
                    case Opening.Window:
                        if (sides.Count > 1)
                        {
                            Debug.LogError($"[Dwelling] a window at ({x}, {z}) on storey {level} stands between two rooms; the plan is wrong.");
                        }

                        Segment(b, window, x, z, alongZ, main.Facing);
                        Daylight(b, window, x, z, alongZ, main.Facing);
                        Hang(b, "ViCurtain", main.Facing, alongZ ? x : z, alongZ ? z : x, 0.2f);
                        break;
                }
            }

            b.FloorY = 0f;
            return portal;
        }

        /// <summary>The rooms beside a wall segment on its storey: the direction from the wall into each, its finish, its place in the plan.</summary>
        private static List<(Vector2Int Facing, Finish Finish, int Index)> Sides(DwellingPlan plan, int x, int z, bool alongZ, int level)
        {
            var sides = new List<(Vector2Int, Finish, int)>();
            void Look(Vector2Int facing, float px, float pz)
            {
                for (var i = 0; i < plan.Rooms.Count; i++)
                {
                    var r = plan.Rooms[i];
                    if (r.Level == level && r.Contains(px, pz))
                    {
                        sides.Add((facing, r.Finish, i));
                    }
                }
            }

            var mid = Module * 0.5f;
            if (alongZ)
            {
                Look(E, x + 0.5f, z + mid);
                Look(W, x - 0.5f, z + mid);
            }
            else
            {
                Look(N, x + mid, z + 0.5f);
                Look(S, x + mid, z - 0.5f);
            }

            return sides;
        }

        /// <summary>
        /// A window's daylight (plan §5): a lit pane of our own unlit material on its glass, as the editor measured it, and
        /// a spot just inside aimed down into the room; <see cref="InteriorLighting"/> sets both from the hour.
        /// </summary>
        private static void Daylight(Job b, string key, int x, int z, bool alongZ, Vector2Int facing)
        {
            var g = b.Kit.GlassOf(key);
            var (u0, u1, y0, y1, depth) = g != null ? (g.u0, g.u1, g.y0, g.y1, g.depth) : (-0.6f, 0.6f, 1.0f, 2.6f, 0.08f);
            var f = new Vector3(facing.x, 0f, facing.y);
            var u = alongZ ? Vector3.forward : Vector3.right;
            var centre = b.Parent.TransformPoint(new Vector3(x, b.FloorY, z) + u * (Module * 0.5f));

            var pane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var probe = pane.GetComponent<Collider>();
            if (Application.isPlaying) Object.Destroy(probe);
            else Object.DestroyImmediate(probe);
            pane.name = "Window pane";
            pane.layer = LayerMask.NameToLayer(NoGridLayer);
            pane.transform.SetParent(b.Parent, false);
            pane.transform.position = centre + u * ((u0 + u1) * 0.5f) + Vector3.up * ((y0 + y1) * 0.5f) + f * (depth + 0.005f);
            pane.transform.rotation = Quaternion.LookRotation(-f); // a quad shows its face to its local -Z: into the room
            pane.transform.localScale = new Vector3(u1 - u0, y1 - y0, 1f);
            var renderer = pane.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = b.Kit.Pane;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            b.Panes.Add(renderer);

            var spot = new GameObject("Window light").AddComponent<Light>();
            spot.transform.SetParent(b.Parent, false);
            spot.transform.position = centre + Vector3.up * (y1 - 0.1f) + f * 0.5f;
            spot.transform.rotation = Quaternion.LookRotation((f * 0.75f) + (Vector3.down * 0.66f)); // about 41° down
            spot.type = LightType.Spot;
            spot.range = 10f;
            spot.spotAngle = 95f;
            spot.innerSpotAngle = 55f;
            spot.shadows = LightShadows.None;
            b.Windows.Add(spot);
        }

        // ------------------------------------------------------------------------------------------- fire and lamps

        /// <summary>
        /// Vefects' small fire (asset map: Fire) in a mantel's firebox, at <see cref="FireScale"/> so its flames stay
        /// inside the opening (measured: 1.27 x 1.23 m, 0.37 m deep, its middle 0.52 m out from the wall's line). Its own
        /// particle lights are switched off: the hearth's one light, lit and put out with the flames by
        /// <see cref="InteriorLighting"/>, is the fire's light (owner, VI-8 remark 6). Shared by the Dwellings and the seven.
        /// </summary>
        public static ParticleSystem PlaceFire(GameObject prefab, Transform hearth)
        {
            var fire = Object.Instantiate(prefab, hearth);
            fire.name = "Fire";
            fire.transform.localPosition = new Vector3(Module * 0.5f, FireSink, 0.52f);
            fire.transform.localRotation = Quaternion.identity;
            // Its systems scale by the hierarchy and simulate locally, so the root's scale shapes the whole fire: wider
            // than tall, a hearth fire rather than a bonfire (owner, VI-8 follow-up).
            fire.transform.localScale = FireShape * FireScale;
            foreach (var t in fire.GetComponentsInChildren<Transform>(true))
            {
                // Its own particle lights: the hearth's one light is the fire's. Its heat haze drew rings round the flames.
                if (t.name == "Lights" || t.name == "Distortion")
                {
                    t.gameObject.SetActive(false);
                }
            }

            return fire.GetComponent<ParticleSystem>();
        }

        /// <summary>Authoring default: the small fire's scale in a firebox 1.27 m wide.</summary>
        public const float FireScale = 0.85f;

        /// <summary>Authoring default: the fire's shape, 1.35 as wide and 0.55 as high as the pack's (owner, VI-8 follow-up).</summary>
        public static readonly Vector3 FireShape = new Vector3(1.35f, 0.55f, 1f);

        /// <summary>Authoring default, m: the emitter's height over the hearth, sunk among the logs so flames rise from them.</summary>
        public const float FireSink = -0.06f;

        /// <summary>
        /// The pack's fire in the mantel's firebox (measured: its middle 0.52 m out from the wall's line) and a warm,
        /// flickering light; a kitchen hearth, and the hearth of a poor family's room, burns all day (plan §4.2, §5).
        /// </summary>
        private static void Fireplace(Job b, GameObject block, Placement f)
        {
            var room = b.Plan.Rooms.FirstOrDefault(r => r.Level == f.Level && r.Contains(f.X + 0.5f, f.Z + 0.5f));
            var allDay = room.Kind == RoomKind.Kitchen || room.Kind == RoomKind.Room || room.Kind == RoomKind.BackRoom;
            block.name = allDay ? "Kitchen hearth" : "Fireplace";
            var flames = b.Kit.Fire != null ? PlaceFire(b.Kit.Fire, block.transform) : null;

            var light = new GameObject("Fire light").AddComponent<Light>();
            light.transform.SetParent(block.transform, false);
            light.transform.localPosition = new Vector3(Module * 0.5f, 0.6f, 1.1f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.25f);
            light.range = 7f;
            light.intensity = 2.5f;
            light.shadows = LightShadows.None;
            b.Hearths.Add(new InteriorLighting.Hearth { Light = light, Flames = flames, AllDay = allDay });
        }

        /// <summary>The pack's chandelier is only emissive, so it carries a warm light of our own.</summary>
        private static void Chandelier(GameObject block)
        {
            var light = new GameObject("Chandelier light").AddComponent<Light>();
            light.transform.SetParent(block.transform, false);
            light.transform.localPosition = new Vector3(1f, -0.9f, 1f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.8f, 0.55f);
            light.range = 9f;
            light.intensity = 3f;
            light.shadows = LightShadows.None;
        }

        /// <summary>The dwelling's lights to <see cref="InteriorLighting"/>, set at once for the hour it shows.</summary>
        private static void Lighting(Job b, Transform root)
        {
            var hearthLights = new HashSet<Light>(b.Hearths.Select(h => h.Light));
            var lamps = b.Parent.GetComponentsInChildren<Light>(true).Where(l => !hearthLights.Contains(l)).ToArray();
            foreach (var lamp in lamps)
            {
                lamp.intensity = Mathf.Max(lamp.intensity, LampIntensity);
                lamp.range = Mathf.Clamp(lamp.range, 6f, 9f);
                lamp.shadows = LightShadows.None;
            }

            var lighting = new GameObject("Lighting").AddComponent<InteriorLighting>();
            lighting.transform.SetParent(root, false);
            lighting.Configure(b.Windows.ToArray(), b.Panes.ToArray(), lamps, b.Hearths.ToArray(), b.Windows.Count == 0);
            lighting.Apply(lighting.PreviewMinute);
        }

        // ------------------------------------------------------------------------------------------- stairs

        /// <summary>Measured (StairLab): a balustrade's handrails run on this line, in from the well's edge, m.</summary>
        public const float RailLine = 0.0445f;

        /// <summary>
        /// Where the pack's newel post stands, as in its own demo scene (StairLab): at each corner where two balustrades
        /// meet, and at each free end of a balustrade, whose open section it closes (owner, VI-8 follow-up). A balustrade's
        /// edge line is its footprint's front, on the well's edge, from its local x 0 to 3; its local +Z is toward the well.
        /// The post's pivot is the corner or the end, a grid node, turned so that its post, which stands off the pivot
        /// toward its local -X and -Z by <see cref="RailLine"/>, stands on both handrails' line: into the floor from both
        /// edges at a corner, into the floor and back along the rail at an end. Two rails in a row need none, and an end
        /// buried in a wall needs none. Read from the balustrades' transforms, so it holds for any plan.
        /// </summary>
        public static List<(Vector3 Node, Quaternion Rotation)> NewelSpots(Transform root)
        {
            Physics.SyncTransforms();
            var ends = new List<(Vector3 At, Vector3 Out, Vector3 Well, KitBlock Rail)>();
            foreach (var rail in root.GetComponentsInChildren<KitBlock>().Where(k => k.name == "Balustrade"))
            {
                var t = rail.transform;
                ends.Add((t.TransformPoint(0f, 0f, 1f), -t.right, t.forward, rail));
                ends.Add((t.TransformPoint(Module, 0f, 1f), t.right, t.forward, rail));
            }

            var spots = new List<(Vector3 Node, Quaternion Rotation)>();
            void Add(Vector3 node, Vector3 d)
            {
                // The post stands at node - RailLine × R(1, 0, 1); Euler y of k × 90° turns (1, 1) to (1, 1), (1, -1), (-1, -1), (-1, 1).
                var turns = d.x > 0f ? (d.z > 0f ? 0 : 1) : (d.z > 0f ? 3 : 2);
                node = new Vector3(Mathf.Round(node.x), node.y, Mathf.Round(node.z));
                if (!spots.Any(s => (s.Node - node).sqrMagnitude < 0.01f))
                {
                    spots.Add((node, Quaternion.Euler(0f, 90f * turns, 0f)));
                }
            }

            foreach (var e in ends)
            {
                var others = ends.Where(o => o.Rail != e.Rail && (o.At - e.At).sqrMagnitude < 0.0025f).ToList();
                var corner = others.FirstOrDefault(o => Mathf.Abs(Vector3.Dot(o.Out, e.Out)) < 0.1f);
                if (corner.Rail != null)
                {
                    Add(e.At, e.Well + corner.Well);
                }
                else if (others.Count == 0 && !Buried(e.At - (e.Well * RailLine) - (e.Out * 0.05f) + (Vector3.up * 1f), root))
                {
                    Add(e.At, e.Well + e.Out);
                }
            }

            return spots;
        }

        // A point of the handrail just short of its end inside a wall's body: the wall closes the end.
        private static bool Buried(Vector3 point, Transform root) =>
            Physics.OverlapSphere(point, 0.02f, LayerMask.GetMask(KitLayers.Structure), QueryTriggerInteraction.Ignore)
                .Any(c => c.transform.IsChildOf(root) && !c.GetComponentsInParent<KitBlock>().Any(p => p.name is "Balustrade" or "Newel" or "Stair"));

        /// <summary>
        /// A stair's solid bounds are its whole slope, so the floor tiles about its foot and its head and the balustrade at
        /// its well's low end stand inside them though they touch nothing of it. They are nested under the stair as its
        /// own pieces (kit standard §6), as in the seven (VI-3).
        /// </summary>
        private static void NestOnStairs(Transform root)
        {
            Physics.SyncTransforms();
            Bounds? Solid(Component block)
            {
                Bounds? bounds = null;
                foreach (var c in block.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger && c.GetComponentInParent<KitBlock>() == block))
                {
                    if (bounds.HasValue)
                    {
                        var u = bounds.Value;
                        u.Encapsulate(c.bounds);
                        bounds = u;
                    }
                    else
                    {
                        bounds = c.bounds;
                    }
                }

                return bounds;
            }

            var blocks = root.GetComponentsInChildren<KitBlock>();
            foreach (var stair in blocks.Where(k => k.name == "Stair"))
            {
                var bounds = Solid(stair);
                if (!bounds.HasValue)
                {
                    continue;
                }

                var inside = bounds.Value;
                inside.Expand(-0.02f);
                foreach (var piece in blocks.Where(k => k != stair && (k.name.StartsWith("Kit_ViRoom_Floor") || k.name == "Balustrade" || k.name == "Newel")))
                {
                    var other = Solid(piece);
                    if (other.HasValue && inside.Intersects(other.Value) && piece.transform.parent.GetComponentInParent<KitBlock>() == null)
                    {
                        piece.transform.SetParent(stair.transform, true);
                    }
                }
            }
        }

        // ------------------------------------------------------------------------------------------- placing

        /// <summary>A linear block on the segment of one module from (x, z), its local +Z turned to <paramref name="facing"/>.</summary>
        private static GameObject Segment(Job b, string key, int x, int z, bool alongZ, Vector2Int facing)
        {
            var pivot = facing == S ? new Vector3(x + Module, 0f, z) : facing == E ? new Vector3(x, 0f, z + Module) : new Vector3(x, 0f, z);
            return At(b, key, pivot, Quaternion.Euler(0f, 90f * DwellingPlanner.Turns(facing), 0f));
        }

        /// <summary>A hung block on the wall line, facing into the room, on the 3 m segment that holds <paramref name="a"/>.</summary>
        private static GameObject Hang(Job b, string key, Vector2Int facing, int line, int a, float y)
        {
            var block = b.Kit.Get(key);
            if (block == null)
            {
                Debug.LogError($"[Dwelling] the kit has no '{key}'.");
                return null;
            }

            a = (a / Module * Module) + ((Module - block.width) / 2);
            Vector3 pivot;
            if (facing == N) pivot = new Vector3(a, y, line);
            else if (facing == S) pivot = new Vector3(a + block.width, y, line);
            else if (facing == E) pivot = new Vector3(line, y, a + block.width);
            else pivot = new Vector3(line, y, a);
            return At(b, key, pivot, Quaternion.Euler(0f, 90f * DwellingPlanner.Turns(facing), 0f));
        }

        /// <summary>A block whose turned footprint's south-west corner is (x, z); a linear or node block starts at (x, z).</summary>
        private static GameObject Put(Job b, string key, int x, int z, int turns = 0, float y = 0f)
        {
            var block = b.Kit.Get(key);
            if (block == null)
            {
                Debug.LogError($"[Dwelling] the kit has no '{key}'.");
                return null;
            }

            var rotation = Quaternion.Euler(0f, 90f * turns, 0f);
            var pivot = new Vector3(x, y, z);
            if (block.depth > 0)
            {
                var corners = new[] { Vector3.zero, new Vector3(block.width, 0f, 0f), new Vector3(0f, 0f, block.depth), new Vector3(block.width, 0f, block.depth) }
                    .Select(c => rotation * c).ToArray();
                pivot -= new Vector3(corners.Min(c => c.x), 0f, corners.Min(c => c.z));
            }

            return At(b, key, pivot, rotation);
        }

        private static GameObject At(Job b, string key, Vector3 pivot, Quaternion rotation)
        {
            var block = b.Kit.Get(key);
            if (block == null || block.prefab == null)
            {
                Debug.LogError($"[Dwelling] the kit has no '{key}'.");
                return null;
            }

            var go = Object.Instantiate(block.prefab, b.Parent);
            go.name = block.prefab.name;
            go.transform.localPosition = new Vector3(Mathf.Round(pivot.x), Mathf.Round((pivot.y + b.FloorY) * 10f) / 10f, Mathf.Round(pivot.z));
            go.transform.localRotation = rotation;
            return go;
        }

        private static Transform Group(Transform root, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            return t;
        }
    }
}
