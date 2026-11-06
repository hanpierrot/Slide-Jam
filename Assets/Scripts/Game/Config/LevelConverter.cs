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

            return new LevelData(rows, cols, colors, unchecked((uint)seed), activeSlots, queueMax, qOpenStart, qGrow, seq);
        }
    }
}
