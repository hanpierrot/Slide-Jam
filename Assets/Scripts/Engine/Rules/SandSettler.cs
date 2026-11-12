using System;
using System.Collections.Generic;

namespace SlideJam.Engine
{
    internal static class SandSettler
    {
        private const int MaxPasses = 500;

        public static List<FallStart> Settle(BoardState state)
        {
            var starts = new Dictionary<int, Cell>();
            var spawned = new int[state.Level.Cols];

            for (int pass = 0; pass < MaxPasses; pass++)
            {
                bool moved = MovePass(state.Grid, starts);
                bool spawnedAny = SpawnPass(state, starts, spawned);
                if (!moved && !spawnedAny)
                {
                    EnsureFull(state.Grid);
                    return Collect(state.Grid, starts);
                }
            }

            throw new InvalidOperationException("Settle did not finish.");
        }

        private static bool MovePass(Block[,] grid, Dictionary<int, Cell> starts)
        {
            int rows = grid.GetLength(0);
            int cols = grid.GetLength(1);
            bool moved = false;
            for (int r = rows - 2; r >= 0; r--)
            {
                bool leftToRight = r % 2 == 0;
                for (int i = 0; i < cols; i++)
                {
                    int c = leftToRight ? i : cols - 1 - i;
                    Block block = grid[r, c];
                    if (block.IsEmpty || block.IsChained) continue;

                    int target = FallTarget(grid, r, c);
                    if (target < 0) continue;

                    if (!starts.ContainsKey(block.Id)) starts[block.Id] = new Cell(r, c);
                    grid[r + 1, target] = block;
                    grid[r, c] = Block.Empty;
                    moved = true;
                }
            }

            return moved;
        }
        
        private static int FallTarget(Block[,] grid, int r, int c)
        {
            if (grid[r + 1, c].IsEmpty) return c;

            int first = (r + c) % 2 != 0 ? -1 : 1;
            if (CanFlow(grid, r, c, first)) return c + first;
            if (CanFlow(grid, r, c, -first)) return c - first;
            return -1;
        }
        
        private static bool CanFlow(Block[,] grid, int r, int c, int side)
        {
            int col = c + side;
            if (col < 0 || col >= grid.GetLength(1)) return false;
            if (!grid[r + 1, col].IsEmpty) return false;

            return grid[r + 1, c].IsChained || grid[r, col].IsChained;
        }

        private static void EnsureFull(Block[,] grid)
        {
            for (int r = 0; r < grid.GetLength(0); r++)
            {
                for (int c = 0; c < grid.GetLength(1); c++)
                {
                    if (!grid[r, c].IsEmpty) continue;

                    throw new InvalidOperationException("The settle left an empty cell at row " + r + ", column " + c
                                                        + ": no block can reach it. Check the chain layout of the level (DESIGN 5.5, I9).");
                }
            }
        }

        private static bool SpawnPass(BoardState state, Dictionary<int, Cell> starts, int[] spawned)
        {
            bool any = false;
            LevelData level = state.Level;
            for (int c = 0; c < level.Cols; c++)
            {
                if (!state.Grid[0, c].IsEmpty) continue;

                int color = ColumnStream.At(level.Seed, c, state.Ptr[c], level.Colors);
                state.Ptr[c]++;
                spawned[c]++;

                int id = state.NextId++;
                state.Grid[0, c] = new Block(id, color);
                starts[id] = new Cell(-spawned[c], c);
                any = true;
            }

            return any;
        }

        private static List<FallStart> Collect(Block[,] grid, Dictionary<int, Cell> starts)
        {
            var result = new List<FallStart>();
            for (int r = 0; r < grid.GetLength(0); r++)
            {
                for (int c = 0; c < grid.GetLength(1); c++)
                {
                    int id = grid[r, c].Id;
                    if (starts.TryGetValue(id, out Cell from) && (from.Row != r || from.Col != c))
                    {
                        result.Add(new FallStart(id, from));
                    }
                }
            }

            return result;
        }
    }
}