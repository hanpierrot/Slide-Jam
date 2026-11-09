using System;
using System.Collections.Generic;
using NUnit.Framework;
using SlideJam.Engine;

namespace SlideJam.Tests.EditMode
{
    public class BoardStateTests
    {
        private static LevelData Level(uint seed, int rows = 6, int cols = 6) => LevelData.Sandbox(rows, cols, 5, seed);

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
            Assert.Throws<System.ArgumentOutOfRangeException>(() => LevelData.Sandbox(2, 6, 5, 1u));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => LevelData.Sandbox(6, 2, 5, 1u));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => LevelData.Sandbox(6, 6, 2, 1u));
        }
        
    private static BoardState Custom()
    {
        return BoardState.New(TestLevels.Make(6, 6, 20261031u, 3, 5, 3, false, TestLevels.Targets(0, 1, 2, 3)));
    }

    [Test]
    public void New_FillsTheSlotsFromTheFirstTargets()
    {
        var seq = new[] { new Target(0, 3), new Target(1, 4), new Target(2, 5), new Target(3, 3) };
        BoardState state = BoardState.New(TestLevels.Make(6, 6, 1u, 3, 5, 3, false, seq));

        Assert.AreEqual(3, state.Slots.Length);
        Assert.AreEqual(1, state.Slots[1].Color);
        Assert.AreEqual(4, state.Slots[1].Cap);
        Assert.AreEqual(0, state.Slots[1].Count);
        Assert.AreEqual(5, state.Slots[2].Cap);
        Assert.AreEqual(3, state.SeqI);
        Assert.AreEqual(3, state.QOpen);
        Assert.AreEqual(0, state.Queue.Count);
        Assert.AreEqual(0, state.Vault.Count);
        Assert.AreEqual(0, state.Done);
        Assert.IsNull(state.Ended);
        CollectionAssert.AreEqual(new int[4], state.Pre);
    }

    [Test]
    public void New_FewerTargetsThanSlotsLeavesEmptySlots()
    {
        BoardState state = BoardState.New(TestLevels.Make(6, 6, 1u, 3, 5, 3, false, TestLevels.Targets(2)));

        Assert.IsFalse(state.Slots[0].IsEmpty);
        Assert.IsTrue(state.Slots[1].IsEmpty);
        Assert.IsTrue(state.Slots[2].IsEmpty);
        Assert.AreEqual(1, state.SeqI);
    }

    [Test]
    public void Clone_SeparatesTheNewFields()
    {
        BoardState original = Custom();
        uint before = original.Hash();
        BoardState clone = original.Clone();
        Assert.AreEqual(before, clone.Hash());

        clone.Queue.Add(1);
        clone.Vault.Add(1);
        clone.Slots[0] = clone.Slots[0].WithCount(2);
        clone.Seq[0] = new Target(4, 3);
        clone.Pre[0] = 1;
        clone.QOpen++;
        clone.SeqI++;
        clone.Done++;
        clone.Ended = EndReason.Win;

        Assert.AreEqual(before, original.Hash());
        Assert.AreEqual(0, original.Queue.Count);
        Assert.AreEqual(0, original.Vault.Count);
        Assert.AreEqual(0, original.Slots[0].Count);
        Assert.AreEqual(0, original.Seq[0].Color);
        Assert.AreEqual(0, original.Pre[0]);
        Assert.IsNull(original.Ended);
    }

    [Test]
    public void Hash_ChangesWithEachNewField()
    {
        var mutations = new (string Name, Action<BoardState> Apply)[]
        {
            ("queue", s => s.Queue.Add(1)),
            ("vault", s => s.Vault.Add(1)),
            ("slot count", s => s.Slots[0] = s.Slots[0].WithCount(1)),
            ("seq", s => s.Seq[0] = new Target(4, 3)),
            ("pre", s => s.Pre[0] = 1),
            ("qOpen", s => s.QOpen++),
            ("seqI", s => s.SeqI++),
            ("done", s => s.Done++),
            ("ended", s => s.Ended = EndReason.QueueOverflow),
        };

        BoardState original = Custom();
        uint before = original.Hash();
        foreach ((string name, Action<BoardState> apply) in mutations)
        {
            BoardState clone = original.Clone();
            apply(clone);
            Assert.AreNotEqual(before, clone.Hash(), name);
        }
    }

    [Test]
    public void LevelData_RejectsBadTargetData()
    {
        Target[] ok = TestLevels.Targets(0, 1, 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => TestLevels.Make(6, 6, 1u, 0, 5, 5, false, ok));
        Assert.Throws<ArgumentOutOfRangeException>(() => TestLevels.Make(6, 6, 1u, 3, 0, 1, false, ok));
        Assert.Throws<ArgumentOutOfRangeException>(() => TestLevels.Make(6, 6, 1u, 3, 5, 0, false, ok));
        Assert.Throws<ArgumentOutOfRangeException>(() => TestLevels.Make(6, 6, 1u, 3, 5, 6, false, ok));
        Assert.Throws<ArgumentException>(() => TestLevels.Make(6, 6, 1u, 3, 5, 5, false, new Target[0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => TestLevels.Make(6, 6, 1u, 3, 5, 5, false, new[] { new Target(0, 0) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => TestLevels.Make(6, 6, 1u, 3, 5, 5, false, new[] { new Target(5, 3) }));
    }
    }
}