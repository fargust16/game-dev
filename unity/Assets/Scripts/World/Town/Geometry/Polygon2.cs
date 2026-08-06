using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Town.Geometry
{
    /// <summary>
    /// A 2D polygon operating in world-space XZ plane (Vector2.x → X, Vector2.y → Z).
    /// Wraps a <see cref="List{T}"/> of <see cref="Vector2"/> vertices and caches
    /// signed area and centroid; the cache is invalidated on any mutation.
    /// </summary>
    [Serializable]
    public sealed class Polygon2
    {
        [SerializeField]
        private List<Vector2> _vertices;

        // --- Cache state ---
        private bool _areaCached;
        private float _cachedArea;
        private bool _centroidCached;
        private Vector2 _cachedCentroid;

        // ------------------------------------------------------------------ ctors

        public Polygon2()
        {
            _vertices = new List<Vector2>();
        }

        public Polygon2(int capacity)
        {
            _vertices = new List<Vector2>(capacity);
        }

        public Polygon2(IEnumerable<Vector2> vertices)
        {
            _vertices = new List<Vector2>(vertices);
        }

        // ------------------------------------------------------------------ read

        /// <summary>Read-only view of the vertex list.</summary>
        public IReadOnlyList<Vector2> Vertices => _vertices;

        /// <summary>Vertex count.</summary>
        public int Count => _vertices.Count;

        /// <summary>Get vertex at <paramref name="index"/>.</summary>
        public Vector2 this[int index] => _vertices[index];

        // ------------------------------------------------------------------ mutation (each invalidates cache)

        public void AddVertex(Vector2 v)
        {
            _vertices.Add(v);
            InvalidateCache();
        }

        public void InsertVertex(int index, Vector2 v)
        {
            _vertices.Insert(index, v);
            InvalidateCache();
        }

        public void SetVertex(int index, Vector2 v)
        {
            _vertices[index] = v;
            InvalidateCache();
        }

        public void RemoveAt(int index)
        {
            _vertices.RemoveAt(index);
            InvalidateCache();
        }

        public void Clear()
        {
            _vertices.Clear();
            InvalidateCache();
        }

        /// <summary>Replace all vertices in one call (single cache invalidation).</summary>
        public void SetVertices(IEnumerable<Vector2> verts)
        {
            _vertices.Clear();
            _vertices.AddRange(verts);
            InvalidateCache();
        }

        // ------------------------------------------------------------------ cached properties

        /// <summary>
        /// Signed area of the polygon (positive = CCW winding, negative = CW).
        /// Computed via the shoelace formula; result is cached until the next mutation.
        /// </summary>
        public float SignedArea
        {
            get
            {
                if (!_areaCached)
                {
                    _cachedArea = Geo2.PolygonArea(_vertices);
                    _areaCached = true;
                }
                return _cachedArea;
            }
        }

        /// <summary>Absolute area (always non-negative).</summary>
        public float Area => Mathf.Abs(SignedArea);

        /// <summary>
        /// Centroid of the polygon computed via the standard area-weighted formula.
        /// Result is cached until the next mutation.
        /// </summary>
        public Vector2 Centroid
        {
            get
            {
                if (!_centroidCached)
                {
                    _cachedCentroid = Geo2.PolygonCentroid(_vertices);
                    _centroidCached = true;
                }
                return _cachedCentroid;
            }
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>
        /// Returns a fresh independent <see cref="List{T}"/> copy of the vertices.
        /// Use when you need to mutate the collection without invalidating this polygon's cache.
        /// </summary>
        public List<Vector2> ToList() => new List<Vector2>(_vertices);

        // ------------------------------------------------------------------ private

        private void InvalidateCache()
        {
            _areaCached = false;
            _centroidCached = false;
        }
    }
}
