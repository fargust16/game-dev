using System;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.People;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// S3a.7: the character's life between fights. Its HP, its conditions and the supplies it carries are kept in the
    /// world state, so a fight starts with what the last one left and ends by writing it back; outside a fight the
    /// conditions and the ordinary return of HP run on the world clock's ticks (10.2). B gives First Aid to the character
    /// itself, the most urgent condition first: in a fight for its AP, outside one for its ticks, with the supplies in the
    /// bag (S5a.7, <see cref="SliceFirstAid"/>). An Environment Action,
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

        /// <summary>Set by the Master of First Aid's lesson (S5a.3); an antidote and a splint need it (10.2).</summary>
        public const string LessonKey = SliceFirstAid.LessonKey;

        // Authoring default, S3a.7: what the development fight tops the bag's supplies up to, by Treatment.
        private static readonly int[] KitSupplies = { 2, 1, 1 };

        private IWorldState _state;
        private IWorldClock _clock;
        // The frame First Aid's bar in a fight was stopped: its Space must not also end the turn.
        private int _checkedFrame = -1;
        private int _lifeShown = -1;

        /// <summary>First Aid with the supplies in the bag (S5a.7); null in a test that needs none.</summary>
        public SliceFirstAid FirstAid { get; set; }

        /// <summary>The character as a fighter in what it wears and holds (S5a.2); null leaves it the development kit.</summary>
        public Func<Fighter> WornFighter { get; set; }

        /// <summary>The ticks of a Limit potion's Assimilation left (S5b.5), the rounds the character enters a fight Stunned.</summary>
        public Func<int> Defenceless { get; set; }

        /// <summary>The character's life as a fight sees it: a fighter in what it wears, with the kept HP and conditions.</summary>
        public Fighter PlayerFighter() => PlayerFighter(false);

        /// <param name="kit">The development command's kit, a pistol, leather, and the rifle as its second set (S3a.4; 10.2 §5d).</param>
        private Fighter PlayerFighter(bool kit)
        {
            var fighter = !kit && WornFighter != null
                ? WornFighter()
                : _settings.FightRules.Fighter(_character, PlayerLimit, Arms(RoleFirearm.Pistol), _settings.KitArmour, Arms(RoleFirearm.Rifle));
            Initiate(fighter); // CR4: its Arcana, Prepared abilities and Will Power
            fighter.Defenceless = Defenceless != null ? Defenceless() : 0; // 20 §3: Stunned while a potion settles
            fighter.CheckShift = Mind != null ? Mind.CheckPenalty : 0; // 07.2 §3.4: a troubled mind's Action Checks
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
            SaveWillPower(fighter);
        }

        private bool Healthy => _state == null
            || (_state.Get(HpLostKey) == 0 && !_state.IsSet(BleedingKey) && !_state.IsSet(PoisonedKey) && !_state.IsSet(LegKey));

        /// <summary>The lesson and the bag's supplies carried into the fight; the development fight tops them up first.</summary>
        private void Equip(Combatant player, bool kit = false)
        {
            if (FirstAid == null)
            {
                player.KnowsFirstAid = _state != null && _state.IsSet(LessonKey);
                return;
            }

            for (var i = 0; kit && i < KitSupplies.Length; i++)
            {
                FirstAid.TopUp((Treatment)i, KitSupplies[i]);
            }

            FirstAid.Load(player);
        }

        /// <summary>What the fight left: HP and conditions, and the supplies it used gone from the bag.</summary>
        private void Unequip(Combatant player)
        {
            SaveLife(player.Fighter);
            FirstAid?.Spend(player);
        }

        /// <summary>
        /// One world tick outside a fight (10.2): Will Power's return (CR4, 07.2 §3.3), the conditions, then HP's ordinary
        /// return while nothing stops it.
        /// </summary>
        private void OnTick(long tick)
        {
            if (_settings == null || _state == null || InFight || _died)
            {
                return;
            }

            if (Mind == null || Mind.WillPowerReturns(tick))
            {
                RestoreWillPower(); // 07.2 §3.3–3.4: slower below Stable, none Very tired
            }

            if (Healthy)
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

        /// <summary>
        /// First Aid in a fight (10.2): with the treatment chosen and its AP in hand, the player's Reaction Check shows at
        /// once, as the fight's turn stands still anyway, and the treatment is given with its result. A Kneel of five
        /// seconds would hold every turn up, so the fight plays none.
        /// </summary>
        private bool TreatInFight(Combatant player)
        {
            var reaction = SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Reaction : null;
            if (FirstAid == null || reaction == null || reaction.Running
                || !(FirstAid.Choose(player.Fighter, t => player.Supplies[(int)t], player.KnowsFirstAid) is Treatment treatment)
                || _turns.ApLeft < _settings.FightRules.TreatmentAp(treatment))
            {
                return false;
            }

            var id = player.Id;
            return reaction.Begin(FirstAid.Bar(treatment), "ui.life.treat.caption", result =>
            {
                _checkedFrame = Time.frameCount;
                if (_turns != null && _turns.IsRunning)
                {
                    _turns.SubmitIntent(TurnIntent.Treat(treatment, id, result != ReactionResult.Bad));
                    _hud.MarkDirty();
                }
            });
        }

        /// <summary>
        /// First Aid outside a fight (10.2): the character kneels, the treatment's ticks pass with the conditions running
        /// on, and the Reaction Check comes where it begins to rise, the world paused (07.1 §8a); a good result ends the
        /// condition and uses the item, a bad one keeps it. False when it could not begin.
        /// </summary>
        public bool TreatOutside()
        {
            if (_settings == null || _state == null || _clock == null || FirstAid == null)
            {
                return false;
            }

            var fault = FirstAid.Fault(PlayerFighter(), out var treatment);
            if (fault != null)
            {
                Note(Loc.Get(Healthy ? "ui.life.nothing" : fault));
                return false;
            }

            var work = SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Work : null;
            if (work == null || !work.Begin(new WorkStroke
                {
                    Clip = "Kneel",
                    Steady = true,
                    Bar = () => FirstAid.Bar(treatment),
                    CaptionKey = "ui.life.treat.caption",
                    Settle = result =>
                    {
                        var success = FirstAid.Finish(PlayerFighter, SaveLife, treatment, result);
                        Note(Loc.Format(success ? "ui.life.treat.ok" : "ui.life.treat.failed", Loc.Get("ui.treatment." + treatment)));
                    },
                }))
            {
                return false;
            }

            FirstAid.Work(treatment);
            return true;
        }

        // A treatment's result is a Screen Message, short and on the screen (owner, 2026-11-12), no longer on the life line.
        private void Note(string text) => DialogueColumn()?.ShowMessage(text);

        /// <summary>The life line: HP and conditions while hurt, and the First Aid key while a condition lasts.</summary>
        private void ShowLife(Fighter inFight)
        {
            if (_hud == null)
            {
                return;
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
            if (lost <= 0 && flags == 0)
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
            TrainTreatment(c, treatment, success);
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
