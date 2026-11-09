using System;
using GameDev.Core.Localization;
using UnityEngine;

namespace GameDev.World.Poi
{
    /// <summary>Who says a tip (09 §3). The slice's S1 sources; Witnesses, Faction Contacts, and Physical Traces come with their scenes.</summary>
    public enum TipSource
    {
        NoticeBoard,
        Bartender,
        StreetRumour,
    }

    /// <summary>What hearing a tip does to its POI (09 *Hinted to Marked*): a tip that names a place a person can walk to Marks it, one that names only a district, a kind of event, or a time Hints it.</summary>
    public enum TipEffect
    {
        None,
        Hints,
        Marks,
    }

    /// <summary>
    /// One line a source can say (SLICE_PEOPLE §2), when it says it, and what it tells of which POI. Its words are in the
    /// string tables (17 X.7): <c>tip.&lt;id&gt;.line</c>, and <c>tip.&lt;id&gt;.note</c> for the Character Notes.
    /// </summary>
    [Serializable]
    public sealed class Tip
    {
        [Tooltip("A short stable id; the world state keys use it, so never change it after a save exists.")]
        public string id;

        public TipSource source;

        [Tooltip("The day it is said on, 1 or 2; 0 for every day.")]
        [Min(0)] public int day;

        [Tooltip("The minute of the day from which it is said.")]
        [Range(0, 1439)] public int fromMinute;

        [Tooltip("The POI it tells of, or none for a notice that leads nowhere yet.")]
        public PoiDefinition poi;

        public TipEffect effect;

        [Tooltip("Said only when this world state key holds at least the value below; empty for always.")]
        public string requiresKey = "";

        public int requiresAtLeast = 1;

        [Tooltip("Said only to a character who uses Convince, Threaten, or Exchange: S2's dialogue; never offered in S1's plain panel.")]
        public bool needsApproach;

        public string LineKey => $"tip.{id}.line";

        /// <summary>The fact in the character's own words for the Character Notes (11): what was found, never what to do with it.</summary>
        public string NoteKey => $"tip.{id}.note";

        /// <summary>What the source says, in the language shown.</summary>
        public string Line => Loc.Get(LineKey);

        public bool HasNote => Loc.Exists(NoteKey);

        /// <summary>The note in the language shown.</summary>
        public string Note => Loc.Get(NoteKey);
    }

    /// <summary>The tips of the slice's sources as data (17 Q.2; 09 *Tips a day*). Immutable at run time.</summary>
    [CreateAssetMenu(menuName = "GameDev/World/Tip Catalogue", fileName = "Tips")]
    public sealed class TipCatalogue : ScriptableObject
    {
        [SerializeField] private Tip[] tips = new Tip[0];

        public Tip[] Tips => tips;

        /// <summary>Authoring and tests.</summary>
        public void Configure(Tip[] all)
        {
            tips = all ?? new Tip[0];
        }
    }
}
