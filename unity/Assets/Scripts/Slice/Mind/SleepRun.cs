using System.Collections.Generic;
using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Input;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Slice.Fights;
using GameDev.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.Slice.Mind
{
    /// <summary>
    /// Sleep in the scene (S5c.2; 18 §3, 11 *Sleep*). Z out of a fight in the city, or E at the Soot &amp; Bell's bed, opens
    /// a page: sleep until 07:00 or for 6 hours, the room at its price. The character lies down where it is, and the
    /// world's ticks pass quickly with the world going on, <see cref="SliceTuning.SleepTicksPerSecond"/> of them a second,
    /// the minutes awake not counting. A thief in the street takes half the coins at its tick and the sleeper sleeps on; a
    /// beast outside the walls wakes the sleeper with its blow. A fight or an Alarm heard wakes it too, and a sleep cut
    /// short gives only the time it had. On waking <see cref="SliceMind.Slept"/> settles Fatigue and Sanity, a full sleep
    /// restores Will Power and, with no Bleeding or Poison left, HP, and the game writes the Suspend Save.
    /// </summary>
    public sealed class SleepRun : MonoBehaviour
    {
        /// <summary>Authoring default, m: the page closes when the character walks this far from where it opened.</summary>
        private const float PageReach = 1.5f;

        private static readonly int Down = Animator.StringToHash("Down");
        private static readonly int Crouch = Animator.StringToHash("Crouch");

        private SliceWorldDriver _world;
        private PlayerLocomotion _player;
        private HudView _hud;
        private bool _pageOpen;
        private SleepPlace _pagePlace;
        private Vector3 _pageAt;
        private string _pageNote;
        private SleepPlace _place;
        private long _start;
        private long _end;
        private long? _visit;
        private int _stolen;
        private bool _wake;
        private double _owed;

        /// <summary>Whether the character sleeps now.</summary>
        public bool Sleeping { get; private set; }

        /// <summary>For tests: the share of sleeps a visitor comes to, in place of the place's one in so many.</summary>
        public double? VisitChance { get; set; }

        /// <summary>Raised on waking: the minutes slept, and whether the sleep was full.</summary>
        public event System.Action<long, bool> Woke;

        public bool IsPageOpen => _pageOpen && _hud != null && _hud.IsShopOpen;

        public void Configure(SliceWorldDriver world)
        {
            _world = world;
            SliceAlarm.Raised -= OnAlarm;
            SliceAlarm.Raised += OnAlarm;
        }

        private void OnDestroy() => SliceAlarm.Raised -= OnAlarm;

        /// <summary>The key of the reason the character cannot sleep now, or null.</summary>
        public string Fault(SleepPlace place)
        {
            var w = _world;
            if (w == null || Sleeping)
            {
                return "ui.sleep.fault.busy";
            }

            if (w.Fights != null && w.Fights.InFight)
            {
                return "ui.bag.fault.fight";
            }

            if (w.TalkOpen || (w.Reaction != null && w.Reaction.Running) || (w.Work != null && w.Work.Busy)
                || (w.Advancement != null && w.Advancement.Assimilating) || SceneTransition.IsTraveling)
            {
                return "ui.sleep.fault.busy";
            }

            if ((w.Fights != null && w.Fights.AlarmUnderWay) || (w.Beasts != null && w.Beasts.Alarmed))
            {
                return "ui.sleep.fault.alarm";
            }

            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            if (_player == null || _player.InWater)
            {
                return "ui.sleep.fault.here";
            }

            if (place == SleepPlace.Room && (w.Bag == null || w.Bag.Bronze < SliceSleep.RoomPrice))
            {
                return "ui.shop.fault.money";
            }

            return null;
        }

        /// <summary>Opens the page of sleep for <paramref name="place"/>, as Z or the bed's E does; false when it cannot open.</summary>
        public bool OpenPage(SleepPlace place)
        {
            _hud = _hud != null ? _hud : FindAnyObjectByType<HudView>();
            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            if (_hud == null || _player == null || Sleeping)
            {
                return false;
            }

            _pageOpen = true;
            _pagePlace = place;
            _pageAt = _player.transform.position;
            _pageNote = null;
            ShowPage();
            return true;
        }

        public void ClosePage()
        {
            if (_pageOpen && _hud != null)
            {
                _hud.CloseShop();
            }

            _pageOpen = false;
        }

        private void ShowPage()
        {
            var w = _world;
            var now = w.Clock.Tick;
            var lines = new List<PageLine>();
            var heading = _pagePlace == SleepPlace.Room ? Loc.Format("ui.sleep.heading.room", Coins.Show(SliceSleep.RoomPrice))
                : Loc.Get(_pagePlace == SleepPlace.Street ? "ui.sleep.heading.street" : "ui.sleep.heading.wild");
            lines.Add(new PageLine(heading, heading: true));
            var morning = SliceSleep.EndOf(now, SleepLength.Morning) - now;
            lines.Add(new PageLine(Loc.Format("ui.sleep.morning", Hours(morning)), () => Choose(SleepLength.Morning)));
            lines.Add(new PageLine(Loc.Format("ui.sleep.six", Hours(SliceSleep.ShortMinutes)), () => Choose(SleepLength.SixHours)));
            _hud.ShowShop(Loc.Get("ui.sleep.title"), BagPage.Money(w.Bag), lines, _pageNote ?? Loc.Get("ui.sleep.hint"));
        }

        private static string Hours(long minutes) => (minutes / 60d).ToString(minutes % 60 == 0 ? "0" : "0.#", System.Globalization.CultureInfo.InvariantCulture);

        private void Choose(SleepLength length)
        {
            var fault = Begin(_pagePlace, length);
            if (fault != null)
            {
                _pageNote = Loc.Get(fault);
                ShowPage();
            }
        }

        /// <summary>Begins a sleep in <paramref name="place"/>: the room's price paid, the visitor drawn. The key of a fault, or null.</summary>
        public string Begin(SleepPlace place, SleepLength length)
        {
            var fault = Fault(place);
            if (fault != null)
            {
                return fault;
            }

            var w = _world;
            if (place == SleepPlace.Room && !w.Bag.Spend(SliceSleep.RoomPrice))
            {
                return "ui.shop.fault.money";
            }

            ClosePage();
            _place = place;
            _start = w.Clock.Tick;
            _end = SliceSleep.EndOf(_start, length);
            _visit = SliceSleep.Visit(w.Seed, place, _start, _end, VisitChance);
            _stolen = 0;
            _wake = false;
            _owed = 0d;
            Sleeping = true;
            if (w.Mind != null)
            {
                w.Mind.Asleep = true;
            }

            LieDown();
            _hud?.ShowMessage(Loc.Get("ui.sleep.asleep"));
            Debug.Log($"[Slice] asleep in {place} from tick {_start} to {_end}{(_visit != null ? $", a visitor at {_visit}" : string.Empty)}.");
            return null;
        }

        private void Update()
        {
            if (Sleeping)
            {
                Pass();
                return;
            }

            if (_pageOpen)
            {
                KeepPage();
                return;
            }

            Listen();
        }

        /// <summary>Z out of a fight in the city opens the page; elsewhere it says where one may sleep.</summary>
        private void Listen()
        {
            var input = GameInputService.Instance;
            var w = _world;
            if (input == null || w == null || !input.SleepPressed || (w.Fights != null && w.Fights.InFight))
            {
                return;
            }

            _hud = _hud != null ? _hud : FindAnyObjectByType<HudView>();
            if (_hud != null && _hud.AnyWindowOpen)
            {
                return;
            }

            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            if (_player == null)
            {
                return;
            }

            if (SceneManager.GetActiveScene().name != w.CityScene)
            {
                _hud?.ShowMessage(Loc.Get("ui.sleep.fault.indoors"));
                return;
            }

            var place = SliceSleep.Outdoors(_player.transform.position);
            var fault = Fault(place);
            if (fault != null)
            {
                _hud?.ShowMessage(Loc.Get(fault));
                return;
            }

            OpenPage(place);
        }

        private void KeepPage()
        {
            var input = GameInputService.Instance;
            var moved = _player == null || (_player.transform.position - _pageAt).sqrMagnitude > PageReach * PageReach;
            if (_hud == null || !_hud.IsShopOpen || moved || (_world.Fights != null && _world.Fights.InFight)
                || (input != null && (input.CancelPressed || input.SleepPressed)))
            {
                ClosePage();
            }
        }

        // A conversation, an Accusation or a paused world (the menu) holds the ticks; the sleeper sleeps on.
        private bool Held() =>
            _world.TalkOpen || (_world.Accusation != null && _world.Accusation.IsOpen) || _world.Clock.IsPaused;

        private void Pass()
        {
            var w = _world;
            if (w == null || _player == null)
            {
                Wake(true);
                return;
            }

            if (_wake || (w.Fights != null && w.Fights.InFight))
            {
                Wake(true);
                return;
            }

            _player.enabled = false;
            if (Held())
            {
                return;
            }

            _owed += Time.deltaTime * Mathf.Max(1f, SliceTuning.Current.SleepTicksPerSecond);
            while (_owed >= 1d && Sleeping && !_wake && !Held())
            {
                _owed -= 1d;
                w.Clock.AdvanceTicks(1);
                if (_visit is long visit && w.Clock.Tick >= visit)
                {
                    _visit = null;
                    Visited();
                }

                if (w.Fights != null && w.Fights.InFight)
                {
                    _wake = true;
                }

                if (w.Clock.Tick >= _end)
                {
                    Wake(false);
                    return;
                }
            }

            if (_wake)
            {
                Wake(true);
            }
        }

        /// <summary>The street's thief takes half the coins and goes; the wild's beast attacks, and the sleeper wakes.</summary>
        private void Visited()
        {
            var w = _world;
            if (_place == SleepPlace.Street)
            {
                var take = SliceSleep.ThiefTakes(w.Bag != null ? w.Bag.Bronze : 0);
                if (take > 0 && w.Bag.Spend(take))
                {
                    _stolen += take;
                    Debug.Log($"[Slice] a Veinmarket thief took {take} bronze from the sleeper.");
                }

                return;
            }

            if (_place == SleepPlace.Wild)
            {
                StandUp(); // a fight needs the character on its feet
                if (w.Beasts != null && w.Beasts.FindSleeper(_player.transform.position))
                {
                    _wake = true;
                    _hud?.ShowMessage(Loc.Get("ui.sleep.beast"));
                }
                else
                {
                    LieDown();
                }
            }
        }

        private void OnAlarm(Alarm alarm)
        {
            if (Sleeping && _player != null && SliceAlarm.Hears(alarm.At, _player.transform.position))
            {
                _wake = true; // 18 §3: an Alarm wakes the sleeper
            }
        }

        private void Wake(bool cut)
        {
            if (!Sleeping)
            {
                return;
            }

            Sleeping = false;
            var w = _world;
            var minutes = w != null ? w.Clock.Tick - _start : 0;
            var full = SliceSleep.Full(minutes);
            if (w != null && w.Mind != null)
            {
                w.Mind.Asleep = false;
                w.Mind.Slept((int)System.Math.Min(int.MaxValue, minutes));
            }

            if (w != null && full && w.State != null)
            {
                w.State.Set(SliceConfront.WillPowerLostKey, 0); // 07.2 §3.3: a full sleep restores Will Power
                if (!w.State.IsSet(SliceConfront.BleedingKey) && !w.State.IsSet(SliceConfront.PoisonedKey) && w.State.Get(SliceConfront.HpLostKey) < 1000)
                {
                    w.State.Set(SliceConfront.HpLostKey, 0); // 18 §3: HP, when no condition blocks it
                }
            }

            var inFight = w != null && w.Fights != null && w.Fights.InFight;
            if (!inFight)
            {
                StandUp();
            }

            if (_player != null)
            {
                _player.enabled = !inFight;
            }

            _hud = _hud != null ? _hud : FindAnyObjectByType<HudView>();
            if (!inFight && _hud != null)
            {
                var line = Loc.Format(cut ? "ui.sleep.woke_cut" : "ui.sleep.woke", Hours(minutes), Clock(w));
                if (_stolen > 0)
                {
                    line += "\n" + Loc.Format("ui.sleep.robbed", Coins.Show(_stolen));
                }

                _hud.ShowMessage(line);
            }

            Debug.Log($"[Slice] awake after {minutes} minutes{(cut ? ", cut short" : string.Empty)}{(full ? ", a full sleep" : string.Empty)}.");
            Woke?.Invoke(minutes, full);
            if (full && !inFight && w != null && w.Save != null && (!Application.isBatchMode || GameDev.Slice.Save.SliceSaveSlot.IsCustomPath))
            {
                w.Save.Write(); // 17 X.3: the game autosaves after each full sleep
            }
        }

        private static string Clock(SliceWorldDriver w)
        {
            var minute = w != null ? (int)(w.Clock.Tick % GameDev.World.Timekeeping.GameClock.MinutesPerDay) : 0;
            return $"{minute / 60:00}:{minute % 60:00}";
        }

        private void LieDown()
        {
            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            if (_player == null)
            {
                return;
            }

            _player.enabled = false;
            var animator = _player.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                return;
            }

            if (HasParameter(animator, Down))
            {
                animator.SetBool(Down, true);
            }
            else if (animator.HasState(0, Crouch))
            {
                animator.CrossFadeInFixedTime(Crouch, 0.4f, 0);
            }
        }

        private void StandUp()
        {
            var animator = _player != null ? _player.GetComponentInChildren<Animator>() : null;
            if (animator != null && HasParameter(animator, Down))
            {
                animator.SetBool(Down, false);
            }
        }

        private static bool HasParameter(Animator animator, int hash)
        {
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == hash && p.type == AnimatorControllerParameterType.Bool)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDisable()
        {
            ClosePage();
            if (Sleeping)
            {
                Wake(true);
            }
        }
    }
}
