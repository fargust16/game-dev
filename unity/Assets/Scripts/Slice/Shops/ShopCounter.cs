using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.UI;
using GameDev.World.Portals;
using UnityEngine;

namespace GameDev.Slice.Shops
{
    /// <summary>
    /// A Master's or the trader's counter on Bawling Row (S5a.3): <c>E  Trade</c> within reach while the shop is open opens
    /// the shop page, whose lines are clicked: a thing on the shelf to buy it, a lesson to learn it, a carried thing to sell
    /// one. Item Creation is worked at the anvil beside the stall (<see cref="CraftStation"/>), not here. E again, Escape, walking away or a fight closes it. The world is not paused. The driver puts one on each keeper
    /// when a scene loads, so no scene is changed for it.
    /// </summary>
    public sealed class ShopCounter : MonoBehaviour, IPromptSource
    {
        /// <summary>Authoring default, m: the reach of a counter, as a conversation's.</summary>
        public const float Reach = 3f;

        private PlayerLocomotion _player;
        private HudView _hud;
        private Portal[] _doors;
        private PersonTalk[] _talks;
        private CraftStation[] _stations;
        private string _note;
        private bool _open;

        public string Keeper { get; private set; }

        public bool IsOpen => _open && _hud != null && _hud.IsShopOpen;

        public void Configure(string keeper) => Keeper = keeper;

        public bool InReach(Vector3 position) => InteractReach.Within(transform, position, Reach);

        public string PromptFor(Vector3 player)
        {
            var shops = Shops();
            if (shops == null || IsOpen || !InReach(player) || Busy() || Yields(player))
            {
                return null;
            }

            return shops.IsOpen ? Loc.Get("ui.prompt.trade") : Loc.Format("ui.prompt.shop_closed", shops.Rules.Opens);
        }

        /// <summary>Opens the shop page, as E does.</summary>
        public bool Open()
        {
            var shops = Shops();
            _hud ??= FindAnyObjectByType<HudView>();
            if (shops == null || _hud == null || !shops.IsOpen || Busy())
            {
                return false;
            }

            _open = true;
            _note = null;
            Show();
            return true;
        }

        public void Close()
        {
            if (_open && _hud != null)
            {
                _hud.CloseShop();
            }

            _open = false;
        }

        /// <summary>
        /// A conversation in reach or open takes E first: Kit stands by the trader's stall. So does the anvil beside Edda
        /// Brann's stall, in reach or with its page open.
        /// </summary>
        private bool Yields(Vector3 player)
        {
            _talks ??= FindObjectsByType<PersonTalk>(FindObjectsInactive.Exclude);
            if (_stations == null || _stations.Length == 0)
            {
                _stations = FindObjectsByType<CraftStation>(FindObjectsInactive.Exclude);
            }

            return _talks.Any(t => t != null && t.isActiveAndEnabled && (t.IsOpen || t.PromptFor(player) != null))
                || _stations.Any(s => s != null && s.isActiveAndEnabled && (s.IsOpen || s.PromptFor(player) != null));
        }

        private static SliceShops Shops() => SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Shops : null;

        private static bool Busy()
        {
            var world = SliceWorldDriver.Running;
            return world != null && world.Fights != null && world.Fights.InFight;
        }

        /// <summary>The page: the shelf, the lessons, and what the character can sell here, each line a click.</summary>
        private void Show()
        {
            var world = SliceWorldDriver.Running;
            var shops = Shops();
            if (world == null || shops == null || _hud == null)
            {
                return;
            }

            var lines = new List<PageLine> { new PageLine(Loc.Get("ui.shop.for_sale"), heading: true) };
            foreach (var thing in shops.Shelf(Keeper))
            {
                var t = thing;
                lines.Add(new PageLine(ThingLine(t), () => Act(shops.Buy(Keeper, t), "ui.shop.bought", t.Item)));
            }

            var shop = shops.Find(Keeper);
            if (shop.lessons.Length > 0)
            {
                lines.Add(new PageLine(Loc.Get("ui.shop.lessons"), heading: true));
                foreach (var lesson in shop.lessons)
                {
                    var l = lesson;
                    var name = Loc.Get("ui.lesson." + l);
                    lines.Add(shops.Knows(l)
                        ? new PageLine(Loc.Format("ui.shop.lesson.known", name))
                        : new PageLine(Loc.Format("ui.shop.lesson", name, Coins.Show(shops.Rules.LessonPrice)), () => Act(shops.Learn(Keeper, l), "ui.shop.learned", null, name)));
                }
            }

            if (shop.workshop && world.Workshop != null)
            {
                // Item Creation is worked at the anvil in the yard, not at the counter (the owner's play test of S5a).
                lines.Add(new PageLine(Loc.Format("ui.craft.at_anvil", Coins.Show(world.Workshop.Rules.WorkshopFee))));
            }

            var sellable = shops.Sellable(Keeper, p => world.Outfit != null && world.Outfit.IsWorn(p));
            if (sellable.Count > 0)
            {
                lines.Add(new PageLine(Loc.Get("ui.shop.to_sell"), heading: true));
                foreach (var (item, piece) in sellable)
                {
                    var i = item;
                    var p = piece;
                    var count = p == null ? world.Bag.Count(i.id) : 1;
                    var text = Loc.Format("ui.shop.sell", Name(i, p), count, Coins.Show(SliceShops.SalePrice(i, p)));
                    lines.Add(new PageLine(text, () => Act(shops.Sell(Keeper, i, p), "ui.shop.sold", i)));
                }
            }

            var title = Loc.Format("ui.shop.title", Loc.Get(GameDev.Slice.People.SliceRoster.NameKey(Keeper)));
            _hud.ShowShop(title, BagPage.Money(world.Bag), lines, _note ?? Loc.Get("ui.shop.hint"));
        }

        private void Act(string fault, string done, ItemDefinition item, string name = null)
        {
            _note = fault != null ? Loc.Get(fault) : Loc.Format(done, name ?? Loc.Get(item.NameKey));
            Show();
        }

        private static string Name(ItemDefinition item, ItemPiece piece) => piece == null
            ? Loc.Get(item.NameKey)
            : Loc.Format("ui.shop.piece", Loc.Get(item.NameKey), Loc.Get(BagPage.QualityKey(piece.Quality)));

        private static string ThingLine(ShelfItem thing)
        {
            var price = Coins.Show(thing.Price);
            if (thing.Piece == null)
            {
                return Loc.Format("ui.shop.good", Loc.Get(thing.Item.NameKey), thing.Count, price);
            }

            var line = Loc.Format("ui.shop.equipment", Name(thing.Item, thing.Piece), BagPage.Kg(thing.Item.weight), price);
            var mods = ItemMaker.Describe(thing.Piece.modifiers);
            return string.IsNullOrEmpty(mods) ? line : line + ": " + mods;
        }

        private void Update()
        {
            if (!_open)
            {
                Listen();
                return;
            }

            var input = GameInputService.Instance;
            _player ??= FindAnyObjectByType<PlayerLocomotion>();
            if (_hud == null || !_hud.IsShopOpen || _player == null || !InReach(_player.transform.position) || Busy()
                || (Shops() != null && !Shops().IsOpen) || (input != null && (input.CancelPressed || input.InteractPressed)))
            {
                Close();
            }
        }

        /// <summary>E within reach opens the page, unless a door or a conversation in reach takes it, as it takes the prompt.</summary>
        private void Listen()
        {
            var input = GameInputService.Instance;
            _player ??= FindAnyObjectByType<PlayerLocomotion>();
            if (input == null || _player == null || !input.InteractPressed || !InReach(_player.transform.position))
            {
                return;
            }

            _doors ??= FindObjectsByType<Portal>(FindObjectsInactive.Exclude);
            if (!_doors.Any(d => d != null && d.isActiveAndEnabled && d.InReach(_player.transform.position)) && !Yields(_player.transform.position))
            {
                Open();
            }
        }

        private void OnEnable()
        {
            Loc.Changed += Relabel;
            if (SliceWorldDriver.Running != null && SliceWorldDriver.Running.Bag != null)
            {
                SliceWorldDriver.Running.Bag.Changed += Relabel;
            }
        }

        private void OnDisable()
        {
            Loc.Changed -= Relabel;
            if (SliceWorldDriver.Running != null && SliceWorldDriver.Running.Bag != null)
            {
                SliceWorldDriver.Running.Bag.Changed -= Relabel;
            }

            Close();
        }

        private void Relabel()
        {
            if (IsOpen)
            {
                Show();
            }
        }
    }
}
