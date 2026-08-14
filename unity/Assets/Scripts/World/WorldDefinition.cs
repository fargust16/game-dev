using System;
using System.Collections.Generic;
using GameDev.World.Poi;
using GameDev.World.Roads;
using GameDev.World.Water;
using UnityEngine;

namespace GameDev.World
{
    /// <summary>
    /// Aggregate snapshot of one overworld generation. Downstream systems read this instead of
    /// re-running placers. Not a ScriptableObject — mutable only during generate, then held
    /// by <see cref="OverworldWorldState"/>.
    /// </summary>
    public sealed class WorldDefinition
    {
        public int Seed { get; set; }
        public WorldGenSettings Settings { get; set; }

        public IReadOnlyList<LakeDefinition> Lakes { get; private set; } =
            Array.Empty<LakeDefinition>();

        public IReadOnlyList<PoiMarker> Pois { get; private set; } =
            Array.Empty<PoiMarker>();

        public IReadOnlyList<RoadDefinition> Roads { get; private set; } =
            Array.Empty<RoadDefinition>();

        public int LakeCount => Lakes.Count;
        public int PoiCount => Pois.Count;
        public float RoadLengthKilometers { get; private set; }

        public void SetLakes(IReadOnlyList<LakeDefinition> lakes)
        {
            Lakes = lakes ?? Array.Empty<LakeDefinition>();
        }

        public void SetPois(IReadOnlyList<PoiMarker> pois)
        {
            if (pois == null || pois.Count == 0)
            {
                Pois = Array.Empty<PoiMarker>();
                return;
            }

            var list = new List<PoiMarker>(pois);
            list.Sort(CompareMarkers);
            Pois = list;
        }

        public void SetRoads(IReadOnlyList<RoadDefinition> roads)
        {
            Roads = roads ?? Array.Empty<RoadDefinition>();
            RoadLengthKilometers = ComputeRoadKm(Roads);
        }

        public static int CompareMarkers(PoiMarker a, PoiMarker b)
        {
            var kind = ((int)a.Kind).CompareTo((int)b.Kind);
            if (kind != 0)
            {
                return kind;
            }

            return string.CompareOrdinal(a.Id.Value, b.Id.Value);
        }

        private static float ComputeRoadKm(IReadOnlyList<RoadDefinition> roads)
        {
            var meters = 0f;
            for (var i = 0; i < roads.Count; i++)
            {
                var pts = roads[i].CenterlineXZ;
                if (pts == null || pts.Count < 2)
                {
                    continue;
                }

                for (var p = 1; p < pts.Count; p++)
                {
                    meters += Vector2.Distance(pts[p - 1], pts[p]);
                }
            }

            return meters * 0.001f;
        }
    }
}
