using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Confront.Rules;
using GameDev.Items;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;

namespace GameDev.Slice.Shops
{
    /// <summary>One thing on a shelf: a stack of goods, or one piece of equipment.</summary>
    public sealed class ShelfItem
    {
        public ShelfItem(ItemDefinition item, int count, ItemPiece piece = null)
        {
            Item = item;
            Count = count;
            Piece = piece;
        }

        public ItemDefinition Item { get; }

        public int Count { get; internal set; }

        public ItemPiece Piece { get; }

        /// <summary>19 §6.3: a piece by its quality, a good by its own price.</summary>
        public int Price => Piece != null ? Piece.Price : Item.price;
    }

    /// <summary>
    /// The slice's shops (S5a.3; 19 §6.1–6.3): open from 08:00 to 20:00, each shelf filled again every morning, goods one to
    /// three of each, equipment one piece of each base made at that day's Shop Skill, 26 and one more a day, by the world
    /// seed. The character buys at the price, sells what the shop sells back for a quarter of the shop's price, and learns a
    /// profession for 1 000 bronze. What the character sells stays on the shelf until the next morning. The shelves live as
    /// long as the world; saving waits for S5c.
    /// </summary>
    public sealed class SliceShops
    {
        private readonly ShopCatalogue _rules;
        private readonly ItemCatalogue _items;
        private readonly IWorldSeed _seed;
        private readonly IWorldClock _clock;
        private readonly Bag _bag;
        private readonly IWorldState _state;
        private readonly Dictionary<string, (int day, List<ShelfItem> shelf)> _shelves = new Dictionary<string, (int, List<ShelfItem>)>();

        public SliceShops(ShopCatalogue rules, ItemCatalogue items, IWorldSeed seed, IWorldClock clock, Bag bag, IWorldState state)
        {
            _rules = rules;
            _items = items;
            _seed = seed;
            _clock = clock;
            _bag = bag;
            _state = state;
        }

        /// <summary>Anything bought, sold or learned.</summary>
        public event Action Changed;

        /// <summary>
        /// 10.2 §5d (owner, 2026-11-12: firearms first): a melee weapon or a shield, which stays in the catalogue as data and
        /// leaves the slice until VS-RANGED's rework: no shelf holds one, no shop buys one and no anvil makes one.
        /// </summary>
        public static bool Withheld(ItemDefinition item) => item.fight is FightWeapon weapon
            ? !weapon.Ranged && !weapon.Unarmed
            : item.fight is FightOffHand off && off.Kind == OffHandKind.Shield;

        public ShopCatalogue Rules => _rules;

        public static string LessonKey(string lesson) => "player.lesson." + lesson;

        public ShopEntry Find(string keeper) => _rules.Shops.FirstOrDefault(s => s.keeper == keeper);

        public bool IsOpen => _clock.MinuteOfDay >= _rules.Opens * 60 && _clock.MinuteOfDay < _rules.Closes * 60;

        /// <summary>
        /// 18 *Townsfolk in the slice* (S5c.3): whether a keeper is at its work now, by its hours; the driver sets it, and with
        /// none every keeper is.
        /// </summary>
        public Func<string, bool> KeeperAtWork { get; set; }

        /// <summary>A shop and its workshop are open in the shops' hours while their keeper is at work: never while it is away, asleep or hiding.</summary>
        public bool IsOpenFor(string keeper) => IsOpen && (KeeperAtWork == null || KeeperAtWork(keeper));

        /// <summary>The hour a keeper's shop opens, as its closed sign shows it: its work's by its hours, else the shops'.</summary>
        public int Opens(string keeper) => GameDev.Slice.People.SliceHours.WorkOpens(keeper) ?? _rules.Opens;

        /// <summary>19 §6.1 World Age: the Shop Skill of today's shelves.</summary>
        public int ShopSkill => _rules.ShopSkill + (_rules.SkillPerDay * (ShelfDay - 1));

        /// <summary>Before the morning the shelves are still yesterday's.</summary>
        private int ShelfDay => _clock.MinuteOfDay < _rules.Opens * 60 ? Math.Max(1, _clock.Day - 1) : _clock.Day;

        public bool Knows(string lesson) => _state.IsSet(LessonKey(lesson));

        /// <summary>What the keeper's shelf holds today, nothing sold out.</summary>
        public IReadOnlyList<ShelfItem> Shelf(string keeper)
        {
            var shop = Find(keeper);
            if (shop == null)
            {
                return new ShelfItem[0];
            }

            var day = ShelfDay;
            if (!_shelves.TryGetValue(keeper, out var kept) || kept.day != day)
            {
                kept = (day, Fill(shop, day));
                _shelves[keeper] = kept;
            }

            return kept.shelf.Where(s => s.Count > 0).ToList();
        }

        /// <summary>What the character carries that the shop buys back: stacks, and pieces not worn or held.</summary>
        public IReadOnlyList<(ItemDefinition item, ItemPiece piece)> Sellable(string keeper, Func<ItemPiece, bool> worn)
        {
            var shop = Find(keeper);
            var list = new List<(ItemDefinition, ItemPiece)>();
            if (shop == null)
            {
                return list;
            }

            list.AddRange(_bag.Stacks.Where(s => Buys(shop, s.item)).Select(s => (s.item, (ItemPiece)null)));
            foreach (var piece in _bag.Pieces)
            {
                var item = _items.Find(piece.item);
                if (item != null && Buys(shop, item) && SalePrice(item, piece) > 0 && (worn == null || !worn(piece)))
                {
                    list.Add((item, piece));
                }
            }

            return list;
        }

        /// <summary>19 §6.3 Selling: a quarter of the shop's price, unless the item names its own.</summary>
        public static int SalePrice(ItemDefinition item, ItemPiece piece) =>
            item.sale > 0 ? item.sale : (piece != null ? piece.Price : item.price) / 4;

        /// <summary>Buys one of a shelf's things; the key of the reason it cannot, or null.</summary>
        public string Buy(string keeper, ShelfItem thing)
        {
            if (!IsOpenFor(keeper))
            {
                return "ui.shop.fault.closed";
            }

            if (thing == null || thing.Count <= 0 || !Shelf(keeper).Contains(thing))
            {
                return "ui.shop.fault.gone";
            }

            if (_bag.Bronze < thing.Price)
            {
                return "ui.shop.fault.money";
            }

            if (!_bag.CanCarry(thing.Item.weight))
            {
                return "ui.shop.fault.heavy";
            }

            _bag.Spend(thing.Price);
            if (thing.Piece != null)
            {
                _bag.AddPiece(thing.Piece);
            }
            else
            {
                _bag.Add(thing.Item.id);
            }

            thing.Count--;
            Changed?.Invoke();
            return null;
        }

        /// <summary>Sells one: a stack's unit or a piece; it stays on the shelf until the morning.</summary>
        public string Sell(string keeper, ItemDefinition item, ItemPiece piece)
        {
            var shop = Find(keeper);
            if (!IsOpenFor(keeper))
            {
                return "ui.shop.fault.closed";
            }

            if (shop == null || item == null || !Buys(shop, item))
            {
                return "ui.shop.fault.gone";
            }

            if (piece != null ? !_bag.RemovePiece(piece) : !_bag.Remove(item.id))
            {
                return "ui.shop.fault.gone";
            }

            _bag.Earn(SalePrice(item, piece));
            var shelf = Shelf(keeper);
            var same = piece == null ? shelf.FirstOrDefault(s => s.Piece == null && s.Item == item) : null;
            if (same != null)
            {
                same.Count++;
            }
            else
            {
                _shelves[keeper].shelf.Add(new ShelfItem(item, 1, piece));
            }

            Changed?.Invoke();
            return null;
        }

        /// <summary>Learns a profession the keeper teaches; the key of the reason it cannot, or null.</summary>
        public string Learn(string keeper, string lesson)
        {
            var shop = Find(keeper);
            if (!IsOpenFor(keeper))
            {
                return "ui.shop.fault.closed";
            }

            if (shop == null || !shop.lessons.Contains(lesson))
            {
                return "ui.shop.fault.gone";
            }

            if (Knows(lesson))
            {
                return "ui.shop.fault.known";
            }

            if (!_bag.Spend(_rules.LessonPrice))
            {
                return "ui.shop.fault.money";
            }

            _state.Set(LessonKey(lesson), 1);
            Changed?.Invoke();
            return null;
        }

        private bool Buys(ShopEntry shop, ItemDefinition item) =>
            shop.sells.Contains(item.id) || (shop.equipment && item.kind == ItemKind.Equipment && item.price > 0 && !Withheld(item));

        /// <summary>A morning's shelf, by the world seed, the shop and the day.</summary>
        private List<ShelfItem> Fill(ShopEntry shop, int day)
        {
            var index = Array.IndexOf(_rules.Shops.ToArray(), shop);
            var rolls = _seed.Fork(SliceSeedStreams.ShopStock, index, day);
            var shelf = new List<ShelfItem>();
            foreach (var id in shop.sells)
            {
                var item = _items.Find(id);
                if (item != null)
                {
                    shelf.Add(new ShelfItem(item, rolls.Next(_rules.LeastStock, _rules.MostStock + 1)));
                }
            }

            if (shop.equipment)
            {
                var p = ItemMaker.ShopChance(_rules.ShopSkill + (_rules.SkillPerDay * (day - 1)));
                foreach (var item in _items.Items.Where(i => i.kind == ItemKind.Equipment && i.price > 0 && !Withheld(i)))
                {
                    shelf.Add(new ShelfItem(item, 1, ItemMaker.Make(item, item.stage, p, rolls)));
                }
            }

            return shelf;
        }
    }
}
