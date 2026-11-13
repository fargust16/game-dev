using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;

namespace GameDev.Confront.Turns
{
    /// <summary>
    /// CR4, abilities in a fight (10.2 §5f, 05.1 *Abilities of the slice*, 07.2 §3.3, §4.5): a fighter uses the active
    /// abilities it prepared, each for its AP and its share of maximum Will Power, which is their only limit; the passive
    /// ones always work. Infusion pours Will Power into each blow or shot for magical damage. The NPCs use theirs as
    /// `tools/balance/tactical_sim.py` does: at a turn's start Дрёма on the most dangerous of two or more awake enemies,
    /// Стой! on one near, Укол разума at the best target and Столп among two or more enemies beside; Сокрушение as a
    /// melee blow; Не сдвинуть from the AP left at the end, with two enemies within 3 cells; an ally beside a sleeper wakes
    /// it. CR5's Пелена and Гнёт are aimed at a cell (TurnEconomy.Area), Двойник leaves a copy and veils its caster
    /// (TurnEconomy.Unseen), and Рывок steps behind an enemy; the slice's NPCs use none of the four (COMBAT-8). Завеса, an
    /// NPC's of Limit 7 whom the slice's AI never uses, waits.
    /// </summary>
    public sealed partial class TurnEconomy
    {
        // tactical_sim.py: the AI's Не сдвинуть wants this many enemies within this many cells (authoring defaults).
        private const int BraceEnemies = 2;
        private const int BraceCells = 3;

        // tactical_sim.py CONTROL_VALUE: what an ability adds to its knower's Threat, the harm of a turn it stands for.
        private static readonly Dictionary<AbilityEffect, double> ControlValue = new Dictionary<AbilityEffect, double>
        {
            { AbilityEffect.Sleep, 6d }, { AbilityEffect.Order, 3d }, { AbilityEffect.Crush, 3d },
            { AbilityEffect.Support, 2d }, { AbilityEffect.Sting, 3d }, { AbilityEffect.Pillar, 3d },
        };

        private readonly List<Combatant> _around = new List<Combatant>();

        /// <summary>Raised after an ability: who, at whom (null for none), which, whether it worked, and what a hit did.</summary>
        public event Action<Combatant, Combatant, FightAbility, bool, StrikeResult> AbilityUsed;

        /// <summary>
        /// Whether <paramref name="c"/> can use its prepared ability <paramref name="index"/> now with the AP and Will Power
        /// it holds: a targeted one at anyone, one with no target where it stands (Столп wants an enemy beside).
        /// </summary>
        public bool CanUse(Combatant c, int index)
        {
            var ability = Prepared(c, index);
            return ability != null && c == Acting && ApLeft >= AbilityAp(c, ability) && FightRules.CanPay(c.Fighter, _rules.AbilityCost(c.Fighter, ability), true)
                && Usable(c, ability);
        }

        /// <summary>Whether <paramref name="c"/>'s ability <paramref name="index"/> can be aimed at <paramref name="target"/> from where it stands.</summary>
        public bool CanAim(Combatant c, int index, Combatant target)
        {
            var ability = Prepared(c, index);
            return ability != null && Aims(c, ability, target);
        }

        /// <summary>The AP an ability costs <paramref name="c"/>: its own, and for Сокрушение its weapon's too.</summary>
        public int AbilityAp(Combatant c, FightAbility ability) =>
            ability.Effect == AbilityEffect.Crush && c.Fighter.Weapon != null ? c.Fighter.Weapon.AttackAp + ability.Ap : ability.Ap;

        /// <summary>10.2 §5f: whether <paramref name="c"/> pours Will Power into its next blow or shot.</summary>
        private bool WouldInfuse(Combatant c)
        {
            var f = c.Fighter;
            if (f.Arcana == Player.Progression.Arcana.None || f.Weapon == null || _rules.InfusionShare(f) <= 0d)
            {
                return false;
            }

            var cost = _rules.InfusionCost(f);
            return c.TakesIntents ? c.Infusing && FightRules.CanPay(f, cost, true) : f.Infuses && f.WillPower >= 2d * cost;
        }

        /// <summary>Pays the next blow's Infusion, when it pours, and gives the share it adds.</summary>
        private double Infuse(Combatant c)
        {
            if (!WouldInfuse(c))
            {
                return 0d;
            }

            c.Fighter.WillPower -= _rules.InfusionCost(c.Fighter);
            return _rules.InfusionShare(c.Fighter);
        }

        /// <summary>05.1 Опора: the physical defence <paramref name="target"/>'s allies lend it, from one within its range that knows it.</summary>
        private double Support(Combatant target)
        {
            foreach (var ally in _order)
            {
                if (ally != target && ally.Standing && ally.Side == target.Side && ally.Fighter.Knows(AbilityEffect.Support) is FightAbility support
                    && Distance(ally.Cell, target.Cell) <= support.Range)
                {
                    return _rules.SupportDefence;
                }
            }

            return 0d;
        }

        /// <summary>A strike's rolls are done: the opening a failed ability left on the one struck closes.</summary>
        private static void Spent(Combatant struck) => struck.Fighter.Exposed = false;

        /// <summary>05.1: a turn begins: Disorientation aims it worse, Не сдвинуть ends, and a Столп's hold is read for it.</summary>
        private static void BeginAbilities(Combatant c)
        {
            var f = c.Fighter;
            f.Dazed = f.Disoriented > 0;
            f.Disoriented = Math.Max(0, f.Disoriented - 1);
            f.Braced = false;
            c.Taunter = c.TauntedBy;
            c.TauntedBy = null;
        }

        private static FightAbility Prepared(Combatant c, int index) =>
            c != null && index >= 0 && index < c.Fighter.Prepared.Count && c.Fighter.Prepared[index].InFight ? c.Fighter.Prepared[index] : null;

        /// <summary>The player's ability: its target, its cell or neither, checked, paid and carried out.</summary>
        private bool UseAbility(Combatant c, int index, Combatant target, CombatCell cell)
        {
            var ability = Prepared(c, index);
            if (ability == null || !CanUse(c, index) || (NeedsTarget(ability) && !Aims(c, ability, target))
                || (NeedsPoint(ability) && !AimsAt(c, ability, cell)))
            {
                return false;
            }

            Use(c, ability, NeedsTarget(ability) ? target : null, cell);
            return true;
        }

        /// <summary>Whether an ability is aimed at one enemy, so the player picks it after the ability.</summary>
        public static bool NeedsTarget(FightAbility ability) =>
            ability.Effect == AbilityEffect.Sleep || ability.Effect == AbilityEffect.Order || ability.Effect == AbilityEffect.Sting
            || ability.Effect == AbilityEffect.Crush || ability.Effect == AbilityEffect.Dash;

        /// <summary>
        /// Whether an ability can be used now, before its target or cell is chosen: Столп wants an enemy beside, Двойник no
        /// copy or veil of its caster's already, Рывок a leg that steps; Завеса waits.
        /// </summary>
        private bool Usable(Combatant c, FightAbility ability)
        {
            switch (ability.Effect)
            {
                case AbilityEffect.Sleep:
                case AbilityEffect.Order:
                case AbilityEffect.Sting:
                case AbilityEffect.Crush:
                case AbilityEffect.Brace:
                case AbilityEffect.Screen:
                case AbilityEffect.Press:
                    return true;
                case AbilityEffect.Pillar:
                    Around(c, _around);
                    return _around.Count > 0;
                case AbilityEffect.Double:
                    return c.Double == null && c.Fighter.Veiled == 0;
                case AbilityEffect.Dash:
                    return !c.Fighter.LegBroken; // a broken leg is Immobilisation (10.2)
                default:
                    return false;
            }
        }

        /// <summary>
        /// Whether <paramref name="ability"/> can reach <paramref name="target"/>: an enemy standing within its range and in
        /// sight (10.1 *Shot Line*); Дрёма only a person; Сокрушение one beside, with a melee weapon in hand; Рывок one with
        /// a free cell behind it.
        /// </summary>
        private bool Aims(Combatant c, FightAbility ability, Combatant target)
        {
            if (target == null || !target.Standing || target.Side == c.Side || Unseen(target))
            {
                return false;
            }

            switch (ability.Effect)
            {
                case AbilityEffect.Crush:
                    return c.Fighter.Weapon != null && !c.Fighter.Weapon.Ranged && Beside(c.Cell, target.Cell);
                case AbilityEffect.Dash:
                    if (!Behind(c, target, out _))
                    {
                        return false;
                    }

                    break;
                case AbilityEffect.Sleep:
                    if (target.Beast)
                    {
                        return false;
                    }

                    break;
                case AbilityEffect.Order:
                case AbilityEffect.Sting:
                    break;
                default:
                    return false;
            }

            return Distance(c.Cell, target.Cell) <= ability.Range && _grid.Shot(c.Cell, target.Cell).Clear;
        }

        /// <summary>Pays and carries out an ability whose AP, price and target or cell are already checked.</summary>
        private void Use(Combatant c, FightAbility ability, Combatant target, CombatCell point = default)
        {
            var f = c.Fighter;
            var ap = AbilityAp(c, ability);
            f.WillPower -= _rules.AbilityCost(f, ability);
            ApLeft -= ap;
            if (target != null)
            {
                Face(c, target.Cell);
            }

            var worked = true;
            var result = new StrikeResult();
            switch (ability.Effect)
            {
                case AbilityEffect.Sleep:
                    worked = _rules.Contest(f, target.Fighter, ability.Contest, _rolls);
                    if (worked)
                    {
                        target.Fighter.Asleep = Math.Max(1, ability.Rounds);
                    }
                    else
                    {
                        f.Exposed = true;
                    }

                    break;
                case AbilityEffect.Order:
                    worked = _rules.Contest(f, target.Fighter, ability.Contest, _rolls);
                    if (worked)
                    {
                        target.Fighter.LostTurns = Math.Max(target.Fighter.LostTurns, Math.Max(1, ability.Rounds));
                    }
                    else
                    {
                        f.Exposed = true;
                    }

                    break;
                case AbilityEffect.Sting:
                    target = ShotTaker(c, c.Cell, target); // one rule for every projectile (10.1 *Shot Line*)
                    result = _rules.Sting(f, target.Fighter, ability, AbilityBonus(c, target), _rolls);
                    Spent(target);
                    worked = result.Hit;
                    if (worked)
                    {
                        target.Fighter.Disoriented = Math.Max(1, ability.Rounds);
                        if (target.Fighter.Down)
                        {
                            _grid.Vacate(target.Id);
                        }
                    }
                    else
                    {
                        f.Disoriented = Math.Max(1, ability.Rounds);
                    }

                    c.Attacked = true;
                    SetHidden(c, false);
                    break;
                case AbilityEffect.Pillar:
                    Around(c, _around);
                    worked = false;
                    foreach (var e in _around)
                    {
                        if (_rules.Contest(f, e.Fighter, ability.Contest, _rolls))
                        {
                            e.TauntedBy = c;
                            worked = true;
                        }
                    }

                    break;
                case AbilityEffect.Crush:
                    ApLeft += ap; // the blow pays its own AP below, as one blow
                    worked = Strike(c, target, null, ability);
                    break;
                case AbilityEffect.Brace:
                    f.Braced = true;
                    break;
                case AbilityEffect.Screen:
                    RaiseScreen(c, ability, point);
                    break;
                case AbilityEffect.Press:
                    worked = Press(c, ability, point);
                    break;
                case AbilityEffect.Double:
                    RaiseDouble(c, ability);
                    break;
                case AbilityEffect.Dash:
                    Dash(c, ability, target);
                    target = null; // a step, not an act on the enemy
                    break;
            }

            AbilityUsed?.Invoke(c, target, ability, worked, result);
            AfterHit(c, target, worked, result.Damage); // 05.1 Двойник: an ability that lands on a copy breaks it
            if (ability.Effect == AbilityEffect.Crush)
            {
                return; // the blow checked the end and the turn itself
            }

            CheckCones();
            if (!CheckEnd() && Acting == c && (ability.EndOfTurn || !c.Standing))
            {
                if (c.Standing)
                {
                    EndTurn(c);
                }
                else
                {
                    NextTurn();
                }
            }
        }

        /// <summary>What the place and the moment add to a targeted ability: cover and Obscured beyond the next cell, an opening, Disorientation.</summary>
        private double AbilityBonus(Combatant c, Combatant target)
        {
            var bonus = _rules.SurroundedBonus(EnemiesBeside(target, c, c.Cell));
            if (Distance(c.Cell, target.Cell) > 1)
            {
                bonus -= _rules.CoverPenalty(CoverFacing(target.Cell, c.Cell));
            }

            if (_grid.Shot(c.Cell, target.Cell).Obscured)
            {
                bonus -= _rules.Obscured;
            }

            return bonus + Opening(c, target);
        }

        /// <summary>05.1: an opening on the target aims better, and Disorientation on the attacker worse.</summary>
        private double Opening(Combatant attacker, Combatant target) =>
            (target.Fighter.Exposed ? _rules.ExposedHit : 0d) - (attacker.Fighter.Dazed ? _rules.DisorientedHit : 0d);

        /// <summary>The awake enemies standing at <paramref name="c"/>'s sides.</summary>
        private void Around(Combatant c, List<Combatant> into)
        {
            into.Clear();
            foreach (var e in _order)
            {
                if (e.Standing && e.Side != c.Side && !e.Helpless && Beside(c.Cell, e.Cell))
                {
                    into.Add(e);
                }
            }
        }

        /// <summary>
        /// tactical_sim.py control: an NPC's abilities at its turn's start, each while its AP and Will Power allow and
        /// with something worth it: Дрёма, Стой!, Укол разума, Столп.
        /// </summary>
        private void Control(Combatant c)
        {
            var f = c.Fighter;
            if (f.Arcana == Player.Progression.Arcana.None || f.Prepared.Count == 0)
            {
                return;
            }

            Foes(c, _foes);
            var awake = 0;
            foreach (var e in _foes)
            {
                awake += e.Helpless ? 0 : 1;
            }

            var weaponAp = f.Weapon != null ? f.Weapon.AttackAp : 0;
            if (f.Knows(AbilityEffect.Sleep) is FightAbility sleep && awake >= 2 && ApLeft >= sleep.Ap && Spare(f, sleep)
                && MostDangerous(c, sleep) is Combatant sleeper)
            {
                Use(c, sleep, sleeper);
            }

            if (IsRunning && Acting == c && f.Knows(AbilityEffect.Order) is FightAbility order && ApLeft >= order.Ap + weaponAp && Spare(f, order)
                && MostDangerous(c, order) is Combatant ordered)
            {
                Use(c, order, ordered);
            }

            if (IsRunning && Acting == c && f.Knows(AbilityEffect.Sting) is FightAbility sting && ApLeft >= sting.Ap && Spare(f, sting))
            {
                Combatant best = null;
                var top = double.NegativeInfinity;
                foreach (var e in _foes)
                {
                    var score = Aims(c, sting, e) ? Score(c, e) : double.NegativeInfinity;
                    if (score > top)
                    {
                        top = score;
                        best = e;
                    }
                }

                if (best != null)
                {
                    Use(c, sting, best);
                }
            }

            if (IsRunning && Acting == c && f.Knows(AbilityEffect.Pillar) is FightAbility pillar && ApLeft >= pillar.Ap + weaponAp && Spare(f, pillar))
            {
                Around(c, _around);
                if (_around.Count >= 2)
                {
                    Use(c, pillar, null);
                }
            }
        }

        /// <summary>The awake enemy with the highest Threat that <paramref name="ability"/> reaches, or null.</summary>
        private Combatant MostDangerous(Combatant c, FightAbility ability)
        {
            Combatant best = null;
            var top = double.NegativeInfinity;
            foreach (var e in _foes)
            {
                if (e.Helpless || !Aims(c, ability, e))
                {
                    continue;
                }

                var threat = Threat(e);
                if (threat > top)
                {
                    top = threat;
                    best = e;
                }
            }

            return best;
        }

        /// <summary>An NPC pays only while it keeps something back (`combat_sim.py` can_spend).</summary>
        private bool Spare(Fighter f, FightAbility ability) => FightRules.CanPay(f, _rules.AbilityCost(f, ability), false);

        /// <summary>tactical_sim.py: an ally beside a sleeper wakes it, for its AP.</summary>
        private bool Wake(Combatant c)
        {
            if (ApLeft < _rules.WakeAp)
            {
                return false;
            }

            foreach (var ally in _order)
            {
                if (ally != c && ally.Standing && ally.Side == c.Side && ally.Fighter.Asleep > 0 && Distance(ally.Cell, c.Cell) == 1)
                {
                    ally.Fighter.Asleep = 0;
                    ApLeft -= _rules.WakeAp;
                    return true;
                }
            }

            return false;
        }

        /// <summary>tactical_sim.py brace: Не сдвинуть from the AP left, with two enemies within 3 cells.</summary>
        private void Brace(Combatant c)
        {
            var f = c.Fighter;
            if (!(f.Knows(AbilityEffect.Brace) is FightAbility brace) || ApLeft < brace.Ap || !Spare(f, brace))
            {
                return;
            }

            var near = 0;
            foreach (var e in _order)
            {
                near += e.Standing && e.Side != c.Side && Distance(e.Cell, c.Cell) <= BraceCells ? 1 : 0;
            }

            if (near >= BraceEnemies)
            {
                f.WillPower -= _rules.AbilityCost(f, brace);
                ApLeft -= brace.Ap;
                f.Braced = true;
                AbilityUsed?.Invoke(c, null, brace, true, default);
            }
        }

        /// <summary>tactical_sim.py threat: what a fighter's abilities add to the harm it does in a turn.</summary>
        private static double Control(Fighter f)
        {
            var sum = 0d;
            foreach (var a in f.Prepared)
            {
                sum += ControlValue.TryGetValue(a.Effect, out var v) ? v : 0d;
            }

            foreach (var a in f.Passives)
            {
                sum += ControlValue.TryGetValue(a.Effect, out var v) ? v : 0d;
            }

            return sum;
        }
    }
}
