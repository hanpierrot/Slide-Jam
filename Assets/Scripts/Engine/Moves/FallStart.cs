namespace SlideJam.Engine
{
    public readonly struct FallStart
    {
        public readonly int Id;
        public readonly Cell From;

        public FallStart(int id, Cell from)
        {
            Id = id;
            From = from;
        }
    }
}