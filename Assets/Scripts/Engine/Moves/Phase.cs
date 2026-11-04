using System;
using System.Collections.Generic;

namespace SlideJam.Engine
{
    public sealed class Phase
    {
        public PhaseKind Kind { get; }
        public BoardState Snap { get; }
        public int Round { get; }
        public IReadOnlyList<Cell> MatchedCells { get; }
        public IReadOnlyList<FallStart> Starts { get; }

        private Phase(PhaseKind kind, BoardState snap, int round,
            IReadOnlyList<Cell> matchedCells, IReadOnlyList<FallStart> starts)
        {
            Kind = kind;
            Snap = snap;
            Round = round;
            MatchedCells = matchedCells;
            Starts = starts;
        }

        internal static Phase Shift(BoardState snap)
        {
            return new Phase(PhaseKind.Shift, snap, 0, Array.Empty<Cell>(), Array.Empty<FallStart>());
        }

        internal static Phase Clear(BoardState snap, int round, IReadOnlyList<Cell> matchedCells)
        {
            return new Phase(PhaseKind.Clear, snap, round, matchedCells, Array.Empty<FallStart>());
        }

        internal static Phase Fall(BoardState snap, IReadOnlyList<FallStart> starts)
        {
            return new Phase(PhaseKind.Fall, snap, 0, Array.Empty<Cell>(), starts);
        }
    }
}