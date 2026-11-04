namespace SlideJam.Engine
{
    public sealed class BoardState
    {
        public LevelData Level { get; }
        public Block[,] Grid { get; }
        public int[] Ptr { get; }
        public int NextId { get; internal set; } 
        public int Moves { get; internal set; } 
        
        
        private BoardState(LevelData level, Block[,] grid, int[] ptr, int nextId, int moves)
        {
            Level = level;
            Grid = grid;
            Ptr = ptr;
            NextId = nextId;
            Moves = moves;
        }

        public static BoardState New(LevelData level)
        {
            Block[,] grid = GridGenerator.Generate(level, out int nextId);
            return new BoardState(level, grid, new int[level.Cols], nextId, 0);
        }
        
        public BoardState Clone()
        {
            return new BoardState(Level, (Block[,])Grid.Clone(), (int[])Ptr.Clone(), NextId, Moves);
        }

        public uint Hash()
        {
            unchecked
            {
                uint h = 2166136261u;
                for (int r = 0; r < Level.Rows; r++)
                {
                    for (int c = 0; c < Level.Cols; c++)
                    {
                        h = Mix(h, (uint)Grid[r, c].Id);
                        h = Mix(h, (uint)Grid[r, c].Color);
                    }
                }

                foreach (int p in Ptr) h = Mix(h, (uint)p);
                h = Mix(h, (uint)NextId);
                h = Mix(h, (uint)Moves);
                return h;
            }
        }

        private static uint Mix(uint h, uint value)
        {
            unchecked
            {
                return (h ^ value) * 16777619u;
            }
        }
    }
}
