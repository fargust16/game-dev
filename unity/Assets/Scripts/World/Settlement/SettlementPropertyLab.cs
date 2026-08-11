using System.Collections.Generic;
using GameDev.World.Buildings.Shell;
using UnityEngine;

namespace GameDev.World.Settlement
{
    public enum SettlementLabBlockMode
    {
        SingleStrip = 0,
        RearLane = 1,
        DoubleFrontage = 2,
        StreetSpine = 3,
        DistrictMix = 4
    }

    /// <summary>
    /// Editor/runtime lab host: generates a property block or street-spine fabric,
    /// draws zone/street gizmos, spawns shells + features.
    /// </summary>
    public sealed class SettlementPropertyLab : MonoBehaviour
    {
        [SerializeField] private int _seed = 110201;
        [SerializeField] private DistrictKind _district = DistrictKind.Worker;
        [SerializeField] private int _plotCount = 8;
        [SerializeField] private int _blockCount = 2;
        [SerializeField] private SettlementLabBlockMode _blockMode = SettlementLabBlockMode.SingleStrip;
        [SerializeField] private float _laneWidthMeters = SettlementBlockLayout.DefaultLaneWidthMeters;
        [SerializeField] private float _streetWidthMeters = SettlementFabricLayout.DefaultStreetWidthMeters;
        [SerializeField] private bool _spawnShells = true;
        [SerializeField] private bool _spawnFeatures = true;

        private ISettlementPropertyLayout _layout;
        private GameObject _shellRoot;
        private GameObject _featureRoot;

        public int Seed => _seed;
        public DistrictKind District => _district;
        public SettlementLabBlockMode BlockMode => _blockMode;
        public ISettlementPropertyLayout Layout => _layout;
        public SettlementFabricLayout Fabric => _layout as SettlementFabricLayout;

        public void SetSeed(int seed) => _seed = seed;

        public void SetDistrict(DistrictKind kind) => _district = kind;

        public void SetPlotCount(int count) => _plotCount = Mathf.Clamp(count, 1, 32);

        public void SetBlockCount(int count) => _blockCount = Mathf.Clamp(count, 1, 4);

        public void SetBlockMode(SettlementLabBlockMode mode) => _blockMode = mode;

        public void SetLaneWidth(float meters) => _laneWidthMeters = Mathf.Max(2f, meters);

        public void SetStreetWidth(float meters) => _streetWidthMeters = Mathf.Max(4f, meters);

        public void Generate()
        {
            ClearVisuals();
            var count = EffectivePlotCount();
            switch (_blockMode)
            {
                case SettlementLabBlockMode.RearLane:
                    _layout = SettlementBlockLayout.CreateWithRearLane(
                        _seed, _district, count, _laneWidthMeters);
                    break;
                case SettlementLabBlockMode.DoubleFrontage:
                    _layout = SettlementBlockLayout.CreateDoubleFrontage(
                        _seed, _district, count, _laneWidthMeters);
                    break;
                case SettlementLabBlockMode.StreetSpine:
                    _layout = SettlementFabricLayout.CreateStreetSpine(
                        _seed,
                        _district,
                        _blockCount,
                        count,
                        _streetWidthMeters,
                        SettlementFabricLayout.DefaultCrossStreetWidthMeters,
                        _laneWidthMeters);
                    break;
                case SettlementLabBlockMode.DistrictMix:
                    _layout = SettlementFabricLayout.CreateDistrictMix(
                        _seed,
                        Mathf.Clamp(count, 2, 4),
                        _streetWidthMeters,
                        SettlementFabricLayout.DefaultCrossStreetWidthMeters,
                        _laneWidthMeters);
                    break;
                default:
                    _layout = SettlementBlockLayout.Create(_seed, _district, count);
                    break;
            }

            if (_spawnShells)
            {
                var archetypes = ShellCatalogFactory.CreateDefaultArchetypes();
                var districts = ShellCatalogFactory.CreateDefaultDistricts();
                var binder = new BuildingShellBinder();
                var bridge = new SettlementShellBridge(binder, archetypes, districts);
                _shellRoot = bridge.SpawnProperties(_layout, _seed, transform);
                _shellRoot.name = "PropertyShellRoot";
            }

            if (_spawnFeatures)
            {
                _featureRoot = PropertyFeatureSpawner.SpawnAll(_layout, transform);
                _featureRoot.name = "PropertyFeatureRoot";
            }
        }

        public void Clear()
        {
            ClearVisuals();
            _layout = null;
        }

        public void ReseedAndGenerate()
        {
            _seed = unchecked(_seed * 1103515245 + 12345);
            if (_seed == int.MinValue)
                _seed = 1;
            Generate();
        }

        private int EffectivePlotCount()
        {
            if (_district == DistrictKind.Elite && _plotCount > 4)
                return 3;
            return _plotCount;
        }

        private void ClearVisuals()
        {
            DestroyChild(ref _shellRoot);
            DestroyChild(ref _featureRoot);

            var stale = new List<GameObject>();
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).gameObject;
                if (child.name.StartsWith("PropertyShellRoot")
                    || child.name.StartsWith("SettlementProperties_")
                    || child.name.StartsWith("PropertyFeatureRoot")
                    || child.name.StartsWith("PropertyFeatures_"))
                {
                    stale.Add(child);
                }
            }

            for (var i = 0; i < stale.Count; i++)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(stale[i]);
                else
#endif
                    Destroy(stale[i]);
            }
        }

        private void DestroyChild(ref GameObject go)
        {
            if (go == null)
                return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(go);
            else
#endif
                Destroy(go);
            go = null;
        }

        private void OnDrawGizmos()
        {
            if (_layout == null || _layout.Properties == null)
                return;

            for (var i = 0; i < _layout.Properties.Count; i++)
                DrawProperty(_layout.Properties[i]);

            if (_layout is SettlementBlockLayout block && block.HasRearLane)
                DrawLane(block.Lane);

            var fabric = _layout as SettlementFabricLayout;
            if (fabric != null)
            {
                for (var i = 0; i < fabric.Lanes.Count; i++)
                    DrawLane(fabric.Lanes[i]);
                for (var i = 0; i < fabric.Streets.Count; i++)
                    DrawStreet(fabric.Streets[i]);
            }
        }

        private static void DrawStreet(SettlementStreetSpec street)
        {
            if (street.WidthMeters <= 0.01f || street.LengthMeters <= 0.01f)
                return;
            var fill = street.Kind == SettlementStreetKind.Primary
                ? new Color(0.35f, 0.35f, 0.4f, 0.45f)
                : new Color(0.4f, 0.38f, 0.32f, 0.4f);
            var wire = street.Kind == SettlementStreetKind.Primary
                ? new Color(0.7f, 0.7f, 0.8f)
                : new Color(0.75f, 0.65f, 0.45f);
            DrawRect(street.Origin, street.LengthMeters, street.WidthMeters, street.YawDegrees, fill);
            DrawRectWire(street.Origin, street.LengthMeters, street.WidthMeters, street.YawDegrees, wire);

            Gizmos.color = wire;
            Gizmos.DrawLine(street.CenterlineStart + Vector3.up * 0.15f, street.CenterlineEnd + Vector3.up * 0.15f);
        }

        private static void DrawLane(SettlementLaneSpec lane)
        {
            if (lane.WidthMeters <= 0.01f || lane.LengthMeters <= 0.01f)
                return;
            DrawRect(
                lane.Origin,
                lane.LengthMeters,
                lane.WidthMeters,
                lane.YawDegrees,
                new Color(0.55f, 0.45f, 0.25f, 0.4f));
            DrawRectWire(
                lane.Origin,
                lane.LengthMeters,
                lane.WidthMeters,
                lane.YawDegrees,
                new Color(0.85f, 0.7f, 0.35f));
        }

        private static void DrawProperty(PropertySpec prop)
        {
            var parcel = prop.Parcel;
            DrawRect(parcel.Origin, parcel.WidthMeters, parcel.DepthMeters, parcel.YawDegrees, new Color(1f, 1f, 1f, 0.35f));

            for (var z = 0; z < prop.Zones.Count; z++)
            {
                var zone = prop.Zones[z];
                DrawRect(zone.Origin, zone.WidthMeters, zone.DepthMeters, zone.YawDegrees, ColorFor(zone.Kind));
            }

            var pad = prop.BuildingPad;
            DrawRectWire(pad.Origin, pad.WidthMeters, pad.DepthMeters, pad.YawDegrees, Color.yellow);

            if (prop.HasSecondaryBuilding)
            {
                var sec = prop.SecondaryBuildingPad;
                DrawRectWire(sec.Origin, sec.WidthMeters, sec.DepthMeters, sec.YawDegrees, new Color(1f, 0.6f, 0.2f));
            }

            for (var b = 0; b < prop.Boundaries.Count; b++)
                DrawBoundary(prop.Boundaries[b]);
        }

        private static void DrawBoundary(PropertyBoundarySegment seg)
        {
            var lift = Vector3.up * seg.HeightMeters;
            Gizmos.color = seg.IsGate ? new Color(1f, 0.75f, 0.2f) : new Color(0.2f, 0.2f, 0.25f);
            Gizmos.DrawLine(seg.StartWorld + lift, seg.EndWorld + lift);
        }

        private static Color ColorFor(PropertyZoneKind kind)
        {
            switch (kind)
            {
                case PropertyZoneKind.FrontGarden: return new Color(0.35f, 0.75f, 0.35f, 0.35f);
                case PropertyZoneKind.RearYard: return new Color(0.45f, 0.65f, 0.3f, 0.3f);
                case PropertyZoneKind.Service: return new Color(0.75f, 0.55f, 0.25f, 0.35f);
                case PropertyZoneKind.Drive: return new Color(0.4f, 0.4f, 0.45f, 0.35f);
                case PropertyZoneKind.BuildingPad: return new Color(0.85f, 0.35f, 0.25f, 0.25f);
                case PropertyZoneKind.SecondaryPad: return new Color(0.7f, 0.4f, 0.2f, 0.35f);
                case PropertyZoneKind.OpenSpace: return new Color(0.3f, 0.7f, 0.4f, 0.4f);
                default: return new Color(0.6f, 0.6f, 0.6f, 0.2f);
            }
        }

        private static void DrawRect(Vector3 origin, float width, float depth, float yawDeg, Color color)
        {
            var yaw = Quaternion.Euler(0f, yawDeg, 0f);
            var center = origin + yaw * new Vector3(width * 0.5f, 0.05f, depth * 0.5f);
            Gizmos.color = color;
            Gizmos.matrix = Matrix4x4.TRS(center, yaw, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, new Vector3(width, 0.08f, depth));
            Gizmos.matrix = Matrix4x4.identity;
        }

        private static void DrawRectWire(Vector3 origin, float width, float depth, float yawDeg, Color color)
        {
            var yaw = Quaternion.Euler(0f, yawDeg, 0f);
            var center = origin + yaw * new Vector3(width * 0.5f, 0.12f, depth * 0.5f);
            Gizmos.color = color;
            Gizmos.matrix = Matrix4x4.TRS(center, yaw, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, 0.2f, depth));
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
