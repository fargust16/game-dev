using System;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>What an Alarm is (10.2 §5e): a shot, a Call for Help, or a Warden's whistle.</summary>
    public enum AlarmKind
    {
        Shot,
        Call,
        Whistle,
    }

    /// <summary>One Alarm: its kind, its point, and who raised it, the shooter, the caller or the Warden.</summary>
    public readonly struct Alarm
    {
        public Alarm(AlarmKind kind, Vector3 at, Transform source)
        {
            Kind = kind;
            At = at;
            Source = source;
        }

        public AlarmKind Kind { get; }

        public Vector3 At { get; }

        public Transform Source { get; }
    }

    /// <summary>
    /// 10.2 §5e, the Alarm (owner, 2026-11-12; PB8a): one event at one point, heard by everyone within 30 cells of it,
    /// through walls as well. Whoever hears it knows where it came from. It is no noise simulation, footsteps and every
    /// other sound staying unsimulated, and not Alert, which follows sight (10.3 §3). Every Alarm is raised here, and every
    /// kind of hearer answers it by its own part of the one Alarm Check: the Wardens in <see cref="SliceConfront"/>.
    /// </summary>
    public static class SliceAlarm
    {
        /// <summary>Authoring default, 10.2 §5e: how far an Alarm is heard, in 1 m cells counted as moves are.</summary>
        public const int Cells = 30;

        /// <summary>Raised with every Alarm, for every kind of hearer.</summary>
        public static event Action<Alarm> Raised;

        /// <summary>The last Alarm raised, or null.</summary>
        public static Alarm? Last { get; private set; }

        /// <summary>Whether a person at <paramref name="listener"/> hears an Alarm at <paramref name="at"/>: within 30 cells either way or across.</summary>
        public static bool Hears(Vector3 at, Vector3 listener) => Mathf.Max(Mathf.Abs(listener.x - at.x), Mathf.Abs(listener.z - at.z)) <= Cells;

        public static void Raise(Alarm alarm)
        {
            Last = alarm;
            Raised?.Invoke(alarm);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Raised = null;
            Last = null;
        }
    }
}
