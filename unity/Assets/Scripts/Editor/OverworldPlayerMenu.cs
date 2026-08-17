#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using GameDev.Player;
using GameDev.World;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.Settlement;
using GameDev.World.Terrain;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WorldSeed = GameDev.World.Seed.WorldSeed;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Wires the Kevin Iglesias Human Character Dummy prefab up to Human Basic Motions locomotion
    /// clips (see ASSET_LIBRARY_REFERENCE.md - locked default for Locomotion V0) so it can
    /// walk/run/sprint/jump around the Overworld terrain: ensure the Dummy FBX is a Humanoid rig,
    /// build a locomotion Animator Controller (Idle/Walk/Run/Sprint 1D blend tree + a
    /// Jump-Begin/Fall/Land sub-sequence) from the Male clip set, then spawn the prefab with a
    /// CharacterController + OverworldPlayerController on top of the generated terrain and point
    /// the existing follow camera at it.
    /// <para>
    /// Prefer the HumanDummy_M White prefab (already Humanoid) for spawn. Step 1 configures the
    /// underlying FBX as Humanoid if needed — open its Rig tab and use "Configure Avatar" only if
    /// auto-mapping fails.
    /// </para>
    /// <para>
    /// The dummy ships with Built-in Render Pipeline materials, which URP does not draw at all, so
    /// the character is invisible in the scene until they are converted — see
    /// <see cref="UrpMaterialUpgrade"/>.
    /// </para>
    /// </summary>
    public static class OverworldPlayerMenu
    {
        private const string CharacterPrefabPath =
            "Assets/Kevin Iglesias/Human Character Dummy/Prefabs/HumanDummy_M White.prefab";
        private const string CharacterFbxPath =
            "Assets/Kevin Iglesias/Human Character Dummy/Models/HumanCharacterDummy_M.fbx";
        private const string ControllerPath = "Assets/Settings/OverworldPlayerAnimator.controller";
        private const string PlayerObjectName = "OverworldPlayer";

        private const string ClipsRoot = "Assets/Kevin Iglesias/Human Animations/Animations/Male";
        private const string IdleClipPath = ClipsRoot + "/Idles/HumanM@Idle01.fbx";
        private const string WalkClipPath = ClipsRoot + "/Movement/Walk/HumanM@Walk01_Forward.fbx";
        private const string RunClipPath = ClipsRoot + "/Movement/Run/HumanM@Run01_Forward.fbx";
        private const string SprintClipPath = ClipsRoot + "/Movement/Sprint/HumanM@Sprint01_Forward.fbx";
        private const string JumpBeginClipPath = ClipsRoot + "/Movement/Jump/HumanM@Jump01 - Begin.fbx";
        private const string FallClipPath = ClipsRoot + "/Movement/Jump/HumanM@Fall01.fbx";
        private const string JumpLandClipPath = ClipsRoot + "/Movement/Jump/HumanM@Jump01 - Land.fbx";

        [MenuItem("GameDev/Overworld/Player/Setup Player (All Steps)")]
        public static void SetupPlayer()
        {
            ConfigureCharacterAsHumanoid();
            UrpMaterialUpgrade.UpgradeCharacterMaterialsMenu();
            BuildLocomotionAnimatorController();
            SpawnOrUpdatePlayerInScene();
        }

        [MenuItem("GameDev/Overworld/Player/1) Configure Dummy As Humanoid")]
        public static void ConfigureCharacterAsHumanoid()
        {
            if (!(AssetImporter.GetAtPath(CharacterFbxPath) is ModelImporter importer))
            {
                Debug.LogError($"[Overworld] No model at '{CharacterFbxPath}'.");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();

            var avatar = AssetDatabase.LoadAllAssetsAtPath(CharacterFbxPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Debug.LogWarning(
                    "[Overworld] Human Character Dummy did not auto-map cleanly as Humanoid. Select the FBX, open " +
                    "the Rig tab, click 'Configure Avatar' and fix any unmapped bones by hand, then " +
                    "re-run 'Setup Player (All Steps)'.");
                return;
            }

            Debug.Log("[Overworld] Human Character Dummy configured and auto-mapped as a valid Humanoid avatar.");
        }

        /// <summary>
        /// Builds the locomotion controller if it is missing, and leaves it alone otherwise.
        /// </summary>
        /// <remarks>
        /// This used to delete and recreate the asset on every run, including every Generate All.
        /// Deleting an <c>AnimatorController</c> that a live <c>Animator</c> in the open scene still
        /// references leaves that reference dangling, and the next domain reload throws
        /// <c>NullReferenceException</c> from <c>UnityEditor.Graphs.Edge.WakeUp</c> — the legacy
        /// graph behind the controller waking up on an edge whose asset is gone. Entering play mode
        /// is when it surfaces.
        /// </remarks>
        [MenuItem("GameDev/Overworld/Player/2) Build Locomotion Animator Controller (if missing)")]
        public static void BuildLocomotionAnimatorController()
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                return;
            }

            RebuildLocomotionAnimatorController();
        }

        [MenuItem("GameDev/Overworld/Player/Rebuild Locomotion Animator Controller (destructive)")]
        public static void RebuildLocomotionAnimatorController()
        {
            EnsureSettingsFolder();

            var idle = LoadClip(IdleClipPath);
            var walk = LoadClip(WalkClipPath);
            var run = LoadClip(RunClipPath);
            var sprint = LoadClip(SprintClipPath);
            var jumpBegin = LoadClip(JumpBeginClipPath);
            var fall = LoadClip(FallClipPath);
            var jumpLand = LoadClip(JumpLandClipPath);
            if (idle == null || walk == null || run == null || sprint == null ||
                jumpBegin == null || fall == null || jumpLand == null)
            {
                Debug.LogError("[Overworld] Missing one or more Human Basic Motions clips - aborting controller build.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

            var stateMachine = controller.layers[0].stateMachine;

            var locomotionTree = new BlendTree { name = "Locomotion", blendType = BlendTreeType.Simple1D, blendParameter = "Speed" };
            AssetDatabase.AddObjectToAsset(locomotionTree, controller);
            locomotionTree.AddChild(idle, 0f);
            locomotionTree.AddChild(walk, 1f);
            locomotionTree.AddChild(run, 2f);
            locomotionTree.AddChild(sprint, 3f);

            var locomotionState = stateMachine.AddState("Locomotion", new Vector3(0, 0));
            locomotionState.motion = locomotionTree;
            stateMachine.defaultState = locomotionState;

            var jumpBeginState = stateMachine.AddState("JumpBegin", new Vector3(300, -80));
            jumpBeginState.motion = jumpBegin;

            var fallState = stateMachine.AddState("Fall", new Vector3(300, 80));
            fallState.motion = fall;

            var jumpLandState = stateMachine.AddState("JumpLand", new Vector3(600, 0));
            jumpLandState.motion = jumpLand;

            // Jump pressed while moving/idle -> leave the ground, regardless of Grounded (button
            // press already implies grounded - OverworldPlayerController only fires it then).
            var toJumpBegin = locomotionState.AddTransition(jumpBeginState);
            toJumpBegin.hasExitTime = false;
            toJumpBegin.duration = 0.05f;
            toJumpBegin.AddCondition(AnimatorConditionMode.If, 0, "Jump");

            // Walking off a ledge without pressing Jump: fall directly once Grounded drops.
            var toFallFromLocomotion = locomotionState.AddTransition(fallState);
            toFallFromLocomotion.hasExitTime = false;
            toFallFromLocomotion.duration = 0.15f;
            toFallFromLocomotion.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");

            // JumpBegin plays once, then always drops into the Fall loop.
            var beginToFall = jumpBeginState.AddTransition(fallState);
            beginToFall.hasExitTime = true;
            beginToFall.exitTime = 0.9f;
            beginToFall.duration = 0.1f;

            // Fall loops until the controller reports Grounded again.
            var fallToLand = fallState.AddTransition(jumpLandState);
            fallToLand.hasExitTime = false;
            fallToLand.duration = 0.05f;
            fallToLand.AddCondition(AnimatorConditionMode.If, 0, "Grounded");

            // Land plays once, then back to Idle/Walk/Run/Sprint.
            var landToLocomotion = jumpLandState.AddTransition(locomotionState);
            landToLocomotion.hasExitTime = true;
            landToLocomotion.exitTime = 0.85f;
            landToLocomotion.duration = 0.15f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            ReattachControllerToPlayer(controller);
            Debug.Log($"[Overworld] Locomotion Animator Controller built at '{ControllerPath}'.");
        }

        /// <summary>
        /// Points the player in the scene at the freshly built controller.
        /// </summary>
        /// <remarks>
        /// Rebuilding deletes the old asset, and every <c>Animator</c> already referencing it is left
        /// holding a destroyed object. That is both symptoms at once: the legacy graph behind the
        /// controller throws <c>NullReferenceException</c> from <c>UnityEditor.Graphs.Edge.WakeUp</c>
        /// on the next domain reload, and at runtime the controller reads as absent, so every
        /// <c>SetFloat</c>/<c>SetBool</c> in <c>OverworldPlayerController.Update</c> warns once per
        /// frame — thousands of lines. Reassigning here closes the window; running the rebuild
        /// <em>after</em> spawning the player was what opened it.
        /// </remarks>
        private static void ReattachControllerToPlayer(RuntimeAnimatorController controller)
        {
            var player = GameObject.Find(PlayerObjectName);
            if (player == null)
            {
                return;
            }

            var animator = player.GetComponent<Animator>();
            if (animator == null)
            {
                return;
            }

            animator.runtimeAnimatorController = controller;
            EditorUtility.SetDirty(animator);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Overworld] Re-pointed the player's Animator at the rebuilt controller.");
        }

        [MenuItem("GameDev/Overworld/Player/3) Spawn Or Update Player In Scene")]
        public static void SpawnOrUpdatePlayerInScene()
        {
            var characterAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
            if (characterAsset == null)
            {
                Debug.LogError($"[Overworld] No prefab at '{CharacterPrefabPath}'.");
                return;
            }

            var controllerAsset = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            if (controllerAsset == null)
            {
                Debug.LogError("[Overworld] No Animator Controller at '" + ControllerPath + "' - run step 2 first.");
                return;
            }

            var existing = GameObject.Find(PlayerObjectName);
            var player = existing != null ? existing : (GameObject)PrefabUtility.InstantiatePrefab(characterAsset);
            player.name = PlayerObjectName;

            var animator = player.GetComponent<Animator>();
            if (animator == null)
            {
                animator = player.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controllerAsset;
            animator.applyRootMotion = false;

            var characterController = player.GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = player.AddComponent<CharacterController>();
            }

            // Visual mesh → WorldScale character height (controller matches the same metres).
            FitPlayerMeshToWorldScale(player);

            characterController.height = GameDev.World.WorldScale.CharacterHeightMeters;
            characterController.radius = GameDev.World.WorldScale.CharacterRadiusMeters;
            characterController.center = new Vector3(0f, characterController.height * 0.5f, 0f);
            characterController.skinWidth = 0.03f;
            characterController.stepOffset = 0.4f;

            if (player.GetComponent<OverworldPlayerController>() == null)
            {
                player.AddComponent<OverworldPlayerController>();
            }

            // Prefer TownApproach / settlement when SpawnPlayerAtTown was used; otherwise keep
            // prior position on update. Only place at tile centre for brand-new player with no town.
            if (existing == null && !s_PreferTownSpawn)
            {
                var spawnXZ = new Vector3(700f, 0f, 700f);
                if (TryFindGroundHeight(spawnXZ, out var groundY))
                {
                    player.transform.position = new Vector3(spawnXZ.x, groundY + 0.1f, spawnXZ.z);
                }
                else
                {
                    Debug.LogWarning(
                        "[Overworld] No ground found under the (700,700) spawn point - generate terrain " +
                        "first (GameDev/Overworld/Generate Terrain From Seed). Player placed at y=0.");
                    player.transform.position = spawnXZ;
                }
            }

            EnsureFollowCamera(player.transform);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = player;
            Debug.Log("[Overworld] Player spawned/updated in scene.");
        }

        private static bool s_PreferTownSpawn;

        /// <summary>
        /// Pipeline Player step: spawn at a seeded TownApproach (or settlement anchor for that id).
        /// </summary>
        public static void SpawnPlayerAtTown()
        {
            ConfigureCharacterAsHumanoid();
            BuildLocomotionAnimatorController();

            IReadOnlyList<PoiMarker> markers = System.Array.Empty<PoiMarker>();
            var state = OverworldWorldState.Find();
            if (state?.Definition?.Pois != null && state.Definition.Pois.Count > 0)
            {
                markers = state.Definition.Pois;
            }

            var towns = new List<PoiMarker>();
            foreach (var m in markers)
            {
                if (m.Kind == PoiKind.TownApproach)
                {
                    towns.Add(m);
                }
            }

            if (towns.Count == 0)
            {
                Debug.LogWarning("[Overworld] Player step: no TownApproach markers — skipping spawn.");
                return;
            }

            var seed = new WorldSeed(OverworldTerrainMenu.LastSeed);
            var rng = seed.Fork("player.spawn");
            var pick = towns[rng.Next(0, towns.Count)];
            var spawn = pick.Position;
            var anchor = Object.FindAnyObjectByType<OverworldSettlementAnchor>();
            if (anchor != null && anchor.ApproachId == pick.Id.Value)
            {
                spawn = anchor.Position;
            }

            s_PreferTownSpawn = true;
            SpawnOrUpdatePlayerInScene();
            s_PreferTownSpawn = false;

            var player = GameObject.Find(PlayerObjectName);
            if (player == null)
            {
                return;
            }

            if (TryFindGroundHeight(spawn, out var groundY))
            {
                player.transform.position = new Vector3(spawn.x, groundY + 0.1f, spawn.z);
            }
            else
            {
                player.transform.position = new Vector3(spawn.x, spawn.y + 0.1f, spawn.z);
            }

            EnsureFollowCamera(player.transform);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = player;
            Debug.Log($"[Overworld] Player at town approach id={pick.Id} pos={player.transform.position}");
        }

        public static void ClearPlayer()
        {
            var existing = GameObject.Find(PlayerObjectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        private static void FitPlayerMeshToWorldScale(GameObject player)
        {
            var renderers = player.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            player.transform.localScale = Vector3.one;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var height = bounds.size.y;
            if (height < 0.05f)
            {
                return;
            }

            var scale = GameDev.World.WorldScale.CharacterHeightMeters / height;
            player.transform.localScale = Vector3.one * scale;
        }

        private static bool TryFindGroundHeight(Vector3 xz, out float groundY)
        {
            var terrain = OverworldTerrainAccess.FindTerrain();
            if (terrain != null && terrain.terrainData != null)
            {
                groundY = terrain.SampleHeight(new Vector3(xz.x, 0f, xz.z)) + terrain.transform.position.y;
                return true;
            }

            groundY = 0f;
            return false;
        }

        /// <summary>Genshin-style close third-person orbit (GameDev.Player.OverworldThirdPersonCamera)
        /// - distinct from GameDev.Player.SimpleFollowCamera, which stays the BG3-tactical top-down
        /// rig for the Town/Grid lab. Reuses whatever Camera already exists in the scene (e.g.
        /// OverworldTerrainMenu's straight-down review camera) rather than adding a second one, and
        /// strips any leftover SimpleFollowCamera from an older run so the two don't fight over the
        /// transform every LateUpdate.
        /// <para>
        /// Uses Skybox clear flags so the overworld atmosphere renders; far clip plane is left as
        /// already configured (terrain setup uses a long far plane for vista distance).
        /// </para>
        /// </summary>
        private static void EnsureFollowCamera(Transform target)
        {
            var cameraObject = Camera.main != null ? Camera.main.gameObject : Object.FindAnyObjectByType<Camera>()?.gameObject;
            if (cameraObject == null)
            {
                cameraObject = new GameObject("Player Follow Camera");
                cameraObject.tag = "MainCamera";
            }

            var camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.clearFlags = CameraClearFlags.Skybox;

            var staleFollowCamera = cameraObject.GetComponent<SimpleFollowCamera>();
            if (staleFollowCamera != null)
            {
                Object.DestroyImmediate(staleFollowCamera);
            }

            var thirdPersonCamera = cameraObject.GetComponent<OverworldThirdPersonCamera>();
            if (thirdPersonCamera == null)
            {
                thirdPersonCamera = cameraObject.AddComponent<OverworldThirdPersonCamera>();
            }

            thirdPersonCamera.SetTarget(target);
        }

        private static AnimationClip LoadClip(string fbxPath)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null)
            {
                Debug.LogError($"[Overworld] No AnimationClip found at '{fbxPath}'.");
            }

            return clip;
        }

        private static void EnsureSettingsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }
        }
    }
}
#endif

