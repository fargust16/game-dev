using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Dialogue;
using GameDev.Input;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Fights;
using GameDev.UI;
using GameDev.World.Seed;
using UnityEngine;

namespace GameDev.Slice.People
{
    /// <summary>
    /// 10.4 §6 *Accusation* (WA5): with the character's Reputation at the Accusation's mark, a Warden on post who sees it
    /// within 8 m stops it and accuses it of its record, in the dialogue column. The world waits, as in any dialogue, and
    /// its end costs a tick. The character pays the fine, goes to the lock-up, offers a bribe, talks the Warden round with
    /// an Argument, fights, or runs. Paid, by the fine, the lock-up or a bribe taken, its Reputation rises to where no
    /// Warden accuses it; talked round, the Wardens let it off until its Reputation falls lower. The numbers are authoring
    /// defaults.
    /// While the character is Wanted for a noticed break-in or theft (S5c.5; 10.3 §6.2), the Warden who sees it opens an
    /// **Arrest** instead (10.4 §6): surrender, a bribe, Deception and Persuasion with their Arguments, a fight, or flight,
    /// and no fine to pay. Settled, the Wardens look for the character no more.
    /// </summary>
    public sealed class WardenAccusation : MonoBehaviour
    {
        /// <summary>What the character can answer, in the order the column numbers them.</summary>
        public enum Choice
        {
            Pay,
            LockUp,
            Bribe,
            Persuade,
            Fight,
            Run,

            /// <summary>The Arrest's Deception (10.4 §6): "Easy to mistake a door", with nothing stolen on the character.</summary>
            Deceive,
        }

        /// <summary>10.4 §6 *Arrest*, Persuasion's Argument: a Reputation of +5 or more (authoring default's reading in points).</summary>
        public const int GoodNameReputation = 5;

        /// <summary>Authoring defaults: how near a Warden sees the character to stop it, m; the bribe, half the fine (10.4 §6 *Arrest*); one Warden in three takes it.</summary>
        public const float Reach = 8f;

        public const int Bribe = SliceConfront.WardensFine / 2;

        public const int CorruptOneIn = 3;

        /// <summary>Authoring default: ticks after a run before a Warden stops the character again.</summary>
        public const int QuietTicks = 10;

        /// <summary>The Wardens' Listener Values, as POI A's and B's Wardens (authoring defaults).</summary>
        public const int ListenerValues = 10;

        /// <summary>Under whose id the Wardens' Attitude is kept, as in POI A's conversation.</summary>
        public const string Listener = "wardens";

        private const float EyeHeight = 1.6f;
        private const float LookSeconds = 0.25f;
        private static readonly int TalkingHash = Animator.StringToHash("Talking");
        private static readonly int DownHash = Animator.StringToHash("Down");

        // Each line gives its speaker and words afresh, so the column follows a change of language.
        private readonly List<Func<(string speaker, string text)>> _said = new List<Func<(string, string)>>();
        private readonly List<Choice> _choices = new List<Choice>();
        private SliceWorldDriver _world;
        private PlayerLocomotion _player;
        private HudView _hud;
        private Transform _warden;
        private Quaternion _wardenFacing;
        private IDisposable _pause;
        private bool _open;
        private bool _refused;
        private bool _failed;
        private bool _arrest;
        private bool _seen;
        private bool _lied;
        private bool _giveBack;
        private long _quietUntil = long.MinValue;
        private float _look;

        /// <summary>Whether a Warden's Accusation is open now.</summary>
        public bool IsOpen => _open;

        /// <summary>Whether the open column is an Arrest for a noticed crime (S5c.5), not an Accusation.</summary>
        public bool IsArrest => _open && _arrest;

        /// <summary>Whether a Warden is on its way to stop the character or stands talking to it.</summary>
        public bool Busy => _open || _warden != null;

        /// <summary>The tick before which no Warden stops the character again; kept by a Suspend Save (17 X.3).</summary>
        public long QuietUntil
        {
            get => _quietUntil;
            set => _quietUntil = value;
        }

        /// <summary>The Warden who accuses, while it is open.</summary>
        public Transform Warden => _open ? _warden : null;

        /// <summary>The fine asked now, in bronze: the lock-up's, and what an honest Warden refused on top.</summary>
        public int Fine { get; private set; }

        /// <summary>The answers offered now.</summary>
        public IReadOnlyList<Choice> Choices => _choices;

        /// <summary>Raised as an answer is chosen.</summary>
        public event Action<Choice> Chosen;

        public void Configure(SliceWorldDriver world) => _world = world;

        /// <summary>Whether <paramref name="warden"/> takes a bribe: one in three, by the seed and its place in the scene.</summary>
        public bool Corrupt(Transform warden) =>
            warden != null && _world.Seed.Fork(SliceSeedStreams.Wardens, StableHash.Of(Path(warden))).Next(CorruptOneIn) == 0;

        /// <summary>Opens the Accusation of <paramref name="warden"/>; false when none can open now.</summary>
        public bool Open(Transform warden)
        {
            if (_open || warden == null || _world == null || Player() == null || Hud() == null)
            {
                return false;
            }

            _open = true;
            _warden = warden;
            _wardenFacing = warden.rotation;
            _arrest = Wanted;
            _seen = _arrest && _world.BreakIns.Seen;
            _world.Fights?.CallOff(warden); // stopped on its way to the place, it is there
            var way = Vector3.ProjectOnPlane(_player.transform.position - warden.position, Vector3.up);
            if (way.sqrMagnitude > 1e-4f)
            {
                warden.rotation = Quaternion.LookRotation(way.normalized, Vector3.up);
            }

            Talking(true);
            _pause = _world.Clock.Pause();
            _player.enabled = false;
            Fine = SliceConfront.WardensFine;
            _refused = false;
            _failed = false;
            _lied = false;
            _said.Clear();
            if (_arrest)
            {
                var line = _seen ? "ui.arrest.line.seen" : "ui.arrest.line.heard";
                _said.Add(() => (Loc.Get("ui.fight.warden"), Loc.Get(line)));
            }
            else
            {
                _said.Add(() => (Loc.Get("ui.fight.warden"), Loc.Format("ui.accuse.line", Coins.Show(SliceConfront.WardensFine))));
            }

            Show();
            return true;
        }

        /// <summary>Answers with <paramref name="choice"/>, if it is offered now.</summary>
        public void Choose(Choice choice)
        {
            if (!_open || !_choices.Contains(choice))
            {
                return;
            }

            var words = Words(choice, false);
            _said.Add(() => (Loc.Get("ui.dialogue.you"), words));
            Chosen?.Invoke(choice);
            switch (choice)
            {
                case Choice.Pay:
                    _world.Bag.Spend(Fine);
                    _world.Standing.Settle();
                    End("ui.accuse.paid");
                    break;
                case Choice.LockUp:
                    var extra = Fine - SliceConfront.WardensFine;
                    if (_arrest)
                    {
                        // 10.4 §6: a surrender is the lock-up, and the stolen goods carried go back to their owners.
                        _world.BreakIns.GiveBack();
                        _world.BreakIns.ClearWanted();
                    }
                    else
                    {
                        _world.Standing.Settle();
                    }

                    Close();
                    _world.Fights.GoToLockUp(extra); // its terms cost the dialogue's tick
                    break;
                case Choice.Bribe when Corrupt(_warden):
                    _world.Bag.Spend(Bribe);
                    if (_arrest)
                    {
                        _world.BreakIns.ClearWanted();
                    }
                    else
                    {
                        _world.Standing.Settle();
                    }

                    End("ui.accuse.bribe.taken");
                    break;
                case Choice.Bribe:
                    _refused = true;
                    Fine += Bribe;
                    var fine = Fine;
                    _said.Add(() => (Loc.Get("ui.fight.warden"), Loc.Format("ui.accuse.bribe.refused", Coins.Show(fine))));
                    Show();
                    break;
                case Choice.Persuade:
                    Persuade();
                    break;
                case Choice.Deceive:
                    Deceive();
                    break;
                case Choice.Fight:
                    var warden = _warden;
                    End(null);
                    _world.Fights.StartAssault(warden);
                    break;
                default:
                    _quietUntil = _world.Clock.Tick + 1 + QuietTicks;
                    End(null);
                    break;
            }
        }

        /// <summary>
        /// A Social Check's chance and what stands behind it (10.4 §3), as the column shows it: Persuasion, or the Arrest's
        /// Deception. The request is Ordinary when the Warden only heard of the crime, Large when it saw it (10.4 §6).
        /// </summary>
        public CheckPreview Preview(CharacterSkill skill = CharacterSkill.Persuasion)
        {
            var rules = _world.CheckRules;
            var listener = new CharacterSheet();
            listener.Set(CharacterAttribute.Strength, ListenerValues);
            listener.Set(CharacterAttribute.Intelligence, ListenerValues);
            listener.Set(CharacterAttribute.Will, ListenerValues);
            var characterValue = SocialCheck.CharacterValue(rules, _world.Character, skill);
            var listenerValue = SocialCheck.ListenerValue(rules, listener, skill);
            var difficulty = SocialCheck.Difficulty(rules, _arrest && _seen ? RequestSize.Large : RequestSize.Ordinary, listenerValue, false);
            var attitude = _world.Standing.Attitude(Listener);
            var standing = _world.Standing.Reading(skill, false);
            var mind = _world.Mind != null ? _world.Mind.SocialPenalty : 0; // 07.2 §3.3–3.4
            var bonus = _world.Fights != null ? _world.Fights.SuggestionFor(skill) : 0; // 05.1 Внушение (S5b.8)
            var modifiers = SocialCheck.Modifiers(rules, attitude + standing.Modifier + mind + bonus);
            var chance = SocialCheck.Chance(rules, characterValue, modifiers, difficulty, _world.Character.Get(CharacterAttribute.Luck));
            return new CheckPreview(chance, characterValue, listenerValue, difficulty, attitude, standing, 0, modifiers);
        }

        // 10.4 §1, §6: the Wardens' debt is the Argument; a success lets the character off, a failure is not tried again here.
        private void Persuade()
        {
            var rolls = _world.Seed.Fork(SliceSeedStreams.SocialChecks, StableHash.Of("accusation." + Path(_warden)), (int)_world.Clock.Tick);
            var success = SocialCheck.Roll(Preview().Chance, rolls);
            var listener = new CharacterSheet();
            listener.Set(CharacterAttribute.Intelligence, ListenerValues);
            listener.Set(CharacterAttribute.Will, ListenerValues);
            SocialCheck.Train(_world.Character, CharacterSkill.Persuasion, listener, success);
            _world.Fights?.Persuaded(CharacterSkill.Persuasion, Listener, success);
            if (success)
            {
                _world.Conduct?.Deed(GameDev.Slice.Alchemy.ConductDeed.Convince, "accusation." + Path(_warden)); // 05.1
                if (_arrest)
                {
                    if (_giveBack)
                    {
                        _world.BreakIns.GiveBack(); // the Argument was giving back what was stolen
                    }

                    _world.BreakIns.ClearWanted();
                    End(_giveBack ? "ui.arrest.returned" : "ui.arrest.letoff");
                    return;
                }

                _world.Standing.LetOff();
                End("ui.accuse.letoff");
                return;
            }

            _failed = true;
            _said.Add(() => (null, Loc.Format("ui.dialogue.failure", PersonTalk.SkillName(CharacterSkill.Persuasion))));
            _said.Add(() => (Loc.Get("ui.fight.warden"), Loc.Get("ui.accuse.persuade.failed")));
            Show();
        }

        // 10.4 §6 *Arrest*: the evidence is the Argument, nothing stolen on the character and the crime itself unseen; a
        // lie is Жрица's Convince as any (05.1, VS-CONDUCT (b)), and a failure is not tried again here.
        private void Deceive()
        {
            const CharacterSkill skill = CharacterSkill.Deception;
            var rolls = _world.Seed.Fork(SliceSeedStreams.SocialChecks, StableHash.Of("arrest.lie." + Path(_warden)), (int)_world.Clock.Tick);
            var success = SocialCheck.Roll(Preview(skill).Chance, rolls);
            var listener = new CharacterSheet();
            listener.Set(CharacterAttribute.Intelligence, ListenerValues);
            listener.Set(CharacterAttribute.Will, ListenerValues);
            SocialCheck.Train(_world.Character, skill, listener, success);
            _world.Fights?.Persuaded(skill, Listener, success);
            if (success)
            {
                _world.Conduct?.Deed(GameDev.Slice.Alchemy.ConductDeed.Convince, "accusation." + Path(_warden));
                _world.BreakIns.ClearWanted();
                End("ui.arrest.deceived");
                return;
            }

            _lied = true;
            _said.Add(() => (null, Loc.Format("ui.dialogue.failure", PersonTalk.SkillName(skill))));
            _said.Add(() => (Loc.Get("ui.fight.warden"), Loc.Get("ui.arrest.deceive.failed")));
            Show();
        }

        /// <summary>Whether the Wardens look for the character for a noticed crime (S5c.5).</summary>
        private bool Wanted => _world != null && _world.BreakIns != null && _world.BreakIns.Wanted;

        private void Show()
        {
            _choices.Clear();
            var bronze = _world.Bag != null ? _world.Bag.Bronze : 0;
            var debt = _world.State.IsSet(SliceConfront.DebtKey(SliceConfront.Wardens));
            if (bronze >= Fine && !_arrest)
            {
                _choices.Add(Choice.Pay); // an Arrest has no fine to pay on the spot (10.4 §6)
            }

            _choices.Add(Choice.LockUp);
            if (!_refused && bronze >= Bribe)
            {
                _choices.Add(Choice.Bribe);
            }

            if (_arrest && !_lied && !_seen && _world.BreakIns.NothingStolenCarried)
            {
                _choices.Add(Choice.Deceive);
            }

            // Persuasion's Arguments: the Wardens' debt; in an Arrest also a good name, or giving back what was stolen.
            var goodName = _arrest && _world.Standing.ReputationPoints >= GoodNameReputation;
            _giveBack = _arrest && !debt && !goodName && _world.BreakIns.CanGiveBack;
            if (!_failed && (debt || goodName || _giveBack))
            {
                _choices.Add(Choice.Persuade);
            }

            _choices.Add(Choice.Fight);
            _choices.Add(Choice.Run);
            var options = _choices.Select((c, i) => (i + 1).ToString(CultureInfo.InvariantCulture) + ". " + Words(c, true)).ToList();
            Hud()?.ShowDialogue(_said.Select(s => s()).ToList(), options);
        }

        private string Words(Choice choice, bool tagged)
        {
            switch (choice)
            {
                case Choice.Pay:
                    return Loc.Format("ui.accuse.pay", Coins.Show(Fine));
                case Choice.LockUp:
                    return Loc.Get(_arrest ? "ui.arrest.surrender" : "ui.accuse.lockup");
                case Choice.Deceive:
                    var lie = Loc.Get("ui.arrest.deceive");
                    return tagged
                        ? PersonTalk.CheckTag(_world.CheckRules, CharacterSkill.Deception, Preview(CharacterSkill.Deception), DebugSight.Read(_world.Character.Get(CharacterAttribute.Intelligence))) + " " + lie
                        : lie;
                case Choice.Bribe:
                    return Loc.Format("ui.accuse.bribe", Coins.Show(Bribe));
                case Choice.Persuade:
                    var words = Loc.Get(_giveBack ? "ui.arrest.giveback" : "ui.accuse.persuade");
                    if (!tagged)
                    {
                        return words;
                    }

                    var intelligence = DebugSight.Read(_world.Character.Get(CharacterAttribute.Intelligence));
                    return PersonTalk.CheckTag(_world.CheckRules, CharacterSkill.Persuasion, Preview(), intelligence) + " " + words;
                case Choice.Fight:
                    return Loc.Get("ui.accuse.fight");
                default:
                    return Loc.Get("ui.accuse.run");
            }
        }

        // The column closes, the world runs on by the dialogue's tick, and the Warden's last words show on the screen.
        private void End(string lineKey)
        {
            Close();
            _world.Clock.AdvanceTicks(1);
            if (lineKey != null)
            {
                Hud()?.ShowMessage(Loc.Get(lineKey));
            }
        }

        private void Close()
        {
            Talking(false);
            if (_warden != null)
            {
                _warden.rotation = _wardenFacing;
            }

            _open = false;
            _warden = null;
            Hud()?.CloseDialogue();
            _pause?.Dispose();
            _pause = null;
            if (_player != null)
            {
                _player.enabled = true;
            }
        }

        private void Update()
        {
            if (_world == null || Player() == null)
            {
                return;
            }

            var input = GameInputService.Instance;
            if (_open)
            {
                if (_warden == null || !_warden.gameObject.activeInHierarchy)
                {
                    End(null);
                    return;
                }

                for (var number = 1; input != null && number <= _choices.Count; number++)
                {
                    if (input.OptionPressed(number))
                    {
                        Choose(_choices[number - 1]);
                        return;
                    }
                }

                return;
            }

            _look -= Time.unscaledDeltaTime;
            if (_look > 0f)
            {
                return;
            }

            _look = LookSeconds;
            if (CanAccuse())
            {
                var warden = Watching();
                if (warden != null)
                {
                    Open(warden);
                }
            }
        }

        // Low Reputation, and the character free: no fight, terms, page, work or water, and the world running.
        private bool CanAccuse()
        {
            var standing = _world.Standing;
            var fights = _world.Fights;
            var clock = _world.Clock;
            var hud = Hud();
            var wanted = Wanted; // S5c.5: a Warden on its way to the place still stops the character it sees
            return standing != null && (standing.Accused || wanted) && fights != null && clock != null && hud != null && !clock.IsPaused
                   && clock.Tick >= _quietUntil && !fights.InFight && fights.Terms == null && !fights.Assaulting && (fights.WardensComing == 0 || wanted)
                   && _player.enabled && !_player.InWater && (_world.Work == null || !_world.Work.Busy)
                   && !hud.IsDialogueOpen && !hud.IsReadingOpen && !hud.IsBagOpen && !hud.IsShopOpen && !hud.IsMapOpen && !hud.IsNotesOpen;
        }

        // The nearest Warden on post within reach who sees the character: standing, on no errand and in no fight.
        private Transform Watching()
        {
            var feet = _player.transform.position;
            foreach (var warden in PeopleAround.Wardens(feet))
            {
                if ((warden.position - feet).sqrMagnitude > Reach * Reach)
                {
                    break;
                }

                if (!Lying(warden) && warden.GetComponent<WardenErrand>() == null && warden.GetComponent<SliceFighter>() == null && Sees(warden, feet))
                {
                    return warden;
                }
            }

            return null;
        }

        // A clear line from the Warden's eyes to the character's head; people stop no sight.
        private bool Sees(Transform warden, Vector3 feet)
        {
            var eye = warden.position + (Vector3.up * EyeHeight);
            var head = feet + (Vector3.up * EyeHeight);
            var line = head - eye;
            foreach (var hit in Physics.RaycastAll(eye, line.normalized, line.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!PeopleAround.IsPerson(hit.collider))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Lying(Transform body)
        {
            var animator = body.GetComponentInChildren<Animator>();
            return animator != null && animator.runtimeAnimatorController != null && animator.parameters.Any(p => p.nameHash == DownHash) && animator.GetBool(DownHash);
        }

        private void Talking(bool on)
        {
            var animator = _warden != null ? _warden.GetComponentInChildren<Animator>() : null;
            if (animator != null && animator.runtimeAnimatorController != null && animator.parameters.Any(p => p.nameHash == TalkingHash))
            {
                animator.SetBool(TalkingHash, on);
            }
        }

        // A Warden's place in the scene, the same in every run of a seed.
        private static string Path(Transform t)
        {
            var parts = new List<string>();
            for (; t != null; t = t.parent)
            {
                parts.Add(t.name + "#" + t.GetSiblingIndex().ToString(CultureInfo.InvariantCulture));
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private PlayerLocomotion Player()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerLocomotion>();
            }

            return _player;
        }

        // A city kept behind an interior keeps its HUD, switched off: look again for the one shown.
        private HudView Hud()
        {
            if (_hud == null || !_hud.isActiveAndEnabled)
            {
                _hud = FindAnyObjectByType<HudView>();
            }

            return _hud;
        }

        private void OnDisable()
        {
            if (_open)
            {
                Close();
            }
        }
    }
}
