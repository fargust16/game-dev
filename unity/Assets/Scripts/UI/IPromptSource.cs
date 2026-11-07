using UnityEngine;

namespace GameDev.UI
{
    /// <summary>Something in the scene the player can use with Interact; the HUD shows its prompt while the player is in reach.</summary>
    public interface IPromptSource
    {
        /// <summary>The prompt, e.g. "E  Read", or null when <paramref name="player"/> is out of reach.</summary>
        string PromptFor(Vector3 player);
    }
}
