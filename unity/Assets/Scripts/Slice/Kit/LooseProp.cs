using UnityEngine;

namespace GameDev.Slice.Kit
{
    /// <summary>
    /// A prop with mass and gravity (kit standard §4, loose props; owner, 2026-11-06). It sleeps from the scene's start,
    /// so it lies still until something touches it. A set's thing leaves its set when the scene starts, since a body
    /// must not ride under another moving body.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class LooseProp : MonoBehaviour
    {
        [SerializeField] private bool thing;

        /// <summary>A set's thing (a bottle, a book), as against the block it stood on.</summary>
        public bool IsThing => thing;

        public void Configure(bool isThing) => thing = isThing;

        private void Awake()
        {
            if (!thing)
            {
                return;
            }

            var block = GetComponentInParent<KitBlock>();
            transform.SetParent(block != null ? block.transform.parent : null, true);
        }

        private void Start() => GetComponent<Rigidbody>().Sleep();
    }
}
