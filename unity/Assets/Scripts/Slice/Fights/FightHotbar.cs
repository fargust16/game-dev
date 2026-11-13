using System;
using System.Collections.Generic;
using GameDev.Confront.Rules;
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

        /// <summary>10.2 §5f Infusion (CR4): a switch, not an action; while on, each blow or shot pours Will Power.</summary>
        Infuse,
    }

    /// <summary>
    /// The fight's hotbar, after BG3's (owner's play test, 2026-11-11: the line of keys did not fit and read badly): a row
    /// of square slots, each an icon of the Modern GDR pack (asset map) with its key in a corner and its AP in the other.
    /// A click takes the action as its key does; a slot the player cannot use now is dimmed and takes no click. Over a
    /// slot a tip stands with the action's name, what it does, its key and its AP. Every word is a string table key
    /// (17 X.7): <c>ui.fight.action.&lt;id&gt;</c>, <c>.tip</c> and <c>.key</c>. After them stand the character's Prepared
    /// Abilities that a fight can use (CR4, 07.2 §4.5), keys 1 to 9, each with its Will Power in its tip; the slot of
    /// Infusion and of an ability being aimed is lit while on.
    /// </summary>
    public sealed class FightHotbar
    {
        private const string Slot = "hud__fight-slot";
        private const string SlotOff = "hud__fight-slot--off";
        private const string SlotOn = "hud__fight-slot--on";
        private const string SlotAbility = "hud__fight-slot--ability";
        private const string SlotAbilityFirst = "hud__fight-slot--ability-first";
        private const string TipVisible = "hud__fight-action-tip--visible";

        /// <summary>The slots in their order on the bar, by the action and its string table id.</summary>
        public static readonly (FightAction Action, string Id)[] Slots =
        {
            (FightAction.EndTurn, "end"), (FightAction.Wait, "wait"), (FightAction.Stance, "stance"), (FightAction.ChangeSet, "set"),
            (FightAction.FirstAid, "aid"), (FightAction.Stealth, "hide"), (FightAction.BreakAway, "flee"), (FightAction.Yield, "yield"),
            (FightAction.Spare, "spare"), (FightAction.Infuse, "infuse"),
        };

        private readonly IReadOnlyList<Sprite> _icons;
        private readonly List<(VisualElement Slot, Label Key, Label Ap)> _slots = new List<(VisualElement, Label, Label)>();
        private readonly bool[] _usable = new bool[Slots.Length];
        private readonly int[] _ap = new int[Slots.Length];
        private readonly List<FightAbility> _abilities = new List<FightAbility>();
        private readonly List<(VisualElement Slot, Label Key, Label Ap)> _abilitySlots = new List<(VisualElement, Label, Label)>();
        private readonly List<(bool Usable, int Ap, int WillPower)> _abilityState = new List<(bool, int, int)>();
        private VisualElement _panel;
        private VisualElement _row;
        private Label _tip;
        private FightAction _picked;
        private int _pickedAbility = -1;
        // A slot the pointer is over: the actions' from 0, the abilities' from Slots.Length.
        private int _hovered = -1;

        /// <param name="icons">One icon for each action in <see cref="FightAction"/>'s order after None; a missing one leaves its slot plain.</param>
        public FightHotbar(IReadOnlyList<Sprite> icons)
        {
            _icons = icons ?? Array.Empty<Sprite>();
        }

        /// <summary>True while the pointer is over the bar: the cursor then points at no cell and a click walks nowhere.</summary>
        public bool Over { get; private set; }

        /// <summary>How many slots of actions the bar shows.</summary>
        public int Count => _slots.Count;

        /// <summary>How many slots of abilities the bar shows after them.</summary>
        public int AbilityCount => _abilitySlots.Count;

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
            _abilitySlots.Clear();
            _abilities.Clear();
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
                Hover(slot, index);
                _row.Add(slot);
                _slots.Add((slot, key, ap));
            }
        }

        /// <summary>
        /// The Prepared Abilities a fight can use, in their order (CR4): their slots are made again only when the list
        /// changes, as a fight begins or the set is changed.
        /// </summary>
        public void SetAbilities(IReadOnlyList<FightAbility> shown)
        {
            var count = shown?.Count ?? 0;
            var same = _row != null && count == _abilities.Count;
            for (var i = 0; same && i < count; i++)
            {
                same = shown[i] == _abilities[i];
            }

            if (same || _row == null)
            {
                return;
            }

            foreach (var (slot, _, _) in _abilitySlots)
            {
                slot.RemoveFromHierarchy();
            }

            _abilitySlots.Clear();
            _abilities.Clear();
            _abilityState.Clear();
            if (_hovered >= Slots.Length)
            {
                _hovered = -1;
            }

            for (var i = 0; i < count; i++)
            {
                var index = i;
                var ability = shown[i];
                var slot = new VisualElement();
                slot.AddToClassList(Slot);
                slot.AddToClassList(SlotAbility);
                slot.EnableInClassList(SlotAbilityFirst, i == 0);
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("hud__fight-slot-icon");
                if (ability != null && ability.Icon != null)
                {
                    icon.style.backgroundImage = new StyleBackground(ability.Icon);
                }

                var key = new Label(i < 9 ? (i + 1).ToString() : string.Empty) { pickingMode = PickingMode.Ignore };
                key.AddToClassList("hud__fight-slot-key");
                var ap = new Label { pickingMode = PickingMode.Ignore };
                ap.AddToClassList("hud__fight-slot-ap");
                slot.Add(icon);
                slot.Add(key);
                slot.Add(ap);
                slot.RegisterCallback<ClickEvent>(_ => ClickAbility(index));
                Hover(slot, Slots.Length + index);
                _row.Add(slot);
                _abilitySlots.Add((slot, key, ap));
                _abilities.Add(ability);
                _abilityState.Add((false, 0, 0));
            }
        }

        private void Hover(VisualElement slot, int index)
        {
            slot.RegisterCallback<PointerEnterEvent>(_ => _hovered = index);
            slot.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (_hovered == index)
                {
                    _hovered = -1;
                }
            });
        }

        /// <summary>The ability slot clicked since the last call, once, by its place on the bar; -1 when none was.</summary>
        public int TakeAbility()
        {
            var picked = _pickedAbility;
            _pickedAbility = -1;
            return picked;
        }

        /// <summary>Clicks an ability's slot as the pointer does: taken only while it is usable.</summary>
        public void ClickAbility(int index)
        {
            if (index >= 0 && index < _abilityState.Count && _abilityState[index].Usable)
            {
                _pickedAbility = index;
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
            _pickedAbility = -1;
            _hovered = -1;
            Over = false;
            _tip?.RemoveFromClassList(TipVisible);
        }

        /// <summary>
        /// Whether each slot is usable now and its AP, and the tip over the slot the pointer is on; for each ability slot
        /// whether it is usable, its AP, its Will Power and whether it is being aimed; and whether Infusion is on.
        /// </summary>
        public void Refresh(Func<FightAction, (bool Usable, int Ap)> state, Func<int, (bool Usable, int Ap, int WillPower, bool On)> ability = null,
            bool infusing = false)
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                var (usable, ap) = state != null ? state(Slots[i].Action) : (false, 0);
                _usable[i] = usable;
                _ap[i] = ap;
                var (slot, key, apLabel) = _slots[i];
                slot.EnableInClassList(SlotOff, !usable);
                slot.EnableInClassList(SlotOn, Slots[i].Action == FightAction.Infuse && infusing);
                key.text = Loc.Get($"ui.fight.action.{Slots[i].Id}.key");
                apLabel.text = ap > 0 ? ap.ToString() : string.Empty;
            }

            for (var i = 0; i < _abilitySlots.Count; i++)
            {
                var (usable, ap, willPower, on) = ability != null ? ability(i) : (false, 0, 0, false);
                _abilityState[i] = (usable, ap, willPower);
                var (slot, _, apLabel) = _abilitySlots[i];
                slot.EnableInClassList(SlotOff, !usable && !on);
                slot.EnableInClassList(SlotOn, on);
                apLabel.text = ap > 0 ? ap.ToString() : string.Empty;
            }

            Tip();
        }

        private void Tip()
        {
            var ability = _hovered - Slots.Length;
            if (_tip == null || _hovered < 0 || (_hovered < Slots.Length ? _hovered >= _slots.Count : ability >= _abilitySlots.Count) || _tip.parent == null)
            {
                _tip?.RemoveFromClassList(TipVisible);
                return;
            }

            VisualElement over;
            if (ability >= 0)
            {
                var (_, ap, willPower) = _abilityState[ability];
                var key = _abilities[ability] != null ? _abilities[ability].Key : string.Empty;
                var last = Loc.Format("ui.fight.ability.cost", willPower);
                if (ability < 9)
                {
                    last += "  ·  " + Loc.Format("ui.fight.action.hotkey", ability + 1);
                }

                if (ap > 0)
                {
                    last += "  ·  " + Loc.Format("ui.fight.action.ap", ap);
                }

                _tip.text = $"<b><size=118%>{Loc.Get($"ability.{key}.name")}</size></b>\n{Loc.Get($"ability.{key}.text")}\n<color=#B8A47E>{last}</color>";
                over = _abilitySlots[ability].Slot;
            }
            else
            {
                var id = Slots[_hovered].Id;
                var last = Loc.Format("ui.fight.action.hotkey", Loc.Get($"ui.fight.action.{id}.key"));
                if (_ap[_hovered] > 0)
                {
                    last += "  ·  " + Loc.Format("ui.fight.action.ap", _ap[_hovered]);
                }

                _tip.text = $"<b><size=118%>{Loc.Get($"ui.fight.action.{id}")}</size></b>\n{Loc.Get($"ui.fight.action.{id}.tip")}\n<color=#B8A47E>{last}</color>";
                over = _slots[_hovered].Slot;
            }

            var box = over.worldBound;
            var frame = _tip.parent.worldBound;
            const float width = 320f;
            _tip.style.left = Mathf.Clamp(box.center.x - frame.x - (width * 0.5f), 8f, Mathf.Max(8f, frame.width - width - 8f));
            _tip.style.bottom = frame.yMax - box.yMin + 10f;
            _tip.AddToClassList(TipVisible);
        }
    }
}
