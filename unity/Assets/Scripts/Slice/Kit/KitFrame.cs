using UnityEngine;

namespace GameDev.Slice.Kit
{
    /// <summary>
    /// A street laid at 45° (Veilmouth C1 §12; kit standard §1): the kit blocks under it keep the 1 m grid and 90° turns
    /// in the frame's own axes. The frame itself stands on a node of the world's 1 m grid at a multiple of 45° about Y.
    /// The Combat Grid stays world-aligned and reads what is built here as it reads anything else.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KitFrame : MonoBehaviour
    {
    }
}
