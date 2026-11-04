namespace GameDev.UI
{
    /// <summary>The HUD's words, apart from Unity so they can be tested.</summary>
    public static class HudText
    {
        public static string DayAndTime(int day, int minuteOfDay)
        {
            return $"Day {day} · {minuteOfDay / 60:00}:{minuteOfDay % 60:00}";
        }
    }
}
