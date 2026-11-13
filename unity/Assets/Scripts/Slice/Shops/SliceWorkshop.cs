using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.Gathering;
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

        /// <summary>The work's ticks are done, and the piece waits for its Reaction Check.</summary>
        Worked,
        Made,

        /// <summary>The Reaction Check failed: no piece, half of each resource lost, the fee paid.</summary>
        Spoiled,
    }

    /// <summary>
    /// Item Creation at the anvil in a Master's yard (S5a.4; 19 §3; the anvil since the owner's play test of S5a). One who
    /// learned it from Edda Brann makes a piece of any base with a recipe: at stage 9 from the recipe's resources, at stage
    /// 8 from half as many again, each rounded up, and one rare ingredient. The work takes 10 or 20 ticks, and the world
    /// runs through them tick by tick, each spending 2 % of maximum Stamina; a fight that starts in them interrupts the work,
    /// and nothing is spent. Then the piece is one Reaction Check of 07.1 §8a on Dexterity and Item Creation against 2 at
    /// stage 9 and 4 at stage 8. A good result takes the resources and the fee and makes the piece, its modifiers rolled at
    /// the character's Item Creation, Perception and Luck by the seed and the count of pieces made; a bad one takes the fee
    /// and half of each resource, rounded up, and makes nothing.
    /// </summary>
    public sealed class SliceWorkshop
    {
        public const string Lesson = "itemcreation";

        /// <summary>Authoring default, 19 §3: a tick of work spends this share of maximum Stamina.</summary>
        public const double WorkStamina = 0.02d;

        /// <summary>Authoring default, 07.1 §8a: the Reaction Check's difficulty at stage 9, and at stage 8.</summary>
        public const int NineDifficulty = 2;
        public const int EightDifficulty = 4;

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

        /// <summary>The character's Stamina, which the work spends; null spends none.</summary>
        public SliceStamina Stamina { get; set; }

        public ShopCatalogue Rules => _shops.Rules;

        public bool Knows => _shops.Knows(Lesson);

        /// <summary>Every base a workshop makes, in the catalogue's order; no melee weapon or shield (10.2 §5d).</summary>
        public IEnumerable<ItemDefinition> Bases =>
            _items.Items.Where(i => i.kind == ItemKind.Equipment && i.recipe != null && i.recipe.Length > 0 && !SliceShops.Withheld(i));

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
            if (!_shops.IsOpenFor(keeper))
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
            if (change > 0f && !_bag.CanCarry(change))
            {
                return "ui.shop.fault.heavy";
            }

            return Stamina != null && Stamina.Current < StaminaFor(stage) - 1e-9 ? "ui.craft.fault.tired" : null;
        }

        /// <summary>19 §3: what a spoiled piece loses of a resource it needed <paramref name="count"/> of: half, rounded up.</summary>
        public static int Lost(int count) => (count + 1) / 2;

        /// <summary>The Stamina the work of a piece spends, in points.</summary>
        public double StaminaFor(int stage) => Stamina == null ? 0d : Ticks(stage) * WorkStamina * Stamina.Max;

        /// <summary>07.1 §8a: the Reaction Check's Δ for a piece of <paramref name="stage"/>.</summary>
        public double Delta(int stage) => ReactionCheck.Delta(_character.Get(CharacterAttribute.Dexterity),
            _character.Get(CharacterSkill.ItemCreation), stage <= 8 ? EightDifficulty : NineDifficulty);

        /// <summary>The bar of the next piece, its zone and pace by Δ, placed by the seed and the count of pieces worked.</summary>
        public ReactionBar Bar(int stage) => ReactionCheck.For(Delta(stage), _seed.Fork(SliceSeedStreams.Workshop, _state.Get(MadeKey), 1));

        /// <summary>
        /// Works the piece through its ticks, each spending its Stamina. <paramref name="interrupted"/> is asked after each
        /// tick; when it answers yes, the work stops with nothing spent. A piece begun before closing is finished after it.
        /// <see cref="CraftOutcome.Worked"/> means the piece waits for its Reaction Check (<see cref="Finish"/>).
        /// </summary>
        public CraftOutcome Work(string keeper, ItemDefinition item, int stage, Func<bool> interrupted)
        {
            if (Fault(keeper, item, stage) != null)
            {
                return CraftOutcome.Refused;
            }

            if (Stamina != null)
            {
                Stamina.Exerting = true;
            }

            try
            {
                for (var t = 0; t < Ticks(stage); t++)
                {
                    _clock.AdvanceTicks(1);
                    Stamina?.Spend(WorkStamina);
                    if (interrupted != null && interrupted())
                    {
                        return CraftOutcome.Interrupted;
                    }
                }
            }
            finally
            {
                if (Stamina != null)
                {
                    Stamina.Exerting = false;
                }
            }

            return CraftOutcome.Worked;
        }

        /// <summary>The work and its Reaction Check in one, for a <paramref name="result"/> already known.</summary>
        public CraftOutcome Make(string keeper, ItemDefinition item, int stage, Func<bool> interrupted, ReactionResult result, out ItemPiece piece)
        {
            piece = null;
            var worked = Work(keeper, item, stage, interrupted);
            return worked == CraftOutcome.Worked ? Finish(item, stage, result, out piece) : worked;
        }

        /// <summary>
        /// Settles a worked piece by its Reaction Check's <paramref name="result"/>: a good one makes it, a bad one spoils
        /// it, taking the fee and half of each resource, rounded up.
        /// </summary>
        public CraftOutcome Finish(ItemDefinition item, int stage, ReactionResult result, out ItemPiece piece)
        {
            piece = null;
            var recipe = Recipe(item, stage);
            if (recipe.Any(p => _bag.Count(p.item) < p.count) || _bag.Bronze < Rules.WorkshopFee)
            {
                return CraftOutcome.Interrupted; // the world took something during the work
            }

            var made = _state.Get(MadeKey);
            _state.Set(MadeKey, made + 1);
            _bag.Spend(Rules.WorkshopFee);

            // Every piece grows Item Creation and Dexterity, a spoiled one half (07.1 §3, S5-1); its stage is the task's.
            _character.Train(CharacterSkill.ItemCreation, stage, Growth.Of(result), new[] { CharacterAttribute.Dexterity });
            if (result == ReactionResult.Bad)
            {
                foreach (var part in recipe)
                {
                    _bag.Remove(part.item, Lost(part.count));
                }

                return CraftOutcome.Spoiled;
            }

            foreach (var part in recipe)
            {
                _bag.Remove(part.item, part.count);
            }

            var chance = ItemMaker.Chance(_character.Get(CharacterSkill.ItemCreation), _character.Get(CharacterAttribute.Perception),
                _character.Get(CharacterAttribute.Luck));
            piece = ItemMaker.Make(item, stage, chance, _seed.Fork(SliceSeedStreams.Workshop, made, 0));
            piece.made = true;
            _bag.AddPiece(piece);
            Made?.Invoke(piece);
            return CraftOutcome.Made;
        }
    }
}
