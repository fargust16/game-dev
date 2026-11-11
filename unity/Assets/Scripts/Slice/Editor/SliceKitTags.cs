using System.IO;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// S3a.7: puts each catalogue entry's 10.1 tag (<see cref="SliceKitCatalogue.Spec.Action"/>) on its wrappers already
    /// built, without building the kit again, so the Combat Grid offers the Environment Action wherever the block stands.
    /// A wrapper built later takes its tag from the catalogue itself.
    /// </summary>
    public static class SliceKitTags
    {
        [MenuItem("Tools/GameDev/Slice/Kit/Tag Environment Actions")]
        public static void Run()
        {
            var tagged = 0;
            foreach (var spec in SliceKitCatalogue.Specs())
            {
                if (!(spec.Action is GameDev.Confront.Grid.EnvironmentActionKind action))
                {
                    continue;
                }

                var folder = $"{SliceKitCatalogue.Folder}/{spec.Family}";
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                foreach (var guid in AssetDatabase.FindAssets($"Kit_{spec.Family}_{spec.Part}_ t:Prefab", new[] { folder }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var name = Path.GetFileNameWithoutExtension(path);
                    if (!name.StartsWith($"Kit_{spec.Family}_{spec.Part}_"))
                    {
                        continue;
                    }

                    var root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        if (!root.TryGetComponent(out KitEnvironmentAction tag))
                        {
                            tag = root.AddComponent<KitEnvironmentAction>();
                        }

                        tag.Set(action, spec.Band);
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        tagged++;
                        Debug.Log($"[KitTags] {name}: {action}, {spec.Band}.");
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[KitTags] {tagged} wrappers tagged.");
        }
    }
}
