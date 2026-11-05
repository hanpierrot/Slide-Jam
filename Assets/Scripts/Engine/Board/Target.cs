namespace SlideJam.Engine
{
    public readonly struct Target
    {
        public readonly int Color;
        public readonly int Cap;

        public Target(int color, int cap)
        {
            Color = color;
            Cap = cap;
        }
    }
}