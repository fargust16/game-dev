using UnityEngine;

namespace GameDev.Grid
{
    [RequireComponent(typeof(GridMap))]
    public sealed class GridDebugDrawer : MonoBehaviour
    {
        [SerializeField] private bool drawInPlayMode = true;
        [SerializeField] private Color walkableColor = new Color(0.2f, 0.7f, 0.3f, 0.25f);
        [SerializeField] private Color blockedColor = new Color(0.7f, 0.15f, 0.1f, 0.35f);
        [SerializeField] private Color specialColor = new Color(0.2f, 0.45f, 0.9f, 0.4f);

        private GridMap _grid;

        private void Awake()
        {
            _grid = GetComponent<GridMap>();
        }

        private void OnDrawGizmos()
        {
            if (!drawInPlayMode && Application.isPlaying)
            {
                return;
            }

            if (_grid == null)
            {
                _grid = GetComponent<GridMap>();
            }

            if (_grid == null || _grid.Width <= 0)
            {
                return;
            }

            // Ensure cells exist when drawing in edit mode after builder ran in play mode only.
            if (!Application.isPlaying)
            {
                return;
            }

            for (var y = 0; y < _grid.Height; y++)
            {
                for (var x = 0; x < _grid.Width; x++)
                {
                    var coord = new GridCoord(x, y);
                    if (!_grid.TryGetCell(coord, out var cell))
                    {
                        continue;
                    }

                    var center = _grid.CoordToWorldCenter(coord);
                    center.y = cell.height * _grid.CellSize * 0.5f + 0.02f;
                    var size = new Vector3(_grid.CellSize * 0.95f, 0.02f, _grid.CellSize * 0.95f);

                    var walkable = (cell.flags & GridCellFlags.Walkable) != 0 && !cell.occupied;
                    var special = (cell.flags & (GridCellFlags.Vaultable | GridCellFlags.Window | GridCellFlags.Climbable | GridCellFlags.Stairs | GridCellFlags.Door)) != 0;

                    if (!walkable)
                    {
                        Gizmos.color = blockedColor;
                    }
                    else if (special)
                    {
                        Gizmos.color = specialColor;
                    }
                    else
                    {
                        Gizmos.color = walkableColor;
                    }

                    Gizmos.DrawCube(center, size);
                }
            }
        }
    }
}
