using UnityEngine;

namespace GameDev.Grid
{
    /// <summary>
    /// Square-grid defaults for Vertical Slice S0+.
    /// Design: 10.1 Grid World and Environment Actions.
    /// </summary>
    [CreateAssetMenu(menuName = "GameDev/Grid/Grid Settings", fileName = "GridSettings")]
    public sealed class GridSettings : ScriptableObject
    {
        [Min(0.25f)]
        public float cellSize = 1f;

        public bool allowDiagonalMove = true;

        [Tooltip("If true, diagonal costs 1.5 cells of Move; else 1.")]
        public bool diagonalCostsFifteen = true;
    }
}
