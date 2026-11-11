using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace GameDev.Confront.Grid
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

    /// <summary>Cover on each side of a cell, one bit a side in <see cref="GridSide"/> order, read at 0.7 m (Low) and 1.5 m (High).</summary>
    public struct CombatGridCover
    {
        public int Low;
        public int High;

        /// <summary>High cover on a side is Full Cover; low cover alone is Half Cover (10.1).</summary>
        public CoverLevel On(GridSide side)
        {
            var bit = 1 << (int)side;
            return (High & bit) != 0 ? CoverLevel.Full : (Low & bit) != 0 ? CoverLevel.Half : CoverLevel.None;
        }
    }

    /// <summary>What the last build read. The reader keeps one and fills it again on every build.</summary>
    public sealed class CombatGridWindow
    {
        public int X;
        public int Z;
        public int Level;
        public int ReachableCount;
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
    /// The Combat Grid (17 C.2): reads world-aligned cells on demand and keeps them, so a moved grid only pays for new
    /// cells. A cell's reading depends on its world position alone, never on where the grid started.
    /// </summary>
    /// <remarks>
    /// Everything the reader keeps lives in arrays made once, in a ring of columns a little wider than the window: a
    /// column goes to the slot of its world position modulo the ring, so a step of the window overwrites the columns
    /// that left it. A step allocates nothing (gate G2 of COMBAT_GRID_TECH_TEST: the lab's reader allocated on every
    /// step, and a step with a garbage collection broke the gate). Unity makes a managed object for a collider the
    /// first time a query hands it over, once a collider; that is the engine's, not a step's.
    /// The grid reads the place; who stands where is the occupancy layer, apart from it.
    /// </remarks>
    public sealed class CombatGridReader : ICombatGrid
    {
        public const int MaxLevels = 16;
        public const int Reach = 20;
        // The grid reads the place, never the characters in it (AGENTS, Vertical Slice target), so it leaves out Water
        // (4), the slice's Character layer (12), and NoGrid (14); Ignore Raycast (2) is already outside the defaults.
        public const int Mask = Physics.DefaultRaycastLayers & ~(1 << 4) & ~(1 << 12) & ~(1 << 14);
        public const float SlopeLimitDegrees = 45f;
        public const float StepOffset = 0.4f;

        // WorldScale's person, sea and relief, which this assembly cannot reference; CombatGridScaleTests holds them equal.
        public const float Radius = 0.3f;
        public const float Height = 1.8f;
        public const float SeaLevel = 24f;
        public const float TopY = 700f + 20f;

        private const float LowCoverHeight = 0.7f;
        private const float HighCoverHeight = 1.5f;
        private const int MaxHits = 32;

        // Cover reads a strip beside each side of the cell, not one ray from its middle, so a wall at any angle gives
        // the same cover as one on the grid lines (Veilmouth C1 §12, 2026-11-05). The strip runs from 0.3 m (the
        // person's radius) to 0.9 m out from the cell's middle, a thin slice at each cover height. Authoring defaults:
        // a wall on a grid line, 0.5 m out, still gives cover as before. Since S3a.6 the strip spans a person's
        // shoulders, 0.6 m, not the cell's whole width, so a wall on one side no longer reads as cover on the two
        // sides beside it.
        private const float CoverNear = 0.3f;
        private const float CoverFar = 0.9f;
        private const float CoverHalfWidth = 0.3f;
        private const float CoverSlice = 0.05f;

        // Authoring default, S3a.6: a shot's line runs from and to a standing person's chest, 1.4 m over the surface.
        private const float ChestHeight = 1.4f;
        private const int CoordOffset = 32768;

        // The ring holds the window and one column beyond each edge, so a reading just outside (an overlay's corner, a
        // tool's neighbour) never pushes out a column of the window.
        private const int Ring = (2 * Reach) + 3;
        private const int Slots = Ring * Ring;
        private const int Nodes = Slots * MaxLevels;
        private const int EdgesPerSlot = MaxLevels * 2 * MaxLevels;
        private const byte Unknown = 0;
        private const byte Open = 1;
        private const byte Closed = 2;

        // Follow: a surface within 2 m of the position is the one the person stands on (authoring default).
        private const float FollowGap = 2f;

        // The path search counts a step 1000 and a diagonal one more, so of the paths with the fewest steps, all of them
        // 1 AP a step (10.1), it takes the one with the fewest diagonals.
        private const int StepCost = 1000;
        private const int DiagonalExtra = 1;
        private const int MaxOccupants = 64;

        private static readonly int[] DirX = { 1, -1, 0, 0 };
        private static readonly int[] DirZ = { 0, 0, 1, -1 };

        private readonly float _cell;
        private readonly CombatGridWindow _window = new CombatGridWindow();
        private bool _built;

        private readonly int[] _slotX = new int[Slots];
        private readonly int[] _slotZ = new int[Slots];
        private readonly bool[] _slotRead = new bool[Slots];
        private readonly int[] _slotLevels = new int[Slots];
        private readonly CombatGridSurface[] _surfaces = new CombatGridSurface[Nodes];
        private readonly byte[] _edges = new byte[Slots * EdgesPerSlot];
        private readonly bool[] _coverRead = new bool[Nodes];
        private readonly byte[] _coverLow = new byte[Nodes];
        private readonly byte[] _coverHigh = new byte[Nodes];
        private readonly int[] _reached = new int[Nodes];
        private readonly int[] _queue = new int[Nodes];
        private int _search;

        private readonly RaycastHit[] _hits = new RaycastHit[MaxHits];
        private readonly Collider[] _near = new Collider[MaxHits];

        private readonly int[] _occupants = new int[MaxOccupants];
        private readonly CombatCell[] _occupied = new CombatCell[MaxOccupants];
        private int _occupantCount;

        // The path search's arrays, made on its first use.
        private int[] _g;
        private int[] _f;
        private int[] _parent;
        private int[] _seen;
        private int[] _closed;
        private int[] _heap;
        private int[] _heapIndex;
        private int _heapCount;
        private int _pathSearch;

        public int OverflowColumns;
        public int TerrainCoverRays;

        public CombatGridReader(float cell)
        {
            _cell = cell;
        }

        public float CellSize => _cell;

        /// <summary>The last build's reading, filled again by every build.</summary>
        public CombatGridWindow Window => _window;

        public CombatCell Origin => new CombatCell(_window.X, _window.Z, _window.Level);

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

        public Vector3 Center(CombatCell cell)
        {
            return Center(cell.X, cell.Z, Column(cell.X, cell.Z)[cell.Level].Y);
        }

        /// <summary>
        /// The surfaces of a column, highest first. The span holds until a column 43 cells away along an axis is read
        /// into the same slot, which a window never does.
        /// </summary>
        public ReadOnlySpan<CombatGridSurface> Column(int ix, int iz)
        {
            var slot = Load(ix, iz);
            return new ReadOnlySpan<CombatGridSurface>(_surfaces, slot * MaxLevels, _slotLevels[slot]);
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

        /// <summary>The standable surface of a column nearest a height and within 2 m of it, or −1.</summary>
        public int LevelNear(int ix, int iz, float y)
        {
            var column = Column(ix, iz);
            var best = -1;
            for (var l = 0; l < column.Length; l++)
            {
                if (column[l].Standable && Mathf.Abs(column[l].Y - y) <= FollowGap
                    && (best < 0 || Mathf.Abs(column[l].Y - y) < Mathf.Abs(column[best].Y - y)))
                {
                    best = l;
                }
            }

            return best;
        }

        /// <summary>Centres the window on a cell and reads it: columns, edges, the cells joined to it, and their cover.</summary>
        public CombatGridWindow Build(int cx, int cz, int level)
        {
            var window = _window;
            window.X = cx;
            window.Z = cz;
            window.Level = level;
            window.ReachableCount = 0;
            window.Standable = 0;
            window.MultiLevel = 0;
            window.LowCover = 0;
            window.HighCover = 0;
            _built = true;

            var started = Stopwatch.GetTimestamp();
            for (var dz = -Reach; dz <= Reach; dz++)
            {
                for (var dx = -Reach; dx <= Reach; dx++)
                {
                    Load(cx + dx, cz + dz);
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

            window.ReachableCount = Search(cx, cz, level);
            window.SearchMs = Lap(ref started);

            for (var dz = -Reach; dz <= Reach; dz++)
            {
                for (var dx = -Reach; dx <= Reach; dx++)
                {
                    var slot = Load(cx + dx, cz + dz);
                    var at = slot * MaxLevels;
                    for (var l = 0; l < _slotLevels[slot]; l++)
                    {
                        if (_reached[at + l] != _search)
                        {
                            continue;
                        }

                        var cover = Cover(cx + dx, cz + dz, l);
                        window.LowCover += cover.Low != 0 ? 1 : 0;
                        window.HighCover += cover.High != 0 ? 1 : 0;
                    }
                }
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
            var node = (Load(ix, iz) * MaxLevels) + level;
            if (_coverRead[node])
            {
                return new CombatGridCover { Low = _coverLow[node], High = _coverHigh[node] };
            }

            var cover = default(CombatGridCover);
            var ground = Center(ix, iz, _surfaces[node].Y);
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

            _coverLow[node] = (byte)cover.Low;
            _coverHigh[node] = (byte)cover.High;
            _coverRead[node] = true;
            return cover;
        }

        // ------------------------------------------------------------------------------------------- ICombatGrid

        public bool InBounds(int x, int z) => _built && InWindow(x, z);

        public bool IsWalkable(CombatCell cell) => TryNode(cell, out _);

        public bool IsReachable(CombatCell cell) => TryNode(cell, out var node) && _reached[node] == _search;

        public bool IsReachable(int x, int z, int level) => IsReachable(new CombatCell(x, z, level));

        public bool CanStep(CombatCell from, CombatCell to)
        {
            var dx = to.X - from.X;
            var dz = to.Z - from.Z;
            if ((dx == 0 && dz == 0) || dx < -1 || dx > 1 || dz < -1 || dz > 1 || !IsWalkable(from) || !IsWalkable(to))
            {
                return false;
            }

            return StepOpen(from.X, from.Z, from.Level, to.X, to.Z, to.Level);
        }

        public CoverLevel CoverOn(CombatCell cell, GridSide side) =>
            IsWalkable(cell) ? Cover(cell.X, cell.Z, cell.Level).On(side) : CoverLevel.None;

        public bool CanSee(CombatCell from, CombatCell to)
        {
            if (!IsWalkable(from) || !IsWalkable(to))
            {
                return false;
            }

            var a = Center(from) + (Vector3.up * ChestHeight);
            var b = Center(to) + (Vector3.up * ChestHeight);
            return !Physics.Linecast(a, b, Mask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// The 10.1 tag of what stands between two cells when no step joins them: a person's capsule, as an edge reads
        /// it, is moved level from the first cell toward the second, and the first collider it meets, or one of its
        /// parents, carries the tag. A ledge to climb is met by its face, a low wall or crate by its side.
        /// </summary>
        public bool TryGetEnvironmentAction(CombatCell from, CombatCell to, out IEnvironmentAction action)
        {
            action = null;
            if (!IsWalkable(from) || CanStep(from, to))
            {
                return false;
            }

            var a = Center(from);
            var toward = new Vector3((to.X + 0.5f) * _cell, a.y, (to.Z + 0.5f) * _cell) - a;
            var distance = toward.magnitude;
            if (distance < 0.01f || !Physics.CapsuleCast(
                    a + Vector3.up * (StepOffset + Radius),
                    a + Vector3.up * (Height - Radius),
                    Radius,
                    toward / distance,
                    out var hit,
                    distance,
                    Mask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            for (var t = hit.collider.transform; t != null; t = t.parent)
            {
                if (t.TryGetComponent(out action))
                {
                    return true;
                }
            }

            action = null;
            return false;
        }

        public bool TryLand(CombatCell from, int dx, int dz, float lowest, float highest, out CombatCell landing)
        {
            landing = default;
            var x = from.X + dx;
            var z = from.Z + dz;
            if (!IsWalkable(from) || !InBounds(x, z))
            {
                return false;
            }

            var y = Center(from).y;
            var column = Column(x, z);
            var best = -1;
            for (var l = 0; l < column.Length; l++)
            {
                var rise = column[l].Y - y;
                if (column[l].Standable && rise >= lowest && rise <= highest
                    && (best < 0 || Mathf.Abs(rise) < Mathf.Abs(column[best].Y - y)))
                {
                    best = l;
                }
            }

            landing = new CombatCell(x, z, Mathf.Max(0, best));
            return best >= 0;
        }

        /// <summary>
        /// Reads the window again from the place, after a block in it is gone (S3a.7: a door or crate breached). It costs
        /// what forming the grid costs, so it is for a change of the place, never for a step.
        /// </summary>
        public void Reread()
        {
            Array.Clear(_slotRead, 0, _slotRead.Length);
            if (_built)
            {
                Build(_window.X, _window.Z, _window.Level);
            }
        }

        public int OccupantAt(CombatCell cell)
        {
            for (var i = 0; i < _occupantCount; i++)
            {
                if (_occupied[i] == cell)
                {
                    return _occupants[i];
                }
            }

            return -1;
        }

        public bool Occupy(int occupant, CombatCell cell)
        {
            if (occupant < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(occupant), "an occupant's id is 0 or more");
            }

            var holder = OccupantAt(cell);
            if (holder >= 0 && holder != occupant)
            {
                return false;
            }

            for (var i = 0; i < _occupantCount; i++)
            {
                if (_occupants[i] == occupant)
                {
                    _occupied[i] = cell;
                    return true;
                }
            }

            if (_occupantCount == MaxOccupants)
            {
                throw new InvalidOperationException($"the grid holds at most {MaxOccupants} occupants");
            }

            _occupants[_occupantCount] = occupant;
            _occupied[_occupantCount] = cell;
            _occupantCount++;
            return true;
        }

        public void Vacate(int occupant)
        {
            for (var i = 0; i < _occupantCount; i++)
            {
                if (_occupants[i] == occupant)
                {
                    _occupantCount--;
                    _occupants[i] = _occupants[_occupantCount];
                    _occupied[i] = _occupied[_occupantCount];
                    return;
                }
            }
        }

        public bool TryCellAt(Vector3 position, out CombatCell cell)
        {
            var x = Mathf.FloorToInt(position.x / _cell);
            var z = Mathf.FloorToInt(position.z / _cell);
            var level = InBounds(x, z) ? LevelNear(x, z, position.y) : -1;
            cell = new CombatCell(x, z, Mathf.Max(0, level));
            return level >= 0 && IsWalkable(cell);
        }

        public bool Follow(Vector3 position)
        {
            var x = Mathf.FloorToInt(position.x / _cell);
            var z = Mathf.FloorToInt(position.z / _cell);
            var level = LevelNear(x, z, position.y);
            if (level < 0 && !TryFindStart(position, out x, out z, out level))
            {
                return false;
            }

            if (_built && x == _window.X && z == _window.Z && level == _window.Level)
            {
                return false;
            }

            Build(x, z, level);
            return true;
        }

        public int FindPath(CombatCell from, CombatCell to, List<CombatCell> path)
        {
            path?.Clear();
            if (!TryNode(from, out var start) || !TryNode(to, out var goal))
            {
                return -1;
            }

            if (start == goal)
            {
                return 0;
            }

            if (OccupantAt(to) >= 0)
            {
                return -1;
            }

            if (_g == null)
            {
                _g = new int[Nodes];
                _f = new int[Nodes];
                _parent = new int[Nodes];
                _seen = new int[Nodes];
                _closed = new int[Nodes];
                _heap = new int[Nodes];
                _heapIndex = new int[Nodes];
            }

            _pathSearch++;
            _heapCount = 0;
            _g[start] = 0;
            _parent[start] = -1;
            _seen[start] = _pathSearch;
            Push(start, Estimate(from.X, from.Z, to.X, to.Z));

            var found = false;
            while (_heapCount > 0)
            {
                var node = Pop();
                if (node == goal)
                {
                    found = true;
                    break;
                }

                _closed[node] = _pathSearch;
                var slot = node / MaxLevels;
                var l = node - (slot * MaxLevels);
                var x = _slotX[slot];
                var z = _slotZ[slot];
                for (var dz = -1; dz <= 1; dz++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var nx = x + dx;
                        var nz = z + dz;
                        if ((dx == 0 && dz == 0) || !InWindow(nx, nz))
                        {
                            continue;
                        }

                        var next = Load(nx, nz);
                        var at = next * MaxLevels;
                        for (var n = 0; n < _slotLevels[next]; n++)
                        {
                            var neighbour = at + n;
                            if (!_surfaces[neighbour].Standable || _closed[neighbour] == _pathSearch
                                || !StepOpen(x, z, l, nx, nz, n) || OccupantAt(new CombatCell(nx, nz, n)) >= 0)
                            {
                                continue;
                            }

                            var g = _g[node] + StepCost + (dx != 0 && dz != 0 ? DiagonalExtra : 0);
                            var seen = _seen[neighbour] == _pathSearch;
                            if (seen && g >= _g[neighbour])
                            {
                                continue;
                            }

                            _g[neighbour] = g;
                            _parent[neighbour] = node;
                            var f = g + Estimate(nx, nz, to.X, to.Z);
                            if (seen)
                            {
                                _f[neighbour] = f;
                                SiftUp(_heapIndex[neighbour]);
                            }
                            else
                            {
                                _seen[neighbour] = _pathSearch;
                                Push(neighbour, f);
                            }
                        }
                    }
                }
            }

            if (!found)
            {
                return -1;
            }

            var steps = 0;
            for (var node = goal; node != start; node = _parent[node])
            {
                steps++;
                if (path != null)
                {
                    var slot = node / MaxLevels;
                    path.Add(new CombatCell(_slotX[slot], _slotZ[slot], node - (slot * MaxLevels)));
                }
            }

            path?.Reverse();
            return steps;
        }

        // ------------------------------------------------------------------------------------------- for tools

        /// <summary>The cells the last build reached, into a list the caller keeps; nothing is allocated if it has room.</summary>
        public void CopyReachable(List<CombatCell> cells)
        {
            cells.Clear();
            for (var dz = -Reach; dz <= Reach && _built; dz++)
            {
                for (var dx = -Reach; dx <= Reach; dx++)
                {
                    var x = _window.X + dx;
                    var z = _window.Z + dz;
                    var slot = Slot(x, z);
                    if (!Holds(slot, x, z))
                    {
                        continue;
                    }

                    for (var l = 0; l < _slotLevels[slot]; l++)
                    {
                        if (_reached[(slot * MaxLevels) + l] == _search)
                        {
                            cells.Add(new CombatCell(x, z, l));
                        }
                    }
                }
            }
        }

        /// <summary>The cells the last build reached as <see cref="Pack"/> keys, in a new set: for tools, never on a step.</summary>
        public HashSet<long> ReachableKeys()
        {
            var cells = new List<CombatCell>(_window.ReachableCount);
            CopyReachable(cells);
            var keys = new HashSet<long>();
            foreach (var cell in cells)
            {
                keys.Add(Pack(cell.X, cell.Z, cell.Level));
            }

            return keys;
        }

        // ------------------------------------------------------------------------------------------- the ring

        private static int Mod(int v)
        {
            var m = v % Ring;
            return m < 0 ? m + Ring : m;
        }

        private static int Slot(int x, int z) => Mod(x) + (Mod(z) * Ring);

        private bool Holds(int slot, int x, int z) => _slotRead[slot] && _slotX[slot] == x && _slotZ[slot] == z;

        private bool InWindow(int x, int z) => Mathf.Abs(x - _window.X) <= Reach && Mathf.Abs(z - _window.Z) <= Reach;

        /// <summary>The slot holding a world column, read into it first if another column held it.</summary>
        private int Load(int x, int z)
        {
            var slot = Slot(x, z);
            if (Holds(slot, x, z))
            {
                return slot;
            }

            _slotX[slot] = x;
            _slotZ[slot] = z;
            _slotRead[slot] = true;
            Array.Clear(_edges, slot * EdgesPerSlot, EdgesPerSlot);
            Array.Clear(_coverRead, slot * MaxLevels, MaxLevels);
            Array.Clear(_reached, slot * MaxLevels, MaxLevels);
            ReadColumn(slot, x, z);
            return slot;
        }

        private bool TryNode(CombatCell cell, out int node)
        {
            node = -1;
            if (!InBounds(cell.X, cell.Z) || cell.Level < 0)
            {
                return false;
            }

            var slot = Load(cell.X, cell.Z);
            if (cell.Level >= _slotLevels[slot] || !_surfaces[(slot * MaxLevels) + cell.Level].Standable)
            {
                return false;
            }

            node = (slot * MaxLevels) + cell.Level;
            return true;
        }

        // ------------------------------------------------------------------------------------------- reading

        private bool CoverRay(Vector3 origin, Vector3 direction)
        {
            var depth = (CoverFar - CoverNear) * _cell;
            var centre = origin + direction * ((CoverNear * _cell) + depth * 0.5f);
            var across = Vector3.Cross(Vector3.up, direction);
            var half = (direction * (depth * 0.5f)) + (across * (_cell * CoverHalfWidth));
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

        private void ReadColumn(int slot, int x, int z)
        {
            var origin = Center(x, z, TopY);
            var count = Physics.RaycastNonAlloc(origin, Vector3.down, _hits, TopY, Mask, QueryTriggerInteraction.Ignore);
            if (count == MaxHits)
            {
                OverflowColumns++;
            }

            // Highest first, by insertion: Array.Sort with a comparer may allocate.
            for (var i = 1; i < count; i++)
            {
                var hit = _hits[i];
                var j = i - 1;
                while (j >= 0 && _hits[j].point.y < hit.point.y)
                {
                    _hits[j + 1] = _hits[j];
                    j--;
                }

                _hits[j + 1] = hit;
            }

            var at = slot * MaxLevels;
            var levels = 0;
            for (var i = 0; i < count && levels < MaxLevels; i++)
            {
                if (levels > 0 && Mathf.Abs(_surfaces[at + levels - 1].Y - _hits[i].point.y) < 0.02f)
                {
                    continue;
                }

                _surfaces[at + levels] = ReadSurface(_hits[i]);
                levels++;
            }

            _slotLevels[slot] = levels;
        }

        private static CombatGridSurface ReadSurface(RaycastHit hit)
        {
            var surface = new CombatGridSurface { Y = hit.point.y, Reason = CombatGridBlock.None, Standable = true };
            // Water is the ground under the sea, so only the terrain below Sea Level reads as water: an interior's
            // floor stands near 0 m and a quay's steps may dip below 24 m, and both are floors (2026-11-06).
            if (hit.point.y < SeaLevel && hit.collider is TerrainCollider)
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
            var a = Load(ax, az);
            var b = Load(bx, bz);
            for (var la = 0; la < _slotLevels[a]; la++)
            {
                if (!_surfaces[(a * MaxLevels) + la].Standable)
                {
                    continue;
                }

                for (var lb = 0; lb < _slotLevels[b]; lb++)
                {
                    if (_surfaces[(b * MaxLevels) + lb].Standable)
                    {
                        EdgeBetween(ax, az, la, bx, bz, lb);
                    }
                }
            }
        }

        /// <param name="direction">0 is +X and 1 is +Z; the edge is kept by the lower cell.</param>
        private bool Edge(int ax, int az, int la, int direction, int lb, int bx, int bz)
        {
            var slot = Load(ax, az);
            var index = (((((slot * MaxLevels) + la) * 2) + direction) * MaxLevels) + lb;
            var known = _edges[index];
            if (known != Unknown)
            {
                return known == Open;
            }

            var a = Center(ax, az, _surfaces[(slot * MaxLevels) + la].Y);
            var b = Center(bx, bz, _surfaces[(Load(bx, bz) * MaxLevels) + lb].Y);
            var delta = b - a;
            var distance = delta.magnitude;
            var open = Mathf.Abs(delta.y) <= _cell * Mathf.Tan(SlopeLimitDegrees * Mathf.Deg2Rad)
                       && !Physics.CapsuleCast(
                           a + Vector3.up * (StepOffset + Radius),
                           a + Vector3.up * (Height - Radius),
                           Radius,
                           delta / distance,
                           distance,
                           Mask,
                           QueryTriggerInteraction.Ignore);
            _edges[index] = open ? Open : Closed;
            return open;
        }

        /// <summary>A step to a cell beside: across a side by its edge; across a corner only when both side routes
        /// reach the same surface, so no corner is cut.</summary>
        private bool StepOpen(int x, int z, int l, int nx, int nz, int n)
        {
            if (nx == x || nz == z)
            {
                return EdgeBetween(x, z, l, nx, nz, n);
            }

            return Via(x, z, l, nx, z, nx, nz, n) && Via(x, z, l, x, nz, nx, nz, n);
        }

        private int Search(int cx, int cz, int level)
        {
            _search++;
            var head = 0;
            var tail = 0;
            var start = (Load(cx, cz) * MaxLevels) + level;
            _reached[start] = _search;
            _queue[tail++] = start;

            while (head < tail)
            {
                var node = _queue[head++];
                var slot = node / MaxLevels;
                var l = node - (slot * MaxLevels);
                var x = _slotX[slot];
                var z = _slotZ[slot];
                for (var d = 0; d < 4; d++)
                {
                    Visit(x, z, l, x + DirX[d], z + DirZ[d], ref tail);
                }

                for (var dx = -1; dx <= 1; dx += 2)
                {
                    for (var dz = -1; dz <= 1; dz += 2)
                    {
                        Visit(x, z, l, x + dx, z + dz, ref tail);
                    }
                }
            }

            return tail;
        }

        private void Visit(int x, int z, int l, int nx, int nz, ref int tail)
        {
            if (!InWindow(nx, nz))
            {
                return;
            }

            var slot = Load(nx, nz);
            var at = slot * MaxLevels;
            for (var n = 0; n < _slotLevels[slot]; n++)
            {
                if (_surfaces[at + n].Standable && _reached[at + n] != _search && StepOpen(x, z, l, nx, nz, n))
                {
                    _reached[at + n] = _search;
                    _queue[tail++] = at + n;
                }
            }
        }

        private bool Via(int ax, int az, int la, int mx, int mz, int bx, int bz, int lb)
        {
            var middle = Load(mx, mz);
            for (var m = 0; m < _slotLevels[middle]; m++)
            {
                if (_surfaces[(middle * MaxLevels) + m].Standable
                    && EdgeBetween(ax, az, la, mx, mz, m)
                    && EdgeBetween(mx, mz, m, bx, bz, lb))
                {
                    return true;
                }
            }

            return false;
        }

        private void Summarise(CombatGridWindow window)
        {
            var hash = 14695981039346656037UL;
            for (var dz = -Reach; dz <= Reach; dz++)
            {
                for (var dx = -Reach; dx <= Reach; dx++)
                {
                    var slot = Load(window.X + dx, window.Z + dz);
                    var at = slot * MaxLevels;
                    var standable = 0;
                    for (var l = 0; l < _slotLevels[slot]; l++)
                    {
                        hash = Mix(hash, Mathf.RoundToInt(_surfaces[at + l].Y * 1000f));
                        hash = Mix(hash, _surfaces[at + l].Standable ? 1 : 0);
                        hash = Mix(hash, _reached[at + l] == _search ? 1 : 0);
                        standable += _surfaces[at + l].Standable ? 1 : 0;
                    }

                    window.Standable += standable > 0 ? 1 : 0;
                    window.MultiLevel += standable > 1 ? 1 : 0;
                }
            }

            window.Hash = hash;
        }

        // ------------------------------------------------------------------------------------------- the path search's heap

        private int Estimate(int x, int z, int tx, int tz) => StepCost * Math.Max(Math.Abs(tx - x), Math.Abs(tz - z));

        private void Push(int node, int f)
        {
            _f[node] = f;
            _heap[_heapCount] = node;
            _heapIndex[node] = _heapCount;
            _heapCount++;
            SiftUp(_heapCount - 1);
        }

        private int Pop()
        {
            var top = _heap[0];
            _heapCount--;
            if (_heapCount > 0)
            {
                _heap[0] = _heap[_heapCount];
                _heapIndex[_heap[0]] = 0;
                SiftDown(0);
            }

            return top;
        }

        /// <summary>Lower f first; of equal f, the deeper node, which keeps the search near a straight line.</summary>
        private bool Before(int a, int b) => _f[a] < _f[b] || (_f[a] == _f[b] && _g[a] > _g[b]);

        private void SiftUp(int i)
        {
            var node = _heap[i];
            while (i > 0)
            {
                var parent = (i - 1) >> 1;
                if (!Before(node, _heap[parent]))
                {
                    break;
                }

                _heap[i] = _heap[parent];
                _heapIndex[_heap[i]] = i;
                i = parent;
            }

            _heap[i] = node;
            _heapIndex[node] = i;
        }

        private void SiftDown(int i)
        {
            var node = _heap[i];
            while (true)
            {
                var child = (2 * i) + 1;
                if (child >= _heapCount)
                {
                    break;
                }

                if (child + 1 < _heapCount && Before(_heap[child + 1], _heap[child]))
                {
                    child++;
                }

                if (!Before(_heap[child], node))
                {
                    break;
                }

                _heap[i] = _heap[child];
                _heapIndex[_heap[i]] = i;
                i = child;
            }

            _heap[i] = node;
            _heapIndex[node] = i;
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
}
