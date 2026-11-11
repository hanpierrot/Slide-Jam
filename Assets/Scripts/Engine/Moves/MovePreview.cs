using System.Collections.Generic;

namespace SlideJam.Engine
{
    public sealed class MovePreview
    {
        public IReadOnlyList<Cell> Cells { get; }
        public int ToQueue { get; }
        public bool Lose { get; }

        internal MovePreview(IReadOnlyList<Cell> cells, int toQueue, bool lose)
        {
            Cells = cells;
            ToQueue = toQueue;
            Lose = lose;
        }
    }
}