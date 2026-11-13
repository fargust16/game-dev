using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Input;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Slice.People;
using GameDev.UI;
using UnityEngine;

namespace GameDev.Slice.Dwellings
{
    /// <summary>
    /// A household's Stash (S5c.5; 19 §6.3, 10.3 §6.1): the chest the planner put in the dwelling. E opens it, and what it
    /// holds reads from who lives there: bronze in the wealth's range, or the goods of a named household's trade, and one
    /// Stash in seven papers, a note shown on the screen. Taken, it stays empty for the run; what it held is the night's
    /// loot, which an Arrest asks back. Taking it is a theft, against Император, and unseen Жрица's deed.
    /// </summary>
    public sealed class DwellingStash : MonoBehaviour, IPromptSource
    {
        /// <summary>Authoring default, m: how near the chest the character stands to open it.</summary>
        public const float Reach = 1.8f;

        private DwellingKey _key;
        private DwellingResidents _residents;
        private string _tradeItem;
        private int _tradePrice;
        private PlayerLocomotion _player;
        private HudView _hud;

        public string Place => _key.DwellingId;

        /// <summary>Finds the Stash under <paramref name="built"/>, if the plan put one there, and makes it one to open.</summary>
        public static DwellingStash At(Transform built, DwellingKey key, DwellingResidents residents, SliceSettings settings)
        {
            foreach (var t in built.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != DwellingAssembler.StashName)
                {
                    continue;
                }

                var stash = t.gameObject.AddComponent<DwellingStash>();
                stash._key = key;
                stash._residents = residents;
                var world = SliceWorldDriver.Running;
                var named = world != null ? SliceHomes.PersonAt(key.DwellingId, world.Seed) : null;
                stash._tradeItem = named != null ? SliceHomes.TradeOf(named) : null;
                var item = stash._tradeItem != null && settings != null && settings.Items != null ? settings.Items.Find(stash._tradeItem) : null;
                stash._tradePrice = item != null ? item.price : 0;
                stash._tradeItem = item != null ? stash._tradeItem : null;
                return stash;
            }

            return null;
        }

        private static SliceWorldDriver World => SliceWorldDriver.Running;

        public string PromptFor(Vector3 player)
        {
            var world = World;
            if (world == null || world.BreakIns == null || !InteractReach.Within(transform, player, Reach))
            {
                return null;
            }

            return Loc.Get(world.BreakIns.StashTaken(Place) ? "ui.prompt.stash.empty" : "ui.prompt.stash");
        }

        private void Update()
        {
            var world = World;
            var input = GameInputService.Instance;
            if (world == null || world.BreakIns == null || input == null || !input.InteractPressed || SceneTransition.IsTraveling)
            {
                return;
            }

            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            _hud = _hud != null && _hud.isActiveAndEnabled ? _hud : FindAnyObjectByType<HudView>();
            var prompt = _player != null ? PromptFor(_player.transform.position) : null;
            if (prompt == null || _hud == null || _hud.AnyWindowOpen || _hud.PromptText != prompt || world.Clock.IsPaused
                || (world.Work != null && world.Work.Busy) || (world.Fights != null && world.Fights.InFight))
            {
                return;
            }

            Open(world);
        }

        /// <summary>Opens the chest: takes what it holds, or says it is empty.</summary>
        public void Open(SliceWorldDriver world)
        {
            var held = world.BreakIns.TakeStash(Place, (int)_key.Wealth, _tradeItem, _tradePrice);
            _hud = _hud != null ? _hud : FindAnyObjectByType<HudView>();
            if (held == null)
            {
                _hud?.ShowMessage(Loc.Get("ui.stash.empty"));
                return;
            }

            world.Clock.AdvanceTicks(1); // authoring default: a tick to go through it
            _residents?.Took();
            var h = held.Value;
            var line = h.Count > 0
                ? Loc.Format("ui.stash.goods", Loc.Get(new ItemDefinition { id = h.Item }.NameKey), h.Count)
                : Loc.Format("ui.stash.bronze", Coins.Show(h.Bronze));
            if (h.Papers > 0)
            {
                line += "\n" + Loc.Get($"ui.stash.papers.{h.Papers}");
            }

            _hud?.ShowMessage(line);
        }
    }
}
