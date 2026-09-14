using System;
using System.Collections.Generic;
using GameDev.World.Hydrology;
using GameDev.World.Poi;
using GameDev.World.Roads;
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

        public IReadOnlyList<PoiMarker> Pois { get; private set; } =
            Array.Empty<PoiMarker>();

        public IReadOnlyList<RoadDefinition> Roads { get; private set; } =
            Array.Empty<RoadDefinition>();

        /// <summary>
        /// Inland water for this world: rivers, lakes and waterfall candidates.
        /// </summary>
        /// <remarks>
        /// The geographic truth, not the rendering. Meshes, gizmos and <c>IWaterSampler</c> all read
        /// from here; nothing writes back through them. Keeping that one-way is what the removed
        /// per-lake scene objects got wrong — they became the record of where water was, and every
        /// other system had to go through the scene to find out.
        /// </remarks>
        public HydrologyDefinition Hydrology { get; private set; } = HydrologyDefinition.Empty;

        public int PoiCount => Pois.Count;
        public float RoadLengthKilometers { get; private set; }
        public int RiverReachCount => Hydrology.Reaches.Count;
        public int LakeCount => Hydrology.Lakes.Count;
        public float RiverLengthKilometers => Hydrology.TotalRiverLengthKilometers;

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

        public void SetHydrology(HydrologyDefinition hydrology)
        {
            Hydrology = hydrology ?? HydrologyDefinition.Empty;
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
                return kind;
            return string.CompareOrdinal(a.Id.Value, b.Id.Value);
        }

        private static float ComputeRoadKm(IReadOnlyList<RoadDefinition> roads)
        {
            var meters = 0f;
            for (var i = 0; i < roads.Count; i++)
            {
                var pts = roads[i].CenterlineXZ;
                if (pts == null || pts.Count < 2)
                    continue;
                for (var p = 1; p < pts.Count; p++)
                    meters += Vector2.Distance(pts[p - 1], pts[p]);
            }

            return meters * 0.001f;
        }
    }
}
