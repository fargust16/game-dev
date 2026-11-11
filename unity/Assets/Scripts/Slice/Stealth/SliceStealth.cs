using System;
using System.Collections.Generic;
using GameDev.Confront.Stealth;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Atmosphere;
using GameDev.Slice.Interiors;
using GameDev.Slice.People;
using GameDev.World.Timekeeping;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.Slice.Stealth
{
    /// <summary>
    /// Stealth in free movement (10.3, S3b.1). C enters and leaves Stealth Mode; the character then moves at three
    /// quarters of walking speed. Every townsperson and named person is an observer whose Notice Meter fills while the
    /// crouching character is in its cone and in sight, by 10.3 §3's Δ with the Darkness of the place and the observer's
    /// Alert; a full meter is a notice: the observer says a line, and the character stands up. A fight takes stealth
    /// over while it lasts (<c>SliceConfront</c>). Dialogue pauses the world, and the meters with it.
    /// </summary>
    public sealed class SliceStealth : MonoBehaviour
    {
        public const int Lines = 3;           // ui.stealth.noticed.1 … 3
        private const float LineSeconds = 3.5f;
        private const float RefusedSeconds = 2.5f;
        private const float StandingHeight = 1.6f;
        private const float RescanSeconds = 2f;
        private const float SunReach = 120f;   // m toward the sun checked for the shadow
        private static readonly HashSet<string> Wardens = new HashSet<string> { "coyle" };

        private readonly RaycastHit[] _hits = new RaycastHit[8];
        private StealthRules _rules;
        private CharacterSheet _character;
        private IWorldClock _clock;
        private Func<bool> _inFight;
        private PlayerLocomotion _player;
        private VeilmouthSky _sky;
        private float _rescan;
        private LayerMask _walls = ~0;

        /// <summary>Whether the character is in Stealth Mode.</summary>
        public bool Active { get; private set; }

        public StealthRules Rules => _rules;

        public CharacterSheet Character => _character;

        /// <summary>The last observer to notice the character.</summary>
        public StealthObserver LastNoticer { get; private set; }

        /// <summary>A short line for the player, such as why Stealth Mode was refused, and until when it shows.</summary>
        public string Hint { get; private set; }

        public float HintUntil { get; private set; }

        public event Action<StealthObserver> Noticed;

        public void Configure(StealthRules rules, CharacterSheet character, IWorldClock clock, Func<bool> inFight)
        {
            _rules = rules;
            _character = character;
            _clock = clock;
            _inFight = inFight;
            if (_clock != null)
            {
                _clock.TickBoundary += OnTick;
            }

            if (GetComponent<StealthHud>() == null)
            {
                gameObject.AddComponent<StealthHud>();
            }
        }

        /// <summary>Enters or leaves Stealth Mode; entering is refused while an observer that noticed the character watches it.</summary>
        public bool SetActive(bool active)
        {
            if (active && !Active)
            {
                foreach (var o in StealthObserver.All)
                {
                    if (o.Meter != null && o.Meter.Noticed && o.Meter.Seeing)
                    {
                        Tell("ui.stealth.watched");
                        return false;
                    }
                }
            }

            Active = active;
            if (_player != null)
            {
                _player.CrouchShare = Active && _rules != null ? _rules.CrouchSpeed : 0f;
            }

            return true;
        }

        /// <summary>
        /// 10.3 §3: the Darkness at a place, added to the hider's Stealth skill. At night (the lamps lit) a street farther
        /// than 6 m from a lit lamp is dark, +4, and the edge of a lamp's light, beyond half that, is dim, +2; by day a
        /// place the sun does not reach is in shadow, +2. Interiors count as lit until their rooms can be dark (S3b.2).
        /// </summary>
        public int Darkness(Vector3 at)
        {
            if (_rules == null || _sky == null || _clock == null)
            {
                return 0;
            }

            if (InteriorLighting.LampsLit(_clock.MinuteOfDay))
            {
                var nearest = float.MaxValue;
                foreach (var lamp in _sky.StreetLamps)
                {
                    if (lamp.Light != null && lamp.Light.isActiveAndEnabled)
                    {
                        nearest = Mathf.Min(nearest, Vector3.Distance(lamp.Light.transform.position, at));
                    }
                }

                return nearest > _rules.LampReach ? _rules.NightStreetDarkness
                    : nearest > _rules.LampReach * 0.5f ? _rules.ShadowDarkness : 0;
            }

            var sun = _sky.Sun;
            if (sun == null)
            {
                return 0;
            }

            var from = at + (Vector3.up * 1f);
            return Blocked(from, from - (sun.transform.forward * SunReach), null, null) ? _rules.ShadowDarkness : 0;
        }

        /// <summary>The time a meter takes to fill against this observer at this distance, 10.3 §3.</summary>
        public double TimeToFill(StealthObserver o, Vector3 at)
        {
            var delta = _rules.Delta(_character.Get(CharacterAttribute.Dexterity), _character.Get(CharacterSkill.Stealth), Darkness(at),
                o.Perception, o.Meter.Alert);
            var cells = Flat(at - o.transform.position).magnitude; // the slice's cells are 1 m
            return _rules.TimeToFill(delta, cells);
        }

        /// <summary>Whether the observer has the character in its cone and in sight.</summary>
        public bool Sees(StealthObserver o, Vector3 feet, bool crouching)
        {
            var eye = o.transform.position + (Vector3.up * (o.EyeHeight > 0f ? o.EyeHeight : _rules.EyeHeight));
            var head = feet + (Vector3.up * (crouching ? _rules.CrouchHeight : StandingHeight));
            return _rules.InCone(o.transform.position, o.transform.forward, feet, 1f) && !Blocked(eye, head, o.transform, _player.transform);
        }

        private void Update()
        {
            if (_rules == null || _character == null)
            {
                return;
            }

            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerLocomotion>();
                _sky = FindAnyObjectByType<VeilmouthSky>();
                if (_player == null)
                {
                    return;
                }

                _player.CrouchShare = Active ? _rules.CrouchSpeed : 0f;
            }

            _rescan -= Time.unscaledDeltaTime;
            if (_rescan <= 0f)
            {
                _rescan = RescanSeconds;
                Enlist();
            }

            if ((_inFight != null && _inFight()) || (_clock != null && _clock.IsPaused))
            {
                return;
            }

            var input = GameInputService.Instance;
            if (input != null && input.StealthPressed)
            {
                SetActive(!Active);
            }

            var feet = _player.transform.position;
            var seconds = Time.deltaTime;
            for (var i = StealthObserver.All.Count - 1; i >= 0; i--)
            {
                Look(StealthObserver.All[i], feet, seconds);
            }
        }

        private void Look(StealthObserver o, Vector3 feet, float seconds)
        {
            if (o.Managed)
            {
                return; // a beast's watch is its host's (S3b.2)
            }

            if (o.Meter == null)
            {
                o.Bind(_rules);
            }

            var sees = Sees(o, feet, Active);
            if (Active && sees)
            {
                if (o.Meter.See(TimeToFill(o, feet), seconds, feet))
                {
                    Notice(o);
                }
            }
            else if (sees)
            {
                o.Meter.Watch(feet);
            }
            else
            {
                o.Meter.Unseen(seconds);
            }

            o.Behave(feet, seconds);
        }

        private void Notice(StealthObserver o) => NoticedBy(o, Loc.Get($"ui.stealth.noticed.{1 + (Stable(o.name) % Lines)}"));

        /// <summary>An observer noticed the character in Stealth Mode: it says its line, and the character stands up.</summary>
        public void NoticedBy(StealthObserver o, string line)
        {
            LastNoticer = o;
            o.Say(line, LineSeconds);
            SetActive(false);
            Noticed?.Invoke(o);
        }

        private void Tell(string key)
        {
            Hint = Loc.Get(key);
            HintUntil = Time.time + RefusedSeconds;
        }

        /// <summary>Every townsperson and named person in the loaded scenes watches; Wardens with Perception 4.</summary>
        private void Enlist()
        {
            foreach (var walker in FindObjectsByType<TownsfolkWalker>(FindObjectsSortMode.None))
            {
                Watch(walker.gameObject, 2);
            }

            foreach (var person in FindObjectsByType<NamedPerson>(FindObjectsSortMode.None))
            {
                Watch(person.gameObject, Wardens.Contains(person.Id) ? 4 : 2);
            }
        }

        private void Watch(GameObject body, int perception)
        {
            if (body.GetComponent<StealthObserver>() != null)
            {
                return;
            }

            var o = body.AddComponent<StealthObserver>();
            o.Perception = perception;
            o.Bind(_rules);
        }

        private void OnTick(long tick)
        {
            foreach (var o in StealthObserver.All)
            {
                o.Meter?.Tick();
            }
        }

        /// <summary>Whether structure stands between two points; the people at either end do not count.</summary>
        private bool Blocked(Vector3 from, Vector3 to, Transform a, Transform b)
        {
            var way = to - from;
            var count = Physics.RaycastNonAlloc(from, way.normalized, _hits, way.magnitude, _walls, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i].transform;
                if ((a == null || !hit.IsChildOf(a)) && (b == null || !hit.IsChildOf(b)) && hit.GetComponentInParent<StealthObserver>() == null)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>A hash that is the same in every run, for the choice of a line.</summary>
        private static int Stable(string s)
        {
            var h = 0;
            foreach (var ch in s)
            {
                h = ((h * 31) + ch) & 0x7fffffff;
            }

            return h;
        }

        private void OnEnable() => SceneManager.activeSceneChanged += OnScene;

        private void OnDisable() => SceneManager.activeSceneChanged -= OnScene;

        private void OnDestroy()
        {
            if (_clock != null)
            {
                _clock.TickBoundary -= OnTick;
            }
        }

        private void OnScene(Scene from, Scene to)
        {
            _player = null;
            _sky = null;
            _rescan = 0f;
            Active = false;
        }
    }
}
