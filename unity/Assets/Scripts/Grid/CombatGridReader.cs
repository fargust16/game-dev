using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameDev.World;
using UnityEngine;

namespace GameDev.Grid
{
    public enum CombatGridBlock
    {
        None = 0,
        Water = 1,
        Slope = 2,
        Clearance = 3
    }

    public struct CombatGridSurface
    {
        public float Y;
        public bool Standable;
        public CombatGridBlock Reason;
    }

    public struct CombatGridCover
    {
        public int Low;
        public int High;
    }

    public sealed class CombatGridWindow
    {
        public int X;
        public int Z;
        public HashSet<long> Reachable;
        public int Standable;
        public int MultiLevel;
        public int LowCover;
        public int HighCover;
        public double ColumnMs;
        public double EdgeMs;
        public double SearchMs;
        public double CoverMs;
        public ulong Hash;

        public double TotalMs => ColumnMs + EdgeMs + SearchMs + CoverMs;
    }

    /// <summary>
    /// Reads world-aligned cells on demand and keeps them, so a moved grid only pays for new cells.
    /// A cell's reading depends on its world position alone, never on where the grid started.
    /// </summary>
    public sealed class CombatGridReader
    {
        public const int MaxLevels = 16;
        public const int Reach = 20;
        // The grid reads the place, never the characters in it (AGENTS, Vertical Slice target), so it leaves out Water
        // (4), the slice's Character layer (12), and NoGrid (14); Ignore Raycast (2) is already outside the defaults.
        public const int Mask = Physics.DefaultRaycastLayers & ~(1 << 4) & ~(1 << 12) & ~(1 << 14);
        public const float SlopeLimitDegrees = 45f;
        public const float StepOffset = 0.4f;
        public const float TopY = WorldScale.TerrainHeightBudgetMeters + 20f;
        public const float Radius = WorldScale.CharacterRadiusMeters;
        public const float Height = WorldScale.CharacterHeightMeters;

        private const float LowCoverHeight = 0.7f;
        private const float HighCoverHeight = 1.5f;
        private const int MaxHits = 32;

        // Cover reads a strip beside each side of the cell, not one ray from its middle, so a wall at any angle gives
        // the same cover as one on the grid lines (Veilmouth C1 §12, 2026-11-05). The strip runs from 0.3 m (the
        // person's radius) to 0.9 m out from the cell's middle, across the cell's whole width, a thin slice at each
        // cover height. Authoring defaults: a wall on a grid line, 0.5 m out, still gives cover as before.
        private const float CoverNear = 0.3f;
        private const float CoverFar = 0.9f;
        private const float CoverSlice = 0.05f;
        private const int CoordOffset = 32768;

        private static readonly int[] DirX = { 1, -1, 0, 0 };
        private static readonly int[] DirZ = { 0, 0, 1, -1 };

        private readonly float _cell;
        private readonly Dictionary<long, CombatGridSurface[]> _columns = new Dictionary<long, CombatGridSurface[]>();
        private readonly Dictionary<long, bool> _edges = new Dictionary<long, bool>();
        private readonly Dictionary<long, CombatGridCover> _cover = new Dictionary<long, CombatGridCover>();
        private readonly RaycastHit[] _hits = new RaycastHit[MaxHits];
        private readonly Collider[] _near = new Collider[MaxHits];

        public int OverflowColumns;
        public int TerrainCoverRays;

        public CombatGridReader(float cell)
        {
            _cell = cell;
        }

        public static long Pack(int ix, int iz, int level)
        {
            return ((long)(ix + CoordOffset) << 20) | ((long)(iz + CoordOffset) << 4) | (uint)level;
        }

        public static void Unpack(long key, out int ix, out int iz, out int level)
        {
            ix = (int)(key >> 20) - CoordOffset;
            iz = (int)((key >> 4) & 0xFFFF) - CoordOffset;
            level = (int)(key & 0xF);
        }

        public Vector3 Center(int ix, int iz, float y)
        {
            return new Vector3((ix + 0.5f) * _cell, y, (iz + 0.5f) * _cell);
        }

        public CombatGridSurface[] Column(int ix, int iz)
        {
            var key = Pack(ix, iz, 0);
            if (!_columns.TryGetValue(key, out var column))
            {
                column = ReadColumn(ix, iz);
                _columns[key] = column;
            }

            return column;
        }

        /// <summary>
        /// The nearest cell with a standable surface, and in it the surface nearest the position's height, so a fight on
        /// an upper storey reads that storey and not the floor under it (VICTORIAN_INTERIORS_PLAN VI-3). With one
        /// surface in a column, as outdoors, it is the same cell as before.
        /// </summary>
        public bool TryFindStart(Vector3 position, out int ix, out int iz, out int level)
        {
            var cx = Mathf.FloorToInt(position.x / _cell);
            var cz = Mathf.FloorToInt(position.z / _cell);
            for (var ring = 0; ring <= Reach; ring++)
            {
                for (var dz = -ring; dz <= ring; dz++)
                {
                    for (var dx = -ring; dx <= ring; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != ring)
                        {
                            continue;
                        }

                        var column = Column(cx + dx, cz + dz);
                        var best = -1;
                        for (var l = column.Length - 1; l >= 0; l--)
                        {
                            if (column[l].Standable &&
                                (best < 0 || Mathf.Abs(column[l].Y - position.y) < Mathf.Abs(column[best].Y - position.y)))
                            {
                                best = l;
                            }
                        }

                        if (best >= 0)
                        {
                            ix = cx + dx;
                            iz = cz + dz;
                            level = best;
                            return true;
                        }
                    }
                }
            }

            ix = cx;
            iz = cz;
            level = 0;
            return false;
        }

        public CombatGridWindow Build(int cx, int cz, int level)
        {
            var window = new CombatGridWindow { X = cx, Z = cz };

            var started = Stopwatch.GetTimestamp();
            for (var dz = -Reach; dz <= Reach; dz++)
            {
                for (var dx = -Reach; dx <= Reach; dx++)
                {
                    Column(cx + dx, cz + dz);
                }
            }

            window.ColumnMs = Lap(ref started);

            for (var dz = -Reach; dz <= Reach; dz++)
            {
                for (var dx = -Reach; dx <= Reach; dx++)
                {
                    if (dx < Reach)
                    {
                        ReadEdges(cx + dx, cz + dz, cx + dx + 1, cz + dz);
                    }

                    if (dz < Reach)
                    {
                        ReadEdges(cx + dx, cz + dz, cx + dx, cz + dz + 1);
                    }
                }
            }

            window.EdgeMs = Lap(ref started);

            window.Reachable = Search(cx, cz, level);
            window.SearchMs = Lap(ref started);

            foreach (var key in window.Reachable)
            {
                Unpack(key, out var ix, out var iz, out var l);
                var cover = Cover(ix, iz, l);
                window.LowCover += cover.Low != 0 ? 1 : 0;
                window.HighCover += cover.High != 0 ? 1 : 0;
            }

            window.CoverMs = Lap(ref started);

            Summarise(window);
            return window;
        }

        public bool EdgeBetween(int ax, int az, int la, int bx, int bz, int lb)
        {
            if (bx == ax + 1)
            {
                return Edge(ax, az, la, 0, lb, bx, bz);
            }

            if (bx == ax - 1)
            {
                return Edge(bx, bz, lb, 0, la, ax, az);
            }

            return bz == az + 1 ? Edge(ax, az, la, 1, lb, bx, bz) : Edge(bx, bz, lb, 1, la, ax, az);
        }

        /// <summary>What decides this edge: the height limit, a collider, or nothing.</summary>
        public string DescribeEdge(Vector3 a, Vector3 b)
        {
            var delta = b - a;
            var distance = delta.magnitude;
            if (Mathf.Abs(delta.y) > _cell * Mathf.Tan(SlopeLimitDegrees * Mathf.Deg2Rad))
            {
                return "height";
            }

            return Physics.CapsuleCast(
                a + Vector3.up * (StepOffset + Radius),
                a + Vector3.up * (Height - Radius),
                Radius,
                delta / distance,
                out var hit,
                distance,
                Mask,
                QueryTriggerInteraction.Ignore)
                ? hit.collider.name
                : "open";
        }

        public CombatGridCover Cover(int ix, int iz, int level)
        {
            var key = Pack(ix, iz, level);
            if (_cover.TryGetValue(key, out var cover))
            {
                return cover;
            }

            var ground = Center(ix, iz, Column(ix, iz)[level].Y);
            for (var d = 0; d < 4; d++)
            {
                var direction = new Vector3(DirX[d], 0f, DirZ[d]);
                if (CoverRay(ground + Vector3.up * LowCoverHeight, direction))
                {
                    cover.Low |= 1 << d;
                }

                if (CoverRay(ground + Vector3.up * HighCoverHeight, direction))
                {
                    cover.High |= 1 << d;
                }
            }

            _cover[key] = cover;
            return cover;
        }

        private bool CoverRay(Vector3 origin, Vector3 direction)
        {
            var depth = (CoverFar - CoverNear) * _cell;
            var centre = origin + direction * ((CoverNear * _cell) + depth * 0.5f);
            var across = Vector3.Cross(Vector3.up, direction);
            var half = (direction * (depth * 0.5f)) + (across * (_cell * 0.5f));
            var extents = new Vector3(Mathf.Abs(half.x), CoverSlice, Mathf.Abs(half.z));
            var count = Physics.OverlapBoxNonAlloc(centre, extents, _near, Quaternion.identity, Mask, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                if (_near[i] is TerrainCollider)
                {
                    TerrainCoverRays++;
                    continue;
                }

                return true;
            }

            return false;
        }

        private CombatGridSurface[] ReadColumn(int ix, int iz)
        {
            var origin = Center(ix, iz, TopY);
            var count = Physics.RaycastNonAlloc(
                origin, Vector3.down, _hits, TopY, Mask, QueryTriggerInteraction.Ignore);
            if (count == MaxHits)
            {
                OverflowColumns++;
            }

            Array.Sort(_hits, 0, count, HitHeightOrder.Instance);

            var surfaces = new List<CombatGridSurface>(count);
            for (var i = 0; i < count && surfaces.Count < MaxLevels; i++)
            {
                var hit = _hits[i];
                if (surfaces.Count > 0 && Mathf.Abs(surfaces[surfaces.Count - 1].Y - hit.point.y) < 0.02f)
                {
                    continue;
                }

                surfaces.Add(ReadSurface(hit));
            }

            return surfaces.ToArray();
        }

        private static CombatGridSurface ReadSurface(RaycastHit hit)
        {
            var surface = new CombatGridSurface { Y = hit.point.y, Reason = CombatGridBlock.None, Standable = true };
            // Water is the ground under the sea, so only the terrain below Sea Level reads as water: an interior's
            // floor stands near 0 m and a quay's steps may dip below 24 m, and both are floors (2026-11-06).
            if (hit.collider is TerrainCollider && hit.point.y < WorldScale.SeaLevelMeters)
            {
                surface.Reason = CombatGridBlock.Water;
            }
            else if (Vector3.Angle(hit.normal, Vector3.up) >= SlopeLimitDegrees)
            {
                surface.Reason = CombatGridBlock.Slope;
            }
            else if (Physics.CheckCapsule(
                         hit.point + Vector3.up * (StepOffset + Radius),
                         hit.point + Vector3.up * (Height - Radius),
                         Radius,
                         Mask,
                         QueryTriggerInteraction.Ignore))
            {
                surface.Reason = CombatGridBlock.Clearance;
            }

            surface.Standable = surface.Reason == CombatGridBlock.None;
            return surface;
        }

        private void ReadEdges(int ax, int az, int bx, int bz)
        {
            var a = Column(ax, az);
            var b = Column(bx, bz);
            for (var la = 0; la < a.Length; la++)
            {
                if (!a[la].Standable)
                {
                    continue;
                }

                for (var lb = 0; lb < b.Length; lb++)
                {
                    if (b[lb].Standable)
                    {
                        EdgeBetween(ax, az, la, bx, bz, lb);
                    }
                }
            }
        }

        /// <param name="direction">0 is +X and 1 is +Z; the key is always the lower cell.</param>
        private bool Edge(int ax, int az, int la, int direction, int lb, int bx, int bz)
        {
            var key = (Pack(ax, az, la) << 5) | ((long)direction << 4) | (uint)lb;
            if (_edges.TryGetValue(key, out var open))
            {
                return open;
            }

            var a = Center(ax, az, Column(ax, az)[la].Y);
            var b = Center(bx, bz, Column(bx, bz)[lb].Y);
            var delta = b - a;
            var distance = delta.magnitude;
            open = Mathf.Abs(delta.y) <= _cell * Mathf.Tan(SlopeLimitDegrees * Mathf.Deg2Rad)
                   && !Physics.CapsuleCast(
                       a + Vector3.up * (StepOffset + Radius),
                       a + Vector3.up * (Height - Radius),
                       Radius,
                       delta / distance,
                       distance,
                       Mask,
                       QueryTriggerInteraction.Ignore);
            _edges[key] = open;
            return open;
        }

        private HashSet<long> Search(int cx, int cz, int level)
        {
            var reached = new HashSet<long> { Pack(cx, cz, level) };
            var queue = new Queue<long>();
            queue.Enqueue(Pack(cx, cz, level));

            while (queue.Count > 0)
            {
                Unpack(queue.Dequeue(), out var x, out var z, out var l);
                for (var d = 0; d < 4; d++)
                {
                    Visit(cx, cz, x, z, l, x + DirX[d], z + DirZ[d], reached, queue);
                }

                for (var dx = -1; dx <= 1; dx += 2)
                {
                    for (var dz = -1; dz <= 1; dz += 2)
                    {
                        VisitDiagonal(cx, cz, x, z, l, dx, dz, reached, queue);
                    }
                }
            }

            return reached;
        }

        private void Visit(int cx, int cz, int x, int z, int l, int nx, int nz, HashSet<long> reached, Queue<long> queue)
        {
            if (!InWindow(cx, cz, nx, nz))
            {
                return;
            }

            var column = Column(nx, nz);
            for (var n = 0; n < column.Length; n++)
            {
                if (column[n].Standable && EdgeBetween(x, z, l, nx, nz, n) && reached.Add(Pack(nx, nz, n)))
                {
                    queue.Enqueue(Pack(nx, nz, n));
                }
            }
        }

        /// <summary>No corner cutting: both side routes must reach the same diagonal surface.</summary>
        private void VisitDiagonal(int cx, int cz, int x, int z, int l, int dx, int dz, HashSet<long> reached, Queue<long> queue)
        {
            var nx = x + dx;
            var nz = z + dz;
            if (!InWindow(cx, cz, nx, nz))
            {
                return;
            }

            var column = Column(nx, nz);
            for (var n = 0; n < column.Length; n++)
            {
                if (column[n].Standable
                    && Via(x, z, l, nx, z, nx, nz, n)
                    && Via(x, z, l, x, nz, nx, nz, n)
                    && reached.Add(Pack(nx, nz, n)))
                {
                    queue.Enqueue(Pack(nx, nz, n));
                }
            }
        }

        private bool Via(int ax, int az, int la, int mx, int mz, int bx, int bz, int lb)
        {
            var middle = Column(mx, mz);
            for (var m = 0; m < middle.Length; m++)
            {
                if (middle[m].Standable
                    && EdgeBetween(ax, az, la, mx, mz, m)
                    && EdgeBetween(mx, mz, m, bx, bz, lb))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool InWindow(int cx, int cz, int x, int z)
        {
            return Mathf.Abs(x - cx) <= Reach && Mathf.Abs(z - cz) <= Reach;
        }

        private void Summarise(CombatGridWindow window)
        {
            var hash = 14695981039346656037UL;
            for (var dz = -Reach; dz <= Reach; dz++)
            {
                for (var dx = -Reach; dx <= Reach; dx++)
                {
                    var ix = window.X + dx;
                    var iz = window.Z + dz;
                    var column = Column(ix, iz);
                    var standable = 0;
                    for (var l = 0; l < column.Length; l++)
                    {
                        hash = Mix(hash, Mathf.RoundToInt(column[l].Y * 1000f));
                        hash = Mix(hash, column[l].Standable ? 1 : 0);
                        hash = Mix(hash, window.Reachable.Contains(Pack(ix, iz, l)) ? 1 : 0);
                        standable += column[l].Standable ? 1 : 0;
                    }

                    window.Standable += standable > 0 ? 1 : 0;
                    window.MultiLevel += standable > 1 ? 1 : 0;
                }
            }

            window.Hash = hash;
        }

        private static ulong Mix(ulong hash, int value)
        {
            return (hash ^ (uint)value) * 1099511628211UL;
        }

        private static double Lap(ref long started)
        {
            var now = Stopwatch.GetTimestamp();
            var ms = (now - started) * 1000.0 / Stopwatch.Frequency;
            started = now;
            return ms;
        }
    }

    internal sealed class HitHeightOrder : IComparer<RaycastHit>
    {
        public static readonly HitHeightOrder Instance = new HitHeightOrder();

        public int Compare(RaycastHit a, RaycastHit b)
        {
            return b.point.y.CompareTo(a.point.y);
        }
    }
}
