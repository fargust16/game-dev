using GameDev.Confront.Grid;
using UnityEngine;

namespace GameDev.Slice.Kit
{
    /// <summary>
    /// Tags a block with the Environment Action it offers and its band (SLICE_MODULAR_KIT_STANDARD §5). The Combat Grid
    /// reads it from the collider that closes an edge, or from that collider's parents.
    /// The action's AP and attributes are data of the fight rules, not of the block.
    /// </summary>
    public sealed class KitEnvironmentAction : MonoBehaviour, IEnvironmentAction
    {
        [SerializeField] private EnvironmentActionKind action = EnvironmentActionKind.VaultJump;
        [SerializeField] private EnvironmentActionBand band = EnvironmentActionBand.Easy;

        public EnvironmentActionKind Action => action;
        public EnvironmentActionBand Band => band;

        /// <summary>Sets the tag on a block made in code, as by an editor command or a test.</summary>
        public void Set(EnvironmentActionKind kind, EnvironmentActionBand difficulty)
        {
            action = kind;
            band = difficulty;
        }
    }
}
