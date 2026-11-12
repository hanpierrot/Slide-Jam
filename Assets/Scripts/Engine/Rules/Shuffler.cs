using System.Collections.Generic;

namespace SlideJam.Engine
{
    internal static class Shuffler
    {
        private const uint MovesStep = 104729u;
        private const int MaxAttempts = 100;

        public static List<Flight> Shuffle(BoardState state)
        {
            int rows = state.Level.Rows;
            int cols = state.Level.Cols;
            int count = rows * cols;

            var original = new Block[count];
            for (int i = 0; i < count; i++) original[i] = state.Grid[i / cols, i % cols];

            uint seed = unchecked(state.Level.Seed + (uint)state.Moves * MovesStep + 1u);
            int draw = 0;
            var order = new int[count];
            var candidate = new Block[rows, cols];
            var movable = new List<int>();
            
            for (int i = 0; i < count; i++)
            {
                if (!original[i].IsSpecial) movable.Add(i);
            }
            
            int moves = movable.Count;
            var permutation = new int[moves];

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                for (int i = 0; i < count; i++) order[i] = i;
                for (int k = 0; k < moves; k++) permutation[k] = k;
                
                for (int k = moves - 1; k > 0; k--)
                {
                    uint r = Mulberry32.At(seed, draw++);
                    int j = (int)(((ulong)r * (uint)(k + 1)) >> 32);
                    int swap = permutation[k];
                    permutation[k] = permutation[j];
                    permutation[j] = swap;
                }

                for (int k = 0; k < moves; k++) order[movable[k]] = movable[permutation[k]];
                for (int i = 0; i < count; i++) candidate[i / cols, i % cols] = original[order[i]];
                if (!Matcher.HasMatch(candidate)) break;
            }

            var flights = new List<Flight>();
            for (int i = 0; i < count; i++)
            {
                state.Grid[i / cols, i % cols] = candidate[i / cols, i % cols];
                if (order[i] == i) continue;

                var from = new Cell(order[i] / cols, order[i] % cols);
                var to = new Cell(i / cols, i % cols);
                flights.Add(new Flight(original[order[i]].Color, Place.InGrid(from), Place.InGrid(to)));
            }

            return flights;
        }
    }
}