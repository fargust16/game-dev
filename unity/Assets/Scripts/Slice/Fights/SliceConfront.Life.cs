using System;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// S3a.7: the character's life between fights. Its HP, its conditions and the supplies it carries are kept in the
    /// world state, so a fight starts with what the last one left and ends by writing it back; outside a fight the
    /// conditions and the ordinary return of HP run on the world clock's ticks (10.2). B gives First Aid to the character
    /// itself, the most urgent condition first: in a fight for its AP, outside one for its ticks. An Environment Action,
    /// a treatment and a condition's tick are said in the fight's log, and the life line by the bottom left shows HP and
    /// conditions while the character is hurt.
    /// </summary>
    public sealed partial class SliceConfront
    {
        // World state keys of this host (17 C.6). HP is kept as the thousandths of maximum HP lost, so a new game, with
        // no key written, starts at full health.
        public const string HpLostKey = "player.hp.lost";
        public const string BleedingKey = "player.bleeding";
        public const string PoisonedKey = "player.poisoned";
        public const string LegKey = "player.leg";

        /// <summary>Set by the Master of First Aid's lesson (S5a); an antidote and a splint need it (10.2).</summary>
        public const string LessonKey = "player.lesson.firstaid";

        /// <summary>The bandages, antidotes and splints carried, by <see cref="Treatment"/>.</summary>
        public static readonly string[] SupplyKeys = { "player.kit.bandage", "player.kit.antidote", "player.kit.splint" };

        // Authoring defaults, S3a.7: what the development kit tops the supplies up to until S5a's shops; and the order
        // First Aid takes: the leg, which holds the character in place, then Bleeding, which takes HP, then Poison.
        private static readonly int[] KitSupplies = { 2, 1, 1 };
        private static readonly Treatment[] Urgency = { Treatment.Splint, Treatment.Bandage, Treatment.Antidote };

        private IWorldState _state;
        private IWorldClock _clock;
        // Authoring default, S3a.7: seconds a treatment's result stays on the life line.
        private const float NoteSeconds = 8f;

        private string _lifeNote;
        private float _lifeNoteUntil;
        private int _lifeShown = -1;

        /// <summary>The character's life as a fight sees it: a fighter in the development kit, with the kept HP and conditions.</summary>
        public Fighter PlayerFighter()
        {
            var fighter = _settings.FightRules.Fighter(_character, null, new WeaponSet(_settings.KitSword), _settings.KitArmour,
                new WeaponSet(_settings.KitRifle, _settings.KitPouch));
            if (_state != null)
            {
                fighter.Hp = fighter.MaxHp * (1d - (Mathf.Clamp(_state.Get(HpLostKey), 0, 1000) / 1000d));
                fighter.Bleeding = _state.IsSet(BleedingKey);
                fighter.Poisoned = _state.IsSet(PoisonedKey);
                fighter.LegBroken = _state.IsSet(LegKey);
            }

            return fighter;
        }

        /// <summary>Keeps the fighter's HP and conditions; at 0 HP the character is dead (S3a.9).</summary>
        private void SaveLife(Fighter fighter)
        {
            if (_state == null)
            {
                return;
            }

            var lost = (int)Math.Round((1d - (fighter.Hp / fighter.MaxHp)) * 1000d);
            _state.Set(HpLostKey, fighter.Down ? 1000 : Mathf.Clamp(lost, 0, 999));
            _state.Set(BleedingKey, fighter.Bleeding ? 1 : 0);
            _state.Set(PoisonedKey, fighter.Poisoned ? 1 : 0);
            _state.Set(LegKey, fighter.LegBroken ? 1 : 0);
        }

        private bool Healthy => _state == null
            || (_state.Get(HpLostKey) == 0 && !_state.IsSet(BleedingKey) && !_state.IsSet(PoisonedKey) && !_state.IsSet(LegKey));

        /// <summary>The development kit's supplies, topped up, and the lesson, carried into the fight.</summary>
        private void Equip(Combatant player)
        {
            player.KnowsFirstAid = _state != null && _state.IsSet(LessonKey);
            for (var i = 0; i < SupplyKeys.Length; i++)
            {
                if (_state != null && _state.Get(SupplyKeys[i]) < KitSupplies[i])
                {
                    _state.Set(SupplyKeys[i], KitSupplies[i]);
                }

                player.Supplies[i] = _state != null ? _state.Get(SupplyKeys[i]) : KitSupplies[i];
            }
        }

        /// <summary>What the fight left: HP, conditions and the supplies still carried.</summary>
        private void Unequip(Combatant player)
        {
            SaveLife(player.Fighter);
            for (var i = 0; i < SupplyKeys.Length && _state != null; i++)
            {
                _state.Set(SupplyKeys[i], player.Supplies[i]);
            }
        }

        /// <summary>One world tick outside a fight (10.2): the conditions, then HP's ordinary return while nothing stops it.</summary>
        private void OnTick(long tick)
        {
            if (_settings == null || _state == null || InFight || _died || Healthy)
            {
                return;
            }

            var fighter = PlayerFighter();
            _settings.FightRules.Tick(fighter, _seed.Fork(SliceSeedStreams.Conditions, (int)tick), true);
            SaveLife(fighter);
            if (fighter.Down && !_died)
            {
                // Bled out after a fight: the run ends as a death in one does (S3a.9).
                _died = true;
                _player ??= FindAnyObjectByType<PlayerLocomotion>();
                if (_player != null)
                {
                    _player.enabled = false;
                }

                SliceDeathScreen.Show(_clock);
            }
        }

        /// <summary>The most urgent treatment the character can give itself now, or null.</summary>
        private Treatment? Choose(Fighter fighter, Func<Treatment, int> supplies, bool knows)
        {
            foreach (var treatment in Urgency)
            {
                if (FightRules.Needs(fighter, treatment) && supplies(treatment) > 0 && (knows || !_settings.FightRules.TreatmentNeedsLesson(treatment)))
                {
                    return treatment;
                }
            }

            return null;
        }

        private bool TreatInFight(Combatant player) =>
            Choose(player.Fighter, t => player.Supplies[(int)t], player.KnowsFirstAid) is Treatment treatment
            && _turns.SubmitIntent(TurnIntent.Treat(treatment, player.Id));

        /// <summary>
        /// First Aid outside a fight (10.2): the treatment takes its ticks, through which the conditions run on, and then its
        /// check; a success ends the condition and uses the item, a failure keeps it.
        /// </summary>
        public bool TreatOutside()
        {
            if (_settings == null || _state == null || _clock == null)
            {
                return false;
            }

            var knows = _state.IsSet(LessonKey);
            var choice = Choose(PlayerFighter(), t => _state.Get(SupplyKeys[(int)t]), knows);
            if (!(choice is Treatment treatment))
            {
                Note(Loc.Get(Healthy ? "ui.life.nothing" : "ui.life.nosupply"));
                return false;
            }

            var rules = _settings.FightRules;
            _clock.AdvanceTicks(rules.TreatmentTicks(treatment));
            var fighter = PlayerFighter();
            var rolls = _seed.Fork(SliceSeedStreams.ActionChecks, (int)_clock.Tick, 7000 + (int)treatment);
            var success = rolls.NextDouble() < rules.TreatmentChance(_character, treatment);
            if (success)
            {
                FightRules.Cure(fighter, treatment);
                SaveLife(fighter);
                _state.Set(SupplyKeys[(int)treatment], _state.Get(SupplyKeys[(int)treatment]) - 1);
            }

            Note(Loc.Format(success ? "ui.life.treat.ok" : "ui.life.treat.failed", Loc.Get("ui.treatment." + treatment)));
            return true;
        }

        private void Note(string text)
        {
            _lifeNote = text;
            _lifeNoteUntil = Time.unscaledTime + NoteSeconds;
            _lifeShown = -1;
        }

        /// <summary>The life line: HP and conditions while hurt, the First Aid key while a condition lasts, and the last treatment.</summary>
        private void ShowLife(Fighter inFight)
        {
            if (_hud == null)
            {
                return;
            }

            if (_lifeNote != null && Time.unscaledTime > _lifeNoteUntil)
            {
                _lifeNote = null;
                _lifeShown = -1;
            }

            int lost;
            int flags;
            if (inFight != null)
            {
                lost = inFight.MaxHp > 0d ? (int)Math.Round((1d - (inFight.Hp / inFight.MaxHp)) * 1000d) : 0;
                flags = (inFight.Bleeding ? 1 : 0) | (inFight.Poisoned ? 2 : 0) | (inFight.LegBroken ? 4 : 0);
            }
            else if (_state != null)
            {
                lost = _state.Get(HpLostKey);
                flags = (_state.IsSet(BleedingKey) ? 1 : 0) | (_state.IsSet(PoisonedKey) ? 2 : 0) | (_state.IsSet(LegKey) ? 4 : 0);
            }
            else
            {
                return;
            }

            var shown = (lost << 3) | flags | (inFight != null ? 1 << 20 : 0);
            if (shown == _lifeShown)
            {
                return;
            }

            _lifeShown = shown;
            if (lost <= 0 && flags == 0 && string.IsNullOrEmpty(_lifeNote))
            {
                _hud.Life(null);
                return;
            }

            var fighter = inFight ?? PlayerFighter();
            var text = Loc.Format("ui.fight.hp", FightHud.Hp(fighter.Hp), FightHud.Hp(fighter.MaxHp));
            var conditions = FightHud.Conditions(fighter);
            if (conditions.Length > 0)
            {
                text += " · " + conditions + "\n" + Loc.Get("ui.life.hint");
            }

            if (!string.IsNullOrEmpty(_lifeNote))
            {
                text += "\n" + _lifeNote;
            }

            _hud.Life(text);
        }

        /// <summary>An Environment Action is said in the log; a breach that worked clears its block, and the grid reads the place again.</summary>
        private void OnTraversed(Combatant c, TraverseOption option, bool success, double fallen)
        {
            if (success && option.Kind == EnvironmentActionKind.KickBreach && option.Tag is Component block && block != null)
            {
                block.gameObject.SetActive(false);
                _grid.Reread();
                _view.Show(_grid);
            }

            var line = Loc.Format(success ? "ui.fight.log.env.done" : "ui.fight.log.env.failed", Name(c), Loc.Get("ui.fight.env.name." + option.Kind));
            if (fallen > 0d)
            {
                line += "  " + Loc.Format("ui.fight.log.fell", Name(c), Math.Max(1, (int)Math.Round(fallen)));
            }

            _hud.Line(line);
            _hud.MarkDirty();
        }

        private void OnTreated(Combatant c, Combatant patient, Treatment treatment, bool success)
        {
            _hud.Line(Loc.Format(success ? "ui.fight.log.treat.ok" : "ui.fight.log.treat.failed", Name(c), Loc.Get("ui.treatment." + treatment)));
            _hud.MarkDirty();
        }

        private void OnConditioned(Combatant c, ConditionTick tick)
        {
            if (tick.Bled > 0d)
            {
                _hud.Line(Loc.Format("ui.fight.log.bleeds", Name(c), Math.Max(1, (int)Math.Round(tick.Bled))));
            }

            _hud.MarkDirty();
        }
    }
}
