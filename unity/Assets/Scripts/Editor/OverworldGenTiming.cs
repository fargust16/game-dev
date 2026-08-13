#if UNITY_EDITOR
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Lightweight Stopwatch helpers for Overworld generation. Logs <c>[Gen] step=... ms=...</c>
    /// so lab builds can find the 30–60 s bottleneck without a profiler session.
    /// </summary>
    public static class OverworldGenTiming
    {
        public static Stopwatch Start() => Stopwatch.StartNew();

        public static long StopMs(Stopwatch sw)
        {
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        public static void Log(string step, long ms, string detail = null)
        {
            if (string.IsNullOrEmpty(detail))
            {
                Debug.Log($"[Gen] step={step} ms={ms}");
            }
            else
            {
                Debug.Log($"[Gen] step={step} ms={ms} | {detail}");
            }
        }

        public static long Time(string step, System.Action action, string detail = null)
        {
            var sw = Start();
            action();
            var ms = StopMs(sw);
            Log(step, ms, detail);
            return ms;
        }
    }
}
#endif
