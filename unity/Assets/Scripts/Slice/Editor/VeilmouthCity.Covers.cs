using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Iron covers set into the paving (VEILMOUTH_CITY_PLAN.md §46; SLICE_MODELS_WANTED.md, "Drainpipes, gutters, coal hole
    /// covers, drain grates"): ambientCG's cut-out covers as flat kit pieces, seen and never solid. A coal hole's plate of
    /// 0.45 m lies on the pavement beside two street doors in three, 1 m out from the door and 1.5 m to one side; a sewer's
    /// cover of 0.7 m lies on the middle of each carriageway every 30 m. Each lies where a ray from above meets the
    /// pavement or the road first, tilted to it and 1.5 cm over it; a ray that meets anything else first lays none. Every
    /// distance is an authoring default, in metres.
    /// </summary>
    public static partial class VeilmouthCity
    {
        private const string CoversName = "Covers";
        private const float CoverLift = 0.015f, CoalOut = 1f, CoalAside = 1.5f, CoalApart = 3f, SewerEvery = 30f, SewerApart = 12f;

        /// <summary>Lays the covers under <paramref name="root"/>, replacing older ones.</summary>
        private static void CoversOf(Transform root)
        {
            var old = root.Find(CoversName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            Physics.SyncTransforms();
            var owner = new GameObject(CoversName).transform;
            owner.SetParent(root, false);
            var ground = LayerMask.NameToLayer(KitLayers.Ground);
            var coal = new List<Vector3>();
            var sewer = new List<Vector3>();

            // Beside two doors in three, on their pavement: the door's own height or below, never on its steps.
            foreach (var door in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == VictorianHouses.DoorMarker))
            {
                var p = door.position;
                var hash = (((int)Mathf.Round(p.x * 10f) * 73856093) ^ ((int)Mathf.Round(p.z * 10f) * 19349663)) & 0x7fffffff;
                if (hash % 3 == 0)
                {
                    continue;
                }

                var side = hash % 2 == 0 ? 1f : -1f;
                var flat = Vector3.ProjectOnPlane(door.forward, Vector3.up).normalized;
                var at = p + (flat * CoalOut) + (Vector3.Cross(Vector3.up, flat) * (side * CoalAside));
                if (!Surface(at, p.y + 3f, ground, false, out var hit) || hit.point.y > p.y + 0.15f || hit.point.y < p.y - 1.5f ||
                    coal.Any(c => (c - hit.point).sqrMagnitude < CoalApart * CoalApart))
                {
                    continue;
                }

                Lay("CoalHoleCover", owner, hit.point, hit.normal, door.eulerAngles.y);
                coal.Add(hit.point);
            }

            // On the middle of each carriageway of 4 m or more.
            foreach (var s in _layout.streets)
            {
                var c0 = s.c0 + s.walk0;
                var c1 = s.c1 - s.walk1;
                if (c1 - c0 < 4f)
                {
                    continue;
                }

                var mid = (c0 + c1) / 2f;
                for (var along = s.a0 + (SewerEvery / 2f); along < s.a1 - 6f; along += SewerEvery)
                {
                    var (px, py) = s.AlongX ? (along, mid) : (mid, along);
                    var at = new Vector3(px - 500f, 0f, 500f - py);
                    if (!Surface(at, 200f, ground, true, out var hit) || sewer.Any(c => (c - hit.point).sqrMagnitude < SewerApart * SewerApart))
                    {
                        continue;
                    }

                    Lay("SewerCover", owner, hit.point, hit.normal, s.AlongX ? 90f : 0f);
                    sewer.Add(hit.point);
                }
            }

            Debug.Log($"[VeilmouthCity] covers (§46): {coal.Count} coal holes by the doors, {sewer.Count} sewers on the carriageways.");
        }

        /// <summary>
        /// The first surface a ray from <paramref name="top"/> straight down meets over <paramref name="at"/>: kept when it is
        /// the paving's (the Ground layer) or, for a road, the terrain, and level within 14°.
        /// </summary>
        private static bool Surface(Vector3 at, float top, int ground, bool terrain, out RaycastHit hit)
        {
            return Physics.Raycast(new Vector3(at.x, top, at.z), Vector3.down, out hit, top + 50f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                   hit.normal.y >= 0.97f && (hit.collider.gameObject.layer == ground || (terrain && hit.collider is TerrainCollider));
        }

        /// <summary>
        /// A flat kit piece laid on a point: its wrapper on the grid node and the 0.1 m step under it (the kit check), its
        /// model moved onto the point, tilted to the surface and turned by <paramref name="yaw"/>, casting no shadow.
        /// </summary>
        private static void Lay(string key, Transform parent, Vector3 point, Vector3 normal, float yaw)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(_kit[key].Prefab, parent);
            go.transform.position = new Vector3(Mathf.Round(point.x), Mathf.Floor(point.y * 10f) / 10f, Mathf.Round(point.z));
            var model = go.transform.Find("Model");
            model.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, yaw, 0f) * model.rotation;
            var renderers = model.GetComponentsInChildren<Renderer>();
            var seen = renderers.Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            model.position += point + (normal * CoverLift) - seen.center;
            foreach (var r in renderers)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(model);
        }
    }
}
