using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameDev.World.Town
{
    /// <summary>
    /// OrganicTown v2 orchestrator — deterministic stage pipeline into <see cref="TownModel"/>.
    /// Milestone A: data + gizmos only (no prefab bake).
    /// </summary>
    public sealed class TownGenerator : MonoBehaviour
    {
        [SerializeField] private int seed = WorldSeed.DefaultPrototypeSeed;
        [SerializeField] private TownProfile profile;
        [SerializeField] private bool generateOnPlay;
        [SerializeField] private bool timeSliced;

        [Header("Gizmos")]
        [SerializeField] private bool drawBoundary = true;
        [SerializeField] private bool drawTerrain = true;
        [SerializeField] private bool drawPortals = true;
        [SerializeField] private bool drawRoads = true;
        [SerializeField] private bool drawDistricts = true;
        [SerializeField] private bool drawBlocks = true;
        [SerializeField] private bool drawPlots = true;
        [SerializeField] private bool drawBuildings = true;
        [SerializeField] private bool drawOpenSpaces = true;
        [SerializeField] private bool drawLandmark = true;

        private readonly TownModel _model = new TownModel();
        private readonly List<string> _validationLog = new List<string>(32);
        private WorldSeed _worldSeed = new WorldSeed();

        public TownModel Model => _model;
        public IReadOnlyList<string> ValidationLog => _validationLog;
        public int Seed => seed;
        public TownProfile Profile => profile;

        public bool DrawBoundary => drawBoundary;
        public bool DrawTerrain => drawTerrain;
        public bool DrawPortals => drawPortals;
        public bool DrawRoads => drawRoads;
        public bool DrawDistricts => drawDistricts;
        public bool DrawBlocks => drawBlocks;
        public bool DrawPlots => drawPlots;
        public bool DrawBuildings => drawBuildings;
        public bool DrawOpenSpaces => drawOpenSpaces;
        public bool DrawLandmark => drawLandmark;

        private void Start()
        {
            if (generateOnPlay && Application.isPlaying)
            {
                if (timeSliced)
                {
                    StartCoroutine(GenerateAsync(seed));
                }
                else
                {
                    Generate(seed);
                }
            }
        }

        public void SetSeed(int newSeed) => seed = newSeed;

        public void SetProfile(TownProfile p) => profile = p;

        public void Generate(int generateSeed)
        {
            seed = generateSeed;
            ResolveProfile();
            _worldSeed.value = seed;
            _model.Reset(seed);
            _validationLog.Clear();

            Stage_SiteAndLandmark.Run(_model, profile, _worldSeed.Fork("site"));
            Stage_PrimaryRoads.Run(_model, profile, _worldSeed.Fork("primaryRoads"));
            Stage_Districts.Run(_model, profile, _worldSeed.Fork("districts"));
            Stage_Streets.Run(_model, profile, _worldSeed.Fork("streets"));
            Stage_Plots.Run(_model, profile, _worldSeed.Fork("plots"));
            Stage_Buildings.Run(_model, profile, _worldSeed.Fork("buildings"));
            Stage_OpenSpaces.Run(_model, profile, _worldSeed.Fork("openSpaces"));
            Stage_ConnectivityAndScale.Run(_model, profile, _worldSeed.Fork("connectivity"), _validationLog);

            Debug.Log(
                $"[Town] seed={seed} portals={_model.Portals.Count} roads={_model.RoadEdges.Count} " +
                $"districts={_model.Districts.Count} blocks={_model.Blocks.Count} plots={_model.Plots.Count} " +
                $"buildings={_model.Buildings.Count} warnings={_validationLog.Count}");
        }

        public IEnumerator GenerateAsync(int generateSeed)
        {
            seed = generateSeed;
            ResolveProfile();
            _worldSeed.value = seed;
            _model.Reset(seed);
            _validationLog.Clear();

            Stage_SiteAndLandmark.Run(_model, profile, _worldSeed.Fork("site"));
            yield return null;
            Stage_PrimaryRoads.Run(_model, profile, _worldSeed.Fork("primaryRoads"));
            yield return null;
            Stage_Districts.Run(_model, profile, _worldSeed.Fork("districts"));
            yield return null;
            Stage_Streets.Run(_model, profile, _worldSeed.Fork("streets"));
            yield return null;
            Stage_Plots.Run(_model, profile, _worldSeed.Fork("plots"));
            yield return null;
            Stage_Buildings.Run(_model, profile, _worldSeed.Fork("buildings"));
            yield return null;
            Stage_OpenSpaces.Run(_model, profile, _worldSeed.Fork("openSpaces"));
            yield return null;
            Stage_ConnectivityAndScale.Run(_model, profile, _worldSeed.Fork("connectivity"), _validationLog);

            Debug.Log($"[Town] async done seed={seed} buildings={_model.Buildings.Count}");
        }

        public void ClearModel()
        {
            _model.Reset(seed);
            _validationLog.Clear();
        }

        private void ResolveProfile()
        {
            if (profile != null)
            {
                return;
            }

#if UNITY_EDITOR
            profile = AssetDatabase.LoadAssetAtPath<TownProfile>("Assets/Settings/DefaultTownProfile.asset");
#endif
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<TownProfile>();
                Debug.LogWarning("[Town] No TownProfile assigned — using runtime defaults.");
            }
        }

        private void OnDrawGizmos()
        {
            TownGizmoDrawer.Draw(this);
        }
    }
}
