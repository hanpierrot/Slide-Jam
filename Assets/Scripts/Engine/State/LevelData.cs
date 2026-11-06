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
        
        private readonly Target[] _seq;
        
        public IReadOnlyList<Target> Seq => _seq;

        public LevelData(int rows, int cols, int colors, uint seed, int activeSlots, int queueMax,
            int qOpenStart, bool qGrow, Target[] seq)
        {
            if (rows < 3) throw new ArgumentOutOfRangeException(nameof(rows));
            if (cols < 3) throw new ArgumentOutOfRangeException(nameof(cols));
            if (colors < 3) throw new ArgumentOutOfRangeException(nameof(colors));
            if (activeSlots < 1) throw new ArgumentOutOfRangeException(nameof(activeSlots));
            if (queueMax < 1) throw new ArgumentOutOfRangeException(nameof(queueMax));
            if (qOpenStart < 1 || qOpenStart > queueMax) throw new ArgumentOutOfRangeException(nameof(qOpenStart)); 
            if (seq == null) throw new ArgumentNullException(nameof(seq));
            if (seq.Length == 0) throw new ArgumentException("A level needs at least one target.", nameof(seq));

            foreach (Target target in seq) 
            {
                if (target.Color < 0 || target.Color >= colors) throw new ArgumentOutOfRangeException(nameof(seq), "Target colour out of range.");
                if (target.Cap < 1) throw new ArgumentOutOfRangeException(nameof(seq), "Target cap must be at least 1.");
            }

            Rows = rows;
            Cols = cols;
            Colors = colors;
            Seed = seed;
            ActiveSlots = activeSlots;
            QueueMax = queueMax; 
            QOpenStart = qOpenStart; 
            QGrow = qGrow;
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
