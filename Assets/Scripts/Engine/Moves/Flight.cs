namespace SlideJam.Engine
{
    public readonly struct Flight
    {
        public readonly int Color;
        public readonly Place From;
        public readonly Place To;

        public Flight(int color, Place from, Place to)
        {
            Color = color;
            From = from;
            To = to;
        }
    }
}