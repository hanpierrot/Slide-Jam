using System;
namespace SlideJam.Engine
{
    public static class ColumnStream
    {
        private const uint ColumnStep = 7919u;

        public static int At(uint seed, int column, int index, int colors)
        {
            if (colors <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(colors));
            }
            
            uint columnSeed = unchecked(seed + (uint)(column + 1) * ColumnStep);
            uint r = Mulberry32.At(columnSeed, index);
            return (int)(((ulong)r * (uint)colors) >> 32);
        }
    }
}
