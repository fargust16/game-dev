#if UNITY_EDITOR
using Den.Tools.Tasks;
using MapMagic.Terrains;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Shared wait helper for every Overworld editor tool that reads a MapMagic tile's generated
    /// data (terrain height, or any <see cref="MapMagic.Terrains.DirectMatricesHolder"/> channel)
    /// right after triggering generation. MapMagic's Apply step - which actually populates
    /// DirectMatricesHolder, see DirectMatricesOutput200.ApplyData.Apply in TexturesOut.cs - runs
    /// through Den.Tools.Tasks.CoroutineManager, normally pumped once per frame by
    /// MapMagicObject.EditorUpdate() via EditorApplication.update. That event never fires while an
    /// Editor menu command is still on the call stack, so sampling immediately after
    /// Refresh()/Pin() without waiting here can read a still-incomplete or stale (previous
    /// generation's) holder - harmless when every channel already existed (a slightly late "height"
    /// value), but a hard "channel not found" exception the moment a caller adds a channel that
    /// didn't exist in whatever stale holder is still attached. Background generation itself
    /// (Den.Tools.Tasks.ThreadManager) keeps progressing on its own real threads regardless; only
    /// this main-thread Apply queue needs help.
    /// </summary>
    public static class MapMagicTileSync
    {
        public static bool WaitForTileReady(TerrainTile tile, float timeoutSeconds = 30f)
        {
            var start = System.DateTime.UtcNow;
            while (tile.main == null || !tile.Ready)
            {
                CoroutineManager.Update();
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
