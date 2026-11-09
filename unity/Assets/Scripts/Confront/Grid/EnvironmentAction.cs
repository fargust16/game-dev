namespace GameDev.Confront.Grid
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
    /// A block's 10.1 tag as the grid reads it. The kit's <c>KitEnvironmentAction</c> carries it on the block
    /// (SLICE_MODULAR_KIT_STANDARD §5); the action's AP and attributes are data of the fight rules, not of the block.
    /// </summary>
    public interface IEnvironmentAction
    {
        EnvironmentActionKind Action { get; }

        EnvironmentActionBand Band { get; }
    }
}
