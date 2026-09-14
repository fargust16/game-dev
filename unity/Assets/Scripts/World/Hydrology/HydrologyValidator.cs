using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Hydrology
{
    /// <summary>
    /// Checks the invariants the hydrology system exists to guarantee, and reports the first failure.
    /// </summary>
    /// <remarks>
    /// Priority-flood makes most of these true by construction, which is precisely why they are
    /// checked anyway: an invariant that is only guaranteed by an argument stops being guaranteed the
    /// moment somebody changes the argument. The ocean-outlet rule in particular is walked
    /// explicitly, never assumed. See <c>HYDROLOGY_GENERATION_PLAN.md</c> §16.
    /// </remarks>
    public static class HydrologyValidator
    {
        /// <summary>
        /// How far a node may sit above its upstream neighbour before it counts as flowing uphill.
        /// </summary>
        /// <remarks>
        /// Ten centimetres. The network builder forces a millimetre of fall per node, so anything
        /// this large is a real defect and not accumulated single-precision drift along a reach that
        /// can be hundreds of nodes long.
        /// </remarks>
        public const float UphillToleranceMeters = 0.1f;

        public readonly struct Report
        {
            public bool IsValid { get; }
            public string Message { get; }

            public Report(bool isValid, string message)
            {
                IsValid = isValid;
                Message = message;
            }

            public static readonly Report Valid = new Report(true, null);

            public static Report Invalid(string message) => new Report(false, message);
        }

        public static Report Validate(
            IReadOnlyList<RiverReach> reaches, IReadOnlyList<LakeBasin> lakes, HydrologyGrid grid)
        {
            if (reaches == null)
            {
                return Report.Invalid("reach list is null");
            }

            var byId = new Dictionary<int, RiverReach>(reaches.Count);
            for (var i = 0; i < reaches.Count; i++)
            {
                var reach = reaches[i];
                if (reach == null)
                {
                    return Report.Invalid($"reach at index {i} is null");
                }

                if (byId.ContainsKey(reach.Id.Value))
                {
                    return Report.Invalid($"duplicate reach id {reach.Id}");
                }

                byId.Add(reach.Id.Value, reach);
            }

            // Rule 7: deterministic ordering. Checked rather than trusted, because a consumer that
            // relies on it would otherwise only find out when two runs of the same seed disagree.
            for (var i = 1; i < reaches.Count; i++)
            {
                if (reaches[i - 1].Id.Value >= reaches[i].Id.Value)
                {
                    return Report.Invalid(
                        $"reaches are not in ascending id order at index {i} "
                        + $"({reaches[i - 1].Id} then {reaches[i].Id})");
                }
            }

            for (var i = 0; i < reaches.Count; i++)
            {
                var geometry = ValidateGeometry(reaches[i], grid);
                if (!geometry.IsValid)
                {
                    return geometry;
                }
            }

            var lakesById = new Dictionary<int, LakeBasin>();
            if (lakes != null)
            {
                for (var i = 0; i < lakes.Count; i++)
                {
                    lakesById[lakes[i].Id.Value] = lakes[i];
                }
            }

            var topology = ValidateOceanReachability(reaches, byId, lakesById);
            if (!topology.IsValid)
            {
                return topology;
            }

            return ValidateLakes(lakes, byId);
        }

        private static Report ValidateGeometry(RiverReach reach, HydrologyGrid grid)
        {
            var nodes = reach.Nodes;
            if (nodes == null || nodes.Count < 2)
            {
                return Report.Invalid($"{reach.Id} has fewer than two nodes");
            }

            var minX = grid?.OriginX ?? float.MinValue;
            var minZ = grid?.OriginZ ?? float.MinValue;
            var maxX = grid != null ? grid.OriginX + (grid.Width - 1) * grid.CellSize : float.MaxValue;
            var maxZ = grid != null ? grid.OriginZ + (grid.Height - 1) * grid.CellSize : float.MaxValue;

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                // Rule 5: finite, in-world coordinates.
                if (!IsFinite(node.PositionXZ.x) || !IsFinite(node.PositionXZ.y)
                    || !IsFinite(node.BedElevation) || !IsFinite(node.WaterElevation))
                {
                    return Report.Invalid($"{reach.Id} node {i} has a non-finite value");
                }

                if (grid != null
                    && (node.PositionXZ.x < minX - 1f || node.PositionXZ.x > maxX + 1f
                        || node.PositionXZ.y < minZ - 1f || node.PositionXZ.y > maxZ + 1f))
                {
                    return Report.Invalid(
                        $"{reach.Id} node {i} at {node.PositionXZ} is outside the world");
                }

                if (node.WaterElevation < node.BedElevation - 1e-3f)
                {
                    return Report.Invalid($"{reach.Id} node {i} has water below its bed");
                }

                if (i == 0)
                {
                    continue;
                }

                var previous = nodes[i - 1];

                // Rule 2: no uphill flow.
                if (node.BedElevation > previous.BedElevation + UphillToleranceMeters)
                {
                    return Report.Invalid(
                        $"{reach.Id} flows uphill at node {i}: "
                        + $"{previous.BedElevation:F2} m to {node.BedElevation:F2} m");
                }

                // Rule 4: no duplicate node positions inside a reach.
                if (Vector2.SqrMagnitude(node.PositionXZ - previous.PositionXZ) < 1e-6f)
                {
                    return Report.Invalid($"{reach.Id} repeats a node position at index {i}");
                }

                if (node.CumulativeDistance < previous.CumulativeDistance)
                {
                    return Report.Invalid($"{reach.Id} has non-monotonic distance at node {i}");
                }
            }

            return Report.Valid;
        }

        /// <summary>
        /// Rules 1 and 3: following the downstream links always ends at the sea, and never loops.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the hard world rule made checkable. The walk carries its own visited set per
        /// reach, so a cycle is caught as a cycle rather than as a hang, and a walk longer than the
        /// reach count is reported instead of being allowed to run.
        /// </para>
        /// <para>
        /// A reach that ends in a lake continues <b>through</b> it: the water leaves by the basin's
        /// outlet, so the walk hops to that reach. Without the hop every tributary above a lake
        /// looks like a river that simply stops, which is the failure this rule exists to catch and
        /// would make it impossible for any lake-fed system to pass.
        /// </para>
        /// </remarks>
        private static Report ValidateOceanReachability(
            IReadOnlyList<RiverReach> reaches,
            IReadOnlyDictionary<int, RiverReach> byId,
            IReadOnlyDictionary<int, LakeBasin> lakesById)
        {
            var visited = new HashSet<int>();
            for (var i = 0; i < reaches.Count; i++)
            {
                visited.Clear();
                var current = reaches[i];
                var steps = 0;

                while (true)
                {
                    if (!visited.Add(current.Id.Value))
                    {
                        return Report.Invalid(
                            $"{reaches[i].Id} is part of a downstream cycle through {current.Id}");
                    }

                    if (current.Termination == ReachTermination.Ocean)
                    {
                        break;
                    }

                    var nextId = current.DownstreamId;
                    if (current.Termination == ReachTermination.Lake)
                    {
                        if (!current.DownstreamLake.IsValid
                            || !lakesById.TryGetValue(current.DownstreamLake.Value, out var lake))
                        {
                            return Report.Invalid(
                                $"{current.Id} terminates in {current.DownstreamLake}, which does "
                                + "not exist, so it never reaches the ocean");
                        }

                        if (lake.SpillsDirectlyToOcean)
                        {
                            // The basin overflows onto the shore, so the water is already at the sea
                            // and there is no further reach to walk.
                            break;
                        }

                        if (!lake.OutletReach.IsValid)
                        {
                            return Report.Invalid(
                                $"{current.Id} drains into {lake.Id}, which has no outlet reach and "
                                + "does not reach the sea, so it never reaches the ocean");
                        }

                        nextId = lake.OutletReach;
                    }

                    if (!nextId.IsValid)
                    {
                        return Report.Invalid(
                            $"{current.Id} terminates as {current.Termination} but has no downstream "
                            + "reach, so it never reaches the ocean");
                    }

                    if (!byId.TryGetValue(nextId.Value, out var next))
                    {
                        return Report.Invalid(
                            $"{current.Id} points at missing downstream {nextId}");
                    }

                    current = next;

                    if (++steps > reaches.Count)
                    {
                        return Report.Invalid(
                            $"{reaches[i].Id} walked more than {reaches.Count} reaches downstream");
                    }
                }
            }

            return Report.Valid;
        }

        private static Report ValidateLakes(
            IReadOnlyList<LakeBasin> lakes, IReadOnlyDictionary<int, RiverReach> byId)
        {
            if (lakes == null)
            {
                return Report.Valid;
            }

            for (var i = 0; i < lakes.Count; i++)
            {
                var lake = lakes[i];
                if (lake.Boundary == null || lake.Boundary.Count < 3)
                {
                    return Report.Invalid($"{lake.Id} has a boundary with fewer than three vertices");
                }

                if (!IsFinite(lake.SurfaceElevation) || !IsFinite(lake.MaxDepth))
                {
                    return Report.Invalid($"{lake.Id} has a non-finite surface or depth");
                }

                // Rule 6: the surface sits above the floor it was measured from.
                if (lake.MaxDepth <= 0f)
                {
                    return Report.Invalid($"{lake.Id} has no depth");
                }

                if (lake.OutletReach.IsValid && !byId.ContainsKey(lake.OutletReach.Value))
                {
                    return Report.Invalid($"{lake.Id} points at missing outlet {lake.OutletReach}");
                }

                for (var n = 0; n < lake.InletReaches.Count; n++)
                {
                    if (!byId.ContainsKey(lake.InletReaches[n].Value))
                    {
                        return Report.Invalid(
                            $"{lake.Id} points at missing inlet {lake.InletReaches[n]}");
                    }
                }

                if (i > 0 && lakes[i - 1].Id.Value >= lake.Id.Value)
                {
                    return Report.Invalid($"lakes are not in ascending id order at index {i}");
                }
            }

            return Report.Valid;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
