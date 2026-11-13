using System.Collections.Generic;

namespace GameDev.Slice.Caches
{
    /// <summary>One of the forty candidate spots of SLICE_TRIAL §5.1.</summary>
    public readonly struct CacheSpot
    {
        public CacheSpot(int number, string district, int stage, float x, float y, float z)
        {
            Number = number;
            District = district;
            Stage = stage;
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>The spot's number in SLICE_TRIAL §5.1, 1–40.</summary>
        public int Number { get; }

        /// <summary>One of <see cref="CacheSpots"/>' district keys.</summary>
        public string District { get; }

        /// <summary>9 in plain places, 8 where tucked away or outside the walls.</summary>
        public int Stage { get; }

        /// <summary>The place in the Veilmouth scene, m; Y is the ground or the prop's top, from which the scene's ground is found.</summary>
        public float X { get; }

        public float Y { get; }

        public float Z { get; }

        public bool Outside => District == CacheSpots.Outside;
    }

    /// <summary>
    /// SLICE_TRIAL §5.1's forty candidate spots in the Veilmouth scene (S5c.6), each placed by its description from the
    /// scene's own objects and ground, measured by probing the scene on 2026-11-13 (authoring defaults). A spot whose
    /// thing the scene lacks stands at the nearest place that fits, as §5.1 says: Squint Lane was dropped with C5's
    /// rectangular blocks, so its cellar window is on Nettle Street; the well, the hay wagon, the milestone, the oak, the
    /// fallen trunk, the cairn, the hide, the den's mouth, the boat and the boundary stone are not modelled, so their caches
    /// lie where those would stand: by the hamlet's green, on a farm's hay, at a roadside post, at a Gloomwood pine, in its
    /// clearings, at a wolf pack's middle, on the bank below the bridge and at a boar ground's middle.
    /// </summary>
    public static class CacheSpots
    {
        public const string ParsonsEnd = "parsons_end";
        public const string SleepyQuarter = "sleepy_quarter";
        public const string ThePenny = "the_penny";
        public const string SaltEnd = "salt_end";
        public const string LowerVeil = "lower_veil";
        public const string TheReek = "the_reek";
        public const string Outside = "outside";

        public static readonly IReadOnlyList<CacheSpot> All = new[]
        {
            // Parsons' End
            new CacheSpot(1, ParsonsEnd, 9, -52.7f, 26.2f, 140.0f),   // the old chapel's east wall, at the forecourt
            new CacheSpot(2, ParsonsEnd, 9, -51.5f, 26.3f, 161.6f),   // a grave's urn, Last Turning Cemetery
            new CacheSpot(3, ParsonsEnd, 8, -48.0f, 26.3f, 161.25f),  // behind the angel's plinth
            new CacheSpot(4, ParsonsEnd, 9, -1.0f, 27.3f, 200.5f),    // the birdbath's foot, Bishop's Garden
            new CacheSpot(5, ParsonsEnd, 8, 71.7f, 27.4f, 202.6f),    // a beech's foot, Quiet Whistle Garden
            new CacheSpot(6, ParsonsEnd, 8, 134.5f, 26.2f, 189.1f),   // the Brothers' bell tower's step
            new CacheSpot(7, ParsonsEnd, 9, -57.5f, 26.1f, 194.5f),   // on the firewood of a slum yard off Last Turning

            // Sleepy Quarter
            new CacheSpot(8, SleepyQuarter, 9, -52.0f, 26.2f, 38.5f),  // the back wall of a shop on Long Tongue, in its yard
            new CacheSpot(9, SleepyQuarter, 9, -47.5f, 26.3f, 95.6f),  // by a bench in a rich yard
            new CacheSpot(10, SleepyQuarter, 8, 10.0f, 25.9f, -49.3f), // the parapet's foot on Powder Embankment
            new CacheSpot(11, SleepyQuarter, 8, -81.5f, 26.05f, 68.0f), // under Creaking Gate's arch, by the north tower
            new CacheSpot(12, SleepyQuarter, 8, -49.9f, 26.5f, 117.0f), // a villa's stoop rail on Widows' Row

            // The Penny
            new CacheSpot(13, ThePenny, 9, 40.0f, 27.55f, 55.6f),      // Stone Grandad's plinth
            new CacheSpot(14, ThePenny, 9, -1.5f, 27.3f, 87.1f),       // the clock tower's step
            new CacheSpot(15, ThePenny, 8, 57.0f, 27.55f, 35.0f),      // a cellar cover beside the Guild Hall

            // Salt End
            new CacheSpot(16, SaltEnd, 9, 196.5f, 26.0f, 54.5f),       // in a coil of rope by Fishlip Quay
            new CacheSpot(17, SaltEnd, 8, 148.6f, 26.1f, 96.6f),       // the Customs House's downpipe
            new CacheSpot(18, SaltEnd, 9, 128.5f, 26.3f, 95.6f),       // by the bench of a middling yard off Salt Gullet

            // Lower Veil
            new CacheSpot(19, LowerVeil, 8, 240.5f, 26.0f, 60.0f),     // Tipsy Pier's last plank
            new CacheSpot(20, LowerVeil, 9, 173.5f, 25.9f, 15.5f),     // in a crate on the quay
            new CacheSpot(21, LowerVeil, 9, 64.5f, 27.0f, -0.5f),      // a tenement's wall on Draught Lane
            new CacheSpot(22, LowerVeil, 9, 96.6f, 26.9f, 16.5f),      // a cellar window on Nettle Street
            new CacheSpot(23, LowerVeil, 8, 120.0f, 26.05f, -33.4f),   // the gutter along the Black Puddle
            new CacheSpot(24, LowerVeil, 9, 138.5f, 26.0f, 18.5f),     // in a dustbin of a poor yard

            // The Reek
            new CacheSpot(25, TheReek, 8, -23.5f, 26.05f, -161.4f),    // a downpipe on Rats' Choir
            new CacheSpot(26, TheReek, 8, 150.0f, 25.9f, -160.6f),     // the coal store's wall in Deaf Boiler Yard
            new CacheSpot(27, TheReek, 9, 46.3f, 26.05f, -105.0f),     // a works shed's wall on Furnace Road
            new CacheSpot(28, TheReek, 8, 31.0f, 26.2f, -86.5f),       // the works end of Grubby Bridge

            // Outside the walls
            new CacheSpot(29, Outside, 9, -124.0f, 26.2f, 52.5f),      // the west end of Last Farthing Bridge
            new CacheSpot(30, Outside, 9, -200.0f, 26.3f, 68.5f),      // on a farm's hay at Crooked Hamlet
            new CacheSpot(31, Outside, 8, -188.0f, 26.2f, 60.0f),      // Crooked Hamlet's green, where its well would be
            new CacheSpot(32, Outside, 9, -309.6f, 26.6f, 52.0f),      // a roadside post on the West Road
            new CacheSpot(33, Outside, 9, -142.6f, 26.2f, 363.6f),     // a pine at Gloomwood's edge
            new CacheSpot(34, Outside, 8, -305.0f, 28.6f, 180.0f),     // in Gloomwood
            new CacheSpot(35, Outside, 8, -109.0f, 26.2f, -290.0f),    // a Gloomwood clearing
            new CacheSpot(36, Outside, 8, 51.0f, 26.2f, -350.0f),      // deep in Gloomwood
            new CacheSpot(37, Outside, 8, 20.0f, 26.2f, 300.0f),       // the middle of a wolf pack's territory
            new CacheSpot(38, Outside, 9, -124.5f, 26.2f, 70.5f),      // the bank below Last Farthing Bridge
            new CacheSpot(39, Outside, 8, 172.0f, 26.2f, 258.0f),      // the open coast beyond the quay
            new CacheSpot(40, Outside, 8, -170.0f, 26.2f, 200.0f),     // the middle of a boar ground
        };

        /// <summary>The spot of <paramref name="number"/>, 1–40.</summary>
        public static CacheSpot Of(int number) => All[number - 1];
    }
}
