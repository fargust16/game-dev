using System;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// A 1-D authored curve: control points sampled by a normalised parameter, smoothstepped
    /// between them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reason a shape reads as a landform rather than a bump. A shelf, a coastal rise and a
    /// mountain flank are all "distance in, height out"; what separates them is the curve, and a
    /// curve is data a human can look at and argue with. An analytic falloff is not.
    /// </para>
    /// <para>
    /// Smoothstep between control points rather than a cubic spline, deliberately: a spline can
    /// overshoot its control points, which for a height profile means a bump nobody authored.
    /// Monotone control points therefore give a monotone curve, which several invariants rely on.
    /// </para>
    /// </remarks>
    public readonly struct ProfileCurve
    {
        private readonly float[] keyT;
        private readonly float[] keyValue;

        /// <summary>Control points, ordered by strictly increasing <paramref name="t"/>.</summary>
        public ProfileCurve(float[] t, float[] value)
        {
            if (t == null || value == null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            if (t.Length != value.Length || t.Length < 2)
            {
                throw new ArgumentException(
                    $"A profile needs at least two matching control points, got {t.Length}/{value.Length}.");
            }

            for (var i = 1; i < t.Length; i++)
            {
                if (t[i] <= t[i - 1])
                {
                    throw new ArgumentException($"Profile t must strictly increase; t[{i}] = {t[i]}.");
                }
            }

            keyT = t;
            keyValue = value;
        }

        public int KeyCount => keyT?.Length ?? 0;

        /// <summary>Value at <paramref name="t"/>, clamped to the first and last control point.</summary>
        public float Evaluate(float t)
        {
            if (keyT == null)
            {
                return 0f;
            }

            if (t <= keyT[0])
            {
                return keyValue[0];
            }

            var last = keyT.Length - 1;
            if (t >= keyT[last])
            {
                return keyValue[last];
            }

            // Linear scan: profiles carry a handful of points, and a binary search here would cost
            // more in branch misprediction than it saves.
            for (var i = 1; i <= last; i++)
            {
                if (t > keyT[i])
                {
                    continue;
                }

                var span = keyT[i] - keyT[i - 1];
                var u = span <= 0f ? 0f : (t - keyT[i - 1]) / span;
                var s = u * u * (3f - 2f * u);
                return Mathf.LerpUnclamped(keyValue[i - 1], keyValue[i], s);
            }

            return keyValue[last];
        }

        /// <summary>True when the control values never decrease.</summary>
        public bool IsMonotoneRising()
        {
            if (keyValue == null)
            {
                return false;
            }

            for (var i = 1; i < keyValue.Length; i++)
            {
                if (keyValue[i] < keyValue[i - 1])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
