using System.Collections.Generic;
using GameDev.Confront;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Player.Progression;
using GameDev.Slice.Alchemy;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// A fight's Conduct Progress (S5b.7; 05.1 *Conduct Progress in the slice*). For Император: a fight won against a side
    /// at least as strong by the Force Estimate at its start; a deeper Limit or a glowing beast beaten in a won fight; a
    /// won fight in which a way out was open, Сбежать at any of the player's turns or a Yield its enemies would take; and
    /// a stealth attack, Break Away or a Yield against it. For Жрица: a fight the character started when a way without one
    /// was open, an Assault or a stealth attack, against it. A fight a conversation's POI settles if won makes its first
    /// deed worth 2.
    /// </summary>
    public sealed partial class SliceConfront
    {
        /// <summary>Authoring default, m: fights in one square of this side are one place for a deed.</summary>
        private const float ConductPlace = 25f;

        private double _conductEstimate;
        private bool _conductWayOut;
        private bool _conductSettles;

        /// <summary>The Force Estimate of the player's side at the last fight's start (for tests).</summary>
        public double LastFightEstimate => _conductEstimate;

        /// <summary>The character's Conduct Progress; set by the world.</summary>
        public SliceConduct Conduct { get; set; }

        /// <summary>Set just before a fight whose win settles a POI's Decision Node (a conversation's); read when it begins.</summary>
        public bool NextFightSettles { get; set; }

        /// <summary>Begins the fight, reading what its start means for Conduct Progress.</summary>
        /// <param name="assault">A blow at someone not fighting opened it (10.2 §5c).</param>
        private bool Begin(EncounterRequest request, bool assault = false)
        {
            var stealth = request.Opener != null && request.Opener.Id == PlayerId && request.Opener.Hidden;
            var settles = NextFightSettles;
            NextFightSettles = false;
            if (!_lock.TryBegin(request))
            {
                return false;
            }

            OutsideFightBegins(); // S5b.8: Двойник's veil falls, and those held whom it takes wake
            _conductSettles = settles;
            _conductWayOut = false;
            var ours = new List<Fighter>();
            var theirs = new List<Fighter>();
            foreach (var c in request.Combatants)
            {
                (c.Side == PlayerSide ? ours : theirs).Add(c.Fighter);
                _conductWayOut |= c.Side != PlayerSide && c.AcceptsYield; // a Yield they would take is a way out
            }

            _conductEstimate = _settings.FightRules.ForceEstimate(ours, theirs);
            if (Conduct != null && (assault || stealth))
            {
                Conduct.Against(Arcana.Priestess); // a fight started when a way without one was open
            }

            if (Conduct != null && stealth)
            {
                Conduct.Against(Arcana.Emperor);
            }

            return true;
        }

        /// <summary>Each frame of a fight: Сбежать open at the player's turn is a way out it did not take.</summary>
        private void WatchConduct()
        {
            if (!_conductWayOut && _playerCombatant != null && _turns.CanBreakAway(_playerCombatant))
            {
                _conductWayOut = true;
            }
        }

        /// <summary>The player left the fight: Break Away and a Yield are against Император.</summary>
        private void ConductDeparted(Combatant c)
        {
            if (c == _playerCombatant && (c.Departure == Departure.BrokeAway || c.Departure == Departure.Yielded))
            {
                Conduct?.Against(Arcana.Emperor);
            }
        }

        /// <summary>The fight ended with <paramref name="side"/> standing: Император's deeds of a fight won.</summary>
        private void ConductEnded(int side)
        {
            if (Conduct == null || _playerCombatant == null || side != PlayerSide || _brokeAway || _yielded || _subdued
                || _playerCombatant.Fighter.Down || _player == null)
            {
                return;
            }

            var at = _player.transform.position;
            var place = $"fight.{Mathf.FloorToInt(at.x / ConductPlace)}.{Mathf.FloorToInt(at.z / ConductPlace)}";
            var settles = _conductSettles;
            if (_conductEstimate <= 1d && Conduct.Deed(ConductDeed.StrongerSide, place, settles))
            {
                settles = false;
            }

            if (_conductWayOut && Conduct.Deed(ConductDeed.Stand, place, settles))
            {
                settles = false;
            }

            foreach (var c in _turns.Order)
            {
                if (c.Side != PlayerSide && Beaten(c) && Stronger(c) && Conduct.Deed(ConductDeed.StrongerFoe, Foe(c), settles))
                {
                    settles = false;
                }
            }
        }

        private static bool Beaten(Combatant c) => c.Fighter.Down || c.Departure == Departure.Yielded || c.Departure == Departure.Spared;

        /// <summary>A deeper Limit than the character's, or a glowing beast.</summary>
        private bool Stronger(Combatant c) => Glowing(c) || (c.Fighter.Limit is int limit && PlayerLimit is int own && limit < own);

        /// <summary>Who a beaten enemy was: a beast by its home, a person by its name.</summary>
        private string Foe(Combatant c)
        {
            if (c.Beast)
            {
                return $"beast.{Mathf.RoundToInt(c.Home.x)}.{Mathf.RoundToInt(c.Home.z)}";
            }

            return _bodies.TryGetValue(c, out var body) && body != null ? "person." + body.name : "person." + c.Id;
        }
    }
}
