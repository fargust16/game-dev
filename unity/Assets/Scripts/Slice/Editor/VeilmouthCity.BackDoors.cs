using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Kit;
using GameDev.VictorianKit.EditorTools;
using GameDev.World.Portals;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The houses' back doors (owner, fifth play test, 2026-11-09: a door that could not be entered, a neighbour's stone
    /// plinth before it). Every door seen opens: a back door gets steps down to the ground when it stands above it, and
    /// later its portal (<see cref="VeilmouthDwellings.AddDoors"/>); one whose way out meets a house, a plinth or a wall,
    /// or whose building keeps its one authored entrance, is walled up with the kit's plain wall.
    /// </summary>
    public static partial class VeilmouthCity
    {
        /// <summary>A walled-up back door keeps its marker under this name, so no portal is added to it.</summary>
        public const string BlindDoorMarker = "YardDoor (blind)";

        /// <summary>Authoring default, m: the free ground a back door needs beyond its steps.</summary>
        private const float BackDoorLanding = 0.8f;

        private static int _backDoorsOpen, _backDoorsBlind;

        /// <summary>After every house stands: each back door opened onto the ground, or walled up.</summary>
        private static void BackDoors()
        {
            _backDoorsOpen = _backDoorsBlind = 0;
            var entries = new Dictionary<GameObject, SliceKitCatalogue.Entry>();
            foreach (var e in _kit.Values.Where(e => e.Prefab != null))
            {
                entries[e.Prefab] = e;
            }

            var portals = _root.GetComponentsInChildren<Portal>(true).Select(p => p.transform.position).ToList();
            foreach (var marker in _root.GetComponentsInChildren<Transform>(true).Where(t => t.name == VeilmouthKitHouses.YardDoorMarker).ToList())
            {
                var house = marker.parent;
                if (portals.Any(p => Flat(p - marker.position) < 2.5f))
                {
                    continue; // the Soot & Bell's yard door, with its own portal
                }

                var street = house.Cast<Transform>().FirstOrDefault(t => t.name == VictorianHouses.DoorMarker);
                var own = street != null && portals.Any(p => Flat(p - street.position) < 2.5f);
                var source = PrefabUtility.GetCorrespondingObjectFromSource(house.gameObject);
                entries.TryGetValue(source != null ? source : house.gameObject, out var entry);
                var dwelling = street != null && VeilmouthDwellings.Classify(street, out _, out _);
                var outward = Plan(marker.position + marker.forward) - Plan(marker.position);
                var floor = house.position.y + (_sunk.Contains(house) ? Sink : 0f);
                if (own || !dwelling || entry == null || !OpensOnto(marker, outward, floor, entry.Stoop))
                {
                    Blind(house, marker);
                    _backDoorsBlind++;
                    continue;
                }

                Doorway(marker.position, outward, floor, entry.Stoop, VictorianHouses.StoopWidth);
                _backDoorsOpen++;
            }
        }

        /// <summary>
        /// Whether a back door's stoop, the steps down from it and a landing beyond stand clear of anything solid: no
        /// house, plinth, wall or other taken ground, and no ground over the door's floor.
        /// </summary>
        private static bool OpensOnto(Transform marker, Vector2 outward, float floor, float stoop)
        {
            var at = Plan(marker.position);
            var foot = at + (outward * stoop);
            var rise = floor - Ground(foot.x + (outward.x * 1.2f), foot.y + (outward.y * 1.2f));
            var steps = rise > VeilmouthKitHouses.StepRise * 0.5f ? Mathf.CeilToInt(rise / VeilmouthKitHouses.StepRise) : 0;
            var run = stoop + (steps * Mathf.Max(VeilmouthKitHouses.StepTread, 0.28f)) + BackDoorLanding;
            var side = new Vector2(-outward.y, outward.x) * (VictorianHouses.StoopWidth * 0.5f);
            for (var d = 0.4f; d <= run + 0.01f; d += 0.4f)
            {
                var c = at + (outward * d);
                if (Ground(c.x, c.y) > floor + 0.05f)
                {
                    return false;
                }

                var probe = new[] { c - side - (outward * 0.2f), c + side - (outward * 0.2f), c + side + (outward * 0.2f), c - side + (outward * 0.2f) };
                if (_taken.Any(t => t.Use == Use.Solid && Overlap(t.Quad, probe)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Walls up a back door: its wall module, frame, leaf, steps and rails hidden, the kit's plain wall in the module's
        /// place in the house's own paint, and the steps' colliders off.
        /// </summary>
        private static void Blind(Transform house, Transform marker)
        {
            marker.name = BlindDoorMarker;
            var model = house.Find("Model");
            if (model == null)
            {
                return;
            }

            Transform wall = null;
            foreach (Transform piece in model)
            {
                var n = piece.name;
                if (n.EndsWith(VeilmouthKitHouses.WalledUp) || !VeilmouthKitHouses.AtYardDoor(piece, new[] { marker.position }))
                {
                    continue;
                }

                if (n.StartsWith("SM_Vic_Wall_Door_"))
                {
                    wall = piece;
                }

                piece.gameObject.SetActive(false);
            }

            if (wall != null)
            {
                var plain = (GameObject)PrefabUtility.InstantiatePrefab(VicKitPaths.LoadModule("SM_Vic_Wall_Plain_2m"), model);
                plain.name = "SM_Vic_Wall_Plain_2m" + VeilmouthKitHouses.WalledUp;
                plain.transform.SetLocalPositionAndRotation(wall.localPosition, wall.localRotation);
                plain.transform.localScale = wall.localScale;
                var paint = wall.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault()?.sharedMaterials;
                foreach (var r in plain.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (paint != null && paint.Length == r.sharedMaterials.Length)
                    {
                        r.sharedMaterials = paint;
                    }
                }
            }

            foreach (Transform child in house)
            {
                if (child.name != "Stoop")
                {
                    continue;
                }

                var boxes = child.GetComponentsInChildren<BoxCollider>();
                var near = boxes.Where(box => Flat(box.transform.TransformPoint(box.center) - marker.position) < 2.5f).ToList();
                if (near.Count == boxes.Length)
                {
                    child.gameObject.SetActive(false); // this door's own steps: no block without a collider is left behind
                    continue;
                }

                near.ForEach(box => box.enabled = false);
            }
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
