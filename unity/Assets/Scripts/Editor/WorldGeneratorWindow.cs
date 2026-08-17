#if UNITY_EDITOR
using GameDev.World;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// World Generator panel: seed, Copy / Regenerate / GENERATE WORLD, subsystem foldouts,
    /// pipeline step checkboxes, and live World Statistics from <see cref="OverworldWorldState"/>.
    /// </summary>
    public sealed class WorldGeneratorWindow : EditorWindow
    {
        private const string PrefsSeed = "GameDev.WorldGen.Seed";
        private const string PrefsSettingsJson = "GameDev.WorldGen.SettingsJson";

        private int seed = OverworldTerrainMenu.DefaultSeed;
        private WorldGenSettings settings = WorldGenSettings.Default;
        private Vector2 scroll;
        private bool foldTerrain = true;
        private bool foldRoads = true;
        private bool foldPoi = true;
        private bool foldVegetation;
        private bool foldSteps = true;

        [MenuItem("GameDev/Overworld/World Generator")]
        public static void Open()
        {
            var win = GetWindow<WorldGeneratorWindow>("World Generator");
            win.minSize = new Vector2(320f, 480f);
            win.LoadPrefs();
            win.Show();
        }

        private void OnEnable() => LoadPrefs();

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("WORLD GENERATOR", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);
            DrawSeedSection();
            EditorGUILayout.Space(8f);
            if (GUILayout.Button("GENERATE WORLD", GUILayout.Height(36f)))
            {
                GenerateWorld();
            }

            EditorGUILayout.Space(10f);
            DrawStepCheckboxes();
            EditorGUILayout.Space(10f);
            DrawSettingsFoldouts();
            EditorGUILayout.Space(10f);
            DrawStatistics();
            EditorGUILayout.Space(8f);
            DrawPoiLegend();
            EditorGUILayout.EndScrollView();
            if (GUI.changed)
            {
                SavePrefs();
                OverworldTerrainMenu.ActiveSettings = settings;
            }
        }

        private void DrawSeedSection()
        {
            EditorGUILayout.LabelField("SEED", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            seed = EditorGUILayout.IntField(seed);
            if (GUILayout.Button("Copy Seed", GUILayout.Width(88f)))
            {
                EditorGUIUtility.systemCopyBuffer = seed.ToString();
                Debug.Log($"[WorldGen] Seed copied: {seed}");
            }

            if (GUILayout.Button("Regenerate", GUILayout.Width(88f)))
            {
                GenerateWorld();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                "Same Seed + same settings → identical geography. Regenerate keeps the field seed. " +
                "Step checkboxes are not seeds — Mountains off yields a stable map without ridges.",
                MessageType.Info);
        }

        private void DrawStepCheckboxes()
        {
            foldSteps = EditorGUILayout.Foldout(foldSteps, "PIPELINE STEPS", true);
            if (!foldSteps)
            {
                return;
            }

            EditorGUI.indentLevel++;
            settings.RunSurface = EditorGUILayout.Toggle("Surface", settings.RunSurface);
            settings.RunZones = EditorGUILayout.Toggle("Zones", settings.RunZones);
            settings.RunMountains = EditorGUILayout.Toggle("Mountains", settings.RunMountains);
            settings.RunMarkers = EditorGUILayout.Toggle("Markers", settings.RunMarkers);
            settings.RunSettlements = EditorGUILayout.Toggle("Settlements", settings.RunSettlements);
            settings.RunRoads = EditorGUILayout.Toggle("Roads", settings.RunRoads);
            settings.RunPlayer = EditorGUILayout.Toggle("Player", settings.RunPlayer);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("All On"))
            {
                SetAllSteps(true);
            }

            if (GUILayout.Button("All Off"))
            {
                SetAllSteps(false);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        private void SetAllSteps(bool on)
        {
            settings.RunSurface = on;
            settings.RunZones = on;
            settings.RunMountains = on;
            settings.RunMarkers = on;
            settings.RunSettlements = on;
            settings.RunRoads = on;
            settings.RunPlayer = on;
        }

        private void DrawSettingsFoldouts()
        {
            foldTerrain = EditorGUILayout.Foldout(foldTerrain, "TERRAIN", true);
            if (foldTerrain)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "Terrain shape comes from Vista / Unity Terrain (seeded forks). " +
                    "Mountains / Zones toggles rebuild with TerrainBakeFlags.",
                    MessageType.None);
                EditorGUI.indentLevel--;
            }

            foldRoads = EditorGUILayout.Foldout(foldRoads, "ROADS", true);
            if (foldRoads)
            {
                EditorGUI.indentLevel++;
                settings.PrimaryRoadCostWeight = EditorGUILayout.Slider(
                    "Primary Road Cost", settings.PrimaryRoadCostWeight, 0.25f, 3f);
                settings.SecondaryRoadCostWeight = EditorGUILayout.Slider(
                    "Secondary Road Cost", settings.SecondaryRoadCostWeight, 0.25f, 3f);
                settings.MaxGrade = EditorGUILayout.Slider("Max Grade", settings.MaxGrade, 0.02f, 0.35f);
                settings.MaxCutDepthMeters = EditorGUILayout.Slider(
                    "Max Cut Depth", settings.MaxCutDepthMeters, 0.25f, 8f);
                settings.MaxFillHeightMeters = EditorGUILayout.Slider(
                    "Max Fill Height", settings.MaxFillHeightMeters, 0.25f, 8f);
                settings.TurnRadiusMeters = EditorGUILayout.Slider(
                    "Turn Radius", settings.TurnRadiusMeters, 4f, 80f);
                settings.ParallelRoadPenalty = EditorGUILayout.Slider(
                    "Parallel Road Penalty", settings.ParallelRoadPenalty, 0.5f, 12f);
                EditorGUI.indentLevel--;
            }

            foldPoi = EditorGUILayout.Foldout(foldPoi, "POI", true);
            if (foldPoi)
            {
                EditorGUI.indentLevel++;
                settings.TownCount = EditorGUILayout.IntSlider("Town Count", settings.TownCount, 1, 12);
                settings.ShrineCount = EditorGUILayout.IntSlider("Shrine Count", settings.ShrineCount, 0, 20);
                settings.SmallCampCount = EditorGUILayout.IntSlider(
                    "Small Camp Count", settings.SmallCampCount, 0, 40);
                settings.LandmarkCount = EditorGUILayout.IntSlider(
                    "Landmark Count", settings.LandmarkCount, 0, 20);
                settings.MinimumPoiDistanceMeters = EditorGUILayout.Slider(
                    "Minimum POI Distance", settings.MinimumPoiDistanceMeters, 16f, 400f);
                EditorGUI.indentLevel--;
            }

            foldVegetation = EditorGUILayout.Foldout(foldVegetation, "VEGETATION", true);
            if (foldVegetation)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "Trees use Fork(\"vegetation\"); undergrowth Fork(\"vegetation.shrubs\"); " +
                    "grass Fork(\"grass\"). Clear trees does not clear grass.",
                    MessageType.None);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawStatistics()
        {
            EditorGUILayout.LabelField("CURRENT WORLD", EditorStyles.boldLabel);
            var def = OverworldWorldState.Find()?.Definition;
            if (def == null)
            {
                EditorGUILayout.HelpBox("No world generated yet.", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField("Seed:", def.Seed.ToString());
            EditorGUILayout.LabelField("Roads:", $"{def.RoadLengthKilometers:F1} km");
            EditorGUILayout.LabelField("POI:", def.PoiCount.ToString());
        }

        private static void DrawPoiLegend()
        {
            EditorGUILayout.LabelField("POI LEGEND", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("● TownApproach  (settlement hub)");
            EditorGUILayout.LabelField("◆ Landmark");
            EditorGUILayout.LabelField("▲ Shrine");
            EditorGUILayout.LabelField("▼ Small Camp");
        }

        private void GenerateWorld()
        {
            settings.Clamp();
            SavePrefs();
            OverworldTerrainMenu.ActiveSettings = settings;
            OverworldPipeline.GenerateAll(seed, settings);
            Repaint();
        }

        private void LoadPrefs()
        {
            seed = EditorPrefs.GetInt(PrefsSeed, OverworldTerrainMenu.DefaultSeed);
            var json = EditorPrefs.GetString(PrefsSettingsJson, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    settings = JsonUtility.FromJson<WorldGenSettings>(json);
                }
                catch
                {
                    settings = WorldGenSettings.Default;
                }
            }
            else
            {
                settings = WorldGenSettings.Default;
            }

            // Older prefs without step flags deserialize as false — treat all-false as Default on.
            if (!settings.RunSurface && !settings.RunRoads && !settings.RunPlayer)
            {
                var d = WorldGenSettings.Default;
                settings.RunSurface = d.RunSurface;
                settings.RunZones = d.RunZones;
                settings.RunMountains = d.RunMountains;
                settings.RunMarkers = d.RunMarkers;
                settings.RunSettlements = d.RunSettlements;
                settings.RunRoads = d.RunRoads;
                settings.RunPlayer = d.RunPlayer;
            }

            settings.Clamp();
            OverworldTerrainMenu.ActiveSettings = settings;
        }

        private void SavePrefs()
        {
            EditorPrefs.SetInt(PrefsSeed, seed);
            EditorPrefs.SetString(PrefsSettingsJson, JsonUtility.ToJson(settings));
        }
    }
}
#endif
