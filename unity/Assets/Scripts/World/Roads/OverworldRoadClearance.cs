using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Roads
{
    /// <summary>
    /// Stores densified road centerline samples for vegetation clearance. Roads themselves are
    /// terrain-only (carve + dirt paint); this component replaces mesh sampling.
    /// </summary>
    public sealed class OverworldRoadClearance : MonoBehaviour
    {
        [SerializeField] private List<Vector2> samples = new List<Vector2>();

        public IReadOnlyList<Vector2> Samples => samples;

        public void SetSamples(IEnumerable<Vector2> points)
        {
            samples.Clear();
            if (points == null)
            {
                return;
            }

            foreach (var p in points)
            {
                samples.Add(p);
            }
        }
    }
}
