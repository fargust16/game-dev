using UnityEngine;

namespace GameDev.World
{
    /// <summary>
    /// When the player interacts with this object, apply Hinted/Marked tips to POIs.
    /// </summary>
    public sealed class TipGiver : MonoBehaviour
    {
        [SerializeField] private PoiShellId[] hintPois;
        [SerializeField] private PoiShellId[] markPois;
        [TextArea] [SerializeField] private string tipSummary = "";

        public void Setup(PoiShellId[] hints, PoiShellId[] marks, string summary)
        {
            hintPois = hints ?? System.Array.Empty<PoiShellId>();
            markPois = marks ?? System.Array.Empty<PoiShellId>();
            tipSummary = summary ?? "";
        }

        public void GiveTips()
        {
            if (PoiDirector.Instance == null)
            {
                return;
            }

            PoiDirector.Instance.ApplyTips(hintPois, markPois, tipSummary);
        }
    }
}
