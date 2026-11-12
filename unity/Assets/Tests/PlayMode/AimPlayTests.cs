#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Cameras;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Fights;
using GameDev.Slice.Gear;
using GameDev.Slice.People;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// 10.2 §5e in Veilmouth, the Aimed Shot (PB8b): out of a fight, with a firearm drawn, the right mouse button raises it
    /// to the eye: the camera stands at the eye and narrows, the firearm's rear and front sight stand at the screen's
    /// centre, the right hand holds it there, and the body faces the aim and walks any way, never running. A click fires a
    /// projectile at what the centre shows, and the next shot waits the loading. The one touched turns to the shot: seeing
    /// the shooter, it is an Assault; beyond its sight, it searches toward the source.
    /// </summary>
    public sealed class AimPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";
        private const float Chest = 1.3f;

        [UnityTest]
        public IEnumerator TheAimRaisesTheFirearmToTheEyeAndASeenShotIsAnAssault()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var aim = world.Aim;
            Assert.IsNotNull(aim, "the world carries the firearm's aim");
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var animator = player.GetComponentInChildren<Animator>();
            for (var t = 0f; t < 2f && !player.IsGrounded; t += Time.deltaTime)
            {
                yield return null;
            }

            var (walker, place) = InTheOpen(6f);
            Assert.IsNotNull(walker, "a walker in the open, six metres off along its street");
            walker.enabled = false;
            player.Teleport(place, Quaternion.LookRotation(Flat(walker.transform.position - place)));
            yield return Wait(0.5f);

            yield return DrawThe(world, "pistol");
            Assert.AreEqual(FightBeats.Pistol, world.Arms.Motion, "a pistol's guard");

            // Held: the camera at the eye, narrowed, and the sights on its line.
            var camera = Camera.main;
            var orbitView = camera.fieldOfView;
            aim.HoldOverride = true;
            yield return LookAt(camera, walker.transform.position + (Vector3.up * Chest), 0.8f);
            Assert.IsTrue(aim.Held, "the right button holds the aim");
            Assert.IsTrue(aim.Aimed, "the firearm comes up to the eye");
            Assert.AreEqual(SliceTuning.Current.Aim.PistolFieldOfView, camera.fieldOfView, 0.5f, "the view narrows to the pistol's");
            Assert.Less(Vector3.Distance(camera.transform.position, aim.Eye), 0.05f, "the camera stands at the eye");
            var gun = player.GetComponent<HeldWeapons>().Main.transform;
            var front = camera.WorldToViewportPoint(gun.Find(PlayerAim.FrontSight).position);
            var rear = camera.WorldToViewportPoint(gun.Find(PlayerAim.RearSight).position);
            Assert.IsTrue(front.z > rear.z && rear.z > camera.nearClipPlane, $"the rear sight before the front sight, both ahead: {rear} {front}");
            Assert.Less(Mathf.Max(Mathf.Abs(front.x - 0.5f), Mathf.Abs(front.y - 0.5f)), 0.01f, $"the front sight stands at the screen's centre: {front}");
            Assert.Less(Mathf.Max(Mathf.Abs(rear.x - 0.5f), Mathf.Abs(rear.y - 0.5f)), 0.01f, $"the rear sight under it: {rear}");
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var grip = HeldWeapons.Grip(animator, true);
            Assert.Less(Vector3.Distance(hand.TransformPoint(grip.position), gun.position), 0.04f, "the right hand holds the firearm there");
            Assert.IsFalse(animator.GetBool("Fighting"), "the legs keep the walk");

            // The Aimed Walk: D walks right, the body facing the aim, never running.
            var start = player.transform.position;
            var right = Flat(camera.transform.right);
            var fastest = 0f;
            var sideways = 0f;
            var standing = Shoulders(animator, player.transform);
            var turned = 0f;
            var samples = string.Empty;
            Hold(Key.D);
            for (var t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                fastest = Mathf.Max(fastest, player.PlanarSpeed);
                sideways = Mathf.Max(sideways, animator.GetFloat("MoveX"));
                if (t > 0.3f)
                {
                    var now = Shoulders(animator, player.transform);
                    samples += $" {now:0}";
                    turned = Mathf.Max(turned, Mathf.Abs(Mathf.DeltaAngle(standing, now)));
                }
                yield return null;
            }

            Hold();
            Assert.Greater(Vector3.Dot(player.transform.position - start, right), 0.3f, "D walks to the right");
            Assert.Less(Vector3.Angle(Flat(player.transform.forward), Flat(camera.transform.forward)), 8f, "the body faces the aim");
            Assert.LessOrEqual(fastest, player.WalkSpeed + 0.15f, "aiming, the character walks and never runs");
            Assert.Greater(sideways, 0.3f, "the walk's side clip plays");
            Assert.Less(turned, 15f, $"the shoulders keep the aim while the side walk turns the hips to the step: standing {standing:0}°, walking{samples}");

            // A click fires, and the pistol loads; a good shot hits.
            world.Character.Set(CharacterSkill.RangedSkill, 20);
            world.Character.Set(CharacterAttribute.Perception, 10);
            world.Character.Set(CharacterAttribute.Dexterity, 10);
            yield return LookAt(camera, walker.transform.position + (Vector3.up * Chest), 0.4f);
            var fights = world.Fights;
            Assert.IsTrue(world.Arms.Strike(), "a click fires");
            Assert.IsTrue(aim.Loading, "the pistol loads");
            Assert.IsFalse(world.Arms.Strike(), "the next shot waits its loading");
            var shot = aim.LastShot;
            Assert.IsNotNull(shot, "a projectile flies");
            for (var t = 0f; t < 2f && shot != null; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(shot == null, "it touched something and is gone");
            Assert.Less(Vector3.Angle(Flat(walker.transform.forward), Flat(player.transform.position - walker.transform.position)), 5f, "the one touched turns to the shot");
            Assert.IsTrue(fights.InFight || Lying(walker.transform), $"seen, the shot is an Assault (hit {fights.LastShot.Hit})");
            if (fights.InFight)
            {
                Assert.IsTrue(fights.Assaulting, "an Assault");
            }

            Assert.IsTrue(fights.LastShot.Hit, "a good shot at six metres hits");
            Assert.IsTrue(fights.Wound(walker.transform) > 0d || Lying(walker.transform), "and its wound stays on the one hit");
            aim.HoldOverride = null;
        }

        [UnityTest]
        public IEnumerator AShotFromBeyondSightMakesTheOneTouchedSearch()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var aim = world.Aim;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            for (var t = 0f; t < 2f && !player.IsGrounded; t += Time.deltaTime)
            {
                yield return null;
            }

            var (walker, place) = InTheOpen(18f);
            Assert.IsNotNull(walker, "a walker in the open, eighteen metres off along its street");
            walker.enabled = false;
            player.Teleport(place, Quaternion.LookRotation(Flat(walker.transform.position - place)));
            yield return Wait(0.5f);

            yield return DrawThe(world, "rifle");
            Assert.AreEqual(FightBeats.Rifle, world.Arms.Motion, "a rifle's guard");
            var camera = Camera.main;
            aim.HoldOverride = true;
            yield return LookAt(camera, walker.transform.position + (Vector3.up * Chest), 0.8f);
            Assert.IsTrue(aim.Aimed, "the rifle comes up to the eye");
            Assert.AreEqual(SliceTuning.Current.Aim.RifleFieldOfView, camera.fieldOfView, 0.5f, "the view narrows to the rifle's");
            var gun = player.GetComponent<HeldWeapons>().Main.transform;
            var front = camera.WorldToViewportPoint(gun.Find(PlayerAim.FrontSight).position);
            Assert.Less(Mathf.Max(Mathf.Abs(front.x - 0.5f), Mathf.Abs(front.y - 0.5f)), 0.01f, $"the front sight stands at the screen's centre: {front}");

            var fights = world.Fights;
            Assert.IsTrue(world.Arms.Strike(), "a click fires");
            var shot = aim.LastShot;
            for (var t = 0f; t < 2f && shot != null; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(shot == null, "it touched something and is gone");
            Assert.IsFalse(fights.InFight, "a shot from beyond the cone starts no fight");
            Assert.Less(Vector3.Angle(Flat(walker.transform.forward), Flat(player.transform.position - walker.transform.position)), 5f, "the one touched turns to the shot");
            var search = walker.GetComponent<FightSearch>();
            Assert.IsTrue((search != null && search.Shot) || Lying(walker.transform), $"unseen, it searches toward the shot's source (hit {fights.LastShot.Hit})");
            if (!fights.LastShot.Hit)
            {
                Assert.AreEqual(0d, fights.Wound(walker.transform), "a graze leaves no wound");
            }

            aim.HoldOverride = null;
        }

        /// <summary>
        /// Diagnostic: the aimed view and the body from the side and from the front, for the pistol and the rifle, standing
        /// and in the Aimed Walk, written to <c>Logs/SliceSurvey/aim_*.png</c>. Run with graphics.
        /// </summary>
        [UnityTest, Explicit("diagnostic: run by name"), Category("Diagnostic")]
        public IEnumerator AimViews()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var aim = world.Aim;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            for (var t = 0f; t < 2f && !player.IsGrounded; t += Time.deltaTime)
            {
                yield return null;
            }

            var (walker, place) = InTheOpen(8f);
            Assert.IsNotNull(walker);
            walker.enabled = false;
            player.Teleport(place, Quaternion.LookRotation(Flat(walker.transform.position - place)));
            yield return Wait(0.5f);
            var camera = Camera.main;
            var side = new GameObject("AimSide").AddComponent<Camera>();
            side.enabled = false;
            side.fieldOfView = 35f;
            side.nearClipPlane = 0.05f;
            var pouch = new ItemPiece("cartridge_pouch");
            Assert.IsTrue(world.Bag.AddPiece(pouch));
            foreach (var firearm in new[] { "pistol", "rifle" })
            {
                var piece = new ItemPiece(firearm);
                Assert.IsTrue(world.Bag.AddPiece(piece));
                Assert.IsNull(world.Outfit.Wear(piece), firearm);
                Assert.IsTrue(world.Arms.Draw(), "R draws");
                yield return Wait(PlayerArms.DrawSeconds + 0.1f);
                aim.HoldOverride = true;
                yield return LookAt(camera, walker.transform.position + (Vector3.up * Chest), 1f);

                var held = player.GetComponent<HeldWeapons>().Main.transform;
                var animator = player.GetComponentInChildren<Animator>();
                var reach = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightHand).TransformPoint(HeldWeapons.Grip(animator, true).position), held.position);
                Debug.Log($"[AimViews] {firearm}: held {aim.Held}, raised {aim.Raised:0.00}, the right hand {reach:0.000} m from its grip, " +
                          $"the front sight at {camera.WorldToViewportPoint(held.Find(PlayerAim.FrontSight).position)}, the camera {Vector3.Distance(camera.transform.position, aim.Eye):0.000} m from the eye, " +
                          $"the player {player.name} of {Object.FindObjectsByType<PlayerLocomotion>(FindObjectsInactive.Exclude).Length}, the camera's target {camera.GetComponent<ThirdPersonCamera>().Target?.name}, " +
                          $"the walker {walker.name} at {Vector3.Distance(walker.transform.position, player.transform.position):0.0} m, the view {camera.fieldOfView:0}°.");
                yield return Shoot(camera, $"aim_{firearm}");
                var body = player.transform;
                var chest = body.position + (Vector3.up * 1.4f);
                side.transform.position = chest + (body.right * 1.8f) + (body.forward * 0.4f) + (Vector3.up * 0.1f);
                side.transform.LookAt(chest + (body.forward * 0.3f));
                yield return Shoot(side, $"aim_{firearm}_side");
                side.transform.position = chest + (body.forward * 2.2f) + (body.right * 0.6f);
                side.transform.LookAt(chest);
                yield return Shoot(side, $"aim_{firearm}_front");

                Hold(Key.D);
                yield return Wait(0.45f);
                chest = body.position + (Vector3.up * 1.2f);
                side.transform.position = chest + (body.right * 2.6f) + (body.forward * 2.2f);
                side.transform.LookAt(chest);
                yield return Shoot(side, $"aim_{firearm}_walk");
                Hold();
                aim.HoldOverride = false;
                yield return Wait(0.5f);
                world.Arms.Sheathe();
                Assert.IsNull(world.Outfit.TakeOff(pouch));
                yield return Wait(0.2f);
            }

            Object.Destroy(side.gameObject);
            aim.HoldOverride = null;
        }

        /// <summary>
        /// The camera draws into a texture in the frame's own order, after the Animator and the aim's work on the bones, as
        /// the screen is drawn; a camera's Render() from a test's step draws what the renderers last took up, which is older.
        /// </summary>
        /// <summary>Degrees the shoulders' line faces off the body's facing, left negative.</summary>
        private static float Shoulders(Animator animator, Transform body)
        {
            var left = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
            var right = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
            return Vector3.SignedAngle(Flat(body.forward), Flat(Vector3.Cross(right - left, Vector3.up)), Vector3.up);
        }

        private static IEnumerator Shoot(Camera camera, string name)
        {
            const int width = 1280;
            const int height = 720;
            var texture = new RenderTexture(width, height, 24);
            var target = camera.targetTexture;
            var on = camera.enabled;
            camera.targetTexture = texture;
            camera.enabled = true;
            yield return null;
            yield return null;
            camera.enabled = on;
            RenderTexture.active = texture;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = null;
            camera.targetTexture = target;
            System.IO.Directory.CreateDirectory("Logs/SliceSurvey");
            System.IO.File.WriteAllBytes($"Logs/SliceSurvey/{name}.png", image.EncodeToPNG());
            Object.Destroy(image);
            texture.Release();
            Object.Destroy(texture);
        }

        /// <summary>The firearm and its pouch into the hands, drawn, and the draw's moment waited.</summary>
        private static IEnumerator DrawThe(SliceWorldDriver world, string firearm)
        {
            var pouch = new ItemPiece("cartridge_pouch");
            Assert.IsTrue(world.Bag.AddPiece(pouch));
            var piece = new ItemPiece(firearm);
            Assert.IsTrue(world.Bag.AddPiece(piece));
            Assert.IsNull(world.Outfit.Wear(piece), firearm);
            Assert.IsTrue(world.Arms.Draw(), "R draws");
            yield return Wait(PlayerArms.DrawSeconds + 0.1f);
        }

        /// <summary>The camera's look turned at a point from wherever it stands, frame by frame, for a while.</summary>
        private static IEnumerator LookAt(Camera camera, Vector3 point, float seconds)
        {
            var look = camera.GetComponent<ThirdPersonCamera>();
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var way = point - camera.transform.position;
                var flat = Flat(way);
                look.Look(Quaternion.LookRotation(flat).eulerAngles.y, -Mathf.Atan2(way.y, new Vector2(way.x, way.z).magnitude) * Mathf.Rad2Deg);
                yield return null;
            }
        }

        /// <summary>
        /// A walker in the street with the open way <paramref name="metres"/> ahead of it, ground there at its own height,
        /// and nothing between an eye there and its chest.
        /// </summary>
        private static (TownsfolkWalker, Vector3) InTheOpen(float metres)
        {
            foreach (var w in Object.FindObjectsByType<TownsfolkWalker>(FindObjectsInactive.Exclude).Where(w => w.isActiveAndEnabled && !w.Inside).OrderBy(w => w.name))
            {
                var at = w.transform.position;
                var way = Flat(w.transform.forward);
                foreach (var turn in new[] { 0f, 180f, 90f, -90f })
                {
                    var probe = at + (Quaternion.Euler(0f, turn, 0f) * way * metres);
                    if (!Physics.Raycast(probe + (Vector3.up * 3f), Vector3.down, out var ground, 8f, ~0, QueryTriggerInteraction.Ignore) ||
                        Mathf.Abs(ground.point.y - at.y) > 0.4f)
                    {
                        continue;
                    }

                    var eye = ground.point + (Vector3.up * 1.6f);
                    var chest = at + (Vector3.up * Chest);
                    var clear = Physics.SphereCastAll(eye, 0.1f, (chest - eye).normalized, Vector3.Distance(eye, chest), ~0, QueryTriggerInteraction.Ignore)
                        .All(h => h.collider.transform.IsChildOf(w.transform));
                    var room = !Physics.CheckCapsule(ground.point + (Vector3.up * 0.4f), ground.point + (Vector3.up * 1.6f), 0.35f, ~0, QueryTriggerInteraction.Ignore);
                    if (clear && room)
                    {
                        return (w, ground.point);
                    }
                }
            }

            return (null, Vector3.zero);
        }

        private static bool Lying(Transform body)
        {
            var animator = body.GetComponentInChildren<Animator>(true);
            return animator != null && animator.parameters.Any(p => p.name == "Down") && animator.GetBool("Down");
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-8f ? v.normalized : Vector3.forward;
        }

        private static IEnumerator Wait(float seconds)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                yield return null;
            }
        }
    }
}
#endif
