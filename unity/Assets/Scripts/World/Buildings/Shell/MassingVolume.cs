using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Buildings.Shell
{
    /// <summary>
    /// One rectangular footprint on one storey. Same-floor slabs must not overlap in XZ.
    /// Authority: SHELL_NON_OVERLAP_RULE.md (floor-stack assembly).
    /// </summary>
    [Serializable]
    public struct FloorSlab
    {
        public string Id;
        public float LocalX;
        public float LocalZ;
        public float WidthMeters;
        public float DepthMeters;
        public bool IsPrimaryStreet;
        public int DoorBay;

        public float MaxX => LocalX + WidthMeters;
        public float MaxZ => LocalZ + DepthMeters;

        public PlotFootprint AsFootprint()
        {
            var widthBays = Mathf.Max(1, Mathf.CeilToInt(WidthMeters / ModularGrid.BayMeters - 0.001f));
            var depthBays = Mathf.Max(1, Mathf.CeilToInt(DepthMeters / ModularGrid.BayMeters - 0.001f));
            return new PlotFootprint
            {
                WidthBays = widthBays,
                DepthBays = depthBays,
                Shape = PlotShape.Rectangle,
                YawDegrees = 0f,
                Origin = new Vector3(LocalX, 0f, LocalZ)
            };
        }

        public bool ContainsXZ(float x, float z, float epsilon = 0.001f)
        {
            return x >= LocalX - epsilon
                   && x <= MaxX + epsilon
                   && z >= LocalZ - epsilon
                   && z <= MaxZ + epsilon;
        }
    }

    /// <summary>
    /// Bottom-up massing: storey 0..N-1 each with non-overlapping slabs.
    /// When a slab disappears on the next storey, that part gets a roof;
    /// continuing walls that overlook a vacated footprint get attic openings.
    /// </summary>
    public sealed class FloorStackMassing
    {
        public int FloorCount;
        public string PrimarySlabId;
        public readonly List<FloorSlab>[] SlabsByFloor;

        public FloorStackMassing(int floorCount)
        {
            FloorCount = Mathf.Max(1, floorCount);
            SlabsByFloor = new List<FloorSlab>[FloorCount];
            for (var i = 0; i < FloorCount; i++)
                SlabsByFloor[i] = new List<FloorSlab>(2);
            PrimarySlabId = "main";
        }

        public IReadOnlyList<FloorSlab> UniqueFootprints()
        {
            var list = new List<FloorSlab>(4);
            for (var f = 0; f < FloorCount; f++)
            {
                var slabs = SlabsByFloor[f];
                for (var i = 0; i < slabs.Count; i++)
                {
                    var id = slabs[i].Id;
                    var found = false;
                    for (var j = 0; j < list.Count; j++)
                    {
                        if (list[j].Id == id)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        list.Add(slabs[i]);
                }
            }

            return list;
        }

        public bool Continues(string slabId, int floor)
        {
            if (floor + 1 >= FloorCount)
                return false;
            var next = SlabsByFloor[floor + 1];
            for (var i = 0; i < next.Count; i++)
            {
                if (next[i].Id == slabId)
                    return true;
            }

            return false;
        }

        public bool CoveredBelow(float x, float z, int floor)
        {
            for (var f = 0; f < floor; f++)
            {
                var slabs = SlabsByFloor[f];
                for (var i = 0; i < slabs.Count; i++)
                {
                    if (slabs[i].ContainsXZ(x, z))
                        return true;
                }
            }

            return false;
        }

        public bool CoveredOnFloor(float x, float z, int floor)
        {
            if (floor < 0 || floor >= FloorCount)
                return false;
            var slabs = SlabsByFloor[floor];
            for (var i = 0; i < slabs.Count; i++)
            {
                if (slabs[i].ContainsXZ(x, z))
                    return true;
            }

            return false;
        }

        public FloorSlab? FindPrimaryTop()
        {
            for (var f = FloorCount - 1; f >= 0; f--)
            {
                var slabs = SlabsByFloor[f];
                for (var i = 0; i < slabs.Count; i++)
                {
                    if (slabs[i].Id == PrimarySlabId || slabs[i].IsPrimaryStreet)
                        return slabs[i];
                }
            }

            if (FloorCount > 0 && SlabsByFloor[FloorCount - 1].Count > 0)
                return SlabsByFloor[FloorCount - 1][0];
            return null;
        }

        /// <summary>
        /// Regions present on <paramref name="floor"/> but not covered by the next storey.
        /// These get a roof before the upper walls continue.
        /// </summary>
        public List<FloorSlab> VacatedRegions(int floor)
        {
            var result = new List<FloorSlab>(2);
            if (floor < 0 || floor >= FloorCount)
                return result;

            var current = SlabsByFloor[floor];
            if (floor + 1 >= FloorCount)
            {
                for (var i = 0; i < current.Count; i++)
                    result.Add(current[i]);
                return result;
            }

            var next = SlabsByFloor[floor + 1];
            for (var i = 0; i < current.Count; i++)
            {
                var a = current[i];
                var fullyCovered = false;
                for (var j = 0; j < next.Count; j++)
                {
                    var b = next[j];
                    if (Mathf.Abs(a.LocalX - b.LocalX) < 0.001f
                        && Mathf.Abs(a.LocalZ - b.LocalZ) < 0.001f
                        && Mathf.Abs(a.WidthMeters - b.WidthMeters) < 0.001f
                        && Mathf.Abs(a.DepthMeters - b.DepthMeters) < 0.001f)
                    {
                        fullyCovered = true;
                        break;
                    }
                }

                if (fullyCovered)
                    continue;

                // Subtract each next slab from a (next slabs are subsets of a in our massing).
                var remain = new List<FloorSlab> { a };
                for (var j = 0; j < next.Count; j++)
                    remain = SubtractAll(remain, next[j]);

                for (var r = 0; r < remain.Count; r++)
                {
                    var slab = remain[r];
                    slab.Id = $"vacated_{floor}_{r}";
                    slab.IsPrimaryStreet = false;
                    slab.DoorBay = -1;
                    result.Add(slab);
                }
            }

            return result;
        }

        private static List<FloorSlab> SubtractAll(List<FloorSlab> regions, FloorSlab cut)
        {
            var output = new List<FloorSlab>(4);
            for (var i = 0; i < regions.Count; i++)
                SubtractOne(regions[i], cut, output);
            return output;
        }

        private static void SubtractOne(FloorSlab a, FloorSlab b, List<FloorSlab> output)
        {
            // No intersection → keep a.
            if (a.LocalX >= b.MaxX - 0.001f || a.MaxX <= b.LocalX + 0.001f
                || a.LocalZ >= b.MaxZ - 0.001f || a.MaxZ <= b.LocalZ + 0.001f)
            {
                output.Add(a);
                return;
            }

            var x0 = Mathf.Max(a.LocalX, b.LocalX);
            var x1 = Mathf.Min(a.MaxX, b.MaxX);
            var z0 = Mathf.Max(a.LocalZ, b.LocalZ);
            var z1 = Mathf.Min(a.MaxZ, b.MaxZ);

            // Left strip
            if (x0 > a.LocalX + 0.001f)
                output.Add(Make(a.LocalX, a.LocalZ, x0 - a.LocalX, a.DepthMeters));
            // Right strip
            if (x1 < a.MaxX - 0.001f)
                output.Add(Make(x1, a.LocalZ, a.MaxX - x1, a.DepthMeters));
            // Front strip (within cut's X band)
            if (z0 > a.LocalZ + 0.001f)
                output.Add(Make(x0, a.LocalZ, x1 - x0, z0 - a.LocalZ));
            // Back strip
            if (z1 < a.MaxZ - 0.001f)
                output.Add(Make(x0, z1, x1 - x0, a.MaxZ - z1));
        }

        private static FloorSlab Make(float x, float z, float w, float d)
        {
            return new FloorSlab
            {
                Id = "part",
                LocalX = x,
                LocalZ = z,
                WidthMeters = w,
                DepthMeters = d,
                IsPrimaryStreet = false,
                DoorBay = -1
            };
        }
    }
}
