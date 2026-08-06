using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Town
{
    /// <summary>Renderer bucket for occlusion (append-only).</summary>
    public enum BuildingVisualCategory : byte
    {
        Other = 0,
        Floor = 1,
        Wall = 2,
        Shell = 3,
        Socket = 4
    }

    /// <summary>
    /// Tags a baked building hierarchy so <see cref="BuildingOcclusionController"/>
    /// can hide walls/shells without guessing from names alone.
    /// </summary>
    public sealed class BuildingVisualRoot : MonoBehaviour
    {
        [SerializeField] private int buildingIndex = -1;
        [SerializeField] private List<Renderer> wallRenderers = new List<Renderer>(16);
        [SerializeField] private List<Renderer> floorRenderers = new List<Renderer>(16);
        [SerializeField] private List<Renderer> shellRenderers = new List<Renderer>(4);
        [SerializeField] private List<Renderer> socketRenderers = new List<Renderer>(8);

        private readonly List<Renderer> _hidden = new List<Renderer>(32);

        public int BuildingIndex => buildingIndex;
        public IReadOnlyList<Renderer> WallRenderers => wallRenderers;
        public IReadOnlyList<Renderer> ShellRenderers => shellRenderers;

        public void Configure(int index)
        {
            buildingIndex = index;
            wallRenderers.Clear();
            floorRenderers.Clear();
            shellRenderers.Clear();
            socketRenderers.Clear();
            _hidden.Clear();
        }

        public void Register(Renderer renderer, BuildingVisualCategory category)
        {
            if (renderer == null)
            {
                return;
            }

            switch (category)
            {
                case BuildingVisualCategory.Wall:
                    wallRenderers.Add(renderer);
                    break;
                case BuildingVisualCategory.Floor:
                    floorRenderers.Add(renderer);
                    break;
                case BuildingVisualCategory.Shell:
                    shellRenderers.Add(renderer);
                    break;
                case BuildingVisualCategory.Socket:
                    socketRenderers.Add(renderer);
                    break;
            }
        }

        public void RegisterAllInChildren(GameObject root, BuildingVisualCategory category)
        {
            if (root == null)
            {
                return;
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                Register(renderers[i], category);
            }
        }

        /// <summary>Hide exterior occlusion targets (walls + solid shells). Floors/sockets stay.</summary>
        public void ApplyOcclusionHidden()
        {
            RestoreOcclusion();
            HideList(wallRenderers);
            HideList(shellRenderers);
        }

        public void RestoreOcclusion()
        {
            for (var i = 0; i < _hidden.Count; i++)
            {
                var r = _hidden[i];
                if (r != null)
                {
                    r.enabled = true;
                }
            }

            _hidden.Clear();
        }

        private void HideList(List<Renderer> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var r = list[i];
                if (r == null || !r.enabled)
                {
                    continue;
                }

                r.enabled = false;
                _hidden.Add(r);
            }
        }
    }
}
