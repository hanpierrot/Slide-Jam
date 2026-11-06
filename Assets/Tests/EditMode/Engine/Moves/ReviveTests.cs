using System.Collections.Generic;
using NUnit.Framework;
using SlideJam.Engine;

namespace SlideJam.Tests.EditMode
{
    public class ReviveTests
    {
        private static BoardState Lost()
        {
            BoardState state = ResolutionTests.Craft(ResolutionTests.Scenario, 3, 2, 2, false, TestLevels.Targets(0, 1, 3));
            return ResolutionTests.Slide(state).State;
        }

        private static List<(int, int)> Blocks(BoardState state)
        {
            var list = new List<(int, int)>();
            foreach (Block block in state.Grid) list.Add((block.Id, block.Color));
            list.Sort();
            return list;
        }

        [Test]
        public void Revive_ClearsTheQueueAndReopensTheRun()
        {
            BoardState lost = Lost();

            MoveResult result = MoveResolver.Revive(lost);

            Assert.IsTrue(result.Ok);
            Assert.IsNull(result.State.Ended);
            Assert.AreEqual(0, result.State.Queue.Count);
            Assert.AreEqual(1, result.Phases.Count);
            Assert.AreEqual(PhaseKind.Revive, result.Phases[0].Kind);
            Assert.AreEqual(3, result.Phases[0].Vanished);
            Assert.AreEqual(lost.Moves, result.State.Moves);
        }

        [Test]
        public void Revive_ShufflesTheSameBlocksIntoAGridWithoutAMatch()
        {
            BoardState lost = Lost();

            BoardState revived = MoveResolver.Revive(lost).State;

            CollectionAssert.AreEqual(Blocks(lost), Blocks(revived));
            Assert.IsFalse(Matcher.HasMatch(revived.Grid));
            foreach (Block block in revived.Grid) Assert.IsFalse(block.IsEmpty);
        }

        [Test]
        public void Revive_ChangesNothingElse()
        {
            BoardState lost = Lost();

            BoardState revived = MoveResolver.Revive(lost).State;

            CollectionAssert.AreEqual(lost.Ptr, revived.Ptr);
            CollectionAssert.AreEqual(lost.Slots, revived.Slots);
            CollectionAssert.AreEqual(lost.Pre, revived.Pre);
            Assert.AreEqual(lost.NextId, revived.NextId);
            Assert.AreEqual(lost.QOpen, revived.QOpen);
            Assert.AreEqual(lost.SeqI, revived.SeqI);
            Assert.AreEqual(lost.Done, revived.Done);
        }

        [Test]
        public void Revive_IsDeterministicAndLeavesTheOldStateAlone()
        {
            BoardState lost = Lost();
            uint before = lost.Hash();

            uint first = MoveResolver.Revive(lost).State.Hash();
            uint second = MoveResolver.Revive(lost).State.Hash();

            Assert.AreEqual(first, second);
            Assert.AreEqual(before, lost.Hash());
        }

        [Test]
        public void Revive_IsRefusedUnlessTheRunWasLostToTheQueue()
        {
            BoardState running = ResolutionTests.Craft(ResolutionTests.Scenario, 3, 5, 5, false, TestLevels.Targets(0, 1, 3));
            BoardState won = ResolutionTests.Slide(
                ResolutionTests.Craft(ResolutionTests.Scenario, 3, 5, 5, false, TestLevels.Targets(2))).State;

            foreach (BoardState state in new[] { running, won })
            {
                uint before = state.Hash();
                MoveResult result = MoveResolver.Revive(state);

                Assert.IsFalse(result.Ok);
                Assert.AreSame(state, result.State);
                Assert.AreEqual(before, state.Hash());
            }
        }

        [Test]
        public void Revive_TheRunTakesMovesAgain()
        {
            BoardState revived = MoveResolver.Revive(Lost()).State;

            MoveResult result = MoveResolver.Move(revived, Axis.Row, 0, 1);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(revived.Moves + 1, result.State.Moves);
        }

        [Test]
        public void Revive_SweepKeepsTheBoardFullAndCleanAfterManyLosses()
        {
            Target[] seq = TestLevels.Targets(0, 1, 2, 3, 4, 0);
            int revived = 0;

            for (uint seed = 1; seed <= 60; seed++)
            {
                BoardState state = BoardState.New(TestLevels.Make(7, 7, seed, 3, 3, 3, false, seq));
                for (int i = 0; i < 120 && !(state.Ended == EndReason.Win); i++)
                {
                    if (state.Ended == EndReason.QueueOverflow)
                    {
                        MoveResult revive = MoveResolver.Revive(state);
                        Assert.IsTrue(revive.Ok, $"seed {seed} move {i}");
                        state = revive.State;
                        revived++;
                        Assert.AreEqual(0, state.Queue.Count);
                        Assert.IsFalse(Matcher.HasMatch(state.Grid), $"seed {seed} move {i}");
                        foreach (Block block in state.Grid) Assert.IsFalse(block.IsEmpty, $"seed {seed} move {i}");
                        continue;
                    }

                    Axis axis = i % 2 == 0 ? Axis.Row : Axis.Col;
                    MoveResult result = MoveResolver.Move(state, axis, (i * 3) % 7, 1 + i % 3);
                    Assert.IsTrue(result.Ok, $"seed {seed} move {i}");
                    state = result.State;
                    foreach (Block block in state.Grid) Assert.IsFalse(block.IsEmpty, $"seed {seed} move {i}");
                }
            }

            Assert.Greater(revived, 0);
        }
    }
}