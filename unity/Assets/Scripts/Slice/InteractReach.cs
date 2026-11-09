using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>
    /// Whether the player can use something in the scene: within reach on the same level and in sight. A person could be
    /// asked from the landing above the taproom (owner's play test VI-5, remark 2), so walls, floors and ceilings block
    /// the sight line, taken at chest height; a counter, which is lower, does not.
    /// </summary>
    public static class InteractReach
    {
        /// <summary>Authoring default, m: a thing more than this above or below the player stands on another level.</summary>
        private const float SameLevel = 1.5f;

        /// <summary>Authoring default, m: the sight line's height over the feet, above a counter's 1.05 m.</summary>
        private const float Chest = 1.4f;

        public static bool Within(Transform thing, Vector3 position, float radius)
        {
            var offset = position - thing.position;
            if (offset.y < -SameLevel || offset.y > SameLevel)
            {
                return false;
            }

            offset.y = 0f;
            if (offset.sqrMagnitude > radius * radius)
            {
                return false;
            }

            // The thing's own body (a notice board, a person) is the last on the line, so hitting it first means
            // nothing stands between.
            var mask = LayerMask.GetMask("Ground", "Structure");
            return mask == 0
                   || !Physics.Linecast(position + (Vector3.up * Chest), thing.position + (Vector3.up * Chest), out var hit, mask, QueryTriggerInteraction.Ignore)
                   || hit.collider.transform.IsChildOf(thing);
        }
    }
}
