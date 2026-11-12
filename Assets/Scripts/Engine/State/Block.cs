namespace SlideJam.Engine
{
    public readonly struct Block
    {
        public readonly int Id;
        public readonly int Color;
        public readonly bool X2;
        public readonly int Seal;
        public readonly bool Spring;
        public readonly int ChainLayers;
        public readonly ChainLock ChainLock;
        private readonly int _dualPlus1;
        
        public Block(int id, int color)
        {
            Id = id;
            Color = color;
            X2 = false;
            Seal = 0;
            Spring = false;
            ChainLayers = 0;
            ChainLock = ChainLock.Both;
            _dualPlus1 = 0;
        }
        
        public Block(int id, int color, bool x2, int dualColor, int seal, bool spring, int chainLayers, ChainLock chainLock)
        {
            Id = id;
            Color = color;
            X2 = x2;
            Seal = seal;
            Spring = spring;
            ChainLayers = chainLayers;
            ChainLock = chainLock;
            _dualPlus1 = dualColor + 1;
        }

        public bool IsEmpty => Id == 0;
        public static Block Empty => default;
        
        public bool HasDual => _dualPlus1 > 0;
        public int DualColor => _dualPlus1 - 1;
        public bool IsSealed => Seal > 0;
        public bool IsChained => ChainLayers > 0;
        public bool IsSpecial => X2 || HasDual || IsSealed || Spring || IsChained;
        
        public Block WithSeal(int seal)
        {
            return new Block(Id, Color, X2, DualColor, seal, Spring, ChainLayers, ChainLock);
        }

        public Block WithChain(int layers)
        {
            return new Block(Id, Color, X2, DualColor, Seal, Spring, layers, ChainLock);
        }
        
        public uint Signature
        {
            get
            {
                unchecked
                {
                    return (X2 ? 1u : 0u)
                           | (Spring ? 2u : 0u)
                           | ((uint)(_dualPlus1 & 0xFF) << 2)
                           | ((uint)(Seal & 0xFF) << 10)
                           | ((uint)(ChainLayers & 0xFF) << 18)
                           | ((uint)ChainLock << 26);
                }
            }
        }
    }
}
