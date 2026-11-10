using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Veilmouth's layout C5 (VEILMOUTH_CITY_PLAN.md §29), as tools/slice/veilmouth_c5_plan.py writes it: streets, squares,
    /// blocks with their yards and passages, every building, stone masses, water, walls, the works and the outskirts, in plan metres
    /// (px east, py south from the area's north-west corner; world X = px − 500, world Z = 500 − py). The planner is the
    /// one source: the ground, the city and the zones read this, and the plan and the player's map are written with it.
    /// </summary>
    [Serializable]
    public sealed class VeilmouthLayout
    {
        public const string Path = "Assets/Data/Slice/Map/VeilmouthC5Layout.json";

        public string version;
        public Street[] streets;
        public Square[] squares;
        public RowSpec[] rows;
        public PlotSpec[] plots;
        public BuildingSpec[] buildings;
        public BoxSpec[] boxes;
        public Water[] water;
        public WallSpec[] walls;
        public TowerSpec[] towers;
        public BridgeSpec[] bridges;
        public SlantSpec[] slants;
        public District[] districts;
        public Candidate[] candidates;
        public BlockSpec[] blocks;
        public PassageSpec[] passages;
        public Area[] roads;
        public PlazaSpec plaza;
        public Cottage[] cottages;
        public ForestSpec forest;
        public Point spawn;

        private static VeilmouthLayout _cached;
        private static DateTime _stamp;

        /// <summary>The layout, read again whenever its file changes.</summary>
        public static VeilmouthLayout Load()
        {
            var stamp = File.GetLastWriteTimeUtc(Path);
            if (_cached == null || stamp != _stamp)
            {
                _cached = JsonUtility.FromJson<VeilmouthLayout>(File.ReadAllText(Path));
                _stamp = stamp;
            }

            return _cached;
        }

        // Derived once per read: the ground asks for these about a million times a shape.
        private Water[] _rivers;
        private Water _sea;
        private float? _openCoast;
        private Area _city, _works;

        public Water[] Rivers => _rivers ??= water.Where(w => w.kind == "river").ToArray();

        /// <summary>The sea along the city: the quay wall's line (x0) between y0 and y1.</summary>
        public Water Sea => _sea ??= water.First(w => w.kind == "sea");

        /// <summary>The open coast north and south of the city.</summary>
        public float OpenCoast => _openCoast ??= water.Where(w => w.kind == "sea").Max(w => w.x0);

        public WallSpec Wall(string name) => walls.First(w => w.name == name);

        /// <summary>The city inside its walls: from the west wall's inner face to the quay wall, the north wall to the river.</summary>
        public Area City
        {
            get
            {
                if (_city == null)
                {
                    var west = Wall("Town wall (west)");
                    var north = Wall("Town wall (north)");
                    _city = new Area { x0 = west.t1, y0 = north.t1, x1 = Sea.x0, y1 = west.a1 };
                }

                return _city;
            }
        }

        /// <summary>The works over the river, inside their walls.</summary>
        public Area Works
        {
            get
            {
                if (_works == null)
                {
                    var west = Wall("Works wall (west)");
                    var south = Wall("Works wall (south)");
                    _works = new Area { x0 = west.t1, y0 = west.a0, x1 = Sea.x0, y1 = south.t0 };
                }

                return _works;
            }
        }

        public Street StreetById(string id) => streets.First(s => s.id == id);

        public Square SquareById(string id) => squares.First(s => s.id == id);

        [Serializable]
        public sealed class Street
        {
            public string id, name, kind, axis, surface, trees;
            public float a0, a1, c0, c1, walk0, walk1;

            public bool AlongX => axis == "x";
        }

        [Serializable]
        public sealed class Square
        {
            public string id, name, surface;
            public float x0, y0, x1, y1;
        }

        [Serializable]
        public sealed class RowSpec
        {
            public string facing, family, stoop;
            public float front, a0, a1;
            public int seed;
        }

        [Serializable]
        public sealed class PlotSpec
        {
            public string facing, kind, name;
            public float front, s0, s1, at;
            public int garden, behind, seed;
        }

        [Serializable]
        public sealed class BuildingSpec
        {
            public string id, key, facing, name, stoop, block;
            public float front, a;
        }

        [Serializable]
        public sealed class BoxSpec
        {
            public string id, name, material;
            public float x0, y0, x1, y1, height, @base;
        }

        [Serializable]
        public sealed class Water
        {
            public string name, kind;
            public float x0, y0, x1, y1;

            public bool Contains(float px, float py) => px >= x0 && px <= x1 && py >= y0 && py <= y1;
        }

        [Serializable]
        public sealed class Gap
        {
            public float a, b;
        }

        [Serializable]
        public sealed class WallSpec
        {
            public string name, axis;
            public float t0, t1, a0, a1, height;
            public Gap[] gaps;
        }

        [Serializable]
        public sealed class TowerSpec
        {
            public string name, facing;
            public float front, a;
        }

        [Serializable]
        public sealed class BridgeSpec
        {
            public string name, axis;
            public float x0, y0, x1, y1;
        }

        [Serializable]
        public sealed class SlantSpec
        {
            public string name, family;
            public float x0, y0, x1, y1, width;
        }

        [Serializable]
        public sealed class District
        {
            public string name, colour;
            public float x0, y0, x1, y1;
        }

        [Serializable]
        public sealed class Candidate
        {
            public string name;
            public float x, y, size;
        }

        [Serializable]
        public sealed class Area
        {
            public string name;
            public float x0, y0, x1, y1;

            public bool Contains(float px, float py) => px >= x0 && px <= x1 && py >= y0 && py <= y1;

            /// <summary>The distance from a plan point to this rectangle, 0 inside.</summary>
            public float Distance(float px, float py)
            {
                var dx = Mathf.Max(x0 - px, Mathf.Max(0f, px - x1));
                var dy = Mathf.Max(y0 - py, Mathf.Max(0f, py - y1));
                return Mathf.Sqrt((dx * dx) + (dy * dy));
            }
        }

        [Serializable]
        public sealed class PlazaSpec
        {
            public string name;
            public float cx, cy, half, ringOuter, ringInner, island, plinth, statue;
            public StallSpec stalls;
        }

        /// <summary>The market round the statue (§29.1 item 5): stalls on a circle, facing the ring's road.</summary>
        [Serializable]
        public sealed class StallSpec
        {
            public string name;
            public float radius, width, depth;
            public int count;
        }

        /// <summary>
        /// A city block (§29.2): its rectangle, ringed by its streets' pavements; an L-block's notch, given to the Penny;
        /// the inner yard behind its houses; its wealth (rich, middling, poor, works, cemetery, close).
        /// </summary>
        [Serializable]
        public sealed class BlockSpec
        {
            public string id, wealth, district;
            public float x0, y0, x1, y1;
            public Area notch, yard;

            /// <summary>JsonUtility reads a null notch as an empty area.</summary>
            public bool HasNotch => notch != null && notch.x1 > notch.x0;
        }

        /// <summary>A passage between a block's houses (R5), from the pavement to the yard: its side, front line, span and depth.</summary>
        [Serializable]
        public sealed class PassageSpec
        {
            public string block, facing;
            public float front, a0, a1, depth;
        }

        [Serializable]
        public sealed class Cottage
        {
            public string key, facing;
            public float front, a;
        }

        [Serializable]
        public sealed class ForestSpec
        {
            public Area[] rects;
        }

        [Serializable]
        public sealed class Point
        {
            public float x, y;
        }
    }
}
