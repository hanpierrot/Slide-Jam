using System;

namespace SlideJam.Engine
{
    public readonly struct LevelData
    {
        public readonly int Rows;
        public readonly int Cols;
        public readonly int Colors;
        public readonly uint Seed;

        public LevelData(int rows, int cols, int colors, uint seed)
        {
            if (rows < 3) throw new ArgumentOutOfRangeException(nameof(rows));
            if (cols < 3) throw new ArgumentOutOfRangeException(nameof(cols));
            if (colors < 3) throw new ArgumentOutOfRangeException(nameof(colors));

            Rows = rows;
            Cols = cols;
            Colors = colors;
            Seed = seed;
        }
    }
}
