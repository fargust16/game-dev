using System;
using System.Collections.Generic;
using GameDev.Confront.Rules;
using UnityEngine;

namespace GameDev.Slice.Gear
{
    /// <summary>
    /// Each weapon's and shield's model as it is held (10.2 §5c), made by <c>SliceWeapons</c> from the asset map's §3
    /// models: a prefab whose origin is the middle of the fist's grip, its +Z leaving the fist on the index finger's side,
    /// its +Y the knuckles' way and its +X the right palm's side (<see cref="HeldWeapons"/>). A Cartridge Pouch and fists
    /// have none. Immutable data.
    /// </summary>
    [CreateAssetMenu(menuName = "GameDev/Slice/Weapon Models", fileName = "WeaponModels")]
    public sealed class WeaponModels : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public FightWeapon weapon;
            public FightOffHand offHand;
            public GameObject held;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public IReadOnlyList<Entry> Entries => entries;

        /// <summary>The weapon's held model, or null for fists, a beast's weapon or none.</summary>
        public GameObject For(FightWeapon weapon)
        {
            if (weapon == null)
            {
                return null;
            }

            foreach (var entry in entries)
            {
                if (entry.weapon == weapon)
                {
                    return entry.held;
                }
            }

            return null;
        }

        /// <summary>The shield's held model, or null for a Cartridge Pouch or none.</summary>
        public GameObject For(FightOffHand offHand)
        {
            if (offHand == null)
            {
                return null;
            }

            foreach (var entry in entries)
            {
                if (entry.offHand == offHand)
                {
                    return entry.held;
                }
            }

            return null;
        }
    }
}
