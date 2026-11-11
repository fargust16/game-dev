using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.Slice.Shops
{
    /// <summary>One shop of the slice (11 *Shops and Masters*, 19 §6.1–6.4): its keeper, what it sells and buys, and what it teaches.</summary>
    [Serializable]
    public sealed class ShopEntry
    {
        [Tooltip("The keeper's id among the named people (NamedPerson.Id).")]
        public string keeper;

        [Tooltip("The items it sells and buys back, by id.")]
        public string[] sells = new string[0];

        [Tooltip("Also every equipment base the catalogue prices, one piece of each, made each morning at the Shop Skill.")]
        public bool equipment;

        [Tooltip("The professions it teaches, by lesson id: firstaid, itemcreation, skinning.")]
        public string[] lessons = new string[0];

        [Tooltip("Its keeper's workshop, where one who learned Item Creation makes a piece (19 §3; S5a.4).")]
        public bool workshop;
    }

    /// <summary>
    /// The slice's shops as data (S5a.3), with 19 §6's numbers: the Shop Skill of the first day and its rise a day, the
    /// hours, the lesson's price, and how many of each good a shelf holds after the morning. Authoring defaults.
    /// </summary>
    [CreateAssetMenu(menuName = "GameDev/Slice/Shops", fileName = "Shops")]
    public sealed class ShopCatalogue : ScriptableObject
    {
        [SerializeField] private ShopEntry[] shops = new ShopEntry[0];

        [Tooltip("19 §6.1: a city shop's Shop Skill on the first day.")]
        [SerializeField] private int shopSkill = 26;

        [Tooltip("19 §6.1 World Age: what the Shop Skill gains a game day.")]
        [SerializeField] private int skillPerDay = 1;

        [Tooltip("The hour a shop opens and fills its shelf again (19 §6.3 Stock), and the hour it closes.")]
        [SerializeField] private int opens = 8;

        [SerializeField] private int closes = 20;

        [Tooltip("19 §6.3: a lesson from a Master, bronze.")]
        [SerializeField] private int lessonPrice = 1000;

        [Tooltip("19 §6.3 Stock: a shelf holds one to three of each good after the morning.")]
        [SerializeField] private int leastStock = 1;

        [SerializeField] private int mostStock = 3;

        [Tooltip("19 §3: what the workshop asks a piece, bronze.")]
        [SerializeField] private int workshopFee = 100;

        [Tooltip("19 §3: the ticks a stage 9 piece takes, and a stage 8 piece.")]
        [SerializeField] private int stageNineTicks = 10;

        [SerializeField] private int stageEightTicks = 20;

        [Tooltip("19 §3, §2.1: the rare ingredient a stage 8 piece needs besides its resources.")]
        [SerializeField] private string rareIngredient = "glow_essence_8";

        public IReadOnlyList<ShopEntry> Shops => shops;
        public int ShopSkill => shopSkill;
        public int SkillPerDay => skillPerDay;
        public int Opens => opens;
        public int Closes => closes;
        public int LessonPrice => lessonPrice;
        public int LeastStock => leastStock;
        public int MostStock => mostStock;
        public int WorkshopFee => workshopFee;
        public int StageNineTicks => stageNineTicks;
        public int StageEightTicks => stageEightTicks;
        public string RareIngredient => rareIngredient;
    }
}
