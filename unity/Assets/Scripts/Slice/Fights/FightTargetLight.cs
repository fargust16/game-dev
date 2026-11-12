using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// The owner's play test of 2026-11-12: in a fight the enemy under the cursor is lit, so the player knows whom an attack
    /// or a spell will hit. Its body, and what it holds, take a thin outline (GameDev/Slice/Target Outline) as one more
    /// material on each mesh renderer, which leaves the body's own materials as they are; a mesh of several parts takes it
    /// on its last part only.
    /// </summary>
    public sealed class FightTargetLight
    {
        private readonly Material _material;
        private readonly List<Renderer> _lit = new List<Renderer>();

        /// <param name="shader">The outline's shader; none leaves every target unlit.</param>
        public FightTargetLight(Shader shader)
        {
            if (shader != null)
            {
                _material = new Material(shader) { name = "Fight Target Outline" };
            }
        }

        /// <summary>The body lit now, or null.</summary>
        public Transform Body { get; private set; }

        /// <summary>Lights <paramref name="body"/> alone; null lights nobody.</summary>
        public void Show(Transform body)
        {
            if (body == Body)
            {
                return;
            }

            Hide();
            if (body == null || _material == null)
            {
                return;
            }

            Body = body;
            foreach (var r in body.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !(r is SkinnedMeshRenderer || r is MeshRenderer))
                {
                    continue;
                }

                var materials = r.sharedMaterials;
                Array.Resize(ref materials, materials.Length + 1);
                materials[materials.Length - 1] = _material;
                r.sharedMaterials = materials;
                _lit.Add(r);
            }
        }

        public void Hide()
        {
            foreach (var r in _lit)
            {
                if (r == null)
                {
                    continue;
                }

                var materials = new List<Material>(r.sharedMaterials);
                var at = materials.LastIndexOf(_material);
                if (at >= 0)
                {
                    materials.RemoveAt(at);
                    r.sharedMaterials = materials.ToArray();
                }
            }

            _lit.Clear();
            Body = null;
        }

        public void Dispose()
        {
            Hide();
            if (_material != null)
            {
                Object.Destroy(_material);
            }
        }
    }
}
