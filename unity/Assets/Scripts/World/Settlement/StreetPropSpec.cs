using System;
using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>One placed street prop along a SettlementStreet ribbon.</summary>
    [Serializable]
    public struct StreetPropSpec
    {
        public StreetPropKind Kind;
        public PropSemantic Semantic;
        public SettlementStreetKind StreetKind;
        public Vector3 Position;
        public Vector3 Scale;
        public float YawDegrees;
        public Color Tint;
    }
}
