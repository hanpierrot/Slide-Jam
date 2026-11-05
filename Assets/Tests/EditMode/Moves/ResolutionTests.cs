using System.Collections.Generic;
using NUnit.Framework;
using SlideJam.Engine;

namespace SlideJam.Tests.EditMode
{
    public class ResolutionTests
    {
        private const uint Seed = 20261031u;

        internal static readonly string[] Scenario = { "234", "204", "132" };

        internal static BoardState Craft(string[] rows, int activeSlots, int queueMax, int qOpenStart, bool qGrow, Target[] seq)
        {
            LevelData level = TestLevels.Make(rows.Length, rows[0].Length, Seed, activeSlots, queueMax, qOpenStart, qGrow, seq);
            BoardState state = BoardState.New(level);
            Block[,] crafted = TestGrids.FromRows(rows);
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < rows[0].Length; c++) state.Grid[r, c] = crafted[r, c];
            }

            return state;
        }

        public static MoveResult Slide(BoardState state) => MoveResolver.Move(state, Axis.Row, 2, 1);

        private static BoardState ThreeTwosInARow(Target[] seq, out List<Cell> cells)
        {
            cells = new List<Cell> { new Cell(0, 0), new Cell(0, 1), new Cell(0, 2) };
            return Craft(new[] { "222", "013", "340" }, 3, 5, 5, false, seq);
        }

        [Test]
        public void RouteUnits_Round1QueuesWhatNoSlotWants()
        {
            BoardState state = ThreeTwosInARow(TestLevels.Targets(0, 1, 3), out List<Cell> cells);

            List<Flight> flights = Resolution.RouteUnits(state, cells, 1, out int vanished);

            Assert.AreEqual(0, vanished);
            CollectionAssert.AreEqual(new[] { 2, 2, 2 }, state.Queue);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(PlaceKind.Grid, flights[i].From.Kind);
                Assert.AreEqual(i, flights[i].From.B);
                Assert.AreEqual(PlaceKind.Queue, flights[i].To.Kind);
                Assert.AreEqual(i, flights[i].To.A);
                Assert.IsTrue(state.Grid[0, i].IsEmpty);
            }
        }

        [Test]
        public void RouteUnits_LaterRoundsVanishInsteadOfQueueing()
        {
            BoardState state = ThreeTwosInARow(TestLevels.Targets(0, 1, 3), out List<Cell> cells);

            List<Flight> flights = Resolution.RouteUnits(state, cells, 2, out int vanished);

            Assert.AreEqual(3, vanished);
            Assert.AreEqual(0, state.Queue.Count);
            foreach (Flight flight in flights) Assert.AreEqual(PlaceKind.Vanish, flight.To.Kind);
        }

        [Test]
        public void RouteUnits_ASlotTakesUnitsUntilItsCapThenTheRoundDecides()
        {
            BoardState round1 = ThreeTwosInARow(TestLevels.Targets(2, 0, 1), out List<Cell> cells1);
            round1.Slots[0] = new Slot(2, 3, 2, false);

            List<Flight> flights = Resolution.RouteUnits(round1, cells1, 1, out int vanished1);

            Assert.AreEqual(PlaceKind.Slot, flights[0].To.Kind);
            Assert.AreEqual(0, flights[0].To.A);
            Assert.AreEqual(2, flights[0].To.B);
            Assert.AreEqual(3, round1.Slots[0].Count);
            Assert.AreEqual(0, vanished1);
            CollectionAssert.AreEqual(new[] { 2, 2 }, round1.Queue);

            BoardState round2 = ThreeTwosInARow(TestLevels.Targets(2, 0, 1), out List<Cell> cells2);
            round2.Slots[0] = new Slot(2, 3, 2, false);

            Resolution.RouteUnits(round2, cells2, 2, out int vanished2);

            Assert.AreEqual(2, vanished2);
            Assert.AreEqual(0, round2.Queue.Count);
        }

        [Test]
        public void RouteUnits_SkipsLockedAndFullSlots()
        {
            BoardState state = ThreeTwosInARow(TestLevels.Targets(0, 1, 3), out List<Cell> cells);
            state.Slots[0] = new Slot(2, 3, 0, true);
            state.Slots[1] = new Slot(2, 3, 1, false);

            List<Flight> flights = Resolution.RouteUnits(state, cells, 1, out _);

            Assert.AreEqual(1, flights[0].To.A);
            Assert.AreEqual(1, flights[0].To.B);
            Assert.AreEqual(1, flights[1].To.A);
            Assert.AreEqual(2, flights[1].To.B);
            Assert.AreEqual(PlaceKind.Queue, flights[2].To.Kind);
            Assert.AreEqual(0, state.Slots[0].Count);
        }

        [Test]
        public void Move_UnitsNoSlotWantsGoToTheQueueInRowMajorOrder()
        {
            BoardState state = Craft(Scenario, 3, 5, 5, false, TestLevels.Targets(0, 1, 3));

            MoveResult result = Slide(state);

            Phase clear = result.Phases[1];
            Assert.AreEqual(PhaseKind.Clear, clear.Kind);
            Assert.AreEqual(3, clear.Flights.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(i, clear.Flights[i].From.A);
                Assert.AreEqual(0, clear.Flights[i].From.B);
                Assert.AreEqual(PlaceKind.Queue, clear.Flights[i].To.Kind);
            }

            CollectionAssert.AreEqual(new[] { 2, 2, 2 }, clear.Snap.Queue);
            Assert.AreEqual(0, clear.Vanished);
        }

        [Test]
        public void Move_AFullSlotCompletesAndTakesTheNextTarget()
        {
            BoardState state = Craft(Scenario, 3, 5, 5, false, TestLevels.Targets(2, 0, 1, 3));

            MoveResult result = Slide(state);

            Phase clear = result.Phases[1];
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(PlaceKind.Slot, clear.Flights[i].To.Kind);
                Assert.AreEqual(0, clear.Flights[i].To.A);
                Assert.AreEqual(i, clear.Flights[i].To.B);
            }

            Assert.AreEqual(3, clear.Snap.Slots[0].Count);

            Phase complete = result.Phases[2];
            Assert.AreEqual(PhaseKind.Complete, complete.Kind);
            Assert.AreEqual(0, complete.Slot);
            Assert.AreEqual(1, complete.Snap.Done);
            Assert.AreEqual(3, complete.Snap.Slots[0].Color);
            Assert.AreEqual(0, complete.Snap.Slots[0].Count);
            Assert.AreEqual(4, complete.Snap.SeqI);
            Assert.AreEqual(PhaseKind.Fall, result.Phases[3].Kind);
        }

        [Test]
        public void Move_AfterACompletionTheQueuePullsIntoTheNewTarget()
        {
            BoardState state = Craft(Scenario, 1, 5, 5, false, TestLevels.Targets(2, 0, 1));
            state.Queue.AddRange(new[] { 0, 0, 3 });

            MoveResult result = Slide(state);

            Assert.AreEqual(PhaseKind.Complete, result.Phases[2].Kind);
            Phase pull = result.Phases[3];
            Assert.AreEqual(PhaseKind.Pull, pull.Kind);
            Assert.AreEqual(2, pull.Flights.Count);
            Assert.AreEqual(PlaceKind.Queue, pull.Flights[0].From.Kind);
            Assert.AreEqual(0, pull.Flights[0].From.A);
            Assert.AreEqual(1, pull.Flights[1].From.A);
            Assert.AreEqual(0, pull.Flights[0].To.A);
            Assert.AreEqual(0, pull.Flights[0].To.B);
            Assert.AreEqual(1, pull.Flights[1].To.B);
            CollectionAssert.AreEqual(new[] { 3 }, pull.Snap.Queue);
            Assert.AreEqual(2, pull.Snap.Slots[0].Count);
        }

        [Test]
        public void Move_APullThatFillsASlotCompletesItToo()
        {
            BoardState state = Craft(Scenario, 1, 5, 5, false, TestLevels.Targets(2, 0, 1));
            state.Queue.AddRange(new[] { 0, 0, 0 });

            MoveResult result = Slide(state);

            Assert.AreEqual(PhaseKind.Complete, result.Phases[2].Kind);
            Assert.AreEqual(PhaseKind.Pull, result.Phases[3].Kind);
            Assert.AreEqual(3, result.Phases[3].Snap.Slots[0].Count);
            Assert.AreEqual(PhaseKind.Complete, result.Phases[4].Kind);
            Assert.AreEqual(2, result.Phases[4].Snap.Done);
            Assert.AreEqual(1, result.Phases[4].Snap.Slots[0].Color);
            Assert.AreEqual(0, result.Phases[4].Snap.Queue.Count);
            Assert.AreEqual(PhaseKind.Fall, result.Phases[5].Kind);
        }

        [Test]
        public void Move_CompletionOpensAQueueChairOnlyWithQGrow()
        {
            Target[] seq = TestLevels.Targets(2, 0, 1, 3);

            MoveResult grow = Slide(Craft(Scenario, 3, 5, 2, true, seq));
            MoveResult noGrow = Slide(Craft(Scenario, 3, 5, 2, false, seq));

            Assert.AreEqual(3, grow.Phases[2].Snap.QOpen);
            Assert.AreEqual(2, noGrow.Phases[2].Snap.QOpen);
        }

        [Test]
        public void Move_QOpenNeverPassesQueueMax()
        {
            MoveResult result = Slide(Craft(Scenario, 3, 5, 5, true, TestLevels.Targets(2, 0, 1, 3)));

            Assert.AreEqual(5, result.Phases[2].Snap.QOpen);
        }

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void Move_ATargetCompletesAtExactlyItsOwnCap(int cap)
        {
            BoardState state = Craft(Scenario, 3, 5, 5, false, TestLevels.TargetsWithCap(cap, 2, 0, 1, 3));
            state.Slots[0] = new Slot(2, cap, cap - 3, false);

            MoveResult result = Slide(state);

            Assert.AreEqual(cap, result.Phases[1].Snap.Slots[0].Count);
            Assert.AreEqual(PhaseKind.Complete, result.Phases[2].Kind);
        }

        [TestCase(4)]
        [TestCase(5)]
        public void Move_ATargetOneUnitShortOfItsCapDoesNotComplete(int cap)
        {
            BoardState state = Craft(Scenario, 3, 5, 5, false, TestLevels.TargetsWithCap(cap, 2, 0, 1, 3));
            state.Slots[0] = new Slot(2, cap, cap - 4, false);

            MoveResult result = Slide(state);

            Assert.AreEqual(cap - 1, result.Phases[1].Snap.Slots[0].Count);
            Assert.AreEqual(PhaseKind.Fall, result.Phases[2].Kind);
        }

        [Test]
        public void Move_OnlyRoundOneCanQueueUnits()
        {
            int queued = 0;
            int cascadeClears = 0;
            int vanished = 0;

            for (uint seed = 1; seed <= 30; seed++)
            {
                BoardState state = BoardState.New(LevelData.Sandbox(7, 7, 5, seed));
                for (int i = 0; i < 25; i++)
                {
                    Axis axis = i % 2 == 0 ? Axis.Row : Axis.Col;
                    MoveResult result = MoveResolver.Move(state, axis, (i * 3) % 7, 1 + i % 3);

                    foreach (Phase phase in result.Phases)
                    {
                        if (phase.Kind != PhaseKind.Clear) continue;

                        int vanishFlights = 0;
                        if (phase.Round >= 2) cascadeClears++;
                        foreach (Flight flight in phase.Flights)
                        {
                            if (phase.Round >= 2) Assert.AreNotEqual(PlaceKind.Queue, flight.To.Kind);
                            if (phase.Round == 1 && flight.To.Kind == PlaceKind.Queue) queued++;
                            if (flight.To.Kind == PlaceKind.Vanish) vanishFlights++;
                        }

                        Assert.AreEqual(vanishFlights, phase.Vanished);
                        vanished += vanishFlights;
                    }

                    state = result.State;
                }
            }

            Assert.Greater(queued, 0);
            Assert.Greater(cascadeClears, 0);
            Assert.Greater(vanished, 0);
        }
    }
}