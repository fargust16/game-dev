namespace GameDev.Player.Progression
{
    /// <summary>
    /// 05.1 *Vertical Slice*: the Arcana a character has entered. The slice holds two, Жрица, which the internal-order
    /// faction offers and its allied followers use, and Император, which the character finds alone and the cult's
    /// followers use; the other twenty wait for the full game. A new Arcana goes at the end.
    /// </summary>
    public enum Arcana
    {
        /// <summary>Uninitiated: no Arcana, no Limit, no Will Power to spend on abilities.</summary>
        None,

        /// <summary>Жрица.</summary>
        Priestess,

        /// <summary>Император.</summary>
        Emperor,
    }
}
