using System;
using UnityEngine;

namespace GameDev.VictorianKit
{
    /// <summary>
    /// Tells each pane of one house which built room stands behind it (VicRooms), so the far look of the window glass
    /// maps that room's own finish, depth, walls, floor and ceiling instead of a made-up room, and the turn from the
    /// built room to the mapped one changes only the parallax (Plans/VEILMOUTH_REFERENCE_PLAN.md R2 step 4).
    /// </summary>
    /// <remarks>
    /// The value rides on the renderer's shader user value, which the SRP Batcher keeps but which is not saved, so it is
    /// set again whenever the house comes to life, in play and in the editor. A pane with no value keeps the old
    /// made-up room.
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class VicWindowRooms : MonoBehaviour
    {
        [SerializeField, Tooltip("The house's window panes, each with the room value at the same index.")]
        private MeshRenderer[] _panes = Array.Empty<MeshRenderer>();

        [SerializeField]
        private uint[] _rooms = Array.Empty<uint>();

        /// <summary>The panes told their room.</summary>
        public int Count => Math.Min(_panes.Length, _rooms.Length);

        /// <summary>The pane at <paramref name="i"/> and its packed room.</summary>
        public (MeshRenderer Pane, uint Room) this[int i] => (_panes[i], _rooms[i]);

        /// <summary>Editor setup when the rooms are built.</summary>
        public void Configure(MeshRenderer[] panes, uint[] rooms)
        {
            _panes = panes;
            _rooms = rooms;
            Apply();
        }

        private void OnEnable() => Apply();

        private void Apply()
        {
            for (var i = 0; i < Count; i++)
            {
                if (_panes[i] != null)
                {
                    _panes[i].SetShaderUserValue(_rooms[i]);
                }
            }
        }

        /// <summary>
        /// One room as the window shader reads it (VicWindow ReadRoom), every length in the pane's UV metres: the
        /// finish (1 dark, 2 panelled, 3 papered) in 2 bits, then the back wall's depth behind the glass, the walls left
        /// (toward −u) and right of the pane's middle, the ceiling over the sill and the floor under it, 6 bits each in
        /// tenths of a metre, so up to 6.3 m. Never 0, which means no room.
        /// </summary>
        public static uint Pack(int finish, float depth, float left, float right, float above, float below)
        {
            static uint Tenths(float metres) => (uint)Mathf.Clamp(Mathf.RoundToInt(metres * 10f), 0, 63);
            return ((uint)Mathf.Clamp(finish, 1, 3)) | (Tenths(depth) << 2) | (Tenths(left) << 8) | (Tenths(right) << 14) |
                   (Tenths(above) << 20) | (Tenths(below) << 26);
        }
    }
}
