using System;
using GameDev.Core.Localization;
using UnityEngine;

namespace GameDev.World.Poi
{
    /// <summary>The Active Window classes of 04; a class is the window's feel, its length is the definition's own.</summary>
    public enum PoiWindowClass
    {
        Brief,
        Short,
        Standard,
        Long,
        Persistent,
    }

    /// <summary>One way a POI ends when nobody comes (04 *Unattended resolution*): its weight among the others, and the world state keys it raises.</summary>
    [Serializable]
    public sealed class PoiOutcome
    {
        [Tooltip("A short name, for the log and the tests.")]
        public string name;

        [Tooltip("Its chance against the other outcomes; drawn once from the seed.")]
        [Min(0f)] public float weight = 1f;

        [Tooltip("World state keys this outcome raises: its Aftermath and residue.")]
        public string[] raises = new string[0];
    }

    /// <summary>
    /// A Point of Interest as data (17 Q.3; 04; 11.1): when it appears, how long its Active Window runs, and how it ends
    /// unattended. Immutable at run time; the run's state lives in the world state store under <c>poi.&lt;id&gt;.</c>.
    /// Minutes count from day 1, 00:00, as the clock's ticks do. Its words are in the string tables (17 X.7):
    /// <c>poi.&lt;id&gt;.title</c>, and <c>poi.&lt;id&gt;.urgency</c>, how the sources word its urgency, never a countdown (04 rule 5).
    /// </summary>
    [CreateAssetMenu(menuName = "GameDev/World/POI Definition", fileName = "Poi")]
    public sealed class PoiDefinition : ScriptableObject
    {
        [Tooltip("A short stable id; the world state keys use it, so never change it after a save exists.")]
        [SerializeField] private string id = "a";

        [SerializeField] private PoiWindowClass windowClass = PoiWindowClass.Standard;

        [Tooltip("The minute it appears, or -1 when it appears only after another POI.")]
        [SerializeField] private int appearsAtMinute = -1;

        [Tooltip("It appears when any of these has resolved, attended or not.")]
        [SerializeField] private PoiDefinition[] appearsAfterAny = new PoiDefinition[0];

        [Tooltip("Minutes its window stays open from its appearance, or 0 to stay open until closesAtMinute.")]
        [SerializeField] private int windowMinutes;

        [Tooltip("The minute its window closes whatever happens, or -1.")]
        [SerializeField] private int closesAtMinute = -1;

        [SerializeField] private PoiOutcome[] unattended = new PoiOutcome[0];

        [Tooltip("Where it happens, world position; the map's pin when Marked.")]
        [SerializeField] private Vector3 place;

        [Tooltip("The map's area when only Hinted, m.")]
        [SerializeField] private float hintRadius = 30f;

        public string Id => id;
        public string TitleKey => $"poi.{id}.title";
        public string UrgencyKey => $"poi.{id}.urgency";

        /// <summary>The title in the language shown.</summary>
        public string Title => Loc.Get(TitleKey);

        public PoiWindowClass WindowClass => windowClass;

        /// <summary>The sources' words of urgency in the language shown, or empty when it has none.</summary>
        public string UrgencyWords => Loc.Exists(UrgencyKey) ? Loc.Get(UrgencyKey) : string.Empty;

        public int AppearsAtMinute => appearsAtMinute;
        public PoiDefinition[] AppearsAfterAny => appearsAfterAny;
        public int WindowMinutes => windowMinutes;
        public int ClosesAtMinute => closesAtMinute;
        public PoiOutcome[] Unattended => unattended;
        public Vector3 Place => place;
        public float HintRadius => hintRadius;

        /// <summary>Authoring and tests: every field at once.</summary>
        public void Configure(string poiId, PoiWindowClass window, int appearsAt, PoiDefinition[] after,
            int windowLength, int closesAt, PoiOutcome[] outcomes, Vector3 at, float hintArea)
        {
            id = poiId;
            windowClass = window;
            appearsAtMinute = appearsAt;
            appearsAfterAny = after ?? new PoiDefinition[0];
            windowMinutes = windowLength;
            closesAtMinute = closesAt;
            unattended = outcomes ?? new PoiOutcome[0];
            place = at;
            hintRadius = hintArea;
        }
    }
}
