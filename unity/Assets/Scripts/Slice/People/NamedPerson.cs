using System.Collections.Generic;
using GameDev.UI;
using UnityEngine;

namespace GameDev.Slice.People
{
    /// <summary>One of the slice's named people (SLICE_PEOPLE §1): who they are, so the HUD names them nearby. Their schedules come with S5c.</summary>
    public sealed class NamedPerson : MonoBehaviour, INameTag
    {
        [Tooltip("The roster's id; stable.")]
        [SerializeField] private string id;

        [Tooltip("The name the player sees nearby; a placeholder the owner may change.")]
        [SerializeField] private string displayName;

        [SerializeField] private string role;

        /// <summary>Authoring default, m: the name hangs this high above the feet.</summary>
        private const float TagHeight = 2.05f;

        public string Id => id;

        public string DisplayName => displayName;

        public string Role => role;

        public Vector3 TagPosition => transform.position + (Vector3.up * TagHeight);

        public void Configure(string personId, string name, string personRole)
        {
            id = personId;
            displayName = name;
            role = personRole;
        }
    }

    /// <summary>The slice's named people and where each stands until S5c's schedules (SLICE_PEOPLE §1, accepted by the owner 2026-11-07).</summary>
    public static class SliceRoster
    {
        public sealed class Entry
        {
            public Entry(string id, string name, string role, string scene, bool female, string colour)
            {
                Id = id;
                Name = name;
                Role = role;
                Scene = scene;
                Female = female;
                Colour = colour;
            }

            public string Id { get; }
            public string Name { get; }
            public string Role { get; }

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

        public static readonly IReadOnlyList<Entry> All = new[]
        {
            new Entry("nell", "Nell Garrow", "Publican of the Soot & Bell", Tavern, true, "Yellow"),
            new Entry("harrow", "Josiah Harrow", "Apothecary", Apothecary, false, "Green"),
            new Entry("pip", "Pip Vance", "Harrow's apprentice", Apothecary, false, "Green"),
            new Entry("edda", "Edda Brann", "Master of Item Creation", Veilmouth, true, "Green"),
            new Entry("silas", "Silas Quill", "Master of Enchanting", Veilmouth, false, "Green"),
            new Entry("ada", "Ada Merrow", "Master of First Aid", Veilmouth, true, "Green"),
            new Entry("tobias", "Tobias Penn", "Trader", Veilmouth, false, "Orange"),
            new Entry("ruth", "Ruth Kettle", "Pawnbroker", Veilmouth, true, "Orange"),
            new Entry("ilse", "Madame Ilse", "Seer", Veilmouth, true, "Orange"),
            new Entry("coyle", "Sergeant Coyle", "Warden sergeant, brass number 14", Station, false, "Blue"),
            new Entry("ashgrave", "Mr Ashgrave", "The faction's recruiter", FactionHouse, false, "Teal"),
            new Entry("lottie", "Lottie Wren", "Thomas Wren's sister", Tavern, true, "Yellow"),
            new Entry("dace", "Dace", "Veinmarket's contact", Tavern, false, "Brown"),
            new Entry("kit", "Kit Mallow", "Newsboy", Veilmouth, false, "Yellow"),
        };
    }
}
