using UnityEngine;

namespace GameDev.Interaction
{
    public sealed class SimpleInteractable : MonoBehaviour
    {
        [SerializeField] private string prompt = "Press E";
        [TextArea] [SerializeField] private string message = "...";

        public string Prompt => prompt;

        public void Setup(string newPrompt, string newMessage)
        {
            prompt = newPrompt;
            message = newMessage;
        }

        public void Interact()
        {
            var tip = GetComponent<GameDev.World.TipGiver>();
            tip?.GiveTips();

            var poi = GetComponent<GameDev.World.PoiShellMarker>();
            if (poi != null)
            {
                poi.HandleInteract();
                return;
            }

            var body = message;
            if (GameDev.World.DistrictResidue.Instance != null)
            {
                var rumour = GameDev.World.DistrictResidue.Instance.BuildMerrinPostRumour();
                if (!string.IsNullOrEmpty(rumour) && gameObject.name.Contains("Merrin"))
                {
                    body = message + "\n\n" + rumour;
                }
            }

            InteractionFeedback.Show(prompt, body);
        }
    }
}
