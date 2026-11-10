using System.Collections.Generic;
using GameDev.Confront.Grid;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// The Combat Grid drawn during Confront (S3a.4): a thin outline inside every cell the player can reach, just above
    /// its surface. It reads the grid and is no part of it: it has no collider, so the grid never reads it.
    /// </summary>
    public sealed class CombatGridView : MonoBehaviour
    {
        // Authoring defaults, S3a.4: metres above the surface, in from a cell's edge, and the line's width.
        private const float Lift = 0.03f;
        private const float Inset = 0.06f;
        private const float Line = 0.035f;

        // Authoring defaults, S3a.5: the lines fade from 5 m round the player to nothing at 14 m, a turn's walk and a
        // little more, so the grid reads where the player can act and melts into the street beyond.
        private static readonly Vector4 Fade = new Vector4(5f, 14f, 0f, 0f);
        private static readonly int FocusId = Shader.PropertyToID("_Focus");
        private static readonly int FadeId = Shader.PropertyToID("_Fade");

        private readonly List<CombatCell> _cells = new List<CombatCell>();
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _triangles = new List<int>();
        private Material _material;
        private Mesh _mesh;

        /// <summary>The cells drawn last.</summary>
        public int Cells => _cells.Count;

        public bool Shown => gameObject.activeSelf;

        public static CombatGridView Create(Transform parent, Material material)
        {
            var go = new GameObject("Combat Grid");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<CombatGridView>();

            // Its own copy of the material, which the fade's centre is written to: the GPU Resident Drawer draws a
            // renderer without its property block.
            view._material = new Material(material) { name = "Combat Grid (fight)" };
            view._material.SetVector(FadeId, Fade);
            go.AddComponent<MeshRenderer>().sharedMaterial = view._material;
            view._mesh = new Mesh { name = "Combat Grid", indexFormat = IndexFormat.UInt32 };
            go.AddComponent<MeshFilter>().sharedMesh = view._mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            go.SetActive(false);
            return view;
        }

        /// <summary>Centres the lines' fade on the fighter the grid follows; cheap enough for every frame.</summary>
        public void Focus(Vector3 at) => _material.SetVector(FocusId, at);

        public void Show(CombatGridReader grid)
        {
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            grid.CopyReachable(_cells);
            _vertices.Clear();
            _triangles.Clear();
            var half = (grid.CellSize * 0.5f) - Inset;
            foreach (var cell in _cells)
            {
                var c = grid.Center(cell) + (Vector3.up * Lift);
                Quad(c + new Vector3(-half, 0f, half - Line), c + new Vector3(half, 0f, half));
                Quad(c + new Vector3(-half, 0f, -half), c + new Vector3(half, 0f, -half + Line));
                Quad(c + new Vector3(-half, 0f, -half + Line), c + new Vector3(-half + Line, 0f, half - Line));
                Quad(c + new Vector3(half - Line, 0f, -half + Line), c + new Vector3(half, 0f, half - Line));
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateBounds();
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>A flat rectangle between two corners, facing up.</summary>
        private void Quad(Vector3 min, Vector3 max)
        {
            var i = _vertices.Count;
            _vertices.Add(new Vector3(min.x, min.y, min.z));
            _vertices.Add(new Vector3(min.x, min.y, max.z));
            _vertices.Add(new Vector3(max.x, min.y, max.z));
            _vertices.Add(new Vector3(max.x, min.y, min.z));
            _triangles.Add(i);
            _triangles.Add(i + 1);
            _triangles.Add(i + 2);
            _triangles.Add(i);
            _triangles.Add(i + 2);
            _triangles.Add(i + 3);
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }

            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
