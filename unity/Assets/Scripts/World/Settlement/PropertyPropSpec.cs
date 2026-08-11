using System;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>One placed property prop (inside a lot zone).</summary>
    [Serializable]
    public struct PropertyPropSpec
    {
        public PropertyPropKind Kind;
        public PropSemantic Semantic;
        public PropertyZoneKind Zone;
        public Vector3 Position;
        public Vector3 Scale;
        public float YawDegrees;
        public Color Tint;
    }
}
