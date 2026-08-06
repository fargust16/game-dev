using System.Collections.Generic;
using GameDev.Environment;
using GameDev.Grid;
using GameDev.Input;
using GameDev.Interaction;
using GameDev.Player;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameDev.World
{
    public enum CityDistrict
    {
        Ridge = 0,
        LowerVeil = 1,
        Undercroft = 2
    }

    /// <summary>
    /// Seed-driven multi-district city scaffold: avenue + alleys, parcels, mandatory anchors.
    /// Design: 13 City Generation / 11 Vertical Slice (POIs stay in Lower Veil).
    /// </summary>
    public sealed class CityGenerator : MonoBehaviour
    {
        private const string PrefabFolder = "Assets/Prefabs/S0_Greybox/";
        private const int MapWidth = 160;
        private const int MapHeight = 120;

        private const int UndercroftMaxY = 34;
        private const int LowerVeilMaxY = 89;
        // Ridge: y 90..119

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

        private readonly bool[,] _road = new bool[MapWidth, MapHeight];
        private readonly bool[,] _alley = new bool[MapWidth, MapHeight];
        private readonly HashSet<long> _reserved = new HashSet<long>();

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
            ClearBoolGrid(_road);
            ClearBoolGrid(_alley);

            var roadsRng = worldSeed.Fork("roads");
            var parcelsRng = worldSeed.Fork("parcels");
            var anchorsRng = worldSeed.Fork("anchors");
            var dressingRng = worldSeed.Fork("dressing");

            var gridGo = new GameObject("GridMap");
            gridGo.transform.SetParent(transform, false);
            var grid = gridGo.AddComponent<GridMap>();
            grid.Configure(MapWidth, MapHeight, cellSize, Vector3.zero);
            gridGo.AddComponent<GridDebugDrawer>();

            // Default: blocked city fabric; roads carve walkable paths.
            PaintAll(grid, walkable: false, height: 0, flags: GridCellFlags.BlocksLoS);

            var avenueY = 48 + roadsRng.Next(0, 12); // Lower Veil band
            var plazaX = 70 + roadsRng.Next(0, 20);
            CarveAvenue(grid, avenueY);
            CarveCrossStreets(grid, roadsRng, avenueY, plazaX);
            CarveAlleys(grid, roadsRng, avenueY);
            CarvePlaza(grid, plazaX, avenueY);
            ApplyDistrictHeights(grid);

            PlaceParcelsAlongRoads(grid, parcelsRng, avenueY);

            // Factory mass — east Lower Veil landmark (always).
            var factoryX0 = MapWidth - 18;
            var factoryY0 = avenueY - 8;
            var factoryX1 = MapWidth - 4;
            var factoryY1 = avenueY + 6;
            PaintRect(grid, factoryX0, factoryY0, factoryX1, factoryY1, walkable: false, height: 2,
                flags: GridCellFlags.BlocksLoS | GridCellFlags.Climbable);
            PlaceBuildingMass(grid, factoryX0, factoryY0, factoryX1, factoryY1);
            PlaceWallStrip(grid, factoryX0, factoryY0, factoryX0, factoryY1);

            BuildRoadFloorStrips(grid);

            var anchors = PlaceMandatoryAnchors(grid, anchorsRng, avenueY, plazaX, factoryX0, factoryY0);
            PlaceDressing(grid, dressingRng, avenueY, anchors);
            PlaceEnvActions(grid, anchors);

            SpawnPlayerAndCamera(grid, anchors.Spawn);
            BuildLighting(anchors);
            BuildSystems();

            Debug.Log(
                $"[City] Generated seed={SeedValue}. Avenue Y={avenueY}, plaza X={plazaX}. " +
                "Slice anchors in Lower Veil. Press Play.");
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
        }

        private AnchorSet PlaceMandatoryAnchors(
            GridMap grid,
            System.Random rng,
            int avenueY,
            int plazaX,
            int factoryX0,
            int factoryY0)
        {
            var set = new AnchorSet();

            // Ridge Approach spawn — north of avenue on a northbound road.
            set.Spawn = SnapToRoad(grid, 80 + rng.Next(-8, 9), LowerVeilMaxY + 8 + rng.Next(0, 10), preferAlley: false);
            Reserve(set.Spawn);

            // Lower Veil slice core along avenue west → east.
            set.Tavern = SnapToRoad(grid, 36 + rng.Next(-4, 5), avenueY + (rng.Next(0, 2) == 0 ? 2 : -2), false);
            Reserve(set.Tavern);
            set.Tincture = SnapToRoad(grid, set.Tavern.x + 14 + rng.Next(0, 6), avenueY + (rng.Next(0, 2) == 0 ? -2 : 2), false);
            Reserve(set.Tincture);
            set.Loom = SnapToRoad(grid, plazaX + 8 + rng.Next(0, 8), avenueY - 1, true);
            Reserve(set.Loom);
            set.Stairs = SnapToRoad(grid, factoryX0 - 2, factoryY0 + 2 + rng.Next(0, 4), false);
            Reserve(set.Stairs);
            set.Factory = SnapToRoad(grid, factoryX0 - 1, avenueY, false);
            Reserve(set.Factory);

            // Ridge / Lower edge garrison.
            set.Warden = SnapToRoad(grid, plazaX - 10 + rng.Next(-3, 4), LowerVeilMaxY - 2, false);
            Reserve(set.Warden);

            // Undercroft stubs.
            set.Veinmarket = SnapToRoad(grid, plazaX + rng.Next(-6, 7), 12 + rng.Next(0, 10), true);
            Reserve(set.Veinmarket);
            set.Doctor = SnapToRoad(grid, 40 + rng.Next(0, 20), 8 + rng.Next(0, 8), true);
            Reserve(set.Doctor);

            var ridge = CreateLandmark(grid, LandmarkId.RidgeApproach, "Ridge Approach", set.Spawn);
            AddWorldLabel(ridge, "Ridge Approach");

            var tavern = CreateLandmark(grid, LandmarkId.SootAndBell, "Soot & Bell Tavern", set.Tavern);
            AddWorldLabel(tavern, "Soot & Bell");

            var boardCoord = NeighborRoad(grid, set.Tavern, 1, 0) ?? NeighborRoad(grid, set.Tavern, -1, 0) ?? set.Tavern;
            if (boardCoord.Equals(set.Tavern))
            {
                boardCoord = new GridCoord(set.Tavern.x + 1, set.Tavern.y);
                EnsureWalkableRoad(grid, boardCoord);
            }

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

            var merrinCoord = NeighborRoad(grid, set.Tavern, 0, -1) ?? NeighborRoad(grid, set.Tavern, 0, 1) ??
                              new GridCoord(set.Tavern.x, set.Tavern.y - 1);
            EnsureWalkableRoad(grid, merrinCoord);
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

            var harrowCoord = NeighborRoad(grid, set.Tincture, 0, -1) ?? set.Tincture;
            EnsureWalkableRoad(grid, harrowCoord);
            Reserve(harrowCoord);
            var harrow = CreatePoi(grid, PoiShellId.PoiA_HarrowArrest, "Harrow & Tincture", harrowCoord,
                "Brass-Number Arrest shell. Get a tip first, then participate before the Active Window ends.",
                "Inspect POI A",
                "Brass-Number Arrest shell.");
            EnsureLandmark(harrow, LandmarkId.HarrowAndTincture, "Harrow & Tincture");
            AddWorldLabel(harrow, "POI A — Harrow");

            var loom = CreateLandmark(grid, LandmarkId.LoomLane, "Loom Lane", set.Loom);
            AddWorldLabel(loom, "Loom Lane");

            var alleyCoord = NeighborRoad(grid, set.Loom, 1, 0) ?? NeighborRoad(grid, set.Loom, -1, 0) ?? set.Loom;
            EnsureWalkableRoad(grid, alleyCoord);
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
            var guardCoord = NeighborRoad(grid, set.Warden, 1, 0) ?? set.Warden;
            EnsureWalkableRoad(grid, guardCoord);
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
            var fixerCoord = NeighborRoad(grid, set.Veinmarket, 0, 1) ?? set.Veinmarket;
            EnsureWalkableRoad(grid, fixerCoord);
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

        private void PlaceDressing(GridMap grid, System.Random rng, int avenueY, AnchorSet anchors)
        {
            var ruskCoord = NeighborRoad(grid, anchors.Tincture, 2, 0) ??
                            SnapToRoad(grid, anchors.Tincture.x + 2, anchors.Tincture.y, false);
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

            var niaCoord = NeighborRoad(grid, anchors.Loom, 0, -1) ?? anchors.Loom;
            EnsureWalkableRoad(grid, niaCoord);
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

            CreateWaymarker(grid, MidpointRoad(grid, anchors.Spawn, anchors.Tavern), "Read waymarker",
                "South to Soot & Bell / Notice Board.");
            CreateWaymarker(grid, MidpointRoad(grid, anchors.Tavern, anchors.Tincture), "Read waymarker",
                "Along the avenue — Harrow & Tincture ahead.");
            CreateWaymarker(grid, MidpointRoad(grid, anchors.Tincture, anchors.Loom), "Read waymarker",
                "East toward Loom Lane alleys and the factory stairs.");
            CreateWaymarker(grid, MidpointRoad(grid, anchors.Loom, anchors.Stairs), "Read waymarker",
                "Coal Stairs / POI C under the factory wall.");

            // Sparse street stubs on avenue.
            for (var i = 0; i < 4; i++)
            {
                var x = 20 + rng.Next(0, MapWidth - 40);
                var c = SnapToRoad(grid, x, avenueY, false);
                if (_reserved.Contains(Key(c)))
                {
                    continue;
                }

                Reserve(c);
                var stub = CreateLandmark(grid, LandmarkId.TinctureRow, $"Street Stub {i + 1}", c);
                stub.name = $"NPC_StreetStub_{i + 1}";
                EnsureInteract(stub, "Nod past", "A Lower Veil face — busy, soot-streaked, not your story yet.");
            }
        }

        private void PlaceEnvActions(GridMap grid, AnchorSet anchors)
        {
            var crates = NeighborRoad(grid, anchors.Tincture, 3, 0) ?? anchors.Tincture;
            CreateEnvAction(grid, crates, "Vault Crates",
                EnvironmentActionType.VaultJump, 0.75f,
                "Vaulted the crates on Tincture Row.",
                "Slipped on wet cobble — noise, wasted effort.",
                GridCellFlags.Walkable | GridCellFlags.Vaultable);

            var window = NeighborRoad(grid, anchors.Tincture, -2, -1) ?? anchors.Tincture;
            CreateEnvAction(grid, window, "Side Window",
                EnvironmentActionType.ClimbThroughWindow, 0.65f,
                "Climbed through Harrow's side window.",
                "Boot scraped the frame — spotted / noisy.",
                GridCellFlags.Walkable | GridCellFlags.Window);

            var ledge = NeighborRoad(grid, anchors.Stairs, -1, 0) ??
                        NeighborRoad(grid, anchors.Stairs, 0, 1) ??
                        new GridCoord(anchors.Stairs.x - 1, anchors.Stairs.y);
            CreateEnvAction(grid, ledge, "Coal Ledge",
                EnvironmentActionType.ClimbUp, 0.7f,
                "Mantled the coal-stair ledge.",
                "Lost grip on sooty brick — drop, Hurt stub.",
                GridCellFlags.Walkable | GridCellFlags.Climbable | GridCellFlags.Stairs);
        }

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

        private void CarveAvenue(GridMap grid, int avenueY)
        {
            for (var x = 2; x < MapWidth - 2; x++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    var y = avenueY + dy;
                    if (y < 0 || y >= MapHeight)
                    {
                        continue;
                    }

                    MarkRoad(grid, x, y, alley: false);
                }
            }
        }

        private void CarveCrossStreets(GridMap grid, System.Random rng, int avenueY, int plazaX)
        {
            var xs = new List<int> { plazaX };
            var x = 18 + rng.Next(0, 8);
            while (x < MapWidth - 12)
            {
                if (Mathf.Abs(x - plazaX) > 10)
                {
                    xs.Add(x);
                }

                x += 18 + rng.Next(0, 11);
            }

            // Ridge approach corridor from spawn band down to avenue.
            xs.Add(80);

            foreach (var cx in xs)
            {
                var y0 = DistrictOfY(avenueY) == CityDistrict.LowerVeil ? UndercroftMaxY - 8 : 4;
                y0 = Mathf.Max(2, y0);
                var y1 = MapHeight - 4;
                for (var y = y0; y <= y1; y++)
                {
                    MarkRoad(grid, cx, y, alley: false);
                    if (cx == plazaX || Mathf.Abs(cx - 80) < 2)
                    {
                        // Slightly wider north approach / plaza cross.
                        if (cx + 1 < MapWidth)
                        {
                            MarkRoad(grid, cx + 1, y, alley: false);
                        }
                    }
                }
            }
        }

        private void CarveAlleys(GridMap grid, System.Random rng, int avenueY)
        {
            // Foot alleys: 1-cell branches off the avenue into both districts.
            var count = 14 + rng.Next(0, 8);
            for (var i = 0; i < count; i++)
            {
                var startX = 10 + rng.Next(0, MapWidth - 20);
                var south = rng.Next(0, 2) == 0;
                var length = 6 + rng.Next(0, 14);
                var x = startX;
                var y = avenueY + (south ? -2 : 2);
                for (var step = 0; step < length; step++)
                {
                    if (x < 2 || x >= MapWidth - 2 || y < 2 || y >= MapHeight - 2)
                    {
                        break;
                    }

                    MarkRoad(grid, x, y, alley: true);
                    if (rng.Next(0, 4) == 0)
                    {
                        x += rng.Next(0, 2) == 0 ? -1 : 1;
                    }
                    else
                    {
                        y += south ? -1 : 1;
                    }
                }
            }

            // Dense Undercroft alley mesh stubs.
            for (var i = 0; i < 10; i++)
            {
                var x0 = 20 + rng.Next(0, MapWidth - 40);
                var y0 = 4 + rng.Next(0, UndercroftMaxY - 8);
                var len = 8 + rng.Next(0, 12);
                for (var s = 0; s < len; s++)
                {
                    var x = x0 + (rng.Next(0, 3) - 1);
                    var y = y0 + s;
                    if (x > 1 && x < MapWidth - 2 && y > 1 && y < UndercroftMaxY)
                    {
                        MarkRoad(grid, x, y, alley: true);
                    }
                }
            }
        }

        private void CarvePlaza(GridMap grid, int plazaX, int avenueY)
        {
            for (var y = avenueY - 3; y <= avenueY + 3; y++)
            {
                for (var x = plazaX - 4; x <= plazaX + 4; x++)
                {
                    if (grid.InBounds(new GridCoord(x, y)))
                    {
                        MarkRoad(grid, x, y, alley: false);
                    }
                }
            }
        }

        private void ApplyDistrictHeights(GridMap grid)
        {
            for (var y = 0; y < MapHeight; y++)
            {
                var h = DistrictOfY(y) == CityDistrict.Ridge ? 1 : 0;
                for (var x = 0; x < MapWidth; x++)
                {
                    if (_road[x, y] || _alley[x, y])
                    {
                        grid.SetHeight(new GridCoord(x, y), h);
                    }
                }
            }
        }

        private void PlaceParcelsAlongRoads(GridMap grid, System.Random rng, int avenueY)
        {
            // Buildings along both sides of avenue (skip road cells).
            for (var x = 4; x < MapWidth - 4; x += 1)
            {
                TryParcel(grid, rng, x, avenueY + 3);
                TryParcel(grid, rng, x, avenueY - 3);
            }

            // Ridge plaza fountain stub.
            var fx = 70 + rng.Next(0, 20);
            var fy = LowerVeilMaxY + 6;
            if (!_road[fx, fy] && !_alley[fx, fy])
            {
                PaintRect(grid, fx, fy, fx + 2, fy + 2, walkable: false, height: 1, flags: GridCellFlags.BlocksLoS);
                PlaceBuildingMass(grid, fx, fy, fx + 2, fy + 2);
                var fountain = CreateLandmark(grid, LandmarkId.RidgeApproach, "Ridge Fountain Stub",
                    SnapToRoad(grid, fx, fy - 2, false));
                AddWorldLabel(fountain, "Fountain");
            }
        }

        private void TryParcel(GridMap grid, System.Random rng, int x, int y)
        {
            if (!grid.InBounds(new GridCoord(x, y)))
            {
                return;
            }

            if (_road[x, y] || _alley[x, y])
            {
                return;
            }

            if (rng.Next(0, 100) < 35)
            {
                return;
            }

            var use2 = building2x2Prefab != null && rng.Next(0, 100) < 40 &&
                       grid.InBounds(new GridCoord(x + 1, y + 1)) &&
                       !_road[x + 1, y] && !_road[x, y + 1] && !_road[x + 1, y + 1];
            if (use2)
            {
                PaintRect(grid, x, y, x + 1, y + 1, walkable: false, height: 1, flags: GridCellFlags.BlocksLoS);
                PlaceBuildingMass(grid, x, y, x + 1, y + 1);
            }
            else if (building1x1Prefab != null)
            {
                PaintRect(grid, x, y, x, y, walkable: false, height: 1, flags: GridCellFlags.BlocksLoS);
                PlaceBuildingMass(grid, x, y, x, y);
            }
        }

        private void BuildRoadFloorStrips(GridMap grid)
        {
            var floorRoot = new GameObject("FloorStrips");
            floorRoot.transform.SetParent(transform, false);

            // Horizontal merged strips per row of contiguous road cells.
            for (var y = 0; y < MapHeight; y++)
            {
                var x = 0;
                while (x < MapWidth)
                {
                    if (!_road[x, y] && !_alley[x, y])
                    {
                        x++;
                        continue;
                    }

                    var start = x;
                    while (x < MapWidth && (_road[x, y] || _alley[x, y]))
                    {
                        x++;
                    }

                    var end = x - 1;
                    SpawnFloorStrip(floorRoot.transform, grid, start, y, end, y);
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

        private void MarkRoad(GridMap grid, int x, int y, bool alley)
        {
            if (!grid.InBounds(new GridCoord(x, y)))
            {
                return;
            }

            if (alley)
            {
                _alley[x, y] = true;
            }
            else
            {
                _road[x, y] = true;
            }

            var height = DistrictOfY(y) == CityDistrict.Ridge ? 1 : 0;
            grid.SetFlags(new GridCoord(x, y), GridCellFlags.Walkable, walkable: true);
            grid.SetHeight(new GridCoord(x, y), height);
        }

        private void EnsureWalkableRoad(GridMap grid, GridCoord c)
        {
            MarkRoad(grid, c.x, c.y, alley: false);
        }

        private GridCoord SnapToRoad(GridMap grid, int x, int y, bool preferAlley)
        {
            x = Mathf.Clamp(x, 2, MapWidth - 3);
            y = Mathf.Clamp(y, 2, MapHeight - 3);
            var best = new GridCoord(x, y);
            var bestDist = int.MaxValue;

            for (var r = 0; r < 40; r++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    for (var dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r)
                        {
                            continue;
                        }

                        var cx = x + dx;
                        var cy = y + dy;
                        if (!grid.InBounds(new GridCoord(cx, cy)))
                        {
                            continue;
                        }

                        var isRoad = _road[cx, cy];
                        var isAlley = _alley[cx, cy];
                        if (!isRoad && !isAlley)
                        {
                            continue;
                        }

                        if (preferAlley && !isAlley && r < 8)
                        {
                            continue;
                        }

                        var d = Mathf.Abs(dx) + Mathf.Abs(dy);
                        if (d < bestDist)
                        {
                            bestDist = d;
                            best = new GridCoord(cx, cy);
                        }
                    }
                }

                if (bestDist < int.MaxValue)
                {
                    return best;
                }
            }

            EnsureWalkableRoad(grid, best);
            return best;
        }

        private GridCoord? NeighborRoad(GridMap grid, GridCoord origin, int dx, int dy)
        {
            var c = new GridCoord(origin.x + dx, origin.y + dy);
            if (!grid.InBounds(c))
            {
                return null;
            }

            if (_road[c.x, c.y] || _alley[c.x, c.y] || grid.IsWalkable(c))
            {
                return c;
            }

            return null;
        }

        private GridCoord MidpointRoad(GridMap grid, GridCoord a, GridCoord b)
        {
            return SnapToRoad(grid, (a.x + b.x) / 2, (a.y + b.y) / 2, false);
        }

        private void Reserve(GridCoord c) => _reserved.Add(Key(c));

        private static long Key(GridCoord c) => ((long)c.y << 32) ^ (uint)c.x;

        public static CityDistrict DistrictOfY(int y)
        {
            if (y <= UndercroftMaxY)
            {
                return CityDistrict.Undercroft;
            }

            if (y <= LowerVeilMaxY)
            {
                return CityDistrict.LowerVeil;
            }

            return CityDistrict.Ridge;
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
            var factoryPos = CoordWorld(anchors.Factory);
            rimFactory.transform.position = factoryPos + new Vector3(3f, 3.5f, 0f);
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
            CreateGasLamp(gasLamps, "GasLamp_Tavern", CoordWorld(anchors.Tavern) + new Vector3(1f, 2.5f, 1f));
            CreateGasLamp(gasLamps, "GasLamp_Tincture", CoordWorld(anchors.Tincture) + Vector3.up * 2.5f);
            CreateGasLamp(gasLamps, "GasLamp_Loom", CoordWorld(anchors.Loom) + Vector3.up * 2.5f);
            CreateGasLamp(gasLamps, "GasLamp_Stairs", CoordWorld(anchors.Stairs) + Vector3.up * 2.5f);
            CreateGasLamp(gasLamps, "GasLamp_Plaza", CoordWorld(new GridCoord(80, 55)) + Vector3.up * 2.5f);

            controller.Bind(sunDay, sunNight, fillSkyDay, rimFactory, warmTavern, coolLoom, gasLamps);
        }

        private Vector3 CoordWorld(GridCoord c)
        {
            return new Vector3((c.x + 0.5f) * cellSize, 0f, (c.y + 0.5f) * cellSize);
        }

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

        private void CreateWaymarker(GridMap grid, GridCoord coord, string prompt, string message)
        {
            if (_reserved.Contains(Key(coord)))
            {
                coord = SnapToRoad(grid, coord.x + 1, coord.y, false);
            }

            Reserve(coord);
            var go = SpawnPrefab(landmarkBlockPrefab, grid, coord, "Waymarker");
            go.transform.localScale = Vector3.Scale(go.transform.localScale, new Vector3(0.55f, 0.45f, 0.55f));
            EnsureInteract(go, prompt, message);
            AddWorldLabel(go, "→");
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

        private static void PaintAll(GridMap grid, bool walkable, int height, GridCellFlags flags)
        {
            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    var c = new GridCoord(x, y);
                    grid.SetFlags(c, flags, walkable);
                    grid.SetHeight(c, height);
                }
            }
        }

        private static void PaintRect(GridMap grid, int x0, int y0, int x1, int y1, bool walkable, int height,
            GridCellFlags flags)
        {
            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var c = new GridCoord(x, y);
                    if (!grid.InBounds(c))
                    {
                        continue;
                    }

                    grid.SetFlags(c, flags, walkable);
                    grid.SetHeight(c, height);
                }
            }
        }

        private void PlaceBuildingMass(GridMap grid, int x0, int y0, int x1, int y1)
        {
            var root = new GameObject($"Buildings_{x0}_{y0}");
            root.transform.SetParent(transform, false);

            var y = y0;
            while (y <= y1)
            {
                var rowUses2x2 = building2x2Prefab != null && y + 1 <= y1;
                var x = x0;
                while (x <= x1)
                {
                    if (rowUses2x2 && x + 1 <= x1)
                    {
                        var go = SpawnPrefab(building2x2Prefab, root.transform, grid, new GridCoord(x, y),
                            $"Building2x2_{x}_{y}");
                        var a = grid.CoordToWorldCenter(new GridCoord(x, y));
                        var b = grid.CoordToWorldCenter(new GridCoord(x + 1, y + 1));
                        var pos = (a + b) * 0.5f;
                        pos.y = 0f;
                        go.transform.position = pos;
                        x += 2;
                    }
                    else if (building1x1Prefab != null)
                    {
                        var go = SpawnPrefab(building1x1Prefab, root.transform, grid, new GridCoord(x, y),
                            $"Building1x1_{x}_{y}");
                        var pos = grid.CoordToWorldCenter(new GridCoord(x, y));
                        pos.y = 0f;
                        go.transform.position = pos;
                        x += 1;
                    }
                    else
                    {
                        x += 1;
                    }
                }

                y += rowUses2x2 ? 2 : 1;
            }
        }

        private void PlaceWallStrip(GridMap grid, int x0, int y0, int x1, int y1)
        {
            if (wallSegmentPrefab == null)
            {
                return;
            }

            var root = new GameObject("WallStrip");
            root.transform.SetParent(transform, false);
            for (var y = y0; y <= y1; y++)
            {
                var go = SpawnPrefab(wallSegmentPrefab, root.transform, grid, new GridCoord(x0, y), $"Wall_{x0}_{y}");
                var pos = grid.CoordToWorldCenter(new GridCoord(x0, y));
                pos.y = 0f;
                go.transform.position = pos;
            }
        }

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
            EnsureWalkableRoad(grid, coord);
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
            return SpawnPrefab(prefab, transform, grid, coord, label);
        }

        private GameObject SpawnPrefab(GameObject prefab, Transform parent, GridMap grid, GridCoord coord, string label)
        {
            GameObject go;
            if (prefab != null)
            {
#if UNITY_EDITOR
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
#else
                go = Object.Instantiate(prefab, parent);
#endif
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetParent(parent, false);
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

        private static void ClearBoolGrid(bool[,] g)
        {
            var w = g.GetLength(0);
            var h = g.GetLength(1);
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    g[x, y] = false;
                }
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
