using UnityEngine;

namespace GameDev.World.Buildings.Shell
{
    public sealed class ShellDebugGizmo : MonoBehaviour
    {
        [SerializeField] private string _archetypeId;
        [SerializeField] private string _districtId;
        [SerializeField] private int _seed;
        [SerializeField] private int _plotIndex;
        [SerializeField] private int _floors;
        [SerializeField] private int _wealth;
        [SerializeField] private string _massing;
        [SerializeField] private bool _hasPortal;
        [SerializeField] private Vector3 _boundsCenter;
        [SerializeField] private Vector3 _boundsSize;
        [SerializeField] private Color _streetFrontColor = new Color(0.2f, 0.8f, 1f, 0.9f);

        private StreetContext _street;
        private PlotFootprint _plot;

        public void Configure(ShellInstance shell)
        {
            _archetypeId = shell.Dna.ArchetypeId;
            _districtId = shell.Dna.DistrictId;
            _seed = shell.Dna.Seed;
            _plotIndex = shell.Dna.PlotIndex;
            _floors = shell.Dna.FloorCount;
            _wealth = shell.Dna.Wealth;
            _massing = shell.Dna.Massing.ToString();
            _hasPortal = shell.Portal.HasValue;
            _boundsCenter = shell.LocalBounds.center;
            _boundsSize = shell.LocalBounds.size;
            _street = shell.Street;
            _plot = shell.Plot;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.35f);
            Gizmos.DrawWireCube(_boundsCenter, _boundsSize);

            Gizmos.color = _streetFrontColor;
            if (_street.IsPublicFront(_street.Front))
                Gizmos.DrawLine(new Vector3(0f, 0.1f, 0f), new Vector3(_plot.WidthMeters, 0.1f, 0f));
            if (_street.IsPublicFront(_street.Left))
                Gizmos.DrawLine(new Vector3(0f, 0.1f, 0f), new Vector3(0f, 0.1f, _plot.DepthMeters));

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.TransformPoint(_boundsCenter + Vector3.up * (_boundsSize.y * 0.5f + 0.5f)),
                $"{_archetypeId} w{_wealth} f{_floors} {_massing}\n{_districtId} seed={_seed}/{_plotIndex}"
                + (_hasPortal ? "\nPORTAL" : string.Empty));
#endif
        }
    }
}
