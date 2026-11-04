using System.Text;
using SlideJam.Engine;

namespace SlideJam.Tests.EditMode
{
    internal static class TestGrids
    {
        public static Block[,] FromRows(params string[] rows)
        {
            int cols = rows[0].Length;
            var grid = new Block[rows.Length, cols];
            int id = 1;
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    char ch = rows[r][c];
                    grid[r, c] = ch == '.' ? Block.Empty : new Block(id++, ch - '0');
                }
            }

            return grid;
        }

        public static string[] ToRows(Block[,] grid)
        {
            var rows = new string[grid.GetLength(0)];
            for (int r = 0; r < rows.Length; r++)
            {
                var sb = new StringBuilder();
                for (int c = 0; c < grid.GetLength(1); c++)
                {
                    sb.Append(grid[r, c].IsEmpty ? '.' : (char)('0' + grid[r, c].Color));
                }

                rows[r] = sb.ToString();
            }

            return rows;
        }
    }
}