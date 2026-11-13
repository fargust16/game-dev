using System;
using UnityEngine;

namespace GameDev.Slice.Creation
{
    /// <summary>
    /// One body the player may take at creation (07 *Creation choices in V1*; S5b.1): a player prefab whose Model, the
    /// body, its controller and the tools in its right hand, is worn under the player's root. The owner's own player
    /// models come from Tripo; until then the slice's present Tripo people are offered (owner, 2026-11-13, S5-4).
    /// </summary>
    [Serializable]
    public sealed class PlayerLook
    {
        [Tooltip("Kept in the save; never shown.")]
        public string id;

        public bool female;

        [Tooltip("A player prefab: its child Model is the body worn.")]
        public GameObject player;

        /// <summary>The body to wear: the prefab's child Model, or null.</summary>
        public GameObject Body => player != null ? player.transform.Find(PlayerBody.ModelName)?.gameObject : null;
    }
}
