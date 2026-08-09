using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Buildings.Shell
{
    [Serializable]
    public struct ShellPieceInstance
    {
        public ExteriorPieceCategory Category;
        public string PieceId;
        public Vector3 LocalPosition;
        public Vector3 LocalEuler;
        public Vector3 LocalScale;
        public Color Tint;

        /// <summary>Owning massing volume id; empty for plot dress (fence/stoop).</summary>
        public string OwnerVolumeId;

        /// <summary>-1 when not a face bay piece.</summary>
        public int FaceIndex;

        /// <summary>-1 when not a face bay piece.</summary>
        public int BayIndex;

        /// <summary>-1 when not storey-scoped.</summary>
        public int FloorIndex;
    }

    [Serializable]
    public struct PortalSocket
    {
        public Vector3 LocalPosition;
        public float YawDegrees;
        public string InteriorKey;
    }

    [Serializable]
    public sealed class ShellInstance
    {
        public BuildingDna Dna;
        public PlotFootprint Plot;
        public StreetContext Street;
        public List<ShellPieceInstance> Pieces = new List<ShellPieceInstance>(64);
        public PortalSocket? Portal;
        public Bounds LocalBounds;

        public void AddPiece(
            ExteriorPieceCategory category,
            string pieceId,
            Vector3 localPosition,
            Vector3 localEuler,
            Vector3 localScale,
            Color tint,
            string ownerVolumeId = null,
            int faceIndex = -1,
            int bayIndex = -1,
            int floorIndex = -1)
        {
            Pieces.Add(new ShellPieceInstance
            {
                Category = category,
                PieceId = pieceId,
                LocalPosition = localPosition,
                LocalEuler = localEuler,
                LocalScale = localScale,
                Tint = tint,
                OwnerVolumeId = ownerVolumeId ?? string.Empty,
                FaceIndex = faceIndex,
                BayIndex = bayIndex,
                FloorIndex = floorIndex
            });
        }
    }
}
