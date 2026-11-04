using UnityEngine;

namespace GameDev.Slice.Kit
{
    /// <summary>Environment Actions a block can offer in a fight (10.1, Environment Actions).</summary>
    public enum EnvironmentActionKind
    {
        VaultJump,
        ClimbUp,
        ClimbThroughWindow,
        MantleOnto,
        KickBreach
    }

    /// <summary>The three bands of 10.1, V1 numbers.</summary>
    public enum EnvironmentActionBand
    {
        Easy,
        Hard,
        VeryHard
    }

    /// <summary>
    /// Tags a block with the Environment Action it offers and its band (SLICE_MODULAR_KIT_STANDARD §5).
    /// The action's AP and attributes are data of the fight rules, not of the block.
    /// </summary>
    public sealed class KitEnvironmentAction : MonoBehaviour
    {
        [SerializeField] private EnvironmentActionKind action = EnvironmentActionKind.VaultJump;
        [SerializeField] private EnvironmentActionBand band = EnvironmentActionBand.Easy;

        public EnvironmentActionKind Action => action;
        public EnvironmentActionBand Band => band;
    }
}
