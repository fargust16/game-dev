using GameDev.Core.Save;
using UnityEngine;

namespace GameDev.World.Timekeeping
{
    /// <summary>The clock's section of a save (17 X.1, X.3): the whole tick and the progress toward the next one.</summary>
    public sealed class GameClockSaveContributor : ISaveContributor
    {
        private readonly GameClock _clock;

        public GameClockSaveContributor(GameClock clock)
        {
            _clock = clock;
        }

        public string SectionKey => SaveSchema.WorldClock;

        public int SectionVersion => 1;

        public string CapturePayload() => JsonUtility.ToJson(_clock.Capture());

        public void RestorePayload(int version, string payload)
        {
            _clock.Restore(JsonUtility.FromJson<GameClockState>(payload));
        }
    }
}
