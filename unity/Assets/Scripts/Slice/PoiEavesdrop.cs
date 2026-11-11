using System.Linq;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Slice.Stealth;
using GameDev.World.Poi;
using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>
    /// 11.1 POI A *Veil and listen first* (S3b.3): a character in Stealth Mode who keeps within reach of the Warden in the
    /// door for as long as his words take, unnoticed, overhears what the Wardens came for, an Argument in this life. The
    /// Warden watches by his own Vision Cone and Notice Meter (10.3 §3); one who notices the listener says so, and both
    /// sides turn colder, but the listener still caught the cabinet. Either way it happens once, and it settles nothing:
    /// the choice is still the player's.
    /// </summary>
    public sealed class PoiEavesdrop : MonoBehaviour
    {
        [Tooltip("The one overheard, who watches by his cone.")]
        [SerializeField] private StealthObserver speaker;

        [Tooltip("Authoring defaults: how near the listener must keep (m, flat), and how long the words take (s).")]
        [SerializeField] private float reach = 4f;
        [SerializeField] private float seconds = 6f;

        [Tooltip("Raised when the words are overheard: the Argument.")]
        [SerializeField] private string[] heard = { "poi.a.heard.list" };

        [Tooltip("Raised when the listener is noticed: the partial tell and the mark.")]
        [SerializeField] private string[] spotted = { "poi.a.heard.cabinet", "poi.a.spotted" };

        [Tooltip("The conversation's listeners who turn colder when the listener is noticed, a step each.")]
        [SerializeField] private string[] colder = { "wardens", "neighbours" };

        [SerializeField] private string overheardLine = "dialogue.poia.overheard";
        [SerializeField] private string spottedLine = "dialogue.poia.spotted";

        private PoiStage _stage;
        private PlayerLocomotion _player;
        private float _listened;

        public StealthObserver Speaker => speaker;

        public float Reach => reach;

        public float Seconds => seconds;

        /// <summary>How long the listener has listened unnoticed, in seconds.</summary>
        public float Listened => _listened;

        /// <summary>Setup for scenes built by code.</summary>
        public void Configure(StealthObserver overheard) => speaker = overheard;

        // The Warden's meter is this host's from the start, so he never walks off his post to look (10.3 §3).
        private void Awake()
        {
            if (speaker != null)
            {
                speaker.Managed = true;
            }
        }

        private void Update()
        {
            var world = SliceWorldDriver.Running;
            var hiding = world != null ? world.Hiding : null;
            _stage ??= GetComponent<PoiStage>();
            if (hiding == null || hiding.Rules == null || speaker == null || _stage == null || !_stage.IsShown || !speaker.isActiveAndEnabled
                || world.Director.Phase(_stage.Poi) != PoiPhase.Open || Done(world))
            {
                return;
            }

            if (_player == null && (_player = FindAnyObjectByType<PlayerLocomotion>()) == null)
            {
                return;
            }

            speaker.Managed = true;
            speaker.Bind(hiding.Rules);
            var dt = Time.deltaTime;
            var feet = _player.transform.position;
            var flat = feet - speaker.transform.position;
            flat.y = 0f;
            if (!hiding.Active || flat.magnitude > reach || world.Fights.InFight)
            {
                speaker.Meter.Unseen(dt);
                _listened = 0f; // the words are missed, and the next try starts them again
                return;
            }

            if (hiding.Sees(speaker, feet, true))
            {
                if (speaker.Meter.See(hiding.TimeToFill(speaker, feet), dt, feet) || speaker.Meter.Noticed)
                {
                    Spotted(world, hiding);
                    return;
                }
            }
            else
            {
                speaker.Meter.Unseen(dt);
            }

            _listened += dt;
            if (_listened >= seconds)
            {
                foreach (var key in heard)
                {
                    world.State.Raise(key);
                }

                speaker.Say(Loc.Get(overheardLine), seconds);
            }
        }

        private bool Done(SliceWorldDriver world) => heard.All(world.State.IsSet) || spotted.All(world.State.IsSet);

        private void Spotted(SliceWorldDriver world, SliceStealth hiding)
        {
            foreach (var key in spotted)
            {
                world.State.Raise(key);
            }

            foreach (var id in colder)
            {
                world.Standing.ChangeAttitude(id, -1);
            }

            hiding.NoticedBy(speaker, Loc.Get(spottedLine));
            speaker.Meter.Reset();
        }
    }
}
