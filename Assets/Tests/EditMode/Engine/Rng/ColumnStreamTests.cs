using NUnit.Framework;
using SlideJam.Engine;

namespace SlideJam.Tests.EditMode
{
    public class ColumnStreamTests 
    {
        private const uint Seed = 20261031u; 
        private const int Colors = 5;

        [Test]
        public void At_StaysInColourRange()
        {
            for (int column = 0; column < 7; column++)
            {
                for (int i = 0; i < 200; i++)
                {
                    int colour = ColumnStream.At(Seed, column, i, Colors);
                    Assert.That(colour, Is.InRange(0, Colors - 1));
                }
            }
        }

        [Test]
        public void At_IsDeterministic()
        {
            Assert.AreEqual(ColumnStream.At(Seed, 3, 17, Colors), ColumnStream.At(Seed, 3, 17, Colors));
        }

        [Test]
        public void At_DifferentColumnsGiveDifferentStreams() 
        {
            bool anyDifferent = false;
            for (int i = 0; i < 32 && !anyDifferent; i++)
            {
                anyDifferent = ColumnStream.At(Seed, 0, i, Colors) != ColumnStream.At(Seed, 1, i, Colors);
            }

            Assert.IsTrue(anyDifferent);
        }

        [Test]
        public void At_UsesEveryColour()
        {
            var seen = new bool[Colors];
            for (int i = 0; i < 200; i++)
            {
                seen[ColumnStream.At(Seed, 0, i, Colors)] = true;
            }

            CollectionAssert.DoesNotContain(seen, false);
        }

        [Test]
        public void At_RejectsNonPositiveColours()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => ColumnStream.At(Seed, 0, 0, 0));
        }
    }
}