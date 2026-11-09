using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Player.Progression;
using Random = System.Random;

namespace GameDev.Confront.Rules
{
    /// <summary>The side that won a headless fight; None when it ran out of rounds.</summary>
    public enum FightSide
    {
        None,
        A,
        B,
    }

    /// <summary>
    /// A fight with no space (S3a.2), in the model of `tools/balance/combat_sim.py` and `beast_sim.py`, on the rules of
    /// <see cref="FightRules"/>. Initiative sets the order once. In its turn each fighter strikes while its AP last, a
    /// melee fighter spending its first turn's AP on closing in, and keeps what is left for reactions. With three or
    /// more standing on its side its target counts as Surrounded by two, with two at even odds. Side A strikes its
    /// weakest enemy standing, as `combat_sim.py`'s focus, and side B at random, until Target Rules V0 (S3a.8). After
    /// each round, a world tick, Bleeding takes its share. A pack breaks at the end of the round in which its first
    /// fighter fell. It stands for no grid and draws nothing.
    /// </summary>
    public sealed class HeadlessFight
    {
        private readonly FightRules _rules;
        private readonly IReadOnlyList<Fighter> _a;
        private readonly IReadOnlyList<Fighter> _b;
        private readonly bool _packB;
        private readonly Random _rolls;
        private readonly List<Fighter> _order = new List<Fighter>();
        private readonly HashSet<Fighter> _bleeding = new HashSet<Fighter>();

        /// <param name="sideBIsPack">Side B breaks once any of its fighters falls, as a wolf pack does (10.2 §5b *Beasts of the slice*).</param>
        public HeadlessFight(FightRules rules, IReadOnlyList<Fighter> sideA, IReadOnlyList<Fighter> sideB, Random rolls, bool sideBIsPack = false)
        {
            _rules = rules;
            _a = sideA;
            _b = sideB;
            _rolls = rolls;
            _packB = sideBIsPack && sideB.Count >= 2;
        }

        public int Round { get; private set; }

        /// <summary>The turn order Initiative set.</summary>
        public IReadOnlyList<Fighter> Order => _order;

        public bool IsBleeding(Fighter fighter) => _bleeding.Contains(fighter);

        /// <summary>The fighter starts to bleed, as a beast's critical hit makes it.</summary>
        public void Bleed(Fighter fighter) => _bleeding.Add(fighter);

        /// <summary>Every fighter enters whole; the higher Initiative goes first, a tie to the higher Dexterity, then at random.</summary>
        public void Begin()
        {
            Round = 0;
            _bleeding.Clear();
            var all = _a.Concat(_b).ToList();
            foreach (var fighter in all)
            {
                fighter.Hp = fighter.MaxHp;
                _rules.Enter(fighter, _rolls);
            }

            var rolled = all.Select(f => (Fighter: f, Initiative: _rules.Initiative(f, _rolls), Tie: _rolls.NextDouble())).ToList();
            _order.Clear();
            _order.AddRange(rolled
                .OrderByDescending(r => r.Initiative)
                .ThenByDescending(r => r.Fighter.Get(CharacterAttribute.Dexterity))
                .ThenBy(r => r.Tie)
                .Select(r => r.Fighter));
        }

        /// <summary>One round: each standing fighter's turn in order, then Bleeding. The winner, or null while undecided.</summary>
        public FightSide? PlayRound()
        {
            Round++;
            foreach (var fighter in _order)
            {
                if (!fighter.Down)
                {
                    Turn(fighter);
                }
            }

            foreach (var fighter in _a.Concat(_b))
            {
                if (fighter.Down || !_bleeding.Contains(fighter))
                {
                    continue;
                }

                fighter.Hp -= _rules.BleedingShare * fighter.MaxHp;
                if (_rolls.NextDouble() < fighter.Get(CharacterAttribute.Endurance) * _rules.BleedingEndsPerEndurance)
                {
                    _bleeding.Remove(fighter);
                }
            }

            if (_a.All(f => f.Down))
            {
                return FightSide.B;
            }

            if (_b.All(f => f.Down) || (_packB && _b.Any(f => f.Down)))
            {
                return FightSide.A;
            }

            return null;
        }

        public FightSide Run()
        {
            Begin();
            for (var round = 0; round < _rules.HeadlessRounds; round++)
            {
                var winner = PlayRound();
                if (winner.HasValue)
                {
                    return winner.Value;
                }
            }

            return FightSide.None;
        }

        private void Turn(Fighter fighter)
        {
            if (fighter.LostTurns > 0)
            {
                fighter.LostTurns--;
                fighter.Reserve = 0;
                return;
            }

            var onA = _a.Contains(fighter);
            var own = onA ? _a : _b;
            var enemies = onA ? _b : _a;
            var weapon = fighter.Weapon;
            var ap = _rules.TurnAp(fighter);
            if (Round == 1 && !weapon.Ranged)
            {
                ap -= _rules.CloseInAp;
            }

            while (ap >= weapon.AttackAp)
            {
                var target = Target(enemies, onA);
                if (target == null)
                {
                    break;
                }

                var standing = own.Count(f => !f.Down);
                var surrounded = !weapon.Ranged && (standing >= 3 || (standing == 2 && _rolls.NextDouble() < 0.5));
                var context = new StrikeContext { HitBonus = surrounded ? _rules.SurroundedBonus(2) : 0d };
                if (_rules.Strike(fighter, target, context, _rolls).Bleeds)
                {
                    _bleeding.Add(target);
                }

                ap -= weapon.AttackAp;
            }

            fighter.Reserve = Math.Max(0, ap);
        }

        private Fighter Target(IReadOnlyList<Fighter> enemies, bool weakest)
        {
            Fighter chosen = null;
            var standing = 0;
            foreach (var enemy in enemies)
            {
                if (enemy.Down)
                {
                    continue;
                }

                standing++;
                if (weakest ? chosen == null || enemy.Hp < chosen.Hp : _rolls.Next(standing) == 0)
                {
                    chosen = enemy;
                }
            }

            return chosen;
        }
    }
}
