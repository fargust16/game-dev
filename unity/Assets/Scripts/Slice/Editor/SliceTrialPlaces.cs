using System.IO;
using System.Linq;
using System.Text;
using GameDev.Slice.People;
using GameDev.Slice.Trial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// S5c.4, once (SLICE_TRIAL §1.2): Thomas Wren's room gets its three Physical Traces in the open, the ledger and the
    /// letter on the table and the burnt page in the cold grate, and the loose board under the bed; the box that stood
    /// for the cache becomes a plain box. Lottie Wren, Mr Ashgrave and Father Hale get their conversations at every post
    /// they keep. The room's trial pieces are rebuilt on each run, and a person already talked to is left as it is. The report
    /// goes to <c>Logs/SliceSurvey/trial_places.txt</c>.
    /// </summary>
    public static class SliceTrialPlaces
    {
        public const string RootName = "Trial - Thomas Wren's room";
        public const string BoardName = "The loose board";
        private const string FactionHouseScenePath = "Assets/Scenes/Slice/Interiors/FactionHouse.unity";
        private const string ReportPath = "Logs/SliceSurvey/trial_places.txt";

        /// <summary>Authoring default, m: a trace's reach, as POI C's.</summary>
        private const float TraceReach = 1.2f;

        private static readonly string[] Talkers = { SliceTrial.Lottie, SliceTrial.Ashgrave, SliceTrial.Hale };

        [MenuItem("GameDev/Slice/Trial/Place the Trial in the Scenes (S5c.4, once)")]
        public static void Run()
        {
            var report = new StringBuilder();
            Room(report);
            foreach (var path in new[] { VeilmouthScene.SootAndBellScenePath, VeilmouthScene.ScenePath, FactionHouseScenePath, SliceActivities.ChapelScenePath })
            {
                Talks(path, report);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? ".");
            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log("[SliceTrialPlaces]\n" + report);
        }

        private static void Room(StringBuilder report)
        {
            EditorSceneManager.OpenScene(VeilmouthDressing.MissingRoomScenePath, OpenSceneMode.Single);
            // The trial's own pieces are rebuilt from the furniture each run; the furniture is left as it is.
            foreach (var old in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name == RootName).ToList())
            {
                Object.DestroyImmediate(old.gameObject);
                report.AppendLine("MissingRoom: the trial's earlier pieces taken away.");
            }

            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            var table = all.FirstOrDefault(t => t.name.StartsWith("Kit_ViFurniture_TableB"));
            var bed = all.FirstOrDefault(t => t.name.StartsWith("Kit_ViFurniture_Bed"));
            var hearth = all.FirstOrDefault(t => t.name == "Fireplace");
            var fire = hearth != null ? hearth.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Fire") : null;
            if (table == null || bed == null || fire == null)
            {
                report.AppendLine($"MissingRoom: no table ({table != null}), bed ({bed != null}) or grate ({fire != null}); nothing placed.");
                return;
            }

            var root = new GameObject(RootName);

            // The ledger and the letter side by side on the table's top, along its longer side.
            var top = BoundsOf(table);
            var along = top.size.x >= top.size.z ? Vector3.right : Vector3.forward;
            var on = new Vector3(top.center.x, top.max.y + 0.005f, top.center.z);
            var leather = SlicePoiC.Material("Trace_Ledger", new Color(0.30f, 0.12f, 0.08f), 0.25f);
            var paper = SlicePoiC.Material("Trace_Paper", new Color(0.86f, 0.82f, 0.70f), 0.05f);
            var burnt = SlicePoiC.Material("Trace_Burnt", new Color(0.10f, 0.08f, 0.06f), 0.05f);

            var ledger = Spot(root, "Trace - the pay ledger", on - (along * 0.25f), "trial.ledger");
            var turn = Quaternion.LookRotation(Vector3.Cross(along, Vector3.up));
            SlicePoiC.Piece(ledger, PrimitiveType.Cube, on - (along * 0.25f) + (Vector3.up * 0.02f), turn * Quaternion.Euler(0f, 8f, 0f), new Vector3(0.22f, 0.04f, 0.30f), leather);
            SlicePoiC.Piece(ledger, PrimitiveType.Cube, on - (along * 0.25f) + (Vector3.up * 0.041f), turn * Quaternion.Euler(0f, 8f, 0f), new Vector3(0.20f, 0.003f, 0.28f), paper);

            var letter = Spot(root, "Trace - the unsent letter", on + (along * 0.25f), "trial.letter");
            SlicePoiC.Piece(letter, PrimitiveType.Cube, on + (along * 0.25f) + (Vector3.up * 0.002f), turn * Quaternion.Euler(0f, -14f, 0f), new Vector3(0.15f, 0.003f, 0.21f), paper);

            // The burnt corner of a page in the cold grate, where the fire would burn, on the hearth's floor.
            var ash = new Vector3(fire.position.x, hearth.position.y + 0.012f, fire.position.z);
            var grate = Spot(root, "Trace - the burnt page", ash, "trial.grate");
            SlicePoiC.Piece(grate, PrimitiveType.Cube, ash, Quaternion.Euler(0f, 33f, 0f), new Vector3(0.08f, 0.002f, 0.06f), burnt);
            SlicePoiC.Piece(grate, PrimitiveType.Cube, ash + new Vector3(0.02f, 0.001f, 0.01f), Quaternion.Euler(0f, 33f, 0f), new Vector3(0.05f, 0.002f, 0.035f), paper);

            // The loose board, under the middle of the bed on the floor.
            var frame = BoundsOf(bed);
            var board = new GameObject(BoardName);
            board.transform.SetParent(root.transform, false);
            board.transform.position = new Vector3(frame.center.x, frame.min.y, frame.center.z);
            board.AddComponent<TrialCache>();

            var box = all.FirstOrDefault(t => t.name == "The cache");
            if (box != null)
            {
                box.name = "Box"; // the cache is under the bed (§1.2), not in a box in the open
            }

            report.AppendLine($"MissingRoom: ledger and letter on {table.name} at {on}, the burnt page at {ash}, the loose board at {board.transform.position}{(box != null ? "; 'The cache' renamed 'Box'" : string.Empty)}.");
            var scene = root.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Transform Spot(GameObject root, string name, Vector3 at, string tip)
        {
            var spot = new GameObject(name);
            spot.transform.SetParent(root.transform, false);
            spot.transform.position = at;
            var look = new GameObject("Look");
            look.transform.SetParent(spot.transform, false);
            spot.AddComponent<TraceSpot>().Configure(null, tip, null, -1, look, TraceReach);
            return look.transform;
        }

        private static Bounds BoundsOf(Transform t)
        {
            var renderers = t.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(t.position, Vector3.zero);
            }

            var bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1))
            {
                bounds.Encapsulate(r.bounds);
            }

            return bounds;
        }

        private static void Talks(string path, StringBuilder report)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var changed = false;
            foreach (var person in Object.FindObjectsByType<NamedPerson>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!Talkers.Contains(person.Id))
                {
                    continue;
                }

                var talk = person.GetComponent<PersonTalk>();
                if (talk != null && talk.Dialogue != null)
                {
                    report.AppendLine($"{scene.name}: {person.name} is talked to already.");
                    continue;
                }

                talk = talk != null ? talk : person.gameObject.AddComponent<PersonTalk>();
                talk.Configure(SlicePeople.DialogueOf(person.Id), SlicePeople.TalkReach(person.Id) ?? 2f);
                report.AppendLine($"{scene.name}: {person.name} ({person.Id}) talks through {talk.Dialogue?.name ?? "nothing"}.");
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
