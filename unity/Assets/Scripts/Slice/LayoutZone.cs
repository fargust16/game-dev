using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>
    /// A zone of the C0 Veilmouth map (VERTICAL_SLICE_IMPLEMENTATION_PLAN §2), drawn in the Scene view as a guide for
    /// placing the kit and shaping the ground by hand. It has no collider and no rule reads it; the transform is the
    /// centre of the zone's footprint at ground level.
    /// </summary>
    public sealed class LayoutZone : MonoBehaviour
    {
        [SerializeField] private Vector3 size = new Vector3(10f, 10f, 10f);
        [SerializeField] private Color color = new Color(1f, 0.8f, 0.2f, 0.9f);

        public Vector3 Size => size;

        public void Configure(Vector3 newSize, Color newColor)
        {
            size = newSize;
            color = newColor;
        }

        /// <summary>Whether a point lies over the zone's footprint, whatever its height.</summary>
        public bool Covers(Vector3 point)
        {
            var local = point - transform.position;
            return Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.z) <= size.z * 0.5f;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = color;
            Gizmos.DrawWireCube(transform.position + Vector3.up * (size.y * 0.5f), size);
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.Label(transform.position + Vector3.up * size.y, name);
        }
#endif
    }
}
