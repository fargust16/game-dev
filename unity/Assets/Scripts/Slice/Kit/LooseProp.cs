using UnityEngine;

namespace GameDev.Slice.Kit
{
    /// <summary>
    /// A prop with mass and gravity (kit standard §4, loose props; owner, 2026-11-06). It sleeps from the scene's start,
    /// so it lies still until something touches it. A set's thing leaves its set on its first Start, since a body must
    /// not ride under another moving body: not on Awake, which runs as soon as a builder instantiates the set and before
    /// it puts the set in place, and left the things hanging where the set was made (owner's fourth play test,
    /// 2026-11-06: things hung in the air in a dwelling).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class LooseProp : MonoBehaviour
    {
        [SerializeField] private bool thing;

        /// <summary>A set's thing (a bottle, a book), as against the block it stood on.</summary>
        public bool IsThing => thing;

        public void Configure(bool isThing) => thing = isThing;

        private void Start()
        {
            if (thing)
            {
                var block = GetComponentInParent<KitBlock>();
                if (block != null)
                {
                    transform.SetParent(block.transform.parent, true);
                }
            }

            GetComponent<Rigidbody>().Sleep();
        }
    }
}
