using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Slice.Kit;
using GameDev.Slice.Stealth;
using GameDev.World.Poi;
using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>
    /// 11.1 POI B *Follow the coach instead* (S3b.3). The conversation's Veil option, offered in Stealth Mode, lets the
    /// collectors go on: they drag the girl along the lane toward the coal stairs, and the one watching the lane glances
    /// back now and then by his Vision Cone and Notice Meter (10.3 §3). A character who keeps near and unnoticed until
    /// they reach the stairs learns the landing; one who falls behind loses them but sees Veinmarket's chalk; one who is
    /// noticed, or seen walking upright, is in a fight in a worse place. Each way settles B.
    /// </summary>
    public sealed class PoiFollow : MonoBehaviour
    {
        private static readonly int WalkState = Animator.StringToHash("Walk");
        private static readonly int IdleState = Animator.StringToHash("Idle");

        [Tooltip("Who goes on along the lane: the collectors and the girl they drag.")]
        [SerializeField] private Transform[] walkers = new Transform[0];

        [Tooltip("The collector who watches the lane behind them.")]
        [SerializeField] private StealthObserver lookout;

        [Tooltip("The key the conversation's Veil option raises to let them go on.")]
        [SerializeField] private string beginKey = "poi.b.following";

        [Tooltip("Authoring defaults: how far they go along the lane at most (m), their dragging pace (m/s), how near the follower must keep (m), and the lookout's glance back, every so often for so long (s).")]
        [SerializeField] private float distance = 28f;
        [SerializeField] private float pace = 1.2f;
        [SerializeField] private float keepWithin = 18f;
        [SerializeField] private float glanceEvery = 6f;
        [SerializeField] private float glanceFor = 2f;

        [SerializeField] private string[] followed = { "poi.b.followed", "poi.b.heard.landing", "aftermath.loom_lane.girls_taken" };
        [SerializeField] private string[] lostThem = { "poi.b.heard.chalk", "aftermath.loom_lane.girls_taken" };
        [SerializeField] private string[] won = { "aftermath.loom_lane.girls_freed", "against.veinmarket", "poi.b.heard.harrow_buyers" };
        [SerializeField] private string[] lost = { "aftermath.loom_lane.girls_taken" };
        [SerializeField] private string fighters = "collectors";
        [SerializeField] private string spottedLine = "dialogue.poib.spotted";
        [SerializeField] private string followedHint = "ui.poi.b.followed";
        [SerializeField] private string lostHint = "ui.poi.b.lost";

        private readonly List<Animator> _animators = new List<Animator>();
        private PoiStage _stage;
        private PlayerLocomotion _player;
        private Vector3 _along;
        private float _left;
        private float _clock;
        private bool _gone;

        public enum Phase
        {
            Waiting,
            Following,
            Followed,
            Lost,
            Spotted,
        }

        public Phase Now { get; private set; }

        public IReadOnlyList<Transform> Walkers => walkers;

        public StealthObserver Lookout => lookout;

        public float KeepWithin => keepWithin;

        /// <summary>Whether the lookout is looking back now.</summary>
        public bool Glancing => Now == Phase.Following && (_clock % glanceEvery) >= glanceEvery - glanceFor;

        /// <summary>Setup for scenes built by code.</summary>
        public void Configure(Transform[] who, StealthObserver watching)
        {
            walkers = who ?? new Transform[0];
            lookout = watching;
        }

        // The lookout's meter is this host's from the start, so he never leaves the girl to go and look (10.3 §3).
        private void Awake()
        {
            if (lookout != null)
            {
                lookout.Managed = true;
            }
        }

        private void Update()
        {
            var world = SliceWorldDriver.Running;
            var hiding = world != null ? world.Hiding : null;
            _stage ??= GetComponent<PoiStage>();
            if (world == null || hiding == null || hiding.Rules == null || _stage == null || lookout == null)
            {
                return;
            }

            if (Now == Phase.Waiting)
            {
                if (world.State.IsSet(beginKey) && _stage.IsShown && world.Director.Phase(_stage.Poi) == PoiPhase.Open)
                {
                    Begin(hiding);
                }

                return;
            }

            if (Now != Phase.Following && Now != Phase.Lost)
            {
                return;
            }

            if (_player == null && (_player = FindAnyObjectByType<PlayerLocomotion>()) == null)
            {
                return;
            }

            if (world.Fights.InFight)
            {
                return;
            }

            var dt = Time.deltaTime;
            _clock += dt;
            Walk(dt);
            if (Now == Phase.Lost)
            {
                if (_left <= 0f && !_gone)
                {
                    Vanish();
                }

                return;
            }

            var feet = _player.transform.position;
            var lookBack = Glancing ? -_along : _along;
            lookout.transform.rotation = Quaternion.RotateTowards(lookout.transform.rotation, Quaternion.LookRotation(lookBack), 360f * dt);
            if (Seen(hiding, feet, dt))
            {
                Spotted(world, hiding);
                return;
            }

            if (Nearest(feet) > keepWithin)
            {
                Settle(world, lostThem);
                hiding.Show(lostHint, 5f);
                Now = Phase.Lost;
                return;
            }

            if (_left <= 0f)
            {
                Settle(world, followed);
                hiding.Show(followedHint, 6f);
                Now = Phase.Followed;
                Vanish();
            }
        }

        private void Begin(SliceStealth hiding)
        {
            Now = Phase.Following;
            _stage.Busy = true;
            lookout.Managed = true;
            lookout.Bind(hiding.Rules);
            _along = -transform.forward; // the stage faces back up the lane; the stairs lie ahead (SlicePoiStages)
            _along.y = 0f;
            _along.Normalize();
            _left = Clearance(distance);
            _animators.Clear();
            foreach (var w in walkers.Where(w => w != null))
            {
                var animator = w.GetComponentInChildren<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null && animator.HasState(0, WalkState))
                {
                    animator.Play(WalkState, 0);
                    _animators.Add(animator);
                }

                if (w != lookout.transform)
                {
                    w.rotation = Quaternion.LookRotation(_along);
                }
            }
        }

        /// <summary>How far the lane runs clear ahead of the girl, up to <paramref name="most"/>, keeping 1.5 m from what stands there.</summary>
        private float Clearance(float most)
        {
            var from = walkers.Where(w => w != null).Select(w => w.position).DefaultIfEmpty(transform.position).First() + (Vector3.up * 1.2f);
            var walls = LayerMask.GetMask(KitLayers.Structure);
            return Physics.Raycast(from, _along, out var hit, most, walls, QueryTriggerInteraction.Ignore) ? Mathf.Max(0f, hit.distance - 1.5f) : most;
        }

        private void Walk(float dt)
        {
            if (_left <= 0f)
            {
                return;
            }

            var step = Mathf.Min(_left, pace * dt);
            _left -= step;
            var ground = ~LayerMask.GetMask(KitLayers.Character, KitLayers.NoGrid);
            foreach (var w in walkers.Where(w => w != null && w.gameObject.activeInHierarchy))
            {
                var at = w.position + (_along * step);
                if (Physics.Raycast(at + (Vector3.up * 1.5f), Vector3.down, out var hit, 3f, ground, QueryTriggerInteraction.Ignore))
                {
                    at.y = hit.point.y;
                }

                w.position = at;
            }

            if (_left <= 0f)
            {
                foreach (var animator in _animators.Where(a => a != null && a.HasState(0, IdleState)))
                {
                    animator.Play(IdleState, 0);
                }
            }
        }

        /// <summary>The lookout sees a crouching follower by his meter, an upright one at once.</summary>
        private bool Seen(SliceStealth hiding, Vector3 feet, float dt)
        {
            if (!hiding.Active)
            {
                return hiding.Sees(lookout, feet, false);
            }

            if (hiding.Sees(lookout, feet, true))
            {
                return lookout.Meter.See(hiding.TimeToFill(lookout, feet), dt, feet) || lookout.Meter.Noticed;
            }

            lookout.Meter.Unseen(dt);
            return false;
        }

        private float Nearest(Vector3 feet) => walkers.Where(w => w != null && w.gameObject.activeInHierarchy)
            .Select(w => Vector3.Distance(Flat(w.position), Flat(feet))).DefaultIfEmpty(float.MaxValue).Min();

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>11.1: spotted, a forced Confront in a worse place, the follower standing and the collectors ready.</summary>
        private void Spotted(SliceWorldDriver world, SliceStealth hiding)
        {
            Now = Phase.Spotted;
            if (hiding.Active)
            {
                hiding.NoticedBy(lookout, Loc.Get(spottedLine));
            }
            else
            {
                lookout.Say(Loc.Get(spottedLine), 3f);
            }

            lookout.transform.rotation = Quaternion.LookRotation(-_along);
            foreach (var animator in _animators.Where(a => a != null && a.HasState(0, IdleState)))
            {
                animator.Play(IdleState, 0);
            }

            if (!_stage.Fight(fighters, (fightWon, spared) => Settle(world, fightWon ? won : lost)))
            {
                Settle(world, lost);
            }

            _stage.Busy = false;
        }

        private void Settle(SliceWorldDriver world, IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                world.State.Raise(key);
            }

            if (world.Director.Phase(_stage.Poi) == PoiPhase.Open)
            {
                world.Director.ResolveParticipated(_stage.Poi);
            }

            _stage.Busy = false;
        }

        // Down the coal stairs to the coach that waits beyond, out of the follower's sight.
        private void Vanish()
        {
            _gone = true;
            foreach (var w in walkers.Where(w => w != null))
            {
                w.gameObject.SetActive(false);
            }
        }
    }
}
