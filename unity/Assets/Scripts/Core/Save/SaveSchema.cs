namespace GameDev.Core.Save
{
    /// <summary>
    /// The save contract of the slice (17 X.3; VERTICAL_SLICE_IMPLEMENTATION_PLAN S0.5): the schema version and the
    /// sections a Suspend Save holds. Each section belongs to one system, which writes and reads it through
    /// <see cref="ISaveContributor"/> and versions it on its own. Nothing regenerable is saved: the scene's geometry,
    /// the kit, and the seed's draws come back from the scene and the seed. A save is written outside a fight and a
    /// dialogue only, so neither the Combat Grid nor a conversation is ever in it.
    /// </summary>
    public static class SaveSchema
    {
        /// <summary>The file's layout. Raise it when <see cref="SaveFile"/> itself changes, not a section.</summary>
        public const int Version = 1;

        /// <summary>The slice seed (W.1); everything drawn from it is drawn again.</summary>
        public const string WorldSeed = "world.seed";

        /// <summary>The tick and the progress toward the next one (X.1).</summary>
        public const string WorldClock = "world.clock";

        /// <summary>The scene the player is in and their pose there (L.2, I.2).</summary>
        public const string PlayerPlace = "player.place";

        /// <summary>Attributes, skills, Arcana and Limit, Conduct Progress, HP, Will Power, Sanity, Fatigue, conditions (P).</summary>
        public const string PlayerCharacter = "player.character";

        /// <summary>The bag, the equipped items and Weapon Sets, and gold (19).</summary>
        public const string PlayerInventory = "player.inventory";

        /// <summary>The Arguments gained in this life, lessons learned, and known recipes (10.4, 19, 20).</summary>
        public const string PlayerKnowledge = "player.knowledge";

        /// <summary>POI states, decision outcomes, residue, interiors' changes by building id, spent resource nodes, shop shelves (C.6, Q.3, W.7).</summary>
        public const string WorldState = "world.state";

        /// <summary>Named NPCs and beasts: alive or dead, where, their task, their opinion of the player (A.2).</summary>
        public const string WorldPeople = "world.people";

        /// <summary>Faction membership, the trial, forces, and City Control (E.1).</summary>
        public const string WorldFactions = "world.factions";

        /// <summary>The General, the ritual, the Weak Points found, and the cult's forces (E.1b).</summary>
        public const string WorldInvasion = "world.invasion";

        /// <summary>Every section of the slice's Suspend Save, in the order they are written.</summary>
        public static readonly string[] Sections =
        {
            WorldSeed, WorldClock, PlayerPlace, PlayerCharacter, PlayerInventory, PlayerKnowledge,
            WorldState, WorldPeople, WorldFactions, WorldInvasion,
        };
    }
}
