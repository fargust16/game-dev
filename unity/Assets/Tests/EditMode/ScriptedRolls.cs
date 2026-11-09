using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace GameDev.Slice.Tests
{
    /// <summary>Rolls in the order given: each NextDouble, and Next read from it, for tests that fix a fight's dice.</summary>
    internal sealed class ScriptedRolls : Random
    {
        private readonly Queue<double> _draws;

        public ScriptedRolls(IEnumerable<double> draws) => _draws = new Queue<double>(draws);

        public override double NextDouble()
        {
            Assert.IsNotEmpty(_draws, "the code drew more rolls than the test gave");
            return _draws.Dequeue();
        }

        protected override double Sample() => NextDouble();

        public override int Next(int maxValue) => (int)(NextDouble() * maxValue);

        public override int Next(int minValue, int maxValue) => minValue + (int)(NextDouble() * (maxValue - minValue));
    }
}
