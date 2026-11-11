using System;
using System.Collections.Generic;
using GameDev.Confront;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// S3b.3: a fight with people who stand in the scene, a POI's Wardens or collectors, in their own bodies. They fight
    /// as the development opponents of their group do (S3a.9), take a Yield when their group does, and may ask for mercy.
    /// By the owner's answer of 2026-11-09 one in three of them, counted from the last, carries a pistol with its
    /// Cartridge Pouch and keeps its melee weapon as the second Weapon Set.
    /// When the last beat has played, the host learns whether the player's side stood and how many were spared; those
    /// who left the fight alive leave the scene, and the fallen lie where they fell until the scene is cleared.
    /// </summary>
    public sealed partial class SliceConfront
    {
        private readonly List<Transform> _peopleInFight = new List<Transform>();
        private Action<bool, int> _peopleEnded;
        private int _sideStanding = -1;

        /// <summary>
        /// The people in <paramref name="bodies"/>, of <paramref name="group"/>, fight the character where they stand.
        /// <paramref name="ended"/> hears, after the last beat, whether the player's side stood and how many were spared.
        /// </summary>
        public bool StartPeopleFight(string group, IReadOnlyList<Transform> bodies, Action<bool, int> ended)
        {
            _player = FindAnyObjectByType<PlayerLocomotion>();
            if (_lock == null || InFight || _died || _terms != null || _player == null || bodies == null || bodies.Count == 0)
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
            _peopleInFight.Clear();
            var rules = _settings.FightRules;
            var player = new Combatant(PlayerId, PlayerSide, PlayerFighter(), at, takesIntents: !AutoPlayer);
            Equip(player);
            var combatants = new List<Combatant> { player };
            _bodies[player] = _player.transform;
            _names[player] = "ui.fight.you";
            _playerCombatant = player;
            var weapon = group == Wardens ? _settings.KitSword : _settings.OpponentWeapon;
            for (var i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                var shooter = _settings.PeoplePistol != null && _settings.KitPouch != null && (bodies.Count - 1 - i) % 3 == 0;
                var fighterRules = shooter
                    ? rules.Fighter(new CharacterSheet(), null, new WeaponSet(_settings.PeoplePistol, _settings.KitPouch), second: new WeaponSet(weapon))
                    : rules.Fighter(new CharacterSheet(), null, new WeaponSet(weapon));
                var opponent = new Combatant(i + 1, HostileSide, fighterRules, body.position)
                {
                    AcceptsYield = TakesYield(group),
                };
                if (!body.TryGetComponent<SliceFighter>(out var fighter))
                {
                    fighter = body.gameObject.AddComponent<SliceFighter>();
                }

                fighter.Combatant = opponent;
                _bodies[opponent] = body;
                _names[opponent] = group == Wardens ? "ui.fight.warden" : "ui.fight.veinmarket";
                _groups[opponent] = group;
                _peopleInFight.Add(body);
                combatants.Add(opponent);
            }

            // 10.3 §7: from Stealth Mode the character enters the fight hidden, and strikes first if one is in reach.
            var request = new EncounterRequest(at, combatants, _seed.Fork(SliceSeedStreams.Combat, _fights), "people: " + group);
            if (Hiding != null && Hiding.Active)
            {
                player.Hidden = true;
                request.Opener = player;
                request.Target = Nearest(player, combatants);
            }

            if (_lock.TryBegin(request))
            {
                _peopleEnded = ended;
                return true;
            }

            Unarm();
            _playerCombatant = null;
            return false;
        }

        /// <summary>The fight's last beat has played: the host hears how it went, and who left alive leaves the scene.</summary>
        private void PeopleAfterFight()
        {
            if (_peopleEnded == null)
            {
                return;
            }

            var ended = _peopleEnded;
            _peopleEnded = null;
            var won = _playerCombatant != null && _sideStanding == _playerCombatant.Side;
            var spared = 0;
            foreach (var pair in _bodies)
            {
                var body = pair.Value;
                if (body == null || pair.Key == _playerCombatant || !_peopleInFight.Contains(body))
                {
                    continue;
                }

                if (pair.Key.Departure == Departure.Spared)
                {
                    spared++;
                }

                if (!pair.Key.Fighter.Down && pair.Key.Departure != Departure.None)
                {
                    body.gameObject.SetActive(false);
                }
            }

            Unarm();
            ended(won, spared);
        }

        // The scene's people fight no more once their fight is over: nothing joins a later fight from them.
        private void Unarm()
        {
            foreach (var body in _peopleInFight)
            {
                if (body != null && body.TryGetComponent<SliceFighter>(out var fighter))
                {
                    Destroy(fighter);
                }
            }

            _peopleInFight.Clear();
        }
    }
}
