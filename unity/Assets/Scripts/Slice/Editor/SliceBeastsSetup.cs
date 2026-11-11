using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Beasts;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// S3b.2, content piece C3 (11; 10.2 *Beasts of the slice*): the wolves and boars of Veilmouth's territories, in the
    /// owner's packs (asset map: Wolf_URP and Wild_Boar). It copies the packs' clips into looping project clips, builds one
    /// controller for each kind with the states <see cref="SliceBeast"/> plays, saves a project prefab for each kind, and
    /// replaces the dragon stand-ins of the city plan's territories with them: six wolf packs, of three and two in turn,
    /// and four boars alone. The seed decides which glow when the slice runs.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.SliceBeastsSetup.BuildAndPlace
    /// </summary>
    public static class SliceBeastsSetup
    {
        public const string WolfPrefabPath = "Assets/Prefabs/Slice/Wolf.prefab";
        public const string BoarPrefabPath = "Assets/Prefabs/Slice/Boar.prefab";
        public const string GroupName = "Beasts";
        public const string TerritoriesName = "Beast Territories";
        private const string Folder = "Assets/Settings/Slice/Beasts";
        private const string WolfModel = "Assets/Wolf/URP/Wolf/Prefab/Wolf_URP.prefab";
        private const string WolfClips = "Assets/Wolf/Models/Wolf.fbx";
        private const string BoarModel = "Assets/Wild Boar/Prefabs/URP/HD/Wild_Boar.prefab";
        private const string BoarClips = "Assets/Wild Boar/Animals/Wild_Boar/Animations";

        // Authoring defaults (the city plan's stand-ins): a wolf 1.6 m long and a boar 1.5 m.
        private const float WolfLength = 1.6f;
        private const float BoarLength = 1.5f;
        public const int WolfPacks = 6;

        // A wolf pack's places round its den, in metres east and north, with each wolf's facing (city plan's stand-ins).
        private static readonly (float East, float North, float Yaw)[] Den = { (0f, 0f, 30f), (2.5f, -1.5f, 80f), (-1.5f, -2.5f, 200f) };

        // The states SliceBeast plays, from each pack's clips; the walk, run and rest loop.
        private static readonly (string State, string Wolf, string Boar, bool Loop)[] States =
        {
            ("Idle", "Idle_New (1)", "Idle", true),
            ("Walk", "Walking", "Walking", true),
            ("Run", "Running_New (1)", "Run", true),
            ("Attack", "AttackL (1)", "Attack_Bite_Forward_Lunge_Tear", false),
            ("Hit", "GetHit_1", "Get_Hit", false),
            ("Dead", "dead (1)", "Dying", false),
            ("Warn", "WolfHowl_New (1)", "Attack_Charge_Gore", false),
        };

        [MenuItem("Game Dev/Slice/Beasts: Build and Place")]
        public static void BuildAndPlace()
        {
            Build();
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var city = scene.GetRootGameObjects().FirstOrDefault(g => g.name == VeilmouthCity.RootName);
            if (city == null)
            {
                Debug.LogError($"[SliceBeastsSetup] Veilmouth has no {VeilmouthCity.RootName}.");
                return;
            }

            var placed = Populate(city.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SliceBeastsSetup] {placed} beasts placed in Veilmouth's territories.");
        }

        /// <summary>The clips, the two controllers and the two prefabs.</summary>
        public static void Build()
        {
            Directory(Folder);
            Directory("Assets/Prefabs/Slice");
            var wolfClips = AssetDatabase.LoadAllAssetsAtPath(WolfClips).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToDictionary(c => c.name);
            var wolf = Controller("Wolf", States.ToDictionary(s => s.State, s => Copy("Wolf", s.State, wolfClips[s.Wolf], s.Loop)));
            var boar = Controller("Boar", States.ToDictionary(s => s.State,
                s => Copy("Boar", s.State, AssetDatabase.LoadAssetAtPath<AnimationClip>($"{BoarClips}/{s.Boar}.anim"), s.Loop)));
            Prefab(WolfModel, WolfLength, wolf, BeastKind.Wolf, WolfPrefabPath);
            Prefab(BoarModel, BoarLength, boar, BeastKind.Boar, BoarPrefabPath);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Replaces the stand-ins and any beasts placed before with the beasts of each territory under the city's root;
        /// returns how many stand.
        /// </summary>
        public static int Populate(Transform city)
        {
            foreach (var t in city.GetComponentsInChildren<Transform>(true).Where(t => t != null && t.name.Contains("(dragon stand-in)")).ToList())
            {
                if (t != null)
                {
                    Object.DestroyImmediate(t.gameObject);
                }
            }

            var old = city.Find(GroupName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var territories = city.Find(TerritoriesName);
            if (territories == null)
            {
                Debug.LogError($"[SliceBeastsSetup] {city.name} has no {TerritoriesName}.");
                return 0;
            }

            Physics.SyncTransforms();
            var group = new GameObject(GroupName).transform;
            group.SetParent(city, false);
            var wolf = AssetDatabase.LoadAssetAtPath<GameObject>(WolfPrefabPath);
            var boar = AssetDatabase.LoadAssetAtPath<GameObject>(BoarPrefabPath);
            var count = 0;
            for (var i = 0; i < WolfPacks; i++)
            {
                var den = territories.Find($"Wolf Territory {i + 1}");
                if (den == null)
                {
                    continue;
                }

                var size = i % 2 == 0 ? 3 : 2; // 10.2: packs of two or three
                for (var k = 0; k < size; k++)
                {
                    var (east, north, yaw) = Den[k];
                    Place(wolf, group, $"wolf.{i + 1}.{(char)('a' + k)}", BeastKind.Wolf, i, den.position, den.position + new Vector3(east, 0f, north), yaw);
                    count++;
                }
            }

            for (var j = 0; ; j++)
            {
                var wallow = territories.Find($"Boar Territory {j + 1}");
                if (wallow == null)
                {
                    break;
                }

                Place(boar, group, $"boar.{j + 1}", BeastKind.Boar, WolfPacks + j, wallow.position, wallow.position, 45f * j);
                count++;
            }

            return count;
        }

        private static void Place(GameObject prefab, Transform group, string id, BeastKind kind, int pack, Vector3 home, Vector3 at, float yaw)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
            go.name = $"{(kind == BeastKind.Wolf ? "Wolf" : "Boar")} {id}";
            var ground = ~LayerMask.GetMask(KitLayers.Character, KitLayers.NoGrid);
            if (Physics.Raycast(at + (Vector3.up * 40f), Vector3.down, out var hit, 80f, ground, QueryTriggerInteraction.Ignore))
            {
                at.y = hit.point.y;
            }

            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            go.GetComponent<SliceBeast>().Configure(id, kind, pack, home);
            EditorUtility.SetDirty(go.GetComponent<SliceBeast>());
        }

        /// <summary>A pack's clip as a project clip that loops or not, so the walk, run and rest repeat.</summary>
        private static AnimationClip Copy(string kind, string state, AnimationClip clip, bool loop)
        {
            if (clip == null)
            {
                throw new System.InvalidOperationException($"[SliceBeastsSetup] {kind} has no clip for {state}.");
            }

            var path = $"{Folder}/{kind}_{state}.anim";
            AssetDatabase.DeleteAsset(path);
            var copy = Object.Instantiate(clip);
            copy.name = $"{kind}_{state}";
            var settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(copy, settings);
            AssetDatabase.CreateAsset(copy, path);
            return copy;
        }

        /// <summary>One state for each clip; the strike, the hit and the warning go back to the rest at their end.</summary>
        private static RuntimeAnimatorController Controller(string kind, Dictionary<string, AnimationClip> clips)
        {
            var path = $"{Folder}/{kind}.controller";
            AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            var states = new Dictionary<string, AnimatorState>();
            foreach (var (state, _, _, _) in States)
            {
                var s = machine.AddState(state);
                s.motion = clips[state];
                states[state] = s;
            }

            foreach (var once in new[] { "Attack", "Hit", "Warn" })
            {
                var back = states[once].AddTransition(states["Idle"]);
                back.hasExitTime = true;
                back.exitTime = 0.95f;
                back.duration = 0.15f;
            }

            machine.defaultState = states["Idle"];
            return controller;
        }

        private static void Prefab(string model, float length, RuntimeAnimatorController controller, BeastKind kind, string path)
        {
            // The pack's model under a root of our own at its feet, so a beast stands where it is placed.
            var body = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(model));
            PrefabUtility.UnpackPrefabInstance(body, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            body.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var size = VeilmouthDressing.Seen(body).size;
            body.transform.localScale = Vector3.one * (length / Mathf.Max(0.01f, Mathf.Max(size.x, size.z)));
            VeilmouthDressing.Stand(body, 0f);
            var root = new GameObject(kind.ToString());
            body.transform.SetParent(root.transform, true);
            SliceKitCatalogue.SetLayer(root, KitLayers.Character);
            var animator = root.GetComponentInChildren<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            if (root.GetComponent<SliceBeast>() == null)
            {
                root.AddComponent<SliceBeast>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void Directory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            Directory(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
