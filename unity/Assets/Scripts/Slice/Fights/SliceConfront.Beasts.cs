using System;
using System.Collections.Generic;
using GameDev.Confront;
using GameDev.Confront.Turns;
using GameDev.Locomotion;
using GameDev.Slice.Beasts;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// S3b.2: a fight with beasts (10.2 *Beasts of the slice*). The beasts fight in their own bodies, by 10.2's table, as
    /// beasts: they keep to their territory and go back to it when the character leaves it, a pack breaks when its first
    /// wolf falls, and none of them takes a Yield. Their clips play as the beats come: the strike as a blow begins, the
    /// hit or the fall as it lands, and the walk and run from how they move.
    /// </summary>
    public sealed partial class SliceConfront
    {
        private const float AttackSeconds = 0.9f;
        private const float HitSeconds = 0.6f;

        private readonly List<SliceBeast> _beastsInFight = new List<SliceBeast>();

        /// <summary>A beast fight has ended and its last beat played: the beasts that fought, their combatants still set.</summary>
        public event Action<IReadOnlyList<SliceBeast>> BeastFightEnded;

        /// <summary>The pack, or the beast alone, attacks the character.</summary>
        public bool StartBeastFight(IReadOnlyList<SliceBeast> beasts, BeastRules rules)
        {
            _player = FindAnyObjectByType<PlayerLocomotion>();
            if (_lock == null || InFight || _died || _terms != null || _player == null || beasts == null || beasts.Count == 0 || rules == null)
            {
                return false;
            }

            var at = _player.transform.position;
            _grid.Follow(at);
            if (!_grid.TryCellAt(at, out _))
            {
                return false;
            }

            _fights++;
            _bodies.Clear();
            _names.Clear();
            _groups.Clear();
            _beastsInFight.Clear();
            var player = new Combatant(PlayerId, PlayerSide, PlayerFighter(), at, takesIntents: !AutoPlayer);
            Equip(player);
            var combatants = new List<Combatant> { player };
            _bodies[player] = _player.transform;
            _names[player] = "ui.fight.you";
            _playerCombatant = player;
            for (var i = 0; i < beasts.Count; i++)
            {
                var b = beasts[i];
                var fighter = rules.Fighter(_settings.FightRules, b.Kind, b.Glowing);
                if (!double.IsNaN(b.Hp))
                {
                    fighter.Hp = Math.Min(b.Hp, fighter.MaxHp);
                }

                var c = new Combatant(i + 1, HostileSide, fighter, b.transform.position)
                {
                    Beast = true,
                    Pack = b.Glowing ? -1 : b.Pack, // a glowing beast walks alone
                    Home = b.Home,
                    Range = rules.TerritoryRadius,
                };
                b.Combatant = c;
                _bodies[c] = b.transform;
                _names[c] = rules.Of(b.Kind, b.Glowing).nameKey;
                _beastsInFight.Add(b);
                combatants.Add(c);
            }

            var request = new EncounterRequest(at, combatants, _seed.Fork(SliceSeedStreams.Combat, _fights), "beasts");
            if (Hiding != null && Hiding.Active)
            {
                player.Hidden = true;
                request.Opener = player;
                request.Target = Nearest(player, combatants);
            }

            if (_lock.TryBegin(request))
            {
                return true;
            }

            foreach (var b in _beastsInFight)
            {
                b.Combatant = null;
            }

            _beastsInFight.Clear();
            _playerCombatant = null;
            return false;
        }

        /// <summary>A beast's strike clip as its blow begins.</summary>
        private void OnBeatStarted(FightBeats.Beat beat)
        {
            if (beat.Points == null && beat.Body != null && beat.Body.TryGetComponent<SliceBeast>(out var beast))
            {
                beast.Act("Attack", AttackSeconds);
            }
        }

        /// <summary>A beast's hit or fall as a blow lands on it.</summary>
        private void BeastStruck(FightBeats.Beat beat)
        {
            if (beat.Target == null || !beat.Target.TryGetComponent<SliceBeast>(out var beast))
            {
                return;
            }

            if (beat.Result.Downed)
            {
                beast.Die(false);
            }
            else if (beat.Result.Damage > 0d)
            {
                beast.Act("Hit", HitSeconds);
            }
        }

        private static bool IsBeast(Transform body) => body != null && body.GetComponent<SliceBeast>() != null;

        /// <summary>The fight's last beat has played: the beasts' host learns how each left it.</summary>
        private void BeastsAfterFight()
        {
            if (_beastsInFight.Count == 0)
            {
                return;
            }

            var fought = _beastsInFight.ToArray();
            _beastsInFight.Clear();
            BeastFightEnded?.Invoke(fought);
        }
    }
}
