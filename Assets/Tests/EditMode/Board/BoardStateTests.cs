using System.Collections.Generic;
using NUnit.Framework;
using SlideJam.Engine;

namespace SlideJam.Tests.EditMode
{
    public class BoardStateTests
    {
        private static LevelData Level(uint seed, int rows = 6, int cols = 6) => new LevelData(rows, cols, 5, seed);

        [Test]
        public void New_IsFullWithUniqueIds()
        {
            BoardState state = BoardState.New(Level(20261031u, 7, 7));
            var ids = new HashSet<int>();
            foreach (Block block in state.Grid)
            {
                Assert.IsFalse(block.IsEmpty);
                Assert.IsTrue(ids.Add(block.Id));
            }

            Assert.AreEqual(49, ids.Count);
            Assert.AreEqual(50, state.NextId);
        }

        [Test]
        public void New_NeverStartsWithAMatch()
        {
            for (uint seed = 1; seed <= 60; seed++)
            {
                Assert.IsFalse(Matcher.HasMatch(BoardState.New(Level(seed)).Grid), $"seed {seed}");
                Assert.IsFalse(Matcher.HasMatch(BoardState.New(Level(seed, 7, 7)).Grid), $"seed {seed} 7x7");
            }
        }

        [Test]
        public void New_StartsWithZeroPointersAndMoves()
        {
            BoardState state = BoardState.New(Level(777123u));
            CollectionAssert.AreEqual(new int[6], state.Ptr);
            Assert.AreEqual(0, state.Moves);
        }

        [Test]
        public void New_SameLevelGivesSameHash()
        {
            Assert.AreEqual(BoardState.New(Level(777123u)).Hash(), BoardState.New(Level(777123u)).Hash());
        }

        [Test]
        public void New_DifferentSeedsGiveDifferentHashes()
        {
            Assert.AreNotEqual(BoardState.New(Level(20261031u)).Hash(), BoardState.New(Level(20261102u)).Hash());
        }

        [Test]
        public void Clone_IsIndependentAndEqualAtFirst()
        {
            BoardState original = BoardState.New(Level(20261031u));
            BoardState clone = original.Clone();
            Assert.AreEqual(original.Hash(), clone.Hash());

            uint before = original.Hash();
            clone.Grid[0, 0] = Block.Empty;
            clone.Ptr[0] = 5;
            Assert.AreEqual(before, original.Hash());
            Assert.AreNotEqual(before, clone.Hash());
        }

        [Test]
        public void LevelData_RejectsTooSmallValues()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new LevelData(2, 6, 5, 1u));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new LevelData(6, 2, 5, 1u));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new LevelData(6, 6, 2, 1u));
        }
    }
}