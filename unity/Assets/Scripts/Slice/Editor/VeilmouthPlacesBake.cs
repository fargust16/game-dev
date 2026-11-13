using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GameDev.Slice.News;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// 09 *News Pool* (PB8d): the places a death is told by, written from layout C5 for the game to read: each square,
    /// bridge, street with its pavements, named district, the West Road and the city itself, as a rectangle in world metres
    /// with its map key (map.csv, the player's map's names), so news names the place the map does. Run again whenever the
    /// layout changes: GameDev → Slice → World → Bake the places of the news, or <c>-executeMethod
    /// GameDev.Slice.EditorTools.VeilmouthPlacesBake.Run</c>.
    /// </summary>
    public static class VeilmouthPlacesBake
    {
        public const string Output = "Assets/Resources/" + SlicePlaces.ResourcePath + ".json";

        [MenuItem("GameDev/Slice/World/Bake the places of the news")]
        public static void Run()
        {
            var layout = VeilmouthLayout.Load();
            var places = new List<SlicePlaces.Place>();
            foreach (var s in layout.squares)
            {
                Add(places, s.name, SlicePlaces.Square, s.x0, s.y0, s.x1, s.y1);
            }

            foreach (var b in layout.bridges)
            {
                Add(places, b.name, SlicePlaces.Bridge, b.x0, b.y0, b.x1, b.y1);
            }

            foreach (var s in layout.streets)
            {
                if (s.AlongX)
                {
                    Add(places, s.name, SlicePlaces.Street, s.a0, s.c0, s.a1, s.c1);
                }
                else
                {
                    Add(places, s.name, SlicePlaces.Street, s.c0, s.a0, s.c1, s.a1);
                }
            }

            foreach (var d in layout.districts)
            {
                Add(places, d.name, SlicePlaces.District, d.x0, d.y0, d.x1, d.y1);
            }

            foreach (var r in layout.roads)
            {
                Add(places, r.name, SlicePlaces.Road, r.x0, r.y0, r.x1, r.y1);
            }

            var city = layout.City;
            places.Add(Place(SlicePlaces.TownKey, SlicePlaces.Town, city.x0, city.y0, city.x1, city.y1));
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            File.WriteAllText(Output, JsonUtility.ToJson(new SlicePlaces.Table { places = places.ToArray() }, true));
            AssetDatabase.ImportAsset(Output);
            Debug.Log($"[VeilmouthPlacesBake] {places.Count} places written to {Output}.");
        }

        /// <summary>The player's map's key of a plan name (CityMapElement.PlanKey): "Bishop's Garden" is map.bishop_s_garden.</summary>
        public static string MapKey(string name) => "map." + Regex.Replace((name ?? string.Empty).ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');

        private static void Add(List<SlicePlaces.Place> places, string name, string kind, float px0, float py0, float px1, float py1)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                places.Add(Place(MapKey(name), kind, px0, py0, px1, py1));
            }
        }

        // Plan metres to world: X = px − 500, Z = 500 − py (VeilmouthLayout).
        private static SlicePlaces.Place Place(string key, string kind, float px0, float py0, float px1, float py1) => new SlicePlaces.Place
        {
            key = key,
            kind = kind,
            x0 = Mathf.Min(px0, px1) - 500f,
            x1 = Mathf.Max(px0, px1) - 500f,
            z0 = 500f - Mathf.Max(py0, py1),
            z1 = 500f - Mathf.Min(py0, py1),
        };
    }
}
