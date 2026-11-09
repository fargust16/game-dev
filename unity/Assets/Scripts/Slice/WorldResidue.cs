using System;
using System.Linq;
using GameDev.World.Portals;
using GameDev.World.State;
using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>One reading of the world state: the value under <see cref="key"/> lies between its bounds.</summary>
    [Serializable]
    public sealed class StateCondition
    {
        public string key;
        public int atLeast = 1;
        public int atMost = int.MaxValue;

        public StateCondition()
        {
        }

        public StateCondition(string key, int atLeast = 1, int atMost = int.MaxValue)
        {
            this.key = key;
            this.atLeast = atLeast;
            this.atMost = atMost;
        }

        public bool Holds(IWorldState state)
        {
            var value = state.Get(key);
            return value >= atLeast && value <= atMost;
        }
    }

    /// <summary>
    /// What the world shows because of what happened (11 *World residue*; 11.1 §6; S2.7): once any of its conditions holds
    /// in the world state, and not before its tick, it shows its objects, hides others, and seals its doors; when none
    /// holds any more it puts them back. A shop sealed with wax and brass, Warden pairs on the street the next day, the
    /// lane's women kept at home, a man out of his shop while the Wardens hold him in the street.
    /// </summary>
    public sealed class WorldResidue : MonoBehaviour
    {
        [Tooltip("It shows when any of these holds.")]
        [SerializeField] private StateCondition[] anyOf = new StateCondition[0];

        [Tooltip("Not before this tick, minutes from day 1's midnight: 1920 is day 2, 08:00, the next morning in the slice.")]
        [SerializeField] private long fromTick;

        [SerializeField] private GameObject[] shows = new GameObject[0];
        [SerializeField] private GameObject[] hides = new GameObject[0];
        [SerializeField] private Portal[] seals = new Portal[0];

        private bool _applied;

        /// <summary>Whether the residue stands now.</summary>
        public bool IsShowing { get; private set; }

        public long FromTick => fromTick;

        public GameObject[] Shows => shows;

        public GameObject[] Hides => hides;

        /// <summary>Setup for the editor command that places it; a placed residue is set in the Inspector.</summary>
        public void Configure(StateCondition[] conditions, long notBefore, GameObject[] shown, GameObject[] hidden, Portal[] sealedDoors)
        {
            anyOf = conditions;
            fromTick = notBefore;
            shows = shown;
            hides = hidden;
            seals = sealedDoors;
        }

        private void Update()
        {
            var world = SliceWorldDriver.Running;
            if (world == null)
            {
                return;
            }

            var on = world.Clock.Tick >= fromTick && anyOf.Any(c => c.Holds(world.State));
            if (_applied && on == IsShowing)
            {
                return;
            }

            _applied = true;
            IsShowing = on;
            foreach (var shown in shows.Where(o => o != null))
            {
                shown.SetActive(on);
            }

            foreach (var hidden in hides.Where(o => o != null))
            {
                hidden.SetActive(!on);
            }

            foreach (var door in seals.Where(d => d != null))
            {
                door.Sealed = on;
            }
        }
    }
}
