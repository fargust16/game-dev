using System.Collections.Generic;

namespace GameDev.Slice
{
    /// <summary>
    /// Names of the slice's seed forks (VERTICAL_SLICE_IMPLEMENTATION_PLAN §1). Each random choice forks
    /// its own stream, so adding a choice never shifts another. Never rename a stream: a rename changes the run.
    /// </summary>
    public static class SliceSeedStreams
    {
        /// <summary>Which Ritual Site Candidate the ritual uses (21 §4).</summary>
        public const string RitualSite = "slice.ritual.site";

        /// <summary>The ritual's two Weak Points (21 §4).</summary>
        public const string RitualWeakPoints = "slice.ritual.weak-points";

        /// <summary>Which beasts glow (11).</summary>
        public const string GlowingBeasts = "slice.beasts.glowing";

        /// <summary>Rolls of one fight; fork it with the fight's number as the key.</summary>
        public const string Combat = "slice.combat";

        /// <summary>A dwelling's rooms, furniture and Stash (15; 10.3 §6.1); fork it with the door's place as the keys.</summary>
        public const string Dwellings = "slice.dwellings";

        /// <summary>How a POI ends when nobody comes (04; 11.1); fork it with the POI's id hashed as the key.</summary>
        public const string PoiUnattended = "slice.poi.unattended";

        public static readonly IReadOnlyList<string> All = new[] { RitualSite, RitualWeakPoints, GlowingBeasts, Combat, Dwellings, PoiUnattended };
    }
}
