using System.Collections.Generic;
using System.Globalization;
using GameDev.Core.Localization;

namespace GameDev.Items
{
    /// <summary>
    /// The bag page's words (S5a.1): the purse in coins, the load against Comfortable Capacity with its band, and a line
    /// for each stack and each piece, stacks first in the catalogue's order.
    /// </summary>
    public static class BagPage
    {
        public static string Money(Bag bag) => Loc.Format("ui.hud.bag.money", Coins.Show(bag.Bronze));

        public static string Load(Bag bag) =>
            Loc.Format("ui.hud.bag.load", Kg(bag.Weight), Kg(bag.Comfortable), Loc.Get(BandKey(bag.Band)));

        public static IReadOnlyList<string> Lines(Bag bag) => Lines(bag, null);

        /// <param name="note">What a piece's line adds, such as where it is worn; null or empty for nothing.</param>
        public static IReadOnlyList<string> Lines(Bag bag, System.Func<ItemPiece, string> note)
        {
            var lines = new List<string>();
            foreach (var (item, count) in bag.Stacks)
            {
                lines.Add(Loc.Format("ui.hud.bag.stack", Loc.Get(item.NameKey), count, Kg(item.weight * count)));
            }

            foreach (var piece in bag.Pieces)
            {
                var item = bag.Catalogue.Find(piece.item);
                var line = Loc.Format("ui.hud.bag.piece", Loc.Get(item.NameKey), Loc.Get(QualityKey(piece.Quality)), Kg(item.weight));
                var mods = ItemMaker.Describe(piece.modifiers);
                if (mods.Length > 0)
                {
                    line += ": " + mods;
                }

                var extra = note?.Invoke(piece);
                lines.Add(string.IsNullOrEmpty(extra) ? line : line + extra);
            }

            return lines;
        }

        public static string BandKey(LoadBand band) => band switch
        {
            LoadBand.Normal => "ui.hud.bag.band.normal",
            LoadBand.High => "ui.hud.bag.band.high",
            LoadBand.Overloaded => "ui.hud.bag.band.overloaded",
            _ => "ui.hud.bag.band.critical",
        };

        public static string QualityKey(ItemQuality quality) => "ui.quality." + quality.ToString().ToLowerInvariant();

        /// <summary>A weight in kg, to two places at most.</summary>
        public static string Kg(float kg) => kg.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
