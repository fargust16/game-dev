using System.Collections.Generic;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    public enum KitFaultKind
    {
        OffGrid,
        WrongLayer,
        MissingCollider,
        ColliderOnNoGrid,
        Overlap
    }

    public readonly struct KitFault
    {
        public readonly KitFaultKind Kind;
        public readonly KitBlock Block;
        public readonly KitBlock Other;
        public readonly string Detail;

        public KitFault(KitFaultKind kind, KitBlock block, string detail, KitBlock other = null)
        {
            Kind = kind;
            Block = block;
            Detail = detail;
            Other = other;
        }

        public override string ToString()
        {
            var other = Other != null ? $" and {Label(Other)}" : string.Empty;
            return $"{Kind}: {Label(Block)}{other}: {Detail}";
        }

        /// <summary>A nested block is named with its owner, and every block with where it stands, so a fault can be found.</summary>
        private static string Label(KitBlock block)
        {
            var owner = block.transform.parent != null ? block.transform.parent.GetComponentInParent<KitBlock>() : null;
            var name = owner != null ? $"{owner.name}/{block.name}" : block.name;
            var p = block.transform.position;
            return $"'{name}' at ({p.x:0.##}, {p.y:0.##}, {p.z:0.##})";
        }
    }

    /// <summary>
    /// Checks hand-placed kit blocks against SLICE_MODULAR_KIT_STANDARD: the 1 m grid and 90° turns (§1–§2), the kit's
    /// layers (§5), colliders (§4), and the non-overlap law (§6): solid bounds may touch, never overlap. A block under a
    /// <see cref="KitFrame"/> keeps the grid and the turns in the frame's axes, and the frame stands on a world grid
    /// node at a multiple of 45° (C1 §12, 2026-11-05); a turned block's overlap is measured on its colliders, not on
    /// its world bounds.
    /// It reports and never moves a block.
    /// </summary>
    public static class KitLayoutCheck
    {
        /// <summary>Authoring default, m: placement error still read as on the grid.</summary>
        public const float PlacementTolerance = 0.001f;

        /// <summary>Authoring default, m: penetration still read as touching.</summary>
        public const float OverlapTolerance = 0.01f;

        /// <summary>Kit standard §2: surfaces sit at whole 0.1 m heights.</summary>
        public const float HeightStep = 0.1f;

        private const float AngleToleranceDegrees = 0.01f;

        [MenuItem("GameDev/Slice/Check Kit Layout")]
        private static void CheckOpenScenes()
        {
            var blocks = Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude);
            var faults = Check(blocks);
            foreach (var fault in faults)
            {
                Debug.LogWarning(fault.ToString(), fault.Block);
            }

            Debug.Log($"Kit layout check: {blocks.Length} blocks, {faults.Count} faults.");
        }

        public static List<KitFault> Check(IReadOnlyList<KitBlock> blocks)
        {
            Physics.SyncTransforms();
            var faults = new List<KitFault>();
            var solids = new List<(KitBlock block, Bounds bounds)>();

            foreach (var block in blocks)
            {
                // A turned-off block is dressing: the rooms behind a kit house's windows turn their blocks off (VicRooms),
                // since nobody enters them and nothing collides there.
                if (block == null || !block.enabled)
                {
                    continue;
                }

                CheckPlacement(block, faults);

                var layer = LayerMask.LayerToName(block.gameObject.layer);
                if (!KitLayers.IsBlockLayer(layer))
                {
                    faults.Add(new KitFault(KitFaultKind.WrongLayer, block, $"layer '{layer}' is not a kit block layer"));
                }

                var solid = TryGetSolidBounds(block, out var bounds);
                if (solid)
                {
                    solids.Add((block, bounds));
                }

                if (layer == KitLayers.NoGrid && solid)
                {
                    faults.Add(new KitFault(KitFaultKind.ColliderOnNoGrid, block, "a NoGrid block must have no solid collider"));
                }
                else if (KitLayers.RequiresCollider(layer) && !solid)
                {
                    faults.Add(new KitFault(KitFaultKind.MissingCollider, block, $"a {layer} block needs a solid collider"));
                }
            }

            FindOverlaps(solids, faults);
            return faults;
        }

        private static void CheckPlacement(KitBlock block, List<KitFault> faults)
        {
            var t = block.transform;
            var frame = block.GetComponentInParent<KitFrame>();
            if (frame != null)
            {
                var f = frame.transform;
                var fyaw = Mathf.Atan2(f.forward.x, f.forward.z) * Mathf.Rad2Deg;
                if (!IsMultiple(f.position.x, 1f) || !IsMultiple(f.position.z, 1f) ||
                    Mathf.Abs(fyaw / 45f - Mathf.Round(fyaw / 45f)) * 45f > AngleToleranceDegrees ||
                    Vector3.Angle(f.up, Vector3.up) > AngleToleranceDegrees)
                {
                    faults.Add(new KitFault(KitFaultKind.OffGrid, block, $"its frame '{frame.name}' is off a grid node or not at a multiple of 45°"));
                }
            }

            var frameRotation = frame != null ? frame.transform.rotation : Quaternion.identity;
            var frameOrigin = frame != null ? frame.transform.position : Vector3.zero;
            var p = Quaternion.Inverse(frameRotation) * (t.position - frameOrigin);
            p.y = t.position.y;
            if (!IsMultiple(p.x, 1f) || !IsMultiple(p.z, 1f) || !IsMultiple(p.y, HeightStep))
            {
                faults.Add(new KitFault(KitFaultKind.OffGrid, block,
                    $"position {p.x:0.###}, {p.y:0.###}, {p.z:0.###} is off the 1 m grid or the 0.1 m height step"));
            }

            var forward = Quaternion.Inverse(frameRotation) * t.forward;
            var yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            var yawOff = Mathf.Abs(yaw / 90f - Mathf.Round(yaw / 90f)) * 90f;
            if (Vector3.Angle(t.up, Vector3.up) > AngleToleranceDegrees || yawOff > AngleToleranceDegrees)
            {
                faults.Add(new KitFault(KitFaultKind.OffGrid, block, "rotation is not a multiple of 90° about Y"));
            }

            if ((t.lossyScale - Vector3.one).sqrMagnitude > PlacementTolerance * PlacementTolerance)
            {
                faults.Add(new KitFault(KitFaultKind.OffGrid, block, $"scale {t.lossyScale} is not 1"));
            }
        }

        private static bool IsMultiple(float value, float step)
        {
            var steps = value / step;
            return Mathf.Abs(steps - Mathf.Round(steps)) * step <= PlacementTolerance;
        }

        /// <summary>Union of the block's own enabled, non-trigger colliders; colliders of a nested block are its own.</summary>
        private static bool TryGetSolidBounds(KitBlock block, out Bounds bounds)
        {
            bounds = default;
            var found = false;
            foreach (var collider in Solids(block))
            {
                if (found)
                {
                    bounds.Encapsulate(collider.bounds);
                }
                else
                {
                    bounds = collider.bounds;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>Sort and sweep along X; a pair overlaps when it penetrates on all three axes.</summary>
        private static void FindOverlaps(List<(KitBlock block, Bounds bounds)> solids, List<KitFault> faults)
        {
            solids.Sort((a, b) => a.bounds.min.x.CompareTo(b.bounds.min.x));
            for (var i = 0; i < solids.Count; i++)
            {
                var a = solids[i];
                for (var j = i + 1; j < solids.Count && solids[j].bounds.min.x < a.bounds.max.x - OverlapTolerance; j++)
                {
                    var b = solids[j];
                    if (a.block.transform.IsChildOf(b.block.transform) || b.block.transform.IsChildOf(a.block.transform))
                    {
                        continue;
                    }

                    var depth = Penetration(a.bounds, b.bounds);
                    if (depth > OverlapTolerance && (Turned(a.block) || Turned(b.block)))
                    {
                        depth = ColliderPenetration(a.block, b.block, depth);
                    }

                    if (depth > OverlapTolerance)
                    {
                        faults.Add(new KitFault(KitFaultKind.Overlap, a.block, $"solid bounds overlap by {depth:0.###} m", b.block));
                    }
                }
            }
        }

        private static bool Turned(KitBlock block)
        {
            var forward = block.transform.forward;
            var yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            return Mathf.Abs(yaw / 90f - Mathf.Round(yaw / 90f)) * 90f > AngleToleranceDegrees;
        }

        /// <summary>The deepest penetration between the two blocks' own solid colliders; world bounds overstate it
        /// for a turned block. A pair Unity cannot measure keeps the bounds' reading.</summary>
        private static float ColliderPenetration(KitBlock a, KitBlock b, float boundsDepth)
        {
            var deepest = 0f;
            foreach (var ca in Solids(a))
            {
                foreach (var cb in Solids(b))
                {
                    if (ca is MeshCollider ma && !ma.convex || cb is MeshCollider mb && !mb.convex)
                    {
                        return boundsDepth;
                    }

                    if (Physics.ComputePenetration(ca, ca.transform.position, ca.transform.rotation,
                            cb, cb.transform.position, cb.transform.rotation, out _, out var distance))
                    {
                        deepest = Mathf.Max(deepest, distance);
                    }
                }
            }

            return deepest;
        }

        /// <summary>
        /// The block's own enabled, non-trigger colliders. A set's loose things leave it when the scene starts, and a
        /// still set's ThingSurface only holds them (kit standard §4), so neither counts.
        /// </summary>
        private static IEnumerable<Collider> Solids(KitBlock block)
        {
            foreach (var collider in block.GetComponentsInChildren<Collider>())
            {
                var loose = collider.GetComponentInParent<LooseProp>();
                if (collider.enabled && !collider.isTrigger && collider.GetComponentInParent<KitBlock>() == block &&
                    (loose == null || !loose.IsThing) && collider.GetComponentInParent<ThingSurface>() == null)
                {
                    yield return collider;
                }
            }
        }

        private static float Penetration(Bounds a, Bounds b)
        {
            var depth = float.MaxValue;
            for (var axis = 0; axis < 3; axis++)
            {
                var overlap = Mathf.Min(a.max[axis], b.max[axis]) - Mathf.Max(a.min[axis], b.min[axis]);
                depth = Mathf.Min(depth, overlap);
            }

            return depth;
        }
    }
}
