using System.Collections.Generic;

namespace SlideJam.Engine
{
    public static class Board
    {
        public static int LineLength(LevelData level, Axis axis)
        {
            return axis == Axis.Row ? level.Cols : level.Rows;
        }

        public static int FloorMod(int value, int modulus)
        {
            int r = value % modulus;
            return r < 0 ? r + modulus : r;
        }
        
        public static int ShiftAmount(int notches, int step, int length)
        {
            return FloorMod(notches * step, length);
        }
        
        public static int StepOf(BoardState state, Axis axis, int index)
        {
            int length = LineLength(state.Level, axis);
            for (int i = 0; i < length; i++)
            {
                if (Get(state.Grid, axis, index, i).Spring) return 2;
            }

            return 1;
        }

        public static List<int> FreePositions(BoardState state, Axis axis, int index)
        {
            int length = LineLength(state.Level, axis);
            var free = new List<int>(length);
            for (int i = 0; i < length; i++)
            {
                if (!Get(state.Grid, axis, index, i).IsChained) free.Add(i);
            }

            return free;
        }
        
        public static bool LineLocked(BoardState state, Axis axis, int index)
        {
            int length = LineLength(state.Level, axis);
            for (int i = 0; i < length; i++)
            {
                Block block = Get(state.Grid, axis, index, i);
                if (!block.IsChained) continue;
                if (block.ChainLock == ChainLock.Both) return true;
                if (axis == Axis.Row ? block.ChainLock == ChainLock.Row : block.ChainLock == ChainLock.Col) return true;
            }

            return false;
        }

        public static void Shift(Block[,] grid, Axis axis, int index, int k)
        {
            int length = axis == Axis.Row ? grid.GetLength(1) : grid.GetLength(0);
            var free = new List<int>(length);
            for (int i = 0; i < length; i++)
            {
                if (!Get(grid, axis, index, i).IsChained) free.Add(i);
            }

            int count = free.Count;
            if (count <= 1) return;

            k = FloorMod(k, count);
            if (k == 0) return;

            var blocks = new Block[count];
            for (int j = 0; j < count; j++) blocks[j] = Get(grid, axis, index, free[j]);
            for (int j = 0; j < count; j++) Set(grid, axis, index, free[(j + k) % count], blocks[j]);
        }

        private static Block Get(Block[,] grid, Axis axis, int index, int pos)
        {
            return axis == Axis.Row ? grid[index, pos] : grid[pos, index];
        }

        private static void Set(Block[,] grid, Axis axis, int index, int pos, Block block)
        {
            if (axis == Axis.Row) grid[index, pos] = block;
            else grid[pos, index] = block;
        }
    }
}
