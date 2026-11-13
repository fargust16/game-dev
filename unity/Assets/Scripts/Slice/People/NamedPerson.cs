using System.Collections.Generic;
using GameDev.Core.Localization;
using GameDev.UI;
using UnityEngine;

namespace GameDev.Slice.People
{
    /// <summary>
    /// One of the slice's named people (SLICE_PEOPLE §1): who they are, so the HUD names them nearby. The name and the
    /// role are in the string tables (17 X.7) as <c>person.&lt;id&gt;.name</c> and <c>.role</c>, placeholders the owner may
    /// change. Their hours are <see cref="SliceHours"/>, kept by <see cref="PersonHours"/> (S5c.3).
    /// </summary>
    public sealed class NamedPerson : MonoBehaviour, INameTag
    {
        [Tooltip("The roster's id; stable.")]
        [SerializeField] private string id;

        /// <summary>Authoring default, m: the name hangs this high above the feet.</summary>
        private const float TagHeight = 2.05f;

        public string Id => id;

        /// <summary>The name the player sees nearby, in the language shown.</summary>
        public string DisplayName => Loc.Get(SliceRoster.NameKey(id));

        public string Role => Loc.Get(SliceRoster.RoleKey(id));

        public Vector3 TagPosition => transform.position + (Vector3.up * TagHeight);

        public void Configure(string personId)
        {
            id = personId;
        }
    }

    /// <summary>
    /// The slice's named people and the scene each works in (SLICE_PEOPLE §1, accepted by the owner 2026-11-07): since the
    /// owner's answer to S5-9 the pawnbroker is Mortimer Kettle, and since S5c.3 Father Ambrose Hale, the old priest, is the
    /// fifteenth; their other posts are <see cref="SliceHours"/>.
    /// </summary>
    public static class SliceRoster
    {
        public sealed class Entry
        {
            public Entry(string id, string scene, bool female, string colour)
            {
                Id = id;
                Scene = scene;
                Female = female;
                Colour = colour;
            }

            public string Id { get; }

            /// <summary>The name in the language shown.</summary>
            public string Name => Loc.Get(NameKey(Id));

            /// <summary>The English name: the person's object in the scene carries it, whatever the language.</summary>
            public string ObjectName => Loc.Tables.Get(NameKey(Id), Loc.English);

            public string Role => Loc.Get(RoleKey(Id));

            /// <summary>The scene's file name under Assets/Scenes/Slice (interiors in Interiors/).</summary>
            public string Scene { get; }

            public bool Female { get; }

            /// <summary>The Dummy's colour code (asset map): Yellow, Green, Orange, Blue, Teal, Brown.</summary>
            public string Colour { get; }
        }

        public const string Veilmouth = "Veilmouth";
        public const string Tavern = "SootAndBell";
        public const string Apothecary = "HarrowApothecary";
        public const string Station = "WardensLockup";
        public const string FactionHouse = "FactionHouse";
        public const string Chapel = "OldChapel";

        public static string NameKey(string id) => $"person.{id}.name";

        public static string RoleKey(string id) => $"person.{id}.role";

        public static readonly IReadOnlyList<Entry> All = new[]
        {
            new Entry("nell", Tavern, true, "Yellow"),
            new Entry("harrow", Apothecary, false, "Green"),
            new Entry("pip", Apothecary, false, "Green"),
            new Entry("edda", Veilmouth, true, "Green"),
            new Entry("silas", Veilmouth, false, "Green"),
            new Entry("ada", Veilmouth, true, "Green"),
            new Entry("tobias", Veilmouth, false, "Orange"),
            new Entry("kettle", Veilmouth, false, "Orange"),
            new Entry("ilse", Veilmouth, true, "Orange"),
            new Entry("coyle", Station, false, "Blue"),
            new Entry("ashgrave", FactionHouse, false, "Teal"),
            new Entry("lottie", Tavern, true, "Yellow"),
            new Entry("dace", Tavern, false, "Brown"),
            new Entry("kit", Veilmouth, false, "Yellow"),
            new Entry("hale", Chapel, false, "Teal"),
        };
    }
}
