using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;

namespace GameDev.Slice.Shops
{
    /// <summary>How a piece of work at a workshop ended.</summary>
    public enum CraftOutcome
    {
        Refused,
        Interrupted,
        Made,
    }

    /// <summary>
    /// Item Creation at a Master's workshop (S5a.4; 19 §3). One who learned it from Edda Brann makes a piece of any base
    /// with a recipe: at stage 9 from the recipe's resources, at stage 8 from half as many again, each rounded up, and one
    /// rare ingredient. The work takes 10 or 20 ticks, and the world runs through them tick by tick; a fight that starts
    /// in them interrupts the work, and nothing is spent. A finished piece takes its resources and the workshop's fee, and
    /// its modifiers roll at the character's Item Creation, Perception and Luck, by the seed and the count of pieces made.
    /// The piece is always made: only its modifiers are random.
    /// </summary>
    public sealed class SliceWorkshop
    {
        public const string Lesson = "itemcreation";

        /// <summary>World state: how many pieces the character has made, which keys each piece's rolls.</summary>
        public const string MadeKey = "player.workshop.made";

        private readonly SliceShops _shops;
        private readonly ItemCatalogue _items;
        private readonly IWorldSeed _seed;
        private readonly IWorldClock _clock;
        private readonly Bag _bag;
        private readonly IWorldState _state;
        private readonly ICharacterValues _character;

        public SliceWorkshop(SliceShops shops, ItemCatalogue items, IWorldSeed seed, IWorldClock clock, Bag bag, IWorldState state,
            ICharacterValues character)
        {
            _shops = shops;
            _items = items;
            _seed = seed;
            _clock = clock;
            _bag = bag;
            _state = state;
            _character = character;
        }

        /// <summary>A piece made.</summary>
        public event Action<ItemPiece> Made;

        public ShopCatalogue Rules => _shops.Rules;

        public bool Knows => _shops.Knows(Lesson);

        /// <summary>Every base a workshop makes, in the catalogue's order.</summary>
        public IEnumerable<ItemDefinition> Bases =>
            _items.Items.Where(i => i.kind == ItemKind.Equipment && i.recipe != null && i.recipe.Length > 0);

        public bool Has(string keeper) => _shops.Find(keeper)?.workshop == true;

        public int Ticks(int stage) => stage <= 8 ? Rules.StageEightTicks : Rules.StageNineTicks;

        /// <summary>19 §3: a stage 9 piece's recipe, or a stage 8 piece's: half as many again, rounded up, and the rare ingredient.</summary>
        public IReadOnlyList<RecipePart> Recipe(ItemDefinition item, int stage)
        {
            if (stage >= 9)
            {
                return item.recipe;
            }

            var parts = item.recipe.Select(p => new RecipePart(p.item, (int)Math.Ceiling(p.count * 1.5d))).ToList();
            parts.Add(new RecipePart(Rules.RareIngredient, 1));
            return parts;
        }

        /// <summary>The key of the reason a piece cannot be begun, or null.</summary>
        public string Fault(string keeper, ItemDefinition item, int stage)
        {
            if (!_shops.IsOpen)
            {
                return "ui.shop.fault.closed";
            }

            if (!Has(keeper) || item == null || item.recipe == null || item.recipe.Length == 0 || stage < 8 || stage > 9)
            {
                return "ui.shop.fault.gone";
            }

            if (!Knows)
            {
                return "ui.craft.fault.lesson";
            }

            if (_bag.Bronze < Rules.WorkshopFee)
            {
                return "ui.shop.fault.money";
            }

            var recipe = Recipe(item, stage);
            if (recipe.Any(p => _bag.Count(p.item) < p.count))
            {
                return "ui.craft.fault.resources";
            }

            var change = item.weight - recipe.Sum(p => (_items.Find(p.item)?.weight ?? 0f) * p.count);
            return change > 0f && !_bag.CanCarry(change) ? "ui.shop.fault.heavy" : null;
        }

        /// <summary>
        /// Works the piece through its ticks. <paramref name="interrupted"/> is asked after each tick; when it answers
        /// yes, the work stops with nothing spent. A piece begun before closing is finished after it.
        /// </summary>
        public CraftOutcome Make(string keeper, ItemDefinition item, int stage, Func<bool> interrupted, out ItemPiece piece)
        {
            piece = null;
            if (Fault(keeper, item, stage) != null)
            {
                return CraftOutcome.Refused;
            }

            for (var t = 0; t < Ticks(stage); t++)
            {
                _clock.AdvanceTicks(1);
                if (interrupted != null && interrupted())
                {
                    return CraftOutcome.Interrupted;
                }
            }

            var recipe = Recipe(item, stage);
            if (recipe.Any(p => _bag.Count(p.item) < p.count) || _bag.Bronze < Rules.WorkshopFee)
            {
                return CraftOutcome.Interrupted; // the world took something during the work
            }

            foreach (var part in recipe)
            {
                _bag.Remove(part.item, part.count);
            }

            _bag.Spend(Rules.WorkshopFee);
            var made = _state.Get(MadeKey);
            _state.Set(MadeKey, made + 1);
            var chance = ItemMaker.Chance(_character.Get(CharacterSkill.ItemCreation), _character.Get(CharacterAttribute.Perception),
                _character.Get(CharacterAttribute.Luck));
            piece = ItemMaker.Make(item, stage, chance, _seed.Fork(SliceSeedStreams.Workshop, made, 0));
            _bag.AddPiece(piece);
            Made?.Invoke(piece);
            return CraftOutcome.Made;
        }
    }
}
