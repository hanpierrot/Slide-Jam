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

        public static void Shift(Block[,] grid, Axis axis, int index, int k)
        {
            int length = axis == Axis.Row ? grid.GetLength(1) : grid.GetLength(0);
            k = FloorMod(k, length);
            if (k == 0) return;

            var line = new Block[length];
            for (int i = 0; i < length; i++) line[i] = Get(grid, axis, index, i);
            for (int i = 0; i < length; i++) Set(grid, axis, index, (i + k) % length, line[i]);
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
