using NUnit.Framework; 
using SlideJam.Engine;

namespace SlideJam.Tests.EditMode
{
    public class Mulberry32Tests
    {
        private static uint[] Sequential(uint seed, int count)
        {
            var result = new uint[count];
            uint a = seed;
            for (int i = 0; i < count; i++)
            {
                a = unchecked(a + 0x6D2B79F5u);
                uint t = unchecked((a ^ (a >> 15)) * (1u | a));
                t = unchecked((t + (t ^ (t >> 7)) * (61u | t)) ^ t);
                result[i] = t ^ (t >> 14);
            }

            return result;
        }

        [Test]
        public void At_MatchesSequentialGenerator() 
        {
            uint[] expected = Sequential(20261031u, 64);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], Mulberry32.At(20261031u, i), $"index {i}");
            }
        }

        [Test]
        public void At_IsDeterministic()
        {
            Assert.AreEqual(Mulberry32.At(777123u, 10), Mulberry32.At(777123u, 10));
        }

        // TODO (NEW): golden test with reference values taken from the original JS prototype,
        // once they are available; the test above only proves closed form == stepwise form.
    }
}