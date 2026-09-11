using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// Marks a Unity Terrain as one tile of the overworld grid, whichever tool created it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tile discovery used to key off Vista's own <c>TerrainTile</c> component, which made
    /// <see cref="OverworldTerrainAccess"/> — and therefore every downstream planner that reads
    /// height, slope or bounds — unable to see a terrain Vista had not baked. That was the single
    /// hard Vista type reference left in the runtime tree.
    /// </para>
    /// <para>
    /// This component carries no generation data and no behaviour. It says only "this Terrain is
    /// part of the overworld grid, and it sits at this tile coordinate". Vista's bake stamps it on
    /// its own tiles, and so does <see cref="OverworldTerrainGrid"/>, so both paths are discovered
    /// by one rule.
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
