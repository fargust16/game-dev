using UnityEngine;

namespace GameDev.World
{
    public sealed class LandmarkMarker : MonoBehaviour
    {
        [SerializeField] private LandmarkId landmarkId;
        [SerializeField] private string displayName;

        public LandmarkId LandmarkId => landmarkId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? landmarkId.ToString() : displayName;

        public void Setup(LandmarkId id, string name)
        {
            landmarkId = id;
            displayName = name;
            gameObject.name = $"Landmark_{id}";
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(1.2f, 2f, 1.2f));
        }
    }
}
