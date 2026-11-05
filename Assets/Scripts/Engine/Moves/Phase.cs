using System;
using System.Collections.Generic;

namespace SlideJam.Engine
{
    public sealed class Phase
    {
        public PhaseKind Kind { get; }
        public BoardState Snap { get; }
        public int Round { get; private set; }
        public IReadOnlyList<Cell> MatchedCells { get; private set; } = Array.Empty<Cell>();
        public IReadOnlyList<FallStart> Starts { get; private set; } = Array.Empty<FallStart>();
        public IReadOnlyList<Flight> Flights { get; private set; } = Array.Empty<Flight>();
        public int Vanished { get; private set; }
        public int Slot { get; private set; }
        public EndReason? Reason { get; private set; }

        private Phase(PhaseKind kind, BoardState snap)
        {
            Kind = kind;
            Snap = snap;
        }

        internal static Phase Shift(BoardState snap)
        {
            return new Phase(PhaseKind.Shift, snap);
        }

        internal static Phase Clear(BoardState snap, int round, IReadOnlyList<Cell> matchedCells,
            IReadOnlyList<Flight> flights, int vanished)
        {
            return new Phase(PhaseKind.Clear, snap)
            {
                Round = round,
                MatchedCells = matchedCells,
                Flights = flights,
                Vanished = vanished,
            };
        }

        internal static Phase Complete(BoardState snap, int slot)
        {
            return new Phase(PhaseKind.Complete, snap) { Slot = slot };
        }

        internal static Phase Pull(BoardState snap, IReadOnlyList<Flight> flights)
        {
            return new Phase(PhaseKind.Pull, snap) { Flights = flights };
        }

        internal static Phase Fall(BoardState snap, IReadOnlyList<FallStart> starts)
        {
            return new Phase(PhaseKind.Fall, snap) { Starts = starts };
        }
        
        internal static Phase End(BoardState snap, EndReason reason)
        {
            return new Phase(PhaseKind.End, snap) { Reason = reason };
        }
    }
}