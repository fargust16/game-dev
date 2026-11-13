using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.UI;
using UnityEngine;

namespace GameDev.Slice.Creation
{
    /// <summary>
    /// The creation page at a new run's start (07 *Creation choices in V1*, owner 2026-11-13, S5-4; S5b.1), in the page
    /// a shop uses, with the name's field over its list: the name; the sex, which changes no rule; one of the bodies of
    /// that sex, worn at once on the character behind the page; and the attributes, 9 free points by hand or one of three
    /// archetypes (07 *Attributes at the start*). The world stands still while it is open, every game key off so a letter
    /// typed opens no page, and Esc does not close it: it ends with "Begin" once a name is given and the points are spent.
    /// Every word is a string table key (17 X.7).
    /// </summary>
    public sealed class CharacterCreation : MonoBehaviour
    {
        private static readonly CharacterAttribute[] Attributes = (CharacterAttribute[])Enum.GetValues(typeof(CharacterAttribute));

        private readonly Dictionary<CharacterAttribute, int> _added = new Dictionary<CharacterAttribute, int>();
        private PlayerLook[] _looks = new PlayerLook[0];
        private CharacterSheet _sheet;
        private Action<CharacterIdentity> _done;
        private HudView _hud;
        private string _name = string.Empty;
        private bool _female;
        private int _look;
        private int _archetype = -1;
        private string _note;

        /// <summary>A batch run meets no creation page unless a test asks for it (as the Suspend Save's offer).</summary>
        public static bool Forced { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Forced = false;

        public static bool ShouldOffer() => Forced || !Application.isBatchMode;

        public bool IsOpen { get; private set; }

        public void Configure(CharacterSheet sheet, PlayerLook[] looks)
        {
            _sheet = sheet;
            _looks = looks ?? new PlayerLook[0];
        }

        /// <summary>The bodies of the sex chosen, in the settings' order.</summary>
        public IReadOnlyList<PlayerLook> Looks => _looks.Where(l => l != null && l.female == _female && l.Body != null).ToList();

        /// <summary>The first body of a sex: the one a character wears before creation and when a save names none.</summary>
        public PlayerLook FirstLook(bool female) => _looks.FirstOrDefault(l => l != null && l.female == female && l.Body != null);

        public PlayerLook Find(string id) => _looks.FirstOrDefault(l => l != null && l.id == id && l.Body != null);

        /// <summary>Opens the page; <paramref name="done"/> hears who the character is once it begins. False with no HUD.</summary>
        public bool Open(HudView hud, Action<CharacterIdentity> done)
        {
            _hud = hud;
            if (_hud == null || _sheet == null || IsOpen)
            {
                return false;
            }

            _done = done;
            _name = string.Empty;
            _female = false;
            _look = 0;
            _archetype = -1;
            _note = null;
            _added.Clear();
            IsOpen = true;
            Hold(true);
            Wear();
            Show();
            return true;
        }

        // ------------------------------------------------------------------------------------------- the choices

        /// <summary>The name as typed; it is kept cleaned (07: a name, <see cref="CharacterStart.NameLength"/> letters at most).</summary>
        public void Name(string name) => _name = name ?? string.Empty;

        /// <summary>The name as typed so far.</summary>
        public string TypedName => _name;

        public void Sex(bool female)
        {
            if (_female == female)
            {
                return;
            }

            _female = female;
            _look = 0;
            Wear();
            Show();
        }

        public void Look(int index)
        {
            var looks = Looks;
            if (index < 0 || index >= looks.Count)
            {
                return;
            }

            _look = index;
            Wear();
            Show();
        }

        /// <summary>An archetype by its index, or -1 for free points (which start again from none).</summary>
        public void TakeArchetype(int index)
        {
            _archetype = index >= 0 && index < CharacterStart.Archetypes.Length ? index : -1;
            _added.Clear();
            if (_archetype >= 0)
            {
                foreach (var pair in CharacterStart.Archetypes[_archetype].Added)
                {
                    _added[pair.Key] = pair.Value;
                }
            }

            _note = null;
            Show();
        }

        /// <summary>One free point onto an attribute, while points are left; it leaves an archetype for free points.</summary>
        public void AddPoint(CharacterAttribute attribute)
        {
            if (_archetype >= 0)
            {
                TakeArchetype(-1);
            }

            if (PointsLeft <= 0)
            {
                _note = Loc.Get("ui.create.note.spent");
                Show();
                return;
            }

            _added[attribute] = Added(attribute) + 1;
            _note = null;
            Show();
        }

        public void ResetPoints() => TakeArchetype(-1);

        public int PointsLeft => _archetype >= 0 ? 0 : CharacterStart.FreePoints - _added.Values.Sum();

        private int Added(CharacterAttribute attribute) => _added.TryGetValue(attribute, out var v) ? v : 0;

        /// <summary>
        /// Begins the life with the choices: the sheet's attributes, the body worn, the page closed. False, with a note on
        /// the page, while the name is empty or free points are left.
        /// </summary>
        public bool Begin()
        {
            if (!IsOpen)
            {
                return false;
            }

            Sync();
            var name = CharacterStart.CleanName(_name);
            var archetype = _archetype >= 0 ? CharacterStart.Archetypes[_archetype] : null;
            if (name.Length == 0)
            {
                _note = Loc.Get("ui.create.note.name");
            }
            else if (archetype == null && PointsLeft > 0)
            {
                _note = Loc.Format("ui.create.note.points", PointsLeft);
            }
            else if (!CharacterStart.Apply(_sheet, _added, archetype))
            {
                _note = Loc.Get("ui.create.note.points.wrong");
            }
            else
            {
                var looks = Looks;
                var identity = new CharacterIdentity
                {
                    name = name,
                    female = _female,
                    look = looks.Count > 0 ? looks[Mathf.Clamp(_look, 0, looks.Count - 1)].id : string.Empty,
                };
                Close();
                _done?.Invoke(identity);
                return true;
            }

            Show();
            return false;
        }

        private void Close()
        {
            IsOpen = false;
            Hold(false);
            if (_hud != null)
            {
                _hud.CloseShop();
            }
        }

        // ------------------------------------------------------------------------------------------- the page

        /// <summary>The name as the field holds it, read before the page is drawn again or the life begins.</summary>
        private void Sync()
        {
            var typed = _hud != null ? _hud.ShopFieldText : null;
            if (typed != null)
            {
                _name = typed;
            }
        }

        private void Show()
        {
            if (_hud == null)
            {
                return;
            }

            Sync();
            var lines = new List<PageLine>
            {
                new PageLine(Loc.Get("ui.create.sex"), heading: true),
                new PageLine(Mark(!_female) + Loc.Get("ui.create.sex.man"), () => Sex(false)),
                new PageLine(Mark(_female) + Loc.Get("ui.create.sex.woman"), () => Sex(true)),
                new PageLine(Loc.Get("ui.create.look"), heading: true),
            };

            var looks = Looks;
            for (var i = 0; i < looks.Count; i++)
            {
                var index = i;
                lines.Add(new PageLine(Mark(i == _look) + Loc.Format("ui.create.look.line", i + 1, looks.Count), () => Look(index)));
            }

            lines.Add(new PageLine(Loc.Get("ui.create.archetype"), heading: true));
            for (var i = 0; i < CharacterStart.Archetypes.Length; i++)
            {
                var index = i;
                var archetype = CharacterStart.Archetypes[i];
                var spread = string.Join(", ", archetype.Added.Select(p => $"{Loc.Get(AttributeKey(p.Key))} +{p.Value}"));
                lines.Add(new PageLine(Mark(_archetype == i) + Loc.Format("ui.create.archetype.line", Loc.Get($"ui.create.archetype.{archetype.Key}"),
                    spread, Loc.Get($"ui.create.archetype.{archetype.Key}.who")), () => TakeArchetype(index)));
            }

            lines.Add(new PageLine(Mark(_archetype < 0) + Loc.Format("ui.create.points", CharacterStart.FreePoints, PointsLeft), () => TakeArchetype(-1)));
            lines.Add(new PageLine(Loc.Get("ui.create.attributes"), heading: true));
            foreach (var attribute in Attributes)
            {
                var a = attribute;
                lines.Add(new PageLine(Loc.Format("ui.create.attribute.line", Loc.Get(AttributeKey(a)), CharacterSheet.Lowest + Added(a)), () => AddPoint(a)));
            }

            lines.Add(new PageLine(Loc.Get("ui.create.points.reset"), ResetPoints));
            lines.Add(new PageLine(Loc.Get("ui.create.begin"), () => Begin()));
            _hud.ShowShop(Loc.Get("ui.create.title"), Loc.Get("ui.create.name"), lines, _note ?? Loc.Get("ui.create.hint"));
            _hud.ShowShopField(_name, Name);
            _hud.ShowShopClose(false);
        }

        public static string AttributeKey(CharacterAttribute attribute) => $"attribute.{attribute.ToString().ToLowerInvariant()}";

        private static string Mark(bool on) => Loc.Get(on ? "ui.create.mark.on" : "ui.create.mark.off") + "  ";

        private void Wear()
        {
            var looks = Looks;
            if (looks.Count > 0)
            {
                PlayerBody.Wear(FindAnyObjectByType<PlayerLocomotion>(), looks[Mathf.Clamp(_look, 0, looks.Count - 1)]);
            }
        }

        /// <summary>The character stands and every game key is off while the page is open.</summary>
        private static void Hold(bool on)
        {
            var input = GameInputService.Instance;
            if (input != null)
            {
                input.Typing = on;
            }

            var player = FindAnyObjectByType<PlayerLocomotion>();
            if (player != null)
            {
                player.enabled = !on;
            }
        }

        private void OnEnable() => Loc.Changed += Relabel;

        private void OnDisable()
        {
            Loc.Changed -= Relabel;
            if (IsOpen)
            {
                Hold(false);
            }
        }

        private void Relabel()
        {
            if (IsOpen)
            {
                Show();
            }
        }

        private void Update()
        {
            // The page stays up whatever closes the shop's panel under it (a scene's new HUD, a stray close).
            if (IsOpen && _hud != null && !_hud.IsShopOpen)
            {
                Show();
            }
        }
    }
}
