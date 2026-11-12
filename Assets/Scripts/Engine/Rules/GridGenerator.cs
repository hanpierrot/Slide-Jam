using System;
using System.Collections.Generic;

namespace SlideJam.Engine
{
    internal static class GridGenerator
    {
        private const int GridStreamColumn = -1;
        private const int MaxDrawsPerCell = 10000;
        
        public static Block[,] Generate(LevelData level, out int nextId)
        {
            var grid = new Block[level.Rows, level.Cols];
            int draw = 0;
            int id = 1;
            for (int r = 0; r < level.Rows; r++)
            {
                for (int c = 0; c < level.Cols; c++)
                {
                    int color;
                    int tries = 0;
                    do
                    {
                        if (++tries > MaxDrawsPerCell) throw new InvalidOperationException("Grid generation did not settle.");
                        color = ColumnStream.At(level.Seed, GridStreamColumn, draw++, level.Colors);
                    } while (WouldMatch(grid, r, c, color));

                    grid[r, c] = new Block(id++, color);
                }
            }

            foreach (SpecialBlock special in level.Specials)
            {
                Block rolled = grid[special.Row, special.Col];
                int color = special.Color >= 0 ? special.Color : rolled.Color;
                grid[special.Row, special.Col] = special.ToBlock(rolled.Id, color);
            }
            
            if (level.Specials.Count > 0)
            {
                List<Cell> matched = Matcher.Find(grid); // changed: the message says where
                if (matched.Count > 0)
                {
                    Cell first = matched[0];
                    throw new InvalidOperationException("Level data: the special blocks leave a match in the initial grid (first cell: row "
                                                        + first.Row + ", column " + first.Col
                                                        + "). A dual block counts as both of its colours: move it, or change its second colour.");
                }
            }

            nextId = id;
            return grid;
        }

        private static bool WouldMatch(Block[,] grid, int r, int c, int color)
        {
            bool horizontal = c >= 2 && grid[r, c - 1].Color == color && grid[r, c - 2].Color == color;
            bool vertical = r >= 2 && grid[r - 1, c].Color == color && grid[r - 2, c].Color == color;
            return horizontal || vertical;
        }
    }
}
