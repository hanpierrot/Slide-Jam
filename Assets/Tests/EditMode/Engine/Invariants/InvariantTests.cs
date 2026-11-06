using NUnit.Framework;
using SlideJam.Engine;

namespace SlideJam.Tests.EditMode
{
    public class InvariantTests
    {
        private static MoveResult Pattern(BoardState state, int i)
        {
            int size = state.Level.Rows;
            Axis axis = i % 2 == 0 ? Axis.Row : Axis.Col;
            return MoveResolver.Move(state, axis, (i * 3) % size, 1 + i % 3);
        }

        [Test]
        public void Units_AreNeverCreatedOrLost()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                BoardState state = BoardState.New(LevelData.Sandbox(7, 7, 5, seed));
                int cleared = 0;
                int vanished = 0;

                for (int i = 0; i < 40; i++)
                {
                    MoveResult result = Pattern(state, i);
                    foreach (Phase phase in result.Phases)
                    {
                        if (phase.Kind != PhaseKind.Clear) continue;
                        cleared += phase.MatchedCells.Count;
                        vanished += phase.Vanished;
                    }

                    state = result.State;

                    int held = state.Queue.Count + state.Vault.Count;
                    int takenCaps = 0;
                    for (int k = 0; k < state.SeqI; k++) takenCaps += state.Seq[k].Cap;

                    int openCaps = 0;
                    foreach (Slot slot in state.Slots)
                    {
                        if (slot.IsEmpty) continue;
                        held += slot.Count;
                        openCaps += slot.Cap;
                    }

                    int completedCaps = takenCaps - openCaps;
                    Assert.AreEqual(cleared, held + completedCaps + vanished, $"seed {seed} move {i}");
                }
            }
        }

        [Test]
        public void Targets_AreAccountedFor()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                BoardState state = BoardState.New(LevelData.Sandbox(7, 7, 5, seed));
                for (int i = 0; i < 40; i++)
                {
                    state = Pattern(state, i).State;

                    int openSlots = 0;
                    foreach (Slot slot in state.Slots)
                    {
                        if (!slot.IsEmpty) openSlots++;
                    }

                    Assert.AreEqual(state.Seq.Length - state.Done, state.Seq.Length - state.SeqI + openSlots,
                        $"seed {seed} move {i}");
                }
            }
        }

        [Test]
        public void ReplayingFromAHeldStateGivesTheSameResult()
        {
            BoardState state = BoardState.New(LevelData.Sandbox(7, 7, 5, 777123u));
            BoardState held = null;
            uint heldHash = 0;
            for (int i = 0; i < 40; i++)
            {
                if (i == 15)
                {
                    held = state;
                    heldHash = state.Hash();
                }

                state = Pattern(state, i).State;
            }

            BoardState replay = held;
            for (int i = 15; i < 40; i++) replay = Pattern(replay, i).State;

            Assert.AreEqual(heldHash, held.Hash());
            Assert.AreEqual(state.Hash(), replay.Hash());
        }

        [Test]
        public void RunsEndWithTheRightStateAndEveryOtherStateIsFullAndClean()
        {
            Target[] seq = TestLevels.Targets(0, 1, 2, 3, 4, 0);
            int ended = 0;

            for (uint seed = 1; seed <= 60; seed++)
            {
                BoardState state = BoardState.New(TestLevels.Make(7, 7, seed, 3, 3, 3, false, seq));
                for (int i = 0; i < 80; i++)
                {
                    MoveResult result = Pattern(state, i);
                    Assert.IsTrue(result.Ok);
                    state = result.State;

                    if (!state.Ended.HasValue)
                    {
                        foreach (Block block in state.Grid) Assert.IsFalse(block.IsEmpty, $"seed {seed} move {i}");
                        Assert.IsFalse(Matcher.HasMatch(state.Grid), $"seed {seed} move {i}");
                        continue;
                    }

                    ended++;
                    foreach (Block block in state.Grid) Assert.IsFalse(block.IsEmpty, $"seed {seed} move {i} ended");
                    Phase last = result.Phases[result.Phases.Count - 1];
                    Assert.AreEqual(PhaseKind.End, last.Kind);
                    Assert.AreEqual(state.Ended, last.Reason);
                    if (state.Ended == EndReason.Win)
                    {
                        Assert.IsFalse(Matcher.HasMatch(state.Grid), $"seed {seed} move {i} won");
                        foreach (Slot slot in state.Slots) Assert.IsTrue(slot.IsEmpty);
                    }
                    else
                    {
                        Assert.Greater(state.Queue.Count, state.QOpen);
                    }

                    Assert.IsFalse(MoveResolver.Move(state, Axis.Row, 0, 1).Ok);
                    break;
                }
            }

            Assert.Greater(ended, 0);
        }
    }
}