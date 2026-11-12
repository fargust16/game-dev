using System.Linq;
using GameDev.Slice.EditorTools;
using GameDev.Slice.People;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// AN6, the living townsfolk (the owner's direction of 2026-11-11; ANIMATION_PIPELINE_PLAN §13): Quaternius' takes are
    /// turned round to face the way the root does; the walkers' visits to Veilmouth's benches, stalls and doors are clear straight walks
    /// from their routes' turns; the Soot &amp; Bell has its drinkers and the old chapel the people in its pews' rows.
    /// </summary>
    public sealed class ActivityTests
    {
        /// <summary>
        /// Quaternius' mannequin faces back along the root in every take (by the shoulders, 148–180 deg off, read from the
        /// curves), so the import turns each take round; ActivityPlayTests measures the facing in play, where the turn applies.
        /// </summary>
        [Test]
        public void TheImportTurnsQuaterniusTakesRound()
        {
            var takes = SliceClips.Clips.Where(e => !e.Kevin).ToList();
            Assert.Greater(takes.Count, 20);
            foreach (var e in takes)
            {
                Assert.AreEqual(180f, AnimationUtility.GetAnimationClipSettings(SliceClips.Get(e.Use)).orientationOffsetY, 0.01f, e.Use);
            }
        }

        [Test]
        public void AWalkerGoesOnAVisitAtHalfTheTurnsThatHaveOne()
        {
            var goes = 0;
            for (var seed = 1u; seed <= 20u; seed++)
            {
                for (var turn = 0; turn < 30; turn++)
                {
                    var visit = TownsfolkWalker.VisitsAt(seed * 7919u, turn);
                    Assert.AreEqual(visit, TownsfolkWalker.VisitsAt(seed * 7919u, turn), "the same turn alike on every run");
                    goes += visit ? 1 : 0;
                }
            }

            Assert.That(goes / 600f, Is.InRange(0.42f, 0.58f), "about one turn in two");
        }

        [Test]
        public void InVeilmouthTheTownsfolkHaveBenchesStallsAndDoorsToVisit()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var spots = scene.GetRootGameObjects().Single(g => g.name == SliceActivities.RootName).GetComponentsInChildren<ActivitySpot>();
            Assert.GreaterOrEqual(spots.Count(s => s.Kind == ActivitySpot.Activity.Sit && s.Count > 0), BenchesAtLeast, "the street benches with a seat read");
            Assert.AreEqual(6, spots.Count(s => s.Kind == ActivitySpot.Activity.Bargain && s.Count > 0), "Bawling Row's six stalls, each with a customer's place");
            Assert.AreEqual(2, spots.Count(s => s.Kind == ActivitySpot.Activity.GoIn && s.Count > 0), "the Soot & Bell's and the old chapel's doors");

            foreach (var s in spots.Where(s => s.Kind == ActivitySpot.Activity.Sit))
            {
                for (var i = 0; i < s.Count; i++)
                {
                    Assert.That(s.PlaceAt(i).Seat.y - s.PlaceAt(i).Stand.y, Is.InRange(0.3f, 0.7f), $"{s.name}: a seat's height over the ground before it");
                }
            }

            var walkers = scene.GetRootGameObjects().First(g => g.name == VeilmouthCity.RootName).transform.Find("People").GetComponentsInChildren<TownsfolkWalker>();
            var bySeed = walkers.ToDictionary(w => TownsfolkWalker.SeedOf(w.Route));
            var blocking = LayerMask.GetMask("Default", "Structure", "Prop", "Vegetation", "Interactable");
            var visits = 0;
            foreach (var s in spots)
            {
                foreach (var v in s.Visits)
                {
                    visits++;
                    Assert.IsTrue(bySeed.TryGetValue(v.Walker, out var w), $"{s.name}: a visit by a walker of Veilmouth");
                    Assert.Less(Vector3.Distance(Flat(w.Route[v.Turn]), Flat(v.At)), 0.1f, $"{w.name}: the visit leaves its route at a turn");
                    var to = s.PlaceAt(v.Place).Stand;
                    Assert.LessOrEqual(Vector3.Distance(Flat(v.At), Flat(to)), SliceActivities.VisitReach, $"{w.name}: within reach");

                    // A person's capsule fits along the walk, but in its last 0.6 m; the bench, stall or door walked to may stand beside it.
                    var length = Vector3.Distance(Flat(v.At), Flat(to));
                    var steps = Mathf.CeilToInt(length / 0.5f);
                    for (var i = 0; i <= steps && (1f - ((float)i / steps)) * length >= SliceActivities.FreeEnd; i++)
                    {
                        var p = Vector3.Lerp(v.At, to, (float)i / steps);
                        Assert.IsTrue(Physics.Raycast(p + (Vector3.up * 1.5f), Vector3.down, out var hit, 4f, LayerMask.GetMask("Ground", "Structure"), QueryTriggerInteraction.Ignore), $"{w.name} to {s.name}: ground at {p}");
                        p.y = hit.point.y;
                        var stop = Physics.OverlapCapsule(p + (Vector3.up * 0.75f), p + (Vector3.up * 1.45f), 0.25f, blocking, QueryTriggerInteraction.Ignore)
                            .Where(c => s.Piece == null || !c.transform.IsChildOf(s.Piece)).Select(c => c.name).ToArray();
                        Assert.IsEmpty(stop, $"{w.name} to {s.name}: something stands in the way at {p}");
                    }
                }
            }

            Assert.Greater(visits, 0);
            Assert.GreaterOrEqual(walkers.Count(w => spots.Any(s => s.Visits.Any(v => v.Walker == TownsfolkWalker.SeedOf(w.Route)))), WalkersWithAVisitAtLeast, "most walkers have somewhere to go");
        }

        [Test]
        public void TheSootAndBellHasItsDrinkersAndTheOldChapelItsPeople()
        {
            EditorSceneManager.OpenScene(VeilmouthScene.SootAndBellScenePath, OpenSceneMode.Single);
            var drinkers = Object.FindObjectsByType<TownsfolkWalker>(FindObjectsSortMode.None).Where(w => w.Home != null).ToList();
            Assert.AreEqual(3, drinkers.Count, "two at a table and one at the bar");
            Assert.IsTrue(drinkers.All(w => w.Route.Length == 0), "they walk no route");
            var seated = drinkers.Where(w => w.Home.Kind == ActivitySpot.Activity.Sit).ToList();
            Assert.AreEqual(2, seated.Count);
            Assert.AreSame(seated[0].Home, seated[1].Home, "at one spot, so they talk");
            Assert.IsFalse(seated[0].Home.Quiet);
            Assert.AreNotEqual(seated[0].HomePlace, seated[1].HomePlace);
            Assert.Less(Vector3.Distance(seated[0].Home.PlaceAt(seated[0].HomePlace).Seat, seated[1].Home.PlaceAt(seated[1].HomePlace).Seat), 2f, "at one table");
            Assert.AreEqual(1, drinkers.Count(w => w.Home.Kind == ActivitySpot.Activity.Drink), "one at the bar");

            // The player still walks up to Nell: nobody stands where the player talks to her from (ConversationTests).
            var nell = Object.FindObjectsByType<PersonTalk>(FindObjectsSortMode.None).Single(t => t.Dialogue.Id == "nell").transform;
            var before = Flat(nell.position + (nell.forward * 1.6f));
            foreach (var w in drinkers)
            {
                Assert.Greater(Vector3.Distance(Flat(w.Home.PlaceAt(w.HomePlace).Stand), before), 1f, $"{w.name} clear of the place before Nell");
            }

            EditorSceneManager.OpenScene(SliceActivities.ChapelScenePath, OpenSceneMode.Single);
            var worshippers = Object.FindObjectsByType<TownsfolkWalker>(FindObjectsSortMode.None).Where(w => w.Home != null).ToList();
            Assert.AreEqual(3, worshippers.Count);

            // No prayer in either clip pack (99 VS-ANIM): they stand in its rows, though since the owner's seat trial the pew is low enough to sit on.
            Assert.IsTrue(worshippers.All(w => w.Home.Kind == ActivitySpot.Activity.Attend && w.Home.Quiet), "standing quietly in the pews' rows");
            Assert.AreEqual(3, worshippers.Select(w => w.HomePlace).Distinct().Count());
        }

        /// <summary>
        /// From the run of SliceActivities on C5 of 2026-11-12 (activities.txt): 32 benches, every seat read, and 22 of the 46
        /// walkers with a visit; the rest walk where no bench or stall stands within 40 m of a turn (the slums, the works,
        /// the quay) or every walk is stopped by a building.
        /// </summary>
        private const int BenchesAtLeast = 32;

        private const int WalkersWithAVisitAtLeast = 22;

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
