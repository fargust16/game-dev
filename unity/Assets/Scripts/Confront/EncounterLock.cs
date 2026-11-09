using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Confront.Turns;
using GameDev.World.Timekeeping;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Confront
{
    /// <summary>Who fights, round which place, and with which rolls: one encounter's start (17 C.1).</summary>
    public sealed class EncounterRequest
    {
        public EncounterRequest(Vector3 centre, IReadOnlyList<Combatant> combatants, Random rolls, string reason)
        {
            Centre = centre;
            Combatants = combatants;
            Rolls = rolls;
            Reason = reason;
        }

        /// <summary>Where the grid forms: the player's place.</summary>
        public Vector3 Centre { get; }

        public IReadOnlyList<Combatant> Combatants { get; }

        /// <summary>The fight's own fork of the seed (the slice forks `slice.combat` with the fight's number).</summary>
        public Random Rolls { get; }

        /// <summary>What started it, for the log: an ambush, a refused Final Check, a development command.</summary>
        public string Reason { get; }
    }

    /// <summary>
    /// Starts Confront from hostility (17 C.1). There is no voluntary Turn-Based Mode in free exploration (10.2,
    /// AGENTS), so a request comes only from something in the world turning hostile, or from a development command.
    /// </summary>
    public interface IEncounterLock
    {
        bool InConfront { get; }

        bool TryBegin(EncounterRequest request);

        event Action<EncounterRequest> ConfrontStarted;

        /// <summary>Raised when the fight ends, with the side left standing or −1; free movement returns.</summary>
        event Action<int> ConfrontEnded;
    }

    /// <summary>
    /// The encounter's frame (S3a.4): hostility stops the world, holding the clock, the grid forms round the centre, and
    /// the turns begin; one world tick passes after each round, the clock moving by whole ticks while it is held
    /// (17 X.1), never during a turn; when no hostile side is left the clock runs again.
    /// </summary>
    public sealed class EncounterLock : IEncounterLock
    {
        private readonly ITurnEconomy _turns;
        private readonly ICombatGrid _grid;
        private readonly IWorldClock _clock;
        private IDisposable _pause;

        public EncounterLock(ITurnEconomy turns, ICombatGrid grid, IWorldClock clock)
        {
            _turns = turns;
            _grid = grid;
            _clock = clock;
            _turns.RoundEnded += _ => _clock.AdvanceTicks(1);
            _turns.Ended += OnEnded;
        }

        public bool InConfront { get; private set; }

        public event Action<EncounterRequest> ConfrontStarted;

        public event Action<int> ConfrontEnded;

        public bool TryBegin(EncounterRequest request)
        {
            if (InConfront || request == null)
            {
                return false;
            }

            _grid.Follow(request.Centre);
            if (!_grid.TryCellAt(request.Centre, out _))
            {
                return false;
            }

            InConfront = true;
            _pause = _clock.Pause();
            ConfrontStarted?.Invoke(request);
            _turns.BeginEncounter(request.Combatants, request.Rolls);
            return true;
        }

        private void OnEnded(int side)
        {
            _pause?.Dispose();
            _pause = null;
            InConfront = false;
            ConfrontEnded?.Invoke(side);
        }
    }
}
