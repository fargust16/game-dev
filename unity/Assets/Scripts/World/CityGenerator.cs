using System.Collections.Generic;
using GameDev.Environment;
using GameDev.Grid;
using GameDev.Input;
using GameDev.Interaction;
using GameDev.Player;
using GameDev.World.Town;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameDev.World
{
    /// <summary>
    /// Organic historic town orchestrator: growth graph → parcels → slice anchors → systems.
    /// Design: 13 City Generation.
    /// </summary>
    public sealed class CityGenerator : MonoBehaviour
    {
        private const string PrefabFolder = "Assets/Prefabs/S0_Greybox/";

        [SerializeField] private WorldSeed worldSeed = new WorldSeed(WorldSeed.DefaultPrototypeSeed);
        [SerializeField] private float cellSize = 1f;

        [Header("S0 Greybox Prefabs")]
        [SerializeField] private GameObject floorTilePrefab;
        [SerializeField] private GameObject landmarkBlockPrefab;
        [SerializeField] private GameObject poiShellPrefab;
        [SerializeField] private GameObject envActionStubPrefab;
        [SerializeField] private GameObject building1x1Prefab;
        [SerializeField] private GameObject building2x2Prefab;
        [SerializeField] private GameObject wallSegmentPrefab;
        [SerializeField] private GameObject noticeBoardPrefab;
        [SerializeField] private GameObject playerCapsulePrefab;

        private TownRoadGraph _graph;
        private readonly HashSet<long> _reserved = new HashSet<long>();
        private readonly List<GridCoord> _pathScratch = new List<GridCoord>(256);

        public int SeedValue => worldSeed != null ? worldSeed.value : WorldSeed.DefaultPrototypeSeed;

        public void SetSeed(int seed)
        {
            if (worldSeed == null)
            {
                worldSeed = new WorldSeed(seed);
            }
            else
            {
                worldSeed.value = seed;
            }
        }

        public void Generate()
        {
            ResolvePrefabs();
            ClearChildren();
            _reserved.Clear();

            var roadsRng = worldSeed.Fork("roads");
            var blocksRng = worldSeed.Fork("blocks");
            var parcelsRng = worldSeed.Fork("parcels");
            var anchorsRng = worldSeed.Fork("anchors");
            var dressingRng = worldSeed.Fork("dressing");

            _graph = OrganicRoadBuilder.Build(roadsRng);

            var gridGo = new GameObject("GridMap");
            gridGo.transform.SetParent(transform, false);
            var grid = gridGo.AddComponent<GridMap>();
            grid.Configure(TownGrowthParams.MapWidth, TownGrowthParams.MapHeight, cellSize, Vector3.zero);
            gridGo.AddComponent<GridDebugDrawer>();

            TownBlockParcelizer.ApplyRoadsToGrid(_graph, grid);
            // blocks stream reserved for future block IDs; parcels consume frontage now.
            _ = blocksRng;
            TownBlockParcelizer.PlaceFactory(_graph, grid, transform, building2x2Prefab, building1x1Prefab,
                wallSegmentPrefab);
            TownBlockParcelizer.PlaceFrontageBuildings(_graph, grid, transform, parcelsRng, building1x1Prefab,
                building2x2Prefab);

            // Re-apply street walkability after factory collar.
            for (var y = 0; y < _graph.Height; y++)
            {
                for (var x = 0; x < _graph.Width; x++)
                {
                    if (!_graph.IsStreet(x, y))
                    {
                        continue;
                    }

                    var c = new GridCoord(x, y);
                    grid.SetFlags(c, GridCellFlags.Walkable, walkable: true);
                    grid.SetHeight(c, _graph.HeightOf(c));
                }
            }

            BuildRoadFloorStrips(grid);
            var anchors = PlaceMandatoryAnchors(grid, anchorsRng);
            EnsureAllAnchorsPathToMarket(grid, anchors);
            PlaceDressing(grid, dressingRng, anchors);
            PlaceEnvActions(grid, anchors);
            SpawnPlayerAndCamera(grid, anchors.Spawn);
            BuildLighting(anchors);
            BuildSystems();

            Debug.Log(
                $"[City] Organic town seed={SeedValue}. Center=({_graph.Center.x},{_graph.Center.y}) " +
                $"radius={_graph.TownRadius}. Market hub + slice anchors ready.");
        }

        private struct AnchorSet
        {
            public GridCoord Spawn;
            public GridCoord Tavern;
            public GridCoord Tincture;
            public GridCoord Loom;
            public GridCoord Stairs;
            public GridCoord Factory;
            public GridCoord Warden;
            public GridCoord Veinmarket;
            public GridCoord Doctor;
            public GridCoord Market;
        }

        private AnchorSet PlaceMandatoryAnchors(GridMap grid, System.Random rng)
        {
            var set = new AnchorSet();
            set.Market = _graph.Center;
            set.Tavern = SnapInDistrict(grid, _graph.Center, CityDistrict.LowerVeil, preferPlaza: true, rng);
            Reserve(set.Tavern);

            set.Spawn = SnapNear(grid, _graph.NorthGate, CityDistrict.Ridge, preferAlley: false, rng);
            Reserve(set.Spawn);

            set.Factory = SnapNear(grid,
                new GridCoord(_graph.FactoryRectMin.x - 1, (_graph.FactoryRectMin.y + _graph.FactoryRectMax.y) / 2),
                CityDistrict.LowerVeil, preferAlley: false, rng);
            Reserve(set.Factory);

            set.Stairs = SnapNear(grid,
                new GridCoord(_graph.FactoryRectMin.x - 2, _graph.FactoryRectMin.y + 2 + rng.Next(0, 4)),
                CityDistrict.LowerVeil, preferAlley: false, rng);
            Reserve(set.Stairs);

            // Harrow ~25-45 cells along east major from plaza.
            set.Tincture = SnapAlongMajorToward(grid, _graph.EastFactory, 28 + rng.Next(0, 16),
                CityDistrict.LowerVeil, rng);
            Reserve(set.Tincture);

            set.Loom = SnapInDistrict(grid,
                LerpCoord(set.Tincture, set.Factory, 0.45f + (float)rng.NextDouble() * 0.2f),
                CityDistrict.LowerVeil, preferPlaza: false, rng, preferAlley: true);
            Reserve(set.Loom);

            set.Warden = SnapInDistrict(grid,
                LerpCoord(set.Spawn, set.Tavern, 0.55f),
                CityDistrict.Ridge, preferPlaza: false, rng);
            Reserve(set.Warden);

            set.Veinmarket = SnapInDistrict(grid, _graph.SouthGate, CityDistrict.Undercroft, false, rng, true);
            Reserve(set.Veinmarket);

            set.Doctor = SnapInDistrict(grid,
                new GridCoord(_graph.SouthGate.x + rng.Next(-12, 13), _graph.SouthGate.y + rng.Next(-4, 8)),
                CityDistrict.Undercroft, false, rng, true);
            Reserve(set.Doctor);

            var ridge = CreateLandmark(grid, LandmarkId.RidgeApproach, "Ridge Approach", set.Spawn);
            AddWorldLabel(ridge, "Ridge Approach");

            var tavern = CreateLandmark(grid, LandmarkId.SootAndBell, "Soot & Bell Tavern", set.Tavern);
            AddWorldLabel(tavern, "Soot & Bell");

            var boardCoord = NeighborStreet(set.Tavern, 1, 0) ?? NeighborStreet(set.Tavern, -1, 0) ??
                             EnsureStreet(grid, new GridCoord(set.Tavern.x + 1, set.Tavern.y));
            Reserve(boardCoord);
            var board = SpawnPrefab(noticeBoardPrefab, grid, boardCoord, "Notice Board");
            EnsureLandmark(board, LandmarkId.NoticeBoard, "Notice Board");
            EnsureInteract(board, "Read notice board",
                "Pinned: 1) Warden seizure — Harrow & Tincture before second-shift bell. 2) Missing persons — Loom Lane. 3) Coal price notice.");
            AddTipGiver(board,
                hints: new[] { PoiShellId.PoiB_EleventhBellCollectors },
                marks: new[] { PoiShellId.PoiA_HarrowArrest },
                summary: "Board marks Harrow seizure and hints trouble on Loom Lane.");
            AddWorldLabel(board, "Notice Board");

            var merrinCoord = NeighborStreet(set.Tavern, 0, -1) ?? NeighborStreet(set.Tavern, 0, 1) ??
                              EnsureStreet(grid, new GridCoord(set.Tavern.x, set.Tavern.y - 1));
            Reserve(merrinCoord);
            var tavernTalk = CreateLandmark(grid, LandmarkId.SootAndBell, "Bartender Merrin", merrinCoord);
            tavernTalk.name = "Bartender_Merrin";
            EnsureInteract(tavernTalk, "Talk to Merrin",
                "Merrin: Brass-numbers are taking old Harrow tonight. After the eleventh bell they take mill girls off Loom Lane. Check the board.");
            AddTipGiver(tavernTalk,
                hints: new[] { PoiShellId.PoiB_EleventhBellCollectors },
                marks: new[] { PoiShellId.PoiA_HarrowArrest },
                summary: "Merrin marks Harrow and hints the eleventh-bell collectors.");
            AddWorldLabel(tavernTalk, "Merrin (bar stub)");

            var tinctureRow = CreateLandmark(grid, LandmarkId.TinctureRow, "Tincture Row", set.Tincture);
            AddWorldLabel(tinctureRow, "Tincture Row");

            var harrowCoord = NeighborStreet(set.Tincture, 0, -1) ?? NeighborStreet(set.Tincture, 1, 0) ??
                              set.Tincture;
            EnsureStreet(grid, harrowCoord);
            Reserve(harrowCoord);
            var harrow = CreatePoi(grid, PoiShellId.PoiA_HarrowArrest, "Harrow & Tincture", harrowCoord,
                "Brass-Number Arrest shell. Get a tip first, then participate before the Active Window ends.",
                "Inspect POI A",
                "Brass-Number Arrest shell.");
            EnsureLandmark(harrow, LandmarkId.HarrowAndTincture, "Harrow & Tincture");
            AddWorldLabel(harrow, "POI A — Harrow");

            var loom = CreateLandmark(grid, LandmarkId.LoomLane, "Loom Lane", set.Loom);
            AddWorldLabel(loom, "Loom Lane");

            var alleyCoord = NeighborStreet(set.Loom, 1, 0) ?? NeighborStreet(set.Loom, -1, 0) ?? set.Loom;
            EnsureStreet(grid, alleyCoord);
            Reserve(alleyCoord);
            var alley = CreatePoi(grid, PoiShellId.PoiB_EleventhBellCollectors, "Alley Mouth", alleyCoord,
                "Eleventh-Bell Collectors shell. Tips make it Hinted/Marked; Brief window is short.",
                "Inspect POI B",
                "Eleventh-Bell Collectors shell.");
            EnsureLandmark(alley, LandmarkId.AlleyMouth, "Alley Mouth");
            AddWorldLabel(alley, "POI B — Alley");

            var factory = CreateLandmark(grid, LandmarkId.FactoryWall, "Factory Wall", set.Factory);
            AddWorldLabel(factory, "Factory Wall");

            var stairs = CreatePoi(grid, PoiShellId.PoiC_SootStairTrace, "Coal Stairs", set.Stairs,
                "Soot Stair Trace. Unlocks after POI A or B leaves Aftermath.",
                "Inspect POI C",
                "Soot Stair Trace shell.");
            EnsureLandmark(stairs, LandmarkId.CoalStairs, "Coal Stairs");
            AddWorldLabel(stairs, "POI C — Stairs");

            var warden = CreateLandmark(grid, LandmarkId.WardenPost, "Warden Post", set.Warden);
            EnsureInteract(warden, "Watch the post",
                "Brass-numbers drill at the edge of Lower Veil. A garrison stub — full Warden AI later.");
            AddWorldLabel(warden, "Warden Post");
            var guardCoord = NeighborStreet(set.Warden, 1, 0) ?? set.Warden;
            EnsureStreet(grid, guardCoord);
            Reserve(guardCoord);
            var guard = CreateLandmark(grid, LandmarkId.WardenPost, "Warden Guard Stub", guardCoord);
            guard.name = "NPC_WardenGuard";
            EnsureInteract(guard, "Talk to guard",
                "Guard: Papers if you linger. Ridge is quiet; the Veil is not.");
            AddWorldLabel(guard, "Guard (stub)");

            var vein = CreateLandmark(grid, LandmarkId.VeinmarketAlley, "Veinmarket Alley", set.Veinmarket);
            EnsureInteract(vein, "Study the door",
                "A dark underworld door stub. Veinmarket fixers keep hours the Wardens pretend not to know.");
            AddWorldLabel(vein, "Veinmarket");
            var fixerCoord = NeighborStreet(set.Veinmarket, 0, 1) ?? set.Veinmarket;
            EnsureStreet(grid, fixerCoord);
            Reserve(fixerCoord);
            var fixer = CreateLandmark(grid, LandmarkId.VeinmarketAlley, "Fixer Stub", fixerCoord);
            fixer.name = "NPC_VeinmarketFixer";
            EnsureInteract(fixer, "Talk to fixer",
                "Fixer: Coin first. Names later. Undercroft remembers who pays.");
            AddWorldLabel(fixer, "Fixer (stub)");

            var doctor = CreateLandmark(grid, LandmarkId.UnlicensedDoctor, "Unlicensed Doctor", set.Doctor);
            EnsureInteract(doctor, "Knock",
                "Optional tip source stub — stitches and whispers for those who find the alley.");
            AddTipGiver(doctor,
                hints: new[] { PoiShellId.PoiA_HarrowArrest },
                marks: System.Array.Empty<PoiShellId>(),
                summary: "Doctor hints the Harrow seizure if you ask the right way.");
            AddWorldLabel(doctor, "Doctor (stub)");

            return set;
        }

        private void EnsureAllAnchorsPathToMarket(GridMap grid, AnchorSet anchors)
        {
            var targets = new[]
            {
                anchors.Spawn, anchors.Tavern, anchors.Tincture, anchors.Loom, anchors.Stairs,
                anchors.Factory, anchors.Warden, anchors.Veinmarket, anchors.Doctor
            };

            foreach (var t in targets)
            {
                if (GridPathfinder.TryFindPath(grid, t, anchors.Market, _pathScratch))
                {
                    continue;
                }

                OrganicRoadBuilder.EnsureConnected(_graph, t, anchors.Market);
                // Paint corrective alley onto grid.
                PaintGraphStreets(grid);
                if (!GridPathfinder.TryFindPath(grid, t, anchors.Market, _pathScratch))
                {
                    // Force a second thicker connect.
                    OrganicRoadBuilder.EnsureConnected(_graph, t, anchors.Tavern);
                    PaintGraphStreets(grid);
                }
            }
        }

        private void PaintGraphStreets(GridMap grid)
        {
            for (var y = 0; y < _graph.Height; y++)
            {
                for (var x = 0; x < _graph.Width; x++)
                {
                    if (!_graph.IsStreet(x, y))
                    {
                        continue;
                    }

                    var c = new GridCoord(x, y);
                    grid.SetFlags(c, GridCellFlags.Walkable, walkable: true);
                    grid.SetHeight(c, _graph.HeightOf(c));
                }
            }
        }

        private void PlaceDressing(GridMap grid, System.Random rng, AnchorSet anchors)
        {
            var ruskCoord = NeighborStreet(anchors.Tincture, 2, 0) ??
                            SnapNear(grid, new GridCoord(anchors.Tincture.x + 2, anchors.Tincture.y),
                                CityDistrict.LowerVeil, false, rng);
            Reserve(ruskCoord);
            var coalHauler = CreateLandmark(grid, LandmarkId.TinctureRow, "Coal Hauler Rusk", ruskCoord);
            coalHauler.name = "NPC_CoalHauler_Rusk";
            EnsureInteract(coalHauler, "Talk to Rusk",
                "Rusk: Keep your boots off the wet coal. Wardens are thick on Tincture tonight — and Loom Lane after the eleventh bell.");
            AddTipGiver(coalHauler,
                hints: new[] { PoiShellId.PoiA_HarrowArrest, PoiShellId.PoiB_EleventhBellCollectors },
                marks: System.Array.Empty<PoiShellId>(),
                summary: "Rusk hints both the seizure street and Loom Lane.");
            AddWorldLabel(coalHauler, "Rusk (street)");

            var niaCoord = NeighborStreet(anchors.Loom, 0, -1) ?? anchors.Loom;
            EnsureStreet(grid, niaCoord);
            Reserve(niaCoord);
            var millGirl = CreateLandmark(grid, LandmarkId.LoomLane, "Mill Girl Nia", niaCoord);
            millGirl.name = "NPC_MillGirl_Nia";
            EnsureInteract(millGirl, "Talk to Nia",
                "Nia: They took two girls last week from this lane. If you hear coach-wheels after the eleventh bell — don't look helpful. Look small.");
            AddTipGiver(millGirl,
                hints: System.Array.Empty<PoiShellId>(),
                marks: new[] { PoiShellId.PoiB_EleventhBellCollectors },
                summary: "Nia marks the eleventh-bell collectors on Loom Lane.");
            AddWorldLabel(millGirl, "Nia (street)");

            PlaceWaymarker(grid, MidpointStreet(anchors.Spawn, anchors.Tavern),
                "South toward the market — Soot & Bell.");
            PlaceWaymarker(grid, MidpointStreet(anchors.Tavern, anchors.Tincture),
                "Tincture Row — Harrow & Tincture ahead.");
            PlaceWaymarker(grid, MidpointStreet(anchors.Tincture, anchors.Loom),
                "Crooked lanes toward Loom and the factory.");
            PlaceWaymarker(grid, MidpointStreet(anchors.Loom, anchors.Stairs),
                "Coal Stairs under the factory wall.");

            // Wells / stubs at a few junctions.
            var wellCount = Mathf.Min(5, _graph.Junctions.Count);
            for (var i = 0; i < wellCount; i++)
            {
                var j = _graph.Junctions[rng.Next(0, _graph.Junctions.Count)];
                if (_reserved.Contains(Key(j)))
                {
                    continue;
                }

                Reserve(j);
                var well = CreateLandmark(grid, LandmarkId.TinctureRow, "Well", j);
                well.name = $"Well_{i}";
                EnsureInteract(well, "Drink from well", "Brackish but cold. A small plaza around the well.");
                AddWorldLabel(well, "Well");
            }
        }

        private void PlaceWaymarker(GridMap grid, GridCoord coord, string message)
        {
            if (_reserved.Contains(Key(coord)))
            {
                coord = SnapNear(grid, coord, _graph.DistrictOf(coord), false,
                    new System.Random(SeedValue ^ Key(coord).GetHashCode()));
            }

            Reserve(coord);
            var go = SpawnPrefab(landmarkBlockPrefab, grid, coord, "Waymarker");
            go.transform.localScale = Vector3.Scale(go.transform.localScale, new Vector3(0.55f, 0.45f, 0.55f));
            EnsureInteract(go, "Read waymarker", message);
            AddWorldLabel(go, "→");
        }

        private void PlaceEnvActions(GridMap grid, AnchorSet anchors)
        {
            var crates = NeighborStreet(anchors.Tincture, 3, 0) ?? anchors.Tincture;
            CreateEnvAction(grid, crates, "Vault Crates",
                EnvironmentActionType.VaultJump, 0.75f,
                "Vaulted the crates on Tincture Row.",
                "Slipped on wet cobble — noise, wasted effort.",
                GridCellFlags.Walkable | GridCellFlags.Vaultable);

            var window = NeighborStreet(anchors.Tincture, -2, -1) ?? anchors.Tincture;
            CreateEnvAction(grid, window, "Side Window",
                EnvironmentActionType.ClimbThroughWindow, 0.65f,
                "Climbed through Harrow's side window.",
                "Boot scraped the frame — spotted / noisy.",
                GridCellFlags.Walkable | GridCellFlags.Window);

            var ledge = NeighborStreet(anchors.Stairs, -1, 0) ??
                        NeighborStreet(anchors.Stairs, 0, 1) ??
                        new GridCoord(anchors.Stairs.x - 1, anchors.Stairs.y);
            CreateEnvAction(grid, ledge, "Coal Ledge",
                EnvironmentActionType.ClimbUp, 0.7f,
                "Mantled the coal-stair ledge.",
                "Lost grip on sooty brick — drop, Hurt stub.",
                GridCellFlags.Walkable | GridCellFlags.Climbable | GridCellFlags.Stairs);
        }

        private GridCoord SnapInDistrict(
            GridMap grid,
            GridCoord hint,
            CityDistrict district,
            bool preferPlaza,
            System.Random rng,
            bool preferAlley = false)
        {
            var best = hint;
            var bestScore = int.MinValue;
            for (var attempt = 0; attempt < 80; attempt++)
            {
                var x = Mathf.Clamp(hint.x + rng.Next(-14, 15), 2, _graph.Width - 3);
                var y = Mathf.Clamp(hint.y + rng.Next(-14, 15), 2, _graph.Height - 3);
                if (!_graph.IsStreet(x, y))
                {
                    continue;
                }

                var c = new GridCoord(x, y);
                if (_graph.DistrictOf(c) != district && attempt < 50)
                {
                    continue;
                }

                if (_reserved.Contains(Key(c)))
                {
                    continue;
                }

                var score = 100 - Mathf.Abs(x - hint.x) - Mathf.Abs(y - hint.y);
                if (preferPlaza && _graph.Plaza[x, y])
                {
                    score += 40;
                }

                if (preferAlley && _graph.Alley[x, y])
                {
                    score += 20;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }

            EnsureStreet(grid, best);
            return best;
        }

        private GridCoord SnapNear(GridMap grid, GridCoord hint, CityDistrict district, bool preferAlley,
            System.Random rng)
        {
            return SnapInDistrict(grid, hint, district, false, rng, preferAlley);
        }

        private GridCoord SnapAlongMajorToward(
            GridMap grid,
            GridCoord toward,
            int targetDist,
            CityDistrict district,
            System.Random rng)
        {
            GridCoord best = _graph.Center;
            var bestDelta = int.MaxValue;
            foreach (var p in _graph.MajorSamples)
            {
                if (_graph.DistrictOf(p) != district)
                {
                    continue;
                }

                var d = Mathf.Abs(p.x - _graph.Center.x) + Mathf.Abs(p.y - _graph.Center.y);
                var towardBias = Mathf.Abs(p.x - toward.x) + Mathf.Abs(p.y - toward.y);
                var delta = Mathf.Abs(d - targetDist) + towardBias / 4;
                if (delta < bestDelta && !_reserved.Contains(Key(p)))
                {
                    bestDelta = delta;
                    best = p;
                }
            }

            // Jitter off the major into a side cell.
            if (rng.Next(0, 2) == 0)
            {
                var side = new GridCoord(best.x + rng.Next(-1, 2), best.y + rng.Next(-1, 2));
                if (_graph.IsStreet(side))
                {
                    best = side;
                }
            }

            EnsureStreet(grid, best);
            return best;
        }

        private GridCoord MidpointStreet(GridCoord a, GridCoord b)
        {
            // Graph-only snap; caller already has streets painted.
            return SnapInDistrict(null, new GridCoord((a.x + b.x) / 2, (a.y + b.y) / 2), _graph.DistrictOf(a),
                false, new System.Random(SeedValue + a.x * 17 + b.y * 31), false);
        }

        private static GridCoord LerpCoord(GridCoord a, GridCoord b, float t)
        {
            return new GridCoord(
                Mathf.RoundToInt(Mathf.Lerp(a.x, b.x, t)),
                Mathf.RoundToInt(Mathf.Lerp(a.y, b.y, t)));
        }

        private GridCoord? NeighborStreet(GridCoord origin, int dx, int dy)
        {
            var c = new GridCoord(origin.x + dx, origin.y + dy);
            if (_graph != null && _graph.IsStreet(c))
            {
                return c;
            }

            return null;
        }

        private GridCoord EnsureStreet(GridMap grid, GridCoord c)
        {
            c = new GridCoord(
                Mathf.Clamp(c.x, 1, TownGrowthParams.MapWidth - 2),
                Mathf.Clamp(c.y, 1, TownGrowthParams.MapHeight - 2));
            _graph.MarkStreet(c, TownGrowthParams.AlleyWidth, alley: false);
            if (grid != null)
            {
                grid.SetFlags(c, GridCellFlags.Walkable, walkable: true);
                grid.SetHeight(c, _graph.HeightOf(c));
            }

            return c;
        }

        private void Reserve(GridCoord c) => _reserved.Add(Key(c));

        private static long Key(GridCoord c) => ((long)c.y << 32) ^ (uint)c.x;

        private void SpawnPlayerAndCamera(GridMap grid, GridCoord spawn)
        {
            var player = SpawnPrefab(playerCapsulePrefab, grid, spawn, "Player");
            DisableBlockingColliders(player);
            var playerCtrl = player.GetComponent<GridPlayerController>();
            if (playerCtrl == null)
            {
                playerCtrl = player.AddComponent<GridPlayerController>();
            }

            playerCtrl.PlaceOnGrid(grid, spawn);

            var camGo = new GameObject("TacticalCamera");
            camGo.transform.SetParent(transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            var tacticalCam = camGo.AddComponent<SimpleFollowCamera>();
            tacticalCam.SetTarget(player.transform);
        }

        private void BuildSystems()
        {
            var systems = new GameObject("City_Systems");
            systems.transform.SetParent(transform, false);
            systems.AddComponent<WorldClock>();
            systems.AddComponent<DistrictResidue>();
            systems.AddComponent<PoiDirector>();
            systems.AddComponent<GameDev.Decisions.DecisionNodeSession>();
            GameInputService.EnsureExists();
        }

        private void BuildRoadFloorStrips(GridMap grid)
        {
            var floorRoot = new GameObject("FloorStrips");
            floorRoot.transform.SetParent(transform, false);

            for (var y = 0; y < _graph.Height; y++)
            {
                var x = 0;
                while (x < _graph.Width)
                {
                    if (!_graph.IsStreet(x, y))
                    {
                        x++;
                        continue;
                    }

                    var start = x;
                    while (x < _graph.Width && _graph.IsStreet(x, y))
                    {
                        x++;
                    }

                    SpawnFloorStrip(floorRoot.transform, grid, start, y, x - 1, y);
                }
            }
        }

        private void SpawnFloorStrip(Transform parent, GridMap grid, int x0, int y0, int x1, int y1)
        {
            var a = grid.CoordToWorldCenter(new GridCoord(x0, y0));
            var b = grid.CoordToWorldCenter(new GridCoord(x1, y1));
            var center = (a + b) * 0.5f;
            var h = grid.TryGetCell(new GridCoord(x0, y0), out var cell) ? cell.height * grid.CellSize : 0f;
            center.y = h;

            GameObject strip;
            if (floorTilePrefab != null)
            {
#if UNITY_EDITOR
                strip = (GameObject)PrefabUtility.InstantiatePrefab(floorTilePrefab, parent);
#else
                strip = Object.Instantiate(floorTilePrefab, parent);
#endif
            }
            else
            {
                strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.transform.SetParent(parent, false);
            }

            strip.name = $"FloorStrip_{x0}_{y0}_{x1}_{y1}";
            strip.transform.position = center;
            var sx = Mathf.Max(1, x1 - x0 + 1) * grid.CellSize;
            var sz = Mathf.Max(1, y1 - y0 + 1) * grid.CellSize;
            strip.transform.localScale = new Vector3(sx, 0.1f, sz);
            DisableBlockingColliders(strip);
        }

        private void BuildLighting(AnchorSet anchors)
        {
            var lightingGo = new GameObject("S0_Lighting");
            lightingGo.transform.SetParent(transform, false);
            var controller = lightingGo.AddComponent<S0_LightingController>();

            var sunDay = MakeDirLight(lightingGo, "Sun_Day", new Vector3(50f, -35f, 0f),
                new Color(1.0f, 0.93f, 0.82f), 1.25f, true);
            var sunNight = MakeDirLight(lightingGo, "Sun_Night", new Vector3(28f, -55f, 0f),
                new Color(0.55f, 0.65f, 0.85f), 0.25f, true);
            var fillSkyDay = MakeDirLight(lightingGo, "Fill_Sky_Day", new Vector3(70f, 120f, 0f),
                new Color(0.55f, 0.62f, 0.72f), 0.25f, false);

            var rimFactory = new GameObject("Rim_Factory");
            rimFactory.transform.SetParent(lightingGo.transform, false);
            rimFactory.transform.position = CoordWorld(anchors.Factory) + new Vector3(3f, 3.5f, 0f);
            rimFactory.transform.rotation = Quaternion.Euler(20f, -90f, 0f);
            var lRim = rimFactory.AddComponent<Light>();
            lRim.type = LightType.Spot;
            lRim.color = new Color(0.40f, 0.65f, 0.75f);
            lRim.intensity = 1.0f;
            lRim.range = 18.0f;
            lRim.spotAngle = 70.0f;
            lRim.shadows = LightShadows.Soft;

            var warmTavern = MakePoint(lightingGo, "Warm_TavernPool",
                CoordWorld(anchors.Tavern) + Vector3.up * 2.5f,
                new Color(1.0f, 0.72f, 0.35f), 0.55f, 10f);
            var coolLoom = MakePoint(lightingGo, "Cool_LoomPool",
                CoordWorld(anchors.Loom) + Vector3.up * 2.5f,
                new Color(0.40f, 0.60f, 0.85f), 0.45f, 10f);

            var gasLamps = new GameObject("Night_GasLamps");
            gasLamps.transform.SetParent(lightingGo.transform, false);
            CreateGasLamp(gasLamps, "GasLamp_Market", CoordWorld(anchors.Market) + Vector3.up * 2.5f);
            CreateGasLamp(gasLamps, "GasLamp_Tavern", CoordWorld(anchors.Tavern) + new Vector3(1f, 2.5f, 1f));
            CreateGasLamp(gasLamps, "GasLamp_Tincture", CoordWorld(anchors.Tincture) + Vector3.up * 2.5f);
            CreateGasLamp(gasLamps, "GasLamp_Loom", CoordWorld(anchors.Loom) + Vector3.up * 2.5f);
            CreateGasLamp(gasLamps, "GasLamp_Stairs", CoordWorld(anchors.Stairs) + Vector3.up * 2.5f);

            controller.Bind(sunDay, sunNight, fillSkyDay, rimFactory, warmTavern, coolLoom, gasLamps);
        }

        private Vector3 CoordWorld(GridCoord c) =>
            new Vector3((c.x + 0.5f) * cellSize, 0f, (c.y + 0.5f) * cellSize);

        private static GameObject MakeDirLight(GameObject parent, string name, Vector3 euler, Color color,
            float intensity, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.rotation = Quaternion.Euler(euler);
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = color;
            l.intensity = intensity;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return go;
        }

        private static GameObject MakePoint(GameObject parent, string name, Vector3 pos, Color color,
            float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.Soft;
            return go;
        }

        private static void CreateGasLamp(GameObject parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.65f, 0.25f);
            l.intensity = 1.8f;
            l.range = 7.5f;
            l.shadows = LightShadows.Soft;
        }

        private void AddWorldLabel(GameObject host, string text)
        {
            var existing = host.transform.Find("WorldLabel");
            if (existing != null)
            {
                SafeDestroy(existing.gameObject);
            }

            var label = new GameObject("WorldLabel");
            label.transform.SetParent(host.transform, false);
            label.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            var tm = label.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 48;
            tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.95f, 0.92f, 0.8f, 1f);
            label.AddComponent<BillboardLabel>();
        }

        private void ResolvePrefabs()
        {
#if UNITY_EDITOR
            floorTilePrefab = LoadIfNull(floorTilePrefab, "Floor_Tile_1x1.prefab");
            landmarkBlockPrefab = LoadIfNull(landmarkBlockPrefab, "Landmark_Block.prefab");
            poiShellPrefab = LoadIfNull(poiShellPrefab, "POI_Shell.prefab");
            envActionStubPrefab = LoadIfNull(envActionStubPrefab, "EnvAction_Stub.prefab");
            building1x1Prefab = LoadIfNull(building1x1Prefab, "Building_Shell_1x1.prefab");
            building2x2Prefab = LoadIfNull(building2x2Prefab, "Building_Shell_2x2.prefab");
            wallSegmentPrefab = LoadIfNull(wallSegmentPrefab, "Wall_Segment_1x3.prefab");
            noticeBoardPrefab = LoadIfNull(noticeBoardPrefab, "Prop_NoticeBoard.prefab");
            playerCapsulePrefab = LoadIfNull(playerCapsulePrefab, "Player_Capsule.prefab");
#endif
        }

#if UNITY_EDITOR
        private static GameObject LoadIfNull(GameObject current, string fileName)
        {
            if (current != null)
            {
                return current;
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + fileName);
        }
#endif

        private GameObject CreateLandmark(GridMap grid, LandmarkId id, string label, GridCoord coord)
        {
            var go = SpawnPrefab(landmarkBlockPrefab, grid, coord, label);
            EnsureLandmark(go, id, label);
            return go;
        }

        private GameObject CreatePoi(
            GridMap grid,
            PoiShellId poiId,
            string label,
            GridCoord coord,
            string poiMessage,
            string interactPrompt,
            string interactMessage)
        {
            var go = SpawnPrefab(poiShellPrefab, grid, coord, label);
            EnsurePoi(go, poiId, poiMessage);
            EnsureInteract(go, interactPrompt, interactMessage);
            return go;
        }

        private void CreateEnvAction(
            GridMap grid,
            GridCoord coord,
            string label,
            EnvironmentActionType type,
            float chance,
            string ok,
            string fail,
            GridCellFlags flags)
        {
            EnsureStreet(grid, coord);
            var go = SpawnPrefab(envActionStubPrefab, grid, coord, label);
            var marker = go.GetComponent<EnvironmentActionMarker>();
            if (marker == null)
            {
                marker = go.AddComponent<EnvironmentActionMarker>();
            }

            marker.Setup(type, chance, ok, fail);
            grid.SetFlags(coord, flags);
        }

        private GameObject SpawnPrefab(GameObject prefab, GridMap grid, GridCoord coord, string label)
        {
            GameObject go;
            if (prefab != null)
            {
#if UNITY_EDITOR
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
#else
                go = Object.Instantiate(prefab, transform);
#endif
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetParent(transform, false);
                Debug.LogWarning($"[City] Missing prefab for '{label}'. Using primitive fallback.");
            }

            go.name = label;
            var pos = grid.CoordToWorldCenter(coord);
            pos.y = grid.TryGetCell(coord, out var cell) ? cell.height * grid.CellSize : 0f;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            return go;
        }

        private static void AddTipGiver(GameObject go, PoiShellId[] hints, PoiShellId[] marks, string summary)
        {
            var tip = go.GetComponent<TipGiver>();
            if (tip == null)
            {
                tip = go.AddComponent<TipGiver>();
            }

            tip.Setup(hints, marks, summary);
        }

        private static void EnsureLandmark(GameObject go, LandmarkId id, string label)
        {
            var marker = go.GetComponent<LandmarkMarker>();
            if (marker == null)
            {
                marker = go.AddComponent<LandmarkMarker>();
            }

            marker.Setup(id, label);
        }

        private static void EnsurePoi(GameObject go, PoiShellId id, string message)
        {
            var marker = go.GetComponent<PoiShellMarker>();
            if (marker == null)
            {
                marker = go.AddComponent<PoiShellMarker>();
            }

            marker.Setup(id, message);
        }

        private static void EnsureInteract(GameObject go, string prompt, string message)
        {
            var interact = go.GetComponent<SimpleInteractable>();
            if (interact == null)
            {
                interact = go.AddComponent<SimpleInteractable>();
            }

            interact.Setup(prompt, message);
        }

        private static void DisableBlockingColliders(GameObject go)
        {
            foreach (var col in go.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }
        }

        private void ClearChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                SafeDestroy(transform.GetChild(i).gameObject);
            }
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(obj);
            }
            else
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}
