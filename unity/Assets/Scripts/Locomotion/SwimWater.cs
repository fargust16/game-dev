using System.Collections.Generic;
using UnityEngine;

namespace GameDev.Locomotion
{
    /// <summary>
    /// A water surface the character swims in (10.1 *Swimming*, AN5): its level is the transform's height, over the
    /// ground plan of its renderer, or everywhere when it has none. Only an enabled surface counts, so a scene kept
    /// switched off behind an interior has no water.
    /// </summary>
    public sealed class SwimWater : MonoBehaviour
    {
        private static readonly List<SwimWater> Active = new List<SwimWater>();

        private Renderer _renderer;

        /// <summary>The surface's height, m.</summary>
        public float Level => transform.position.y;

        private void OnEnable()
        {
            _renderer = GetComponentInChildren<Renderer>();
            Active.Add(this);
        }

        private void OnDisable() => Active.Remove(this);

        /// <summary>The highest water surface over the point's ground plan, if any.</summary>
        public static bool LevelAt(Vector3 point, out float level)
        {
            level = float.MinValue;
            foreach (var water in Active)
            {
                if (water._renderer != null)
                {
                    var b = water._renderer.bounds;
                    if (point.x < b.min.x || point.x > b.max.x || point.z < b.min.z || point.z > b.max.z)
                    {
                        continue;
                    }
                }

                level = Mathf.Max(level, water.Level);
            }

            return level > float.MinValue;
        }
    }
}
