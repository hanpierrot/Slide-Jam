using System;
using System.Collections;
using System.Collections.Generic;
using SlideJam.Engine;
using UnityEngine;

namespace SlideJam.Game
{
    public static class LevelConverter
    {
        public static LevelData Convert(string levelName, int rows, int cols, int colors, int seed, int activeSlots,
            int queueMax, int qOpenStart, bool qGrow, IReadOnlyList<TargetEntry> targets, int capMin, int capMax)
        {
            return Convert(levelName, rows, cols, colors, seed, activeSlots, queueMax, qOpenStart, qGrow, targets,
                new List<SpecialEntry>(), -1, capMin, capMax);
        }
        
        public static LevelData Convert(string levelName, int rows, int cols, int colors, int seed, int activeSlots,
            int queueMax, int qOpenStart, bool qGrow, IReadOnlyList<TargetEntry> targets,
            IReadOnlyList<SpecialEntry> specials, int lockedSlotIndex, int capMin, int capMax)
        {
            if (capMin > capMax)
            {
                throw new ArgumentException($"GameConfig: capMin ({capMin}) is above capMax ({capMax}).");
            }

            var seq = new Target[targets.Count];
            for (int i = 0; i < seq.Length; i++)
            {
                TargetEntry entry = targets[i];
                if (entry.Cap < capMin || entry.Cap > capMax)
                {
                    throw new ArgumentOutOfRangeException(nameof(targets), entry.Cap,
                        $"Level '{levelName}': target {i} has cap {entry.Cap}; the allowed range is {capMin} to {capMax}.");
                }

                seq[i] = new Target(entry.Color, entry.Cap);
            }

            var blocks = new SpecialBlock[specials.Count];
            for (int i = 0; i < blocks.Length; i++)
            {
                SpecialEntry e = specials[i];
                blocks[i] = new SpecialBlock(e.Row, e.Col, e.UseColor ? e.Color : -1, e.X2, e.Dual ? e.DualColor : -1,
                    e.Seal, e.Spring, e.ChainLayers, e.ChainLock); // changed: the switches turn the colours on
            }

            return new LevelData(rows, cols, colors, unchecked((uint)seed), activeSlots, queueMax, qOpenStart, qGrow, seq,
                lockedSlotIndex, blocks);
        }
    }
}
