using GameDev.Core.Localization;

namespace GameDev.UI
{
    /// <summary>The HUD's words, apart from Unity so they can be tested; the words themselves are in the string tables (17 X.7).</summary>
    public static class HudText
    {
        public static string DayAndTime(int day, int minuteOfDay)
        {
            return Loc.Format("ui.hud.time", day, minuteOfDay / 60, minuteOfDay % 60);
        }
    }
}
