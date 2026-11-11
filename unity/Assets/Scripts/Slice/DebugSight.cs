using GameDev.Player.Progression;

namespace GameDev.Slice
{
    /// <summary>
    /// The development build's sight (owner's play test, 2026-11-11: in a debug build the cones and the chances should
    /// show). In the editor and a development build the HUD shows what the highest Intelligence and Perception show: the
    /// Vision Cones in Stealth Mode, the Notice Meter, a blow's or a check's chance and its parts. The character's own
    /// values stay as they are, so a play test still plays the character's real fights; F8 still sets the values
    /// themselves. The world's driver turns it on, and play tests turn it off to test the disclosure rules themselves.
    /// </summary>
    public static class DebugSight
    {
        public static bool On { get; set; }

        /// <summary>The value a disclosure reads: the character's own, or the highest while the sight is on.</summary>
        public static int Read(int value) => On ? CharacterSheet.Highest : value;
    }
}
