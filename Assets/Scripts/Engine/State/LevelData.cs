using System;
using System.Collections.Generic;

namespace SlideJam.Engine
{
    public readonly struct LevelData
    {
        public readonly int Rows;
        public readonly int Cols;
        public readonly int Colors;
        public readonly uint Seed;
        public readonly int ActiveSlots;
        public readonly int QueueMax; 
        public readonly int QOpenStart;
        public readonly bool QGrow;
        public readonly int LockedSlotIndex;
        
        private readonly Target[] _seq;
        private readonly SpecialBlock[] _specials; 
        
        public IReadOnlyList<Target> Seq => _seq;
        public IReadOnlyList<SpecialBlock> Specials => _specials ?? Array.Empty<SpecialBlock>();

        public LevelData(int rows, int cols, int colors, uint seed, int activeSlots, int queueMax,
            int qOpenStart, bool qGrow, Target[] seq)
            : this(rows, cols, colors, seed, activeSlots, queueMax, qOpenStart, qGrow, seq, -1, Array.Empty<SpecialBlock>())
        {
        }
        
        public LevelData(int rows, int cols, int colors, uint seed, int activeSlots, int queueMax,
            int qOpenStart, bool qGrow, Target[] seq, int lockedSlotIndex, SpecialBlock[] specials)
        {
            if (rows < 3) throw new ArgumentOutOfRangeException(nameof(rows));
            if (cols < 3) throw new ArgumentOutOfRangeException(nameof(cols));
            if (colors < 3) throw new ArgumentOutOfRangeException(nameof(colors));
            if (activeSlots < 1) throw new ArgumentOutOfRangeException(nameof(activeSlots));
            if (queueMax < 1) throw new ArgumentOutOfRangeException(nameof(queueMax));
            if (qOpenStart < 1 || qOpenStart > queueMax) throw new ArgumentOutOfRangeException(nameof(qOpenStart));
            if (seq == null) throw new ArgumentNullException(nameof(seq));
            if (seq.Length == 0) throw new ArgumentException("A level needs at least one target.", nameof(seq));
            if (lockedSlotIndex < -1 || lockedSlotIndex >= activeSlots) throw new ArgumentOutOfRangeException(nameof(lockedSlotIndex));
            if (specials == null) throw new ArgumentNullException(nameof(specials));

            foreach (Target target in seq)
            {
                if (target.Color < 0 || target.Color >= colors) throw new ArgumentOutOfRangeException(nameof(seq), "Target colour out of range.");
                if (target.Cap < 1) throw new ArgumentOutOfRangeException(nameof(seq), "Target cap must be at least 1.");
            }

            var taken = new bool[rows, cols];
            foreach (SpecialBlock special in specials)
            {
                if (special.Row >= rows || special.Col >= cols) throw new ArgumentOutOfRangeException(nameof(specials), "A special block is outside the grid.");
                if (taken[special.Row, special.Col]) throw new ArgumentException("Two special blocks stand on the same cell.", nameof(specials));
                if (special.Color >= colors || special.DualColor >= colors) throw new ArgumentOutOfRangeException(nameof(specials), "A special block colour is out of range.");

                taken[special.Row, special.Col] = true;
            }

            Rows = rows;
            Cols = cols;
            Colors = colors;
            Seed = seed;
            ActiveSlots = activeSlots;
            QueueMax = queueMax;
            QOpenStart = qOpenStart;
            QGrow = qGrow;
            LockedSlotIndex = lockedSlotIndex;
            _specials = (SpecialBlock[])specials.Clone();
            _seq = (Target[])seq.Clone();
        }
        
        public static LevelData Sandbox(int rows, int cols, int colors, uint seed)
        {
            const int targets = 500;
            const int cap = 3;
            const int queue = 1000;

            var seq = new Target[targets];
            for (int i = 0; i < targets; i++) seq[i] = new Target(colors > 0 ? i % colors : 0, cap);

            return new LevelData(rows, cols, colors, seed, 3, queue, queue, false, seq);
        }
    }
}
