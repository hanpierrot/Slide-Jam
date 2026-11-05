using NUnit.Framework;
using SlideJam.Engine;

namespace SlideJam.Tests.EditMode
{
    public class EndTests
    {
        private static readonly string[] Twin = { "230", "231", "342" };

        private static int EmptyCells(BoardState state)
        {
            int empty = 0;
            foreach (Block block in state.Grid)
            {
                if (block.IsEmpty) empty++;
            }

            return empty;
        }

        [Test]
        public void Move_QueueOverflowEndsTheMoveBeforeTheSettle()
        {
            BoardState state = ResolutionTests.Craft(ResolutionTests.Scenario, 3, 2, 2, false, TestLevels.Targets(0, 1, 3));

            MoveResult result = ResolutionTests.Slide(state);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(3, result.Phases.Count);
            Phase end = result.Phases[2];
            Assert.AreEqual(PhaseKind.End, end.Kind);
            Assert.AreEqual(EndReason.QueueOverflow, end.Reason);
            Assert.AreEqual(EndReason.QueueOverflow, result.State.Ended);
            Assert.AreEqual(3, result.State.Queue.Count);
            Assert.AreEqual(1, result.State.Moves);
            Assert.AreEqual(3, EmptyCells(result.State));
        }

        [Test]
        public void Move_CompletingTheLastTargetWins()
        {
            BoardState state = ResolutionTests.Craft(ResolutionTests.Scenario, 3, 5, 5, false, TestLevels.Targets(2));

            MoveResult result = ResolutionTests.Slide(state);

            Assert.AreEqual(4, result.Phases.Count);
            Assert.AreEqual(PhaseKind.Complete, result.Phases[2].Kind);
            Assert.AreEqual(PhaseKind.End, result.Phases[3].Kind);
            Assert.AreEqual(EndReason.Win, result.Phases[3].Reason);
            Assert.AreEqual(EndReason.Win, result.State.Ended);
            Assert.AreEqual(1, result.State.Done);
            foreach (Slot slot in result.State.Slots) Assert.IsTrue(slot.IsEmpty);
        }

        [Test]
        public void Move_AnOverflowInsideARoundIsRescuedByThePullOfACompletion()
        {
            BoardState state = ResolutionTests.Craft(Twin, 3, 5, 2, false, TestLevels.Targets(2, 0, 1, 3));

            MoveResult result = ResolutionTests.Slide(state);

            Assert.AreEqual(3, result.Phases[1].Snap.Queue.Count);
            Assert.AreEqual(2, result.Phases[1].Snap.QOpen);
            Assert.IsNull(result.State.Ended);
            Assert.AreEqual(0, result.State.Queue.Count);
            Assert.GreaterOrEqual(result.State.Done, 2);
            foreach (Phase phase in result.Phases) Assert.AreNotEqual(PhaseKind.End, phase.Kind);
        }

        [Test]
        public void Move_AnOverflowInsideARoundIsRescuedByTheChairACompletionOpens()
        {
            BoardState state = ResolutionTests.Craft(Twin, 3, 5, 2, true, TestLevels.Targets(2, 0, 1, 4));

            MoveResult result = ResolutionTests.Slide(state);

            Assert.AreEqual(PhaseKind.Complete, result.Phases[2].Kind);
            Assert.AreEqual(3, result.Phases[2].Snap.QOpen);
            Assert.AreEqual(3, result.Phases[2].Snap.Queue.Count);
            Assert.IsNull(result.State.Ended);
        }

        [Test]
        public void Move_TheSameMoveWithoutAChairOpeningLoses()
        {
            BoardState state = ResolutionTests.Craft(Twin, 3, 5, 2, false, TestLevels.Targets(2, 0, 1, 4));

            MoveResult result = ResolutionTests.Slide(state);

            Assert.AreEqual(4, result.Phases.Count);
            Assert.AreEqual(PhaseKind.End, result.Phases[3].Kind);
            Assert.AreEqual(EndReason.QueueOverflow, result.State.Ended);
            Assert.AreEqual(3, result.State.Queue.Count);
            Assert.AreEqual(2, result.State.QOpen);
        }

        [Test]
        public void Move_OnlyTheEndPhaseCarriesAReason()
        {
            BoardState state = ResolutionTests.Craft(ResolutionTests.Scenario, 3, 5, 5, false, TestLevels.Targets(2));

            MoveResult result = ResolutionTests.Slide(state);

            for (int i = 0; i < result.Phases.Count - 1; i++) Assert.IsNull(result.Phases[i].Reason);
            Assert.IsNotNull(result.Phases[result.Phases.Count - 1].Reason);
        }

        [Test]
        public void Move_AFinishedRunTakesNoMoreMoves()
        {
            BoardState won = ResolutionTests.Slide(
                ResolutionTests.Craft(ResolutionTests.Scenario, 3, 5, 5, false, TestLevels.Targets(2))).State;
            BoardState lost = ResolutionTests.Slide(
                ResolutionTests.Craft(ResolutionTests.Scenario, 3, 2, 2, false, TestLevels.Targets(0, 1, 3))).State;

            foreach (BoardState finished in new[] { won, lost })
            {
                uint before = finished.Hash();
                MoveResult result = MoveResolver.Move(finished, Axis.Row, 0, 1);

                Assert.IsFalse(result.Ok);
                Assert.AreSame(finished, result.State);
                Assert.AreEqual(0, result.Phases.Count);
                Assert.AreEqual(before, finished.Hash());
            }
        }
    }
}