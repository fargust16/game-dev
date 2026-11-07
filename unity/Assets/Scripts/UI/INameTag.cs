using UnityEngine;

namespace GameDev.UI
{
    /// <summary>Someone the HUD names when the player is near (S1.5).</summary>
    public interface INameTag
    {
        string DisplayName { get; }

        /// <summary>Where the name hangs, world position, about a head above the feet.</summary>
        Vector3 TagPosition { get; }
    }
}
