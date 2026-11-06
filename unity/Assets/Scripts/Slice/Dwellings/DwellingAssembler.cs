using System.Collections.Generic;
using System.Linq;
using GameDev.World.Portals;
using UnityEngine;

namespace GameDev.Slice.Dwellings
{
    /// <summary>
    /// Builds a <see cref="DwellingPlan"/> from the kit, by the rules the seven hand-built interiors follow (city plan
    /// C1 §14–§15): a floor and a ceiling per module cell, one wall block per segment with a seen face for a second room,
    /// a post at every node, a door leaf in the street doorway with the portal back, a ceiling collider for the camera,
    /// and one warm light per room.
    /// </summary>
    public static class DwellingAssembler
    {
        /// <summary>City plan C1 §15: BK's walls stretched to 3.6 m put the ceiling at 3.48 m.</summary>
        public const float CeilingHeight = 3.48f;

        /// <summary>The camera meets this layer and the Combat Grid does not read it (kit standard §6).</summary>
        public const string CeilingLayer = "Ignore Raycast";

        public const string RoomMarker = "Room - ";
        public const string StashName = "Stash";

        private static readonly Vector2Int N = new Vector2Int(0, 1), S = new Vector2Int(0, -1), E = new Vector2Int(1, 0), W = new Vector2Int(-1, 0);

        /// <summary>Builds the plan under <paramref name="parent"/> and returns the street door's portal, set to lead back.</summary>
        public static Portal Build(DwellingPlan plan, DwellingKit kit, Transform parent, DwellingKey key, string insidePortalId)
        {
            var rooms = Group(parent, "Rooms");
            var furniture = Group(parent, "Furniture");
            var portal = Rooms(plan, kit, rooms, key, insidePortalId);
            foreach (var f in plan.Furniture)
            {
                var go = f.Mode == PlacementMode.Hang
                    ? Hang(kit, furniture, f.Key, f.Facing, f.Line, f.Along, f.Y)
                    : Put(kit, furniture, f.Key, f.X, f.Z, f.Turns);
                if (go != null && f.Stash)
                {
                    go.name = StashName;
                }
            }

            return portal;
        }

        private static Portal Rooms(DwellingPlan plan, DwellingKit kit, Transform parent, DwellingKey key, string insidePortalId)
        {
            const int m = DwellingPlan.Module;
            var segments = new SortedSet<(int X, int Z, bool AlongZ)>();
            foreach (var r in plan.Rooms)
            {
                for (var cx = r.X; cx < r.X + r.Width; cx += m)
                {
                    for (var cz = r.Z; cz < r.Z + r.Depth; cz += m)
                    {
                        Put(kit, parent, r.Stone ? "BkStoneFloor" : "BkFloor", cx, cz);
                        Put(kit, parent, "BkCeiling", cx, cz, 0, CeilingHeight);
                    }

                    segments.Add((cx, r.Z, false));
                    segments.Add((cx, r.Z + r.Depth, false));
                }

                for (var cz = r.Z; cz < r.Z + r.Depth; cz += m)
                {
                    segments.Add((r.X, cz, true));
                    segments.Add((r.X + r.Width, cz, true));
                }

                var marker = new GameObject(RoomMarker + r.Name).transform;
                marker.SetParent(parent, false);
                marker.localPosition = new Vector3(r.X + r.Width * 0.5f, 0f, r.Z + r.Depth * 0.5f);
                marker.localScale = new Vector3(r.Width, CeilingHeight, r.Depth);

                var ceiling = new GameObject("Ceiling collider - " + r.Name);
                ceiling.transform.SetParent(parent, false);
                ceiling.layer = LayerMask.NameToLayer(CeilingLayer);
                var box = ceiling.AddComponent<BoxCollider>();
                box.center = new Vector3(r.X + r.Width * 0.5f, CeilingHeight + 0.15f, r.Z + r.Depth * 0.5f);
                box.size = new Vector3(r.Width, 0.3f, r.Depth);

                var light = new GameObject("Light - " + r.Name).AddComponent<Light>();
                light.transform.SetParent(parent, false);
                light.transform.localPosition = new Vector3(r.X + r.Width * 0.5f, CeilingHeight - 0.4f, r.Z + r.Depth * 0.5f);
                light.type = LightType.Point;
                light.color = new Color(1f, 0.82f, 0.6f);
                light.range = Mathf.Max(r.Width, r.Depth) * 1.1f;
                light.intensity = 2.5f;
                light.shadows = LightShadows.None;
            }

            Portal portal = null;
            var nodes = new HashSet<(int, int)>();
            var face = 0;
            foreach (var (x, z, alongZ) in segments)
            {
                var kind = plan.OpeningAt(x, z, alongZ);
                var brickB = face++ % 3 == 2;
                string Plain(bool stone) => stone ? brickB ? "BkBrickB" : "BkBrick" : "BkPlaster";
                string Doorway(bool stone) => stone ? "BkDoorway" : "BkPlasterDoorway";

                // BK draws a wall on its local +Z side only: the solid block faces the first room, a second room gets a
                // seen face, and each side takes its own room's family.
                var sides = Sides(plan, x, z, alongZ);
                GameObject Wall(System.Func<bool, string> family)
                {
                    var solid = Segment(kit, parent, family(sides[0].Stone), x, z, alongZ, sides[0].Facing);
                    if (sides.Count > 1)
                    {
                        Segment(kit, parent, family(sides[1].Stone) + "Face", x, z, alongZ, sides[1].Facing);
                    }

                    return solid;
                }

                switch (kind)
                {
                    case Opening.Wall:
                        Wall(Plain);
                        break;
                    case Opening.Window:
                        Segment(kit, parent, "BkWindow", x, z, alongZ, sides[0].Facing);
                        break;
                    case Opening.Door:
                        Wall(Doorway);
                        break;
                    case Opening.Street:
                        var doorway = Segment(kit, parent, Doorway(sides[0].Stone), x, z, alongZ, sides[0].Facing);
                        var leaf = Segment(kit, parent, "BkDoor", x, z, alongZ, sides[0].Facing);
                        if (doorway != null && leaf != null)
                        {
                            leaf.transform.SetParent(doorway.transform, true); // the shut leaf, nested in its doorway
                        }

                        var door = Put(kit, parent, "PortalLeafRoom", x, z, alongZ ? 3 : 0);
                        portal = door != null ? door.GetComponent<Portal>() : null;
                        if (portal != null)
                        {
                            portal.Configure(insidePortalId, key.StreetScene, key.StreetPortalId, true);
                            portal.gameObject.name = $"Portal - {insidePortalId}";
                        }

                        break;
                }

                nodes.Add((x, z));
                nodes.Add(alongZ ? (x, z + m) : (x + m, z));
            }

            foreach (var (x, z) in nodes)
            {
                Put(kit, parent, "BkPost", x, z);
            }

            return portal;
        }

        /// <summary>The rooms beside a wall segment: the direction from the wall into each, and whether it is stone.</summary>
        private static List<(Vector2Int Facing, bool Stone)> Sides(DwellingPlan plan, int x, int z, bool alongZ)
        {
            var sides = new List<(Vector2Int, bool)>();
            void Look(Vector2Int facing, float px, float pz)
            {
                foreach (var r in plan.Rooms.Where(r => r.Contains(px, pz)))
                {
                    sides.Add((facing, r.Stone));
                }
            }

            var mid = DwellingPlan.Module * 0.5f;
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

        /// <summary>A linear block on the segment of one module from (x, z), its local +Z turned to <paramref name="facing"/>.</summary>
        private static GameObject Segment(DwellingKit kit, Transform parent, string key, int x, int z, bool alongZ, Vector2Int facing)
        {
            const int m = DwellingPlan.Module;
            var pivot = facing == S ? new Vector3(x + m, 0f, z) : facing == E ? new Vector3(x, 0f, z + m) : new Vector3(x, 0f, z);
            return At(kit, parent, key, pivot, Quaternion.Euler(0f, 90f * DwellingPlanner.Turns(facing), 0f));
        }

        /// <summary>A Face block hung on the wall line, facing into the room, spanning [a, a + width] along it.</summary>
        private static GameObject Hang(DwellingKit kit, Transform parent, string key, Vector2Int facing, int line, int a, float y)
        {
            var block = kit.Get(key);
            if (block == null)
            {
                return null;
            }

            Vector3 pivot;
            if (facing == N) pivot = new Vector3(a, y, line);
            else if (facing == S) pivot = new Vector3(a + block.width, y, line);
            else if (facing == E) pivot = new Vector3(line, y, a + block.width);
            else pivot = new Vector3(line, y, a);
            return At(kit, parent, key, pivot, Quaternion.Euler(0f, 90f * DwellingPlanner.Turns(facing), 0f));
        }

        /// <summary>A block whose turned footprint's south-west corner is (x, z); a linear or node block starts at (x, z).</summary>
        private static GameObject Put(DwellingKit kit, Transform parent, string key, int x, int z, int turns = 0, float y = 0f)
        {
            var block = kit.Get(key);
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

            return At(kit, parent, key, pivot, rotation);
        }

        private static GameObject At(DwellingKit kit, Transform parent, string key, Vector3 pivot, Quaternion rotation)
        {
            var block = kit.Get(key);
            if (block == null || block.prefab == null)
            {
                Debug.LogError($"[Dwelling] the kit has no '{key}'.");
                return null;
            }

            var go = Object.Instantiate(block.prefab, parent);
            go.name = block.prefab.name;
            go.transform.localPosition = new Vector3(Mathf.Round(pivot.x), Mathf.Round(pivot.y * 10f) / 10f, Mathf.Round(pivot.z));
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
