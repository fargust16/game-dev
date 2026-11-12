using System;
using System.Collections.Generic;
using GameDev.Core.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameDev.Slice.Fights
{
    /// <summary>An action of the player's turn that the hotbar offers, besides walking and striking by the cursor.</summary>
    public enum FightAction
    {
        None,
        EndTurn,
        Wait,
        Stance,
        ChangeSet,
        FirstAid,
        Stealth,
        BreakAway,
        Yield,
        Spare,
    }

    /// <summary>
    /// The fight's hotbar, after BG3's (owner's play test, 2026-11-11: the line of keys did not fit and read badly): a row
    /// of square slots, each an icon of the Modern GDR pack (asset map) with its key in a corner and its AP in the other.
    /// A click takes the action as its key does; a slot the player cannot use now is dimmed and takes no click. Over a
    /// slot a tip stands with the action's name, what it does, its key and its AP. Every word is a string table key
    /// (17 X.7): <c>ui.fight.action.&lt;id&gt;</c>, <c>.tip</c> and <c>.key</c>.
    /// </summary>
    public sealed class FightHotbar
    {
        private const string Slot = "hud__fight-slot";
        private const string SlotOff = "hud__fight-slot--off";
        private const string TipVisible = "hud__fight-action-tip--visible";

        /// <summary>The slots in their order on the bar, by the action and its string table id.</summary>
        public static readonly (FightAction Action, string Id)[] Slots =
        {
            (FightAction.EndTurn, "end"), (FightAction.Wait, "wait"), (FightAction.Stance, "stance"), (FightAction.ChangeSet, "set"),
            (FightAction.FirstAid, "aid"), (FightAction.Stealth, "hide"), (FightAction.BreakAway, "flee"), (FightAction.Yield, "yield"),
            (FightAction.Spare, "spare"),

        };

        private readonly IReadOnlyList<Sprite> _icons;
        private readonly List<(VisualElement Slot, Label Key, Label Ap)> _slots = new List<(VisualElement, Label, Label)>();
        private readonly bool[] _usable = new bool[Slots.Length];
        private readonly int[] _ap = new int[Slots.Length];
        private VisualElement _panel;
        private VisualElement _row;
        private Label _tip;
        private FightAction _picked;
        private int _hovered = -1;

        /// <param name="icons">One icon for each action in <see cref="FightAction"/>'s order after None; a missing one leaves its slot plain.</param>
        public FightHotbar(IReadOnlyList<Sprite> icons)
        {
            _icons = icons ?? Array.Empty<Sprite>();
        }

        /// <summary>True while the pointer is over the bar: the cursor then points at no cell and a click walks nowhere.</summary>
        public bool Over { get; private set; }

        /// <summary>How many slots the bar shows.</summary>
        public int Count => _slots.Count;

        /// <summary>The tip over a slot, or null while none shows.</summary>
        public string TipText => _tip != null && _tip.ClassListContains(TipVisible) ? _tip.text : null;

        /// <summary>Takes the HUD's elements, making the slots the first time and again after the HUD is built anew.</summary>
        public void Bind(VisualElement panel, VisualElement row, Label tip)
        {
            if (row == _row && _slots.Count > 0)
            {
                return;
            }

            _panel = panel;
            _row = row;
            _tip = tip;
            _slots.Clear();
            Over = false;
            _hovered = -1;
            if (_row == null)
            {
                return;
            }

            _row.Clear();
            _panel?.RegisterCallback<PointerEnterEvent>(_ => Over = true);
            _panel?.RegisterCallback<PointerLeaveEvent>(_ => Over = false);
            for (var i = 0; i < Slots.Length; i++)
            {
                var index = i;
                var slot = new VisualElement();
                slot.AddToClassList(Slot);
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("hud__fight-slot-icon");
                var sprite = (int)Slots[i].Action - 1 < _icons.Count ? _icons[(int)Slots[i].Action - 1] : null;
                if (sprite != null)
                {
                    icon.style.backgroundImage = new StyleBackground(sprite);
                }

                var key = new Label { pickingMode = PickingMode.Ignore };
                key.AddToClassList("hud__fight-slot-key");
                var ap = new Label { pickingMode = PickingMode.Ignore };
                ap.AddToClassList("hud__fight-slot-ap");
                slot.Add(icon);
                slot.Add(key);
                slot.Add(ap);
                slot.RegisterCallback<ClickEvent>(_ => Click(index));
                slot.RegisterCallback<PointerEnterEvent>(_ => _hovered = index);
                slot.RegisterCallback<PointerLeaveEvent>(_ =>
                {
                    if (_hovered == index)
                    {
                        _hovered = -1;
                    }
                });
                _row.Add(slot);
                _slots.Add((slot, key, ap));
            }
        }

        /// <summary>The action clicked since the last call, once; None when none was.</summary>
        public FightAction Take()
        {
            var picked = _picked;
            _picked = FightAction.None;
            return picked;
        }

        /// <summary>Clicks a slot as the pointer does: taken only while it is usable.</summary>
        public void Click(int index)
        {
            if (index >= 0 && index < Slots.Length && _usable[index])
            {
                _picked = Slots[index].Action;
            }
        }

        /// <summary>Forgets the pointer and any click, as a fight begins or ends.</summary>
        public void Reset()
        {
            _picked = FightAction.None;
            _hovered = -1;
            Over = false;
            _tip?.RemoveFromClassList(TipVisible);
        }

        /// <summary>Whether each slot is usable now and its AP, and the tip over the slot the pointer is on.</summary>
        public void Refresh(Func<FightAction, (bool Usable, int Ap)> state)
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                var (usable, ap) = state != null ? state(Slots[i].Action) : (false, 0);
                _usable[i] = usable;
                _ap[i] = ap;
                var (slot, key, apLabel) = _slots[i];
                slot.EnableInClassList(SlotOff, !usable);
                key.text = Loc.Get($"ui.fight.action.{Slots[i].Id}.key");
                apLabel.text = ap > 0 ? ap.ToString() : string.Empty;
            }

            Tip();
        }

        private void Tip()
        {
            if (_tip == null || _hovered < 0 || _hovered >= _slots.Count || _tip.parent == null)
            {
                _tip?.RemoveFromClassList(TipVisible);
                return;
            }

            var id = Slots[_hovered].Id;
            var last = Loc.Format("ui.fight.action.hotkey", Loc.Get($"ui.fight.action.{id}.key"));
            if (_ap[_hovered] > 0)
            {
                last += "  ·  " + Loc.Format("ui.fight.action.ap", _ap[_hovered]);
            }

            _tip.text = $"<b><size=118%>{Loc.Get($"ui.fight.action.{id}")}</size></b>\n{Loc.Get($"ui.fight.action.{id}.tip")}\n<color=#B8A47E>{last}</color>";
            var box = _slots[_hovered].Slot.worldBound;
            var frame = _tip.parent.worldBound;
            const float width = 320f;
            _tip.style.left = Mathf.Clamp(box.center.x - frame.x - (width * 0.5f), 8f, Mathf.Max(8f, frame.width - width - 8f));
            _tip.style.bottom = frame.yMax - box.yMin + 10f;
            _tip.AddToClassList(TipVisible);
        }
    }
}
