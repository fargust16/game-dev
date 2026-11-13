namespace GameDev.Slice.People
{
    /// <summary>
    /// A walker's hours (S5c.3; 18 *Townsfolk in the slice*): the crowd keeps no schedule, but fewer walk at night, and the
    /// people sitting at a place with no route sit there only while its keeper keeps it open (<see cref="SliceHours"/>).
    /// <see cref="PersonHours"/> sends a walker home by switching its body off where it stands, and back by switching it
    /// on, so it walks on from where it was.
    /// </summary>
    public sealed partial class TownsfolkWalker
    {
        /// <summary>Whether it walks a route; one that does not keeps its place for ever (a drinker, a worshipper).</summary>
        public bool Roams => route.Length >= 2;

        /// <summary>Its route's seed, by which it is one of those at home at night or not.</summary>
        public uint RouteSeed => SeedOf(route);

        /// <summary>
        /// Whether it may go home now: on its route or standing at a turn, not inside a door, at a place, greeting, or
        /// heeding an Alarm. One with no route sits at its place, and goes with it.
        /// </summary>
        public bool FreeToGoHome => !Roams || ((_mode == Mode.Route || _mode == Mode.Stand) && !Inside);
    }
}
