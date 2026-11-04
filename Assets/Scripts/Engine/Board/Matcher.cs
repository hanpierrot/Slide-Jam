using System.Collections.Generic;

namespace SlideJam.Engine
{
    public static class Matcher
    {
        public const int MinRun = 3;
        
        public static List<Cell> Find(Block[,] grid)
        {
            int rows = grid.GetLength(0);
            int cols = grid.GetLength(1);
            var marked = new bool[rows, cols];
            MarkRuns(grid, marked, Axis.Row);
            MarkRuns(grid, marked, Axis.Col);

            var cells = new List<Cell>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (marked[r, c]) cells.Add(new Cell(r, c));
                }
            }

            return cells;
        }

        public static bool HasMatch(Block[,] grid)
        {
            return Find(grid).Count > 0;
        }

        private static void MarkRuns(Block[,] grid, bool[,] marked, Axis axis)
        {
            int lines = axis == Axis.Row ? grid.GetLength(0) : grid.GetLength(1);
            int length = axis == Axis.Row ? grid.GetLength(1) : grid.GetLength(0);
            for (int line = 0; line < lines; line++)
            {
                int start = 0;
                while (start < length)
                {
                    Block first = At(grid, axis, line, start);
                    int end = start + 1;
                    while (end < length && SameColour(first, At(grid, axis, line, end))) end++;

                    if (!first.IsEmpty && end - start >= MinRun)
                    {
                        for (int i = start; i < end; i++)
                        {
                            if (axis == Axis.Row) marked[line, i] = true;
                            else marked[i, line] = true;
                        }
                    }

                    start = end;
                }
            }
        }

        private static Block At(Block[,] grid, Axis axis, int line, int pos)
        {
            return axis == Axis.Row ? grid[line, pos] : grid[pos, line];
        }

        private static bool SameColour(Block a, Block b)
        {
            return !a.IsEmpty && !b.IsEmpty && a.Color == b.Color;
        }
    }
}
