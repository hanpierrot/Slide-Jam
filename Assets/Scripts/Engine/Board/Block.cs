namespace SlideJam.Engine
{
    public readonly struct Block
    {
        public readonly int Id;
        public readonly int Color;
        
        public Block(int id, int color)
        {
            Id = id;
            Color = color;
        }

        public bool IsEmpty => Id == 0;
        public static Block Empty => default;
    }
}
