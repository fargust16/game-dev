using System;
using UnityEngine;

namespace GameDev.World.Buildings.Shell
{
    [Serializable]
    public struct PlotFootprint
    {
        public int WidthBays;
        public int DepthBays;
        public PlotShape Shape;
        public float YawDegrees;
        public Vector3 Origin;

        public float WidthMeters => WidthBays * ModularGrid.BayMeters;
        public float DepthMeters => DepthBays * ModularGrid.BayMeters;

        public static PlotFootprint Rect(int widthBays, int depthBays, Vector3 origin, float yawDegrees = 0f)
        {
            return new PlotFootprint
            {
                WidthBays = Mathf.Max(1, widthBays),
                DepthBays = Mathf.Max(1, depthBays),
                Shape = PlotShape.Rectangle,
                YawDegrees = yawDegrees,
                Origin = origin
            };
        }

        public static PlotFootprint Corner(int widthBays, int depthBays, Vector3 origin, float yawDegrees = 0f)
        {
            var p = Rect(widthBays, depthBays, origin, yawDegrees);
            p.Shape = PlotShape.Corner;
            return p;
        }
    }
}
