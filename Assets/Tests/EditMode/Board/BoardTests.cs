using NUnit.Framework;
using SlideJam.Engine;

namespace SlideJam.Tests.EditMode
{
    public class BoardTests
    {
        [Test]
        public void Shift_RowMovesRightAndWraps()
        {
            var grid = TestGrids.FromRows("123");
            Board.Shift(grid, Axis.Row, 0, 1);
            Assert.AreEqual(new[] { "312" }, TestGrids.ToRows(grid));
        }

        [Test]
        public void Shift_ColumnMovesDownAndWraps()
        {
            var grid = TestGrids.FromRows("1", "2", "3");
            Board.Shift(grid, Axis.Col, 0, 1);
            Assert.AreEqual(new[] { "3", "1", "2" }, TestGrids.ToRows(grid));
        }

        [Test]
        public void Shift_NegativeNotchesSlideTheOtherWay()
        {
            var grid = TestGrids.FromRows("123");
            int k = Board.ShiftAmount(-1, 1, 3);
            Board.Shift(grid, Axis.Row, 0, k);
            Assert.AreEqual(new[] { "231" }, TestGrids.ToRows(grid));
        }

        [Test]
        public void Shift_FullRevolutionChangesNothing()
        {
            var grid = TestGrids.FromRows("123");
            Board.Shift(grid, Axis.Row, 0, 3);
            Assert.AreEqual(new[] { "123" }, TestGrids.ToRows(grid));
        }

        [Test]
        public void Shift_OnlyTouchesItsLine()
        {
            var grid = TestGrids.FromRows("123", "456", "789");
            Board.Shift(grid, Axis.Row, 1, 1);
            Assert.AreEqual(new[] { "123", "645", "789" }, TestGrids.ToRows(grid));
        }

        [Test]
        public void ShiftAmount_SpringOnEvenLineCanReachZero()
        {
            Assert.AreEqual(0, Board.ShiftAmount(2, 2, 4));
            Assert.AreEqual(2, Board.ShiftAmount(1, 2, 7));
        }

        [Test]
        public void FloorMod_IsNeverNegative()
        {
            Assert.AreEqual(2, Board.FloorMod(-1, 3));
            Assert.AreEqual(0, Board.FloorMod(-3, 3));
        }
    }
}