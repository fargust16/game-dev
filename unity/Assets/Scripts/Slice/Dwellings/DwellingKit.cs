using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.Slice.Dwellings
{
    /// <summary>
    /// The kit blocks a dwelling is built from, with their footprints and solid bodies as the editor measured them
    /// (immutable data; written by <c>VeilmouthDwellings</c> from <c>SliceKitCatalogue</c>). The Dwelling scene builds
    /// its rooms from these prefabs at runtime, so they are referenced here rather than looked up by path.
    /// </summary>
    [CreateAssetMenu(menuName = "GameDev/Slice/Dwelling Kit", fileName = "DwellingKit")]
    public sealed class DwellingKit : ScriptableObject, IDwellingFootprints
    {
        [Serializable]
        public sealed class Block
        {
            public string key;
            public GameObject prefab;
            public int width;
            public int depth;
            public bool solid;
            [Tooltip("The solid body in the footprint's metres: x along local X, y along local Z.")]
            public Rect body;

            /// <summary>The height of what is seen, m (a piece under a window, or under a hung one).</summary>
            public float height;
        }

        /// <summary>
        /// A window wall's glass, measured in the editor (VICTORIAN_INTERIORS_PLAN §5): its rectangle in metres from the
        /// segment's middle along the wall and up from the floor, and how far into the room from the wall's line it stands.
        /// </summary>
        [Serializable]
        public sealed class Glass
        {
            public string key;
            public float u0, u1, y0, y1, depth;
        }

        [SerializeField] private List<Block> blocks = new List<Block>();

        [Tooltip("The pack's fire, lit in each fireplace.")]
        [SerializeField] private GameObject fire;

        [Tooltip("Our own unlit material of a window's lit pane; its colour follows the sky.")]
        [SerializeField] private Material pane;

        [SerializeField] private List<Glass> glass = new List<Glass>();

        public GameObject Fire => fire;

        public Material Pane => pane;

        public Glass GlassOf(string key) => glass.Find(g => g.key == key);

        public void SetLight(GameObject fireSource, Material paneMaterial, IEnumerable<Glass> windows)
        {
            fire = fireSource;
            pane = paneMaterial;
            glass = new List<Glass>(windows);
        }

        private Dictionary<string, Block> _byKey;

        public IReadOnlyList<Block> Blocks => blocks;

        public void SetBlocks(IEnumerable<Block> source)
        {
            blocks = new List<Block>(source);
            _byKey = null;
        }

        public Block Get(string key)
        {
            if (_byKey == null)
            {
                _byKey = new Dictionary<string, Block>();
                foreach (var b in blocks)
                {
                    _byKey[b.key] = b;
                }
            }

            return _byKey.TryGetValue(key, out var block) ? block : null;
        }

        public bool TryGet(string key, out Footprint footprint)
        {
            var b = Get(key);
            footprint = b == null ? default : new Footprint(b.width, b.depth, b.solid, b.body, b.height);
            return b != null && b.prefab != null;
        }

        private void OnValidate()
        {
            _byKey = null;
        }
    }
}
