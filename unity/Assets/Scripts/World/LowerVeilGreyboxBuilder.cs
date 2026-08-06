using GameDev.Environment;
using GameDev.Grid;
using GameDev.Interaction;
using GameDev.Player;
using UnityEngine;

namespace GameDev.World
{
    /// <summary>
    /// Builds the S0 Lower Veil greybox on a square grid.
    /// Design: 11.2 Milestone S0 Greybox.
    /// </summary>
    public sealed class LowerVeilGreyboxBuilder : MonoBehaviour
    {
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Material greyFloorMaterial;
        [SerializeField] private Material greyBlockMaterial;
        [SerializeField] private Material ridgeMaterial;
        [SerializeField] private Material landmarkMaterial;

        private Material _floorMat;
        private Material _blockMat;
        private Material _ridgeMat;
        private Material _landmarkMat;

        public void Build()
        {
            ClearChildren();
            CacheMaterials();

            var gridGo = new GameObject("GridMap");
            gridGo.transform.SetParent(transform, false);
            var grid = gridGo.AddComponent<GridMap>();
            grid.Configure(36, 28, cellSize, Vector3.zero);
            gridGo.AddComponent<GridDebugDrawer>();

            // Default all walkable, then carve.
            PaintRect(grid, 0, 0, 35, 27, walkable: true, height: 0, flags: GridCellFlags.Walkable);

            // Ridge (north) higher class strip.
            PaintRect(grid, 0, 22, 35, 27, walkable: true, height: 1, flags: GridCellFlags.Walkable);
            // Factory wall mass (east).
            PaintRect(grid, 30, 4, 35, 18, walkable: false, height: 2, flags: GridCellFlags.BlocksLoS | GridCellFlags.Climbable);
            // Block a few solid shop walls.
            PaintRect(grid, 8, 14, 14, 16, walkable: false, height: 1, flags: GridCellFlags.BlocksLoS | GridCellFlags.Window);

            BuildFloorVisuals(grid);
            BuildVolume(grid, 30, 4, 35, 18, 2.5f, _blockMat);
            BuildVolume(grid, 8, 14, 14, 16, 1.8f, _blockMat);

            // Landmarks / route from 11.2
            CreateLandmark(grid, LandmarkId.RidgeApproach, "Ridge Approach", new GridCoord(18, 25), _ridgeMat);
            CreateLandmark(grid, LandmarkId.SootAndBell, "Soot & Bell Tavern", new GridCoord(10, 20), _landmarkMat);

            var board = CreateLandmark(grid, LandmarkId.NoticeBoard, "Notice Board", new GridCoord(12, 20), _landmarkMat);
            AddInteract(board, "Read board",
                "Pinned: 1) Warden seizure — Harrow & Tincture before second-shift bell. 2) Missing persons — Loom Lane. 3) Coal price notice.");

            var tavernTalk = CreateLandmark(grid, LandmarkId.SootAndBell, "Bartender Merrin", new GridCoord(9, 19), _landmarkMat);
            tavernTalk.name = "Bartender_Merrin";
            AddInteract(tavernTalk, "Talk",
                "Merrin: Brass-numbers are taking old Harrow tonight. After the eleventh bell they take mill girls off Loom Lane. Check the board.");

            CreateLandmark(grid, LandmarkId.TinctureRow, "Tincture Row", new GridCoord(11, 15), _landmarkMat);

            var harrow = CreateLandmark(grid, LandmarkId.HarrowAndTincture, "Harrow & Tincture", new GridCoord(10, 14), _landmarkMat);
            AddPoi(harrow, PoiShellId.PoiA_HarrowArrest, "S1 will start Decision Node: Brass-Number Arrest at Harrow & Tincture.");
            AddInteract(harrow, "Inspect POI A shell", "POI A shell ready. S1 will start the arrest Decision Node here.");

            CreateLandmark(grid, LandmarkId.LoomLane, "Loom Lane", new GridCoord(18, 10), _landmarkMat);

            var alley = CreateLandmark(grid, LandmarkId.AlleyMouth, "Alley Mouth", new GridCoord(22, 9), _landmarkMat);
            AddPoi(alley, PoiShellId.PoiB_EleventhBellCollectors, "S1 will start Decision Node: Eleventh-Bell Collectors.");
            AddInteract(alley, "Inspect POI B shell", "POI B shell ready. S1 will start the collectors Decision Node here.");

            CreateLandmark(grid, LandmarkId.FactoryWall, "Factory Wall", new GridCoord(28, 11), _landmarkMat);

            var stairs = CreateLandmark(grid, LandmarkId.CoalStairs, "Coal Stairs", new GridCoord(26, 8), _landmarkMat);
            AddPoi(stairs, PoiShellId.PoiC_SootStairTrace, "S1 will start Decision Node: Soot Stair Trace.");
            AddInteract(stairs, "Inspect POI C shell", "POI C shell ready. Aftermath Trace will live here after A/B.");

            // Environment action stubs
            var vault = CreateMarker(grid, new GridCoord(13, 14), "Vault Crates");
            vault.AddComponent<EnvironmentActionMarker>().Setup(
                EnvironmentActionType.VaultJump,
                0.75f,
                "Vaulted the crates on Tincture Row.",
                "Slipped on wet cobble — noise, wasted effort.");
            grid.SetFlags(new GridCoord(13, 14), GridCellFlags.Walkable | GridCellFlags.Vaultable);

            var window = CreateMarker(grid, new GridCoord(8, 15), "Side Window");
            window.AddComponent<EnvironmentActionMarker>().Setup(
                EnvironmentActionType.ClimbThroughWindow,
                0.65f,
                "Climbed through Harrow's side window.",
                "Boot scraped the frame — spotted / noisy.");
            grid.SetFlags(new GridCoord(8, 15), GridCellFlags.Walkable | GridCellFlags.Window);

            var climb = CreateMarker(grid, new GridCoord(29, 8), "Coal Ledge");
            climb.AddComponent<EnvironmentActionMarker>().Setup(
                EnvironmentActionType.ClimbUp,
                0.7f,
                "Mantled the coal-stair ledge.",
                "Lost grip on sooty brick — drop, Hurt stub.");
            grid.SetFlags(new GridCoord(29, 8), GridCellFlags.Walkable | GridCellFlags.Climbable | GridCellFlags.Stairs);

            // Player + camera
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.SetParent(transform, false);
            SafeDestroy(player.GetComponent<Collider>());
            var playerCtrl = player.AddComponent<GridPlayerController>();
            playerCtrl.PlaceOnGrid(grid, new GridCoord(18, 25));

            var camGo = new GameObject("FollowCamera");
            camGo.transform.SetParent(transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            var follow = camGo.AddComponent<SimpleFollowCamera>();
            follow.SetTarget(player.transform);
            camGo.transform.position = player.transform.position + new Vector3(0f, 14f, -10f);

            // Light
            var lightGo = new GameObject("Directional Light");
            lightGo.transform.SetParent(transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Debug.Log("[S0] Lower Veil greybox built. WASD/Arrows move, E interacts. Check Console for board/bartender/POI/env stubs.");
        }

        private void CacheMaterials()
        {
            _floorMat = greyFloorMaterial != null ? greyFloorMaterial : CreateColorMat(new Color(0.25f, 0.25f, 0.28f));
            _blockMat = greyBlockMaterial != null ? greyBlockMaterial : CreateColorMat(new Color(0.18f, 0.18f, 0.2f));
            _ridgeMat = ridgeMaterial != null ? ridgeMaterial : CreateColorMat(new Color(0.35f, 0.34f, 0.3f));
            _landmarkMat = landmarkMaterial != null ? landmarkMaterial : CreateColorMat(new Color(0.55f, 0.4f, 0.2f));
        }

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

                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Floor_{x}_{y}";
                    tile.transform.SetParent(floorRoot.transform, false);
                    var center = grid.CoordToWorldCenter(coord);
                    center.y = cell.height * grid.CellSize - 0.05f;
                    tile.transform.position = center;
                    tile.transform.localScale = new Vector3(grid.CellSize * 0.98f, 0.1f, grid.CellSize * 0.98f);
                    tile.GetComponent<MeshRenderer>().sharedMaterial = cell.height > 0 ? _ridgeMat : _floorMat;
                    SafeDestroy(tile.GetComponent<Collider>());
                }
            }
        }

        private void BuildVolume(GridMap grid, int x0, int y0, int x1, int y1, float worldHeight, Material mat)
        {
            var min = grid.CoordToWorldCenter(new GridCoord(x0, y0));
            var max = grid.CoordToWorldCenter(new GridCoord(x1, y1));
            var center = (min + max) * 0.5f;
            center.y = worldHeight * 0.5f;
            var size = new Vector3(
                Mathf.Abs(max.x - min.x) + grid.CellSize,
                worldHeight,
                Mathf.Abs(max.z - min.z) + grid.CellSize);

            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "BlockVolume";
            block.transform.SetParent(transform, false);
            block.transform.position = center;
            block.transform.localScale = size;
            block.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private GameObject CreateLandmark(GridMap grid, LandmarkId id, string label, GridCoord coord, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(transform, false);
            var pos = grid.CoordToWorldCenter(coord);
            pos.y = 0.75f + (grid.TryGetCell(coord, out var cell) ? cell.height * grid.CellSize : 0f);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.8f, 1.5f, 0.8f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            SafeDestroy(go.GetComponent<Collider>());
            go.AddComponent<LandmarkMarker>().Setup(id, label);
            return go;
        }

        private GameObject CreateMarker(GridMap grid, GridCoord coord, string label)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(transform, false);
            var pos = grid.CoordToWorldCenter(coord);
            pos.y = 0.4f;
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
            go.GetComponent<MeshRenderer>().sharedMaterial = _landmarkMat;
            SafeDestroy(go.GetComponent<Collider>());
            go.name = label;
            return go;
        }

        private static void AddInteract(GameObject go, string prompt, string message)
        {
            go.AddComponent<SimpleInteractable>().Setup(prompt, message);
        }

        private static void AddPoi(GameObject go, PoiShellId id, string message)
        {
            go.AddComponent<PoiShellMarker>().Setup(id, message);
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

        private static Material CreateColorMat(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }
    }
}
