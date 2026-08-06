using GameDev.Environment;
using GameDev.Grid;
using GameDev.Interaction;
using GameDev.Player;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameDev.World
{
    /// <summary>
    /// Builds the S0 Lower Veil greybox on a square grid using S0_Greybox prefabs.
    /// Design: 11.2 Milestone S0 Greybox.
    /// </summary>
    public sealed class LowerVeilGreyboxBuilder : MonoBehaviour
    {
        private const string PrefabFolder = "Assets/Prefabs/S0_Greybox/";

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

        public void Build()
        {
            ResolvePrefabs();
            ClearChildren();

            var gridGo = new GameObject("GridMap");
            gridGo.transform.SetParent(transform, false);
            var grid = gridGo.AddComponent<GridMap>();
            grid.Configure(36, 28, cellSize, Vector3.zero);
            gridGo.AddComponent<GridDebugDrawer>();

            // S0.2: compact district — readable loop Ridge → Tavern → Tincture → Loom → Stairs.
            PaintRect(grid, 0, 0, 31, 23, walkable: true, height: 0, flags: GridCellFlags.Walkable);
            PaintRect(grid, 0, 18, 31, 23, walkable: true, height: 1, flags: GridCellFlags.Walkable);

            // Factory mass (east) and Harrow shop block.
            PaintRect(grid, 26, 4, 31, 14, walkable: false, height: 2, flags: GridCellFlags.BlocksLoS | GridCellFlags.Climbable);
            PaintRect(grid, 7, 11, 12, 13, walkable: false, height: 1, flags: GridCellFlags.BlocksLoS | GridCellFlags.Window);

            // Frame the main street so the loop is obvious (side courtyards blocked).
            PaintRect(grid, 0, 0, 4, 10, walkable: false, height: 1, flags: GridCellFlags.BlocksLoS);
            PaintRect(grid, 14, 14, 20, 16, walkable: false, height: 1, flags: GridCellFlags.BlocksLoS);
            PaintRect(grid, 0, 14, 5, 16, walkable: false, height: 1, flags: GridCellFlags.BlocksLoS);

            BuildFloorVisuals(grid);
            PlaceBuildingMass(grid, 26, 4, 31, 14);
            PlaceBuildingMass(grid, 7, 11, 12, 13);
            PlaceBuildingMass(grid, 0, 0, 4, 10);
            PlaceBuildingMass(grid, 14, 14, 20, 16);
            PlaceWallStrip(grid, 26, 4, 26, 14);

            var ridge = CreateLandmark(grid, LandmarkId.RidgeApproach, "Ridge Approach", new GridCoord(16, 21));
            AddWorldLabel(ridge, "Ridge Approach");

            var tavern = CreateLandmark(grid, LandmarkId.SootAndBell, "Soot & Bell Tavern", new GridCoord(10, 17));
            AddWorldLabel(tavern, "Soot & Bell");

            var board = SpawnPrefab(noticeBoardPrefab, grid, new GridCoord(12, 17), "Notice Board");
            EnsureLandmark(board, LandmarkId.NoticeBoard, "Notice Board");
            EnsureInteract(board, "Read notice board",
                "Pinned: 1) Warden seizure — Harrow & Tincture before second-shift bell. 2) Missing persons — Loom Lane. 3) Coal price notice.");
            AddTipGiver(board,
                hints: new[] { PoiShellId.PoiB_EleventhBellCollectors },
                marks: new[] { PoiShellId.PoiA_HarrowArrest },
                summary: "Board marks Harrow seizure and hints trouble on Loom Lane.");
            AddWorldLabel(board, "Notice Board");

            var tavernTalk = CreateLandmark(grid, LandmarkId.SootAndBell, "Bartender Merrin", new GridCoord(9, 16));
            tavernTalk.name = "Bartender_Merrin";
            EnsureInteract(tavernTalk, "Talk to Merrin",
                "Merrin: Brass-numbers are taking old Harrow tonight. After the eleventh bell they take mill girls off Loom Lane. Check the board. (Outdoor hub stub — no tavern interior yet.)");
            AddTipGiver(tavernTalk,
                hints: new[] { PoiShellId.PoiB_EleventhBellCollectors },
                marks: new[] { PoiShellId.PoiA_HarrowArrest },
                summary: "Merrin marks Harrow and hints the eleventh-bell collectors.");
            AddWorldLabel(tavernTalk, "Merrin (bar stub)");

            var tincture = CreateLandmark(grid, LandmarkId.TinctureRow, "Tincture Row", new GridCoord(10, 13));
            AddWorldLabel(tincture, "Tincture Row");

            var harrow = CreatePoi(grid, PoiShellId.PoiA_HarrowArrest, "Harrow & Tincture", new GridCoord(9, 12),
                "Brass-Number Arrest shell. Get a tip first, then participate before the Active Window ends.",
                "Inspect POI A",
                "Brass-Number Arrest shell.");
            EnsureLandmark(harrow, LandmarkId.HarrowAndTincture, "Harrow & Tincture");
            AddWorldLabel(harrow, "POI A — Harrow");

            var loom = CreateLandmark(grid, LandmarkId.LoomLane, "Loom Lane", new GridCoord(16, 9));
            AddWorldLabel(loom, "Loom Lane");

            var alley = CreatePoi(grid, PoiShellId.PoiB_EleventhBellCollectors, "Alley Mouth", new GridCoord(19, 8),
                "Eleventh-Bell Collectors shell. Tips make it Hinted/Marked; Brief window is short.",
                "Inspect POI B",
                "Eleventh-Bell Collectors shell.");
            EnsureLandmark(alley, LandmarkId.AlleyMouth, "Alley Mouth");
            AddWorldLabel(alley, "POI B — Alley");

            var factory = CreateLandmark(grid, LandmarkId.FactoryWall, "Factory Wall", new GridCoord(24, 10));
            AddWorldLabel(factory, "Factory Wall");

            var stairs = CreatePoi(grid, PoiShellId.PoiC_SootStairTrace, "Coal Stairs", new GridCoord(23, 7),
                "Soot Stair Trace. Unlocks after POI A or B leaves Aftermath.",
                "Inspect POI C",
                "Soot Stair Trace shell.");
            EnsureLandmark(stairs, LandmarkId.CoalStairs, "Coal Stairs");
            AddWorldLabel(stairs, "POI C — Stairs");

            // Street NPC stubs (S0.3).
            var coalHauler = CreateLandmark(grid, LandmarkId.TinctureRow, "Coal Hauler Rusk", new GridCoord(12, 14));
            coalHauler.name = "NPC_CoalHauler_Rusk";
            EnsureInteract(coalHauler, "Talk to Rusk",
                "Rusk: Keep your boots off the wet coal. Wardens are thick on Tincture tonight — and Loom Lane after the eleventh bell.");
            AddTipGiver(coalHauler,
                hints: new[] { PoiShellId.PoiA_HarrowArrest, PoiShellId.PoiB_EleventhBellCollectors },
                marks: System.Array.Empty<PoiShellId>(),
                summary: "Rusk hints both the seizure street and Loom Lane.");
            AddWorldLabel(coalHauler, "Rusk (street)");

            var millGirl = CreateLandmark(grid, LandmarkId.LoomLane, "Mill Girl Nia", new GridCoord(17, 8));
            millGirl.name = "NPC_MillGirl_Nia";
            EnsureInteract(millGirl, "Talk to Nia",
                "Nia: They took two girls last week from this lane. If you hear coach-wheels after the eleventh bell — don't look helpful. Look small.");
            AddTipGiver(millGirl,
                hints: System.Array.Empty<PoiShellId>(),
                marks: new[] { PoiShellId.PoiB_EleventhBellCollectors },
                summary: "Nia marks the eleventh-bell collectors on Loom Lane.");
            AddWorldLabel(millGirl, "Nia (street)");

            // Waymarkers along the loop (E for hint if labels are missed).
            CreateWaymarker(grid, new GridCoord(13, 19), "Read waymarker", "South to Soot & Bell / Notice Board.");
            CreateWaymarker(grid, new GridCoord(10, 15), "Read waymarker", "Down Tincture Row — Harrow & Tincture ahead.");
            CreateWaymarker(grid, new GridCoord(13, 11), "Read waymarker", "East to Loom Lane, then the factory stairs.");
            CreateWaymarker(grid, new GridCoord(20, 9), "Read waymarker", "Coal Stairs / POI C under the factory wall.");

            CreateEnvAction(grid, new GridCoord(13, 12), "Vault Crates",
                EnvironmentActionType.VaultJump, 0.75f,
                "Vaulted the crates on Tincture Row.",
                "Slipped on wet cobble — noise, wasted effort.",
                GridCellFlags.Walkable | GridCellFlags.Vaultable);

            CreateEnvAction(grid, new GridCoord(6, 12), "Side Window",
                EnvironmentActionType.ClimbThroughWindow, 0.65f,
                "Climbed through Harrow's side window.",
                "Boot scraped the frame — spotted / noisy.",
                GridCellFlags.Walkable | GridCellFlags.Window);

            CreateEnvAction(grid, new GridCoord(25, 7), "Coal Ledge",
                EnvironmentActionType.ClimbUp, 0.7f,
                "Mantled the coal-stair ledge.",
                "Lost grip on sooty brick — drop, Hurt stub.",
                GridCellFlags.Walkable | GridCellFlags.Climbable | GridCellFlags.Stairs);

            var player = SpawnPrefab(playerCapsulePrefab, grid, new GridCoord(16, 21), "Player");
            DisableBlockingColliders(player);
            var playerCtrl = player.GetComponent<GridPlayerController>();
            if (playerCtrl == null)
            {
                playerCtrl = player.AddComponent<GridPlayerController>();
            }

            playerCtrl.PlaceOnGrid(grid, new GridCoord(16, 21));

            var camGo = new GameObject("TacticalCamera");
            camGo.transform.SetParent(transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            var tacticalCam = camGo.AddComponent<SimpleFollowCamera>();
            tacticalCam.SetTarget(player.transform);

            BuildLighting();
            BuildS1Systems();

            Debug.Log("[S1] Greybox + tips/Active Windows ready. Board/Merrin mark POIs. ] advances 1 world-hour. H legend.");
        }

        private void BuildS1Systems()
        {
            var systems = new GameObject("S1_Systems");
            systems.transform.SetParent(transform, false);
            systems.AddComponent<WorldClock>();
            systems.AddComponent<DistrictResidue>();
            systems.AddComponent<PoiDirector>();
            systems.AddComponent<GameDev.Decisions.DecisionNodeSession>();
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

        private void BuildLighting()
        {
            var lightingGo = new GameObject("S0_Lighting");
            lightingGo.transform.SetParent(transform, false);
            var controller = lightingGo.AddComponent<S0_LightingController>();

            // 1. Sun_Day
            var sunDay = new GameObject("Sun_Day");
            sunDay.transform.SetParent(lightingGo.transform, false);
            var lSunDay = sunDay.AddComponent<Light>();
            lSunDay.type = LightType.Directional;
            sunDay.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            lSunDay.color = new Color(1.0f, 0.93f, 0.82f);
            lSunDay.intensity = 1.25f;
            lSunDay.shadows = LightShadows.Soft;
            lSunDay.shadowStrength = 0.8f;

            // 2. Sun_Night
            var sunNight = new GameObject("Sun_Night");
            sunNight.transform.SetParent(lightingGo.transform, false);
            var lSunNight = sunNight.AddComponent<Light>();
            lSunNight.type = LightType.Directional;
            sunNight.transform.rotation = Quaternion.Euler(28f, -55f, 0f);
            lSunNight.color = new Color(0.55f, 0.65f, 0.85f);
            lSunNight.intensity = 0.25f;
            lSunNight.shadows = LightShadows.Soft;
            lSunNight.shadowStrength = 0.5f;

            // 3. Fill_Sky_Day
            var fillSkyDay = new GameObject("Fill_Sky_Day");
            fillSkyDay.transform.SetParent(lightingGo.transform, false);
            var lFill = fillSkyDay.AddComponent<Light>();
            lFill.type = LightType.Directional;
            fillSkyDay.transform.rotation = Quaternion.Euler(70f, 120f, 0f);
            lFill.color = new Color(0.55f, 0.62f, 0.72f);
            lFill.intensity = 0.25f;
            lFill.shadows = LightShadows.None;

            // 4. Rim_Factory
            var rimFactory = new GameObject("Rim_Factory");
            rimFactory.transform.SetParent(lightingGo.transform, false);
            rimFactory.transform.position = new Vector3(27.0f, 3.5f, 10.5f);
            rimFactory.transform.rotation = Quaternion.Euler(20f, -90f, 0f);
            var lRim = rimFactory.AddComponent<Light>();
            lRim.type = LightType.Spot;
            lRim.color = new Color(0.40f, 0.65f, 0.75f);
            lRim.intensity = 1.0f;
            lRim.range = 15.0f;
            lRim.spotAngle = 70.0f;
            lRim.shadows = LightShadows.Soft;

            // 5. Warm_TavernPool
            var warmTavern = new GameObject("Warm_TavernPool");
            warmTavern.transform.SetParent(lightingGo.transform, false);
            warmTavern.transform.position = new Vector3(10.5f, 2.5f, 17.0f);
            var lTavern = warmTavern.AddComponent<Light>();
            lTavern.type = LightType.Point;
            lTavern.color = new Color(1.0f, 0.72f, 0.35f);
            lTavern.intensity = 0.45f;
            lTavern.range = 8.0f;
            lTavern.shadows = LightShadows.Soft;

            // 6. Cool_LoomPool
            var coolLoom = new GameObject("Cool_LoomPool");
            coolLoom.transform.SetParent(lightingGo.transform, false);
            coolLoom.transform.position = new Vector3(17.0f, 2.5f, 9.0f);
            var lLoom = coolLoom.AddComponent<Light>();
            lLoom.type = LightType.Point;
            lLoom.color = new Color(0.40f, 0.60f, 0.85f);
            lLoom.intensity = 0.35f;
            lLoom.range = 8.0f;
            lLoom.shadows = LightShadows.Soft;

            // 7. Night_GasLamps
            var gasLamps = new GameObject("Night_GasLamps");
            gasLamps.transform.SetParent(lightingGo.transform, false);

            CreateGasLamp(gasLamps, "GasLamp_Tavern", new Vector3(11.5f, 2.5f, 17.5f));
            CreateGasLamp(gasLamps, "GasLamp_Tincture", new Vector3(10.0f, 2.5f, 13.0f));
            CreateGasLamp(gasLamps, "GasLamp_Loom", new Vector3(18.0f, 2.5f, 9.0f));
            CreateGasLamp(gasLamps, "GasLamp_Stairs", new Vector3(24.0f, 2.5f, 7.5f));

            controller.Bind(
                sunDay,
                sunNight,
                fillSkyDay,
                rimFactory,
                warmTavern,
                coolLoom,
                gasLamps);
            // Lighting follows WorldClock (starts ~09:00 day, eases through dusk/night).
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
            l.range = 6.5f;
            l.shadows = LightShadows.Soft;
        }

        private void CreateWaymarker(GridMap grid, GridCoord coord, string prompt, string message)
        {
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

        private void PaintRect(GridMap grid, int x0, int y0, int x1, int y1, bool walkable, int height, GridCellFlags flags)
        {
            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var c = new GridCoord(x, y);
                    grid.SetFlags(c, flags, walkable);
                    grid.SetHeight(c, height);
                }
            }
        }

        private void BuildFloorVisuals(GridMap grid)
        {
            var floorRoot = new GameObject("FloorVisuals");
            floorRoot.transform.SetParent(transform, false);

            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    var coord = new GridCoord(x, y);
                    if (!grid.TryGetCell(coord, out var cell))
                    {
                        continue;
                    }

                    if ((cell.flags & GridCellFlags.Walkable) == 0)
                    {
                        continue;
                    }

                    var tile = SpawnPrefab(floorTilePrefab, floorRoot.transform, grid, coord, $"Floor_{x}_{y}");
                    DisableBlockingColliders(tile);
                    var pos = grid.CoordToWorldCenter(coord);
                    pos.y = cell.height * grid.CellSize;
                    tile.transform.position = pos;
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
                        var go = SpawnPrefab(building2x2Prefab, root.transform, grid, new GridCoord(x, y), $"Building2x2_{x}_{y}");
                        var a = grid.CoordToWorldCenter(new GridCoord(x, y));
                        var b = grid.CoordToWorldCenter(new GridCoord(x + 1, y + 1));
                        var pos = (a + b) * 0.5f;
                        pos.y = 0f;
                        go.transform.position = pos;
                        x += 2;
                    }
                    else if (building1x1Prefab != null)
                    {
                        var go = SpawnPrefab(building1x1Prefab, root.transform, grid, new GridCoord(x, y), $"Building1x1_{x}_{y}");
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
                Debug.LogWarning($"[S0] Missing prefab for '{label}'. Using primitive fallback.");
            }

            go.name = label;
            var pos = grid.CoordToWorldCenter(coord);
            pos.y = grid.TryGetCell(coord, out var cell) ? cell.height * grid.CellSize : 0f;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            return go;
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
