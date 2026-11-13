using System;
using System.Collections.Generic;

namespace GameDev.Items
{
    /// <summary>19 §2.2: quality follows the modifier count; Unique is authored, never counted.</summary>
    public enum ItemQuality
    {
        Normal,
        Magic,
        Rare,
        Epic,
        Legendary,
    }

    /// <summary>One modifier on a piece (19 §2.2, §2.4): its type, its group, its tier (9 weakest) and its rolled value.</summary>
    [Serializable]
    public sealed class ItemModifier
    {
        public string type;
        public bool major;
        public int tier = 9;
        public float value;
    }

    /// <summary>
    /// One piece of equipment, its base, stage and modifiers, or one Limit potion with what its brewing gave (S5b.4). Pieces
    /// are carried one by one, never stacked.
    /// </summary>
    [Serializable]
    public sealed class ItemPiece
    {
        public string item;
        public int stage = 9;
        public List<ItemModifier> modifiers = new List<ItemModifier>();

        /// <summary>A potion's perfect results of its six brewing checks (20 §3); 0 for equipment.</summary>
        public int perfect;

        /// <summary>A potion's bad results of its six brewing checks (20 §3); 0 for equipment.</summary>
        public int bad;

        /// <summary>Made at an anvil by the character (S5c.7): a Master's order for a piece of one's own making wants such a one.</summary>
        public bool made;

        public ItemPiece(string itemId, int itemStage = 9)
        {
            item = itemId;
            stage = itemStage;
        }

        public ItemQuality Quality => QualityOf(modifiers.Count);

        /// <summary>Normal 0, Magic 1–2, Rare 3, Epic 4, Legendary 5–6 (19 §2.2).</summary>
        public static ItemQuality QualityOf(int count) => count switch
        {
            <= 0 => ItemQuality.Normal,
            <= 2 => ItemQuality.Magic,
            3 => ItemQuality.Rare,
            4 => ItemQuality.Epic,
            _ => ItemQuality.Legendary,
        };

        /// <summary>A shop's price by quality, in bronze (19 §6.3), whatever the base.</summary>
        public static int PriceOf(ItemQuality quality) => quality switch
        {
            ItemQuality.Normal => 500,
            ItemQuality.Magic => 1500,
            ItemQuality.Rare => 4000,
            ItemQuality.Epic => 10000,
            _ => 25000,
        };

        public int Price => PriceOf(Quality);
    }
}
