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

        /// <summary>05.1 Двойник out of a fight (S5b.8): no cone sees the character meanwhile.</summary>
        public bool Unseen { get; set; }

        /// <summary>05.1 Пелена out of a fight (S5b.8): a cloud between an eye and the character puts the look out.</summary>
        public Func<Vector3, Vector3, bool> Screened { get; set; }

        /// <summary>05.1 Родство (S5b.8): an observer that senses the character, walls or no walls, a follower of its Arcana near.</summary>
        public Func<StealthObserver, bool> Kin { get; set; }

        /// <summary>S4.4: the world state key the black drop raises when drunk (11.1 *Slice build (S4)*; 99 VS-DROP).</summary>
        public const string FogKey = "bargain.black_drop";

        /// <summary>Whether the fog speaks for the character, the black drop drunk; set by the world.</summary>
        public Func<bool> FogSpeaks { get; set; }

        private bool Fog => FogSpeaks != null && FogSpeaks();

        /// <summary>
        /// The Perception the stealth HUD reads for the Notice Meter (10.3 §4). The fog speaks the enemies' steps: the
        /// black drop shows every meter, as Perception 4 would.
        /// </summary>
        public int SightPerception => FogValue(_character.Get(CharacterAttribute.Perception), _rules.MeterFromPerception, Fog);

        /// <summary>The Intelligence the stealth HUD reads for the Vision Cones: the black drop shows them clear, as Intelligence 6 would.</summary>
        public int SightIntelligence => FogValue(_character.Get(CharacterAttribute.Intelligence), _rules.ConesClearFromIntelligence, Fog);

        /// <summary>What the fog makes of a value the HUD reads: at least <paramref name="floor"/> once it speaks.</summary>
        public static int FogValue(int actual, int floor, bool fog) => fog ? Math.Max(actual, floor) : actual;

        /// <summary>
        /// The Darkness added to the hider's Stealth skill (10.3 §3). The black drop's price: the fog speaks the
        /// character's steps too, so no shadow hides it, and Darkness adds nothing.
        /// </summary>
        public int HidingDarkness(Vector3 at) => HiddenBy(Darkness(at), Fog);

        public static int HiddenBy(int darkness, bool fog) => fog ? 0 : darkness;

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
            var delta = _rules.Delta(_character.Get(CharacterAttribute.Dexterity), _character.Get(CharacterSkill.Stealth), HidingDarkness(at),
                o.Perception, o.Meter.Alert);
            var cells = Flat(at - o.transform.position).magnitude; // the slice's cells are 1 m
            return _rules.TimeToFill(delta, cells);
        }

        /// <summary>Whether the observer has the character in its cone and in sight.</summary>
        public bool Sees(StealthObserver o, Vector3 feet, bool crouching)
        {
            if (Kin != null && Kin(o))
            {
                return true; // 05.1 Родство: it senses the character, the price of the Arcana
            }

            if (Unseen)
            {
                return false;
            }

            var eye = o.transform.position + (Vector3.up * (o.EyeHeight > 0f ? o.EyeHeight : _rules.EyeHeight));
            var head = feet + (Vector3.up * (crouching ? _rules.CrouchHeight : StandingHeight));
            return _rules.InCone(o.transform.position, o.transform.forward, feet, 1f) && !Blocked(eye, head, o.transform, _player.transform)
                   && (Screened == null || !Screened(eye, head));
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
                _slipping.Add(o);
                if (o.Meter.See(TimeToFill(o, feet), seconds, feet))
                {
                    Notice(o);
                }
            }
            else if (o.Meter.Fill <= 0f && !o.Meter.Noticed && _slipping.Remove(o))
            {
                Train(o, true); // the look ended and its meter ran empty: the character slipped past
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

        /// <summary>
        /// Every use grows (07.1 §3, S5b.2): each look the character meets in Stealth Mode is one use of Stealth and
        /// Dexterity, slipped past or noticed, the observer's Perception, raised when Alert, its difficulty on §1's bands.
        /// A look counts once its meter has run empty again, so a cone's edge flickering over the character is one look.
        /// </summary>
        private void Train(StealthObserver o, bool slipped)
        {
            var difficulty = (int)Math.Round(o.Perception * (o.Meter != null && o.Meter.Alert ? ReactionCheck.AlertPerception : 1d));
            _character?.Train(CharacterSkill.Stealth, Growth.StageOfDifficulty(difficulty), slipped ? CheckOutcome.Normal : CheckOutcome.Failure,
                new[] { CharacterAttribute.Dexterity });
        }

        private readonly HashSet<StealthObserver> _slipping = new HashSet<StealthObserver>();

        private void Notice(StealthObserver o) => NoticedBy(o, Loc.Get($"ui.stealth.noticed.{1 + (Stable(o.name) % Lines)}"));

        /// <summary>An observer noticed the character in Stealth Mode: it says its line, and the character stands up.</summary>
        public void NoticedBy(StealthObserver o, string line)
        {
            if (_slipping.Remove(o))
            {
                Train(o, false);
            }

            LastNoticer = o;
            o.Say(line, LineSeconds);
            SetActive(false);
            Noticed?.Invoke(o);
        }

        /// <summary>A line at the foot of the screen for so long, where the Stealth Mode badge shows (S3b.3: what a follow came to).</summary>
        public void Show(string key, float seconds)
        {
            Hint = Loc.Get(key);
            HintUntil = Time.time + seconds;
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
