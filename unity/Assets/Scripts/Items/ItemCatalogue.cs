using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameDev.Items
{
    /// <summary>What an item is for (19 §4.1): stacked goods, or a piece of equipment carried one by one.</summary>
    public enum ItemKind
    {
        Resource,
        Ingredient,
        Tool,
        Supply,
        Equipment,
    }

    /// <summary>Where a piece of equipment goes (19 §4.2). A hand item goes in either hand, a two-handed one in both.</summary>
    public enum EquipSlot
    {
        None,
        OneHand,
        TwoHands,
        Head,
        Body,
        Shoulders,
        Gloves,
        Belt,
        Trousers,
        Boots,
        Ring,
        Necklace,
    }

    /// <summary>One kind of item, as 19 gives it: its weight in kg and its shop price in bronze (§4.1, §6.3).</summary>
    [Serializable]
    public sealed class ItemDefinition
    {
        public string id;
        public ItemKind kind;

        [Tooltip("kg, 19 §4.1")]
        public float weight;

        [Tooltip("bronze, 19 §6.3; for equipment, the Normal price")]
        public int price;

        public EquipSlot slot;

        [Tooltip("The item stage, 9 lowest (19 §2.1).")]
        public int stage = 9;

        [Tooltip("Armour's family: cloth, leather, chain or plate; empty for anything else.")]
        public string family;

        public string NameKey => $"item.{id}";

        public bool Stacks => kind != ItemKind.Equipment;
    }

    /// <summary>Every item of the slice as data (S5a.1), found by id.</summary>
    [CreateAssetMenu(menuName = "GameDev/Items/Item Catalogue", fileName = "Items")]
    public sealed class ItemCatalogue : ScriptableObject
    {
        [SerializeField] private ItemDefinition[] items = new ItemDefinition[0];

        private Dictionary<string, ItemDefinition> _byId;

        public IReadOnlyList<ItemDefinition> Items => items;

        public ItemDefinition Find(string id)
        {
            if (_byId == null || _byId.Count != items.Length)
            {
                _byId = items.Where(i => i != null && !string.IsNullOrEmpty(i.id)).GroupBy(i => i.id).ToDictionary(g => g.Key, g => g.First());
            }

            return id != null && _byId.TryGetValue(id, out var item) ? item : null;
        }

        /// <summary>What is wrong with the catalogue: missing or repeated ids, weights or prices below zero, equipment with no slot.</summary>
        public IReadOnlyList<string> Problems()
        {
            var problems = new List<string>();
            foreach (var group in items.GroupBy(i => i?.id).Where(g => g.Count() > 1 || string.IsNullOrEmpty(g.Key)))
            {
                problems.Add($"id '{group.Key}' is missing or repeated");
            }

            foreach (var item in items.Where(i => i != null))
            {
                if (item.weight < 0f || item.price < 0)
                {
                    problems.Add($"{item.id}: weight or price below zero");
                }

                if ((item.kind == ItemKind.Equipment) != (item.slot != EquipSlot.None))
                {
                    problems.Add($"{item.id}: equipment, and only equipment, has a slot");
                }
            }

            return problems;
        }
    }
}
