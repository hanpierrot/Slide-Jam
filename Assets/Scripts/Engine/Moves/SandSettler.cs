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
                if (!moved && !spawnedAny) return Collect(state.Grid, starts);
            }

            throw new InvalidOperationException("Settle did not finish.");
        }

        private static bool MovePass(Block[,] grid, Dictionary<int, Cell> starts)
        {
            bool moved = false;
            for (int r = grid.GetLength(0) - 2; r >= 0; r--)
            {
                for (int c = 0; c < grid.GetLength(1); c++)
                {
                    Block block = grid[r, c];
                    if (block.IsEmpty || !grid[r + 1, c].IsEmpty) continue;

                    if (!starts.ContainsKey(block.Id)) starts[block.Id] = new Cell(r, c);
                    grid[r + 1, c] = block;
                    grid[r, c] = Block.Empty;
                    moved = true;
                }
            }

            return moved;
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