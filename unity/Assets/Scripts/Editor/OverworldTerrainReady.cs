#if UNITY_EDITOR
using GameDev.World.Terrain;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Shared wait helper for Overworld editor tools that need a ready Unity Terrain.
    /// </summary>
    public static class OverworldTerrainReady
    {
        public static bool WaitForTerrainReady(float timeoutSeconds = 30f)
        {
            var start = System.DateTime.UtcNow;
            while (OverworldTerrainAccess.FindTerrain() == null)
            {
                System.Threading.Thread.Sleep(1);

                if ((System.DateTime.UtcNow - start).TotalSeconds > timeoutSeconds)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
#endif
