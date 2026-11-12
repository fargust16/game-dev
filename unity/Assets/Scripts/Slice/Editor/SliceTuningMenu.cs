using UnityEditor;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The owner's tuning page in the Inspector at one click (owner, 2026-11-12): GameDev → Slice → Tuning selects
    /// <c>Assets/Resources/SliceTuning.asset</c>, in Play mode too, where a change shows at once and stays after it.
    /// </summary>
    public static class SliceTuningMenu
    {
        public const string AssetPath = "Assets/Resources/" + SliceTuning.ResourceName + ".asset";

        [MenuItem("GameDev/Slice/Tuning")]
        public static void Open()
        {
            var page = AssetDatabase.LoadAssetAtPath<SliceTuning>(AssetPath);
            if (page == null)
            {
                EditorUtility.DisplayDialog("Tuning", $"{AssetPath} is missing.", "OK");
                return;
            }

            Selection.activeObject = page;
            EditorGUIUtility.PingObject(page);
        }
    }
}
