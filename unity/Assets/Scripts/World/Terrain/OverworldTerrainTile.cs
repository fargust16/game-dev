using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// Marks a Unity Terrain as one tile of the overworld grid, whichever tool created it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This component carries no generation data and no behaviour. It says only "this Terrain is
    /// part of the overworld grid, and it sits at this tile coordinate". <see cref="OverworldTerrainGrid"/>
    /// stamps it on every tile, so <see cref="OverworldTerrainAccess"/> and every downstream planner
    /// discover tiles by one rule.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class OverworldTerrainTile : MonoBehaviour
    {
        [SerializeField] private int tileX;
        [SerializeField] private int tileZ;

        /// <summary>Tile column along world +X.</summary>
        public int TileX => tileX;

        /// <summary>Tile row along world +Z.</summary>
        public int TileZ => tileZ;

        public void SetCoord(int x, int z)
        {
            tileX = x;
            tileZ = z;
        }

        /// <summary>Adds (or updates) the marker on a terrain object.</summary>
        public static OverworldTerrainTile Stamp(GameObject terrainObject, int x, int z)
        {
            if (terrainObject == null)
            {
                return null;
            }

            var marker = terrainObject.GetComponent<OverworldTerrainTile>();
            if (marker == null)
            {
                marker = terrainObject.AddComponent<OverworldTerrainTile>();
            }

            marker.SetCoord(x, z);
            return marker;
        }
    }
}
