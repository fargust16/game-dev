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
            Debug.Log($"[Interact] {gameObject.name}: {message}", this);
        }
    }
}
