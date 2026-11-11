using System;
using System.Linq;
using GameDev.Dialogue;
using GameDev.Locomotion;
using GameDev.World.Poi;
using GameDev.World.State;
using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>
    /// A POI's scene where it happens (04 *Lazy activation*; VERTICAL_SLICE_IMPLEMENTATION_PLAN S2.5): its people stand
    /// hidden until the player enters the area while the POI's window is open, and then stay, posed, as scenes stand still
    /// in S2. Its conversation, a <see cref="PersonTalk"/> beside it, opens only while the scene is there and the POI open;
    /// one already open runs on to its end, so the outcome that settles the POI is still read. Once the POI has resolved,
    /// its people leave when the player is out of the area, so nobody sees them go. S3b.3: a conversation that ends in a
    /// fight, a Confront or a lost Final Check, is fought here by the people its listener names, and the fight's end
    /// writes the option's Won or Lost. S4.2: a person of the place, such as POI C's sweep, keeps hours and can be sent
    /// away for good by a world state key; outside them it counts as closed.
    /// </summary>
    public sealed class PoiStage : MonoBehaviour
    {
        private static readonly int Idle = Animator.StringToHash("Idle");

        /// <summary>The people a listener of the conversation is, when it comes to a fight.</summary>
        [Serializable]
        public sealed class Fighters
        {
            [Tooltip("The conversation's listener these people are, e.g. collectors.")]
            public string listener;

            [Tooltip("Their group in a fight, e.g. wardens or veinmarket (SliceConfront).")]
            public string group;

            public Transform[] bodies = new Transform[0];
        }

        [SerializeField] private PoiDefinition poi;

        [Tooltip("The scene's people, inactive in the scene.")]
        [SerializeField] private GameObject cast;

        [Tooltip("Authoring default, m: the area whose entry brings the scene, measured flat from this point.")]
        [SerializeField] private float appearRadius = 60f;

        [Tooltip("S3b.3: who fights when the conversation ends in a fight with one of its listeners.")]
        [SerializeField] private Fighters[] fighters = new Fighters[0];

        [Tooltip("World state keys raised when a fight here is won and one of them was spared (11.1: Spare can buy a fragment).")]
        [SerializeField] private string[] whenSpared = new string[0];

        [Header("A person of the place (S4.2)")]
        [Tooltip("Authoring default: the minute of the day the people come, 0–1439; -1, with Until -1, for any hour.")]
        [SerializeField] private int fromMinute = -1;

        [Tooltip("Authoring default: the minute of the day they go, after From.")]
        [SerializeField] private int untilMinute = -1;

        [Tooltip("A world state key that sends them away for good, e.g. poi.c.sweep_gone; empty for none.")]
        [SerializeField] private string goneKey = "";

        private PersonTalk _talk;
        private PlayerLocomotion _player;

        public PoiDefinition Poi => poi;

        public float AppearRadius => appearRadius;

        public bool IsShown => cast != null && cast.activeSelf;

        public PersonTalk Talk => _talk != null ? _talk : _talk = GetComponent<PersonTalk>();

        public GameObject Cast => cast;

        /// <summary>While something plays out in the world, a follow (S3b.3), the conversation does not open.</summary>
        public bool Busy { get; set; }

        /// <summary>The people who fight as <paramref name="listener"/>, or none.</summary>
        public Transform[] FightersOf(string listener) => fighters.FirstOrDefault(f => f.listener == listener)?.bodies ?? new Transform[0];

        /// <summary>Setup for scenes built by code.</summary>
        public void Configure(PoiDefinition definition, GameObject people, float radius)
        {
            poi = definition;
            cast = people;
            appearRadius = radius;
        }

        /// <summary>Setup for scenes built by code: who fights as which listener, and what sparing one of them buys.</summary>
        public void Arm(Fighters[] who, string[] spared)
        {
            fighters = who ?? new Fighters[0];
            whenSpared = spared ?? new string[0];
        }

        /// <summary>Setup for scenes built by code: the hours its people keep, and the key that sends them away for good.</summary>
        public void Keep(int from, int until, string gone)
        {
            fromMinute = from;
            untilMinute = until;
            goneKey = gone ?? "";
        }

        public int FromMinute => fromMinute;

        public int UntilMinute => untilMinute;

        public string GoneKey => goneKey;

        /// <summary>
        /// Whether the people are there at <paramref name="tick"/>, minutes from day 1's midnight: within their hours, or
        /// at any hour without them, and not sent away for good.
        /// </summary>
        public static bool Present(IWorldState state, long tick, int from, int until, string gone)
        {
            if (!string.IsNullOrEmpty(gone) && state.IsSet(gone))
            {
                return false;
            }

            if (from < 0 || until < 0)
            {
                return true;
            }

            var minute = tick % (24 * 60);
            return minute >= from && minute < until;
        }

        /// <summary>Whether <paramref name="position"/> is inside the scene's area.</summary>
        public bool Within(Vector3 position)
        {
            var flat = position - transform.position;
            flat.y = 0f;
            return flat.sqrMagnitude <= appearRadius * appearRadius;
        }

        private void Awake()
        {
            if (cast != null)
            {
                cast.SetActive(false);
            }

            if (Talk != null)
            {
                Talk.Available = false;
                Talk.Ended += OnTalkEnded;
            }
        }

        private void OnDestroy()
        {
            if (Talk != null)
            {
                Talk.Ended -= OnTalkEnded;
            }
        }

        /// <summary>10.2, 10.4 §6: the conversation ended in a fight, so its listener's people fight the character here.</summary>
        private void OnTalkEnded(PersonTalk talk, DialogueEnd how)
        {
            var conversation = talk.Conversation;
            if (how != DialogueEnd.Fight || conversation?.FightWith == null || conversation.Fought == null)
            {
                return;
            }

            if (!Fight(conversation.FightWith.id, (won, _) => conversation.AfterFight(won)))
            {
                // No fight can be fought here: the scene goes on as a fight lost, so the POI is still settled.
                Debug.LogWarning($"[PoiStage] {name}: no fight with '{conversation.FightWith.id}' could start; it counts as lost.");
                conversation.AfterFight(false);
            }
        }

        /// <summary>
        /// The people who are <paramref name="listener"/> fight the character where they stand; <paramref name="decided"/>
        /// hears whether the player's side stood and how many were spared, after a spared one's words are raised.
        /// </summary>
        public bool Fight(string listener, Action<bool, int> decided)
        {
            var who = fighters.FirstOrDefault(f => f.listener == listener);
            var bodies = who?.bodies.Where(b => b != null && b.gameObject.activeInHierarchy).ToList();
            var fights = SliceWorldDriver.Running?.Fights;
            return fights != null && bodies != null && bodies.Count > 0
                && fights.StartPeopleFight(who.group, bodies, (won, spared) =>
                {
                    var state = SliceWorldDriver.Running?.State;
                    if (won && spared > 0 && state != null)
                    {
                        foreach (var key in whenSpared)
                        {
                            state.Raise(key);
                        }
                    }

                    decided?.Invoke(won, spared);
                });
        }

        private void Update()
        {
            var world = SliceWorldDriver.Running;
            if (world == null || poi == null || cast == null)
            {
                return;
            }

            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerLocomotion>();
                if (_player == null)
                {
                    return;
                }
            }

            // S4.2: a person of the place keeps hours, and like a resolved scene goes unseen at their end. One sent away
            // for good goes as soon as no conversation holds them, as its words say (S4.3: the sweep, the stranger).
            var gone = !string.IsNullOrEmpty(goneKey) && world.State.IsSet(goneKey);
            var open = world.Director.Phase(poi) == PoiPhase.Open && Present(world.State, world.Clock.Tick, fromMinute, untilMinute, goneKey);
            var near = Within(_player.transform.position);
            var talking = Talk != null && Talk.IsOpen;
            if (!IsShown && open && near)
            {
                Show();
            }
            else if (IsShown && !talking && (gone || (!open && !near)))
            {
                cast.SetActive(false);
            }

            if (Talk != null)
            {
                Talk.Available = IsShown && open && !Busy;
            }
        }

        // Scenes stand still: anyone with an idle pose takes it, the townsfolk's default being their walk.
        private void Show()
        {
            cast.SetActive(true);
            foreach (var animator in cast.GetComponentsInChildren<Animator>())
            {
                if (animator.runtimeAnimatorController != null && animator.HasState(0, Idle))
                {
                    animator.Play(Idle, 0);
                }
            }
        }
    }
}
