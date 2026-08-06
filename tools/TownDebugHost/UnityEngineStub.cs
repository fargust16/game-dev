using System;
using System.IO;

namespace UnityEngine
{
    public struct Vector2 : IEquatable<Vector2>
    {
        public float x;
        public float y;

        public static readonly Vector2 zero = new Vector2(0f, 0f);
        public static readonly Vector2 one = new Vector2(1f, 1f);
        public static readonly Vector2 right = new Vector2(1f, 0f);
        public static readonly Vector2 up = new Vector2(0f, 1f);

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public float magnitude => (float)Math.Sqrt(x * x + y * y);
        public float sqrMagnitude => x * x + y * y;

        public Vector2 normalized
        {
            get
            {
                var m = magnitude;
                return m > 1e-8f ? this / m : zero;
            }
        }

        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator -(Vector2 a) => new Vector2(-a.x, -a.y);
        public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator *(float d, Vector2 a) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator /(Vector2 a, float d) => new Vector2(a.x / d, a.y / d);

        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
        }

        public bool Equals(Vector2 other) => x.Equals(other.x) && y.Equals(other.y);
        public override bool Equals(object obj) => obj is Vector2 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(x, y);
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static readonly Vector3 up = new Vector3(0f, 1f, 0f);
        public static readonly Vector3 zero = new Vector3(0f, 0f, 0f);
    }

    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public static float Abs(float v) => Math.Abs(v);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Sqrt(float v) => (float)Math.Sqrt(v);
        public static float Sin(float v) => (float)Math.Sin(v);
        public static float Cos(float v) => (float)Math.Cos(v);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static int CeilToInt(float v) => (int)Math.Ceiling(v);
        public static int RoundToInt(float v) => (int)Math.Round(v);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public const float Rad2Deg = 360f / ((float)Math.PI * 2f);
        public const float Deg2Rad = ((float)Math.PI * 2f) / 360f;

        public static float DeltaAngle(float current, float target)
        {
            var delta = (target - current) % 360f;
            if (delta > 180f) delta -= 360f;
            if (delta < -180f) delta += 360f;
            return delta;
        }
    }

    public class Object
    {
        public static void DestroyImmediate(Object obj) { }
        public static void Destroy(Object obj) { }
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() => new T();
    }

    public static class Debug
    {
        public static void Log(object message) => Console.WriteLine(message);
        public static void LogWarning(object message) => Console.WriteLine("WARN: " + message);
        public static void LogError(object message) => Console.Error.WriteLine("ERR: " + message);
    }

    public static class Application
    {
        public static bool isPlaying => false;

        public static string dataPath
        {
            get
            {
                // tools/TownDebugHost → repo/unity/Assets
                var toolsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
                return Path.GetFullPath(Path.Combine(toolsDir, "..", "..", "unity", "Assets"));
            }
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string tooltip) { }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header) { }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class RangeAttribute : Attribute
    {
        public RangeAttribute(float min, float max) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string menuName { get; set; }
        public string fileName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeFieldAttribute : Attribute { }
}
