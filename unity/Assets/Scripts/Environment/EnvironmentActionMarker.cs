using GameDev.Interaction;
using UnityEngine;

namespace GameDev.Environment
{
    public sealed class EnvironmentActionMarker : MonoBehaviour
    {
        [SerializeField] private EnvironmentActionType actionType = EnvironmentActionType.VaultJump;
        [SerializeField] [Range(0f, 1f)] private float successChance = 0.75f;
        [SerializeField] private string successMessage = "Traversal succeeded.";
        [SerializeField] private string failMessage = "Traversal failed — noise / wasted effort.";

        public EnvironmentActionType ActionType => actionType;
        public string Prompt => $"Try {actionType}";

        public void Setup(EnvironmentActionType type, float chance, string ok, string fail)
        {
            actionType = type;
            successChance = chance;
            successMessage = ok;
            failMessage = fail;
            gameObject.name = $"Env_{type}";
        }

        public bool TryPerform()
        {
            var ok = Random.value <= successChance;
            InteractionFeedback.Show(
                ok ? $"{actionType} — success" : $"{actionType} — fail",
                ok ? successMessage : failMessage);
            return ok;
        }
    }
}
