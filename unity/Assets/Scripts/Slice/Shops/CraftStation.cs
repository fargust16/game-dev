using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.UI;
using GameDev.World.Portals;
using UnityEngine;

namespace GameDev.Slice.Shops
{
    /// <summary>
    /// The anvil in a Master's yard, where Item Creation is worked (19 §3; the owner's play test of S5a: crafting at a
    /// station, not through the Master). <c>E  Work at the anvil</c> within reach opens its page of recipes; a click works
    /// the piece through its ticks, the world running, as the character steps to the anvil and strikes it; its Reaction
    /// Check comes on the last blow, where the hammer meets the anvil's face, the world paused (the owner's second
    /// direction of 2026-11-11): a good stop makes the piece, a bad one spoils it and half of each resource. The page
    /// opens again with what came of it once the hammer has come up. E again, Escape, walking away or a fight closes it.
    /// The driver puts one on the anvil nearest each workshop's Master when a scene loads, so no scene is changed for it.
    /// </summary>
    public sealed class CraftStation : MonoBehaviour, IPromptSource
    {
        /// <summary>Authoring default, m: how near the anvil the character stands to work it.</summary>
        public const float Reach = 2.2f;

        /// <summary>
        /// Authoring default, m: how far beyond the anvil's edge the character stands to strike it, the hammer's head
        /// reaching about this far ahead of the chest at the blow (`clip_survey.txt`, HammerDown).
        /// </summary>
        public const float StrikeReach = 0.45f;

        /// <summary>The blows before the last, each struck whole: two blows with their rebounds.</summary>
        private static readonly string[] LeadIn = { "HammerDown", "HammerUp", "HammerDown", "HammerUp" };

        private PlayerLocomotion _player;
        private HudView _hud;
        private Portal[] _doors;
        private string _note;
        private bool _open;

        /// <summary>The Master whose workshop this is: its hours, its lesson and its fee.</summary>
        public string Keeper { get; private set; }

        public bool IsOpen => _open && _hud != null && _hud.IsShopOpen;

        public void Configure(string keeper) => Keeper = keeper;

        public bool InReach(Vector3 position) => InteractReach.Within(transform, position, Reach);

        public string PromptFor(Vector3 player)
        {
            var world = SliceWorldDriver.Running;
            if (world == null || world.Workshop == null || IsOpen || Working(world) || !InReach(player) || Busy())
            {
                return null;
            }

            return world.Shops.IsOpen ? Loc.Get("ui.prompt.anvil") : Loc.Format("ui.prompt.anvil_closed", world.Shops.Rules.Opens);
        }

        /// <summary>Opens the page, as E does.</summary>
        public bool Open()
        {
            var world = SliceWorldDriver.Running;
            _hud ??= FindAnyObjectByType<HudView>();
            if (world == null || world.Workshop == null || _hud == null || !world.Shops.IsOpen || Busy() || Working(world))
            {
                return false;
            }

            _open = true;
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

        private static bool Working(SliceWorldDriver world) => (world.Reaction != null && world.Reaction.Running) || (world.Work != null && world.Work.Busy);

        private static bool Busy()
        {
            var world = SliceWorldDriver.Running;
            return world != null && world.Fights != null && world.Fights.InFight;
        }

        /// <summary>Every recipe once Item Creation is learned, stage 8 too while its rare ingredient is carried.</summary>
        private void Show()
        {
            var world = SliceWorldDriver.Running;
            var workshop = world.Workshop;
            var lines = new List<PageLine> { new PageLine(Loc.Format("ui.craft.heading", Coins.Show(workshop.Rules.WorkshopFee)), heading: true) };
            if (!workshop.Knows)
            {
                lines.Add(new PageLine(Loc.Get("ui.craft.unlearned")));
            }
            else
            {
                var stages = world.Bag.Count(workshop.Rules.RareIngredient) > 0 ? new[] { 9, 8 } : new[] { 9 };
                foreach (var item in workshop.Bases)
                {
                    foreach (var stage in stages)
                    {
                        var i = item;
                        var s = stage;
                        var parts = string.Join(", ", workshop.Recipe(i, s).Select(p => Loc.Format("ui.craft.part", p.count, Loc.Get("item." + p.item))));
                        var text = Loc.Format(s == 9 ? "ui.craft.recipe" : "ui.craft.recipe.eight", Loc.Get(i.NameKey), parts, workshop.Ticks(s));
                        lines.Add(new PageLine(text, () => Craft(i, s)));
                    }
                }
            }

            _hud.ShowShop(Loc.Get("ui.anvil.title"), BagPage.Money(world.Bag), lines, _note ?? Loc.Get("ui.anvil.hint"));
        }

        /// <summary>
        /// Works the piece: the character steps to the anvil and strikes, the piece's ticks pass, and the last blow brings
        /// its bar; a fight in the ticks interrupts it and nothing is spent.
        /// </summary>
        private void Craft(ItemDefinition item, int stage)
        {
            var world = SliceWorldDriver.Running;
            var workshop = world.Workshop;
            var fault = workshop.Fault(Keeper, item, stage);
            if (fault != null || world.Work == null)
            {
                _note = Loc.Get(fault ?? "ui.craft.interrupted");
                Show();
                return;
            }

            Close();
            var stroke = new WorkStroke
            {
                Clip = "HammerDown",
                LeadIn = LeadIn,
                Then = "HammerUp",
                Steady = true,
                Bar = () => workshop.Bar(stage),
                CaptionKey = "ui.craft.caption",
                Settle = result =>
                {
                    var outcome = workshop.Finish(item, stage, result, out var piece);
                    _note = outcome == CraftOutcome.Made ? Loc.Format("ui.craft.made", Name(item, piece), workshop.Ticks(stage))
                        : outcome == CraftOutcome.Spoiled ? Loc.Get("ui.craft.spoiled")
                        : Loc.Get("ui.craft.interrupted");
                },
                Ended = broken => Open(),
                Face = transform,
                StandOff = PlayerWork.Radius(transform) + StrikeReach,
            };
            if (!world.Work.Begin(stroke))
            {
                _note = Loc.Get("ui.craft.interrupted");
                Open();
                return;
            }

            if (workshop.Work(Keeper, item, stage, Busy) != CraftOutcome.Worked)
            {
                _note = Loc.Get("ui.craft.interrupted");
                world.Work.Stop();
            }
        }

        private static string Name(ItemDefinition item, ItemPiece piece) => piece == null
            ? Loc.Get(item.NameKey)
            : Loc.Format("ui.shop.piece", Loc.Get(item.NameKey), Loc.Get(BagPage.QualityKey(piece.Quality)));

        private void Update()
        {
            if (!_open)
            {
                Listen();
                return;
            }

            var input = GameInputService.Instance;
            _player ??= FindAnyObjectByType<PlayerLocomotion>();
            var shops = SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Shops : null;
            if (_hud == null || !_hud.IsShopOpen || _player == null || !InReach(_player.transform.position) || Busy()
                || (shops != null && !shops.IsOpen) || (input != null && (input.CancelPressed || input.InteractPressed)))
            {
                Close();
            }
        }

        /// <summary>E within reach opens the page while the HUD shows the anvil's prompt, so a counter or a door beside it keeps E.</summary>
        private void Listen()
        {
            var input = GameInputService.Instance;
            _player ??= FindAnyObjectByType<PlayerLocomotion>();
            if (input == null || _player == null || !input.InteractPressed || !InReach(_player.transform.position))
            {
                return;
            }

            _hud ??= FindAnyObjectByType<HudView>();
            if (_hud != null && _hud.PromptText != Loc.Get("ui.prompt.anvil"))
            {
                return;
            }

            _doors ??= FindObjectsByType<Portal>(FindObjectsInactive.Exclude);
            if (!_doors.Any(d => d != null && d.isActiveAndEnabled && d.InReach(_player.transform.position)))
            {
                _note = null;
                Open();
            }
        }

        private void OnEnable() => Loc.Changed += Relabel;

        private void OnDisable()
        {
            Loc.Changed -= Relabel;
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
