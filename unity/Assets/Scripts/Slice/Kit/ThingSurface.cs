using UnityEngine;

namespace GameDev.Slice.Kit
{
    /// <summary>
    /// The colliders a still set (a shelf, a counter, a chest of drawers) holds its loose things on (kit standard §4).
    /// The layout check does not count them, so a shelf stays a NoGrid block.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThingSurface : MonoBehaviour
    {
    }
}
