using System.Collections.Generic;
using GameDev.Environment;
using GameDev.Grid;
using GameDev.Input;
using GameDev.Interaction;
using GameDev.Player;
using GameDev.World.Town.Geometry;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameDev.World.Town
{
    /// <summary>
    /// P0 — after greybox + grid bake: spawn player/camera, socket interactables, env markers, S0 dress.
    /// </summary>
    public static class TownPlayableBootstrap
    {
        public const string ContentRootName = "Town_PlayableContent";
        private const string PrefabFolder = "Assets/Prefabs/S0_Greybox/";

        public struct PrefabRefs
        {
            public GameObject PlayerCapsule;
            public GameObject EnvStub;
            public GameObject PropStub;
            public GameObject LampStub;
        }

        public struct Options
        {
            public float PlayableRadiusMetres;
            public bool SpawnPlayer;
            public bool SpawnInteractables;
            public bool SpawnEnvActions;
            public bool ScatterDress;
            public Material RoadMaterial;
            public Material WallMaterial;
            public Material FloorMaterial;
            public Material PlasterMaterial;
            public Material RoofMaterial;
            public Material WoodMaterial;
        }

        public static TownGridBaker.BakeResult Bootstrap(
            TownGenerator generator,
            ref PrefabRefs prefs,
            Options options)
        {
            if (generator == null)
            {
                return default;
            }

            ResolvePrefabs(ref prefs);
            ClearContent(generator.transform);

            var bake = TownGridBaker.Bake(
                generator.transform,
                generator.Model,
                options.PlayableRadiusMetres);

            if (bake.Grid == null || bake.WalkableCount <= 0)
            {
                Debug.LogWarning("[TownPlayable] Grid bake produced no walkable cells.");
                return bake;
            }

            var content = new GameObject(ContentRootName);
            content.transform.SetParent(generator.transform, false);

            GameObject player = null;
            if (options.SpawnPlayer)
            {
                player = SpawnPlayerAndCamera(generator.transform, bake.Grid, bake.Spawn, prefs.PlayerCapsule);
                var occlusion = generator.GetComponent<BuildingOcclusionController>();
                if (occlusion != null && player != null)
                {
                    occlusion.SetTrackTarget(player.transform);
                }

                GameInputService.EnsureExists();
            }

            if (options.SpawnInteractables)
            {
                SpawnSocketInteractables(
                    content.transform,
                    generator.Model,
                    bake.Center,
                    options.PlayableRadiusMetres,
                    prefs);
            }

            if (options.SpawnEnvActions)
            {
                SpawnEnvActions(content.transform, bake.Grid, bake.Spawn, prefs.EnvStub);
            }

            if (options.ScatterDress)
            {
                ScatterProps(content.transform, bake.Grid, bake.Spawn, prefs, generator.Seed);
                ApplyMaterialDress(generator.transform, options);
                EnsureBasicFog();
            }

            return bake;
        }

        public static void ClearContent(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (child.name == ContentRootName ||
                    child.name == "Player" ||
                    child.name == "TacticalCamera" ||
                    child.name.StartsWith("Player_"))
                {
                    SafeDestroy(child.gameObject);
                }
            }

            TownGridBaker.Clear(parent);
        }

        private static GameObject SpawnPlayerAndCamera(
            Transform parent,
            GridMap grid,
            GridCoord spawn,
            GameObject playerPrefab)
        {
            var player = SpawnPrefab(playerPrefab, parent, "Player");
            player.transform.position = grid.CoordToWorldCenter(spawn);
            DisableBlockingColliders(player);

            var playerCtrl = player.GetComponent<GridPlayerController>();
            if (playerCtrl == null)
            {
                playerCtrl = player.AddComponent<GridPlayerController>();
            }

            playerCtrl.PlaceOnGrid(grid, spawn);

            var camGo = new GameObject("TacticalCamera");
            camGo.transform.SetParent(parent, false);
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            var tacticalCam = camGo.AddComponent<SimpleFollowCamera>();
            tacticalCam.SetTarget(player.transform);
            return player;
        }

        private static void SpawnSocketInteractables(
            Transform parent,
            TownModel model,
            Vector2 center,
            float radiusMetres,
            PrefabRefs prefs)
        {
            if (model?.Buildings == null)
            {
                return;
            }

            var radiusSq = radiusMetres * radiusMetres;
            var root = new GameObject("Interactables");
            root.transform.SetParent(parent, false);

            for (var bi = 0; bi < model.Buildings.Count; bi++)
            {
                var building = model.Buildings[bi];
                if (building?.Sockets == null || building.Sockets.Count == 0)
                {
                    continue;
                }

                for (var si = 0; si < building.Sockets.Count; si++)
                {
                    var socket = building.Sockets[si];
                    if ((socket.WorldPos - center).sqrMagnitude > radiusSq)
                    {
                        continue;
                    }

                    if (socket.CellKind == BuildingInteriorCellKind.NpcSocket)
                    {
                        SpawnNpc(root.transform, socket, prefs.PlayerCapsule);
                    }
                    else if (socket.CellKind == BuildingInteriorCellKind.PropSocket)
                    {
                        SpawnProp(root.transform, socket, prefs.PropStub);
                    }
                }
            }
        }

        private static void SpawnNpc(Transform parent, PlacedBuildingSocket socket, GameObject capsulePrefab)
        {
            var go = SpawnPrefab(capsulePrefab, parent, $"NPC_{socket.Role}_{socket.Index}");
            go.transform.position = Geo2.ToVector3(socket.WorldPos, 0f);
            go.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            DisableBlockingColliders(go);
            // Avoid double GridPlayerController on NPC stubs.
            var ctrl = go.GetComponent<GridPlayerController>();
            if (ctrl != null)
            {
                SafeDestroy(ctrl);
            }

            var lines = RoleLines(socket.Role);
            EnsureInteract(go, lines.prompt, lines.message);
        }

        private static void SpawnProp(Transform parent, PlacedBuildingSocket socket, GameObject propPrefab)
        {
            GameObject go;
            if (propPrefab != null)
            {
                go = SpawnPrefab(propPrefab, parent, $"Prop_{socket.Role}_{socket.Index}");
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Prop_{socket.Role}_{socket.Index}";
                go.transform.SetParent(parent, false);
                go.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            }

            go.transform.position = Geo2.ToVector3(socket.WorldPos, 0.3f);
            DisableBlockingColliders(go);
            EnsureInteract(go, $"Inspect {socket.Role}", PropLine(socket.Role));
        }

        private static void SpawnEnvActions(Transform parent, GridMap grid, GridCoord spawn, GameObject stubPrefab)
        {
            var root = new GameObject("EnvActions");
            root.transform.SetParent(parent, false);

            // Vault near spawn on a walkable neighbor.
            if (TryFindWalkableOffset(grid, spawn, 3, 0, out var vault) ||
                TryFindWalkableOffset(grid, spawn, 2, 1, out vault))
            {
                PlaceEnv(root.transform, grid, vault, stubPrefab,
                    EnvironmentActionType.VaultJump, 0.75f,
                    "Vaulted a low fence stub.",
                    "Slipped — noise, wasted effort.",
                    GridCellFlags.Walkable | GridCellFlags.Vaultable);
            }

            // Window: first Window-flagged cell near spawn, or make one.
            if (TryFindFlaggedNear(grid, spawn, GridCellFlags.Window, 40, out var windowCell))
            {
                // Make climbable: open window cell as walkable + Window.
                PlaceEnv(root.transform, grid, windowCell, stubPrefab,
                    EnvironmentActionType.ClimbThroughWindow, 0.65f,
                    "Climbed through a side window.",
                    "Boot scraped the frame — noisy.",
                    GridCellFlags.Walkable | GridCellFlags.Window);
            }
            else if (TryFindWalkableOffset(grid, spawn, -4, 2, out windowCell))
            {
                PlaceEnv(root.transform, grid, windowCell, stubPrefab,
                    EnvironmentActionType.ClimbThroughWindow, 0.65f,
                    "Climbed through a side window.",
                    "Boot scraped the frame — noisy.",
                    GridCellFlags.Walkable | GridCellFlags.Window);
            }

            // Climb ledge stub: slightly farther walkable cell with height band.
            if (TryFindWalkableOffset(grid, spawn, 5, -2, out var ledge) ||
                TryFindWalkableOffset(grid, spawn, 4, 3, out ledge))
            {
                grid.SetHeight(ledge, 1);
                PlaceEnv(root.transform, grid, ledge, stubPrefab,
                    EnvironmentActionType.ClimbUp, 0.7f,
                    "Mantled the ledge.",
                    "Lost grip — drop, Hurt stub.",
                    GridCellFlags.Walkable | GridCellFlags.Climbable | GridCellFlags.Stairs);
            }

            // Door Open/Close on a Door cell near spawn.
            if (TryFindFlaggedNear(grid, spawn, GridCellFlags.Door, 50, out var door))
            {
                PlaceEnv(root.transform, grid, door, stubPrefab,
                    EnvironmentActionType.OpenClose, 0.9f,
                    "Door opens on worn hinges.",
                    "Latch sticks — noise.",
                    GridCellFlags.Walkable | GridCellFlags.Door);
            }
        }

        private static void PlaceEnv(
            Transform parent,
            GridMap grid,
            GridCoord coord,
            GameObject stubPrefab,
            EnvironmentActionType type,
            float chance,
            string ok,
            string fail,
            GridCellFlags flags)
        {
            var go = stubPrefab != null
                ? SpawnPrefab(stubPrefab, parent, $"Env_{type}")
                : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            if (stubPrefab == null)
            {
                go.name = $"Env_{type}";
                go.transform.SetParent(parent, false);
                go.transform.localScale = new Vector3(0.4f, 0.35f, 0.4f);
            }

            go.transform.position = grid.CoordToWorldCenter(coord) + Vector3.up * 0.2f;
            DisableBlockingColliders(go);
            var marker = go.GetComponent<EnvironmentActionMarker>();
            if (marker == null)
            {
                marker = go.AddComponent<EnvironmentActionMarker>();
            }

            marker.Setup(type, chance, ok, fail);
            grid.SetFlags(coord, flags, walkable: true);
        }

        private static void ScatterProps(
            Transform parent,
            GridMap grid,
            GridCoord spawn,
            PrefabRefs prefs,
            int seed)
        {
            var root = new GameObject("DressScatter");
            root.transform.SetParent(parent, false);
            var rng = new System.Random(seed ^ 0x50D5E55);

            // Lamp stubs along walkable ring around spawn.
            var lampCount = 0;
            for (var attempt = 0; attempt < 80 && lampCount < 8; attempt++)
            {
                var ox = rng.Next(-25, 26);
                var oy = rng.Next(-25, 26);
                var c = new GridCoord(spawn.x + ox, spawn.y + oy);
                if (!grid.IsWalkable(c))
                {
                    continue;
                }

                if (Mathf.Abs(ox) + Mathf.Abs(oy) < 8)
                {
                    continue;
                }

                var lamp = prefs.LampStub != null
                    ? SpawnPrefab(prefs.LampStub, root.transform, $"Lamp_{lampCount}")
                    : CreatePole(root.transform, $"Lamp_{lampCount}");
                lamp.transform.position = grid.CoordToWorldCenter(c);
                DisableBlockingColliders(lamp);

                var lightGo = new GameObject("PointLight");
                lightGo.transform.SetParent(lamp.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 2.4f, 0f);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.72f, 0.45f);
                light.intensity = 1.4f;
                light.range = 10f;
                lampCount++;
            }

            // Barrel/crate stubs near spawn.
            for (var i = 0; i < 6; i++)
            {
                if (!TryFindWalkableOffset(grid, spawn, rng.Next(2, 12) * (rng.Next(0, 2) == 0 ? 1 : -1),
                        rng.Next(2, 10) * (rng.Next(0, 2) == 0 ? 1 : -1), out var c))
                {
                    continue;
                }

                var prop = prefs.PropStub != null
                    ? SpawnPrefab(prefs.PropStub, root.transform, $"ScatterProp_{i}")
                    : GameObject.CreatePrimitive(PrimitiveType.Cube);
                if (prefs.PropStub == null)
                {
                    prop.name = $"ScatterProp_{i}";
                    prop.transform.SetParent(root.transform, false);
                    prop.transform.localScale = new Vector3(0.6f, 0.7f, 0.6f);
                }

                prop.transform.position = grid.CoordToWorldCenter(c) + Vector3.up * 0.35f;
                DisableBlockingColliders(prop);
            }
        }

        private static void ApplyMaterialDress(Transform townRoot, Options options)
        {
            if (townRoot == null)
            {
                return;
            }

            var greybox = townRoot.Find(TownGreyboxBaker.RootName);

            if (greybox == null)
            {
                return;
            }

            var renderers = greybox.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null)
                {
                    continue;
                }

                var n = r.gameObject.name;
                var mat = ResolveLibraryMaterial(n, options);
                if (mat != null)
                {
                    r.sharedMaterial = mat;
                }
                else
                {
                    // Fallback tint when library materials not imported yet.
                    ApplyFallbackTint(r, n);
                }
            }
        }

        private static Material ResolveLibraryMaterial(string name, Options options)
        {
            if (name.StartsWith("Road_") || name.StartsWith("Seg_"))
            {
                return options.RoadMaterial;
            }

            if (name.Contains("Roof") || name.Contains("Chimney") || name.Contains("Eave"))
            {
                return options.RoofMaterial != null ? options.RoofMaterial : options.WallMaterial;
            }

            if (name.Contains("Wood") || name.Contains("Beam") || name.Contains("Door"))
            {
                return options.WoodMaterial != null ? options.WoodMaterial : options.FloorMaterial;
            }

            if (name.Contains("Plaster") || name.Contains("Stucco"))
            {
                return options.PlasterMaterial != null ? options.PlasterMaterial : options.WallMaterial;
            }

            if (name.StartsWith("Wall") || name.Contains("Wall_") || name.StartsWith("Building_") ||
                name.Contains("Shell") || name.Contains("Landmark"))
            {
                // Alternate plaster vs brick by name hash for library variety without extra packs.
                if (options.PlasterMaterial != null && (StableNameHash(name) & 1) == 0)
                {
                    return options.PlasterMaterial;
                }

                return options.WallMaterial;
            }

            if (name.Contains("Floor"))
            {
                return options.FloorMaterial != null ? options.FloorMaterial : options.RoadMaterial;
            }

            return null;
        }

        private static int StableNameHash(string s)
        {
            var h = 0;
            if (s == null)
            {
                return 0;
            }

            for (var i = 0; i < s.Length; i++)
            {
                h = unchecked(h * 31 + s[i]);
            }

            return h;
        }

        private static void ApplyFallbackTint(Renderer r, string name)
        {
            if (r.sharedMaterial == null)
            {
                return;
            }

            // Don't instantiate unique mats for every renderer in editor bake — tint via MPB.
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            Color c;
            if (name.StartsWith("Road_") || name.StartsWith("Seg_"))
            {
                c = new Color(0.35f, 0.33f, 0.30f);
            }
            else if (name.Contains("Roof") || name.Contains("Chimney"))
            {
                c = new Color(0.28f, 0.30f, 0.32f);
            }
            else if (name.Contains("Wall") || name.StartsWith("Building_") || name.Contains("Landmark"))
            {
                c = new Color(0.55f, 0.38f, 0.28f);
            }
            else if (name.Contains("Floor") || name.Contains("Door") || name.Contains("Wood"))
            {
                c = new Color(0.42f, 0.40f, 0.36f);
            }
            else
            {
                return;
            }

            block.SetColor("_BaseColor", c);
            block.SetColor("_Color", c);
            r.SetPropertyBlock(block);
        }

        private static void EnsureBasicFog()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.018f;
            RenderSettings.fogColor = new Color(0.45f, 0.48f, 0.52f);
        }

        private static (string prompt, string message) RoleLines(BuildingSocketRole role)
        {
            switch (role)
            {
                case BuildingSocketRole.Keeper:
                    return ("Talk to keeper", "Keeper: Mind the threshold — coin first, questions second.");
                case BuildingSocketRole.Occupant:
                    return ("Talk to occupant", "Occupant: Keep your voice down. Walls here remember.");
                case BuildingSocketRole.Guard:
                    return ("Talk to guard", "Guard: Papers if you linger. Move on.");
                case BuildingSocketRole.Clergy:
                    return ("Talk to clergy", "Clergy: Light a candle if you must. Leave the rest unspoken.");
                case BuildingSocketRole.Scholar:
                    return ("Talk to scholar", "Scholar: Names have weight. Do not spend them cheaply.");
                case BuildingSocketRole.Notice:
                    return ("Read notice", "A damp notice — shift bells, missing names, coal prices.");
                default:
                    return ($"Talk ({role})", $"{role}: A short look. Nothing free.");
            }
        }

        private static string PropLine(BuildingSocketRole role)
        {
            switch (role)
            {
                case BuildingSocketRole.Counter:
                    return "A worn counter. Coins click softer than words.";
                case BuildingSocketRole.Seating:
                    return "A sticky bench. Someone left a half-empty glass.";
                case BuildingSocketRole.Storage:
                    return "Crates and barrels — stamped, not sealed.";
                default:
                    return $"A {role} prop. Ordinary until it isn't.";
            }
        }

        private static bool TryFindWalkableOffset(
            GridMap grid,
            GridCoord origin,
            int dx,
            int dy,
            out GridCoord found)
        {
            found = new GridCoord(origin.x + dx, origin.y + dy);
            if (grid.IsWalkable(found))
            {
                return true;
            }

            for (var r = 1; r <= 4; r++)
            {
                for (var oy = -r; oy <= r; oy++)
                {
                    for (var ox = -r; ox <= r; ox++)
                    {
                        var c = new GridCoord(found.x + ox, found.y + oy);
                        if (grid.IsWalkable(c))
                        {
                            found = c;
                            return true;
                        }
                    }
                }
            }

            found = origin;
            return false;
        }

        private static bool TryFindFlaggedNear(
            GridMap grid,
            GridCoord origin,
            GridCellFlags flag,
            int maxRadius,
            out GridCoord found)
        {
            for (var r = 0; r <= maxRadius; r++)
            {
                for (var y = origin.y - r; y <= origin.y + r; y++)
                {
                    for (var x = origin.x - r; x <= origin.x + r; x++)
                    {
                        if (r > 0 &&
                            Mathf.Abs(x - origin.x) != r &&
                            Mathf.Abs(y - origin.y) != r)
                        {
                            continue;
                        }

                        var c = new GridCoord(x, y);
                        if (!grid.TryGetCell(c, out var cell))
                        {
                            continue;
                        }

                        if ((cell.flags & flag) != 0)
                        {
                            found = c;
                            return true;
                        }
                    }
                }
            }

            found = origin;
            return false;
        }

        private static GameObject CreatePole(Transform parent, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(0.15f, 1.2f, 0.15f);
            return go;
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

        private static GameObject SpawnPrefab(GameObject prefab, Transform parent, string name)
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
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.SetParent(parent, false);
            }

            go.name = name;
            return go;
        }

        private static void ResolvePrefabs(ref PrefabRefs prefs)
        {
#if UNITY_EDITOR
            if (prefs.PlayerCapsule == null)
            {
                prefs.PlayerCapsule =
                    AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "Player_Capsule.prefab");
            }

            if (prefs.EnvStub == null)
            {
                prefs.EnvStub =
                    AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "EnvAction_Stub.prefab");
            }

            if (prefs.PropStub == null)
            {
                prefs.PropStub =
                    AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "Building_Shell_1x1.prefab");
            }

            if (prefs.LampStub == null)
            {
                prefs.LampStub =
                    AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "Landmark_Block.prefab");
            }
#endif
        }

        private static void DisableBlockingColliders(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            var cols = go.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < cols.Length; i++)
            {
                cols[i].enabled = false;
            }
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(obj);
                return;
            }
#endif
            Object.Destroy(obj);
        }
    }
}
