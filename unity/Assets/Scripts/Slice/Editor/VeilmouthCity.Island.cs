using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Veilmouth on the island (Assets/Plans/ISLAND_CITY_REBUILD_PLAN.md §3): a district is a frame. Each district of
    /// the island's layout is built here by C5's own steps in its own local metres, where everything is along the axes
    /// as C5 is, and then set in its place under a <see cref="KitFrame"/> that carries its turn.
    /// <para>
    /// A district is built on the slip, not in place: its plan point (u, v) stands at C5's world point for that plan
    /// point while it is built, and only the ground's height is read where the district will lie
    /// (<see cref="_frameWorld"/>). The finished district is one rigid group, moved and turned as a whole. So none of
    /// the builder's rectangles, quarter turns and lines along an axis change.
    /// </para>
    /// Built in the island's lab scene, a duplicate site: Veilmouth.unity, its terrain and C5's layout are not opened.
    /// This stage (IC3-IC4) builds the streets with their pavements, the buildings, houses and yards, the stone masses,
    /// the lamps and the street trees. What lies between the districts, the dress passes and the game's own places
    /// come in the stages after it.
    /// Batch, after VeilmouthIslandLab.Build: -executeMethod GameDev.Slice.EditorTools.VeilmouthCity.BuildIsland
    /// </summary>
    public static partial class VeilmouthCity
    {
        public const string IslandScenePath = "Assets/Scenes/Slice/Lab/IslandLab.unity";
        public const string IslandLayoutPath = "Assets/Data/Slice/Map/VeilmouthIslandLayout.json";
        public const string IslandRootName = "Veilmouth Island City";

        /// <summary>The generated meshes of the island's districts, apart from C5's and written again by each build.</summary>
        public const string IslandWorksFolder = SliceKitCatalogue.Folder + "/Works/Island";

        [Serializable]
        private sealed class IslandFile
        {
            public string version;
            public IslandFrame[] frames;
        }

        [Serializable]
        private sealed class IslandFrame
        {
            public string id, name, district;
            public VeilmouthLayout.Point origin;
            public float angle, U, V;
            public VeilmouthLayout.Street[] streets;
            public VeilmouthLayout.Square[] squares;
            public VeilmouthLayout.BlockSpec[] blocks;
            public VeilmouthLayout.BuildingSpec[] buildings;
            public VeilmouthLayout.PassageSpec[] passages;
            public VeilmouthLayout.BoxSpec[] boxes;
            public VeilmouthLayout.PlazaSpec plaza;
            public VeilmouthLayout.Candidate[] candidates;
        }

        public static void BuildIsland()
        {
            var scene = EditorSceneManager.OpenScene(IslandScenePath, OpenSceneMode.Single);
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == IslandRootName))
            {
                Object.DestroyImmediate(old);
            }

            _kit = SliceKitCatalogue.Load();
            // Never baked here: baking writes the kit's prefabs, which are the production city's too.
            foreach (var pair in VeilmouthKitHouses.Load())
            {
                _kit[pair.Key] = pair.Value;
            }

            var file = JsonUtility.FromJson<IslandFile>(File.ReadAllText(IslandLayoutPath));
            var missing = file.frames.SelectMany(f => f.buildings ?? Array.Empty<VeilmouthLayout.BuildingSpec>())
                .Select(b => KitKey(b.key)).Distinct().Where(k => !_kit.ContainsKey(k)).ToList();
            if (missing.Count > 0)
            {
                throw new InvalidOperationException("the island's layout asks for houses the kit has not baked: " + string.Join(", ", missing));
            }

            _terrain = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Terrain>()).Single();
            var data = _terrain.terrainData;
            var paving = PavingLayers(data);
            var maps = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);

            var city = new GameObject(IslandRootName).transform;
            var report = new StringBuilder();
            report.AppendLine($"Veilmouth on the island, {DateTime.Now:yyyy-MM-dd HH:mm}: layout {file.version}, {file.frames.Length} frames");
            if (AssetDatabase.IsValidFolder(IslandWorksFolder))
            {
                AssetDatabase.DeleteAsset(IslandWorksFolder);
            }

            try
            {
                foreach (var f in file.frames.Where(f => f.streets != null && f.streets.Length > 0))
                {
                    BuildFrame(f, city, maps, paving, report);
                }
            }
            finally
            {
                // Whatever happened, the builder is C5's again: built in place, its meshes in its own folder.
                _frameWorld = null;
                _worksFolder = SliceKitCatalogue.Folder + "/Works";
            }

            data.SetAlphamaps(0, 0, maps);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Physics.SyncTransforms();

            var blocks = city.GetComponentsInChildren<KitBlock>(false).Where(b => b.enabled).ToArray();
            var faults = KitLayoutCheck.Check(blocks);
            report.AppendLine($"kit check: {blocks.Length} blocks, {faults.Count} faults");
            foreach (var group in faults.GroupBy(x => x.Kind).OrderByDescending(g => g.Count()))
            {
                report.AppendLine($"  {group.Key}: {group.Count()}");
                foreach (var fault in group.Take(8))
                {
                    report.AppendLine("     " + fault);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Logs/IslandCity");
            File.WriteAllText("Logs/IslandCity/report.txt", report.ToString());
            Debug.Log("[VeilmouthCity] the island\n" + report);
        }

        private static void BuildFrame(IslandFrame f, Transform city, float[,,] maps, (int Grass, int Earth, int Setts, int Cobbles) paving, StringBuilder report)
        {
            // The builder's own state, as Run sets it, fresh for each district: a district is built from start to
            // finish before the next begins, and its lists are in its own metres.
            _taken = new List<(Vector2[], Use)>();
            _lanes = new List<Lane>();
            _doors = new List<(Vector2, Vector2)>();
            _partyWalls = new List<(bool, float, float)>();
            _houses = new List<Vector2[]>();
            _stoops = new List<Vector2[]>();
            _backs = new List<(Facing, float, int, int, int)>();
            _fenced = new HashSet<(bool, float, int, int)>();
            _stepsClear = float.MaxValue;
            _lampsDropped = 0;
            _postsAt.Clear();
            _postsAdded = _postsHidden = _railingsDrawnOut = 0;
            _pavements = null;
            _passages = 0;
            _openSpans = 0;
            _decks.Clear();
            _sunk.Clear();
            (_terraceCuts, _terraceLift) = (0, 0f);

            var yaw = KitLayoutCheck.NearestMeasuredTurn(f.angle);
            var (cos, sin) = (Math.Cos(yaw * Math.PI / 180d), Math.Sin(yaw * Math.PI / 180d));
            _frameWorld = (px, py) => new Vector3(
                (float)(f.origin.x + (px * cos) - (py * sin)) - 500f, 0f, 500f - (float)(f.origin.y + (px * sin) + (py * cos)));
            _layout = FrameLayout(f);

            _root = new GameObject("Blocks").transform;
            _people = Group("People");
            _gardens = Group("Gardens");
            // Every stone work of this district writes its mesh into the district's own folder, never into C5's.
            _worksFolder = $"{IslandWorksFolder}/{f.id}";
            _works = new Works(Group("Stone Works"), true);

            var ran = new List<string>();
            void Step(string name, Action step)
            {
                try
                {
                    step();
                    ran.Add(name);
                }
                catch (Exception e)
                {
                    ran.Add($"{name} FAILED ({e.GetType().Name}: {e.Message.Split('\n')[0]})");
                    Debug.LogException(e);
                }
            }

            Step("streets", Streets);
            Step("paint", () => PaintFrame(maps, paving));
            _pavements = new VeilmouthPavements(_lanes.Where(l => l.Pavement).Select(l => l.Quad),
                _lanes.Where(l => l.Road).Select(l => (l.Street, l.Quad)), BareGround);
            _parent = Group("Civic Buildings");
            Step("buildings", Buildings);
            _parent = Group("Houses");
            Step("houses", Houses);
            Step("passages' backs", () =>
            {
                foreach (var b in _backs)
                {
                    PlaceBack(b.F, b.Front, b.A0, b.A1, b.Depth);
                }

                _backs.Clear();
            });
            Step("back doors", BackDoors);
            // C5's order (Run). A step that belongs to one place runs only in the district that has it.
            _parent = Group("Districts");
            Step("masses", Masses);
            if (_layout.plaza.half > 0f)
            {
                Step("statue", Statue);
            }

            if (_layout.squares.Any(s => s.id == "deaf_boiler") && _layout.boxes.Any(b => b.id == "ES2_e"))
            {
                Step("cellar door", CellarDoor);
                Step("works' dress", () => WorksDressOf(_root.Find(WorksGroup)));
            }

            Step("cemetery", Cemetery);
            Step("market", Market);
            _parent = Group("Ritual Circles");
            Step("ritual circles", () =>
            {
                foreach (var c in _layout.candidates)
                {
                    Circle(World(c.x, c.y, Up(Ground(c.x, c.y))), c.name);
                }
            });
            var lamps = Group("Lamps");
            _parent = lamps;
            Step("lamps", LampsC4);
            _parent = Group("Street Trees");
            Step("street trees", StreetTreesC4);
            _parent = lamps;
            Step("square lamps", SquareLamps);
            _parent = Group("Parks");
            Step("parks", Parks);
            _parent = Group("Yards");
            Step("yards", Yards);
            Step("stone works", () => _works.Finish(WallStone));
            Step("pavements", () => _pavements.Build(_root, _houses, _worksFolder));
            _worksFolder = SliceKitCatalogue.Folder + "/Works";

            // To its place: the frame on its whole-metre node with its turn; the district under it, its plan point
            // (u, v) at the frame's local (u, 0, -v).
            var frame = new GameObject(f.name);
            frame.AddComponent<KitFrame>();
            frame.transform.SetParent(city, false);
            frame.transform.SetPositionAndRotation(new Vector3(f.origin.x - 500f, 0f, 500f - f.origin.y), Quaternion.Euler(0f, yaw, 0f));
            _root.SetParent(frame.transform, false);
            _root.localPosition = new Vector3(500f, 0f, -500f);
            _root.localRotation = Quaternion.identity;
            _frameWorld = null;

            report.AppendLine($"{f.name} at ({f.origin.x:0}, {f.origin.y:0}), {yaw:0.###} deg: {_houses.Count} footprints, {_doors.Count} doors, {_lanes.Count} lanes, " +
                              $"{_passages} passages; rows on one level: raised {_terraceLift:0.00} m at most, {_terraceCuts} runs cut");
            report.AppendLine("   " + string.Join("; ", ran));
        }

        /// <summary>
        /// A district's layout as C5's steps read one. The frame's own rectangle stands for the city inside its walls,
        /// so the ground under it is paved and built on; it has no works, river or sea of its own, and the squares
        /// that reach out over the water (the piers) wait for the stage that builds the waterside.
        /// </summary>
        private static VeilmouthLayout FrameLayout(IslandFrame f)
        {
            VeilmouthLayout.WallSpec Wall(string name, string axis, float t0, float t1, float a0, float a1) =>
                new VeilmouthLayout.WallSpec { name = name, axis = axis, t0 = t0, t1 = t1, a0 = a0, a1 = a1, height = 0f, gaps = Array.Empty<VeilmouthLayout.Gap>() };

            return new VeilmouthLayout
            {
                version = "I1",
                streets = f.streets,
                squares = (f.squares ?? Array.Empty<VeilmouthLayout.Square>()).Where(s => s.y0 >= 0f).ToArray(),
                rows = Array.Empty<VeilmouthLayout.RowSpec>(),
                plots = Array.Empty<VeilmouthLayout.PlotSpec>(),
                buildings = f.buildings ?? Array.Empty<VeilmouthLayout.BuildingSpec>(),
                boxes = f.boxes ?? Array.Empty<VeilmouthLayout.BoxSpec>(),
                water = new[] { new VeilmouthLayout.Water { name = "the frame's far side", kind = "sea", x0 = f.U, y0 = 0f, x1 = f.U + 1f, y1 = f.V } },
                walls = new[]
                {
                    Wall("Town wall (west)", "y", -2f, 0f, 0f, f.V), Wall("Town wall (north)", "x", -2f, 0f, 0f, f.U),
                    Wall("Works wall (west)", "y", f.U, f.U, 0f, 0f), Wall("Works wall (south)", "x", 0f, 0f, f.U, f.U),
                },
                towers = Array.Empty<VeilmouthLayout.TowerSpec>(),
                bridges = Array.Empty<VeilmouthLayout.BridgeSpec>(),
                slants = Array.Empty<VeilmouthLayout.SlantSpec>(),
                districts = Array.Empty<VeilmouthLayout.District>(),
                candidates = f.candidates ?? Array.Empty<VeilmouthLayout.Candidate>(),
                blocks = f.blocks ?? Array.Empty<VeilmouthLayout.BlockSpec>(),
                passages = f.passages ?? Array.Empty<VeilmouthLayout.PassageSpec>(),
                roads = Array.Empty<VeilmouthLayout.Area>(),
                plaza = f.plaza ?? new VeilmouthLayout.PlazaSpec(),
                cottages = Array.Empty<VeilmouthLayout.Cottage>(),
                forest = new VeilmouthLayout.ForestSpec { rects = Array.Empty<VeilmouthLayout.Area>() },
                spawn = new VeilmouthLayout.Point(),
            };
        }

        /// <summary>
        /// The island terrain's layers the city paints with: turf, bare earth, and the city's own setts and cobbles,
        /// which the island's material never reads stochastically (owner, 2026-11-14).
        /// </summary>
        private static (int Grass, int Earth, int Setts, int Cobbles) PavingLayers(TerrainData data)
        {
            int Find(string part)
            {
                var index = Array.FindIndex(data.terrainLayers, l => l != null && l.name.Contains(part));
                if (index < 0)
                {
                    throw new InvalidOperationException($"the island's terrain has no layer '{part}': build the lab with the city's surfaces first");
                }

                return index;
            }

            return (Find("grass_ground"), Find("dirt_floor"), Find("Slice_Setts"), Find("Slice_Cobbles"));
        }

        /// <summary>
        /// The district's lanes painted where it will lie, in C5's order (every carriageway last): each lane's quad is
        /// carried from the district's metres to the plan's, and the terrain's texels inside it take the lane's surface.
        /// </summary>
        private static void PaintFrame(float[,,] maps, (int Grass, int Earth, int Setts, int Cobbles) paving)
        {
            var size = maps.GetLength(0);
            var layers = maps.GetLength(2);
            var cell = VeilmouthScene.Extent / size;
            foreach (var lane in _lanes.Where(l => !l.Road).Concat(_lanes.Where(l => l.Road)))
            {
                var layer = lane.Layer == Grass ? paving.Grass : lane.Layer == YardEarth ? paving.Earth : lane.Layer == Cobbles ? paving.Cobbles : paving.Setts;
                var quad = lane.Quad.Select(p =>
                {
                    var w = _frameWorld(p.x, p.y);
                    return new Vector2(w.x + 500f, 500f - w.z);
                }).ToArray();
                var x0 = Mathf.Max(0, Mathf.FloorToInt(quad.Min(p => p.x) / cell) - 1);
                var x1 = Mathf.Min(size - 1, Mathf.CeilToInt(quad.Max(p => p.x) / cell) + 1);
                var z0 = Mathf.Max(0, Mathf.FloorToInt((VeilmouthScene.Extent - quad.Max(p => p.y)) / cell) - 1);
                var z1 = Mathf.Min(size - 1, Mathf.CeilToInt((VeilmouthScene.Extent - quad.Min(p => p.y)) / cell) + 1);
                for (var z = z0; z <= z1; z++)
                {
                    for (var x = x0; x <= x1; x++)
                    {
                        if (!Inside(quad, new Vector2((x + 0.5f) * cell, VeilmouthScene.Extent - ((z + 0.5f) * cell))))
                        {
                            continue;
                        }

                        for (var l = 0; l < layers; l++)
                        {
                            maps[z, x, l] = l == layer ? 1f : 0f;
                        }
                    }
                }
            }
        }
    }
}
