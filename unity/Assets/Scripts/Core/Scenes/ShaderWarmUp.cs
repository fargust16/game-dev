using System;
using System.Diagnostics;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace GameDev.Core.Scenes
{
    /// <summary>
    /// Builds the shaders' pipelines before the first scene loads (Plans/VEILMOUTH_REFERENCE_PLAN.md R1.5). A
    /// development build traces the shader variants and graphics states it draws into a
    /// <see cref="GraphicsStateCollection"/>, kept in <c>Resources/GraphicsStates</c>, one per graphics API and platform.
    /// Every player starts warming the collection that fits its graphics API on worker threads before its first
    /// scene loads, so Direct3D 12 builds the pipelines behind the veil of the cold load instead of on a door's first
    /// frame. A development build also records the states the collection lacked (<see cref="Collection"/>'s
    /// <c>cacheMissCollection</c>). <c>-noShaderWarmUp</c> on the command line turns it off for comparison; the editor
    /// compiles its shaders its own way and never warms.
    /// </summary>
    public static class ShaderWarmUp
    {
        public const string Folder = "GraphicsStates";
        public const string Off = "-noShaderWarmUp";

        private static readonly Stopwatch Watch = new();
        private static GraphicsStateCollection _collection;
        private static JobHandle _job;
        private static double _seconds = -1d;

        /// <summary>The collection being warmed or warmed, or null when none fits or warming is off.</summary>
        public static GraphicsStateCollection Collection => _collection;

        /// <summary>True once the warming has finished; false while it runs or when there is none.</summary>
        public static bool IsDone => _seconds >= 0d;

        /// <summary>The real time the warming took, s, once done; otherwise -1.</summary>
        public static double Seconds => _seconds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _collection = null;
            _job = default;
            _seconds = -1d;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Begin()
        {
            if (Application.isEditor || Array.IndexOf(Environment.GetCommandLineArgs(), Off) >= 0)
            {
                return;
            }

            _collection = Pick(Resources.LoadAll<GraphicsStateCollection>(Folder));
            if (_collection == null)
            {
                return;
            }

            Watch.Restart();
            _job = _collection.WarmUp(default, Debug.isDebugBuild);
            var watcher = new GameObject("Shader Warm-Up") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(watcher);
            watcher.AddComponent<Watcher>();
        }

        /// <summary>The collection for this graphics API and platform, the one traced at this quality level first.</summary>
        private static GraphicsStateCollection Pick(GraphicsStateCollection[] all)
        {
            var quality = QualitySettings.names[QualitySettings.GetQualityLevel()];
            GraphicsStateCollection fit = null;
            foreach (var c in all)
            {
                if (c.graphicsDeviceType != SystemInfo.graphicsDeviceType || c.runtimePlatform != Application.platform)
                {
                    continue;
                }

                if (c.qualityLevelName == quality)
                {
                    return c;
                }

                fit ??= c;
            }

            return fit;
        }

        /// <summary>Notes the end of the warming once its job has finished, then removes itself.</summary>
        private sealed class Watcher : MonoBehaviour
        {
            private void Update()
            {
                if (!_job.IsCompleted)
                {
                    return;
                }

                _job.Complete();
                _seconds = Watch.Elapsed.TotalSeconds;
                Debug.Log($"[ShaderWarmUp] {_collection.completedWarmupCount} of {_collection.totalGraphicsStateCount} graphics states " +
                          $"for {_collection.variantCount} shader variants warmed in {_seconds:0.00} s ({_collection.name})");
                Destroy(gameObject);
            }
        }
    }
}
