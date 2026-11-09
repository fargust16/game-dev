using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// A development build for the scene load diagnostic (Plans/VEILMOUTH_REFERENCE_PLAN.md R1.4): an empty first
    /// scene, Veilmouth and every interior, built to <c>Builds/LoadProbe</c> (ignored by git). The project's build
    /// settings are left as they are. Run it with <c>-sceneLoadTiming &lt;report file&gt;</c> and the player measures
    /// itself (<c>GameDev.Slice.Diagnostics.SceneLoadProbe</c>) and quits.
    /// <code>Unity.exe -batchmode -quit -projectPath unity -executeMethod GameDev.Slice.EditorTools.SliceLoadProbeBuild.Build</code>
    /// </summary>
    public static class SliceLoadProbeBuild
    {
        public const string Boot = "Assets/Scenes/Slice/LoadProbeBoot.unity";
        public const string Output = "Builds/LoadProbe/Veilmouth.exe";

        public static void Build()
        {
            if (!File.Exists(Boot))
            {
                var empty = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(empty, Boot);
            }

            var scenes = new[] { Boot, "Assets/Scenes/Slice/Veilmouth.unity" }
                .Concat(Directory.GetFiles("Assets/Scenes/Slice/Interiors", "*.unity").Select(p => p.Replace('\\', '/')).OrderBy(p => p))
                .ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            });
            var s = report.summary;
            Debug.Log($"[SliceLoadProbeBuild] {s.result}: {scenes.Length} scenes, {s.totalSize / 1e6:0} MB, {s.totalErrors} errors, in {s.totalTime:hh\\:mm\\:ss} to {Output}");
            if (s.result != BuildResult.Succeeded && Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
