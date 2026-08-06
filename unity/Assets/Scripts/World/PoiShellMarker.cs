using UnityEngine;

namespace GameDev.World
{
    public sealed class PoiShellMarker : MonoBehaviour
    {
        [SerializeField] private PoiShellId poiId;
        [SerializeField] private string stubMessage = "S1 will start Decision Node here.";

        public PoiShellId PoiId => poiId;
        public string StubMessage => stubMessage;

        public void Setup(PoiShellId id, string message)
        {
            poiId = id;
            stubMessage = message;
            gameObject.name = $"POI_{id}";
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.6f);
        }
    }
}
