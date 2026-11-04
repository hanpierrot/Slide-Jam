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

            const int step = 1;
            int k = Board.ShiftAmount(notches, step, Board.LineLength(level, axis));
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

                foreach (Cell cell in matched) next.Grid[cell.Row, cell.Col] = Block.Empty;
                phases.Add(Phase.Clear(next.Clone(), round, matched));

                List<FallStart> starts = SandSettler.Settle(next);
                phases.Add(Phase.Fall(next.Clone(), starts));
            }

            return new MoveResult(true, next, phases);
        }
    }
}