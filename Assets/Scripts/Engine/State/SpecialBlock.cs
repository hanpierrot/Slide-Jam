using System;

namespace SlideJam.Engine
{
    public readonly struct SpecialBlock
    {
        public readonly int Row;
        public readonly int Col;
        public readonly int Color;
        public readonly bool X2;
        public readonly int DualColor;
        public readonly int Seal;
        public readonly bool Spring;
        public readonly int ChainLayers;
        public readonly ChainLock ChainLock;

        public SpecialBlock(int row, int col, int color, bool x2, int dualColor, int seal, bool spring,
            int chainLayers, ChainLock chainLock)
        {
            if (row < 0) throw new ArgumentOutOfRangeException(nameof(row));
            if (col < 0) throw new ArgumentOutOfRangeException(nameof(col));
            if (color < -1) throw new ArgumentOutOfRangeException(nameof(color));
            if (dualColor < -1) throw new ArgumentOutOfRangeException(nameof(dualColor));
            if (seal < 0) throw new ArgumentOutOfRangeException(nameof(seal));
            if (chainLayers < 0) throw new ArgumentOutOfRangeException(nameof(chainLayers));

            Row = row;
            Col = col;
            Color = color;
            X2 = x2;
            DualColor = dualColor;
            Seal = seal;
            Spring = spring;
            ChainLayers = chainLayers;
            ChainLock = chainLock;
        }

        public bool HasDual => DualColor >= 0;

        public Block ToBlock(int id, int color)
        {
            return new Block(id, color, X2, DualColor, Seal, Spring, ChainLayers, ChainLock);
        }
    }
}