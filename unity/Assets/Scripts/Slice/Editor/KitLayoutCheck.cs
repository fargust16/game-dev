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
            var other = Other != null ? $" and '{Other.name}'" : string.Empty;
            return $"{Kind}: '{Block.name}'{other}: {Detail}";
        }
    }

    /// <summary>
    /// Checks hand-placed kit blocks against SLICE_MODULAR_KIT_STANDARD: the 1 m grid and 90° turns (§1–§2), the kit's
    /// layers (§5), colliders (§4), and the non-overlap law (§6): solid bounds may touch, never overlap.
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
                if (block == null)
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
            var p = t.position;
            if (!IsMultiple(p.x, 1f) || !IsMultiple(p.z, 1f) || !IsMultiple(p.y, HeightStep))
            {
                faults.Add(new KitFault(KitFaultKind.OffGrid, block,
                    $"position {p.x:0.###}, {p.y:0.###}, {p.z:0.###} is off the 1 m grid or the 0.1 m height step"));
            }

            var forward = t.forward;
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
            foreach (var collider in block.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || collider.isTrigger || collider.GetComponentInParent<KitBlock>() != block)
                {
                    continue;
                }

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
                    if (depth > OverlapTolerance)
                    {
                        faults.Add(new KitFault(KitFaultKind.Overlap, a.block, $"solid bounds overlap by {depth:0.###} m", b.block));
                    }
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
