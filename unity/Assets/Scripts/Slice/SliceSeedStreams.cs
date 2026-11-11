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

        /// <summary>Rolls of action checks (07.1 §8); the system that checks forks it with keys that no two attempts share.</summary>
        public const string ActionChecks = "slice.check.action";

        /// <summary>
        /// Rolls of Social Checks (10.4 §2); fork it with the option's id hashed and the tick as the keys. A failed check
        /// is never tried again on the spot, so the pair never repeats.
        /// </summary>
        public const string SocialChecks = "slice.check.social";

        /// <summary>Whether a lie that worked comes out at the next day boundary (10.4 §5); forked by the listener's id hashed and the new day.</summary>
        public const string Lies = "slice.lies";

        /// <summary>The character's conditions on world ticks outside a fight (10.2, S3a.7); fork it with the tick as the key.</summary>
        public const string Conditions = "slice.conditions";

        /// <summary>A shop's morning shelf (S5a.3; 19 §6.3); fork it with the shop's index and the day.</summary>
        public const string ShopStock = "slice.shop.stock";

        /// <summary>A piece made at a workshop (S5a.4; 19 §3); fork it with the count of pieces made before it.</summary>
        public const string Workshop = "slice.workshop";

        /// <summary>A gathering cycle (S5a.5; 19 §6.4); fork it with the tick and the node's id hashed.</summary>
        public const string Gathering = "slice.gathering";

        /// <summary>A skinning's check (S5a.6; 19 §6.4); fork it with the beast's id hashed.</summary>
        public const string Skinning = "slice.skinning";

        /// <summary>A theft's bar and purse (S5a.8; 10.3 §6, 19 §6.3); fork it with the person's name hashed and the day.</summary>
        public const string Theft = "slice.theft";

        /// <summary>Where a beast pack wanders in its territory and when (10.2, the owner's play test of S5a); fork it with the pack and its wander.</summary>
        public const string BeastWander = "slice.beasts.wander";

        public static readonly IReadOnlyList<string> All = new[]
        {
            RitualSite, RitualWeakPoints, GlowingBeasts, Combat, Dwellings, PoiUnattended, ActionChecks, SocialChecks, Lies, Conditions,
            ShopStock, Workshop, Gathering, Skinning, Theft, BeastWander,
        };
    }
}
