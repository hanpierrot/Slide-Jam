namespace SlideJam.Engine
{
    public readonly struct Slot
    {
        public readonly int Color;
        public readonly int Cap;
        public readonly int Count;
        public readonly bool Locked;

        public Slot(int color, int cap, int count, bool locked)
        {
            Color = color;
            Cap = cap;
            Count = count;
            Locked = locked;
        }

        public bool IsEmpty => Cap == 0;
        public bool IsFull => Cap > 0 && Count >= Cap;
        public static Slot Empty => default;

        public Slot WithCount(int count)
        {
            return new Slot(Color, Cap, count, Locked);
        }

        public Slot WithLocked(bool locked) // added: a locked slot opens when its neighbour completes (DESIGN 5.4)
        {
            return new Slot(Color, Cap, Count, locked);
        }
    }
}