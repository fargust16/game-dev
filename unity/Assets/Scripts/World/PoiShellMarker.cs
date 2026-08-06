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
            RefreshPresentation();
        }

        public void HandleInteract()
        {
            if (PoiDirector.Instance != null)
            {
                PoiDirector.Instance.HandlePoiInteract(poiId);
                return;
            }

            Debug.Log($"[POI] {poiId}: {stubMessage}", this);
        }

        public void RefreshPresentation()
        {
            if (PoiDirector.Instance == null)
            {
                return;
            }

            var label = PoiDirector.Instance.BuildShellLabel(poiId);
            var worldLabel = transform.Find("WorldLabel");
            if (worldLabel != null)
            {
                var tm = worldLabel.GetComponent<TextMesh>();
                if (tm != null)
                {
                    tm.text = label;
                }
            }

            var interact = GetComponent<Interaction.SimpleInteractable>();
            if (interact != null)
            {
                interact.Setup($"Inspect {label}", stubMessage);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.6f);
        }
    }
}
