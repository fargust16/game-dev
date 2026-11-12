#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private const float SeatHeight = TownsfolkWalker.SeatTop;

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

        /// <summary>A bench facing north (+z): its seat at the owner's 0.42 m, its places 1.2 m apart along x.</summary>
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

        private static readonly string[] Bodies =
        {
            ManWalker, "Assets/Prefabs/Slice/Townswoman.prefab", "Assets/Prefabs/Slice/People/GentlemanWalker.prefab",
            "Assets/Prefabs/Slice/People/LadyWalker.prefab", WomanWalker,
        };

        /// <summary>Every skin of the body baked as it stands now, in its root's frame.</summary>
        private static List<Vector3> Skin(GameObject body, Transform root)
        {
            var list = new List<Vector3>();
            var mesh = new Mesh();
            foreach (var s in body.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!s.enabled || !s.gameObject.activeInHierarchy)
                {
                    continue;
                }

                // Baked unscaled, in the renderer's frame without its scale (measured: the Tripo people carry 1.7 of scale
                // above the renderer, which either other pairing counts once too few or too many times).
                s.BakeMesh(mesh, false);
                var m = Matrix4x4.TRS(s.transform.position, s.transform.rotation, Vector3.one);
                list.AddRange(mesh.vertices.Select(v => root.InverseTransformPoint(m.MultiplyPoint3x4(v))));
            }

            Object.Destroy(mesh);
            return list;
        }

        /// <summary>
        /// The seated body in play, for the sizes of seats (the owner's question of 2026-11-12): each walker's skin baked
        /// in Sit and through SitTalk, in its root's frame (+z forward, the root on the floor). Writes
        /// <c>Logs/SliceSurvey/seated_body.txt</c>.
        /// </summary>
        [UnityTest, Explicit("diagnostic: run by name"), Category("Diagnostic")]
        public IEnumerator MeasureTheSeatedBody()
        {
            yield return Ground();
            var report = new StringBuilder("The seated body in play (ActivityPlayTests.MeasureTheSeatedBody): m in the root's frame, +z forward, the root on the floor;\n" +
                                           "under = the lowest skin under the hips (where a seat's top meets it), back@h = the back's skin h over that, knees = the knee joints\n\n");
            var x = 0f;
            foreach (var prefab in Bodies)
            {
                var w = Walker(prefab, new Vector3[0], new Vector3(x, 0f, 20f));
                x += 3f;
                w.enabled = false;
                var animator = w.GetComponentInChildren<Animator>();
                var hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);
                animator.Play("Idle", 0, 0.5f);
                yield return null;
                yield return null;
                var standing = Skin(w.gameObject, w.transform);
                report.AppendLine($"{System.IO.Path.GetFileNameWithoutExtension(prefab)}: standing {standing.Max(v => v.y):0.00} tall, its soles at {standing.Min(v => v.y):0.00}, " +
                                  $"its hips {w.transform.InverseTransformPoint(hipsBone.position).y:0.00} up, half its width {standing.Max(v => Mathf.Abs(v.x)):0.00}");
                foreach (var (state, times) in new[] { ("Sit", new[] { 0.25f, 0.5f, 0.75f }), ("SitTalk", Enumerable.Range(0, 10).Select(i => 0.05f + (i * 0.1f)).ToArray()) })
                {
                    var rows = new List<float[]>();
                    foreach (var t in times)
                    {
                        animator.Play(state, 0, t);
                        yield return null;
                        yield return null;
                        var v = Skin(w.gameObject, w.transform);
                        var hips = w.transform.InverseTransformPoint(hipsBone.position);
                        var knees = new[] { HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg }.Select(b => w.transform.InverseTransformPoint(animator.GetBoneTransform(b).position)).ToArray();
                        var under = v.Where(p => Mathf.Abs(p.x - hips.x) < 0.12f && p.z > hips.z - 0.15f && p.z < hips.z + 0.1f).Min(p => p.y);
                        float Back(float h) => v.Where(p => Mathf.Abs(p.y - (under + h)) < 0.04f && Mathf.Abs(p.x - hips.x) < 0.2f).Select(p => p.z).DefaultIfEmpty(float.NaN).Min();
                        rows.Add(new[]
                        {
                            hips.y, hips.z, under, v.Where(p => p.y < under + 0.2f && p.z < hips.z + 0.05f).Min(p => p.z), Back(0.15f), Back(0.35f), Back(0.55f),
                            knees.Max(k => k.z), knees.Max(k => k.y), v.Min(p => p.y), v.Where(p => p.y < 0.2f).Select(p => p.z).DefaultIfEmpty(float.NaN).Max(),
                            v.Where(p => p.y > under && p.y < under + 0.15f && p.z < knees.Min(k => k.z) - 0.05f).Select(p => Mathf.Abs(p.x - hips.x)).DefaultIfEmpty(float.NaN).Max(),
                            v.Where(p => p.y > under + 0.1f && p.y < under + 0.45f).Select(p => Mathf.Abs(p.x - hips.x)).DefaultIfEmpty(float.NaN).Max(),
                        });
                    }

                    string R(int i) => $"{rows.Min(r => r[i]):0.00}..{rows.Max(r => r[i]):0.00}";
                    report.AppendLine($"  {state}: hips up {R(0)} and z {R(1)}; under {R(2)}; seat of the trousers z {R(3)}; back z @0.15 {R(4)}, @0.35 {R(5)}, @0.55 {R(6)}; " +
                                      $"knees z {R(7)} y {R(8)}; soles y {R(9)}, toes z {R(10)}; half-width of the hips and thighs {R(11)}, at arm-rest height (under +0.1..0.45) {R(12)}");
                }

                Object.Destroy(w.gameObject);
            }

            System.IO.Directory.CreateDirectory("Logs/SliceSurvey");
            System.IO.File.WriteAllText("Logs/SliceSurvey/seated_body.txt", report.ToString());
            Debug.Log("[MeasureTheSeatedBody]\n" + report);
        }

        /// <summary>
        /// The seated bodies seen from the side on trial seats, their feet on the floor (the owner's question of 2026-11-12):
        /// a seat 0.50 m deep with its front 0.02 m before the root and a back behind it, at each trial height. Writes
        /// <c>Logs/SliceSurvey/seated_side_&lt;cm&gt;.png</c>.
        /// </summary>
        [UnityTest, Explicit("diagnostic: run by name"), Category("Diagnostic")]
        public IEnumerator RenderTheSeatedBodies()
        {
            yield return Ground();
            var origin = Area + new Vector3(0f, 0f, -20f);
            var parts = new List<Transform>();
            for (var i = 0; i < Bodies.Length; i++)
            {
                var w = Walker(Bodies[i], new Vector3[0], origin - Area + new Vector3(0f, 0f, i * 1.6f));
                w.enabled = false;
                w.GetComponentInChildren<Animator>().Play("Sit", 0, 0.5f);
                var seat = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                var back = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                foreach (var c in new[] { seat, back })
                {
                    Object.Destroy(c.GetComponent<Collider>());
                    parts.Add(c);
                }
            }

            var camera = new GameObject("Side camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1.05f;
            camera.transform.SetPositionAndRotation(origin + new Vector3(6f, 0.95f, 3f), Quaternion.Euler(0f, -90f, 0f)); // looking west: +z to the right
            camera.backgroundColor = new Color(0.85f, 0.87f, 0.9f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            var texture = new RenderTexture(2850, 750, 24);
            camera.targetTexture = texture;
            camera.aspect = 3.8f;
            foreach (var height in new[] { 0.40f, 0.45f })
            {
                for (var i = 0; i < Bodies.Length; i++)
                {
                    var z = origin.z + (i * 1.6f);
                    parts[2 * i].position = new Vector3(origin.x, origin.y + (height * 0.5f), z - 0.23f);
                    parts[2 * i].localScale = new Vector3(0.5f, height, 0.5f);
                    parts[(2 * i) + 1].position = new Vector3(origin.x, origin.y + height + 0.25f, z - 0.51f);
                    parts[(2 * i) + 1].localScale = new Vector3(0.5f, 0.5f, 0.04f);
                }

                yield return null;
                yield return null;
                camera.Render();
                RenderTexture.active = texture;
                var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                RenderTexture.active = null;
                System.IO.Directory.CreateDirectory("Logs/SliceSurvey");
                System.IO.File.WriteAllBytes($"Logs/SliceSurvey/seated_side_{Mathf.RoundToInt(height * 100f)}.png", image.EncodeToPNG());
            }

            // Close up, about 1 mm to a pixel: each body's hips on each trial seat, a row to a height.
            var close = new RenderTexture(3000, 1200, 24);
            camera.targetTexture = close;
            camera.orthographicSize = 0.32f;
            var heights = new[] { 0.40f, 0.45f };
            for (var row = 0; row < heights.Length; row++)
            {
                for (var i = 0; i < Bodies.Length; i++)
                {
                    var z = origin.z + (i * 1.6f);
                    parts[2 * i].position = new Vector3(origin.x, origin.y + (heights[row] * 0.5f), z - 0.23f);
                    parts[2 * i].localScale = new Vector3(0.5f, heights[row], 0.5f);
                    parts[(2 * i) + 1].position = new Vector3(origin.x, origin.y + heights[row] + 0.25f, z - 0.51f);
                }

                yield return null;
                for (var i = 0; i < Bodies.Length; i++)
                {
                    camera.rect = new Rect(i / (float)Bodies.Length, row == 0 ? 0.5f : 0f, 1f / Bodies.Length, 0.5f);
                    camera.aspect = 1f;
                    camera.transform.position = new Vector3(origin.x + 6f, origin.y + 0.45f, origin.z + (i * 1.6f) - 0.2f);
                    camera.Render();
                }
            }

            RenderTexture.active = close;
            var sheet = new Texture2D(close.width, close.height, TextureFormat.RGB24, false);
            sheet.ReadPixels(new Rect(0, 0, close.width, close.height), 0, 0);
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes("Logs/SliceSurvey/seated_close.png", sheet.EncodeToPNG());
            Debug.Log("[RenderTheSeatedBodies] Logs/SliceSurvey/seated_side_40.png, _45.png, seated_close.png");
        }

        // ------------------------------------------------------------- the kit's seats seen (the owner, 2026-11-12)

        /// <summary>
        /// Every seat's places as the editor reads them, which a play test cannot (a mesh not marked readable): written by
        /// <c>SliceActivities.SurveySeats</c>, run first. Kind (kit or room), prefab or scene, the kit seat's turn, places.
        /// </summary>
        private static List<(string Kind, string Path, float Yaw, ActivitySpot.Place[] Places)> SeatPlaces()
        {
            const string file = "Logs/SliceSurvey/seat_places.txt";
            Assert.IsTrue(System.IO.File.Exists(file), $"{file}: run SliceActivities.SurveySeats first");
            float F(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            ActivitySpot.Place[] Places(string text) => text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split(' ').Select(F).ToArray())
                .Select(v => new ActivitySpot.Place { Seat = new Vector3(v[0], v[1], v[2]), Stand = new Vector3(v[3], v[4], v[5]), Yaw = v[6] }).ToArray();
            return System.IO.File.ReadAllLines(file).Select(l => l.Split('\t')).Where(f => f.Length >= 3)
                .Select(f => (f[0], f[1], F(f[2]), f.Length > 3 ? Places(f[3]) : new ActivitySpot.Place[0])).ToList();
        }

        /// <summary>Every loose prop held where it stands: a body set down in its seat would push a loose chair over.</summary>
        private static void HoldLooseProps()
        {
            foreach (var body in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Where(b => b.GetComponentInParent<Animator>() == null))
            {
                body.isKinematic = true;
            }
        }

        private static GameObject SeatedBody(string prefab, ActivitySpot.Place place)
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefab), place.Stand, Quaternion.Euler(0f, place.Yaw, 0f));
            var animator = go.GetComponentInChildren<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var walker = go.GetComponent<TownsfolkWalker>();
            if (walker != null)
            {
                walker.enabled = false;
            }

            animator.Play("Sit", 0, 0.5f);
            return go;
        }

        private static Bounds Seen(GameObject g)
        {
            var renderers = g.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            var b = renderers[0].bounds;
            foreach (var r in renderers)
            {
                b.Encapsulate(r.bounds);
            }

            return b;
        }

        private static Texture2D Read(RenderTexture texture)
        {
            RenderTexture.active = texture;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            RenderTexture.active = null;
            return image;
        }

        /// <summary>Writes <c>Logs/SliceSurvey/&lt;name&gt;.png</c>, and with a <c>&lt;name&gt;_before.png</c> beside it, the two one over the other as <c>&lt;name&gt;_compare.png</c>.</summary>
        private static void Write(string name, Texture2D image)
        {
            System.IO.Directory.CreateDirectory("Logs/SliceSurvey");
            System.IO.File.WriteAllBytes($"Logs/SliceSurvey/{name}.png", image.EncodeToPNG());
            var before = $"Logs/SliceSurvey/{name}_before.png";
            if (!System.IO.File.Exists(before))
            {
                return;
            }

            var old = new Texture2D(2, 2);
            old.LoadImage(System.IO.File.ReadAllBytes(before));
            if (old.width != image.width)
            {
                return;
            }

            const int band = 8;
            var both = new Texture2D(image.width, old.height + band + image.height, TextureFormat.RGB24, false);
            both.SetPixels(0, 0, image.width, image.height, image.GetPixels());
            both.SetPixels(0, image.height, image.width, band, Enumerable.Repeat(Color.black, image.width * band).ToArray());
            both.SetPixels(0, image.height + band, old.width, old.height, old.GetPixels());
            System.IO.File.WriteAllBytes($"Logs/SliceSurvey/{name}_compare.png", both.EncodeToPNG());
        }

        /// <summary>
        /// Every seat of the kit with the townsfolk seated where they sit (the owner's trial of 2026-11-12, the seats'
        /// scale): a sheet of 4 × 3, in the order of their names, each turned to face the camera, from the front at
        /// three quarters and from the side. Writes <c>kit_seats_front.png</c> and <c>kit_seats_side.png</c>.
        /// </summary>
        [UnityTest, Explicit("diagnostic: run by name"), Category("Diagnostic")]
        public IEnumerator RenderTheKitSeats()
        {
            yield return Ground();
            var kit = SeatPlaces().Where(s => s.Kind == "kit").ToList();
            var origin = Area + new Vector3(-25f, 0f, 22f);
            var centres = new List<Vector3>();
            var body = 0;
            for (var i = 0; i < kit.Count; i++)
            {
                var at = origin + new Vector3(i * 4f, 0f, i * -4f); // on a diagonal out of each other's views
                var seat = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(kit[i].Path), at, Quaternion.Euler(0f, kit[i].Yaw, 0f)); // its front toward -z, the camera
                HoldLooseProps();
                foreach (var place in kit[i].Places)
                {
                    SeatedBody(Bodies[body++ % Bodies.Length], new ActivitySpot.Place { Seat = at + place.Seat, Stand = at + place.Stand, Yaw = place.Yaw });
                }

                centres.Add(Seen(seat).center);
                Debug.Log($"[RenderTheKitSeats] {i + 1}: {System.IO.Path.GetFileNameWithoutExtension(kit[i].Path)}, {kit[i].Places.Length} seated");
            }

            yield return new WaitForSeconds(1f);
            var camera = new GameObject("Seat camera").AddComponent<Camera>();
            camera.backgroundColor = new Color(0.85f, 0.87f, 0.9f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            foreach (var side in new[] { false, true })
            {
                var sheet = new RenderTexture(side ? 2400 : 3200, side ? 1800 : 1800, 24);
                camera.targetTexture = sheet;
                camera.orthographic = side;
                camera.orthographicSize = 0.85f;
                camera.fieldOfView = 35f;
                for (var i = 0; i < centres.Count && i < 12; i++)
                {
                    camera.rect = new Rect((i % 4) / 4f, 1f - (((i / 4) + 1) / 3f), 0.25f, 1f / 3f);
                    camera.aspect = side ? 1f : 4f / 3f;
                    var c = centres[i];
                    if (side)
                    {
                        camera.transform.SetPositionAndRotation(new Vector3(c.x + 6f, origin.y + 0.75f, c.z), Quaternion.Euler(0f, -90f, 0f));
                    }
                    else
                    {
                        var eye = c + new Vector3(-2.2f, 0.9f, -3.6f);
                        camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(c + (Vector3.up * 0.05f) - eye));
                    }

                    camera.Render();
                }

                Write(side ? "kit_seats_side" : "kit_seats_front", Read(sheet));
            }
        }

        /// <summary>
        /// The seats where they stand, every free place taken (the owner's trial of 2026-11-12): the Soot &amp; Bell's
        /// taproom and the old chapel, each from its two most open sides, and two of Veilmouth's street benches. Writes
        /// <c>seats_soot_and_bell.png</c>, <c>seats_old_chapel.png</c> and <c>seats_street.png</c>.
        /// </summary>
        [UnityTest, Explicit("diagnostic: run by name"), Category("Diagnostic")]
        public IEnumerator RenderTheSeatsInPlace()
        {
            yield return Room("Assets/Scenes/Slice/Interiors/SootAndBell.unity", "seats_soot_and_bell");
            yield return Room("Assets/Scenes/Slice/Interiors/OldChapel.unity", "seats_old_chapel");

            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var benches = ActivitySpot.All.Where(s => s.Kind == ActivitySpot.Activity.Sit && s.Piece != null && s.Piece.name.Contains("Bench")).Take(2).ToList();
            HoldLooseProps();
            var texture = new RenderTexture(3200, 900, 24);
            var camera = new GameObject("Bench camera").AddComponent<Camera>();
            camera.targetTexture = texture;
            camera.fieldOfView = 40f;
            var body = 0;
            foreach (var spot in benches)
            {
                for (var p = 0; p < spot.Count; p++)
                {
                    SeatedBody(Bodies[body++ % Bodies.Length], spot.PlaceAt(p));
                }
            }

            yield return new WaitForSeconds(1f);
            for (var i = 0; i < benches.Count; i++)
            {
                var place = benches[i].PlaceAt(0);
                var middle = Enumerable.Range(0, benches[i].Count).Select(p => benches[i].PlaceAt(p).Seat).Aggregate(Vector3.zero, (a, v) => a + v) / benches[i].Count;
                var forward = Quaternion.Euler(0f, place.Yaw, 0f) * Vector3.forward;
                var right = Vector3.Cross(Vector3.up, forward);
                var eye = middle + (forward * 3.2f) + (right * 1.4f) + (Vector3.up * 0.8f);
                camera.rect = new Rect(i * 0.5f, 0f, 0.5f, 1f);
                camera.aspect = 16f / 9f;
                camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(middle + (Vector3.up * 0.2f) - eye));
                camera.Render();
            }

            Write("seats_street", Read(texture));
        }

        private static IEnumerator Room(string scene, string name)
        {
            yield return LoadScene(scene);
            var people = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).Select(a => a.transform.position).ToList();
            var seats = SeatPlaces().Where(s => s.Kind == "room" && s.Path == scene).ToList();
            HoldLooseProps();
            var ground = seats.SelectMany(s => s.Places).Select(p => p.Stand.y).DefaultIfEmpty(0f).Min();
            var body = 0;
            var centres = new List<Vector3>();
            foreach (var seat in seats)
            {
                foreach (var place in seat.Places.Where(p => p.Stand.y < ground + 1f)) // the ground floor's
                {
                    centres.Add(place.Seat);
                    if (body < 18 && people.All(p => Vector3.Distance(Flat(p), Flat(place.Seat)) > 0.6f))
                    {
                        SeatedBody(Bodies[body++ % Bodies.Length], place);
                    }
                }
            }

            Debug.Log($"[RenderTheSeatsInPlace] {scene}: {seats.Count} seats, {centres.Count} places, {body} seated");
            yield return new WaitForSeconds(1f);
            var centre = centres.Count > 0 ? centres.Aggregate(Vector3.zero, (a, v) => a + v) / centres.Count : Vector3.zero;
            var floor = ground;
            var eyeLevel = new Vector3(centre.x, floor + 2.3f, centre.z);

            // The two most open ways from the seats' middle, at least 90° apart.
            var ways = Enumerable.Range(0, 24).Select(k => Quaternion.Euler(0f, k * 15f, 0f) * Vector3.forward)
                .Select(d => (d, far: Physics.Raycast(eyeLevel, d, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore) ? hit.distance : 10f))
                .OrderByDescending(w => w.far).ToList();
            var first = ways[0];
            var second = ways.First(w => Vector3.Angle(w.d, first.d) >= 90f);
            var texture = new RenderTexture(3200, 900, 24);
            var camera = new GameObject("Room camera").AddComponent<Camera>();
            camera.targetTexture = texture;
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.05f;
            var views = new[] { first, second };
            for (var i = 0; i < views.Length; i++)
            {
                var eye = eyeLevel + (views[i].d * Mathf.Max(0.5f, views[i].far - 0.4f));
                camera.rect = new Rect(i * 0.5f, 0f, 0.5f, 1f);
                camera.aspect = 16f / 9f;
                camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(new Vector3(centre.x, floor + 0.7f, centre.z) - eye));
                camera.Render();
            }

            Write(name, Read(texture));
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

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

        /// <summary>
        /// Diagnostic, run by name: every person's walk and idle over its root, degrees, positive to the right (clockwise
        /// from above), averaged over the clip: the shoulders, the hips by the thighs, the feet by heel to toe, and the
        /// animator's body rotation. Writes <c>Logs/SliceSurvey/walk_facing.txt</c>.
        /// </summary>
        [UnityTest, Explicit("diagnostic: run by name"), Category("Diagnostic")]
        public IEnumerator MeasureTheWalkFacing()
        {
            yield return Ground();
            var report = new StringBuilder("Facing over the root, deg, + to the right; shoulders / hips / feet / body rotation, mean (min..max)\n");
            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Slice" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.Contains("/SliceKit/") && AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponentInChildren<Animator>() != null)
                .ToList();
            var at = 0;
            foreach (var path in prefabs)
            {
                var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), Area + new Vector3(-20f + (at++ * 3f), 0f, -20f), Quaternion.identity);
                foreach (var b in go.GetComponentsInChildren<MonoBehaviour>())
                {
                    b.enabled = false;
                }

                var animator = go.GetComponentInChildren<Animator>();
                if (!animator.isHuman || animator.GetBoneTransform(HumanBodyBones.LeftUpperArm) == null)
                {
                    Object.Destroy(go);
                    continue;
                }

                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;
                yield return null;
                foreach (var state in new[] { "Walk", "Idle", "Run" })
                {
                    var hash = Animator.StringToHash(state);
                    if (!animator.HasState(0, hash))
                    {
                        continue;
                    }

                    var rows = new List<float[]>();
                    for (var i = 0; i < 16; i++)
                    {
                        animator.Play(hash, 0, i / 16f);
                        animator.Update(0f);
                        rows.Add(new[]
                        {
                            Facing(animator, go.transform),
                            Across(go.transform, animator, HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg),
                            Feet(go.transform, animator),
                            Yaw(go.transform.InverseTransformDirection(animator.bodyRotation * Vector3.forward)),
                        });
                    }

                    string Col(int c) => $"{Mean(rows.Select(r => r[c])):+0;-0} ({rows.Min(r => r[c]):0}..{rows.Max(r => r[c]):0})";
                    report.AppendLine($"{System.IO.Path.GetFileNameWithoutExtension(path)} {state} [{animator.runtimeAnimatorController.name}]: {Col(0)} / {Col(1)} / {Col(2)} / {Col(3)}");
                }

                Object.Destroy(go);
            }

            System.IO.Directory.CreateDirectory("Logs/SliceSurvey");
            System.IO.File.WriteAllText("Logs/SliceSurvey/walk_facing.txt", report.ToString());
            Debug.Log("[MeasureTheWalkFacing]\n" + report);
        }

        /// <summary>
        /// The owner's play test of 2026-11-12: every walker, and the player in both bodies, walks forward facing its way,
        /// the body over the walk within 4° of the root's forward. The walkers went 13–30° to the right before the
        /// model's facing was read in the avatar's neutral pose (SlicePeopleModels.Fit), and the player 7° to the left.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryWalkerWalksFacingItsWay()
        {
            yield return Ground();
            var means = new List<(string name, float deg)>();
            var at = 0;
            foreach (var path in Bodies.Concat(new[] { "Assets/Prefabs/Slice/SlicePlayer.prefab", "Assets/Prefabs/Slice/SlicePlayerWoman.prefab" }))
            {
                var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), Area + new Vector3(-20f + (at++ * 3f), 0f, 0f), Quaternion.identity);
                foreach (var b in go.GetComponentsInChildren<MonoBehaviour>())
                {
                    b.enabled = false;
                }

                var animator = go.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;
                yield return null;
                var walk = Animator.StringToHash("Walk");
                var state = animator.HasState(0, walk) ? walk : Animator.StringToHash("Locomotion");
                var body = new List<float>();
                for (var i = 0; i < 16; i++)
                {
                    if (state != walk)
                    {
                        animator.SetFloat("MoveX", 0f);
                        animator.SetFloat("MoveZ", 1f); // the player's walk forward in its blend
                    }

                    animator.Play(state, 0, i / 16f);
                    animator.Update(0f);
                    body.Add(Yaw(go.transform.InverseTransformDirection(animator.bodyRotation * Vector3.forward)));
                }

                means.Add((System.IO.Path.GetFileNameWithoutExtension(path), Mean(body)));
                Object.Destroy(go);
            }

            Assert.IsTrue(means.All(m => Mathf.Abs(m.deg) < 4f), "every body walks facing its way: " + string.Join(", ", means.Select(m => $"{m.name} {m.deg:+0.0;-0.0}")));
        }

        private static float Yaw(Vector3 v) => Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;

        private static float Across(Transform root, Animator animator, HumanBodyBones left, HumanBodyBones right) =>
            Yaw(Vector3.Cross(root.InverseTransformDirection(animator.GetBoneTransform(right).position - animator.GetBoneTransform(left).position), Vector3.up));

        private static float Feet(Transform root, Animator animator)
        {
            var sum = Vector3.zero;
            foreach (var (foot, toes) in new[] { (HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes), (HumanBodyBones.RightFoot, HumanBodyBones.RightToes) })
            {
                var f = animator.GetBoneTransform(foot);
                var t = animator.GetBoneTransform(toes);
                if (f != null && t != null)
                {
                    sum += Flat(root.InverseTransformDirection(t.position - f.position)).normalized;
                }
            }

            return sum == Vector3.zero ? float.NaN : Yaw(sum);
        }

        private static float Mean(IEnumerable<float> degrees)
        {
            var s = degrees.Where(d => !float.IsNaN(d)).Aggregate(Vector2.zero, (a, d) => a + new Vector2(Mathf.Sin(d * Mathf.Deg2Rad), Mathf.Cos(d * Mathf.Deg2Rad)));
            return Mathf.Atan2(s.x, s.y) * Mathf.Rad2Deg;
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
                Assert.AreEqual(Area.y, w.transform.position.y, 0.02f, $"{w.name}: its feet on the floor");
                Assert.That(hips.y - seat.y, Is.InRange(0.08f, 0.25f), $"{w.name}: its hip joint over the seat's top, the skin under it on the seat (MeasureTheSeatedBody)");
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
