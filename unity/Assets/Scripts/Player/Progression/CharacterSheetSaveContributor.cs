using GameDev.Core.Save;
using UnityEngine;

namespace GameDev.Player.Progression
{
    /// <summary>The player's sheet in a save (17 X.3): the player.character section, every value by name.</summary>
    public sealed class CharacterSheetSaveContributor : ISaveContributor
    {
        private readonly CharacterSheet _sheet;

        public CharacterSheetSaveContributor(CharacterSheet sheet)
        {
            _sheet = sheet;
        }

        public string SectionKey => SaveSchema.PlayerCharacter;

        public int SectionVersion => 1;

        public string CapturePayload() => JsonUtility.ToJson(_sheet.Capture());

        public void RestorePayload(int version, string payload)
        {
            _sheet.Restore(JsonUtility.FromJson<CharacterSheetState>(payload));
        }
    }
}
