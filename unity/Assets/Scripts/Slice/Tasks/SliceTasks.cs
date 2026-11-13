using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Items;
using GameDev.World.State;

namespace GameDev.Slice.Tasks
{
    /// <summary>Who gives a paid task (SLICE_TRIAL §3): the Notice Board, a Master at its counter, or the faction through Ashgrave.</summary>
    public enum TaskGiver
    {
        Board,
        Master,
        Faction,
    }

    /// <summary>One paid task of SLICE_TRIAL §3, as data.</summary>
    public sealed class PaidTask
    {
        public PaidTask(string id, TaskGiver giver, string who, int day, int pay, string handAt, params (string Item, int Count)[] wants)
        {
            Id = id;
            Giver = giver;
            Who = who;
            Day = day;
            Pay = pay;
            HandAt = handAt;
            Wants = wants;
        }

        public string Id { get; }

        public TaskGiver Giver { get; }

        /// <summary>The Master's id, <see cref="SliceTasks.Board"/> or <see cref="SliceTasks.Ashgrave"/>.</summary>
        public string Who { get; }

        /// <summary>The day it is first offered, from 08:00 on day 2 (§3).</summary>
        public int Day { get; }

        /// <summary>The last day it is offered, 0 for every day after <see cref="Day"/> (Edda's order of the day).</summary>
        public int LastDay { get; set; }

        public int Pay { get; }

        /// <summary>The spot it is handed in at, or null for its giver.</summary>
        public string HandAt { get; }

        /// <summary>What it is paid for, handed over.</summary>
        public (string Item, int Count)[] Wants { get; }

        /// <summary>An item the task hands the character: on taking when <see cref="PickUpAt"/> is null, else at that spot.</summary>
        public string Gives { get; set; }

        public string PickUpAt { get; set; }

        /// <summary>The stage of the Perception check that finds <see cref="Gives"/> at its spot, 0 for none.</summary>
        public int NoticeStage { get; set; }

        /// <summary>Ticks from taking within which it must be handed in, 0 for none.</summary>
        public int WithinTicks { get; set; }

        /// <summary>The wanted piece must be one the character made at the anvil.</summary>
        public bool Made { get; set; }

        /// <summary>The notice's or the order's words, and the line said when it is handed in.</summary>
        public string TextKey => $"task.{Id}.text";
    }

    /// <summary>
    /// SLICE_TRIAL §3 (S5c.7), with no scene code: the Notice Board's, the Masters' and the faction's paid tasks. A task is
    /// taken by reading its notice, at its Master's counter, or from Ashgrave, and paid in bronze when it is handed in with its
    /// condition met; each is open once a run, and the next day's tasks come at 08:00. Edda offers the ore on day 1 and the
    /// jerkin from day 2; the faction's tasks are for members, one at a time, in the table's order. What the tasks need of a
    /// conversation lives in the world's keys <c>task.&lt;id&gt;.offer</c> and <c>.ready</c>, kept here, and a conversation
    /// raises <c>.taken</c> and <c>.handed</c>; the rest of the state is <c>task.&lt;id&gt;.*</c> too, so a save keeps it.
    /// </summary>
    public sealed class SliceTasks
    {
        public const string Board = "board";
        public const string Ashgrave = "ashgrave";
        public const string MemberKey = GameDev.Slice.Alchemy.SliceAlchemy.MemberKey;

        /// <summary>§3.3: the Lower Veil dwelling whose Stash holds the stolen censer, a tenement on Nettle Street.</summary>
        public const string CenserDwelling = "dwelling.960.140";

        public const string CenserItem = "censer";

        /// <summary>§3.3: the three warded doors in the Reek, walked past after 22:00 and before 06:00.</summary>
        public const int ChalkDoors = 3;

        public const int NightFrom = 22 * 60;
        public const int NightTo = 6 * 60;

        /// <summary>§3: the next day's tasks come at this minute of the day.</summary>
        public const int MorningMinute = 8 * 60;

        public const string DaceWordKey = "task.dace.word";

        private const int Day = 1440;

        public static readonly IReadOnlyList<PaidTask> All = new[]
        {
            new PaidTask("firewood", TaskGiver.Board, Board, 1, 600, "brothers", ("wood", 4)),
            new PaidTask("locket", TaskGiver.Board, Board, 1, 800, "widow", ("locket", 1)) { Gives = "locket", PickUpAt = "angel", NoticeStage = 9 },
            new PaidTask("letter", TaskGiver.Board, Board, 2, 1000, "farmer", ("letter_hamlet", 1)) { Gives = "letter_hamlet", PickUpAt = "customs" },
            new PaidTask("infirmary", TaskGiver.Board, Board, 2, 700, "brothers", ("herb", 5)),
            new PaidTask("edda.ore", TaskGiver.Master, "edda", 1, 1200, null, ("iron_ore", 3)) { LastDay = 1 },
            new PaidTask("edda.jerkin", TaskGiver.Master, "edda", 2, 2000, null, ("leather_body", 1)) { Made = true },
            new PaidTask("ada.herbs", TaskGiver.Master, "ada", 1, 1200, null, ("herb", 6)),
            new PaidTask("silas.fangs", TaskGiver.Master, "silas", 1, 1800, null, ("wolf_fang", 2), ("boar_tusk", 1)),
            new PaidTask("harrow.herbs", TaskGiver.Master, "harrow", 1, 1000, null, ("herb", 4), ("flax", 2)),
            new PaidTask("tobias.crate", TaskGiver.Master, "tobias", 1, 1000, "warehouse", ("crate", 1)) { Gives = "crate", WithinTicks = 30 },
            new PaidTask("censer", TaskGiver.Faction, Ashgrave, 1, 3000, null, (CenserItem, 1)),
            new PaidTask("chalk", TaskGiver.Faction, Ashgrave, 1, 2000, null),
            new PaidTask("dace", TaskGiver.Faction, Ashgrave, 2, 2500, null),
        };

        private readonly IWorldState _state;
        private readonly Bag _bag;
        private readonly Func<long> _tick;
        private bool _keeping;

        public SliceTasks(IWorldState state, Bag bag, Func<long> tick)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _bag = bag ?? throw new ArgumentNullException(nameof(bag));
            _tick = tick ?? (() => 0L);
            _state.Changed += OnChanged;
            _bag.Changed += Keep;
        }

        /// <summary>A task was handed in and paid: its id, and the bronze.</summary>
        public event Action<PaidTask> Paid;

        /// <summary>Feels the chalk walk unseen as Жрица's conduct (05.1): set by the world.</summary>
        public Action<string> Unnoticed { get; set; }

        public static PaidTask Find(string id) => All.FirstOrDefault(t => t.Id == id);

        public static string Key(string id, string part) => $"task.{id}.{part}";

        public bool Taken(string id) => _state.IsSet(Key(id, "taken"));

        public bool PaidOut(string id) => _state.IsSet(Key(id, "paid"));

        /// <summary>Ended unpaid: the crate came too late.</summary>
        public bool Failed(string id) => _state.IsSet(Key(id, "failed"));

        public bool Held(string id) => Taken(id) && !PaidOut(id) && !Failed(id);

        public bool Got(string id) => _state.IsSet(Key(id, "got"));

        public bool Noticed(string id) => _state.IsSet(Key(id, "noticed"));

        public void Detach()
        {
            _state.Changed -= OnChanged;
            _bag.Changed -= Keep;
        }

        /// <summary>Whether <paramref name="task"/> is offered at <paramref name="tick"/>: its day come, not taken, and its giver's rule kept.</summary>
        public bool Offered(PaidTask task, long tick)
        {
            if (task == null)
            {
                return false;
            }

            var day = (int)(tick / Day) + 1;
            var come = day > task.Day || (day == task.Day && (task.Day == 1 || tick % Day >= MorningMinute));
            if (Taken(task.Id) || !come || (task.LastDay > 0 && day > task.LastDay))
            {
                return false;
            }

            if (task.Giver != TaskGiver.Faction)
            {
                return true;
            }

            // §3.3: for members only (a keeper of the trial's cache is none), one at a time, in the table's order.
            if (!_state.IsSet(MemberKey) || All.Any(t => t.Giver == TaskGiver.Faction && Held(t.Id)))
            {
                return false;
            }

            return All.First(t => t.Giver == TaskGiver.Faction && !Taken(t.Id)) == task;
        }

        /// <summary>The tasks <paramref name="who"/> offers now.</summary>
        public IEnumerable<PaidTask> OfferedBy(string who, long tick) => All.Where(t => t.Who == who && Offered(t, tick));

        /// <summary>Takes <paramref name="id"/>: false when it is not offered.</summary>
        public bool Take(string id, long tick)
        {
            var task = Find(id);
            if (task == null || !Offered(task, tick))
            {
                return false;
            }

            _state.Raise(Key(id, "taken")); // OnChanged notes when, and gives what it gives
            return true;
        }

        /// <summary>Whether the task's condition is met, so it can be handed in now.</summary>
        public bool Ready(PaidTask task)
        {
            if (task == null || !Held(task.Id))
            {
                return false;
            }

            switch (task.Id)
            {
                case "chalk":
                    return _state.Get(Key("chalk", "doors")) >= ChalkDoors;
                case "dace":
                    return _state.IsSet(DaceWordKey);
            }

            foreach (var (item, count) in task.Wants)
            {
                if (task.Made ? MadePiece(item) == null : _bag.Count(item) < count)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Hands <paramref name="id"/> in: takes what it wants and pays it, or, for the crate come too late, takes the crate
        /// and pays nothing. The pay, 0 when late, or -1 when it cannot be handed in.
        /// </summary>
        public int HandIn(string id, long tick)
        {
            var task = Find(id);
            if (!Ready(task))
            {
                return -1;
            }

            foreach (var (item, count) in task.Wants)
            {
                if (task.Made)
                {
                    _bag.RemovePiece(MadePiece(item));
                }
                else
                {
                    _bag.Remove(item, count);
                }
            }

            if (task.WithinTicks > 0 && tick >= _state.Get(Key(id, "at")) - 1L + task.WithinTicks)
            {
                _state.Raise(Key(id, "failed"));
                Keep();
                return 0;
            }

            _bag.Earn(task.Pay);
            _state.Raise(Key(id, "paid"));
            Keep();
            Paid?.Invoke(task);
            return task.Pay;
        }

        /// <summary>Whether the crate's time is up: handed in now it pays nothing.</summary>
        public bool Late(string id, long tick)
        {
            var task = Find(id);
            return task != null && task.WithinTicks > 0 && Held(id) && tick >= _state.Get(Key(id, "at")) - 1L + task.WithinTicks;
        }

        /// <summary>The task whose item is picked up at <paramref name="spot"/> now, or null.</summary>
        public PaidTask PickUpAt(string spot) =>
            All.FirstOrDefault(t => t.PickUpAt == spot && Held(t.Id) && !Got(t.Id) && (t.NoticeStage == 0 || Noticed(t.Id)));

        /// <summary>The task handed in at <paramref name="spot"/> now, or null.</summary>
        public PaidTask HandInAt(string spot) => All.FirstOrDefault(t => t.HandAt == spot && Ready(t));

        /// <summary>The task whose item lies unnoticed at <paramref name="spot"/>, for the Perception check, or null.</summary>
        public PaidTask HiddenAt(string spot) => All.FirstOrDefault(t => t.PickUpAt == spot && t.NoticeStage > 0 && Held(t.Id) && !Noticed(t.Id));

        public void Notice(string id) => _state.Raise(Key(id, "noticed"));

        /// <summary>Picks the task's item up at its spot into the bag; false when it is not there or the bag cannot carry it.</summary>
        public bool PickUp(string id)
        {
            var task = Find(id);
            if (task == null || task.Gives == null || Got(id) || !_bag.Add(task.Gives))
            {
                return false;
            }

            _state.Raise(Key(id, "got"));
            Keep();
            return true;
        }

        /// <summary>The censer lies in its dwelling's Stash while the task is held and it was not taken yet.</summary>
        public bool CenserIn(string dwelling) => dwelling == CenserDwelling && Held("censer") && !Got("censer");

        /// <summary>Takes the censer from the Stash; false when it is not there or the bag cannot carry it.</summary>
        public bool TakeCenser(string dwelling)
        {
            if (!CenserIn(dwelling) || !_bag.Add(CenserItem))
            {
                return false;
            }

            _state.Raise(Key("censer", "got"));
            Keep();
            return true;
        }

        /// <summary>
        /// §3.3: the character walked past warded door <paramref name="door"/> (1–3) at <paramref name="tick"/>, seen or not
        /// by a Warden or a Veinmarket man; it counts once, at night and unseen. True when it counted.
        /// </summary>
        public bool PassDoor(int door, long tick, bool seen)
        {
            var minute = (int)(tick % Day);
            var night = minute >= NightFrom || minute < NightTo;
            if (!Held("chalk") || door < 1 || door > ChalkDoors || !night || seen || _state.IsSet(Key("chalk", $"door.{door}")))
            {
                return false;
            }

            _state.Raise(Key("chalk", $"door.{door}"));
            var doors = _state.Get(Key("chalk", "doors")) + 1;
            _state.Set(Key("chalk", "doors"), doors);
            if (doors >= ChalkDoors)
            {
                Unnoticed?.Invoke("task.chalk"); // Жрица: a watched place gone through unseen (05.1), once
            }

            Keep();
            return true;
        }

        /// <summary>Whether warded door <paramref name="door"/> has been walked past.</summary>
        public bool Passed(int door) => _state.IsSet(Key("chalk", $"door.{door}"));

        /// <summary>Keeps the keys a conversation reads, <c>.offer</c> and <c>.ready</c>; the world calls it each tick.</summary>
        public void Keep()
        {
            if (_keeping)
            {
                return;
            }

            _keeping = true;
            try
            {
                var tick = _tick();
                foreach (var task in All)
                {
                    _state.Set(Key(task.Id, "offer"), Offered(task, tick) ? 1 : 0);
                    _state.Set(Key(task.Id, "ready"), Ready(task) ? 1 : 0);
                }
            }
            finally
            {
                _keeping = false;
            }
        }

        private ItemPiece MadePiece(string item) => _bag.Pieces.FirstOrDefault(p => p.item == item && p.made);

        private void OnChanged(string key, int before, int after)
        {
            if (_keeping || before != 0 || after == 0 || !key.StartsWith("task.", StringComparison.Ordinal))
            {
                return;
            }

            foreach (var task in All)
            {
                if (key == Key(task.Id, "taken"))
                {
                    _state.Set(Key(task.Id, "at"), (int)_tick() + 1);
                    if (task.Gives != null && task.PickUpAt == null && _bag.Add(task.Gives))
                    {
                        _state.Raise(Key(task.Id, "got"));
                    }

                    Keep();
                    return;
                }

                if (key == Key(task.Id, "handed"))
                {
                    HandIn(task.Id, _tick()); // a conversation handed it in (Ashgrave's)
                    return;
                }
            }

            if (key == DaceWordKey)
            {
                Keep();
            }
        }
    }
}
