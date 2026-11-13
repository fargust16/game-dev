using UnityEngine;

namespace GameDev.Slice.People
{
    /// <summary>
    /// One of a person's posts in a scene (S5c.3; <see cref="SliceHours"/>): the body that stands there while the person's
    /// hours say it is there, and is switched off at any other hour. The work post is the one the roster placed; the
    /// other posts are bodies of the same person in other places, switched off in the scene as saved. Only
    /// <see cref="PersonHours"/> switches it, so a residue that keeps the person away holds it instead
    /// (<see cref="Held"/>) and the two never fight over the body.
    /// </summary>
    public sealed class PersonPost : MonoBehaviour
    {
        [Tooltip("The person's id: a named person's roster id, or SliceHours.MarketTrader.")]
        [SerializeField] private string person;

        [Tooltip("Which of the person's posts this body stands at (SliceHours: work, tavern, tavern-door, lane, quay, forecourt).")]
        [SerializeField] private string post;

        [Tooltip("The post the scene shows when no hours are kept: the roster's place.")]
        [SerializeField] private bool primary;

        public string Person => person;

        public string Post => post;

        public bool Primary => primary;

        /// <summary>A residue keeps the person away from here now (the Wardens hold Harrow in the street, or took him).</summary>
        public bool Held { get; set; }

        /// <summary>Editor setup.</summary>
        public void Configure(string personId, string postId, bool isPrimary)
        {
            person = personId;
            post = postId;
            primary = isPrimary;
        }
    }
}
