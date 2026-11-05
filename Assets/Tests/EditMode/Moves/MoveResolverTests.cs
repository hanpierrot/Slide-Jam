using NUnit.Framework;
using SlideJam.Engine;

namespace SlideJam.Tests.EditMode
{
    public class MoveResolverTests
    {
        private const uint Seed = 20261031u;
        private const int Colors = 5;

        private static LevelData Level(uint seed, int size = 6) => LevelData.Sandbox(size, size, Colors, seed);

        private static BoardState StateWith(uint seed, params string[] rows)
        {
            BoardState state = BoardState.New(LevelData.Sandbox(rows.Length, rows[0].Length, Colors, seed));
            Block[,] crafted = TestGrids.FromRows(rows);
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < rows[0].Length; c++) state.Grid[r, c] = crafted[r, c];
            }

            return state;
        }

        private static int Spawn(int column, int i) => ColumnStream.At(Seed, column, i, Colors);

        private static void AssertFullAndClean(BoardState state)
        {
            foreach (Block block in state.Grid) Assert.IsFalse(block.IsEmpty);
            Assert.IsFalse(Matcher.HasMatch(state.Grid));
        }

        private static MoveResult Pattern(BoardState state, int i)
        {
            int size = state.Level.Rows;
            Axis axis = i % 2 == 0 ? Axis.Row : Axis.Col;
            return MoveResolver.Move(state, axis, (i * 3) % size, 1 + i % 3);
        }
        
        private static Phase FirstOf(MoveResult result, PhaseKind kind)
        {
            foreach (Phase phase in result.Phases)
            {
                if (phase.Kind == kind) return phase;
            }

            Assert.Fail($"No {kind} phase.");
            return null;
        }

        [Test]
        public void Move_ZeroShiftIsNotAMove()
        {
            BoardState state = BoardState.New(Level(Seed));
            foreach (int notches in new[] { 0, 6, -6 })
            {
                MoveResult result = MoveResolver.Move(state, Axis.Row, 0, notches);
                Assert.IsFalse(result.Ok);
                Assert.AreSame(state, result.State);
                Assert.AreEqual(0, result.Phases.Count);
            }
        }

        [Test]
        public void Move_DoesNotMutateTheInput()
        {
            BoardState state = BoardState.New(Level(Seed));
            uint before = state.Hash();
            MoveResult result = MoveResolver.Move(state, Axis.Row, 0, 1);
            Assert.IsTrue(result.Ok);
            Assert.AreEqual(before, state.Hash());
            Assert.AreEqual(0, state.Moves);
            Assert.AreEqual(1, result.State.Moves);
        }

        [Test]
        public void Move_AlwaysStartsWithTheShiftPhase()
        {
            MoveResult result = MoveResolver.Move(BoardState.New(Level(Seed)), Axis.Col, 2, 1);
            Assert.AreEqual(PhaseKind.Shift, result.Phases[0].Kind);
        }

        [Test]
        public void Move_WithoutAMatchHasOnlyTheShiftPhase()
        {
            BoardState state = StateWith(Seed, "012", "120", "201");
            MoveResult result = MoveResolver.Move(state, Axis.Row, 0, 1);
            Assert.AreEqual(1, result.Phases.Count);
            Assert.AreEqual(new[] { "201", "120", "201" }, TestGrids.ToRows(result.State.Grid));
        }

        [Test]
        public void Move_ClearedColumnIsRefilledFromItsStream()
        {
            BoardState state = StateWith(Seed, "234", "204", "132");
            MoveResult result = MoveResolver.Move(state, Axis.Row, 2, 1);

            Phase clear = result.Phases[1];
            Assert.AreEqual(PhaseKind.Clear, clear.Kind);
            Assert.AreEqual(1, clear.Round);
            Assert.AreEqual(3, clear.MatchedCells.Count);
            for (int r = 0; r < 3; r++)
            {
                Assert.AreEqual(r, clear.MatchedCells[r].Row);
                Assert.AreEqual(0, clear.MatchedCells[r].Col);
            }

            Phase fall = FirstOf(result, PhaseKind.Fall);
            Assert.AreEqual(PhaseKind.Fall, fall.Kind);
            Block[,] grid = fall.Snap.Grid;
            
            Assert.AreEqual(Spawn(0, 2), grid[0, 0].Color);
            Assert.AreEqual(Spawn(0, 1), grid[1, 0].Color);
            Assert.AreEqual(Spawn(0, 0), grid[2, 0].Color);
            Assert.AreEqual(3, fall.Starts.Count);
            Assert.AreEqual(-3, fall.Starts[0].From.Row);
            Assert.AreEqual(-2, fall.Starts[1].From.Row);
            Assert.AreEqual(-1, fall.Starts[2].From.Row);
            Assert.AreEqual(3, fall.Snap.Ptr[0]);
        }

        [Test]
        public void Move_BlocksAboveAClearFallOneCell()
        {
            BoardState state = StateWith(Seed, "231", "114", "023");
            MoveResult result = MoveResolver.Move(state, Axis.Col, 2, 1);

            Phase fall = FirstOf(result, PhaseKind.Fall);
            Block[,] grid = fall.Snap.Grid;
            Assert.AreEqual(new[] { "233", "024" }, new[] { TestGrids.ToRows(grid)[1], TestGrids.ToRows(grid)[2] });
            for (int c = 0; c < 3; c++) Assert.AreEqual(Spawn(c, 0), grid[0, c].Color);

            Assert.AreEqual(6, fall.Starts.Count);
            for (int c = 0; c < 3; c++)
            {
                Assert.AreEqual(-1, fall.Starts[c].From.Row);
                Assert.AreEqual(c, fall.Starts[c].From.Col);
                Assert.AreEqual(0, fall.Starts[3 + c].From.Row);
                Assert.AreEqual(c, fall.Starts[3 + c].From.Col);
            }
        }

        [Test]
        public void Move_LeavesAFullBoardWithNoMatch()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                foreach (int size in new[] { 6, 7 })
                {
                    BoardState state = BoardState.New(Level(seed, size));
                    for (int i = 0; i < 25; i++)
                    {
                        MoveResult result = Pattern(state, i);
                        Assert.IsTrue(result.Ok);
                        state = result.State;
                        AssertFullAndClean(state);
                    }
                }
            }
        }

        [Test]
        public void Move_SpawnsExactlyAsManyBlocksAsItClears()
        {
            BoardState state = BoardState.New(Level(Seed, 7));
            for (int i = 0; i < 40; i++)
            {
                MoveResult result = Pattern(state, i);
                int cleared = 0;
                foreach (Phase phase in result.Phases) cleared += phase.MatchedCells.Count;

                int spawnedBlocks = 0;
                for (int c = 0; c < state.Ptr.Length; c++) spawnedBlocks += result.State.Ptr[c] - state.Ptr[c];

                Assert.AreEqual(cleared, spawnedBlocks);
                state = result.State;
            }
        }

        [Test]
        public void Move_RoundsCountUpFromOneAndAlternateClearAndFall()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                BoardState state = BoardState.New(Level(seed, 7));
                for (int i = 0; i < 25; i++)
                {
                    MoveResult result = Pattern(state, i);
                    Assert.AreEqual(PhaseKind.Shift, result.Phases[0].Kind);

                    int round = 0;
                    int p = 1;
                    while (p < result.Phases.Count)
                    {
                        Assert.AreEqual(PhaseKind.Clear, result.Phases[p].Kind);
                        Assert.AreEqual(++round, result.Phases[p].Round);
                        p++;

                        while (p < result.Phases.Count
                               && (result.Phases[p].Kind == PhaseKind.Complete || result.Phases[p].Kind == PhaseKind.Pull)) p++;

                        Assert.Less(p, result.Phases.Count);
                        Assert.AreEqual(PhaseKind.Fall, result.Phases[p].Kind);
                        p++;
                    }

                    state = result.State;
                }
            }
        }

        [Test]
        public void Move_SameSeedAndMovesGiveTheSameHash()
        {
            uint Run()
            {
                BoardState state = BoardState.New(Level(777123u, 7));
                for (int i = 0; i < 40; i++) state = Pattern(state, i).State;
                return state.Hash();
            }

            Assert.AreEqual(Run(), Run());
        }

        [Test]
        public void Move_SameMoveFromTheSameStateGivesTheSameResult()
        {
            BoardState before = BoardState.New(Level(Seed));
            BoardState first = MoveResolver.Move(before, Axis.Row, 1, 2).State;
            BoardState second = MoveResolver.Move(before, Axis.Row, 1, 2).State;
            Assert.AreEqual(first.Hash(), second.Hash());
        }
    }
}