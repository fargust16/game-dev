using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.UI;
using GameDev.World.Poi;
using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>
    /// A source the player reads (09; S1.3): the Notice Board. Interact in reach shows what it says now in the HUD's
    /// plain panel; Interact again, or walking away, closes it. Reading does not pause the world. People are asked in
    /// conversation instead (<see cref="PersonTalk"/>, S2.4). Its words are string table keys (17 X.7), and an open panel
    /// follows a change of language.
    /// </summary>
    public sealed class TipSourcePoint : MonoBehaviour, IPromptSource
    {
        [SerializeField] private TipSource source;

        [Tooltip("The string table key of the panel's title, e.g. the board's name.")]
        [SerializeField] private string titleKey = "source.board.title";

        [Tooltip("The string table key of what the panel says when the source has nothing now.")]
        [SerializeField] private string nothingKey = "source.board.nothing";

        [Tooltip("Authoring default, m: horizontal reach from this point.")]
        [SerializeField] private float interactRadius = 2f;

        private PlayerLocomotion _player;
        private HudView _hud;
        private IReadOnlyList<Tip> _shown = new Tip[0];
        private IReadOnlyList<System.Func<string>> _news = new System.Func<string>[0];
        private IReadOnlyList<(GameDev.Slice.Tasks.PaidTask Task, bool New)> _tasks = new (GameDev.Slice.Tasks.PaidTask, bool)[0];
        private long _readAt;

        public TipSource Source => source;

        public bool IsOpen { get; private set; }

        /// <summary>Setup for scenes built by code; a placed source is set in the Inspector.</summary>
        public void Configure(TipSource tipSource, string panelTitleKey, string nothingLineKey, float radius)
        {
            source = tipSource;
            titleKey = panelTitleKey;
            nothingKey = nothingLineKey;
            interactRadius = radius;
        }

        /// <summary>True when the player stands within reach on the same level and sees the source.</summary>
        public bool InReach(Vector3 position) => InteractReach.Within(transform, position, interactRadius);

        public string PromptFor(Vector3 player) => !IsOpen && InReach(player) ? Loc.Get("ui.prompt.read") : null;

        /// <summary>What Interact does: opens the panel with what the source says now, or closes it.</summary>
        public void Use()
        {
            if (IsOpen)
            {
                Close();
                return;
            }

            var world = SliceWorldDriver.Running;
            var hud = Hud();
            if (world == null || hud == null)
            {
                return;
            }

            _shown = world.Tips.Read(source, world.Clock.Tick);
            _tasks = source == TipSource.NoticeBoard ? BoardTasks(world) : new (GameDev.Slice.Tasks.PaidTask, bool)[0];
            // 09 *News Pool* (PB8d, S5c.7): the board pins what the city is talking about, the newest first, after its
            // notices, leaving out an outcome one of its notices already tells.
            _readAt = world.Clock.Tick;
            var shown = _shown;
            _news = world.News != null ? world.News.Tell(source, _readAt, key => GameDev.Slice.News.SliceNews.ToldBy(shown, key)) : new System.Func<string>[0];
            IsOpen = true;
            Show();
        }

        // What was said stays what was said; only its words follow the language.
        private void Show()
        {
            if (IsOpen)
            {
                var tasks = _tasks.Select(t => Loc.Format(t.New ? "ui.board.task" : "ui.board.task.taken", Loc.Get(t.Task.TextKey), Coins.Show(t.Task.Pay)));
                var lines = tasks.Concat(_shown.Select(t => t.Line)).Concat(_news.Select(n => n())).ToList();
                Hud()?.ShowReading(Loc.Get(titleKey), lines.Count > 0 ? string.Join("\n\n", lines) : Loc.Get(nothingKey));
            }
        }

        /// <summary>
        /// SLICE_TRIAL §3.1 (S5c.7): reading the board takes each paid notice it offers now, as one takes a slip; the ones
        /// taken before and not yet done stay pinned, marked taken.
        /// </summary>
        private static IReadOnlyList<(GameDev.Slice.Tasks.PaidTask, bool)> BoardTasks(SliceWorldDriver world)
        {
            var tasks = world.Tasks;
            if (tasks == null)
            {
                return new (GameDev.Slice.Tasks.PaidTask, bool)[0];
            }

            var tick = world.Clock.Tick;
            var held = GameDev.Slice.Tasks.SliceTasks.All.Where(t => t.Giver == GameDev.Slice.Tasks.TaskGiver.Board && tasks.Held(t.Id))
                .Select(t => (t, false)).ToList();
            foreach (var task in tasks.OfferedBy(GameDev.Slice.Tasks.SliceTasks.Board, tick).ToList())
            {
                if (tasks.Take(task.Id, tick))
                {
                    held.Add((task, true));
                }
            }

            return held;
        }

        private void OnEnable()
        {
            Loc.Changed += Show;
        }

        private void Close()
        {
            IsOpen = false;
            Hud()?.CloseReading();
        }

        private void Update()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerLocomotion>();
                if (_player == null)
                {
                    return;
                }
            }

            // Closed from outside, by Esc or by another window (owner, 2026-11-12): the next E opens it again, not closes nothing.
            if (IsOpen && Hud() != null && !Hud().IsReadingOpen)
            {
                IsOpen = false;
            }

            var inReach = InReach(_player.transform.position);
            if (IsOpen && !inReach)
            {
                Close();
                return;
            }

            var input = GameInputService.Instance;
            if (inReach && input != null && input.InteractPressed)
            {
                Use();
            }
        }

        private void OnDisable()
        {
            Loc.Changed -= Show;
            if (IsOpen)
            {
                Close();
            }
        }

        private HudView Hud()
        {
            if (_hud == null)
            {
                _hud = FindAnyObjectByType<HudView>();
            }

            return _hud;
        }
    }
}
