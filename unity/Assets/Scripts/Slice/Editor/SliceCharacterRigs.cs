using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The people's rigs (ANIMATION_PIPELINE_PLAN.md, owner 2026-11-10). Every Tripo person carries Tripo's own skeleton
    /// of the same bone names; the importer reads it as a Unity Humanoid, so one Humanoid clip, from Mixamo or our own,
    /// plays on every person whatever its proportions. <see cref="Convert"/> sets each model's importer to Humanoid with
    /// an avatar of its own; where Unity's own mapping fails, the bones are mapped by name. <see cref="Check"/> poses every
    /// person in the same clips and draws them from the front and the side, so a wrong mapping, an arm through a body or
    /// a skirt torn by a leg shows before any clip is made. The owner's files are changed only in their import settings.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.SliceCharacterRigs.SurveyConvertAndCheck
    /// </summary>
    public static class SliceCharacterRigs
    {
        private const string OutputFolder = "Logs/SliceSurvey";
        private const string Tripo = "Assets/TripoModels";

        /// <summary>The Tripo people, by file, in the order of the check sheet.</summary>
        public static readonly string[] People =
        {
            "vintage_gentleman_3d_model", "victorian_dress_3d_model", "victorian_woman_3d_model", "military_officer_3d_model",
            "steampunk_detective_3d_model", "medieval_blacksmith_3d_model",
            "military_uniform_3d_model", "human_figure_3d_model", "human_body_3d_model",
        };

        public static string ModelPath(string file) => $"{Tripo}/{file}/{file}.fbx";

        /// <summary>Survey, conversion and check in one Unity start.</summary>
        public static void SurveyConvertAndCheck()
        {
            SliceAssetSurvey.RunTripoNew();
            Convert();
            Check();
        }

        // ------------------------------------------------------------------------------------------- conversion

        [MenuItem("GameDev/Slice/People/Make the Tripo People Humanoid")]
        public static void Convert()
        {
            var report = new StringBuilder();
            foreach (var file in People)
            {
                var path = ModelPath(file);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    report.AppendLine($"{file}: MISSING");
                    continue;
                }

                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.SaveAndReimport();
                }

                var avatar = Avatar(path);
                var how = "Unity's mapping";
                if (avatar == null || !avatar.isValid || !avatar.isHuman)
                {
                    importer.humanDescription = ByName(path);
                    importer.SaveAndReimport();
                    avatar = Avatar(path);
                    how = "mapped by name";
                }

                var human = importer.humanDescription.human;
                report.AppendLine($"{file}: avatar {(avatar != null && avatar.isValid && avatar.isHuman ? "valid Humanoid" : "INVALID")} ({how}), {human.Length} bones mapped");
                report.AppendLine("  " + string.Join(", ", human.OrderBy(h => h.humanName).Select(h => $"{h.humanName}={h.boneName}")));
                report.AppendLine("  arms below the horizontal at rest: " + ArmDroop(path));
            }

            Directory.CreateDirectory(OutputFolder);
            File.WriteAllText($"{OutputFolder}/rigs.txt", report.ToString());
            Debug.Log("[SliceCharacterRigs]\n" + report);
        }

        private static Avatar Avatar(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();

        /// <summary>The upper arms' angle below the horizontal in the model's rest pose, degrees: about 0 is a T-pose, about 45 an A-pose.</summary>
        private static string ArmDroop(string path)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                var animator = model.GetComponent<Animator>();
                if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                {
                    return "no Humanoid avatar";
                }

                string Side(HumanBodyBones upper, HumanBodyBones lower)
                {
                    var a = animator.GetBoneTransform(upper);
                    var b = animator.GetBoneTransform(lower);
                    if (a == null || b == null)
                    {
                        return "?";
                    }

                    var d = (b.position - a.position).normalized;
                    return $"{Mathf.Asin(-d.y) * Mathf.Rad2Deg:0}°";
                }

                return $"left {Side(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm)}, right {Side(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm)}";
            }
            finally
            {
                Object.DestroyImmediate(model);
            }
        }

        /// <summary>
        /// Humanoid bones by Tripo's names: separators and case are ignored, a side is read from a leading or trailing
        /// Left/Right, and the part from the rest (Mixamo's UpLeg, Leg, ForeArm and Spine1–2 as well as Unity's own names).
        /// </summary>
        private static HumanDescription ByName(string path)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var transforms = model.GetComponentsInChildren<Transform>(true);
            var human = new List<HumanBone>();
            var taken = new HashSet<string>();
            foreach (var t in transforms)
            {
                var name = HumanName(t.name);
                if (name != null && taken.Add(name))
                {
                    human.Add(new HumanBone { humanName = name, boneName = t.name, limit = new HumanLimit { useDefaultValues = true } });
                }
            }

            var skeleton = transforms.Select(t => new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale }).ToArray();
            Object.DestroyImmediate(model);
            return new HumanDescription
            {
                human = human.ToArray(), skeleton = skeleton, upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f,
                armStretch = 0.05f, legStretch = 0.05f, feetSpacing = 0f, hasTranslationDoF = false,
            };
        }

        internal static string HumanName(string bone)
        {
            var n = bone.ToLowerInvariant().Replace("mixamorig:", "").Replace("_", "").Replace(".", "").Replace(" ", "").Replace("-", "");
            string side = null;
            if (n.StartsWith("left")) { side = "Left"; n = n.Substring(4); }
            else if (n.StartsWith("right")) { side = "Right"; n = n.Substring(5); }
            else if (n.EndsWith("left")) { side = "Left"; n = n.Substring(0, n.Length - 4); }
            else if (n.EndsWith("right")) { side = "Right"; n = n.Substring(0, n.Length - 5); }

            switch (n)
            {
                case "hips": case "pelvis": return side == null ? "Hips" : null;
                case "spine": return side == null ? "Spine" : null;
                case "spine1": case "chest": return side == null ? "Chest" : null;
                case "spine2": case "upperchest": return side == null ? "UpperChest" : null;
                case "neck": return side == null ? "Neck" : null;
                case "head": return side == null ? "Head" : null;
                case "jaw": return side == null ? "Jaw" : null;
            }

            if (side == null)
            {
                return null;
            }

            var part = n switch
            {
                "shoulder" or "clavicle" => "Shoulder",
                "arm" or "upperarm" => "UpperArm",
                "forearm" or "lowerarm" => "LowerArm",
                "hand" => "Hand",
                "upleg" or "thigh" or "upperleg" => "UpperLeg",
                "leg" or "calf" or "lowerleg" or "shin" => "LowerLeg",
                "foot" => "Foot",
                "toebase" or "toe" or "toes" => "Toes",
                "eye" => "Eye",
                _ => null,
            };
            if (part != null)
            {
                return side + part;
            }

            // Fingers: thumb, index, middle, ring, pinky or little, then 1–3.
            foreach (var (key, finger) in new[] { ("thumb", "Thumb"), ("index", "Index"), ("middle", "Middle"), ("ring", "Ring"), ("pinky", "Little"), ("little", "Little") })
            {
                if (n.StartsWith(key) && n.Length == key.Length + 1 && "123".Contains(n[key.Length]))
                {
                    var joint = n[key.Length] == '1' ? "Proximal" : n[key.Length] == '2' ? "Intermediate" : "Distal";
                    return $"{side} {finger} {joint}";
                }
            }

            return null;
        }

        // ------------------------------------------------------------------------------------------- check

        /// <summary>
        /// The clips every person is posed in before they are wired (ANIMATION_PIPELINE_PLAN.md §8): Kevin Iglesias' idle,
        /// walk at mid-stride, talk, one-handed blow, rifle's aim, end of a death, pick and hammer; Quaternius' axe, swim,
        /// seat and drink.
        /// </summary>
        private static readonly (string Use, float At)[] Poses =
        {
            ("Idle", 0.5f), ("Walk", 0.25f), ("Talk", 0.5f), ("Attack1H", 0.45f), ("RifleAim", 0.5f), ("Die", 0.97f),
            ("Mine", 0.3f), ("HammerDown", 1f), ("Chop", 0.3f), ("Swim", 0.25f), ("Sit", 0.5f), ("Drink", 0.5f),
        };

        [MenuItem("GameDev/Slice/People/Check the Tripo People's Rigs")]
        public static void Check()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);

            var clips = Poses.Select(p => (Clip: SliceClips.Get(p.Use), p.At)).ToList();
            const int tile = 256;
            var muscles = new HashSet<string>(HumanTrait.MuscleName.Select(ClipMuscle));
            foreach (var (clip, _) in clips.Where(c => c.Clip != null))
            {
                var names = AnimationUtility.GetCurveBindings(clip).Select(b => $"{b.type.Name}:{b.path}:{b.propertyName}").ToList();
                Debug.Log($"[SliceCharacterRigs] {clip.name}: {names.Count} curves, {names.Count(n => muscles.Contains(n.Substring(n.LastIndexOf(':') + 1)))} muscles; " +
                          string.Join(" | ", names.Take(12)));
            }

            var columns = 2 + (clips.Count * 2);
            var sheet = new Texture2D(columns * tile, People.Length * tile, TextureFormat.RGB24, false);
            var log = new StringBuilder();
            for (var row = 0; row < People.Length; row++)
            {
                var path = ModelPath(People[row]);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                SliceAssetSurvey.Colour(model, path);
                var animator = model.GetComponent<Animator>();
                var column = 0;
                Draw(model, sheet, row, ref column, tile); // the rest pose
                foreach (var (clip, at) in clips)
                {
                    if (clip == null || animator == null || animator.avatar == null || !animator.avatar.isHuman)
                    {
                        column += 2;
                        log.AppendLine($"{People[row]}: {(clip == null ? "a clip is missing" : "no Humanoid avatar")}");
                        continue;
                    }

                    // The pose is read from the clip's own curves and set through the avatar: in batch mode neither a
                    // playable graph nor the editor's sampling moved more than the root.
                    var handler = new HumanPoseHandler(animator.avatar, model.transform);
                    var pose = PoseAt(clip, clip.length * at);
                    handler.SetHumanPose(ref pose);
                    handler.Dispose();
                    Draw(model, sheet, row, ref column, tile);
                }

                Object.DestroyImmediate(model);
            }

            sheet.Apply();
            Directory.CreateDirectory(OutputFolder);
            File.WriteAllBytes($"{OutputFolder}/rig_check.png", sheet.EncodeToPNG());
            File.WriteAllText($"{OutputFolder}/rig_check.txt", log.Length > 0 ? log.ToString() : "every person posed\n");
            Debug.Log($"[SliceCharacterRigs] rig check written to {OutputFolder}/rig_check.png");
        }

        /// <summary>A Humanoid clip's pose at <paramref name="time"/>, read from its muscle and root curves.</summary>
        internal static HumanPose PoseAt(AnimationClip clip, float time)
        {
            var pose = new HumanPose { muscles = new float[HumanTrait.MuscleCount], bodyRotation = Quaternion.identity, bodyPosition = Vector3.up };
            var index = Enumerable.Range(0, HumanTrait.MuscleCount).ToDictionary(i => ClipMuscle(HumanTrait.MuscleName[i]), i => i);
            var root = new[] { 0f, 1f, 0f, 0f, 0f, 0f, 1f };
            string[] rootNames = { "RootT.x", "RootT.y", "RootT.z", "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" };
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type != typeof(Animator))
                {
                    continue;
                }

                var value = AnimationUtility.GetEditorCurve(clip, binding).Evaluate(time);
                var r = System.Array.IndexOf(rootNames, binding.propertyName);
                if (r >= 0)
                {
                    root[r] = value;
                }
                else if (index.TryGetValue(binding.propertyName, out var i))
                {
                    pose.muscles[i] = value;
                }
            }

            pose.bodyPosition = new Vector3(root[0], root[1], root[2]);
            pose.bodyRotation = new Quaternion(root[3], root[4], root[5], root[6]).normalized;
            return pose;
        }

        /// <summary>
        /// A muscle's name as a clip's curve names it: the fingers' muscles are "LeftHand.Thumb.1 Stretched" in a clip and
        /// "Left Thumb 1 Stretched" in <see cref="HumanTrait.MuscleName"/>; the rest are the same.
        /// </summary>
        internal static string ClipMuscle(string muscle)
        {
            foreach (var side in new[] { "Left", "Right" })
            {
                foreach (var finger in new[] { "Thumb", "Index", "Middle", "Ring", "Little" })
                {
                    var prefix = $"{side} {finger} ";
                    if (muscle.StartsWith(prefix))
                    {
                        return $"{side}Hand.{finger}.{muscle.Substring(prefix.Length)}";
                    }
                }
            }

            return muscle;
        }

        /// <summary>The front and the side of the posed model, side by side, into the sheet's row (of <paramref name="rows"/>, the people's by default).</summary>
        internal static void Draw(GameObject model, Texture2D sheet, int row, ref int column, int tile, int rows = 0)
        {
            rows = rows > 0 ? rows : People.Length;
            // The skin baked on the CPU from the bones as they stand: a skinned renderer drawn in batch mode kept its rest pose.
            var baked = new List<GameObject>();
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                smr.BakeMesh(mesh, true);
                var copy = new GameObject("Baked", typeof(MeshFilter), typeof(MeshRenderer));
                copy.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
                // The bake keeps the renderer's own scale but not its parents' (a Tripo body's Model node is scaled):
                // without it the skin drew at a fraction of the props beside it.
                copy.transform.localScale = smr.transform.parent != null ? smr.transform.parent.lossyScale : Vector3.one;
                copy.GetComponent<MeshFilter>().sharedMesh = mesh;
                copy.GetComponent<MeshRenderer>().sharedMaterials = smr.sharedMaterials;
                baked.Add(copy);
                smr.enabled = false;
            }

            var renderers = baked.Count > 0 ? baked.Select(b => (Renderer)b.GetComponent<MeshRenderer>()).ToArray() : model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            var size = Mathf.Max(bounds.size.y, 0.1f);
            foreach (var dir in new[] { Vector3.forward, Vector3.right })
            {
                var camera = new GameObject("RigCheckCamera").AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = size * 0.6f;
                camera.transform.SetPositionAndRotation(bounds.center + dir * size * 3f, Quaternion.LookRotation(-dir));
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.78f, 0.84f, 0.9f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = size * 8f;
                var target = new RenderTexture(tile, tile, 24);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(tile, tile, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, tile, tile), 0, 0);
                image.Apply();
                RenderTexture.active = null;
                sheet.SetPixels(column * tile, (rows - 1 - row) * tile, tile, tile, image.GetPixels());
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(camera.gameObject);
                column++;
            }

            foreach (var copy in baked)
            {
                Object.DestroyImmediate(copy.GetComponent<MeshFilter>().sharedMesh);
                Object.DestroyImmediate(copy);
            }

            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.enabled = true;
            }
        }
    }
}
