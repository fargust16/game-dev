using System.Collections.Generic;
using GameDev.Core.Localization;

namespace GameDev.Items
{
    /// <summary>
    /// 19 §6.3: three coins, 100 bronze to the silver and 100 silver to the gold. Every amount is whole bronze; it is shown
    /// in coins, a coin whose count is zero left out, and nothing at all as 0 bronze.
    /// </summary>
    public static class Coins
    {
        public const int BronzePerSilver = 100;
        public const int BronzePerGold = 10000;

        public static (int gold, int silver, int bronze) Split(int bronze) =>
            (bronze / BronzePerGold, bronze % BronzePerGold / BronzePerSilver, bronze % BronzePerSilver);

        public static string Show(int bronze)
        {
            var (g, s, b) = Split(bronze);
            var parts = new List<string>();
            if (g > 0)
            {
                parts.Add(Loc.Format("ui.coins.gold", g));
            }

            if (s > 0)
            {
                parts.Add(Loc.Format("ui.coins.silver", s));
            }

            if (b > 0 || parts.Count == 0)
            {
                parts.Add(Loc.Format("ui.coins.bronze", b));
            }

            return string.Join(" ", parts);
        }
    }
}
