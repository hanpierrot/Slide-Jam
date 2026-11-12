using System.Collections.Generic;

namespace SlideJam.Engine
{
    public sealed class BoardState
    {
        public LevelData Level { get; }
        public Block[,] Grid { get; }
        public int[] Ptr { get; }
        public Slot[] Slots { get; }
        public List<int> Queue { get; }
        public List<int> Vault { get; } 
        public Target[] Seq { get; }
        public int[] Pre { get; }
        public int NextId { get; internal set; }
        public int Moves { get; internal set; }
        public int QOpen { get; internal set; }
        public int SeqI { get; internal set; }
        public int Done { get; internal set; }
        public EndReason? Ended { get; internal set; }
        
        private BoardState(LevelData level, Block[,] grid, int nextId)
        {
            Level = level;
            Grid = grid;
            NextId = nextId;
            Ptr = new int[level.Cols];
            Slots = new Slot[level.ActiveSlots];
            Queue = new List<int>();
            Vault = new List<int>();
            Seq = new Target[level.Seq.Count];
            for (int i = 0; i < Seq.Length; i++) Seq[i] = level.Seq[i];

            Pre = new int[Seq.Length];
            QOpen = level.QOpenStart;
            for (int i = 0; i < Slots.Length && SeqI < Seq.Length; i++)
            {
                Target target = Seq[SeqI];
                Slots[i] = new Slot(target.Color, target.Cap, Pre[SeqI], i == level.LockedSlotIndex);
                SeqI++;
            }
        }
        
        private BoardState(BoardState source)
        {
            Level = source.Level;
            Grid = (Block[,])source.Grid.Clone();
            Ptr = (int[])source.Ptr.Clone();
            Slots = (Slot[])source.Slots.Clone();
            Queue = new List<int>(source.Queue);
            Vault = new List<int>(source.Vault);
            Seq = (Target[])source.Seq.Clone();
            Pre = (int[])source.Pre.Clone();
            NextId = source.NextId;
            Moves = source.Moves;
            QOpen = source.QOpen;
            SeqI = source.SeqI;
            Done = source.Done;
            Ended = source.Ended;
        }

        public static BoardState New(LevelData level)
        {
            Block[,] grid = GridGenerator.Generate(level, out int nextId);
            return new BoardState(level, grid, nextId);
        }
        
        public BoardState Clone()
        {
            return new BoardState(this);
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
                        if (Grid[r, c].IsSpecial) h = Mix(h, Grid[r, c].Signature);
                    }
                }

                foreach (int p in Ptr) h = Mix(h, (uint)p);
                h = Mix(h, (uint)NextId);
                h = Mix(h, (uint)Moves);

                foreach (Slot slot in Slots)
                {
                    h = Mix(h, (uint)slot.Color);
                    h = Mix(h, (uint)slot.Cap);
                    h = Mix(h, (uint)slot.Count);
                    h = Mix(h, slot.Locked ? 1u : 0u);
                }

                h = HashList(h, Queue);
                h = HashList(h, Vault);

                foreach (Target target in Seq)
                {
                    h = Mix(h, (uint)target.Color);
                    h = Mix(h, (uint)target.Cap);
                }

                foreach (int pre in Pre) h = Mix(h, (uint)pre);

                h = Mix(h, (uint)QOpen);
                h = Mix(h, (uint)SeqI);
                h = Mix(h, (uint)Done);
                h = Mix(h, Ended.HasValue ? (uint)Ended.Value + 1u : 0u);
                return h;
            }
        }
        
        private static uint HashList(uint h, List<int> list)
        {
            h = Mix(h, (uint)list.Count);
            foreach (int value in list) h = Mix(h, (uint)value);
            return h;
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
