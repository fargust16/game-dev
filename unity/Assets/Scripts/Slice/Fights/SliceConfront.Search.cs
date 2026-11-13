using System;
using System.Collections.Generic;
using GameDev.Confront;
using GameDev.Confront.Turns;
using GameDev.Locomotion;
using GameDev.Slice.People;
using GameDev.World.Seed;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// 10.2 §9, the search (owner, 2026-11-12; PB5b). When the player breaks away, each enemy standing that had chosen
    /// the character as its target and saw it runs to the place where it last saw it, looks round there, and goes back
    /// to where it stood when the fight began (<see cref="FightSearch"/>); the rest go back at once, as before. If a
    /// searcher sees the character, the fight starts again in place with every searcher still about, its HP as it
    /// left, and everyone within 30 cells joins as ever. Beasts keep to their own host (S3b.2), which sends them home
    /// and whose watch starts a new fight; a resident searches one time in ten, by the seed from who it is (§5a). A
    /// people fight's host hears how it ended only when the search is over, or from the fight it led back into.
    /// </summary>
    public sealed partial class SliceConfront
    {
        /// <summary>Authoring default, 10.2 §5a: one resident in ten searches.</summary>
        public const int ResidentSearchOneIn = 10;

        private readonly Dictionary<Combatant, (Vector3 At, Quaternion Facing)> _starts = new Dictionary<Combatant, (Vector3, Quaternion)>();
        private readonly List<(Combatant Combatant, Transform Body)> _toSearch = new List<(Combatant, Transform)>();
        private readonly List<FightSearch> _searching = new List<FightSearch>();
        private Action<bool, int> _searchEnded;
        private int _searchSpared;

        /// <summary>The enemies searching for the character after it broke away.</summary>
        public IReadOnlyList<FightSearch> Searching => _searching;

        /// <summary>Whether a resident searches for a character who broke away: one in ten, by the seed from who it is.</summary>
        public bool ResidentSearches(Transform body) =>
            _seed != null && body != null && _seed.Fork(SliceSeedStreams.Search, StableHash.Of(Who(body))).Next(ResidentSearchOneIn) == 0;

        /// <summary>Where each body stood when it came into the fight, the place it goes back to after a search.</summary>
        private void RecordStart(Combatant c, Transform body)
        {
            if (c != _playerCombatant && body != null && !_starts.ContainsKey(c))
            {
                _starts[c] = (body.position, body.rotation);
            }
        }

        /// <summary>At the player's break away, before the fight stops: who will search, and where.</summary>
        private void ChooseSearchers(Combatant player)
        {
            _toSearch.Clear();
            foreach (var c in _turns.Order)
            {
                if (c == player || !c.Standing || c.Side == player.Side || c.AsksMercy || c.Fighter.LostTurns > 0
                    || c.Chosen != player || c.SawWho != player || !c.SawAt.HasValue
                    || !_bodies.TryGetValue(c, out var body) || body == null || IsBeast(body)
                    || (Group(c) == Residents && !ResidentSearches(body)))
                {
                    continue;
                }

                _toSearch.Add((c, body));
            }
        }

        /// <summary>After the fight's last beat: the searchers set out; a development opponent stays until its search is over.</summary>
        private void BeginSearch()
        {
            foreach (var (c, body) in _toSearch)
            {
                if (body == null || !body.gameObject.activeInHierarchy)
                {
                    continue;
                }

                // A Warden goes back to its post (10.2 §9); the search takes it there, not the Assault's return.
                var coming = _returning.Find(r => r.Body == body);
                var start = coming != null ? (coming.Post, coming.PostFacing) : _starts.TryGetValue(c, out var s) ? s : (body.position, body.rotation);
                if (coming != null)
                {
                    _returning.Remove(coming);
                }


                var development = _developmentOpponents.Remove(body.gameObject);
                var search = FightSearch.Begin(body, c.SawAt.Value, start.Item1, start.Item2, c.Fighter, Group(c), _names.TryGetValue(c, out var key) ? key : null,
                    development, _settings.StealthRules, _clock);
                _searching.Add(search);
            }

            _toSearch.Clear();
            _starts.Clear();
        }

        /// <summary>
        /// Out of a fight, each frame: a searcher that sees the character starts the fight again; one back where it stood
        /// ends its search, and once none is left the host of the fight it broke away from hears how it ended.
        /// </summary>
        private void WatchTheSearch()
        {
            if (_searching.Count == 0)
            {
                return;
            }

            _searching.RemoveAll(s => s == null);
            var player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            if (player != null && player.enabled && !_died && _terms == null && !player.InWater)
            {
                foreach (var s in _searching)
                {
                    if (s.Now == FightSearch.Phase.Done || !Sees(s, player.transform.position))
                    {
                        continue;
                    }

                    // One the character shot unseen starts an Assault (10.2 §5e); the list has changed either way.
                    if (s.Shot)
                    {
                        ShotSearcherSees(s);
                        return;
                    }

                    if (Resume())
                    {
                        return;
                    }
                }
            }

            for (var i = _searching.Count - 1; i >= 0; i--)
            {
                var s = _searching[i];
                if (s.Now != FightSearch.Phase.Done)
                {
                    continue;
                }

                _searching.RemoveAt(i);
                var body = s.gameObject;
                s.End(walkAgain: true);
                if (s.Development)
                {
                    Destroy(body);
                }
            }

            if (_searching.Count == 0)
            {
                var ended = _searchEnded;
                _searchEnded = null;
                ended?.Invoke(false, _searchSpared);
                _searchSpared = 0;
            }
        }

        /// <summary>
        /// 10.3 through a searcher's eyes, Alert: out of Stealth Mode the character in its cone and in sight is seen; in
        /// Stealth Mode its Notice Meter must fill first.
        /// </summary>
        private bool Sees(FightSearch s, Vector3 feet)
        {
            var eyes = s.Eyes;
            if (Hiding == null || eyes == null || eyes.Meter == null)
            {
                return false;
            }

            var crouching = Hiding.Active;
            if (!Hiding.Sees(eyes, feet, crouching))
            {
                eyes.Meter.Unseen(Time.deltaTime);
                return false;
            }

            if (!crouching)
            {
                return true;
            }

            if (eyes.Meter.See(Hiding.TimeToFill(eyes, feet), Time.deltaTime, feet))
            {
                Hiding.NoticedBy(eyes, Core.Localization.Loc.Get("ui.stealth.noticed.1"));
                return true;
            }

            return eyes.Meter.Noticed;
        }

        /// <summary>
        /// The fight starts again where the character stands, with every searcher still about, its HP as it left; the
        /// fight broken away from keeps its host, and the searchers' walks wait for its end.
        /// </summary>
        private bool Resume()
        {
            _player = FindAnyObjectByType<PlayerLocomotion>();
            if (_lock == null || InFight || _died || _terms != null || _player == null || _player.InWater)
            {
                return false;
            }

            var at = _player.transform.position;
            _grid.Follow(at);
            if (!_grid.TryCellAt(at, out _))
            {
                return false;
            }

            var kit = _searching.Exists(s => s != null && s.Development);
            _fights++;
            _bodies.Clear();
            _names.Clear();
            _groups.Clear();
            _peopleInFight.Clear();
            var player = new Combatant(PlayerId, PlayerSide, PlayerFighter(kit), at, takesIntents: !AutoPlayer);
            Equip(player, kit);
            var combatants = new List<Combatant> { player };
            _bodies[player] = _player.transform;
            _names[player] = "ui.fight.you";
            _playerCombatant = player;
            var id = 1;
            foreach (var s in _searching)
            {
                if (s == null || !s.gameObject.activeInHierarchy || s.Fighter.Down)
                {
                    continue;
                }

                var body = s.transform;
                var c = new Combatant(id++, HostileSide, s.Fighter, body.position) { AcceptsYield = TakesYield(s.Group) };
                if (!body.TryGetComponent<SliceFighter>(out var part))
                {
                    part = body.gameObject.AddComponent<SliceFighter>();
                }

                part.Combatant = c;
                _bodies[c] = body;
                if (s.NameKey != null)
                {
                    _names[c] = s.NameKey;
                }

                _groups[c] = s.Group;
                _starts[c] = (s.Post, s.PostFacing); // a later search goes back to the first post
                _peopleInFight.Add(body);
                if (s.Development)
                {
                    _developmentOpponents.Add(s.gameObject);
                }

                if (body.TryGetComponent<TownsfolkWalker>(out var walker))
                {
                    walker.enabled = false;
                    _stopped.Add(walker); // walks again when the fight is over
                }

                combatants.Add(c);
            }

            foreach (var s in _searching)
            {
                s?.End(walkAgain: false);
            }

            _searching.Clear();
            var request = new EncounterRequest(at, combatants, _seed.Fork(SliceSeedStreams.Combat, _fights), "search");
            if (!Begin(request))
            {
                _starts.Clear();
                Unarm();

                _playerCombatant = null;
                return false;
            }

            Hiding?.SetActive(false);
            _peopleEnded = _searchEnded ?? ((_, _) => WalkAgain());
            _searchEnded = null;
            return true;
        }
    }
}
