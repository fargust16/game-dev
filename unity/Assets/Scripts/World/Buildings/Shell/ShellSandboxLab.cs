using System.Collections.Generic;
using GameDev.World.Seed;
using UnityEngine;

namespace GameDev.World.Buildings.Shell
{
    /// <summary>
    /// Editor/play lab: street strip of exterior shells driven by district profile.
    /// </summary>
    public sealed class ShellSandboxLab : MonoBehaviour
    {
        public const string RootName = "ShellSandboxRoot";

        public enum GenerateMode
        {
            SingleDistrict = 0,
            CompareAll = 1
        }

        [SerializeField] private int _seed = 42;
        [SerializeField] private int _buildingCount = 16;
        [SerializeField] private DistrictKind _district = DistrictKind.Worker;
        [SerializeField] private GenerateMode _mode = GenerateMode.SingleDistrict;
        [SerializeField] private bool _includeCorners = true;
        [SerializeField] private bool _showDebugLabels = true;

        private readonly BuildingShellBinder _binder = new BuildingShellBinder();
        private List<BuildingArchetypeDef> _archetypes;
        private List<DistrictProfileDef> _districts;
        private readonly List<ShellInstance> _lastShells = new List<ShellInstance>();

        public int Seed => _seed;
        public DistrictKind District => _district;
        public IReadOnlyList<ShellInstance> LastShells => _lastShells;

        public void SetSeed(int seed) => _seed = seed;

        public void SetDistrict(DistrictKind kind)
        {
            _district = kind;
            _mode = GenerateMode.SingleDistrict;
        }

        public void SetCompareAll()
        {
            _mode = GenerateMode.CompareAll;
        }

        public void Generate()
        {
            Clear();
            EnsureCatalog();

            var root = new GameObject(RootName);
            root.transform.SetParent(transform, false);
            _lastShells.Clear();

            if (_mode == GenerateMode.CompareAll)
                GenerateCompareAll(root.transform);
            else
                GenerateSingle(root.transform, FindDistrict(DistrictIds.FromKind(_district)), 0f, _buildingCount, 0);

            if (!_showDebugLabels)
            {
                var gizmos = root.GetComponentsInChildren<ShellDebugGizmo>();
                for (var i = 0; i < gizmos.Length; i++)
                    gizmos[i].enabled = false;
            }

            var withOpenings = 0;
            for (var i = 0; i < _lastShells.Count; i++)
            {
                var pieces = _lastShells[i].Pieces;
                for (var p = 0; p < pieces.Count; p++)
                {
                    var c = pieces[p].Category;
                    if (c == ExteriorPieceCategory.Window || c == ExteriorPieceCategory.Door
                        || c == ExteriorPieceCategory.Shopfront || c == ExteriorPieceCategory.AtticWindow)
                    {
                        withOpenings++;
                        break;
                    }
                }
            }

            Debug.Log(
                $"[ShellSandbox] Generated {_lastShells.Count} shells | seed={_seed} | mode={_mode} | district={_district} "
                + $"| assembly=FLOOR_STACK_V2 | shellsWithOpenings={withOpenings}/{_lastShells.Count}");
        }

        public void Clear()
        {
            _lastShells.Clear();
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name != RootName && !child.name.StartsWith("SettlementStubRoot"))
                    continue;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(child.gameObject);
                else
                    Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }

        public void ReseedAndGenerate()
        {
            _seed = unchecked(_seed * 397) ^ 110201;
            Generate();
        }

        private void GenerateCompareAll(Transform root)
        {
            var kinds = new[]
            {
                DistrictKind.Poor,
                DistrictKind.Worker,
                DistrictKind.Commercial,
                DistrictKind.Affluent,
                DistrictKind.Rich,
                DistrictKind.Elite
            };
            var z = 0f;
            var perRow = Mathf.Max(6, _buildingCount / 2);
            for (var k = 0; k < kinds.Length; k++)
            {
                var district = FindDistrict(DistrictIds.FromKind(kinds[k]));
                var row = new GameObject($"Row_{district.Id}");
                row.transform.SetParent(root, false);
                row.transform.localPosition = new Vector3(0f, 0f, z);
                GenerateSingle(row.transform, district, 0f, perRow, k * 100);
                z += 18f;
            }
        }

        private void GenerateSingle(
            Transform parent,
            DistrictProfileDef district,
            float startX,
            int count,
            int indexOffset)
        {
            if (district == null)
                return;

            var selector = new ShellArchetypeSelector(_archetypes);
            var worldSeed = new WorldSeed(_seed);
            var cursorX = startX;
            var gap = district.EffectiveGapMeters();
            var cornerEvery = Mathf.Max(3, district.CornerEveryN);

            for (var i = 0; i < count; i++)
            {
                var plotIndex = indexOffset + i;
                var plotRng = worldSeed.Fork($"plot_{district.Id}_{plotIndex}");
                var width = district.RollWidthBays(plotRng);
                var depth = district.RollDepthBays(plotRng);
                var isCorner = _includeCorners && (i % cornerEvery == cornerEvery - 1);
                var origin = new Vector3(cursorX, 0f, 0f);
                var plot = isCorner
                    ? PlotFootprint.Corner(width, depth, origin)
                    : PlotFootprint.Rect(width, depth, origin);
                var street = isCorner
                    ? StreetContext.CornerFrontLeft()
                    : StreetContext.StreetFrontYardBack();

                var archRng = worldSeed.Fork($"arch_{district.Id}_{plotIndex}");
                var archetype = selector.Select(plot, district, archRng);
                if (archetype == null)
                    continue;

                // If shop-biased pick made a plot too small for Shop, re-roll width once.
                if (!archetype.FitsPlot(plot))
                {
                    width = Mathf.Clamp(width + 1, district.MinWidthBays, district.MaxWidthBays);
                    plot = isCorner
                        ? PlotFootprint.Corner(width, depth, origin)
                        : PlotFootprint.Rect(width, depth, origin);
                    archetype = selector.Select(plot, district, archRng);
                    if (archetype == null)
                        continue;
                }

                var dnaRng = worldSeed.Fork($"dna_{district.Id}_{plotIndex}");
                var dna = ShellDnaSampler.Sample(
                    archetype, district, plot, street, _seed, plotIndex, dnaRng);
                var shell = _binder.Bind(dna, plot, street, archetype, district);
                _lastShells.Add(shell);

                var name = $"Shell_{plotIndex:00}_{dna.ArchetypeId}_{district.Id}";
                ShellPlaceholderFactory.SpawnShellRoot(shell, parent, name);
                cursorX += plot.WidthMeters + gap;
            }
        }

        private DistrictProfileDef FindDistrict(string id)
        {
            for (var i = 0; i < _districts.Count; i++)
            {
                if (_districts[i] != null && _districts[i].Id == id)
                    return _districts[i];
            }

            return _districts.Count > 0 ? _districts[0] : null;
        }

        private void EnsureCatalog()
        {
            if (_archetypes == null || _archetypes.Count == 0)
                _archetypes = ShellCatalogFactory.CreateDefaultArchetypes();
            if (_districts == null || _districts.Count == 0)
                _districts = ShellCatalogFactory.CreateDefaultDistricts();
        }
    }
}
