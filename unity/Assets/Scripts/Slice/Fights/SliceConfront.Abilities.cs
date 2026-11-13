using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Player.Progression;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// CR4, the character's abilities (05.1 *Abilities of the slice*, 07.2 §3.3, §4.5, 10.2 §5f). Its Arcana, its Limit,
    /// the Will Power it has spent and its Prepared set are kept in the world state. A fighter made of the character
    /// enters its Arcana at its Limit with its passives and its Prepared abilities, and starts with the Will Power left;
    /// a fight's end writes it back, and outside a fight it comes back by about 1 % of its maximum each 10 ticks. In the
    /// player's turn keys 1 to 9 or the hotbar's slots use a Prepared ability; one aimed at an enemy waits for the click
    /// on it, and any other click puts it down. U turns Infusion on and off. Each Limit gives its whole Ability
    /// Pool (owner, COMBAT-1), so the character knows every ability of its Arcana at its Limit and the Limits above. It
    /// enters an Arcana by a Limit potion (`SliceAdvancement`, S5b.5), and the development command F9 steps it through no
    /// Arcana, Жрица 9 and 8, and Император 9 and 8. Sleep's full return
    /// of Will Power waits for the slice's sleep.
    /// </summary>
    public sealed partial class SliceConfront
    {
        // World state keys of this host (17 C.6). Will Power is kept as the thousandths of its maximum spent, so a new
        // game, with no key written, starts it full; the Prepared set as one bit for each of the settings' abilities.
        public const string ArcanaKey = "player.arcana";
        public const string LimitKey = "player.limit";
        public const string WillPowerLostKey = "player.wp.lost";
        public const string PreparedKey = "player.prepared";

        // 07.2 §3.3: about 1 % of maximum Will Power each 10 world ticks, a thousandth a tick (authoring default).
        private const int WillPowerPerTick = 1;

        // The development command's steps (CR4 authoring default, F9).
        private static readonly (Arcana Arcana, int Limit)[] DevSteps =
        {
            (Arcana.None, 9), (Arcana.Priestess, 9), (Arcana.Priestess, 8), (Arcana.Emperor, 9), (Arcana.Emperor, 8),
        };

        private readonly List<FightAbility> _shownAbilities = new List<FightAbility>();
        private readonly List<int> _shownIndices = new List<int>();
        private int _aiming = -1;
        private readonly List<CombatCell> _area = new List<CombatCell>();
        private bool _infuse;

        /// <summary>The Arcana the character entered; <see cref="Arcana.None"/> while it is Uninitiated.</summary>
        public Arcana PlayerArcana => _state != null ? (Arcana)Mathf.Clamp(_state.Get(ArcanaKey), 0, (int)Arcana.Emperor) : Arcana.None;

        /// <summary>The character's Limit, 9 or 8 in V1 (05.1); null while it is Uninitiated.</summary>
        public int? PlayerLimit => PlayerArcana == Arcana.None ? (int?)null : Mathf.Clamp(_state.Get(LimitKey), 8, 9);

        /// <summary>Whether Infusion is on, which the player's turn keeps from fight to fight.</summary>
        public bool Infusing => _infuse;

        /// <summary>The Prepared ability being aimed, by its place in the fighter's set; -1 for none.</summary>
        public int Aiming => _aiming;

        /// <summary>Enters the character's Arcana at its Limit, as the development command does; a Limit potion does it by `SliceAdvancement` (S5b.5).</summary>
        public void Initiate(Arcana arcana, int limit)
        {
            if (_state == null)
            {
                return;
            }

            _state.Set(ArcanaKey, (int)arcana);
            _state.Set(LimitKey, arcana == Arcana.None ? 0 : limit);
            _state.Set(PreparedKey, 0); // a new Arcana prepares its first abilities again
        }

        /// <summary>
        /// The abilities the character knows: every one of its Arcana at its Limit and the Limits above, Limit 7 being the
        /// NPCs' only, each Limit giving its whole pool (05.1, owner COMBAT-1).
        /// </summary>
        public List<FightAbility> KnownAbilities()
        {
            var known = new List<FightAbility>();
            var arcana = PlayerArcana;
            if (arcana == Arcana.None || _settings == null || _settings.Abilities == null)
            {
                return known;
            }

            var limit = PlayerLimit ?? 9;
            foreach (var a in _settings.Abilities)
            {
                if (a != null && a.Arcana == arcana && a.Limit >= limit)
                {
                    known.Add(a);
                }
            }

            return known;
        }

        /// <summary>
        /// The character's Prepared Abilities, at most <paramref name="slots"/>: the set it keeps, or while none is kept the
        /// first it knows. Only an ability a fight can use is prepared: out of a fight every known active ability is open
        /// and the slots bind in a fight alone (07.2 §4.5).
        /// </summary>
        public List<FightAbility> PreparedAbilities(int slots)
        {
            var prepared = new List<FightAbility>();
            var mask = _state != null ? _state.Get(PreparedKey) : 0;
            foreach (var a in KnownAbilities())
            {
                if (a.InFight && (mask == 0 || (mask & Bit(a)) != 0) && prepared.Count < slots)
                {
                    prepared.Add(a);
                }
            }

            return prepared;
        }

        /// <summary>07.2 §4.5: the character's Prepared slots by its Intelligence and Will.</summary>
        public int PreparedSlots() => _settings != null && _character != null ? _settings.FightRules.PreparedSlots(_character) : 0;

        /// <summary>Whether <paramref name="ability"/> is in the Prepared set.</summary>
        public bool IsPrepared(FightAbility ability) => ability != null && PreparedAbilities(PreparedSlots()).Contains(ability);

        /// <summary>
        /// 07.2 §4.5: prepares <paramref name="ability"/> or puts it away, out of a fight, for one tick; the key of the
        /// reason it cannot, or null.
        /// </summary>
        public string TogglePrepared(FightAbility ability)
        {
            if (ability == null || !ability.InFight || !KnownAbilities().Contains(ability) || _state == null)
            {
                return "ui.prepared.none";
            }

            if (InFight)
            {
                return "ui.bag.fault.fight";
            }

            var slots = PreparedSlots();
            var prepared = PreparedAbilities(slots);
            if (prepared.Contains(ability))
            {
                prepared.Remove(ability);
            }
            else if (prepared.Count >= slots)
            {
                return "ui.prepared.full";
            }
            else
            {
                prepared.Add(ability);
            }

            var mask = 0;
            foreach (var a in prepared)
            {
                mask |= Bit(a);
            }

            // An empty set is kept as one bit nobody holds, so it does not read as "none kept yet".
            _state.Set(PreparedKey, mask == 0 ? 1 << 30 : mask);
            _clock?.AdvanceTicks(1);
            return null;
        }

        private int Bit(FightAbility a)
        {
            var i = _settings != null && _settings.Abilities != null ? Array.IndexOf(_settings.Abilities, a) : -1;
            return i >= 0 && i < 30 ? 1 << i : 0;
        }

        /// <summary>The fighter made of the character enters its Arcana at its Limit, with the Will Power it has left.</summary>
        private void Initiate(Fighter fighter)
        {
            var arcana = PlayerArcana;
            if (arcana == Arcana.None || _settings == null)
            {
                return;
            }

            var slots = _settings.FightRules.PreparedSlots(fighter.Values);
            var known = new List<FightAbility>();
            foreach (var a in KnownAbilities())
            {
                if (a.Passive)
                {
                    known.Add(a);
                }
            }

            known.AddRange(PreparedAbilities(slots));
            fighter.Prepare(arcana, known, slots);
            if (_state != null)
            {
                fighter.WillPower = fighter.MaxWillPower * (1d - (Mathf.Clamp(_state.Get(WillPowerLostKey), 0, 1000) / 1000d));
            }
        }

        /// <summary>Keeps the Will Power the fighter has left.</summary>
        private void SaveWillPower(Fighter fighter)
        {
            if (_state == null || fighter.Arcana == Arcana.None || fighter.MaxWillPower <= 0d)
            {
                return;
            }

            var lost = (int)Math.Round((1d - (fighter.WillPower / fighter.MaxWillPower)) * 1000d);
            _state.Set(WillPowerLostKey, Mathf.Clamp(lost, 0, 1000));
        }

        /// <summary>07.2 §3.3: one tick outside a fight gives back a thousandth of maximum Will Power.</summary>
        private void RestoreWillPower()
        {
            var lost = _state.Get(WillPowerLostKey);
            if (lost > 0)
            {
                _state.Set(WillPowerLostKey, Math.Max(0, lost - WillPowerPerTick));
            }
        }

        /// <summary>The development command (F9): the next of no Arcana, Жрица 9 and 8, Император 9 and 8, said on the screen.</summary>
        private void StepArcana()
        {
            var at = 0;
            for (var i = 0; i < DevSteps.Length; i++)
            {
                if (DevSteps[i].Arcana == PlayerArcana && (DevSteps[i].Arcana == Arcana.None || DevSteps[i].Limit == PlayerLimit))
                {
                    at = i;
                }
            }

            var next = DevSteps[(at + 1) % DevSteps.Length];
            Initiate(next.Arcana, next.Limit);
            Note(next.Arcana == Arcana.None
                ? Loc.Get("ui.dev.arcana.none")
                : Loc.Format("ui.dev.arcana", Loc.Get(ArcanaName(next.Arcana)), next.Limit));
        }

        /// <summary>The string table key of an Arcana's name.</summary>
        public static string ArcanaName(Arcana arcana) => arcana == Arcana.Emperor ? "arcana.emperor" : "arcana.priestess";

        /// <summary>The Prepared abilities a fight can use, as the hotbar shows them, and their places in the fighter's set.</summary>
        private void ShowAbilities(Combatant player)
        {
            _shownAbilities.Clear();
            _shownIndices.Clear();
            if (player == null)
            {
                return;
            }

            var prepared = player.Fighter.Prepared;
            for (var i = 0; i < prepared.Count; i++)
            {
                if (prepared[i].InFight)
                {
                    _shownAbilities.Add(prepared[i]);
                    _shownIndices.Add(i);
                }
            }
        }

        /// <summary>
        /// The player's abilities in its turn: U turns Infusion on and off; a key 1 to 9 or a slot uses an ability with no
        /// target at once and takes up one aimed at an enemy, or puts it down when pressed again. True when it acted.
        /// </summary>
        private bool PlayerAbilities(Combatant player, GameInputService input, FightAction picked)
        {
            ShowAbilities(player);
            if (input.InfusePressed || picked == FightAction.Infuse)
            {
                if (CanInfuse(player))
                {
                    _infuse = !_infuse;
                }

                _hud.MarkDirty();
            }

            player.Infusing = _infuse;
            var slot = _hud.TakeAbility();
            for (var n = 1; slot < 0 && n <= Math.Min(9, _shownIndices.Count); n++)
            {
                if (input.OptionPressed(n))
                {
                    slot = n - 1;
                }
            }

            if (slot < 0 || slot >= _shownIndices.Count)
            {
                return false;
            }

            var index = _shownIndices[slot];
            if (!_turns.CanUse(player, index))
            {
                return false;
            }

            if (TurnEconomy.NeedsTarget(player.Fighter.Prepared[index]) || TurnEconomy.NeedsPoint(player.Fighter.Prepared[index]))
            {
                _aiming = _aiming == index ? -1 : index;
                _hud.MarkDirty();
                return false;
            }

            _aiming = -1;
            return _turns.SubmitIntent(TurnIntent.UseAbility(index));
        }

        /// <summary>
        /// While an ability is aimed: the strike's cursor over an enemy it reaches, the blocked one elsewhere, and its tip;
        /// a click on that enemy uses it, any other click puts it down. True when the click was taken.
        /// </summary>
        private bool AimAbility(Combatant player, GameInputService input)
        {
            _hud.AimText = null;
            if (_aiming < 0)
            {
                return false;
            }

            if (_aiming >= player.Fighter.Prepared.Count || !_turns.CanUse(player, _aiming))
            {
                _aiming = -1;
                return false;
            }

            var ability = player.Fighter.Prepared[_aiming];
            if (TurnEconomy.NeedsPoint(ability))
            {
                return AimPoint(player, ability, input);
            }

            var target = _cursor.Target;
            var reaches = target != null && _turns.CanAim(player, _aiming, target);
            _cursor.Aim(reaches);
            var name = Loc.Get($"ability.{ability.Key}.name");
            _hud.AimText = target == null ? Loc.Format("ui.fight.ability.target", name)
                : !reaches ? Loc.Format("ui.fight.ability.far", name)
                : ability.Contest > 0f ? Loc.Format("ui.fight.ability.chance", name, (int)Math.Round(_settings.FightRules.WillContest(player.Fighter, target.Fighter, ability.Contest) * 100d))
                : name;
            if (!input.ClickMovePressed)
            {
                return false;
            }

            var index = _aiming;
            _aiming = -1;
            _hud.AimText = null;
            if (reaches)
            {
                _turns.SubmitIntent(TurnIntent.UseAbility(index, target.Id));
            }

            _cursor.Clear();
            _hud.MarkDirty();
            return true;
        }

        /// <summary>
        /// CR5, while Пелена or Гнёт is aimed: its area framed round the cell under the cursor, gold where it can be cast and
        /// red where not, and its tip, for a Гнёт how many enemies and of the player's own people it would strike; a click
        /// there casts it, any other click puts it down. True when the click was taken.
        /// </summary>
        private bool AimPoint(Combatant player, FightAbility ability, GameInputService input)
        {
            var point = _cursor.Under;
            var fits = point.HasValue && _turns.CanAimAt(player, _aiming, point.Value);
            if (point.HasValue)
            {
                _turns.AreaOf(point.Value, ability, _area);
                _cursor.AimArea(_area, fits);
            }
            else
            {
                _cursor.Aim(false);
            }

            var name = Loc.Get($"ability.{ability.Key}.name");
            if (!point.HasValue)
            {
                _hud.AimText = Loc.Format("ui.fight.ability.point", name);
            }
            else if (!fits)
            {
                _hud.AimText = Loc.Format("ui.fight.ability.far", name);
            }
            else if (ability.Effect == AbilityEffect.Press)
            {
                var (foes, friends) = _turns.PressCount(player, ability, point.Value);
                _hud.AimText = Loc.Format("ui.fight.ability.press", name, foes, friends);
            }
            else
            {
                _hud.AimText = name;
            }

            if (!input.ClickMovePressed)
            {
                return false;
            }

            var index = _aiming;
            _aiming = -1;
            _hud.AimText = null;
            if (fits)
            {
                _turns.SubmitIntent(TurnIntent.UseAbilityAt(index, point.Value));
            }

            _cursor.Clear();
            _hud.MarkDirty();
            return true;
        }

        /// <summary>10.2 §5f: Infusion needs an Arcana, a weapon, and armour that lets some of it through.</summary>
        private bool CanInfuse(Combatant player) =>
            player != null && player.Fighter.Arcana != Arcana.None && player.Fighter.Weapon != null && _settings.FightRules.InfusionShare(player.Fighter) > 0d;

        /// <summary>For each ability slot by its place: usable now, its AP, its Will Power, and whether it is being aimed.</summary>
        private (bool Usable, int Ap, int WillPower, bool On) AbilityState(int slot)
        {
            var player = _playerCombatant;
            if (player == null || slot < 0 || slot >= _shownIndices.Count || _settings == null)
            {
                return (false, 0, 0, false);
            }

            var index = _shownIndices[slot];
            var ability = player.Fighter.Prepared[index];
            var ours = _turns != null && _turns.IsRunning && _turns.Acting == player && !_beats.Busy && _terms == null;
            var wp = (int)Math.Ceiling(_settings.FightRules.AbilityCost(player.Fighter, ability) - 1e-9);
            return (ours && _turns.CanUse(player, index), _turns != null ? _turns.AbilityAp(player, ability) : ability.Ap, wp, _aiming == index);
        }

        /// <summary>
        /// An ability was used: said in the log; a Укол разума is shown as its caster's blow, so its target is hit or falls
        /// at the impact and the log says it then, and a death by it counts as a blow's does.
        /// </summary>
        private void OnAbilityUsed(Combatant user, Combatant target, FightAbility ability, bool worked, StrikeResult result)
        {
            if (ability != null && ability.Effect == AbilityEffect.Crush)
            {
                return; // Сокрушение is a blow, shown and said as one
            }

            if (target != null && result.Hit)
            {
                CountKill(user, target);
                FightDeath(user, target);
            }

            if (ability != null && ability.Effect == AbilityEffect.Press)
            {
                PressHit(user, target, ability, result); // all it struck, shown and said at once
            }
            else if (ability != null && ability.Effect == AbilityEffect.Sting && _bodies.TryGetValue(user, out var body) && body != null)
            {
                _bodies.TryGetValue(target, out var targetBody);
                _beats.Strike(body, targetBody, Motion(user.Fighter, false), result, user, target, ability);
            }
            else
            {
                _hud.LogAbility(user, target, ability, worked, result);
            }

            _hud.MarkDirty();
        }
    }
}
