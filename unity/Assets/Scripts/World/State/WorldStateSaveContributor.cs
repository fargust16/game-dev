using GameDev.Core.Save;
using UnityEngine;

namespace GameDev.World.State
{
    /// <summary>The world state's section of a save (17 X.3): every value that is not zero, by key.</summary>
    public sealed class WorldStateSaveContributor : ISaveContributor
    {
        private readonly WorldState _state;

        public WorldStateSaveContributor(WorldState state)
        {
            _state = state;
        }

        public string SectionKey => SaveSchema.WorldState;

        public int SectionVersion => 1;

        public string CapturePayload() => JsonUtility.ToJson(_state.Capture());

        public void RestorePayload(int version, string payload)
        {
            _state.Restore(JsonUtility.FromJson<WorldStateEntries>(payload));
        }
    }
}
