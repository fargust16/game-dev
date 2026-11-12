#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using GameDev.Slice.People;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// AN6, the living townsfolk (ANIMATION_PIPELINE_PLAN §13): Quaternius' clips face the way the root does in play; a
    /// walker sent to a bench sits with its hips on the seat, talks while someone sits beside it, and walks back to its
    /// route; at a stall it bargains; at a door it goes in out of sight and comes out; two walkers who meet face to face
    /// nod to one another; a person with no route keeps its seat. A ground of its own beside the movement test scene.
    /// </summary>
    public sealed class ActivityPlayTests : SliceInputTestBase
    {
        private const string ScenePath = "Assets/Scenes/Slice/S0_Movement_Test.unity";
        private const string ManWalker = "Assets/Prefabs/Slice/Townsperson.prefab";
        private const string WomanWalker = "Assets/Prefabs/Slice/People/WorkingWomanWalker.prefab";
        private static readonly Vector3 Area = new Vector3(-300f, 0f, 300f);
        private const float SeatHeight = 0.45f;

        private static IEnumerator Ground()
        {
            yield return LoadScene(ScenePath);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Activity test ground";
            ground.layer = LayerMask.NameToLayer("Ground");
            ground.transform.position = Area + (Vector3.down * 0.5f);
            ground.transform.localScale = new Vector3(60f, 1f, 60f);
            Physics.SyncTransforms();
        }

        private static TownsfolkWalker Walker(string prefab, Vector3[] route, Vector3 at, float yaw = 0f)
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefab), Area + at, Quaternion.Euler(0f, yaw, 0f));
            go.GetComponentInChildren<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate; // far from the camera
            var walker = go.GetComponent<TownsfolkWalker>();
            var points = new Vector3[route.Length];
            for (var i = 0; i < route.Length; i++)
            {
                points[i] = Area + route[i];
            }

            walker.Configure(points, walker.Speed, LayerMask.GetMask("Ground", "Structure"));
            return walker;
        }

        /// <summary>A bench facing north (+z): its seat 0.45 m up, its places 1.2 m apart along x.</summary>
        private static ActivitySpot Spot(ActivitySpot.Activity kind, float seconds, int places = 2, bool quiet = false)
        {
            var list = new ActivitySpot.Place[places];
            for (var i = 0; i < places; i++)
            {
                var seat = Area + new Vector3(i * 1.2f, SeatHeight, 0f);
                list[i] = new ActivitySpot.Place { Seat = seat, Stand = new Vector3(seat.x, Area.y, seat.z + TownsfolkWalker.SeatedHipsBack), Yaw = 0f };
            }

            var spot = new GameObject($"{kind} spot").AddComponent<ActivitySpot>();
            spot.Configure(kind, quiet, seconds, seconds, list);
            return spot;
        }

        private static IEnumerator Until(Func<bool> condition, float seconds, string what)
        {
            var end = Time.time + seconds;
            while (!condition() && Time.time < end)
            {
                yield return null;
            }

            Assert.IsTrue(condition(), $"within {seconds} s: {what}");
        }

        private static string State(TownsfolkWalker w)
        {
            var animator = w.GetComponentInChildren<Animator>();
            var info = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : animator.GetCurrentAnimatorStateInfo(0);
            foreach (var name in new[] { "Walk", "Idle", "StandToSit", "Sit", "SitTalk", "SitToStand", "TalkIdle", "Nod", "ShakeHead", "Drink" })
            {
                if (info.IsName(name))
                {
                    return name;
                }
            }

            return "?";
        }

        /// <summary>The body's facing over its root by the shoulders, degrees.</summary>
        private static float Facing(Animator animator, Transform root)
        {
            var across = root.InverseTransformDirection(animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position - animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position);
            var facing = Vector3.Cross(across, Vector3.up);
            return Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg;
        }

        [UnityTest]
        public IEnumerator QuaterniusClipsFaceTheWayTheRootDoesInPlay()
        {
            yield return Ground();
            var w = Walker(ManWalker, new Vector3[0], new Vector3(10f, 0f, 10f));
            w.enabled = false;
            var animator = w.GetComponentInChildren<Animator>();
            var report = new List<string>();
            var idle = 0f;
            var standing = 0f;
            foreach (var state in new[] { "Idle", "Sit", "StandToSit", "TalkIdle", "Nod", "ShakeHead", "Drink", "FoldArms" })
            {
                animator.Play(state, 0, 0.5f);
                yield return null;
                yield return null;
                var facing = Facing(animator, w.transform);
                idle = state == "Idle" ? facing : idle;
                standing = state == "Idle" ? w.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Hips).position).y : standing;
                report.Add($"{state} {facing:0}");

                // Against the body's own idle, a Kevin Iglesias clip: the shoulders turn with a gesture, never round (they
                // faced back, 148–180 deg off, before the import turned Quaternius' takes).
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(idle, facing)), Is.LessThan(50f), $"{state} faces {facing:0} deg off the root's forward ({string.Join(", ", report)})");
            }

            // Seated, the hips stand where the walker puts them: over the root and behind it.
            animator.Play("Sit", 0, 0.5f);
            yield return null;
            yield return null;
            var hips = w.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Hips).position);
            var k = standing / TownsfolkWalker.StandingHipsUp; // the body against the measured man, by its hips, as the walker reads it
            Assert.AreEqual(TownsfolkWalker.SeatedHipsUp * k, hips.y, 0.04f, "the seated hips' height over the root");
            Assert.AreEqual(-TownsfolkWalker.SeatedHipsBack * k, hips.z, 0.04f, "and behind it");
        }

        [UnityTest]
        public IEnumerator AWalkerSentToABenchSitsTalksWithCompanyAndWalksBack()
        {
            yield return Ground();
            var bench = Spot(ActivitySpot.Activity.Sit, 7f);
            var square = new[] { new Vector3(-3f, 0f, 3f), new Vector3(4f, 0f, 3f), new Vector3(4f, 0f, 9f), new Vector3(-3f, 0f, 9f) };
            var a = Walker(ManWalker, square, square[0]);
            var b = Walker(WomanWalker, square, square[1]);
            yield return null;
            Assert.IsTrue(a.Send(bench, 0), "the bench's first place is free");
            Assert.IsTrue(b.Send(bench, 1));
            Assert.IsFalse(bench.Free(0));
            Assert.IsTrue(a.Away);

            yield return Until(() => a.Seated && State(a) != "StandToSit" && State(a) != "Walk" && State(a) != "Idle", 10f, "the man sits");
            yield return Until(() => b.Seated && State(b) != "StandToSit" && State(b) != "Walk" && State(b) != "Idle", 10f, "the woman sits");
            yield return new WaitForSeconds(1f); // each reads its own seated hips and settles onto its seat
            foreach (var w in new[] { a, b })
            {
                var animator = w.GetComponentInChildren<Animator>();
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips).position;
                var k = w.BodyScale;
                Assert.That(k, Is.InRange(0.8f, 1.1f), $"{w.name}: its scale read from its hips");
                var seat = bench.PlaceAt(w == a ? 0 : 1).Seat;
                Assert.AreEqual(seat.y + (TownsfolkWalker.HipsOverSeat * k), hips.y, 0.08f, $"{w.name}: its hips on the seat");
                Assert.Less(new Vector2(hips.x - seat.x, hips.z - seat.z).magnitude, 0.12f, $"{w.name}: over the seat (hips {w.transform.InverseTransformPoint(hips):F3} over the root, off the seat by {hips - seat:F3}, in {State(w)})");
                Assert.Less(Mathf.Abs(Mathf.DeltaAngle(w.transform.eulerAngles.y, 0f)), 2f, $"{w.name}: facing out from the bench");
            }

            yield return Until(() => State(a) == "SitTalk" && State(b) == "SitTalk", 5f, "two on one bench talk");

            yield return Until(() => !a.Seated && !b.Seated, 10f, "they get up when their while is over");
            Assert.IsTrue(bench.Free(0) && bench.Free(1), "and leave the bench free");
            yield return Until(() => !a.Away && !b.Away, 8f, "and walk back to their route");
            Assert.AreEqual("Walk", State(a));
            Assert.Less(Vector3.Distance(a.transform.position, square[0] + Area), 3f, "the man back where he left his route, walking on");
        }

        [UnityTest]
        public IEnumerator AtAStallABuyerBargainsAndAtADoorAPersonGoesInOutOfSight()
        {
            yield return Ground();
            var stall = Spot(ActivitySpot.Activity.Bargain, 9f, 1);
            var route = new[] { new Vector3(-2f, 0f, 2f), new Vector3(6f, 0f, 2f), new Vector3(6f, 0f, 8f) };
            var buyer = Walker(ManWalker, route, route[0]);
            yield return null;
            Assert.IsTrue(buyer.Send(stall, 0));
            yield return Until(() => buyer.Busy, 6f, "the buyer reaches the stall");
            var seen = new HashSet<string>();
            var end = Time.time + 7f;
            while (Time.time < end)
            {
                seen.Add(State(buyer));
                yield return null;
            }

            Assert.IsTrue(seen.Contains("TalkIdle") && seen.Contains("Nod"), $"it talks and nods: {string.Join(", ", seen)}");
            Assert.IsFalse(buyer.Seated);
            yield return Until(() => !buyer.Away, 10f, "and walks back after its while");

            var door = Spot(ActivitySpot.Activity.GoIn, 3f, 1);
            var visitor = Walker(WomanWalker, route, route[1]);
            yield return null;
            Assert.IsTrue(visitor.Send(door, 0));
            yield return Until(() => visitor.Inside, 10f, "the visitor goes in");
            Assert.IsFalse(visitor.transform.Find("Model").gameObject.activeSelf, "out of sight");
            Assert.IsFalse(visitor.GetComponent<CapsuleCollider>().enabled, "and out of the way");
            yield return Until(() => !visitor.Inside, 5f, "and comes out after its while");
            Assert.IsTrue(visitor.transform.Find("Model").gameObject.activeSelf);
            Assert.IsTrue(visitor.GetComponent<CapsuleCollider>().enabled);
            Assert.IsTrue(door.Free(0));
        }

        [UnityTest]
        public IEnumerator TwoWalkersWhoMeetFaceToFaceNodToOneAnother()
        {
            yield return Ground();
            var east = Walker(ManWalker, new[] { new Vector3(-8f, 0f, 0f), new Vector3(8f, 0f, 0f) }, new Vector3(-8f, 0f, 0f), 90f);
            var west = Walker(WomanWalker, new[] { new Vector3(8f, 0f, 0.6f), new Vector3(-8f, 0f, 0.6f) }, new Vector3(8f, 0f, 0.6f), 270f);
            yield return Until(() => east.Greeting && west.Greeting, 12f, "they greet when they meet");
            Assert.AreEqual(1, east.Greetings);
            Assert.AreEqual(1, west.Greetings);
            Assert.Less(Vector3.Distance(east.transform.position, west.transform.position), TownsfolkWalker.GreetReach + 0.1f);
            yield return null;
            Assert.AreEqual("Nod", State(east));
            yield return Until(() => !east.Greeting && !west.Greeting, 4f, "a nod, and they walk on");
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(1, east.Greetings, "once in 45 s");
            Assert.AreEqual("Walk", State(east));
        }

        [UnityTest]
        public IEnumerator APersonWithNoRouteKeepsItsSeatAndGoesBackToIt()
        {
            yield return Ground();
            var pew = Spot(ActivitySpot.Activity.Sit, 5f, 2, true);
            var worshipper = Walker(WomanWalker, new Vector3[0], new Vector3(0f, 0f, 3f));
            worshipper.SetHome(pew, 1);
            yield return null;
            yield return null;
            Assert.IsTrue(worshipper.Seated && worshipper.Busy, "seated from the start");
            Assert.IsFalse(pew.Free(1));
            yield return new WaitForSeconds(8f);
            Assert.IsTrue(worshipper.Seated, "it keeps its seat past any while");
            Assert.AreEqual("Sit", State(worshipper), "quiet in a pew");

            // Stopped, as an observer that noticed the character stops it, it stands up at once; walking again, it sits again.
            worshipper.enabled = false;
            Assert.IsFalse(worshipper.Seated);
            Assert.IsTrue(pew.Free(1));
            worshipper.enabled = true;
            yield return Until(() => worshipper.Seated && State(worshipper) == "Sit", 6f, "back in its seat");
        }
    }
}
#endif
