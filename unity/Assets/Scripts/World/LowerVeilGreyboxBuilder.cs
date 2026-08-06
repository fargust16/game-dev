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

            PaintRect(grid, 0, 0, 35, 27, walkable: true, height: 0, flags: GridCellFlags.Walkable);
            PaintRect(grid, 0, 22, 35, 27, walkable: true, height: 1, flags: GridCellFlags.Walkable);
            PaintRect(grid, 30, 4, 35, 18, walkable: false, height: 2, flags: GridCellFlags.BlocksLoS | GridCellFlags.Climbable);
            PaintRect(grid, 8, 14, 14, 16, walkable: false, height: 1, flags: GridCellFlags.BlocksLoS | GridCellFlags.Window);

            BuildFloorVisuals(grid);
            PlaceBuildingMass(grid, 30, 4, 35, 18);
            PlaceBuildingMass(grid, 8, 14, 14, 16);
            PlaceWallStrip(grid, 30, 4, 30, 18);

            CreateLandmark(grid, LandmarkId.RidgeApproach, "Ridge Approach", new GridCoord(18, 25));
            CreateLandmark(grid, LandmarkId.SootAndBell, "Soot & Bell Tavern", new GridCoord(10, 20));

            var board = SpawnPrefab(noticeBoardPrefab, grid, new GridCoord(12, 20), "Notice Board");
            EnsureLandmark(board, LandmarkId.NoticeBoard, "Notice Board");
            EnsureInteract(board, "Read board",
                "Pinned: 1) Warden seizure — Harrow & Tincture before second-shift bell. 2) Missing persons — Loom Lane. 3) Coal price notice.");

            var tavernTalk = CreateLandmark(grid, LandmarkId.SootAndBell, "Bartender Merrin", new GridCoord(9, 19));
            tavernTalk.name = "Bartender_Merrin";
            EnsureInteract(tavernTalk, "Talk",
                "Merrin: Brass-numbers are taking old Harrow tonight. After the eleventh bell they take mill girls off Loom Lane. Check the board.");

            CreateLandmark(grid, LandmarkId.TinctureRow, "Tincture Row", new GridCoord(11, 15));

            var harrow = CreatePoi(grid, PoiShellId.PoiA_HarrowArrest, "Harrow & Tincture", new GridCoord(10, 14),
                "S1 will start Decision Node: Brass-Number Arrest at Harrow & Tincture.",
                "Inspect POI A shell",
                "POI A shell ready. S1 will start the arrest Decision Node here.");
            EnsureLandmark(harrow, LandmarkId.HarrowAndTincture, "Harrow & Tincture");

            CreateLandmark(grid, LandmarkId.LoomLane, "Loom Lane", new GridCoord(18, 10));

            var alley = CreatePoi(grid, PoiShellId.PoiB_EleventhBellCollectors, "Alley Mouth", new GridCoord(22, 9),
                "S1 will start Decision Node: Eleventh-Bell Collectors.",
                "Inspect POI B shell",
                "POI B shell ready. S1 will start the collectors Decision Node here.");
            EnsureLandmark(alley, LandmarkId.AlleyMouth, "Alley Mouth");

            CreateLandmark(grid, LandmarkId.FactoryWall, "Factory Wall", new GridCoord(28, 11));

            var stairs = CreatePoi(grid, PoiShellId.PoiC_SootStairTrace, "Coal Stairs", new GridCoord(26, 8),
                "S1 will start Decision Node: Soot Stair Trace.",
                "Inspect POI C shell",
                "POI C shell ready. Aftermath Trace will live here after A/B.");
            EnsureLandmark(stairs, LandmarkId.CoalStairs, "Coal Stairs");

            CreateEnvAction(grid, new GridCoord(13, 14), "Vault Crates",
                EnvironmentActionType.VaultJump, 0.75f,
                "Vaulted the crates on Tincture Row.",
                "Slipped on wet cobble — noise, wasted effort.",
                GridCellFlags.Walkable | GridCellFlags.Vaultable);

            CreateEnvAction(grid, new GridCoord(8, 15), "Side Window",
                EnvironmentActionType.ClimbThroughWindow, 0.65f,
                "Climbed through Harrow's side window.",
                "Boot scraped the frame — spotted / noisy.",
                GridCellFlags.Walkable | GridCellFlags.Window);

            CreateEnvAction(grid, new GridCoord(29, 8), "Coal Ledge",
                EnvironmentActionType.ClimbUp, 0.7f,
                "Mantled the coal-stair ledge.",
                "Lost grip on sooty brick — drop, Hurt stub.",
                GridCellFlags.Walkable | GridCellFlags.Climbable | GridCellFlags.Stairs);

            var player = SpawnPrefab(playerCapsulePrefab, grid, new GridCoord(18, 25), "Player");
            DisableBlockingColliders(player);
            var playerCtrl = player.GetComponent<GridPlayerController>();
            if (playerCtrl == null)
            {
                playerCtrl = player.AddComponent<GridPlayerController>();
            }

            playerCtrl.PlaceOnGrid(grid, new GridCoord(18, 25));

            var camGo = new GameObject("TacticalCamera");
            camGo.transform.SetParent(transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            var tacticalCam = camGo.AddComponent<SimpleFollowCamera>();
            tacticalCam.SetTarget(player.transform);

            var lightGo = new GameObject("Directional Light");
            lightGo.transform.SetParent(transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Debug.Log("[S0] Greybox ready. LMB/Arrows move avatar; WASD pans camera; E interact (on-screen toast).");
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
