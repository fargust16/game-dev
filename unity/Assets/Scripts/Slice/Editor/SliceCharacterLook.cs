using System.IO;
using System.Linq;
using GameDev.Cameras;
using GameDev.Locomotion;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// How the slice's characters look and stand, and the fixes the owner's play test of 2026-11-05 asked for:
    /// <list type="bullet">
    /// <item>The Human Character Dummy wears built-in <c>Unlit/Texture</c>, which URP does not draw: the player and every
    /// Dummy but the project-coloured ones were invisible. Each vendor material gets a URP Lit copy with the same palette
    /// texture and offset under <c>Materials/Slice/Dummy</c>; the vendor files stay as they are.</item>
    /// <item>People wore the player's animator, whose <c>Grounded</c> starts false, so they hung in its fall pose. They
    /// get an idle-only controller.</item>
    /// <item>The dragons' own controllers are the pack's demo, which plays every clip in turn, flight and death among
    /// them. The beasts' stand-ins get a controller with the pack's idle only.</item>
    /// <item>The portal leaf is hidden: only its trigger stays (owner).</item>
    /// <item>The camera's spring arm meets only the ground and structures; props, plants and people no longer pull it in.</item>
    /// </list>
    /// Batch: -executeMethod GameDev.Slice.EditorTools.SliceCharacterLook.FixAll
    /// </summary>
    public static class SliceCharacterLook
    {
        private const string MaterialFolder = "Assets/Materials/Slice/Dummy";
        private const string AnimatorFolder = "Assets/Settings/Slice";
        public const string PersonIdlePath = AnimatorFolder + "/SlicePersonIdle.controller";
        private const string PortalLeafPath = SliceKitCatalogue.Folder + "/Portal/Kit_Portal_Leaf_3x2.prefab";

        /// <summary>What the camera's spring arm meets: the ground, the buildings and the interiors' ceilings, nothing smaller.</summary>
        public static LayerMask CameraCollision =>
            LayerMask.GetMask(KitLayers.Ground, KitLayers.Structure, VeilmouthInteriors.CeilingLayer);

        [MenuItem("GameDev/Slice/Fix Characters, Doors and Camera in Slice Scenes")]
        public static void FixAll()
        {
            HidePortalLeaf();
            FixPlayerPrefab();
            var scenes = new[] { VeilmouthScene.ScenePath, VeilmouthScene.SootAndBellScenePath }
                .Concat(VeilmouthDressing.InteriorScenePaths)
                .Append(SliceTestScenes.MovementScenePath).Append(SliceTestScenes.TavernScenePath);
            foreach (var path in scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var people = 0;
                foreach (var animator in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (animator.GetComponentInParent<PlayerLocomotion>() != null)
                    {
                        continue;
                    }

                    people += Fix(animator.gameObject) ? 1 : 0;
                }

                foreach (var rig in Object.FindObjectsByType<ThirdPersonCamera>(FindObjectsSortMode.None))
                {
                    rig.SetCollisionMask(CameraCollision);
                    EditorUtility.SetDirty(rig);
                }

                EditorSceneManager.SaveScene(scene);
                Debug.Log($"{path}: {people} characters fixed.");
            }
        }

        /// <summary>A person or a beast placed by the dressing: its look and its idle.</summary>
        public static bool Fix(GameObject character)
        {
            var animator = character.GetComponent<Animator>();
            if (animator == null)
            {
                return false;
            }

            if (animator.avatar != null && animator.avatar.isHuman)
            {
                Look(character);
                animator.runtimeAnimatorController = PersonIdle();
            }
            else if (animator.runtimeAnimatorController != null && animator.runtimeAnimatorController.name.EndsWith("CTRL"))
            {
                animator.runtimeAnimatorController = BeastIdle(animator.runtimeAnimatorController);
            }
            else
            {
                return false;
            }

            animator.applyRootMotion = false;
            EditorUtility.SetDirty(animator);
            return true;
        }

        /// <summary>Gives every built-in-shader Dummy material its URP copy.</summary>
        public static void Look(GameObject character)
        {
            foreach (var r in character.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = r.sharedMaterials.Select(m => m != null && !m.shader.name.StartsWith("Universal") ? UrpCopy(m) : m).ToArray();
                EditorUtility.SetDirty(r);
            }
        }

        private static Material UrpCopy(Material vendor)
        {
            var path = $"{MaterialFolder}/{vendor.name}.mat";
            var copy = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (copy != null)
            {
                return copy;
            }

            Directory.CreateDirectory(MaterialFolder);
            copy = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = vendor.name };
            if (vendor.HasProperty("_MainTex"))
            {
                copy.SetTexture("_BaseMap", vendor.GetTexture("_MainTex"));
                copy.SetTextureScale("_BaseMap", vendor.GetTextureScale("_MainTex"));
                copy.SetTextureOffset("_BaseMap", vendor.GetTextureOffset("_MainTex"));
            }

            copy.SetColor("_BaseColor", vendor.HasProperty("_Color") ? vendor.GetColor("_Color") : Color.white);
            copy.SetFloat("_Smoothness", 0.25f);
            AssetDatabase.CreateAsset(copy, path);
            return copy;
        }

        public static RuntimeAnimatorController PersonIdle()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PersonIdlePath);
            if (existing != null)
            {
                return existing;
            }

            // A standing person's controller on the Mixamo clips (ANIMATION_PIPELINE_PLAN.md A1).
            return SliceMixamo.Person(PersonIdlePath, "Idle");
        }

        /// <summary>The pack's own idle, alone: no demo chain from one clip to the next.</summary>
        public static RuntimeAnimatorController BeastIdle(RuntimeAnimatorController vendor)
        {
            var name = vendor.name.Replace("CTRL", "");
            var path = $"{AnimatorFolder}/SliceBeastIdle_{name}.controller";
            var existing = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
            if (existing != null)
            {
                return existing;
            }

            var source = (AnimatorController)vendor;
            var idle = source.layers[0].stateMachine.states.First(s => s.state.name == "Idle").state.motion;
            Directory.CreateDirectory(AnimatorFolder);
            return AnimatorController.CreateAnimatorControllerAtPathWithClip(path, (AnimationClip)idle);
        }

        private static void FixPlayerPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(SliceTestScenes.PlayerPrefabPath);
            Look(root);
            PrefabUtility.SaveAsPrefabAsset(root, SliceTestScenes.PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void HidePortalLeaf()
        {
            var root = PrefabUtility.LoadPrefabContents(PortalLeafPath);
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = r.GetComponent<MeshFilter>();
                Object.DestroyImmediate(r);
                Object.DestroyImmediate(filter);
            }

            PrefabUtility.SaveAsPrefabAsset(root, PortalLeafPath);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
