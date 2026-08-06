using UnityEngine;

namespace GameDev.Grid
{
    /// <summary>
    /// Runtime square grid for S0+. Design: 10.1 Grid World and Environment Actions.
    /// Cell data is serialized so Play Mode keeps walkability painted by the greybox builder.
    /// </summary>
    public sealed class GridMap : MonoBehaviour
    {
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private int width = 36;
        [SerializeField] private int height = 28;
        [SerializeField] private Vector3 origin = Vector3.zero;
        [SerializeField] private bool allowDiagonalMove = true;
        [SerializeField] private bool diagonalCostsFifteen = true;
        [SerializeField] private GridCell[] cells;

        public float CellSize => cellSize;
        public int Width => width;
        public int Height => height;
        public Vector3 Origin => origin;
        public bool AllowDiagonalMove => allowDiagonalMove;
        public bool DiagonalCostsFifteen => diagonalCostsFifteen;
        public bool HasCells => cells != null && cells.Length == width * height;

        private void Awake()
        {
            EnsureCells();
        }

        public void Configure(int newWidth, int newHeight, float newCellSize, Vector3 newOrigin)
        {
            width = Mathf.Max(1, newWidth);
            height = Mathf.Max(1, newHeight);
            cellSize = Mathf.Max(0.25f, newCellSize);
            origin = newOrigin;
            AllocateDefaultWalkable();
        }

        public void EnsureCells()
        {
            if (!HasCells)
            {
                AllocateDefaultWalkable();
            }
        }

        public void AllocateDefaultWalkable()
        {
            cells = new GridCell[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var coord = new GridCoord(x, y);
                    cells[Index(coord)] = new GridCell
                    {
                        coord = coord,
                        height = 0,
                        flags = GridCellFlags.Walkable,
                        occupied = false
                    };
                }
            }
        }

        public bool InBounds(GridCoord coord) =>
            coord.x >= 0 && coord.y >= 0 && coord.x < width && coord.y < height;

        public ref GridCell GetCell(GridCoord coord)
        {
            return ref cells[Index(coord)];
        }

        public bool TryGetCell(GridCoord coord, out GridCell cell)
        {
            if (!InBounds(coord) || !HasCells)
            {
                cell = default;
                return false;
            }

            cell = cells[Index(coord)];
            return true;
        }

        public bool IsWalkable(GridCoord coord)
        {
            if (!TryGetCell(coord, out var cell))
            {
                return false;
            }

            return (cell.flags & GridCellFlags.Walkable) != 0 && !cell.occupied;
        }

        public void SetFlags(GridCoord coord, GridCellFlags flags, bool walkable = true)
        {
            if (!InBounds(coord) || !HasCells)
            {
                return;
            }

            ref var cell = ref GetCell(coord);
            cell.flags = flags;
            if (walkable)
            {
                cell.flags |= GridCellFlags.Walkable;
            }
            else
            {
                cell.flags &= ~GridCellFlags.Walkable;
            }
        }

        public void SetHeight(GridCoord coord, int cellHeight)
        {
            if (!InBounds(coord) || !HasCells)
            {
                return;
            }

            GetCell(coord).height = cellHeight;
        }

        public Vector3 CoordToWorldCenter(GridCoord coord)
        {
            return origin + new Vector3((coord.x + 0.5f) * cellSize, 0f, (coord.y + 0.5f) * cellSize);
        }

        public GridCoord WorldToCoord(Vector3 world)
        {
            var local = world - origin;
            var x = Mathf.FloorToInt(local.x / cellSize);
            var y = Mathf.FloorToInt(local.z / cellSize);
            return new GridCoord(x, y);
        }

        private int Index(GridCoord coord) => coord.y * width + coord.x;

        private void OnValidate()
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            cellSize = Mathf.Max(0.25f, cellSize);
        }
    }
}
