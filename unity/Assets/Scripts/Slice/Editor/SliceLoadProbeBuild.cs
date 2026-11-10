using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
        public const string FramesOutput = "Builds/FrameProbe/Veilmouth.exe";

        public static void Build() => Build(Output);

        /// <summary>
        /// The same build for the frame diagnostic (<c>GameDev.Slice.Diagnostics.FrameProbe</c>, city plan §29.7), to
        /// <c>Builds/FrameProbe</c>: the GPU Resident Drawer draws with the shaders' DOTS variants, which the build keeps
        /// only with the Graphics settings' "BatchRendererGroup Variants" at Keep All, and its compute shaders only with
        /// the drawer on in the pipeline asset; so both are set for the build and put back after it, the pipeline asset's
        /// file as it was (a build saves the asset), and the player switches the drawer off and on itself. Run it with
        /// <c>-frameTiming &lt;report file&gt;</c>.
        /// </summary>
        public static void BuildForFrames()
        {
            var settings = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var strip = settings.FindProperty("m_BrgStripping");
            var was = strip.intValue;
            var urp = (UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline;
            var (mode, occlusion) = (urp.gpuResidentDrawerMode, urp.gpuResidentDrawerEnableOcclusionCullingInCameras);
            var path = AssetDatabase.GetAssetPath(urp);
            var file = File.ReadAllBytes(path);
            strip.intValue = (int)BatchRendererGroupStrippingMode.KeepAll;
            settings.ApplyModifiedPropertiesWithoutUndo();
            urp.gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing;
            urp.gpuResidentDrawerEnableOcclusionCullingInCameras = true;
            try
            {
                Build(FramesOutput);
            }
            finally
            {
                urp.gpuResidentDrawerMode = mode;
                urp.gpuResidentDrawerEnableOcclusionCullingInCameras = occlusion;
                File.WriteAllBytes(path, file);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                strip.intValue = was;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void Build(string output)
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
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            });
            var s = report.summary;
            Debug.Log($"[SliceLoadProbeBuild] {s.result}: {scenes.Length} scenes, {s.totalSize / 1e6:0} MB, {s.totalErrors} errors, in {s.totalTime:hh\\:mm\\:ss} to {output}");
            if (s.result != BuildResult.Succeeded && Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
