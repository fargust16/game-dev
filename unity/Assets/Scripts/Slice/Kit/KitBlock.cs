using UnityEngine;

namespace GameDev.Slice.Kit
{
    /// <summary>
    /// Marks the root of one kit block (SLICE_MODULAR_KIT_STANDARD). The layout check reads every block in the open
    /// scenes: placement on the grid, layer, colliders, and overlap with other blocks.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KitBlock : MonoBehaviour
    {
    }
}
