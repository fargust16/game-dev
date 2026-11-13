using UnityEngine;

namespace GameDev.Slice.People
{
    /// <summary>
    /// A person's body that lies fallen (10.2 §5c): it marks the body, keeps the fall it fell by, and lays it down again
    /// whenever its scene is switched on behind an interior, since a controller switched off forgets its Down. A Suspend
    /// Save reads the mark, which an inactive controller cannot give (17 X.3).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FallenBody : MonoBehaviour
    {
        private static readonly int DownHash = Animator.StringToHash("Down");
        private static readonly int DeathHash = Animator.StringToHash("Death");

        [SerializeField] private float death;
        private bool _lay;

        /// <summary>The death clip it fell by, the controller's Death.</summary>
        public float Death => death;

        /// <summary>Marks <paramref name="body"/> fallen, or takes the mark off one that stands again.</summary>
        public static void Mark(Transform body, bool down, float deathClip)
        {
            if (body == null)
            {
                return;
            }

            var mark = body.GetComponent<FallenBody>();
            if (!down)
            {
                if (mark != null)
                {
                    Destroy(mark);
                }

                return;
            }

            mark ??= body.gameObject.AddComponent<FallenBody>();
            mark.death = deathClip;
        }

        /// <summary>Lies as it fell at once: out of its walk, its colliders off, its controller at the end of its fall.</summary>
        public void Lay()
        {
            _lay = false;
            if (TryGetComponent<TownsfolkWalker>(out var walker))
            {
                walker.enabled = false;
            }

            foreach (var part in GetComponents<Collider>())
            {
                part.enabled = false;
            }

            var animator = GetComponentInChildren<Animator>();
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            {
                return;
            }

            foreach (var p in animator.parameters)
            {
                if (p.nameHash == DeathHash)
                {
                    animator.SetFloat(DeathHash, death);
                }
            }

            animator.SetBool(DownHash, true);
            animator.Update(0f);
            for (var i = 0; i < 4; i++)
            {
                animator.Update(1f); // through the transition and the whole fall
            }
        }

        // Switched on again with its scene: its controller wakes in its first state, so the body is laid down once more
        // on the next frame, after every component of the scene has woken.
        private void OnEnable() => _lay = true;

        private void LateUpdate()
        {
            if (_lay)
            {
                Lay();
            }
        }
    }
}
