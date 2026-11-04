using NUnit.Framework;
using SlideJam.Engine;

namespace SlideJam.Tests.EditMode
{
    public class MatcherTests
    {
        [Test]
        public void Find_HorizontalRunOfThree()
        {
            var grid = TestGrids.FromRows("01110", "20202", "02020");
            Assert.AreEqual(3, Matcher.Find(grid).Count);
        }

        [Test]
        public void Find_VerticalRunOfThree()
        {
            var grid = TestGrids.FromRows("12", "13", "14");
            Assert.AreEqual(3, Matcher.Find(grid).Count);
        }

        [Test]
        public void Find_RunOfFourCountsFourCells()
        {
            Assert.AreEqual(4, Matcher.Find(TestGrids.FromRows("2222")).Count);
        }

        [Test]
        public void Find_RunOfTwoIsNotAMatch()
        {
            Assert.IsFalse(Matcher.HasMatch(TestGrids.FromRows("110", "001")));
        }

        [Test]
        public void Find_CellInTwoRunsCountsOnce()
        {
            var grid = TestGrids.FromRows("111", "1..", "1..");
            Assert.AreEqual(5, Matcher.Find(grid).Count);
        }

        [Test]
        public void Find_EmptyCellsNeverMatch()
        {
            Assert.IsFalse(Matcher.HasMatch(TestGrids.FromRows("...")));
        }
    }
}