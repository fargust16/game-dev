#if UNITY_EDITOR
using System;
using GameDev.Grid;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Random = System.Random;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Opens Phase B of the Combat Grid technical test in Play Mode. Everything it builds lives in
    /// memory, and the scene is reloaded from disk on leaving Play Mode, so nothing is saved.
    /// </summary>
    public static class CombatGridLabMenu
    {
        private const string ActiveKey = "CombatGridLab.Active";
        private const string SmokeArg = "-gridLabOut";
        private const int MinOpenCells = 1000;

        [MenuItem("GameDev/Combat Grid Test/Play Lab")]
        public static void PlayLab()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Prepare("");
            SessionState.SetBool(ActiveKey, true);
            EditorApplication.EnterPlaymode();
        }

        /// <summary>Batch check. Must run without <c>-quit</c>: the lab exits the process itself.</summary>
        public static void RunSmoke()
        {
            var args = System.Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, SmokeArg);
            if (index < 0 || index + 1 >= args.Length)
            {
                throw new ArgumentException("Pass " + SmokeArg + " <folder>.");
            }

            Prepare(args[index + 1]);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void WatchPlayMode()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(ActiveKey, false))
                {
                    SessionState.EraseBool(ActiveKey);
                    EditorSceneManager.OpenScene(CombatGridProbe.OverworldScene, OpenSceneMode.Single);
                }
            };
        }

        private static void Prepare(string smokeFolder)
        {
            EditorSceneManager.OpenScene(CombatGridProbe.OverworldScene, OpenSceneMode.Single);
            var town = CombatGridProbe.RebuildTown();
            CombatGridProbe.AddMissingColliders(town);
            Physics.SyncTransforms();

            var player = GameObject.Find("OverworldPlayer");
            if (player == null)
            {
                throw new InvalidOperationException("The overworld scene has no player.");
            }

            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = FindOpenCellNear(town) + Vector3.up * 0.1f;
            controller.enabled = true;
            Physics.SyncTransforms();

            var shells = town.transform.Find(CombatGridProbe.ShellsName);
            new GameObject("CombatGridLab").AddComponent<CombatGridLab>().Configure(
                player.transform,
                shells,
                smokeFolder);
        }

        private static Vector3 FindOpenCellNear(GameObject town)
        {
            var renderers = town.GetComponentsInChildren<MeshRenderer>();
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var rng = new Random(20261102);
            for (var tries = 0; tries < 400; tries++)
            {
                var x = bounds.min.x + (float)rng.NextDouble() * bounds.size.x;
                var z = bounds.min.z + (float)rng.NextDouble() * bounds.size.z;
                var reader = new CombatGridReader(1f);
                if (reader.TryFindStart(new Vector3(x, 0f, z), out var ix, out var iz, out var level)
                    && reader.Build(ix, iz, level).Reachable.Count >= MinOpenCells)
                {
                    return reader.Center(ix, iz, reader.Column(ix, iz)[level].Y);
                }
            }

            throw new InvalidOperationException("No open cell near the town.");
        }
    }
}
#endif
