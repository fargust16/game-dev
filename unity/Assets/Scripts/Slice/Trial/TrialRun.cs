using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.UI;
using UnityEngine;

namespace GameDev.Slice.Trial
{
    /// <summary>
    /// The page of the Faction Gift (S5c.4; 19 §6.2): when a conversation with Ashgrave ends and the gift is still to be
    /// taken, a page shows the three items the World Seed offers, and a click takes one. Closing the page leaves the gift
    /// for the next conversation with him (agent's reading, 99 VS-TRIAL). Esc, walking 3 m away or a fight closes it.
    /// </summary>
    public sealed class TrialRun : MonoBehaviour
    {
        /// <summary>Authoring default, m: the page closes when the character walks this far from where it opened.</summary>
        private const float PageReach = 3f;

        private SliceWorldDriver _world;
        private PlayerLocomotion _player;
        private HudView _hud;
        private bool _pageOpen;
        private Vector3 _pageAt;
        private bool _wasTalking;
        private PersonTalk _ashgrave;

        public bool IsPageOpen => _pageOpen && _hud != null && _hud.IsShopOpen;

        public void Configure(SliceWorldDriver world) => _world = world;

        /// <summary>Opens the gift's page; false when no gift is due or the page cannot open.</summary>
        public bool OpenGiftPage()
        {
            var trial = _world != null ? _world.Trial : null;
            _hud = _hud != null ? _hud : FindAnyObjectByType<HudView>();
            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            if (trial == null || !trial.GiftDue || _hud == null || _player == null)
            {
                return false;
            }

            _pageOpen = true;
            _pageAt = _player.transform.position;
            ShowPage();
            return true;
        }

        public void ClosePage()
        {
            if (_pageOpen && _hud != null)
            {
                _hud.CloseShop();
            }

            _pageOpen = false;
        }

        private void ShowPage()
        {
            var trial = _world.Trial;
            var lines = new List<PageLine> { new PageLine(Loc.Get("ui.trial.gift.heading"), heading: true) };
            var offer = trial.GiftOffer();
            for (var i = 0; i < offer.Count; i++)
            {
                var index = i;
                var piece = offer[i];
                var item = _world.Bag.Catalogue.Find(piece.item);
                if (item == null)
                {
                    continue;
                }

                var line = Loc.Format("ui.shop.piece", Loc.Get(item.NameKey), Loc.Get(BagPage.QualityKey(piece.Quality)));
                var mods = ItemMaker.Describe(piece.modifiers);
                line = string.IsNullOrEmpty(mods) ? line : line + ": " + mods;
                if (SliceTrial.IsFirearm(item.id))
                {
                    line += "  " + Loc.Get("ui.trial.gift.pouch");
                }

                lines.Add(new PageLine(line, () => Take(index)));
            }

            _hud.ShowShop(Loc.Get("ui.trial.gift.title"), BagPage.Money(_world.Bag), lines, Loc.Get("ui.trial.gift.hint"));
        }

        private void Take(int index)
        {
            var piece = _world.Trial.GiftOffer().ElementAtOrDefault(index);
            if (piece == null || !_world.Trial.TakeGift(index))
            {
                return;
            }

            ClosePage();
            var item = _world.Bag.Catalogue.Find(piece.item);
            _hud?.ShowMessage(Loc.Format("ui.trial.gift.taken", item != null ? Loc.Get(item.NameKey) : piece.item));
        }

        private void Update()
        {
            if (_world == null || _world.Trial == null)
            {
                return;
            }

            if (_pageOpen)
            {
                KeepPage();
                return;
            }

            // A conversation with Ashgrave just ended: the gift's page, while one is due.
            var talking = _world.TalkOpen;
            if (talking && !_wasTalking)
            {
                _ashgrave = FindObjectsByType<PersonTalk>(FindObjectsInactive.Exclude)
                    .FirstOrDefault(t => t.IsOpen && t.Dialogue != null && t.Dialogue.Id == SliceTrial.Ashgrave);
            }

            if (!talking && _wasTalking && _ashgrave != null && _world.Trial.GiftDue && (_world.Fights == null || !_world.Fights.InFight))
            {
                OpenGiftPage();
            }

            if (!talking)
            {
                _ashgrave = null;
            }

            _wasTalking = talking;
        }

        private void KeepPage()
        {
            var input = GameInputService.Instance;
            var moved = _player == null || (_player.transform.position - _pageAt).sqrMagnitude > PageReach * PageReach;
            if (_hud == null || !_hud.IsShopOpen || moved || (_world.Fights != null && _world.Fights.InFight) || (input != null && input.CancelPressed))
            {
                ClosePage();
            }
        }

        private void OnDisable() => ClosePage();
    }
}
