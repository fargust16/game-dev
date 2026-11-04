using UnityEngine;

namespace GameDev.World.Timekeeping
{
    /// <summary>Immutable clock numbers. Changing the pace or the start changes only this asset.</summary>
    [CreateAssetMenu(menuName = "GameDev/Time/Game Clock Settings", fileName = "GameClockSettings")]
    public sealed class GameClockSettings : ScriptableObject
    {
        [Tooltip("18 World Time §2: one tick is one game minute, 10 real seconds at the initial, owner-approved pace.")]
        [SerializeField, Min(0.01f)] private float realSecondsPerTick = 10f;

        [Tooltip("Day the run starts on, from 1. Authoring default: day 1.")]
        [SerializeField, Min(1)] private int startDay = 1;

        [Tooltip("Minute of that day the run starts at. Authoring default: 480, 08:00, when the Masters open (11, 18).")]
        [SerializeField, Range(0, GameClock.MinutesPerDay - 1)] private int startMinuteOfDay = 480;

        public float RealSecondsPerTick => realSecondsPerTick;
        public long StartTick => (startDay - 1) * (long)GameClock.MinutesPerDay + startMinuteOfDay;
    }
}
