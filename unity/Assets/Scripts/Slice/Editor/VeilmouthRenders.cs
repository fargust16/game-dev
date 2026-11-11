using System.IO;
using System.Linq;
using GameDev.VictorianKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Pictures of the dressed Veilmouth and its interiors for checking a dressing pass: views from above and from a
    /// walker's height, written to Logs/SliceSurvey outside Assets. The ritual circles are shown for the picture only,
    /// and nothing is saved. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderAll
    /// </summary>
    public static class VeilmouthRenders
    {
        private const string OutputFolder = "Logs/SliceSurvey";

        [MenuItem("GameDev/Slice/Render Veilmouth Views")]
        public static void RenderAll()
        {
            // Since C5 (city plan §29.4) the city's views are C5's; the interiors follow.
            RenderC5();
            RenderInteriors();
            Debug.Log($"Veilmouth views written to {OutputFolder}.");
        }

        /// <summary>
        /// The city of plan C4 (VEILMOUTH_CITY_PLAN.md §28) from above and from a walker's height, with its people, dogs,
        /// ornaments and mystic layer (§28.10). Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderC4
        /// </summary>
        public static void RenderC4()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            ShowCircles();
            Shader.SetGlobalVector("_Growth_WindSettings", VeilmouthDressing.WindSettings);
            Shader.SetGlobalTexture("_Growth_WindNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(VeilmouthDressing.WindNoisePath));
            Top("c4_world", Vector3.zero, 500f);
            Top("c4_city", new Vector3(50f, 0f, 18f), 225f);
            Top("c4_hamlet", new Vector3(-190f, 0f, 60f), 70f);
            Top("c4_penny", new Vector3(40f, 0f, 60f), 50f);
            Top("c4_parsons", new Vector3(50f, 0f, 195f), 65f);
            Top("c4_sleepy", new Vector3(-25f, 0f, 45f), 60f);
            Top("c4_lowerveil", new Vector3(120f, 0f, 5f), 60f);
            Top("c4_reek", new Vector3(50f, 0f, -145f), 75f);
            Eye("c4_eye_longtongue", -70f, 60f, 40f, 60f, 3f);
            Eye("c4_eye_saltgullet", 150f, 60f, 40f, 60f, 3f);
            Eye("c4_eye_lenten", 40f, 128f, 40f, 60f, 4f);
            Eye("c4_eye_smoked", 40f, -30f, 40f, 60f, 4f);
            Eye("c4_eye_embankment", -70f, -47f, 150f, -47f, 2f);
            Eye("c4_eye_gate", -140f, 60f, -60f, 60f, 4f);
            Eye("c4_eye_grubby", 40f, -100f, 40f, -30f, 3f);
            Eye("c4_eye_quay", 170f, -40f, 170f, 120f, 2f);
            Eye("c4_eye_slant", 80f, 20f, 107f, -7f, 2f);
            Eye("c4_eye_widows", -75f, 140f, 150f, 140f, 3f);
            Eye("c4_eye_penny", 2f, 58f, 40f, 60f, 2f);
            Eye("c4_eye_cathedral", 40f, 108f, 40f, 186f, 2f);
            Eye("c4_eye_market", -49f, 52f, -49f, 5f, 2f);
            Eye("c4_eye_cemetery", -63f, 200f, -20f, 205f, 2f);
            Eye("c4_eye_works", 40f, -108f, 40f, -190f, 2f);
            Eye("c4_eye_hamlet", -135f, 60f, -250f, 60f, 2f);
            Eye("c4_eye_gloomwood", -240f, -50f, -200f, -110f, 2f);
            // The slice's places (§28.10): the Masters before their stalls, the Wardens at the Assize, the Black Puddle with
            // the pawnbroker, Draught Lane where POI B happens, the lions before the Governor's House.
            Eye("c4_eye_masters", -49f, 28f, -55f, 40f, 1.7f);
            Eye("c4_eye_assize", 66f, 88f, 66f, 103f, 1.7f);
            Eye("c4_eye_puddle", 112f, -30f, 133f, -1f, 1.7f);
            Eye("c4_eye_draught", 158f, 23.5f, 115f, 23.5f, 1.7f);
            Eye("c4_eye_lions", 15f, 40f, 15f, 18f, 2f);
            Debug.Log($"Veilmouth C4 views written to {OutputFolder}.");
        }

        /// <summary>
        /// The city of plan C5 (VEILMOUTH_CITY_PLAN.md §29) from above and from a walker's height: the blocks ringed by
        /// pavements, carriageways unbroken through every junction, lanes, the Penny with Bawling Row, the quay's edge and
        /// a pier's foot, the yards. Plan to world: X = px − 500, Z = 500 − py.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderC5
        /// </summary>
        public static void RenderC5()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            ShowCircles();
            Shader.SetGlobalVector("_Growth_WindSettings", VeilmouthDressing.WindSettings);
            Shader.SetGlobalTexture("_Growth_WindNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(VeilmouthDressing.WindNoisePath));
            Top("c5_world", Vector3.zero, 500f);
            Top("c5_city", new Vector3(50f, 0f, 18f), 225f);
            Top("c5_penny", new Vector3(40f, 0f, 60f), 34f);
            Top("c5_parsons", new Vector3(45f, 0f, 185f), 70f);
            Top("c5_sleepy", new Vector3(-20f, 0f, 40f), 60f);
            Top("c5_lowerveil", new Vector3(107f, 0f, 0f), 60f);
            Top("c5_reek", new Vector3(50f, 0f, -145f), 75f);
            Top("c5_port", new Vector3(170f, 0f, 60f), 30f);
            Top("c5_junction", new Vector3(-22f, 0f, 4f), 14f); // Thimble Street over Button Lane
            Eye("c5_eye_longtongue", -75f, 60f, 35f, 60f, 3f);
            Eye("c5_eye_saltgullet", 150f, 60f, 40f, 60f, 3f);
            Eye("c5_eye_lenten", 40f, 125f, 40f, 60f, 4f);
            Eye("c5_eye_smoked", 40f, -40f, 40f, 60f, 4f);
            Eye("c5_eye_embankment", -70f, -40.5f, 150f, -40.5f, 2f);
            Eye("c5_eye_gate", -140f, 60f, -60f, 60f, 4f);
            Eye("c5_eye_grubby", 40f, -100f, 40f, -30f, 3f);
            Eye("c5_eye_quay", 170f, -40f, 170f, 120f, 2f);
            Eye("c5_eye_quayedge", 177f, 40f, 180f, 58f, 1.7f);
            Eye("c5_eye_widows", -75f, 123.5f, 150f, 123.5f, 3f);
            Eye("c5_eye_button", -75f, 4f, 35f, 4f, 1.7f);
            Eye("c5_eye_spade", -75f, 179f, -15f, 179f, 1.7f);
            Eye("c5_eye_thimble", -22f, 120f, -22f, -40f, 1.7f);
            Eye("c5_eye_nettle", 102f, 230f, 102f, 5f, 1.7f);
            Eye("c5_eye_penny", 2f, 58f, 40f, 60f, 2f);
            Eye("c5_eye_market", 40f, 75f, 40f, 60f, 1.7f);
            Eye("c5_eye_cathedral", 40f, 118f, 40f, 186f, 2f);
            Eye("c5_eye_puddle", 100f, -30f, 140f, -5f, 1.7f);
            Eye("c5_eye_works", 40f, -108f, 40f, -190f, 2f);
            Eye("c5_eye_deafboiler", 141f, -146f, 141f, -173f, 1.7f);
            // Step 5 (§29.3): a block's corner where two rows meet flush, a passage into a yard, a poor yard and a rich one.
            Eye("c5_eye_corner", -26f, -9f, -15f, 0f, 1.7f);
            Eye("c5_eye_passage", -21f, 88.5f, -4f, 88.5f, 1.7f);
            Eye("c5_eye_yard_poor", 120f, 22f, 136f, 34f, 1.7f);
            Eye("c5_eye_yard_rich", -56f, 84f, -40f, 102f, 1.7f);
            Debug.Log($"Veilmouth C5 views written to {OutputFolder}.");
        }

        /// <summary>
        /// City plan §35–§36: the cathedral's and the works' placeholder dress seen from the streets at noon.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderDress -lookLabel dress1
        /// </summary>
        public static void RenderDress()
        {
            var label = Argument("-lookLabel") ?? "dress";
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            Shader.SetGlobalVector("_Growth_WindSettings", VeilmouthDressing.WindSettings);
            Shader.SetGlobalTexture("_Growth_WindNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(VeilmouthDressing.WindNoisePath));
            // -lookMinute 1410: the night lighthouse with the sky and the grade the player sees (city plan §38).
            if (int.TryParse(Argument("-lookMinute"), out var minute) && Object.FindFirstObjectByType<GameDev.Slice.Atmosphere.VeilmouthSky>() is { } sky)
            {
                _skyAndGrade = true;
                try
                {
                    sky.Apply(minute);
                    Eye(label + "_lighthouse", 150f, -60f, 245f, -25f, 3f);
                    Eye(label + "_pier", 120f, -10f, 245f, -25f, 2f);
                    Eye(label + "_lenten", 40f, 30f, 40f, -60f, 4f);
                    Eye(label + "_grandad", 8f, 60f, 40f, 60f, 2f); // §37: Stone Grandad's lantern over the Penny
                    Eye(label + "_grandadnear", 22f, 56f, 40f, 60f, 1.7f);
                    Eye(label + "_clock", 30f, 40f, -4f, 92f, 4f); // §38: the clock's dials glow with the lamps
                    Eye(label + "_cemetery", -74f, 162f, -28f, 161f, 2f); // §40
                    Eye(label + "_gatein", -58f, 62f, -81f, 60f, 1.7f); // §41–§42
                }
                finally
                {
                    _skyAndGrade = false;
                }

                Debug.Log($"Veilmouth night dress views written to {OutputFolder}.");
                return;
            }

            Eye(label + "_works", 40f, -108f, 40f, -190f, 2f);
            Eye(label + "_deafboiler", 141f, -146f, 141f, -173f, 1.7f);
            Eye(label + "_grubby", 40f, -30f, 40f, -100f, 3f);
            Eye(label + "_cathedral", 40f, 118f, 40f, 186f, 2f);
            Eye(label + "_close", 85f, 200f, 50f, 200f, 1.7f);
            Eye(label + "_lenten", 40f, 30f, 40f, -60f, 4f);
            Eye(label + "_mill", 40f, -131f, 60f, -131f, 2f);
            Eye(label + "_grandad", 8f, 60f, 40f, 60f, 2f);
            Eye(label + "_clock", 30f, 40f, -4f, 92f, 4f);
            Eye(label + "_lighthouse", 150f, -60f, 245f, -25f, 3f);
            Eye(label + "_grandadnear", 22f, 56f, 40f, 60f, 1.7f);
            Eye(label + "_millstreet", 36f, -100f, 47f, -131f, 2f);
            Eye(label + "_cemetery", -74f, 162f, -28f, 161f, 2f); // Last Turning Cemetery from its gate
            Eye(label + "_graves", -50f, 173f, -46f, 149f, 1.7f);
            Eye(label + "_gate", -140f, 60f, -60f, 60f, 4f); // §41: Creaking Gate from beyond the river
            Eye(label + "_gatein", -58f, 62f, -81f, 60f, 1.7f);
            Eye(label + "_wall", -125f, 120f, -82f, 150f, 3f); // §42: the town wall north of the gate, from across the river
            Debug.Log($"Veilmouth dress views written to {OutputFolder}.");
        }

        /// <summary>
        /// C4's ground (VEILMOUTH_CITY_PLAN.md §28.5) from above and from a walker's height, with every built root hidden
        /// for the picture only: the river round the west and south, the sea, the rise under the Penny, the forest's land.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderC4Ground
        /// </summary>
        public static void RenderC4Ground()
        {
            Directory.CreateDirectory(OutputFolder);
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects().Where(r => r.GetComponentInChildren<Terrain>() == null && r.name != "Water" &&
                                                                      r.GetComponentInChildren<Renderer>() != null))
            {
                root.SetActive(false);
            }

            Top("c4_ground_world", Vector3.zero, 500f);
            Top("c4_ground_city", new Vector3(50f, 0f, -20f), 190f);
            Eye("c4_ground_gate", -150f, 60f, -60f, 60f, 2f);
            Eye("c4_ground_river_south", 40f, -110f, 40f, -40f, 2f);
            Eye("c4_ground_coast", 150f, 300f, 220f, 200f, 0f);
            Eye("c4_ground_rise", -60f, 0f, 40f, 60f, 2f);
            Debug.Log($"Veilmouth C4 ground views written to {OutputFolder}.");
        }

        /// <summary>
        /// Each interior from above with its ceilings hidden, and each room from a walker's eye in its south-west
        /// corner toward the far corner, at the scene's preview hour. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderInteriors
        /// </summary>
        [MenuItem("GameDev/Slice/Render Veilmouth Interiors")]
        public static void RenderInteriors() => RenderInteriorsAt(null, "");

        /// <summary>
        /// The interiors at noon and at 23:00 (VI-2's acceptance, VICTORIAN_INTERIORS_PLAN §8): daylight through the
        /// windows, then lamps, candles and the evening fire. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderInteriorsNoonAndNight
        /// </summary>
        public static void RenderInteriorsNoonAndNight()
        {
            RenderInteriorsAt(12 * 60, "_noon");
            RenderInteriorsAt(23 * 60, "_night");
        }

        private static void RenderInteriorsAt(int? minute, string suffix)
        {
            Directory.CreateDirectory(OutputFolder);
            foreach (var (_, path) in VeilmouthInteriors.All)
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                ShowCircles();
                var lighting = Object.FindFirstObjectByType<Interiors.InteriorLighting>();
                if (lighting != null)
                {
                    lighting.Apply(minute ?? lighting.PreviewMinute);
                }

                var name = Path.GetFileNameWithoutExtension(path) + suffix;
                var ceilings = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                    .Where(t => t.name.StartsWith("Kit_BkRoom_Ceiling") || t.name.StartsWith("Kit_ViRoom_Ceiling"))
                    .Select(t => t.gameObject).ToArray();
                foreach (var c in ceilings)
                {
                    c.SetActive(false);
                }

                var room = Bounds();
                var markers = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                    .Where(t => t.name.StartsWith(VeilmouthInteriors.RoomMarker)).ToArray();
                var storeys = markers.Length == 0 ? 1 : Mathf.RoundToInt(markers.Max(m => m.position.y) / VeilmouthInteriors.Storey) + 1;
                for (var storey = 0; storey < storeys; storey++)
                {
                    // Each storey from above, with everything from the next floor up hidden (VI-3).
                    var above = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                        .Where(r => r.enabled && r.bounds.min.y >= (storey + 1) * VeilmouthInteriors.Storey - 0.25f).ToArray();
                    foreach (var r in above)
                    {
                        r.enabled = false;
                    }

                    Top("room_" + name + (storey > 0 ? $"_floor{storey + 1}" : string.Empty), room.center, Mathf.Max(room.extents.x, room.extents.z) + 0.5f);
                    foreach (var r in above)
                    {
                        r.enabled = true;
                    }
                }

                foreach (var c in ceilings)
                {
                    c.SetActive(true);
                }

                foreach (var marker in markers)
                {
                    var p = marker.position;
                    var half = marker.localScale * 0.5f;
                    var label = marker.name.Substring(VeilmouthInteriors.RoomMarker.Length).Replace(' ', '_').ToLowerInvariant();
                    Eye($"roomeye_{name}_{label}", new Vector3(p.x - half.x + 0.5f, p.y + 1.7f, p.z - half.z + 0.5f),
                        new Vector3(p.x + half.x - 0.5f, p.y + 1.0f, p.z + half.z - 0.5f));
                }
            }
        }

        /// <summary>
        /// Sample dwellings, one of each kind at each wealth it comes in, from above with their ceilings hidden, and
        /// their kit faults in the log. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderDwellings
        /// </summary>
        [MenuItem("GameDev/Slice/Dwellings/Render Sample Dwellings")]
        public static void RenderDwellings()
        {
            Directory.CreateDirectory(OutputFolder);
            var kit = VeilmouthDwellings.Kit;
            var settings = VeilmouthDwellings.Settings;
            var samples = new[]
            {
                (Dwellings.DwellingArchetype.Tenement, Dwellings.DwellingWealth.Poor), (Dwellings.DwellingArchetype.Terrace, Dwellings.DwellingWealth.Middling),
                (Dwellings.DwellingArchetype.Terrace, Dwellings.DwellingWealth.Rich), (Dwellings.DwellingArchetype.Shop, Dwellings.DwellingWealth.Poor),
                (Dwellings.DwellingArchetype.Shop, Dwellings.DwellingWealth.Middling), (Dwellings.DwellingArchetype.Villa, Dwellings.DwellingWealth.Rich),
                (Dwellings.DwellingArchetype.Warehouse, Dwellings.DwellingWealth.Poor),
            };
            var faults = 0;
            foreach (var (archetype, wealth) in samples)
            {
                for (var n = 0; n < 2; n++)
                {
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                    var key = VeilmouthDwellings.PreviewKey(archetype, wealth, 1000 + n * 37, -2000 - n * 53);
                    var found = VeilmouthDwellings.BuildAndCheck(key, kit, settings, out var root);
                    faults += found.Count;
                    var plan = Dwellings.DwellingRuntime.PlanFor(key, settings, kit);
                    if (plan.Unplaced.Count > 0)
                    {
                        Debug.Log($"[Dwelling] {archetype} {wealth} #{n} found no room for: {string.Join(", ", plan.Unplaced)}");
                    }
                    foreach (var f in found)
                    {
                        Debug.LogWarning($"[Dwelling] {archetype} {wealth} #{n}: {f}");
                    }

                    // Storey by storey, with everything above it hidden, as the seven are rendered (VI-3, VI-4).
                    var b = Bounds();
                    var renderers = root.GetComponentsInChildren<Renderer>(true);
                    for (var storey = 0; storey < plan.Floors; storey++)
                    {
                        var top = ((storey + 1) * Dwellings.DwellingPlan.Storey) - 0.25f;
                        foreach (var r in renderers)
                        {
                            r.enabled = r.bounds.min.y < top;
                        }

                        var suffix = storey == 0 ? string.Empty : $"_floor{storey + 1}";
                        Top($"dwelling_{archetype}_{wealth}_{n}{suffix}".ToLowerInvariant(), b.center, Mathf.Max(b.extents.x, b.extents.z) + 0.5f);
                    }
                }
            }

            Debug.Log($"[Dwelling] {samples.Length * 2} sample dwellings rendered, {faults} kit faults.");
        }

        /// <summary>
        /// Close views of a rich villa at a walker's eye: each stair from its head and its foot, the top step from the
        /// side, the well from beside it, and the street door from inside (owner's play test VI-5).
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderDwellingDetails
        /// </summary>
        public static void RenderDwellingDetails()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            foreach (var sun in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(sun.gameObject);
            }

            var key = VeilmouthDwellings.PreviewKey(Dwellings.DwellingArchetype.Villa, Dwellings.DwellingWealth.Rich, 1000, -2000);
            VeilmouthDwellings.BuildAndCheck(key, VeilmouthDwellings.Kit, VeilmouthDwellings.Settings, out var root);
            var n = 0;
            var plan = Dwellings.DwellingRuntime.PlanFor(key, VeilmouthDwellings.Settings, VeilmouthDwellings.Kit);
            foreach (var w in plan.Wells)
            {
                // From the plan: the well's far end is the stair's head, on the module line; its foot a metre short of
                // the well's near end, a storey down. The dwelling stands at the origin.
                var climb = w.ClimbsEast ? Vector3.right : Vector3.left;
                var side = Vector3.back;
                var floor = w.Level * Dwellings.DwellingPlan.Storey;
                var z = w.Z + (Dwellings.DwellingWell.Depth * 0.5f);
                var top = new Vector3(w.ClimbsEast ? w.X + Dwellings.DwellingWell.Width : w.X, floor, z);
                var foot = new Vector3(w.ClimbsEast ? w.X - 1 : w.X + Dwellings.DwellingWell.Width + 1, floor - Dwellings.DwellingPlan.Storey, z);
                var centre = new Vector3(w.X + (Dwellings.DwellingWell.Width * 0.5f), floor, z);
                Eye($"detail_stair{n}_head", top + (climb * 2.5f) + (Vector3.up * 1.7f), centre - (climb * 1.5f) - (Vector3.up * 1.5f));
                Eye($"detail_stair{n}_foot", foot - (climb * 1.5f) + (Vector3.up * 1.7f), top + (Vector3.up * 0.3f));
                Eye($"detail_stair{n}_topside", top + (climb * 1.2f) + (Vector3.up * 0.8f) + (Vector3.forward * 0.4f), top - (climb * 1f) - (Vector3.up * 0.3f));
                Eye($"detail_stair{n}_well", centre + (side * 3.5f) + (Vector3.up * 1.7f), centre - (Vector3.up * 1f));
                n++;
            }

            var door = root.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.StartsWith("Kit_ViFurniture_Door") || t.name.StartsWith("Kit_ViRoom_Door"));
            if (door != null)
            {
                var at = door.position + (Vector3.up * 1.2f);
                Eye("detail_street_door", at + (Vector3.forward * 3f) + (Vector3.up * 0.4f), at);
            }

            Debug.Log($"[Dwelling] details of {n} stairs rendered.");
        }

        /// <summary>
        /// The Soot & Bell's places from the owner's VI-8 remarks, close up: the walls between the taproom's storeys and
        /// under a ceiling, the stage, the hearth by day and by night, the stair well's balustrades.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderSootAndBellDetails
        /// </summary>
        [MenuItem("GameDev/Slice/Render Soot and Bell Details")]
        public static void RenderSootAndBellDetails()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.SootAndBellScenePath, OpenSceneMode.Single);
            var lighting = Object.FindFirstObjectByType<Interiors.InteriorLighting>();
            const float up = VeilmouthInteriors.Storey;
            foreach (var (minute, suffix) in new[] { (12 * 60, "noon"), (21 * 60, "night") })
            {
                lighting.Apply(minute);
                // A still renders particles only once simulated: two seconds of the lit fires' flames.
                foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude))
                {
                    ps.Simulate(2f, true, true);
                }

                Eye($"detail_sb_{suffix}_hearth", new Vector3(4.5f, 1.5f, 7.5f), new Vector3(0.6f, 0.7f, 7.5f));
                if (suffix == "night")
                {
                    continue;
                }

                Eye("detail_sb_frieze_back", new Vector3(10.5f, 1.7f, 3f), new Vector3(10.5f, 3.6f, 12f));
                Eye("detail_sb_frieze_front", new Vector3(10.5f, 1.7f, 9f), new Vector3(4f, 3.6f, 0f));
                Eye("detail_sb_stage", new Vector3(12f, 1.7f, 5f), new Vector3(19.5f, 0.3f, 6f));
                Eye("detail_sb_hall_ceiling", new Vector3(14f, 1.7f, 20f), new Vector3(21f, 3.2f, 22f));
                Eye("detail_sb_well_from_west", new Vector3(10f, up + 1.7f, 16.5f), new Vector3(16f, up - 0.5f, 16.5f));
                Eye("detail_sb_well_sw_corner", new Vector3(14f, up + 1.6f, 12.8f), new Vector3(11.8f, up + 0.4f, 15.2f));
                Eye("detail_sb_well_nw_corner", new Vector3(10.5f, up + 1.6f, 20f), new Vector3(12f, up + 0.4f, 17.5f));
                Eye("detail_sb_stair_from_foot", new Vector3(9.6f, 1.7f, 16.5f), new Vector3(18f, up + 1f, 16.5f));
                // Every joint of the balustrades close up is StairLab.RenderSootAndBell's, found from the posts and rails.
            }

            Debug.Log("[VeilmouthRenders] the Soot & Bell's details rendered.");
        }

        /// <summary>
        /// The owner's garden ornaments in Veilmouth (VeilmouthOrnaments), each from a few metres.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderOrnaments
        /// </summary>
        [MenuItem("GameDev/Slice/Render Veilmouth Ornaments")]
        public static void RenderOrnaments()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var i = 0;
            foreach (var t in GameObject.Find("Ornaments").transform.Cast<Transform>())
            {
                var at = t.position + (t.rotation * new Vector3(0.5f, 0f, 0.5f));
                var tall = t.name.StartsWith("Angel") ? 1.4f : 0.6f;
                Eye($"ornament_{i++}", at + new Vector3(2.2f, 1.7f, 3.4f) * (tall > 1f ? 1.6f : 1f), at + (Vector3.up * tall));
            }

            Debug.Log($"[VeilmouthRenders] {i} ornaments rendered.");
        }

        private static void ShowCircles()
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(t => t.name.StartsWith("Ritual Circle")))
            {
                t.gameObject.SetActive(true);
            }
        }

        private static Bounds Bounds()
        {
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            var b = renderers[0].bounds;
            foreach (var r in renderers.Skip(1))
            {
                b.Encapsulate(r.bounds);
            }

            return b;
        }

        private static void Top(string name, Vector3 centre, float half)
        {
            var camera = Make();
            camera.orthographic = true;
            camera.orthographicSize = half;
            camera.transform.SetPositionAndRotation(new Vector3(centre.x, 300f, centre.z), Quaternion.LookRotation(Vector3.down, Vector3.forward));
            camera.farClipPlane = 400f;
            Shoot(camera, name, 1400, 1400);
        }

        /// <summary>An eye 1.7 m above the ground at (x, z), looking at (toX, toZ) at <paramref name="up"/> above its ground.</summary>
        private static void Eye(string name, float x, float z, float toX, float toZ, float up)
        {
            var terrain = Terrain.activeTerrain;
            float Ground(float gx, float gz) => terrain.SampleHeight(new Vector3(gx, 0f, gz)) + terrain.transform.position.y;
            Eye(name, new Vector3(x, Ground(x, z) + 1.7f, z), new Vector3(toX, Ground(toX, toZ) + up, toZ));
        }

        internal static void Eye(string name, Vector3 from, Vector3 to, float fieldOfView = 65f)
        {
            Directory.CreateDirectory(OutputFolder);
            var camera = Make();
            camera.fieldOfView = fieldOfView;
            camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(to - from));
            camera.farClipPlane = 1500f;
            Shoot(camera, name, 1600, 900);
        }

        /// <summary>
        /// A walking townsperson close by, from the front and the side, and the townsfolk's routes from above.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderTownsfolk
        /// </summary>
        [MenuItem("GameDev/Slice/Render Veilmouth Townsfolk")]
        public static void RenderTownsfolk()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var walkers = Object.FindObjectsByType<GameDev.Slice.People.TownsfolkWalker>(FindObjectsSortMode.None).OrderBy(w => w.transform.position.x).ToList();
            var line = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(1f, 0.15f, 0.6f) };
            foreach (var w in walkers)
            {
                var animator = w.GetComponentInChildren<Animator>();
                animator.runtimeAnimatorController.animationClips.First(c => c.name == "Walk").SampleAnimation(animator.gameObject, 0.6f);
                var lr = new GameObject("Route").AddComponent<LineRenderer>();
                lr.sharedMaterial = line;
                lr.loop = true;
                lr.widthMultiplier = 0.5f;
                lr.positionCount = w.Route.Length;
                lr.SetPositions(w.Route.Select(p => p + (Vector3.up * 0.3f)).ToArray());
            }

            foreach (var (prefix, prefab) in new[] { ("townsperson", SliceTownsfolk.PrefabPath), ("townswoman", SliceTownsfolk.WomanPrefabPath) })
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);
                var kind = walkers.Where(w => PrefabUtility.GetCorrespondingObjectFromSource(w.gameObject) == source).ToList();
                var one = kind[kind.Count / 2].transform;
                var at = one.position + (Vector3.up * 1.1f);
                Shot($"{prefix}_front", at + (one.forward * 3.2f) + (Vector3.up * 0.2f), at);
                Shot($"{prefix}_side", at + (one.right * 3.2f) + (Vector3.up * 0.2f), at);
            }

            Top("townsfolk_routes_city", new Vector3(130f, 0f, -250f), 150f);
            Top("townsfolk_routes_market", new Vector3(50f, 0f, -270f), 40f);
            Debug.Log($"Townsfolk views written to {OutputFolder}.");
        }

        /// <summary>
        /// S3b.2: a wolf pack, a boar and a glowing wolf in their territories, at 10:00 and at 20:30, from a walker's eye
        /// seven metres off, the eye placed where nothing stands between it and the beasts. Nothing is saved. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderBeasts
        /// </summary>
        public static void RenderBeasts()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var sky = Object.FindFirstObjectByType<GameDev.Slice.Atmosphere.VeilmouthSky>();
            var beasts = Object.FindObjectsByType<GameDev.Slice.Beasts.SliceBeast>(FindObjectsSortMode.None).OrderBy(b => b.Id).ToList();
            if (sky == null || beasts.Count == 0)
            {
                Debug.LogError("[VeilmouthRenders] Veilmouth needs its sky and its beasts (SliceBeastsSetup).");
                return;
            }

            Shader.SetGlobalVector("_Growth_WindSettings", VeilmouthDressing.WindSettings);
            Shader.SetGlobalTexture("_Growth_WindNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(VeilmouthDressing.WindNoisePath));
            foreach (var b in beasts)
            {
                var animator = b.GetComponentInChildren<Animator>();
                var idle = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name.EndsWith("_Idle"));
                if (idle != null)
                {
                    idle.SampleAnimation(animator.gameObject, 0.4f);
                }
            }

            // The second pack's first wolf, glowing alone as the seed may make it, its pack gone for the picture.
            var lone = beasts.First(b => b.Pack == 1);
            foreach (var b in beasts.Where(b => b.Pack == 1 && b != lone))
            {
                b.gameObject.SetActive(false);
            }

            var rules = AssetDatabase.LoadAssetAtPath<GameDev.Slice.Beasts.BeastRules>("Assets/Data/Slice/BeastRules.asset");
            lone.Glow(rules.Glow, rules.GlowLight, rules.GlowRange, rules.GlowEmission);

            // The boar's HD body with its fur shells and the pack's mobile body, side by side, for the choice of body.
            var boar = beasts.First(b => b.Kind == GameDev.Slice.Beasts.BeastKind.Boar);
            var mobile = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Wild Boar/Prefabs/URP/Mobile/Wild_Boar.prefab"));
            mobile.transform.SetPositionAndRotation(boar.transform.position + (boar.transform.right * 2f), boar.transform.rotation);
            mobile.transform.localScale = boar.GetComponentInChildren<Animator>().transform.lossyScale;
            var subjects = new (string Name, Vector3 At)[]
            {
                ("beasts_pack", Centre(beasts.Where(b => b.Pack == 0))),
                ("beasts_boar", beasts.First(b => b.Kind == GameDev.Slice.Beasts.BeastKind.Boar).transform.position),
                ("beasts_glowing", lone.transform.position),
                ("beasts_boars", boar.transform.position + boar.transform.right),
            };
            var hours = new[] { (10 * 60, "1000"), ((20 * 60) + 30, "2030") };
            _skyAndGrade = true;
            try
            {
                sky.Apply(hours[0].Item1);
                Eye("beasts_warmup", subjects[0].At + new Vector3(6f, 1.7f, 0f), subjects[0].At);
                File.Delete($"{OutputFolder}/beasts_warmup.png");
                foreach (var (minute, hour) in hours)
                {
                    sky.Apply(minute);
                    foreach (var (name, at) in subjects)
                    {
                        var to = at + (Vector3.up * 0.45f);
                        Eye($"{name}_{hour}", Clear(to, name == "beasts_boars" ? 4f : 7f), to, 50f);
                    }
                }
            }
            finally
            {
                _skyAndGrade = false;
            }

            Debug.Log($"[VeilmouthRenders] the beasts' views written to {OutputFolder} as beasts_*.");
        }

        private static Vector3 Centre(System.Collections.Generic.IEnumerable<GameDev.Slice.Beasts.SliceBeast> beasts)
        {
            var list = beasts.ToList();
            return list.Aggregate(Vector3.zero, (sum, b) => sum + b.transform.position) / Mathf.Max(1, list.Count);
        }

        /// <summary>An eye at a walker's height this far from a place, on the first side with nothing in between.</summary>
        private static Vector3 Clear(Vector3 to, float distance)
        {
            var terrain = Terrain.activeTerrain;
            Vector3 eye = default;
            for (var k = 0; k < 16; k++)
            {
                var flat = to + (Quaternion.Euler(0f, 200f + (k * 22.5f), 0f) * Vector3.forward * distance);
                eye = new Vector3(flat.x, terrain.SampleHeight(flat) + terrain.transform.position.y + 1.7f, flat.z);
                var hits = Physics.RaycastAll(eye, (to - eye).normalized, (to - eye).magnitude, ~0, QueryTriggerInteraction.Ignore);
                if (!hits.Any(h => h.collider.GetComponentInParent<GameDev.Slice.Beasts.SliceBeast>() == null))
                {
                    return eye;
                }
            }

            return eye;
        }

        private static void Shot(string name, Vector3 from, Vector3 to)
        {
            var camera = Make();
            camera.fieldOfView = 45f;
            camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(to - from));
            Shoot(camera, name, 900, 900);
        }

        /// <summary>While set, the cameras draw the scene's sky and its post-processing volumes, as the player's camera does.</summary>
        private static bool _skyAndGrade;

        private static Camera Make()
        {
            var camera = new GameObject("RenderCamera").AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.clearFlags = _skyAndGrade ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.62f, 0.74f, 0.86f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = _skyAndGrade;
            return camera;
        }

        /// <summary>
        /// The mystic layer (WORLD_STYLE_GUIDE §5–§6) at noon, at dusk and at night: streets, the crimson moon over the
        /// sea, a ward and runes up close, the strange windows and a fogged court. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderMystic
        /// </summary>
        public static void RenderMystic()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var sky = Object.FindFirstObjectByType<GameDev.Slice.Atmosphere.VeilmouthSky>();
            if (sky == null)
            {
                Debug.LogError("[VeilmouthRenders] the scene has no VeilmouthSky: place the mystic layer first.");
                return;
            }

            Shader.SetGlobalVector("_Growth_WindSettings", VeilmouthDressing.WindSettings);
            Shader.SetGlobalTexture("_Growth_WindNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(VeilmouthDressing.WindNoisePath));
            var root = GameObject.Find(VeilmouthMystic.RootName).transform;
            Transform First(string prefix) => root.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.StartsWith(prefix));
            var ward = First("Ward - door");
            var gate = First("Runes - west gate");
            _skyAndGrade = true;
            try
            {
                foreach (var (minute, label) in new[] { (12 * 60, "noon"), (19 * 60, "dusk"), ((23 * 60) + 30, "night") })
                {
                    sky.Apply(minute);
                    var banks = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
                    foreach (var fog in banks)
                    {
                        fog.Simulate(30f, true, true);
                    }

                    Debug.Log($"[VeilmouthRenders] {label}: {banks.Length} particle systems, {banks.Sum(b => b.particleCount)} particles.");

                    Eye($"mystic_{label}_highstreet", -10f, -235f, 100f, -235f, 3f);
                    Eye($"mystic_{label}_bellstreet", 195f, -278f, 100f, -278f, 2.5f);
                    Eye($"mystic_{label}_sootstair", 204f, -178f, 204f, -215f, 0f);
                    Eye($"mystic_{label}_court", 151.5f, -246f, 151.5f, -258f, 2f);
                    Eye($"mystic_{label}_harrow", 175f, -280f, 175f, -271f, 5f);
                    Eye($"mystic_{label}_moon", new Vector3(60f, VeilmouthScene.StreetLevel + 1.8f, -318f), new Vector3(129.5f, 70f, -390f));
                    if (ward != null)
                    {
                        Eye($"mystic_{label}_ward", ward.position - (ward.forward * 3.5f) + (Vector3.down * 1.6f), ward.position, 50f);
                    }

                    if (gate != null)
                    {
                        Eye($"mystic_{label}_runes", gate.position - (gate.forward * 3f) + (Vector3.down * 0.2f), gate.position, 55f);
                    }
                }
            }
            finally
            {
                _skyAndGrade = false;
            }

            Debug.Log($"Veilmouth mystic views written to {OutputFolder}.");
        }

        /// <summary>
        /// The owner's street lamp (city plan §30) at noon and at night, with the sky and the grade the player sees: three
        /// of C5's lamps from a passer-by's eye 7 m off, on a side with nothing in the way, and the middle one close by.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderLamps
        /// </summary>
        public static void RenderLamps()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var sky = Object.FindFirstObjectByType<GameDev.Slice.Atmosphere.VeilmouthSky>();
            if (sky == null)
            {
                Debug.LogError("[VeilmouthRenders] the scene has no VeilmouthSky: place the mystic layer first.");
                return;
            }

            Shader.SetGlobalVector("_Growth_WindSettings", VeilmouthDressing.WindSettings);
            Shader.SetGlobalTexture("_Growth_WindNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(VeilmouthDressing.WindNoisePath));
            Physics.SyncTransforms();
            var lamps = Object.FindObjectsByType<GameDev.Slice.Kit.KitBlock>(FindObjectsSortMode.None).Where(b => b.name.StartsWith("Kit_Street_Lamp"))
                .OrderBy(b => b.transform.position.x).ThenBy(b => b.transform.position.z).ToList();
            if (lamps.Count == 0)
            {
                Debug.LogError("[VeilmouthRenders] no street lamps in Veilmouth.");
                return;
            }

            var picks = new[] { lamps[lamps.Count / 4], lamps[lamps.Count / 2], lamps[(3 * lamps.Count) / 4] };
            _skyAndGrade = true;
            try
            {
                foreach (var (minute, label) in new[] { (12 * 60, "noon"), ((23 * 60) + 30, "night") })
                {
                    sky.Apply(minute);
                    for (var i = 0; i < picks.Length; i++)
                    {
                        var b = picks[i].GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((p, q) => { p.Encapsulate(q); return p; });
                        var head = new Vector3(b.center.x, b.max.y - 0.45f, b.center.z);
                        var side = new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left }
                            .OrderBy(d => Physics.Raycast(head + (Vector3.down * 2.2f), d, 7.5f, ~0, QueryTriggerInteraction.Ignore) ? 1 : 0).First();
                        Eye($"lamp_{label}_{i}", new Vector3(head.x, b.min.y + 1.7f, head.z) + (side * 7f), head + (Vector3.down * 1.3f), 60f);
                        if (i == 1)
                        {
                            Eye($"lamp_{label}_close", head + (side * 1.4f) + (Vector3.down * 0.4f), head, 40f);
                        }
                    }
                }
            }
            finally
            {
                _skyAndGrade = false;
            }

            Debug.Log($"[VeilmouthRenders] {lamps.Count} street lamps; views of three written to {OutputFolder}.");
        }

        /// <summary>
        /// The reference look's check (reference plan R3): six fixed views of C5, Long Tongue Street, the Penny, the quay,
        /// the city from a pier, the river and Long Tongue westward, at 08:00, 16:00, 19:30 and 23:00, with the sky and the
        /// grade the player sees; each view's four hours also go in one sheet at half size. The command-line argument
        /// <c>-lookLabel name</c> prefixes the files, so a before and an after stand side by side. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderLook
        /// </summary>
        public static void RenderLook()
        {
            var label = Argument("-lookLabel") ?? "look";
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var sky = Object.FindFirstObjectByType<GameDev.Slice.Atmosphere.VeilmouthSky>();
            if (sky == null)
            {
                Debug.LogError("[VeilmouthRenders] the scene has no VeilmouthSky: place the mystic layer first.");
                return;
            }

            Shader.SetGlobalVector("_Growth_WindSettings", VeilmouthDressing.WindSettings);
            Shader.SetGlobalTexture("_Growth_WindNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(VeilmouthDressing.WindNoisePath));
            var street = VeilmouthScene.StreetLevel;
            // C5's streets (city plan §29): Long Tongue Street from the west gate to the Penny, the Penny's market round
            // the statue, the quay with the sea on its right, the city from a pier's end across the water, the river
            // north from the west gate's bridge (the eye at a fixed height over the parapet, since the ground under a
            // bridge is the riverbed), and Long Tongue westward into the evening sun.
            var views = new (string Name, Vector3 From, Vector3 To)[]
            {
                ("longtongue", Ground(-75f, 60f, 1.7f), Ground(35f, 60f, 3f)),
                ("penny", Ground(2f, 58f, 1.7f), Ground(40f, 60f, 2f)),
                ("quay", Ground(170f, -40f, 1.7f), Ground(170f, 120f, 2f)),
                ("pier", new Vector3(238f, street + 1.7f, 61f), new Vector3(120f, street + 6f, 61f)),
                ("river", new Vector3(-101f, street + 5f, 64f), new Vector3(-101f, street - 2f, 250f)),
                // Long Tongue back toward the west gate, into the low evening sun through the street trees (R3 step 6).
                ("westward", Ground(22f, 61f, 1.7f), Ground(-75f, 61f, 4f)),
            };
            var hours = new[] { (8 * 60, "0800"), (16 * 60, "1600"), ((19 * 60) + 30, "1930"), (23 * 60, "2300") };
            _skyAndGrade = true;
            try
            {
                // The first picture after opening the scene is drawn before every texture has loaded: one to waste.
                sky.Apply(hours[0].Item1);
                Eye("look_warmup", views[0].From, views[0].To);
                File.Delete($"{OutputFolder}/look_warmup.png");
                foreach (var (minute, hour) in hours)
                {
                    sky.Apply(minute);
                    foreach (var fog in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                    {
                        fog.Simulate(30f, true, true);
                    }

                    foreach (var view in views)
                    {
                        Eye($"{label}_{view.Name}_{hour}", view.From, view.To);
                    }
                }

                foreach (var view in views)
                {
                    Grid($"{label}_{view.Name}", hours.Select(h => $"{OutputFolder}/{label}_{view.Name}_{h.Item2}.png").ToArray(), 2);
                }
            }
            finally
            {
                _skyAndGrade = false;
            }

            Debug.Log($"[VeilmouthRenders] the reference look's views written to {OutputFolder} as {label}_*.");
        }

        /// <summary>
        /// The raised pavements and passages (reference plan R4, R5) at noon: junctions from above, where carriageways cut
        /// the pavements and the kerbs turn round, and two views from the carriageway at the kerb. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderPavements -lookLabel r4
        /// </summary>
        public static void RenderPavements()
        {
            var label = Argument("-lookLabel") ?? "r4";
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var sky = Object.FindFirstObjectByType<GameDev.Slice.Atmosphere.VeilmouthSky>();
            Shader.SetGlobalVector("_Growth_WindSettings", VeilmouthDressing.WindSettings);
            Shader.SetGlobalTexture("_Growth_WindNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(VeilmouthDressing.WindNoisePath));
            _skyAndGrade = sky != null;
            try
            {
                sky?.Apply(12 * 60);
                Eye($"{label}_warmup", Ground(5f, -235f, 1.7f), Ground(19f, -242f, 0.3f));
                File.Delete($"{OutputFolder}/{label}_warmup.png");
                // Plan (px, py) is world (px − 500, 500 − py).
                Top($"{label}_top_cheap", new Vector3(22f, 0f, -240f), 14f);   // the High Street meets Cheap Street
                Top($"{label}_top_crown", new Vector3(112f, 0f, -232f), 18f);  // Crown Avenue's mall at the High Street
                Top($"{label}_top_bell", new Vector3(205f, 0f, -278f), 16f);   // Bell Street meets Dock Road
                Top($"{label}_top_mercers", new Vector3(22f, 0f, -278f), 14f); // Mercers Row crosses Cheap Street
                Top($"{label}_top_parade", new Vector3(245f, 0f, -172f), 14f); // North Road meets the Parade
                Top($"{label}_top_slant", new Vector3(-10f, 0f, -246f), 16f);  // Tanners Slant leaves the High Street
                Top($"{label}_top_lowerveil", new Vector3(160f, 0f, -270f), 45f);
                Eye($"{label}_eye_corner", Ground(5f, -235f, 1.7f), Ground(19f, -242f, 0.3f));
                Eye($"{label}_eye_kerb", Ground(30f, -238.5f, 1.2f), Ground(70f, -241f, 0.2f));
                Eye($"{label}_eye_crown", Ground(112f, -238f, 1.7f), Ground(112f, -200f, 1f));
            }
            finally
            {
                _skyAndGrade = false;
            }

            Debug.Log($"[VeilmouthRenders] the pavements' views written to {OutputFolder} as {label}_*.");
        }

        /// <summary>
        /// The city's greenery and the roadside (city plan §25) at noon: Wardley Garden's beds, the forecourt's parterre and
        /// a villa's hedge from above; the street trees, the beds and the hedges at eye height; the flax field, a farm's
        /// yard and the two roads; and the joins of a wall, a plank fence and a hedge close up. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderGreenery
        /// </summary>
        public static void RenderGreenery()
        {
            const string label = "green";
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var sky = Object.FindFirstObjectByType<GameDev.Slice.Atmosphere.VeilmouthSky>();
            Shader.SetGlobalVector("_Growth_WindSettings", VeilmouthDressing.WindSettings);
            Shader.SetGlobalTexture("_Growth_WindNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(VeilmouthDressing.WindNoisePath));
            _skyAndGrade = sky != null;
            try
            {
                sky?.Apply(12 * 60);
                // Plan (px, py) is world (px − 500, 500 − py).
                Eye($"{label}_warmup", Ground(20f, -228.5f, 1.7f), Ground(80f, -229.5f, 2f));
                File.Delete($"{OutputFolder}/{label}_warmup.png");
                Top($"{label}_top_wardley", new Vector3(58f, 0f, -198f), 16f);
                Top($"{label}_top_parterre", new Vector3(112f, 0f, -159f), 28f);
                Top($"{label}_top_villa", new Vector3(44f, 0f, -160f), 18f);
                Top($"{label}_top_highstreet", new Vector3(60f, 0f, -235f), 30f);
                Top($"{label}_top_farm", new Vector3(-345f, 0f, -150f), 22f);
                Eye($"{label}_eye_highstreet", Ground(20f, -228.5f, 1.7f), Ground(80f, -229.5f, 2f));
                Eye($"{label}_eye_bell", Ground(190f, -276.5f, 1.7f), Ground(130f, -277f, 2f));
                Eye($"{label}_eye_terrace", Ground(22f, -195f, 1.7f), Ground(22f, -215f, 1.5f));
                Eye($"{label}_eye_villa", Ground(44f, -170f, 1.7f), Ground(44f, -163f, 0.8f));
                Eye($"{label}_eye_wardley", Ground(58f, -209.5f, 1.7f), Ground(58f, -190f, 1f));
                Eye($"{label}_eye_parterre", Ground(112f, -167f, 1.7f), Ground(112f, -150f, 1.5f));
                Eye($"{label}_eye_flax", Ground(-322f, -244f, 1.7f), Ground(-322f, -268f, 0.5f));
                Eye($"{label}_eye_farm", Ground(-338f, -162f, 1.7f), Ground(-346f, -144f, 1f));
                Eye($"{label}_eye_westroad", Ground(-290f, -235f, 1.7f), Ground(-230f, -235f, 1f));
                Eye($"{label}_eye_northroad", Ground(247f, -100f, 1.7f), Ground(247f, -20f, 1f));
                Eye($"{label}_close_wall", Ground(-398f, -233f, 1.2f), Ground(-396f, -229f, 0.4f), 50f);
                Eye($"{label}_close_plank", Ground(-349f, -152f, 1.2f), Ground(-352f, -148f, 0.6f), 50f);
                Eye($"{label}_close_hedge", Ground(48f, -170f, 1.2f), Ground(46f, -166.8f, 0.4f), 50f);
            }
            finally
            {
                _skyAndGrade = false;
            }

            Debug.Log($"[VeilmouthRenders] the greenery's views written to {OutputFolder} as {label}_*.");
        }

        /// <summary>
        /// The raised doors that had porches (reference plan R6) close up at noon: both villas, a civic house, the palace
        /// and a detached house of the Terraces, each from 7 m before its door and from the side. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderPorches -lookLabel r6
        /// </summary>
        public static void RenderPorches()
        {
            var label = Argument("-lookLabel") ?? "r6";
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var sky = Object.FindFirstObjectByType<GameDev.Slice.Atmosphere.VeilmouthSky>();
            _skyAndGrade = sky != null;
            try
            {
                sky?.Apply(12 * 60);
                // A named building (the Town Hall, the palace) keeps its kit house as its prefab's source.
                string Source(Transform t) => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) ? PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)?.name ?? "" : "";
                var houses = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => Source(t).StartsWith("Kit_House_")).OrderBy(t => Source(t)).ToList();
                foreach (var kind in new[] { "Villa_stucco", "Villa_white", "Civic", "Palace", "Terrace8_white_detached" })
                {
                    var house = houses.FirstOrDefault(t => Source(t).Contains(kind) && t.Find(VictorianHouses.DoorMarker) != null);
                    if (house == null)
                    {
                        Debug.LogWarning($"[VeilmouthRenders] no {kind} house with a door stands in the city.");
                        continue;
                    }

                    var door = house.Find(VictorianHouses.DoorMarker).position;
                    var at = door + (Vector3.up * 2.2f);
                    var name = kind.ToLowerInvariant();
                    Eye($"{label}_porch_{name}", door + (house.forward * 7f) + (Vector3.up * 1.7f), at, 50f);
                    Eye($"{label}_porch_{name}_side", door + (house.forward * 4f) + (house.right * 4f) + (Vector3.up * 1.7f), at, 50f);
                }
            }
            finally
            {
                _skyAndGrade = false;
            }

            Debug.Log($"[VeilmouthRenders] the doors' views written to {OutputFolder} as {label}_porch_*.");
        }

        /// <summary>
        /// The street dressing of reference plan R7 at noon: the High Street's shop fronts along both pavements, with the
        /// goods set out by their doors, and a Ridge villa's iron balcony close up. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderDressing -lookLabel r7
        /// </summary>
        public static void RenderDressing()
        {
            var label = Argument("-lookLabel") ?? "r7";
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var sky = Object.FindFirstObjectByType<GameDev.Slice.Atmosphere.VeilmouthSky>();
            Shader.SetGlobalVector("_Growth_WindSettings", VeilmouthDressing.WindSettings);
            Shader.SetGlobalTexture("_Growth_WindNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(VeilmouthDressing.WindNoisePath));
            _skyAndGrade = sky != null;
            try
            {
                sky?.Apply(12 * 60);
                Eye($"{label}_warmup", Ground(-5f, -228.5f, 1.7f), Ground(40f, -226f, 1f));
                File.Delete($"{OutputFolder}/{label}_warmup.png");
                // Plan (px, py) is world (px − 500, 500 − py): the High Street's shop fronts stand on py 726 and 744.
                Eye($"{label}_shops_north", Ground(-5f, -228.8f, 1.7f), Ground(40f, -226.2f, 0.8f), 55f);
                Eye($"{label}_shops_south", Ground(130f, -241.2f, 1.7f), Ground(85f, -243.8f, 0.8f), 55f);
                string Source(Transform t) => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) ? PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)?.name ?? "" : "";
                var villa = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                    .Where(t => Source(t).StartsWith("Kit_House_Villa") && t.Find(VictorianHouses.DoorMarker) != null).OrderBy(t => Source(t)).FirstOrDefault();
                if (villa != null)
                {
                    var door = villa.Find(VictorianHouses.DoorMarker).position;
                    Eye($"{label}_villa_balcony", door + (villa.forward * 6f) + (villa.right * 2.5f) + (Vector3.up * 2.2f), door + (Vector3.up * 5f), 50f);
                }
            }
            finally
            {
                _skyAndGrade = false;
            }

            Debug.Log($"[VeilmouthRenders] the street dressing's views written to {OutputFolder} as {label}_*.");
        }

        /// <summary>
        /// The project's painted skies (Free HDR Sky) seen from an empty scene: north, east, south, west at 10° up and
        /// straight up, so the sun and the horizon can be found before the sky is set by the clock. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderSkies
        /// </summary>
        public static void RenderSkies()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (var path in new[] { "Assets/Skybox/Materials/Skybox_Daytime.mat", "Assets/Skybox/Materials/Skybox_Sunset.mat" })
            {
                RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(path);
                var name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                var files = new System.Collections.Generic.List<string>();
                foreach (var (dir, yaw, pitch) in new[] { ("n", 0f, -10f), ("e", 90f, -10f), ("s", 180f, -10f), ("w", 270f, -10f), ("up", 0f, -90f) })
                {
                    var camera = new GameObject("SkyCamera").AddComponent<Camera>();
                    camera.clearFlags = CameraClearFlags.Skybox;
                    camera.fieldOfView = 90f;
                    camera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                    Shoot(camera, $"sky_{name}_{dir}", 800, 800);
                    files.Add($"{OutputFolder}/sky_{name}_{dir}.png");
                }

                SideBySide($"sky_{name}", files.ToArray());
            }

            Debug.Log($"[VeilmouthRenders] the painted skies written to {OutputFolder} as sky_*.");
        }

        /// <summary>
        /// The palette sheet (reference plan R3 step 2): a swatch of every kit material and of the retinted leaves, in
        /// rows, facing Veilmouth's noon sun under its sky and grade, high over the city where nothing shades them. Each
        /// swatch's mean colour is measured from the picture, and its value and saturation (HSV of the sRGB mean) go to
        /// <c>palette.txt</c>, with a mark on any above 40 %. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderPalette
        /// </summary>
        public static void RenderPalette()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var sky = Object.FindFirstObjectByType<GameDev.Slice.Atmosphere.VeilmouthSky>();
            if (sky == null)
            {
                Debug.LogError("[VeilmouthRenders] the scene has no VeilmouthSky: place the mystic layer first.");
                return;
            }

            Shader.SetGlobalVector("_Growth_WindSettings", VeilmouthDressing.WindSettings);
            Shader.SetGlobalTexture("_Growth_WindNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(VeilmouthDressing.WindNoisePath));
            sky.Apply(12 * 60);
            var swatches = GameDev.VictorianKit.EditorTools.VicMaterials.Names.Where(n => !n.StartsWith("Glass"))
                .Select(n => (Name: n, Material: GameDev.VictorianKit.EditorTools.VicMaterials.Load(n)))
                .Concat(AssetDatabase.FindAssets("t:Material", new[] { VeilmouthLook.Folder }).Select(AssetDatabase.GUIDToAssetPath)
                    .Select(p => (Name: Path.GetFileNameWithoutExtension(p), Material: AssetDatabase.LoadAssetAtPath<Material>(p))))
                .Where(s => s.Material != null).ToList();

            // The board faces the sun across the ground, so every swatch takes the same light, as a sunlit wall does.
            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l => l.type == LightType.Directional && l.enabled && l.intensity > 0f);
            var toSun = -sun.transform.forward;
            toSun.y = 0f;
            toSun.Normalize();
            var facing = Quaternion.LookRotation(-toSun);
            var right = facing * Vector3.right;
            const int columns = 6;
            const float size = 1.6f, step = 2f;
            var rows = (swatches.Count + columns - 1) / columns;
            var centre = new Vector3(100f, VeilmouthScene.StreetLevel + 150f, -250f);
            var at = new Vector3[swatches.Count];
            var board = new GameObject("PaletteBoard");
            for (var k = 0; k < swatches.Count; k++)
            {
                at[k] = centre + (right * ((k % columns) - ((columns - 1) * 0.5f)) * step) + (Vector3.up * (((rows - 1) * 0.5f) - (k / columns)) * step);
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = swatches[k].Name;
                quad.transform.SetParent(board.transform);
                quad.transform.SetPositionAndRotation(at[k], facing);
                quad.transform.localScale = new Vector3(size, size, 1f);
                quad.GetComponent<Renderer>().sharedMaterial = swatches[k].Material;
            }

            _skyAndGrade = true;
            try
            {
                for (var pass = 0; pass < 2; pass++) // the first picture is drawn before every texture has loaded
                {
                    var camera = Make();
                    camera.fieldOfView = 40f;
                    camera.aspect = 16f / 9f;
                    camera.farClipPlane = 1500f;
                    camera.transform.SetPositionAndRotation(centre + (toSun * 12f), facing);
                    var rects = at.Select(p => (Lo: camera.WorldToViewportPoint(p - (right * size * 0.3f) - (Vector3.up * size * 0.3f)), Hi: camera.WorldToViewportPoint(p + (right * size * 0.3f) + (Vector3.up * size * 0.3f)))).ToArray();
                    Shoot(camera, "palette", 1600, 900);
                    if (pass == 0)
                    {
                        continue;
                    }

                    var image = new Texture2D(2, 2);
                    image.LoadImage(File.ReadAllBytes($"{OutputFolder}/palette.png"));
                    var report = new System.Text.StringBuilder("material\tvalue\tsaturation\tsRGB mean\n");
                    for (var k = 0; k < swatches.Count; k++)
                    {
                        var sum = Vector3.zero;
                        var n = 0;
                        for (var y = (int)(rects[k].Lo.y * image.height); y < (int)(rects[k].Hi.y * image.height); y++)
                        {
                            for (var x = (int)(rects[k].Lo.x * image.width); x < (int)(rects[k].Hi.x * image.width); x++)
                            {
                                var c = image.GetPixel(x, y);
                                sum += new Vector3(Mathf.GammaToLinearSpace(c.r), Mathf.GammaToLinearSpace(c.g), Mathf.GammaToLinearSpace(c.b));
                                n++;
                            }
                        }

                        var mean = new Color(Mathf.LinearToGammaSpace(sum.x / n), Mathf.LinearToGammaSpace(sum.y / n), Mathf.LinearToGammaSpace(sum.z / n));
                        Color.RGBToHSV(mean, out _, out var s, out var v);
                        report.Append($"{swatches[k].Name}\t{v:0.00}\t{s:0.00}{(s > 0.4f ? " OVER 40 %" : string.Empty)}\t({mean.r:0.00}, {mean.g:0.00}, {mean.b:0.00})\n");
                    }

                    Object.DestroyImmediate(image);
                    File.WriteAllText($"{OutputFolder}/palette.txt", report.ToString());
                    Debug.Log($"[VeilmouthRenders] palette sheet, left to right and top to bottom:\n{report}");
                }
            }
            finally
            {
                _skyAndGrade = false;
                Object.DestroyImmediate(board);
            }
        }

        /// <summary>
        /// The check of reference plan R2 step 4: the same street views with every window glass forced to its near look
        /// (the built rooms through clear glass) and to its far look (the mapped room), at noon and at 21:00, side by
        /// side, so the turn between them can be judged; each pair's mean difference over the image goes to
        /// <c>room_turn.txt</c>. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderRoomTurn
        /// </summary>
        public static void RenderRoomTurn() => RenderRoomTurn("roomturn");

        /// <inheritdoc cref="RenderRoomTurn()"/>
        public static void RenderRoomTurn(string label)
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var sky = Object.FindFirstObjectByType<GameDev.Slice.Atmosphere.VeilmouthSky>();
            var glasses = new[] { "Glass_Rich", "Glass_Poor" }.Select(GameDev.VictorianKit.EditorTools.VicMaterials.Load).Where(m => m != null).ToList();
            var saved = glasses.Select(m => (m.GetFloat("_RoomNear"), m.GetFloat("_RoomFar"))).ToList();
            // Windows 12–45 m away, within the built rooms' 48 m.
            var views = new (string Name, Vector3 From, Vector3 To)[]
            {
                ("highstreet", Ground(40f, -238f, 1.7f), Ground(72f, -226f, 4f)),
                ("market", Ground(60f, -252f, 1.7f), Ground(95f, -270f, 4f)),
            };
            var report = new System.Text.StringBuilder("view\thour\tmean difference (0-255)\n");
            _skyAndGrade = true;
            try
            {
                foreach (var (minute, hour) in new[] { (12 * 60, "1200"), (21 * 60, "2100") })
                {
                    sky.Apply(minute);
                    foreach (var view in views)
                    {
                        var files = new System.Collections.Generic.List<string>();
                        foreach (var (look, near, far) in new[] { ("near", 1000f, 1001f), ("far", 0f, 0.01f) })
                        {
                            foreach (var m in glasses)
                            {
                                m.SetFloat("_RoomNear", near);
                                m.SetFloat("_RoomFar", far);
                            }

                            var name = $"{label}_{view.Name}_{hour}_{look}";
                            Eye(name, view.From, view.To, 55f);
                            Eye(name, view.From, view.To, 55f); // the first picture after a change is drawn before the rooms switch
                            files.Add($"{OutputFolder}/{name}.png");
                        }

                        SideBySide($"{label}_{view.Name}_{hour}", files.ToArray());
                        var a = new Texture2D(2, 2);
                        var b = new Texture2D(2, 2);
                        a.LoadImage(File.ReadAllBytes(files[0]));
                        b.LoadImage(File.ReadAllBytes(files[1]));
                        var pa = a.GetPixels32();
                        var pb = b.GetPixels32();
                        double sum = 0;
                        for (var k = 0; k < pa.Length; k++)
                        {
                            sum += (Mathf.Abs(pa[k].r - pb[k].r) + Mathf.Abs(pa[k].g - pb[k].g) + Mathf.Abs(pa[k].b - pb[k].b)) / 3.0;
                        }

                        report.Append($"{view.Name}\t{hour}\t{sum / pa.Length:0.00}\n");
                        Object.DestroyImmediate(a);
                        Object.DestroyImmediate(b);
                    }
                }
            }
            finally
            {
                _skyAndGrade = false;
                for (var k = 0; k < glasses.Count; k++)
                {
                    glasses[k].SetFloat("_RoomNear", saved[k].Item1);
                    glasses[k].SetFloat("_RoomFar", saved[k].Item2);
                }
            }

            File.WriteAllText($"{OutputFolder}/{label}.txt", report.ToString());
            Debug.Log($"[VeilmouthRenders] room turn ({label}):\n{report}");
        }

        /// <summary>
        /// The merged far LOD against the pieces it replaces (city plan §29.10): FrameProbe's seven views with the player's
        /// 60° at 16:00 and 23:00, each drawn with the kit houses alone forced to their pieces, then to their far LOD, then
        /// the whole city as built, and the difference of the first two four times as bright, with its mean and the share
        /// of pixels differing by more than 8 of 255. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderFarLod
        /// </summary>
        public static void RenderFarLod()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var sky = Object.FindFirstObjectByType<GameDev.Slice.Atmosphere.VeilmouthSky>();
            Shader.SetGlobalVector("_Growth_WindSettings", VeilmouthDressing.WindSettings);
            Shader.SetGlobalTexture("_Growth_WindNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(VeilmouthDressing.WindNoisePath));
            var views = new (string Name, Vector3 From, Vector3 To)[]
            {
                ("longtongue", Ground(-70f, 60f, 1.7f), Ground(30f, 60f, 2f)),
                ("saltgullet", Ground(150f, 60f, 1.7f), Ground(40f, 60f, 2f)),
                ("penny", Ground(2f, 58f, 1.7f), Ground(40f, 60f, 2f)),
                ("thimble", Ground(-22f, 115f, 1.7f), Ground(-22f, -40f, 2f)),
                ("draught", Ground(150f, 4f, 1.7f), Ground(50f, 4f, 2f)),
                ("westroad", Ground(-140f, 60f, 1.7f), Ground(-60f, 60f, 2f)),
                ("gloomwood", Ground(-240f, -50f, 1.7f), Ground(-200f, -110f, 2f)),
            };
            var houses = Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Where(l => l.transform.Find("Model/" + VeilmouthFarLod.Name) != null).ToList();
            var houseRoots = new System.Collections.Generic.HashSet<Transform>(houses.Select(l => l.transform));
            var others = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r =>
            {
                for (var t = r.transform; t != null; t = t.parent)
                {
                    if (houseRoots.Contains(t))
                    {
                        return false;
                    }
                }

                return !r.forceRenderingOff;
            }).ToList();
            var report = new System.Collections.Generic.List<string>
            {
                $"{houses.Count} kit houses with a far LOD, drawn alone; their pieces against as played (the switch at {VeilmouthFarLod.FarDistance:0} m) and against the far LOD forced, 0–255 per channel:",
            };
            _skyAndGrade = sky != null;
            try
            {
                sky?.Apply(16 * 60);
                Eye("farlod_warmup", views[0].From, views[0].To, 60f);
                File.Delete($"{OutputFolder}/farlod_warmup.png");
                foreach (var (minute, hour) in new[] { (16 * 60, "1600"), (23 * 60, "2300") })
                {
                    sky?.Apply(minute);
                    foreach (var fog in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                    {
                        fog.Simulate(30f, true, true);
                    }

                    foreach (var view in views)
                    {
                        var stem = $"{OutputFolder}/farlod_{view.Name}_{hour}";
                        // The kit houses alone forced to their pieces, forced to their far LOD, and switching by distance as in
                        // play, so the trees' wind is no difference; then the whole city as built.
                        foreach (var (level, alone, label) in new[] { (0, true, "near"), (1, true, "far"), (-1, true, "auto"), (-1, false, "built") })
                        {
                            others.ForEach(r => r.forceRenderingOff = alone);
                            houses.ForEach(l => l.ForceLOD(level));
                            Eye(Path.GetFileName($"{stem}_{label}"), view.From, view.To, 60f);
                        }

                        var (mean, share, _) = Difference($"{stem}_near.png", $"{stem}_far.png", $"{stem}_diff.png");
                        var (playMean, playShare, worst) = Difference($"{stem}_near.png", $"{stem}_auto.png", $"{stem}_playdiff.png");
                        Grid(Path.GetFileName($"{stem}_sheet"), new[] { $"{stem}_near.png", $"{stem}_auto.png", $"{stem}_built.png", $"{stem}_playdiff.png" }, 2);
                        Crop($"{stem}_crop.png", worst, new[] { $"{stem}_near.png", $"{stem}_auto.png", $"{stem}_far.png", $"{stem}_playdiff.png" });
                        report.Add($"{view.Name} {hour}: as played mean {playMean:0.000}, {playShare:P3} of pixels over 8; all far mean {mean:0.000}, {share:P3}; " +
                                   $"worst box at ({worst.x}, {worst.y})");
                    }
                }
            }
            finally
            {
                houses.ForEach(l => l.ForceLOD(-1));
                others.ForEach(r => r.forceRenderingOff = false);
                _skyAndGrade = false;
            }

            File.WriteAllLines($"{OutputFolder}/c5_farlod_views.txt", report);
            Debug.Log($"[VeilmouthRenders] far LOD views written to {OutputFolder} as farlod_*:\n{string.Join("\n", report)}");
        }

        private const int CropWidth = 200, CropHeight = 112;

        /// <summary>The same box from each picture, side by side, three times as large.</summary>
        private static void Crop(string output, RectInt box, string[] files)
        {
            const int scale = 3;
            var sheet = new Texture2D((box.width * scale * files.Length) + (4 * (files.Length - 1)), box.height * scale, TextureFormat.RGB24, false);
            var white = Enumerable.Repeat(new Color32(255, 255, 255, 255), sheet.width * sheet.height).ToArray();
            sheet.SetPixels32(white);
            for (var k = 0; k < files.Length; k++)
            {
                var t = new Texture2D(2, 2);
                t.LoadImage(File.ReadAllBytes(files[k]));
                for (var y = 0; y < box.height * scale; y++)
                {
                    for (var x = 0; x < box.width * scale; x++)
                    {
                        sheet.SetPixel((k * ((box.width * scale) + 4)) + x, y, t.GetPixel(box.x + (x / scale), box.y + (y / scale)));
                    }
                }
            }

            sheet.Apply();
            File.WriteAllBytes(output, sheet.EncodeToPNG());
        }

        /// <summary>
        /// The mean channel difference of two pictures, the share of pixels differing by more than 8 in any channel, and
        /// the box of <see cref="CropWidth"/> × <see cref="CropHeight"/> where they differ most; the difference ×4 written out.
        /// </summary>
        private static (double Mean, double Share, RectInt Worst) Difference(string a, string b, string output)
        {
            var ta = new Texture2D(2, 2);
            var tb = new Texture2D(2, 2);
            ta.LoadImage(File.ReadAllBytes(a));
            tb.LoadImage(File.ReadAllBytes(b));
            var pa = ta.GetPixels32();
            var pb = tb.GetPixels32();
            var diff = new Color32[pa.Length];
            long sum = 0;
            var over = 0;
            for (var i = 0; i < pa.Length; i++)
            {
                int dr = Mathf.Abs(pa[i].r - pb[i].r), dg = Mathf.Abs(pa[i].g - pb[i].g), db = Mathf.Abs(pa[i].b - pb[i].b);
                sum += dr + dg + db;
                over += Mathf.Max(dr, Mathf.Max(dg, db)) > 8 ? 1 : 0;
                diff[i] = new Color32((byte)Mathf.Min(255, dr * 4), (byte)Mathf.Min(255, dg * 4), (byte)Mathf.Min(255, db * 4), 255);
            }

            var t = new Texture2D(ta.width, ta.height, TextureFormat.RGB24, false);
            t.SetPixels32(diff);
            File.WriteAllBytes(output, t.EncodeToPNG());

            // The worst box, on a grid of a quarter box.
            var worst = new RectInt(0, 0, CropWidth, CropHeight);
            long most = -1;
            for (var y0 = 0; y0 + CropHeight <= ta.height; y0 += CropHeight / 4)
            {
                for (var x0 = 0; x0 + CropWidth <= ta.width; x0 += CropWidth / 4)
                {
                    long box = 0;
                    for (var y = y0; y < y0 + CropHeight; y++)
                    {
                        for (var x = x0; x < x0 + CropWidth; x++)
                        {
                            var c = diff[(y * ta.width) + x];
                            box += c.r + c.g + c.b;
                        }
                    }

                    if (box > most)
                    {
                        (most, worst) = (box, new RectInt(x0, y0, CropWidth, CropHeight));
                    }
                }
            }

            return (sum / (3.0 * pa.Length), (double)over / pa.Length, worst);
        }

        /// <summary>A point <paramref name="up"/> metres above Veilmouth's ground at (x, z).</summary>
        private static Vector3 Ground(float x, float z, float up)
        {
            var terrain = Terrain.activeTerrain;
            return new Vector3(x, terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y + up, z);
        }

        /// <summary>The value after <paramref name="name"/> on the command line, or null.</summary>
        private static string Argument(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            var i = System.Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        /// <summary>Pictures of one size at half size in a grid of <paramref name="columns"/>, left to right and top to bottom.</summary>
        private static void Grid(string name, string[] files, int columns)
        {
            var parts = files.Select(f =>
            {
                var t = new Texture2D(2, 2);
                t.LoadImage(File.ReadAllBytes(f));
                return t;
            }).ToArray();
            int w = parts[0].width / 2, h = parts[0].height / 2, rows = (parts.Length + columns - 1) / columns;
            var sheet = new Texture2D(w * columns, h * rows, TextureFormat.RGB24, false);
            for (var k = 0; k < parts.Length; k++)
            {
                var ox = (k % columns) * w;
                var oy = (rows - 1 - (k / columns)) * h; // the first picture at the top left
                for (var y = 0; y < h; y++)
                {
                    for (var x = 0; x < w; x++)
                    {
                        sheet.SetPixel(ox + x, oy + y, parts[k].GetPixelBilinear((x + 0.5f) / w, (y + 0.5f) / h));
                    }
                }

                Object.DestroyImmediate(parts[k]);
            }

            sheet.Apply();
            File.WriteAllBytes($"{OutputFolder}/{name}.png", sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
        }

        /// <summary>
        /// The same street views at noon at the quality level the editor used to play at (Very Low) and at the one the
        /// editor and the game use now (High; reference plan R2, step 1), left and right in one picture. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderQuality
        /// </summary>
        public static void RenderQuality()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            Shader.SetGlobalVector("_Growth_WindSettings", VeilmouthDressing.WindSettings);
            Shader.SetGlobalTexture("_Growth_WindNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(VeilmouthDressing.WindNoisePath));
            Object.FindFirstObjectByType<GameDev.Slice.Atmosphere.VeilmouthSky>()?.Apply(12 * 60);
            var saved = QualitySettings.GetQualityLevel();
            var levels = new[] { ("verylow", System.Array.IndexOf(QualitySettings.names, "Very Low")), ("high", System.Array.IndexOf(QualitySettings.names, "High")) };
            var views = new[]
            {
                ("highstreet", -10f, -235f, 100f, -235f, 3f),
                ("parade", 0f, -174f, 120f, -174f, 3f),
                ("bellstreet", 195f, -278f, 100f, -278f, 2.5f),
                ("terraces", 22f, -184f, 22f, -215f, 2f),
            };
            _skyAndGrade = true;
            try
            {
                // The first picture after opening the scene was drawn before every texture had loaded: draw one to waste.
                Eye("quality_warmup", views[0].Item2, views[0].Item3, views[0].Item4, views[0].Item5, views[0].Item6);
                File.Delete($"{OutputFolder}/quality_warmup.png");
                foreach (var (view, x, z, toX, toZ, up) in views)
                {
                    foreach (var (label, level) in levels)
                    {
                        QualitySettings.SetQualityLevel(level, true);
                        Eye($"quality_{view}_{label}", x, z, toX, toZ, up);
                    }

                    SideBySide($"quality_{view}", levels.Select(l => $"{OutputFolder}/quality_{view}_{l.Item1}.png").ToArray());
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(saved, true);
                _skyAndGrade = false;
            }

            Debug.Log($"Veilmouth quality views written to {OutputFolder}.");
        }

        /// <summary>
        /// The rooms' distance switch checked by pictures (reference plan R2, step 3): the four street views at noon and at
        /// night, each drawn as the switch leaves it, with every room on, and with every room off. The switch is unseen
        /// when the first two match, and the rooms near the camera are drawn when the first and the last differ. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderRoomsSwitch
        /// </summary>
        public static void RenderRoomsSwitch()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            Shader.SetGlobalVector("_Growth_WindSettings", Vector4.zero); // no wind: the leaves stand still between the pictures
            Shader.SetGlobalTexture("_Growth_WindNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(VeilmouthDressing.WindNoisePath));
            var sky = Object.FindFirstObjectByType<GameDev.Slice.Atmosphere.VeilmouthSky>();
            var switches = Object.FindObjectsByType<VicRoomsSwitch>(FindObjectsSortMode.None);
            var views = new[]
            {
                ("highstreet", -10f, -235f, 100f, -235f, 3f),
                ("parade", 0f, -174f, 120f, -174f, 3f),
                ("bellstreet", 195f, -278f, 100f, -278f, 2.5f),
                ("terraces", 22f, -184f, 22f, -215f, 2f),
            };

            // Only the houses are drawn for the comparison: leaves and people differ from one picture to the next.
            var houses = new System.Collections.Generic.HashSet<Transform>(switches.Select(s => s.GetComponentInParent<GameDev.Slice.Kit.KitBlock>()).Where(b => b != null).Select(b => b.transform));
            bool InHouse(Transform t)
            {
                for (; t != null; t = t.parent)
                {
                    if (houses.Contains(t))
                    {
                        return true;
                    }
                }

                return false;
            }

            var others = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => !r.forceRenderingOff && !InHouse(r.transform)).ToArray();
            var report = new System.Collections.Generic.List<string> { $"{switches.Length} houses with rooms; {others.Length} other renderers hidden for the comparison" };
            _skyAndGrade = true;
            try
            {
                sky?.Apply(12 * 60);
                Eye("rooms_warmup", views[0].Item2, views[0].Item3, views[0].Item4, views[0].Item5, views[0].Item6);
                File.Delete($"{OutputFolder}/rooms_warmup.png");
                foreach (var (time, minute) in new[] { ("noon", 12 * 60), ("night", 23 * 60) })
                {
                    sky?.Apply(minute);
                    foreach (var (view, x, z, toX, toZ, up) in views)
                    {
                        var name = $"rooms_{view}_{time}";
                        foreach (var r in others)
                        {
                            r.forceRenderingOff = true;
                        }

                        Eye(name + "_switch", x, z, toX, toZ, up);
                        var shown = switches.Count(s => s.Shown);
                        ForceRooms(switches, true);
                        Eye(name + "_on", x, z, toX, toZ, up);
                        ForceRooms(switches, false);
                        Eye(name + "_off", x, z, toX, toZ, up);
                        ReleaseRooms(switches);
                        var (unseen, worst) = Differ($"{OutputFolder}/{name}_switch.png", $"{OutputFolder}/{name}_on.png");
                        var (near, _) = Differ($"{OutputFolder}/{name}_switch.png", $"{OutputFolder}/{name}_off.png");
                        report.Add($"{name}: rooms on in {shown} of {switches.Length} houses; {unseen} pixels differ from every room on (largest {worst}/255), " +
                                   $"{near} from every room off");
                        if (time == "noon")
                        {
                            report.AddRange(Culprits(name, x, z, toX, toZ, up, switches));
                        }

                        foreach (var r in others)
                        {
                            r.forceRenderingOff = false;
                        }
                        SideBySide(name, new[] { $"{OutputFolder}/{name}_switch.png", $"{OutputFolder}/{name}_off.png" });
                    }
                }
            }
            finally
            {
                ReleaseRooms(switches);
                foreach (var r in others)
                {
                    r.forceRenderingOff = false;
                }

                _skyAndGrade = false;
            }

            File.WriteAllLines($"{OutputFolder}/rooms_switch.txt", report);
            Debug.Log("[RoomsSwitch]\n" + string.Join("\n", report));
        }

        /// <summary>
        /// The houses whose rooms make the difference between a view as the switch leaves it and with every room on: a
        /// ray through each differing pixel to the nearest rooms' box it passes within 3 m of, counted by house; the
        /// four worst are drawn close up with every room on and every room off.
        /// </summary>
        private static System.Collections.Generic.IEnumerable<string> Culprits(string name, float x, float z, float toX, float toZ, float up, VicRoomsSwitch[] switches)
        {
            var terrain = Terrain.activeTerrain;
            float Ground(float gx, float gz) => terrain.SampleHeight(new Vector3(gx, 0f, gz)) + terrain.transform.position.y;
            var from = new Vector3(x, Ground(x, z) + 1.7f, z);
            var to = new Vector3(toX, Ground(toX, toZ) + up, toZ);
            var camera = Make();
            camera.fieldOfView = 65f;
            camera.aspect = 1600f / 900f;
            camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(to - from));
            var mask = new Texture2D(2, 2);
            mask.LoadImage(File.ReadAllBytes($"{OutputFolder}/{name}_on_diff.png"));
            var pixels = mask.GetPixels32();
            var hits = new System.Collections.Generic.Dictionary<VicRoomsSwitch, (int Count, Vector3 Point)>();
            for (var i = 0; i < pixels.Length; i += 1)
            {
                if (pixels[i].r != 255 || pixels[i].g != 0)
                {
                    continue;
                }

                var ray = camera.ViewportPointToRay(new Vector3(((i % mask.width) + 0.5f) / mask.width, ((i / mask.width) + 0.5f) / mask.height, 0f));
                VicRoomsSwitch best = null;
                var bestT = float.MaxValue;
                foreach (var s in switches.Where(s => !s.Shown))
                {
                    var local = new Ray(s.transform.InverseTransformPoint(ray.origin), s.transform.InverseTransformDirection(ray.direction));
                    var grown = s.Box;
                    grown.Expand(3f);
                    if (grown.IntersectRay(local, out var t) && t < bestT)
                    {
                        (best, bestT) = (s, t);
                    }
                }

                if (best != null)
                {
                    var point = best.transform.TransformPoint(new Ray(best.transform.InverseTransformPoint(ray.origin), best.transform.InverseTransformDirection(ray.direction)).GetPoint(bestT));
                    hits[best] = hits.TryGetValue(best, out var h) ? (h.Count + 1, h.Point) : (1, point);
                }
            }

            Object.DestroyImmediate(mask);
            Object.DestroyImmediate(camera.gameObject);
            var lines = new System.Collections.Generic.List<string>();
            var k = 0;
            foreach (var (rooms, (count, point)) in hits.OrderByDescending(p => p.Value.Count).Select(p => (p.Key, p.Value)))
            {
                var house = rooms.GetComponentInParent<GameDev.Slice.Kit.KitBlock>();
                var label = house != null ? house.name : rooms.transform.root.name;
                lines.Add($"  {count} pixels: {label} at {point.x:0},{point.y:0},{point.z:0}, {rooms.DistanceFrom(from):0.0} m from the rooms' box");
                if (k >= 4)
                {
                    continue;
                }

                // Close up: 14 m back toward the view and 4 m up, every room on, then off.
                var back = from - point;
                back.y = 0f;
                var eye = point + (back.normalized * 14f) + (Vector3.up * 4f);
                var shot = $"{name}_culprit{k}";
                ForceRooms(switches, true);
                Eye(shot + "_on", eye, point);
                ForceRooms(switches, false);
                Eye(shot + "_off", eye, point);
                ReleaseRooms(switches);
                SideBySide(shot, new[] { $"{OutputFolder}/{shot}_on.png", $"{OutputFolder}/{shot}_off.png" });
                k++;
            }

            return lines;
        }

        // The switches stand aside while every room is drawn or hidden by hand, and take over again with their own state.
        private static void ForceRooms(VicRoomsSwitch[] switches, bool on)
        {
            foreach (var s in switches)
            {
                s.enabled = false;
                foreach (var r in s.GetComponentsInChildren<Renderer>(true))
                {
                    r.forceRenderingOff = !on;
                }
            }
        }

        private static void ReleaseRooms(VicRoomsSwitch[] switches)
        {
            foreach (var s in switches)
            {
                s.enabled = true;
                foreach (var r in s.GetComponentsInChildren<Renderer>(true))
                {
                    r.forceRenderingOff = !s.Shown;
                }
            }
        }

        /// <summary>
        /// How many pixels of two pictures differ by more than 2 of 255 in any channel, and the largest difference; the
        /// differing pixels are written red over the first picture, darkened, as "{b}_diff.png".
        /// </summary>
        private static (int Count, int Largest) Differ(string a, string b)
        {
            var ta = new Texture2D(2, 2);
            var tb = new Texture2D(2, 2);
            ta.LoadImage(File.ReadAllBytes(a));
            tb.LoadImage(File.ReadAllBytes(b));
            var pa = ta.GetPixels32();
            var pb = tb.GetPixels32();
            var mask = new Color32[pa.Length];
            int count = 0, largest = 0;
            for (var i = 0; i < pa.Length; i++)
            {
                var d = Mathf.Max(Mathf.Abs(pa[i].r - pb[i].r), Mathf.Abs(pa[i].g - pb[i].g), Mathf.Abs(pa[i].b - pb[i].b));
                largest = Mathf.Max(largest, d);
                mask[i] = d > 2 ? new Color32(255, 0, 0, 255) : new Color32((byte)(pa[i].r / 3), (byte)(pa[i].g / 3), (byte)(pa[i].b / 3), 255);
                if (d > 2)
                {
                    count++;
                }
            }

            var diff = new Texture2D(ta.width, ta.height, TextureFormat.RGB24, false);
            diff.SetPixels32(mask);
            diff.Apply();
            File.WriteAllBytes(Path.ChangeExtension(b, null) + "_diff.png", diff.EncodeToPNG());
            Object.DestroyImmediate(diff);

            // The six 40-pixel tiles with the most differing pixels, each 120 pixels round it from both pictures, ×3.
            const int tile = 40, half = 60, zoom = 3;
            var tiles = new System.Collections.Generic.Dictionary<int, int>();
            for (var i = 0; i < pa.Length; i++)
            {
                if (mask[i].r == 255 && mask[i].g == 0)
                {
                    var key = ((i / ta.width / tile) * 1000) + ((i % ta.width) / tile);
                    tiles[key] = tiles.TryGetValue(key, out var n) ? n + 1 : 1;
                }
            }

            var worst = tiles.OrderByDescending(t => t.Value).Take(6).ToList();
            if (worst.Count > 0)
            {
                var side = 2 * half * zoom;
                var sheet = new Texture2D(side * 2, side * worst.Count, TextureFormat.RGB24, false);
                for (var k = 0; k < worst.Count; k++)
                {
                    var cy = ((worst[k].Key / 1000) * tile) + (tile / 2);
                    var cx = ((worst[k].Key % 1000) * tile) + (tile / 2);
                    for (var y = 0; y < side; y++)
                    {
                        for (var x = 0; x < side; x++)
                        {
                            var sx = Mathf.Clamp(cx - half + (x / zoom), 0, ta.width - 1);
                            var sy = Mathf.Clamp(cy - half + (y / zoom), 0, ta.height - 1);
                            sheet.SetPixel(x, (k * side) + y, pa[(sy * ta.width) + sx]);
                            sheet.SetPixel(side + x, (k * side) + y, pb[(sy * ta.width) + sx]);
                        }
                    }
                }

                sheet.Apply();
                File.WriteAllBytes(Path.ChangeExtension(b, null) + "_zoom.png", sheet.EncodeToPNG());
                Object.DestroyImmediate(sheet);
            }
            Object.DestroyImmediate(ta);
            Object.DestroyImmediate(tb);
            return (count, largest);
        }

        private static void SideBySide(string name, string[] files)
        {
            var parts = files.Select(f =>
            {
                var t = new Texture2D(2, 2);
                t.LoadImage(File.ReadAllBytes(f));
                return t;
            }).ToArray();
            var sheet = new Texture2D(parts.Sum(p => p.width), parts.Max(p => p.height), TextureFormat.RGB24, false);
            var x = 0;
            foreach (var part in parts)
            {
                sheet.SetPixels(x, 0, part.width, part.height, part.GetPixels());
                x += part.width;
                Object.DestroyImmediate(part);
            }

            sheet.Apply();
            File.WriteAllBytes($"{OutputFolder}/{name}.png", sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
        }

        private static void Shoot(Camera camera, string name, int width, int height)
        {
            // The pipeline's multisampling, as the player's camera draws (reference plan R2, step 1).
            var samples = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp ? urp.msaaSampleCount : 1;
            var target = new RenderTexture(width, height, 24) { antiAliasing = Mathf.Max(1, samples) };
            if (_skyAndGrade)
            {
                Object.FindFirstObjectByType<GameDev.Slice.Atmosphere.VeilmouthSky>()?.FaceMoon(camera.transform.position);
            }

            camera.targetTexture = target;
            camera.Render();
            if (target.antiAliasing > 1)
            {
                target.ResolveAntiAliasedSurface();
            }

            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes($"{OutputFolder}/{name}.png", image.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(camera.gameObject);
        }
    }
}
