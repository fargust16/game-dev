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
    /// `tools/balance/tactical_sim.py` does (10.2 §5f *The balance after the answers*, CR7): at a turn's start Дрёма on the
    /// most dangerous of two or more awake enemies, Стой! on one near, Гнёт where it strikes enemies enough more than its
    /// own people, Укол разума at the best target and Столп among two or more enemies beside; Сокрушение as a melee
    /// blow, and Рывок when it saves a walk of 3 cells or more; from the AP left at the end Пелена over its own people
    /// when a shooter can fire at it, Двойник, and Не сдвинуть, the last two whenever an enemy could attack it in its next
    /// turn; a wounded one keeps Двойник's AP from its turn's start; an ally beside a sleeper wakes it. CR5's Пелена and
    /// Гнёт are aimed at a cell (TurnEconomy.Area), and Двойник leaves a copy and veils its caster (TurnEconomy.Unseen).
    /// Завеса, an NPC's of Limit 7 whom the slice's AI never uses, waits.
    /// </summary>
    public sealed partial class TurnEconomy
    {
        // tactical_sim.py threatened: an enemy threatens a fighter from its weapon's reach and this many cells more
        // (authoring default).
        private const int ThreatCells = 6;

        // tactical_sim.py screen: Пелена is worth it with an enemy shooter within its range and this many cells more.
        private const int ScreenCells = 4;

        // tactical_sim.py PRESS_FRIEND, PRESS_NET: Гнёт wants the enemies it strikes, less this much for each of its own,
        // to come to this (authoring defaults).
        private const double PressFriend = 1.5d;
        private const double PressNet = 2d;

        // tactical_sim.py dash: Рывок is worth it when the walk it saves is this many cells or more (authoring default).
        private const int DashSaves = 3;

        // tactical_sim.py DOUBLE_HP: at or under this share of its HP an NPC keeps Двойник's AP (authoring default).
        private const double DoubleHp = 0.5d;

        // tactical_sim.py CONTROL_VALUE: what an ability adds to its knower's Threat, the harm of a turn it stands for.
        private static readonly Dictionary<AbilityEffect, double> ControlValue = new Dictionary<AbilityEffect, double>
        {
            { AbilityEffect.Sleep, 6d }, { AbilityEffect.Order, 3d }, { AbilityEffect.Crush, 3d },
            { AbilityEffect.Support, 2d }, { AbilityEffect.Sting, 3d }, { AbilityEffect.Pillar, 3d },
        };

        private readonly List<Combatant> _around = new List<Combatant>();
        private readonly List<CombatCell> _spots = new List<CombatCell>();

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
        /// with something worth it: Дрёма, Стой!, Гнёт, Укол разума, Столп.
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

            if (IsRunning && Acting == c && f.Knows(AbilityEffect.Press) is FightAbility press && ApLeft >= press.Ap && Spare(f, press)
                && PressSpot(c, press) is CombatCell spot)
            {
                Use(c, press, null, spot);
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

        /// <summary>
        /// tactical_sim.py brace: Не сдвинуть from the AP left, whenever an enemy could attack it in its next turn (since the
        /// balance after COMBAT-7, not only with two near).
        /// </summary>
        private void Brace(Combatant c)
        {
            var f = c.Fighter;
            if (!(f.Knows(AbilityEffect.Brace) is FightAbility brace) || ApLeft < brace.Ap || !Spare(f, brace) || !Threatened(c))
            {
                return;
            }

            f.WillPower -= _rules.AbilityCost(f, brace);
            ApLeft -= brace.Ap;
            f.Braced = true;
            AbilityUsed?.Invoke(c, null, brace, true, default);
        }

        /// <summary>
        /// tactical_sim.py threatened: an awake enemy it sees could attack it in that enemy's next turn, standing within its
        /// weapon's reach and <see cref="ThreatCells"/> more.
        /// </summary>
        private bool Threatened(Combatant c)
        {
            foreach (var e in _order)
            {
                var weapon = e.Fighter.Weapon;
                if (e.Standing && e.Side != c.Side && !e.Helpless && !Unseen(e) && weapon != null
                    && Distance(e.Cell, c.Cell) <= (weapon.Ranged ? weapon.RangeCells : 1) + ThreatCells)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// tactical_sim.py screen: Пелена from the AP a turn leaves, over the caster and as many of its own people as it
        /// can cover, when no cloud covers it yet and an awake enemy shooter it sees stands within its range and
        /// <see cref="ScreenCells"/> more.
        /// </summary>
        private void ScreenAtEnd(Combatant c)
        {
            var f = c.Fighter;
            if (!(f.Knows(AbilityEffect.Screen) is FightAbility screen) || ApLeft < screen.Ap || !Spare(f, screen) || Clouded(c.Cell))
            {
                return;
            }

            var shooter = false;
            foreach (var e in _order)
            {
                var weapon = e.Fighter.Weapon;
                shooter |= e.Standing && e.Side != c.Side && !e.Helpless && !Unseen(e) && weapon != null && weapon.Ranged
                    && Distance(e.Cell, c.Cell) <= weapon.RangeCells + ScreenCells;
            }

            if (shooter && ScreenSpot(c, screen) is CombatCell spot)
            {
                Use(c, screen, null, spot);
            }
        }

        /// <summary>
        /// tactical_sim.py screen_spot: of the centres whose cloud covers the caster, the one covering most of its own people
        /// not yet in a cloud, each enemy covered counting twice against it; null if none covers more of them.
        /// </summary>
        private CombatCell? ScreenSpot(Combatant c, FightAbility screen)
        {
            var half = Math.Max(1, screen.Area) / 2;
            AreaOf(c.Cell, screen, _spots); // the centres within half the area of the caster are those whose cloud covers it
            CombatCell? spot = null;
            var best = 0;
            foreach (var center in _spots)
            {
                if (!AimsAt(c, screen, center))
                {
                    continue;
                }

                int mine = 0, foes = 0;
                foreach (var e in _order)
                {
                    if (!e.Standing || !InArea(e.Cell, center, half))
                    {
                        continue;
                    }

                    if (e.Side != c.Side)
                    {
                        foes++;
                    }
                    else if (!Clouded(e.Cell))
                    {
                        mine++;
                    }
                }

                var value = mine - (2 * foes);
                if (mine > 0 && value > best)
                {
                    best = value;
                    spot = center;
                }
            }

            return spot;
        }

        /// <summary>
        /// tactical_sim.py press_spot: of the cells round each enemy it sees and within reach, the one where the enemies Гнёт
        /// strikes, less <see cref="PressFriend"/> for each of its own, come to most, and to <see cref="PressNet"/> at least.
        /// </summary>
        private CombatCell? PressSpot(Combatant c, FightAbility press)
        {
            CombatCell? spot = null;
            var best = PressNet - 1e-9;
            foreach (var e in _foes)
            {
                AreaOf(e.Cell, press, _spots);
                foreach (var center in _spots)
                {
                    if (!AimsAt(c, press, center))
                    {
                        continue;
                    }

                    var (foes, friends) = PressCount(c, press, center);
                    var value = foes - (PressFriend * friends);
                    if (value > best)
                    {
                        best = value;
                        spot = center;
                    }
                }
            }

            return spot;
        }

        /// <summary>
        /// tactical_sim.py dash: Рывок puts a melee fighter behind its target, not beside it yet, when walking there takes
        /// <see cref="DashSaves"/> cells or more and a blow is still paid for after.
        /// </summary>
        private bool TryDash(Combatant c, Combatant target, int cost)
        {
            var f = c.Fighter;
            if (!(f.Knows(AbilityEffect.Dash) is FightAbility dash) || f.Weapon == null || f.Weapon.Ranged || target == null
                || Beside(c.Cell, target.Cell) || ApLeft < dash.Ap + cost || !Usable(c, dash) || !Spare(f, dash) || !Aims(c, dash, target))
            {
                return false;
            }

            var walk = PathToStrike(c, target, null);
            if (walk >= 0 && walk < DashSaves)
            {
                return false;
            }

            Use(c, dash, target);
            return true;
        }

        /// <summary>tactical_sim.py double: Двойник from the AP a turn leaves, whenever an enemy could attack it in its next turn.</summary>
        private void DoubleAtEnd(Combatant c)
        {
            var f = c.Fighter;
            if (f.Knows(AbilityEffect.Double) is FightAbility copy && ApLeft >= copy.Ap && Usable(c, copy) && Spare(f, copy) && Threatened(c))
            {
                Use(c, copy, null);
            }
        }

        /// <summary>
        /// tactical_sim.py keeps_for_double: the AP a wounded NPC keeps back from its turn's start for Двойник, at or under
        /// <see cref="DoubleHp"/> of its HP and threatened; none otherwise.
        /// </summary>
        private int KeepsForDouble(Combatant c)
        {
            var f = c.Fighter;
            return f.Knows(AbilityEffect.Double) is FightAbility copy && f.Hp <= DoubleHp * f.MaxHp && Usable(c, copy) && Spare(f, copy)
                && Threatened(c) ? Math.Min(copy.Ap, ApLeft) : 0;
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
