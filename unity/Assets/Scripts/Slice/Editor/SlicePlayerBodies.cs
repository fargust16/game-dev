using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The bodies offered at creation (07 *Creation choices in V1*, owner 2026-11-13, S5-4; S5b.1): three of each sex. The
    /// owner makes the player's own models in Tripo; until then the choice offers the slice's present Tripo people, the
    /// player's two bodies and two townsfolk of each sex. Each look is a player prefab in its body, at its stature,
    /// playing the player's controller of its sex, the tools in its right hand (<see cref="SliceTools.Arm"/>); the
    /// settings' list names them in order, men first.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.SlicePlayerBodies.Run
    /// </summary>
    public static class SlicePlayerBodies
    {
        public const string Folder = "Assets/Prefabs/Slice/PlayerBodies";
        private const string Output = "Logs/SliceSurvey";

        /// <summary>The looks offered: the id kept in the save, the look, the player prefab that wears it.</summary>
        public static readonly (string Id, SlicePeopleModels.Look Look, string Path)[] Offered =
        {
            ("man", SlicePeopleModels.PlayerMan, SliceTestScenes.PlayerPrefabPath),
            ("gentleman", SlicePeopleModels.Gentleman, Folder + "/SlicePlayer_Gentleman.prefab"),
            ("dockworker", SlicePeopleModels.Dockworker, Folder + "/SlicePlayer_Dockworker.prefab"),
            ("woman", SlicePeopleModels.PlayerWoman, SlicePeopleModels.PlayerWomanPrefab),
            ("lady", SlicePeopleModels.Lady, Folder + "/SlicePlayer_Lady.prefab"),
            ("working_woman", SlicePeopleModels.WorkingWoman, Folder + "/SlicePlayer_WorkingWoman.prefab"),
        };

        /// <summary>The prefabs this step makes: every look but the player's two bodies, which have their own.</summary>
        public static bool Made(string path) => path.StartsWith(Folder + "/");

        public static void Run()
        {
            var report = new StringBuilder();
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(Folder).Replace('\\', '/'), Path.GetFileName(Folder));
            }

            foreach (var (_, look, path) in Offered.Where(o => Made(o.Path)))
            {
                var root = PrefabUtility.LoadPrefabContents(SliceTestScenes.PlayerPrefabPath);
                root.name = Path.GetFileNameWithoutExtension(path);
                report.AppendLine($"{path}: {SlicePeopleModels.Wear(root, look)}");
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            SliceTools.Measure(report);
            foreach (var (_, _, path) in Offered.Where(o => Made(o.Path)))
            {
                SliceTools.Arm(path, report);
            }

            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SliceConfrontSetup.SettingsPath);
            var so = new SerializedObject(settings);
            var list = so.FindProperty("playerLooks");
            list.arraySize = Offered.Length;
            for (var i = 0; i < Offered.Length; i++)
            {
                var (id, look, path) = Offered[i];
                var element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").stringValue = id;
                element.FindPropertyRelative("female").boolValue = look.Female;
                element.FindPropertyRelative("player").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                report.AppendLine($"look {i}: {id}, {(look.Female ? "woman" : "man")}, {path}");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Output);
            File.WriteAllText($"{Output}/player_bodies.txt", report.ToString());
            Debug.Log("[SlicePlayerBodies]\n" + report);
        }
    }
}
