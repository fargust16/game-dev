using GameDev.Slice.Fights;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// 10.2 §5e (PB8a): an Alarm, a shot, a Call for Help or a whistle, is heard within 30 cells of its point, counted as
    /// moves are, through walls and floors, and every kind of hearer is told of it.
    /// </summary>
    public sealed class SliceAlarmTests
    {
        [Test]
        public void AnAlarmIsHeardWithinThirtyCellsCountedAsMovesAre()
        {
            var at = new Vector3(10f, 2f, -4f);
            Assert.IsTrue(SliceAlarm.Hears(at, at + new Vector3(30f, 0f, 0f)), "thirty cells along");
            Assert.IsTrue(SliceAlarm.Hears(at, at + new Vector3(-30f, 0f, 30f)), "thirty cells across, as moves count them");
            Assert.IsTrue(SliceAlarm.Hears(at, at + new Vector3(5f, 12f, 5f)), "upstairs or below, the height not counted");
            Assert.IsFalse(SliceAlarm.Hears(at, at + new Vector3(30.5f, 0f, 0f)), "beyond thirty cells");
            Assert.IsFalse(SliceAlarm.Hears(at, at + new Vector3(12f, 0f, -31f)), "beyond thirty cells across");
        }

        [Test]
        public void EveryHearerIsToldOfAnAlarm()
        {
            Alarm? told = null;
            void Listen(Alarm a) => told = a;
            SliceAlarm.Raised += Listen;
            try
            {
                var alarm = new Alarm(AlarmKind.Whistle, new Vector3(1f, 0f, 2f), null);
                SliceAlarm.Raise(alarm);
                Assert.IsTrue(told.HasValue, "told");
                Assert.AreEqual(AlarmKind.Whistle, told.Value.Kind, "of its kind");
                Assert.AreEqual(alarm.At, told.Value.At, "and its point");
                Assert.IsTrue(SliceAlarm.Last.HasValue && SliceAlarm.Last.Value.Kind == AlarmKind.Whistle, "the last Alarm kept");
            }
            finally
            {
                SliceAlarm.Raised -= Listen;
            }
        }
    }
}
