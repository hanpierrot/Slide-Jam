using System;
using System.Collections.Generic;

namespace SlideJam.Engine
{
    public static class MoveResolver
    {
        private const int MaxRounds = 500;

        public static MoveResult Move(BoardState state, Axis axis, int index, int notches)
        {
            LevelData level = state.Level;
            int lines = axis == Axis.Row ? level.Rows : level.Cols;
            if (index < 0 || index >= lines) throw new ArgumentOutOfRangeException(nameof(index));
            if (state.Ended.HasValue) return MoveResult.Rejected(state);

            if (Board.LineLocked(state, axis, index)) return MoveResult.Rejected(state);
           
            int step = Board.StepOf(state, axis, index);
            int free = Board.FreePositions(state, axis, index).Count; 
            if (free <= 1) return MoveResult.Rejected(state);
            int k = Board.ShiftAmount(notches, step, free);
            if (k == 0) return MoveResult.Rejected(state);

            BoardState next = state.Clone();
            var phases = new List<Phase>();

            Board.Shift(next.Grid, axis, index, k);
            next.Moves++;
            phases.Add(Phase.Shift(next.Clone()));

            for (int round = 1;; round++)
            {
                List<Cell> matched = Matcher.Find(next.Grid);
                if (matched.Count == 0) break;
                if (round > MaxRounds) throw new InvalidOperationException("Resolution did not finish.");

                List<Cell> consumed = Resolution.Consumable(next, matched);
                List<Flight> flights = Resolution.RouteUnits(next, consumed, round, out int vanished);
                List<int> unsealed = Resolution.OpenSeals(next, consumed);
                List<int> unchained = Resolution.BreakChains(next, matched);
                phases.Add(Phase.Clear(next.Clone(), round, consumed, flights, vanished, unsealed, unchained));

                Resolution.CompleteAndPull(next, phases);
                Resolution.CheckEnd(next);

                List<FallStart> starts = SandSettler.Settle(next);
                phases.Add(Phase.Fall(next.Clone(), starts));

                if (next.Ended == EndReason.QueueOverflow) break;
            }

            if (next.Ended.HasValue) phases.Add(Phase.End(next.Clone(), next.Ended.Value));

            return new MoveResult(true, next, phases);
        }
        
        public static MovePreview Preview(BoardState state, Axis axis, int index, int notches)
        {
            MoveResult result = Move(state, axis, index, notches);
            if (!result.Ok) return null;

            foreach (Phase phase in result.Phases)
            {
                if (phase.Kind != PhaseKind.Clear) continue;

                int toQueue = result.State.Queue.Count - state.Queue.Count;
                bool lose = result.State.Ended == EndReason.QueueOverflow;
                return new MovePreview(phase.MatchedCells, toQueue, lose);
            }

            return null;
        }
        
        public static MoveResult Revive(BoardState state)
        {
            if (state.Ended != EndReason.QueueOverflow) return MoveResult.Rejected(state);

            BoardState next = state.Clone();
            int cleared = next.Queue.Count;
            next.Queue.Clear();
            next.Ended = null;
            List<Flight> flights = Shuffler.Shuffle(next);

            return new MoveResult(true, next, new List<Phase> { Phase.Revive(next.Clone(), flights, cleared) });
        }
    }
}