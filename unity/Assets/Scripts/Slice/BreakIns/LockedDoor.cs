using System;
using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.UI;
using GameDev.World.Portals;
using UnityEngine;

namespace GameDev.Slice.BreakIns
{
    /// <summary>
    /// A locked street door (S5c.5; 10.3 §6.1): its portal stays sealed until the door is open. With Lockpicks E picks
    /// it: the try's tick passes, the hands go to the lock, and at the clip's key frame the world pauses for the Reaction
    /// Check of Theft and Dexterity against the Lock. A good result opens the door until morning, and the next E goes
    /// through; a bad one breaks a pick. Anyone outside who sees a try notices (<see cref="CrimeWatch"/>). A door with a
    /// key of its own, as Thomas Wren's, opens with the key for good. Every dwelling's street door carries one, put by
    /// the Dwelling scene's hook when its city loads.
    /// </summary>
    [RequireComponent(typeof(Portal))]
    public sealed class LockedDoor : MonoBehaviour
    {
        /// <summary>Authoring default, m: beyond this the door leaves the player be.</summary>
        public const float Near = 4f;

        /// <summary>Authoring default, m: how far from the lock the character stands to pick it.</summary>
        public const float StandOff = 0.6f;

        /// <summary>The hands at the lock: Q1 Interact, as brewing's, its key frame measured in AN3 (07.1 §8a).</summary>
        public const string Clip = "Interact";

        /// <summary>Kit standard §3: the opening's middle, from the door's pivot, as <see cref="Portal"/> reads it.</summary>
        private static readonly Vector3 OpeningCentre = new Vector3(1.5f, 1.0f, 0f);

        private Portal _portal;
        private PlayerLocomotion _player;
        private HudView _hud;
        private Transform _lock;

        /// <summary>The place whose door this is: a dwelling's id, or the trial's room.</summary>
        public string Place { get; private set; }

        /// <summary>10.3 §6.1: 2 in a poor home, 4 in a middling one, 6 in a rich one.</summary>
        public int Lock { get; private set; }

        /// <summary>Whether the door is open for good, as Thomas Wren's once his key turned.</summary>
        public Func<bool> OpenForGood { get; set; }

        /// <summary>Whether the character holds the door's own key, the prompt it gives, and what using it does.</summary>
        public Func<bool> HasKey { get; set; }

        public string KeyPrompt { get; set; }

        public Action UseKey { get; set; }

        /// <summary>Whether the door is open now.</summary>
        public bool IsOpen
        {
            get
            {
                var breakIns = SliceWorldDriver.Running != null ? SliceWorldDriver.Running.BreakIns : null;
                return (OpenForGood != null && OpenForGood()) || (breakIns != null && breakIns.IsOpen(Place));
            }
        }

        public void Configure(string place, int lockValue)
        {
            Place = place;
            Lock = lockValue;
            Portal().Sealed = !IsOpen;
        }

        /// <summary>The point at the lock the character faces as it picks it.</summary>
        public Transform LockPoint
        {
            get
            {
                if (_lock == null)
                {
                    _lock = new GameObject("Lock").transform;
                    _lock.SetParent(transform, false);
                    _lock.localPosition = OpeningCentre;
                }

                return _lock;
            }
        }

        private Portal Portal() => _portal != null ? _portal : _portal = GetComponent<Portal>();

        private void Awake() => Portal().Sealed = true;

        private void Update()
        {
            var world = SliceWorldDriver.Running;
            var breakIns = world != null ? world.BreakIns : null;
            if (breakIns == null || Place == null)
            {
                return;
            }

            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            if (_player == null || (_player.transform.position - transform.position).sqrMagnitude > Near * Near)
            {
                return;
            }

            var portal = Portal();
            portal.Sealed = !IsOpen;
            if (!portal.Sealed)
            {
                return;
            }

            var key = HasKey != null && HasKey();
            portal.SealedPrompt = key && KeyPrompt != null ? KeyPrompt : breakIns.Picks > 0 ? "ui.prompt.lock.pick" : "ui.prompt.locked";
            var input = GameInputService.Instance;
            if (input == null || !input.InteractPressed || SceneTransition.IsTraveling || !portal.InReach(_player.transform.position))
            {
                return;
            }

            // E belongs to the prompt the HUD shows, and the character must be free.
            _hud = _hud != null && _hud.isActiveAndEnabled ? _hud : FindAnyObjectByType<HudView>();
            if (_hud == null || _hud.AnyWindowOpen || _hud.PromptText != Loc.Get(portal.SealedPrompt)
                || world.Work == null || world.Work.Busy || world.Clock.IsPaused || (world.Fights != null && world.Fights.InFight))
            {
                return;
            }

            if (key)
            {
                UseKey?.Invoke();
                breakIns.Open(Place);
                portal.Sealed = false;
                SceneTransition.Travel(portal.Target);
                return;
            }

            if (breakIns.Picks <= 0)
            {
                _hud.ShowMessage(Loc.Get("ui.lock.nopicks"));
                return;
            }

            Pick(world, breakIns);
        }

        /// <summary>Begins a try at the lock; false when the work cannot begin.</summary>
        public bool Pick(SliceWorldDriver world, SliceBreakIns breakIns)
        {
            world.Clock.AdvanceTicks(1); // 10.3 §6.1: each try takes one tick, before the hands reach the lock
            return world.Work.Begin(new WorkStroke
            {
                Clip = Clip,
                Bar = () => breakIns.Bar(Place, Lock),
                CaptionKey = "ui.lock.caption",
                PauseWorld = true,
                Steady = true,
                Settle = result => Settle(world, breakIns, result),
                Face = LockPoint,
                StandOff = StandOff,
            });
        }

        private void Settle(SliceWorldDriver world, SliceBreakIns breakIns, ReactionResult result)
        {
            var opened = breakIns.Resolve(Place, Lock, result);
            _hud = _hud != null && _hud.isActiveAndEnabled ? _hud : FindAnyObjectByType<HudView>();
            _hud?.ShowMessage(opened ? Loc.Get("ui.lock.opened") : Loc.Format("ui.lock.broke", breakIns.Picks));

            // 10.3 §6: anyone outside who sees a lock picked notices; the first who does says so.
            var feet = _player != null ? _player.transform.position : transform.position;
            var seeing = CrimeWatch.Seeing(world, feet);
            if (seeing.Count > 0)
            {
                CrimeWatch.Noticed(world, seeing[0], feet, Place, "ui.breakin.witness");
            }
        }
    }
}
