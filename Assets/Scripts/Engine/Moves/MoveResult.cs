using System;
using System.Collections.Generic;

namespace SlideJam.Engine
{
    public sealed class MoveResult
    {
        public bool Ok { get; }
        public BoardState State { get; }
        public IReadOnlyList<Phase> Phases { get; }

        internal MoveResult(bool ok, BoardState state, IReadOnlyList<Phase> phases)
        {
            Ok = ok;
            State = state;
            Phases = phases;
        }

        internal static MoveResult Rejected(BoardState state)
        {
            return new MoveResult(false, state, Array.Empty<Phase>());
        }
    }
}