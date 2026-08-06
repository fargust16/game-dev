using System.Collections.Generic;
using GameDev.Player;
using UnityEngine;

namespace GameDev.World.Town
{
    /// <summary>How exteriors are suppressed while inside a volume. Append-only.</summary>
    public enum BuildingOcclusionMode : byte
    {
        /// <summary>V1 greybox: disable wall/shell renderers.</summary>
        HideWalls = 0,
        /// <summary>Reserved: transparent materials / x-ray (needs art pass).</summary>
        FadeMaterials = 1
    }

    /// <summary>
    /// D2 — BG3-like interior readability: while the tracked subject is inside a
    /// building volume, hide that building's walls/shells. Floors and socket markers stay.
    /// </summary>
    public sealed class BuildingOcclusionController : MonoBehaviour
    {
        [SerializeField] private TownGenerator townGenerator;
        [SerializeField] private Transform trackTarget;
        [SerializeField] private BuildingOcclusionMode mode = BuildingOcclusionMode.HideWalls;
        [SerializeField] private float sampleHeight = 1.0f;

        private readonly List<BuildingVisualRoot> _roots = new List<BuildingVisualRoot>(256);
        private readonly List<BuildingVolume> _volumes = new List<BuildingVolume>(256);
        private int _activeBuildingIndex = -1;
        private BuildingVisualRoot _activeRoot;
        private SimpleFollowCamera _camera;

        public int ActiveBuildingIndex => _activeBuildingIndex;
        public BuildingOcclusionMode Mode => mode;
        public int VolumeCount => _volumes.Count;
        public int VisualRootCount => _roots.Count;

        public void SetTownGenerator(TownGenerator generator)
        {
            townGenerator = generator;
            RefreshFromTown();
        }

        public void SetTrackTarget(Transform target) => trackTarget = target;

        /// <summary>Call after greybox bake or model regenerate.</summary>
        public void RefreshFromTown()
        {
            RestoreActive();
            _roots.Clear();
            _volumes.Clear();
            _activeBuildingIndex = -1;
            _activeRoot = null;

            if (townGenerator == null)
            {
                townGenerator = FindFirstObjectByType<TownGenerator>();
            }

            if (townGenerator == null)
            {
                return;
            }

            townGenerator.GetComponentsInChildren(true, _roots);

            var model = townGenerator.Model;
            if (model == null)
            {
                return;
            }

            for (var i = 0; i < model.Buildings.Count; i++)
            {
                _volumes.Add(BuildingVolume.FromBuilding(i, model.Buildings[i]));
            }

            _camera = FindFirstObjectByType<SimpleFollowCamera>();
        }

        /// <summary>Test helper / tools: which building contains this world position.</summary>
        public int FindContainingBuildingIndex(Vector3 worldPos)
        {
            for (var i = 0; i < _volumes.Count; i++)
            {
                if (_volumes[i].Contains(worldPos))
                {
                    return _volumes[i].BuildingIndex;
                }
            }

            return -1;
        }

        private void Awake()
        {
            if (townGenerator == null)
            {
                townGenerator = GetComponent<TownGenerator>();
            }
        }

        private void Start()
        {
            RefreshFromTown();
        }

        private void OnDisable()
        {
            RestoreActive();
        }

        private void LateUpdate()
        {
            if (_volumes.Count == 0)
            {
                return;
            }

            // FadeMaterials reserved — HideWalls until transparent art path exists.
            _ = mode;

            var pos = ResolveTrackPosition();
            var inside = FindContainingBuildingIndex(pos);
            if (inside == _activeBuildingIndex)
            {
                return;
            }

            RestoreActive();
            if (inside >= 0)
            {
                ApplyHidden(inside);
            }
        }

        private Vector3 ResolveTrackPosition()
        {
            if (trackTarget != null)
            {
                return trackTarget.position;
            }

            if (_camera == null)
            {
                _camera = FindFirstObjectByType<SimpleFollowCamera>();
            }

            if (_camera != null)
            {
                return _camera.GetOcclusionSamplePosition(sampleHeight);
            }

            return new Vector3(0f, sampleHeight, 0f);
        }

        private void ApplyHidden(int buildingIndex)
        {
            _activeBuildingIndex = buildingIndex;
            _activeRoot = FindRoot(buildingIndex);
            if (_activeRoot != null)
            {
                _activeRoot.ApplyOcclusionHidden();
            }
        }

        private void RestoreActive()
        {
            if (_activeRoot != null)
            {
                _activeRoot.RestoreOcclusion();
            }

            _activeRoot = null;
            _activeBuildingIndex = -1;
        }

        private BuildingVisualRoot FindRoot(int buildingIndex)
        {
            for (var i = 0; i < _roots.Count; i++)
            {
                var root = _roots[i];
                if (root != null && root.BuildingIndex == buildingIndex)
                {
                    return root;
                }
            }

            return null;
        }
    }
}
