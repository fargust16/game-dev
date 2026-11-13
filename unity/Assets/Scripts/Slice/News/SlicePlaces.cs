using System;
using System.Collections.Generic;
using System.Globalization;
using GameDev.Core.Localization;
using GameDev.World.Seed;
using UnityEngine;

namespace GameDev.Slice.News
{
    /// <summary>
    /// 09 *News Pool* (PB8d): the name of the place where something happened, as the player's map names it. The places are
    /// written from layout C5 (VeilmouthPlacesBake) as world rectangles; the narrowest kind that holds a point names it: a
    /// square, a bridge, a street with its pavements and a metre round, a district, the West Road and 6 m round it, the
    /// city; beyond them the land outside the walls.
    /// </summary>
    public static class SlicePlaces
    {
        public const string ResourcePath = "Slice/VeilmouthPlaces";

        public const string Square = "square";
        public const string Bridge = "bridge";
        public const string Street = "street";
        public const string District = "district";
        public const string Road = "road";
        public const string Town = "town";

        /// <summary>The city within its walls, where no narrower place holds the point.</summary>
        public const string TownKey = "news.place.town";

        /// <summary>The land outside the city.</summary>
        public const string OutsideKey = "news.place.outside";

        /// <summary>Authoring defaults, m: how far round a street and the road a point still counts as on it.</summary>
        public const float StreetMargin = 1f;

        public const float RoadMargin = 6f;

        private static readonly string[] Order = { Square, Bridge, Street, District, Road, Town };
        private static Place[] _places;

        [Serializable]
        public sealed class Place
        {
            public string key;
            public string kind;
            public float x0;
            public float z0;
            public float x1;
            public float z1;
        }

        [Serializable]
        public sealed class Table
        {
            public Place[] places = new Place[0];
        }

        /// <summary>Every place the news can name, read once from Resources.</summary>
        public static IReadOnlyList<Place> All => _places ??= Load();

        /// <summary>The map key of the place that holds <paramref name="at"/>.</summary>
        public static string KeyAt(Vector3 at)
        {
            Place best = null;
            var bestRank = int.MaxValue;
            var bestArea = float.MaxValue;
            foreach (var p in All)
            {
                var margin = p.kind == Street ? StreetMargin : p.kind == Road ? RoadMargin : 0f;
                if (at.x < p.x0 - margin || at.x > p.x1 + margin || at.z < p.z0 - margin || at.z > p.z1 + margin)
                {
                    continue;
                }

                var rank = Array.IndexOf(Order, p.kind);
                var area = (p.x1 - p.x0) * (p.z1 - p.z0);
                if (rank < bestRank || (rank == bestRank && area < bestArea))
                {
                    best = p;
                    bestRank = rank;
                    bestArea = area;
                }
            }

            return best != null ? best.key : OutsideKey;
        }

        /// <summary>The key a stored hash stands for (the world state holds numbers only).</summary>
        public static string KeyOf(int hash)
        {
            if (hash == StableHash.Of(TownKey))
            {
                return TownKey;
            }

            foreach (var p in All)
            {
                if (StableHash.Of(p.key) == hash)
                {
                    return p.key;
                }
            }

            return OutsideKey;
        }

        /// <summary>The place's name in the language shown; a district written in capitals on the map in ordinary case.</summary>
        public static string Name(string key)
        {
            var name = Loc.Get(key);
            var culture = Loc.Language == Loc.English ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo("ru-RU");
            return name.Length > 1 && name == name.ToUpper(culture) && name != name.ToLower(culture) ? culture.TextInfo.ToTitleCase(name.ToLower(culture)) : name;
        }

        private static Place[] Load()
        {
            var text = Resources.Load<TextAsset>(ResourcePath);
            return text != null ? JsonUtility.FromJson<Table>(text.text).places ?? new Place[0] : new Place[0];
        }
    }
}
