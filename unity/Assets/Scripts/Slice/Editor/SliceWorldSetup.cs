using System.Linq;
using GameDev.World.Poi;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Puts the slice's run-time world (S1.3) into scenes: the world driver with the POIs and tips, and the Notice
    /// Board as a source the player reads. Veilmouth is the owner's scene, so it is changed only by the one-shot
    /// entry below, which adds these two things and touches nothing else.
    /// </summary>
    public static class SliceWorldSetup
    {
        private const string VeilmouthPath = "Assets/Scenes/Slice/Veilmouth.unity";
        private const string SettingsPath = "Assets/Data/Slice/SliceSettings.asset";
        private const string PoiFolder = "Assets/Data/Slice/Poi/";
        private const string BoardName = "Notice Board";

        /// <summary>Authoring default, m: reach from the board's pivot.</summary>
        private const float BoardReach = 2.5f;

        /// <summary>A world driver in the open scene, set with the slice's assets.</summary>
        internal static SliceWorldDriver AddWorld()
        {
            var driver = new GameObject("SliceWorld").AddComponent<SliceWorldDriver>();
            Set(driver);
            return driver;
        }

        /// <summary>The driver's assets: the settings, the POIs, the tips, and the city plan C4 for the map (S1.6, city plan §28.5).</summary>
        private static void Set(SliceWorldDriver driver)
        {
            driver.Configure(
                AssetDatabase.LoadAssetAtPath<SliceSettings>(SettingsPath),
                new[] { "PoiA", "PoiB", "PoiC" }.Select(n => AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiFolder + n + ".asset")).ToArray(),
                AssetDatabase.LoadAssetAtPath<TipCatalogue>(PoiFolder + "Tips.asset"),
                AssetDatabase.LoadAssetAtPath<TextAsset>(PlanPath));
            EditorUtility.SetDirty(driver);
        }

        private const string PlanPath = "Assets/Data/Slice/Map/VeilmouthC5Plan.xml";

        /// <summary>Adds what Veilmouth lacks of the world, and sets the existing driver's assets again (S1.6 added the plan).</summary>
        [MenuItem("GameDev/Slice/World/Add the Slice World and the Notice Board to Veilmouth (once)")]
        public static void AddToVeilmouth()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthPath, OpenSceneMode.Single);
            var existing = Object.FindAnyObjectByType<SliceWorldDriver>();
            if (existing == null)
            {
                AddWorld();
            }
            else
            {
                Set(existing);
            }

            var board = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == BoardName);
            if (board == null)
            {
                Debug.LogError($"Veilmouth has no '{BoardName}'.");
                return;
            }

            if (board.GetComponent<TipSourcePoint>() == null)
            {
                board.gameObject.AddComponent<TipSourcePoint>().Configure(TipSource.NoticeBoard, "source.board.title", "source.board.nothing", BoardReach);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"SliceWorldSetup: the world and the board's tips are in Veilmouth; the board stands at {board.position}.");
        }
    }
}
