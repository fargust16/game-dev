using System;
using GameDev.Locomotion;
using GameDev.Slice.Gear;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDev.Slice.Creation
{
    /// <summary>
    /// Who the character is (07 *Creation choices in V1*; S5b.1): the name, the sex, which changes no rule, only the
    /// model and the words addressed to the character, and the body's id. Kept in the save's player.character section.
    /// </summary>
    [Serializable]
    public sealed class CharacterIdentity
    {
        public string name = string.Empty;
        public bool female;
        public string look = string.Empty;
    }

    /// <summary>
    /// The player's body changed under its root (S5b.1): the chosen look's Model, with its controller and the tools in its
    /// hand, takes the old one's place, and the locomotion and the held weapons read the new Animator. Those that find
    /// the player's Animator when they need it (the work, the arms, the aim, the fights) find the new one.
    /// </summary>
    public static class PlayerBody
    {
        public const string ModelName = "Model";

        /// <summary>Wears <paramref name="look"/>'s body on <paramref name="player"/>; false when there is nothing to wear.</summary>
        public static bool Wear(PlayerLocomotion player, PlayerLook look)
        {
            var body = look?.Body;
            if (player == null || body == null)
            {
                return false;
            }

            var old = player.transform.Find(ModelName);
            var worn = Object.Instantiate(body, player.transform, false);
            worn.name = ModelName;
            worn.transform.SetSiblingIndex(0);
            if (old != null)
            {
                // Switched off at once, so nothing finds its Animator before it is gone at the frame's end.
                old.gameObject.SetActive(false);
                Object.Destroy(old.gameObject);
            }

            player.Rebind();
            var held = player.GetComponent<HeldWeapons>();
            if (held != null)
            {
                held.Rebind();
            }

            return true;
        }
    }
}
