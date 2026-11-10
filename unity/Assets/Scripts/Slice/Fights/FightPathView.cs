using System.Collections.Generic;
using GameDev.Confront.Grid;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// The player's path before the click (S3a.5): a mark in each cell it steps into and the frame of the cell it ends in,
    /// gold while the AP last and a dull red past them. No collider, so the grid never reads it.
    /// </summary>
    public sealed class FightPathView : MonoBehaviour
    {
        // Authoring defaults, S3a.5: metres above the surface, a step's mark, the end frame's inset and width, and the
        // colours, a warm gold and a dull red in the city's muted range.
        private const float Lift = 0.04f;
        private const float Mark = 0.16f;
        private const float Inset = 0.08f;
        private const float Line = 0.07f;
        private static readonly Color Within = new Color(0.93f, 0.76f, 0.38f);
        private static readonly Color Beyond = new Color(0.72f, 0.26f, 0.2f);

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _within = new List<int>();
        private readonly List<int> _beyond = new List<int>();
        private Mesh _mesh;
        private Material[] _materials;

        /// <summary>The cells drawn, and how many of them the AP reach.</summary>
        public int Steps { get; private set; }

        public int Affordable { get; private set; }

        public bool Shown => gameObject.activeSelf;

        public static FightPathView Create(Transform parent, Material grid)
        {
            var go = new GameObject("Fight Path");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<FightPathView>();
            view._mesh = new Mesh { name = "Fight Path", subMeshCount = 2 };
            go.AddComponent<MeshFilter>().sharedMesh = view._mesh;
            view._materials = new[] { Tinted(grid, Within), Tinted(grid, Beyond) };
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = view._materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            go.SetActive(false);
            return view;
        }

        private static Material Tinted(Material grid, Color colour)
        {
            var material = new Material(grid) { name = "Fight Path " + colour };
            material.SetColor("_BaseColor", colour);
            return material;
        }

        /// <summary>The path's cells, the first <paramref name="affordable"/> of them in gold, with the last one framed.</summary>
        public void Show(ICombatGrid grid, IReadOnlyList<CombatCell> path, int affordable)
        {
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _vertices.Clear();
            _within.Clear();
            _beyond.Clear();
            Steps = path.Count;
            Affordable = Mathf.Min(affordable, path.Count);
            for (var i = 0; i < path.Count; i++)
            {
                var c = grid.Center(path[i]) + (Vector3.up * Lift);
                var into = i < affordable ? _within : _beyond;
                if (i == path.Count - 1)
                {
                    Frame(c, (grid.CellSize * 0.5f) - Inset, into);
                }
                else
                {
                    Quad(c + new Vector3(-Mark, 0f, -Mark), c + new Vector3(Mark, 0f, Mark), into);
                }
            }

            _mesh.Clear();
            _mesh.subMeshCount = 2;
            _mesh.SetVertices(_vertices);
            _mesh.SetTriangles(_within, 0);
            _mesh.SetTriangles(_beyond, 1);
            _mesh.RecalculateBounds();
            gameObject.SetActive(path.Count > 0);
        }

        public void Hide()
        {
            Steps = 0;
            Affordable = 0;
            gameObject.SetActive(false);
        }

        private void Frame(Vector3 c, float half, List<int> into)
        {
            Quad(c + new Vector3(-half, 0f, half - Line), c + new Vector3(half, 0f, half), into);
            Quad(c + new Vector3(-half, 0f, -half), c + new Vector3(half, 0f, -half + Line), into);
            Quad(c + new Vector3(-half, 0f, -half + Line), c + new Vector3(-half + Line, 0f, half - Line), into);
            Quad(c + new Vector3(half - Line, 0f, -half + Line), c + new Vector3(half, 0f, half - Line), into);
        }

        /// <summary>A flat rectangle between two corners, facing up.</summary>
        private void Quad(Vector3 min, Vector3 max, List<int> into)
        {
            var i = _vertices.Count;
            _vertices.Add(new Vector3(min.x, min.y, min.z));
            _vertices.Add(new Vector3(min.x, min.y, max.z));
            _vertices.Add(new Vector3(max.x, min.y, max.z));
            _vertices.Add(new Vector3(max.x, min.y, min.z));
            into.Add(i);
            into.Add(i + 1);
            into.Add(i + 2);
            into.Add(i);
            into.Add(i + 2);
            into.Add(i + 3);
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }

            if (_materials != null)
            {
                foreach (var m in _materials)
                {
                    Destroy(m);
                }
            }
        }
    }
}
