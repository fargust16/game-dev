using System.Linq;
using GameDev.Confront.Stealth;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.Slice.Stealth;
using GameDev.UI;
using GameDev.World.Poi;
using GameDev.World.Portals;
using GameDev.World.State;
using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>
    /// POI C's shuttered nook in the cellar under the Soot Stair (11.1 *Slice build (S4)*; VERTICAL_SLICE_IMPLEMENTATION_PLAN
    /// S4.3). While C is open, Interact in reach opens the shutters on the folded note and the black drop: the nook's
    /// conversation, on its <c>nook</c> node. If the stranger is there and sees the character, he steps in and makes the
    /// offer himself: the same conversation, on his <c>stranger</c> node. He sees a standing character in his cone and
    /// in sight, and one in Stealth Mode once his Notice Meter has filled (10.3 §3).
    /// </summary>
    public sealed class NookSpot : MonoBehaviour, IPromptSource
    {
        public const string NookNode = "nook";
        public const string EmptyNode = "nook_empty";
        public const string StrangerNode = "stranger";

        /// <summary>Raised when the stranger, threatened and unmoved, takes the drop with him.</summary>
        public const string DropTaken = "poi.c.drop_taken";

        /// <summary>Raised when the stranger leaves for good.</summary>
        public const string StrangerGone = "poi.c.stranger_gone";

        [SerializeField] private PoiDefinition poi;

        [Tooltip("The conversation opened here, a PersonTalk that does not open itself.")]
        [SerializeField] private PersonTalk talk;

        [Tooltip("The stranger's scene: his body, his hours and his leaving.")]
        [SerializeField] private PoiStage stranger;

        [Tooltip("The stranger's eyes.")]
        [SerializeField] private StealthObserver watcher;

        [Tooltip("Authoring default, m: horizontal reach from this point.")]
        [SerializeField] private float interactRadius = 1.2f;

        private PlayerLocomotion _player;
        private Portal[] _doors;

        public PoiDefinition Poi => poi;

        public PersonTalk Talk => talk;

        public PoiStage Stranger => stranger;

        public StealthObserver Watcher => watcher;

        /// <summary>The node the last Interact opened, for tests.</summary>
        public string LastOpened { get; private set; }

        /// <summary>Setup for scenes built by code.</summary>
        public void Configure(PoiDefinition definition, PersonTalk conversation, PoiStage strangerStage, StealthObserver eyes, float radius)
        {
            poi = definition;
            talk = conversation;
            stranger = strangerStage;
            watcher = eyes;
            interactRadius = radius;
        }

        /// <summary>
        /// Whether an observer sees the character: standing, by being in its cone and in sight; in Stealth Mode, only once
        /// its Notice Meter has filled and it still sees the character (10.3 §3).
        /// </summary>
        public static bool Sees(bool hiding, bool inSight, NoticeMeter meter) =>
            hiding ? meter != null && meter.Noticed && meter.Seeing : inSight;

        public bool InReach(Vector3 position) => InteractReach.Within(transform, position, interactRadius);

        /// <summary>Whether the shutters can be opened: C open, and no conversation here already.</summary>
        public bool CanOpen(IWorldState state, PoiPhase phase) => phase == PoiPhase.Open && talk != null && !talk.IsOpen;

        /// <summary>Whether the stranger is there and sees the character standing at <paramref name="feet"/>.</summary>
        public bool StrangerSees(Vector3 feet)
        {
            var world = SliceWorldDriver.Running;
            var stealth = world != null ? world.Hiding : null;
            if (stranger == null || !stranger.IsShown || watcher == null || !watcher.isActiveAndEnabled || stealth == null || stealth.Rules == null)
            {
                return false;
            }

            if (stranger.GoneKey.Length > 0 && world.State.IsSet(stranger.GoneKey))
            {
                return false;
            }

            if (watcher.Meter == null)
            {
                watcher.Bind(stealth.Rules);
            }

            return Sees(stealth.Active, stealth.Sees(watcher, feet, stealth.Active), watcher.Meter);
        }

        public string PromptFor(Vector3 player)
        {
            var world = SliceWorldDriver.Running;
            return world != null && poi != null && CanOpen(world.State, world.Director.Phase(poi)) && InReach(player)
                ? Loc.Get("ui.prompt.open_shutters")
                : null;
        }

        /// <summary>What Interact does: the shutters open on the nook, or the stranger, seeing the character, steps in.</summary>
        public void Use()
        {
            var world = SliceWorldDriver.Running;
            if (world == null || poi == null || !CanOpen(world.State, world.Director.Phase(poi)) || Player() == null)
            {
                return;
            }

            LastOpened = Entry(StrangerSees(_player.transform.position), world.State);
            talk.Open(LastOpened);
        }

        /// <summary>
        /// The node the shutters open on: the stranger's when he sees the character; the empty shelf once a failed threat
        /// sent him off with the drop (11.1: the watcher flees, leaving only the note); else the note and the drop.
        /// </summary>
        public static string Entry(bool strangerSees, IWorldState state) =>
            strangerSees ? StrangerNode : state.IsSet(DropTaken) ? EmptyNode : NookNode;

        private void Update()
        {
            if (Player() == null)
            {
                return;
            }

            var input = GameInputService.Instance;
            var at = _player.transform.position;
            _doors ??= FindObjectsByType<Portal>(FindObjectsInactive.Exclude);
            if (input != null && input.InteractPressed && InReach(at) && (talk == null || !talk.IsOpen)
                && !_doors.Any(d => d != null && d.isActiveAndEnabled && d.InReach(at)))
            {
                Use();
            }
        }

        private PlayerLocomotion Player()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerLocomotion>();
            }

            return _player;
        }
    }
}
