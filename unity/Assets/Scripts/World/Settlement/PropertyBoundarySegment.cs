using System;
using GameDev.World.Buildings.Shell;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// One fence/wall/gate segment as a world-space line from <see cref="StartWorld"/> to <see cref="EndWorld"/>.
    /// </summary>
    [Serializable]
    public struct PropertyBoundarySegment
    {
        public PlotFace Edge;
        public BoundaryStyle Style;
        public bool IsGate;
        public Vector3 StartWorld;
        public Vector3 EndWorld;
        public float HeightMeters;
        public float ThicknessMeters;

        public float LengthMeters => Vector3.Distance(StartWorld, EndWorld);

        public static float HeightFor(BoundaryStyle style)
        {
            switch (style)
            {
                case BoundaryStyle.LowFence: return 1.0f;
                case BoundaryStyle.IronFence: return 1.4f;
                case BoundaryStyle.Wall: return 2.2f;
                default: return 0f;
            }
        }

        public static float ThicknessFor(BoundaryStyle style)
        {
            switch (style)
            {
                case BoundaryStyle.Wall: return 0.22f;
                case BoundaryStyle.IronFence: return 0.18f;
                case BoundaryStyle.LowFence: return 0.14f;
                default: return 0.12f;
            }
        }
    }
}
