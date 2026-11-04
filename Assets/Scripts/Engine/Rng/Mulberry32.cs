namespace SlideJam.Engine
{
    public static class Mulberry32
    {
        private const uint Increment = 0x6D2B79F5u;
        
        public static uint At(uint seed, int index)
        {
            unchecked
            {
                uint a = seed + Increment * (uint)(index + 1);
                uint t = (a ^ (a >> 15)) * (1u | a);
                t = (t + (t ^ (t >> 7)) * (61u | t)) ^ t;
                return t ^ (t >> 14);
            }
        }
    }
}
