using System.Collections.Generic;

namespace SlideJam.Engine
{
    public static class Resolution
    {
        private static readonly int[] NeighbourRow = { -1, 1, 0, 0 };
        private static readonly int[] NeighbourCol = { 0, 0, -1, 1 };
        
        public static List<Flight> RouteUnits(BoardState state, IReadOnlyList<Cell> cells, int round, out int vanished)
        {
            var flights = new List<Flight>(cells.Count);
            vanished = 0;

            foreach (Cell cell in cells)
            {
                Block block = state.Grid[cell.Row, cell.Col];
                int color = block.Color;
                state.Grid[cell.Row, cell.Col] = Block.Empty;

                int units = block.X2 ? 2 : 1;
                for (int unit = 0; unit < units; unit++)
                {
                    int unitColor = color;
                    int slot = FindSlot(state, unitColor);
                    if (slot < 0 && block.HasDual)
                    {
                        slot = FindSlot(state, block.DualColor);
                        if (slot >= 0) unitColor = block.DualColor;
                    }
                    
                    Place to;
                    if (slot >= 0)
                    {
                        to = Fill(state, slot);
                    }
                    else if (round == 1)
                    {
                        state.Queue.Add(unitColor);
                        to = Place.InQueue(state.Queue.Count - 1);
                    }
                    else
                    {
                        vanished++;
                        to = Place.ToVanish;
                    }
                    
                    flights.Add(new Flight(unitColor, Place.InGrid(cell), to));
                }
            }

            return flights;
        }

        public static List<int> OpenSeals(BoardState state, IReadOnlyList<Cell> consumed)
        {
            var opened = new List<int>();
            int rows = state.Level.Rows;
            int cols = state.Level.Cols;
            foreach (Cell cell in consumed)
            {
                for (int d = 0; d < 4; d++)
                {
                    int row = cell.Row + NeighbourRow[d];
                    int col = cell.Col + NeighbourCol[d];
                    if (row < 0 || row >= rows || col < 0 || col >= cols) continue;

                    Block block = state.Grid[row, col];
                    if (!block.IsSealed) continue;

                    Block lowered = block.WithSeal(block.Seal - 1);
                    state.Grid[row, col] = lowered;
                    if (!lowered.IsSealed) opened.Add(block.Id);
                }
            }

            return opened;
        }
        
        public static List<Cell> Consumable(BoardState state, IReadOnlyList<Cell> matched)
        {
            var consumed = new List<Cell>(matched.Count);
            foreach (Cell cell in matched)
            {
                if (!state.Grid[cell.Row, cell.Col].IsChained) consumed.Add(cell);
            }

            return consumed;
        }
        
        public static List<int> BreakChains(BoardState state, IReadOnlyList<Cell> matched)
        {
            int rows = state.Level.Rows;
            int cols = state.Level.Cols;
            var touched = new bool[rows, cols];
            foreach (Cell cell in matched)
            {
                touched[cell.Row, cell.Col] = true;
                for (int d = 0; d < 4; d++)
                {
                    int row = cell.Row + NeighbourRow[d];
                    int col = cell.Col + NeighbourCol[d];
                    if (row >= 0 && row < rows && col >= 0 && col < cols) touched[row, col] = true;
                }
            }

            var freed = new List<int>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Block block = state.Grid[r, c];
                    if (!touched[r, c] || !block.IsChained) continue;

                    Block lowered = block.WithChain(block.ChainLayers - 1);
                    state.Grid[r, c] = lowered;
                    if (!lowered.IsChained) freed.Add(block.Id);
                }
            }

            return freed;
        }
        
        public static void CompleteAndPull(BoardState state, List<Phase> phases)
        {
            while (true)
            {
                int slot = FirstFull(state);
                if (slot < 0) return;

                List<int> unlocked = Complete(state, slot);
                phases.Add(Phase.Complete(state.Clone(), slot, unlocked));

                List<Flight> pulled = Pull(state);
                if (pulled.Count > 0) phases.Add(Phase.Pull(state.Clone(), pulled));
            }
        }
        
        public static void CheckEnd(BoardState state)
        {
            if (state.Ended.HasValue) return;
            
            if (state.Queue.Count > state.QOpen) state.Ended = EndReason.QueueOverflow;
            else if (AllSlotsEmpty(state)) state.Ended = EndReason.Win;
        }

        private static bool AllSlotsEmpty(BoardState state)
        {
            foreach (Slot slot in state.Slots)
            {
                if (!slot.IsEmpty) return false;
            }

            return true;
        }
        
        private static int FindSlot(BoardState state, int color)
        {
            for (int i = 0; i < state.Slots.Length; i++)
            {
                Slot slot = state.Slots[i];
                if (!slot.IsEmpty && !slot.Locked && slot.Color == color && slot.Count < slot.Cap) return i;
            }

            return -1;
        }
        
        private static Place Fill(BoardState state, int slot)
        {
            int pip = state.Slots[slot].Count;
            state.Slots[slot] = state.Slots[slot].WithCount(pip + 1);
            return Place.InSlot(slot, pip);
        }

        private static int FirstFull(BoardState state)
        {
            for (int i = 0; i < state.Slots.Length; i++)
            {
                if (state.Slots[i].IsFull) return i;
            }

            return -1;
        }
        
        private static List<int> Complete(BoardState state, int slot)
        {
            state.Done++;
            if (state.SeqI < state.Seq.Length)
            {
                Target target = state.Seq[state.SeqI];
                state.Slots[slot] = new Slot(target.Color, target.Cap, state.Pre[state.SeqI], false);
                state.SeqI++;
            }
            else
            {
                state.Slots[slot] = Slot.Empty;
            }

            var unlocked = new List<int>();
            Unlock(state, slot - 1, unlocked);
            Unlock(state, slot + 1, unlocked);

            if (state.Level.QGrow && state.QOpen < state.Level.QueueMax) state.QOpen++;
            return unlocked;
        }

        private static void Unlock(BoardState state, int index, List<int> unlocked)
        {
            if (index < 0 || index >= state.Slots.Length || !state.Slots[index].Locked) return;

            state.Slots[index] = state.Slots[index].WithLocked(false);
            unlocked.Add(index);
        }

        private static List<Flight> Pull(BoardState state)
        {
            var flights = new List<Flight>();
            PullFrom(state, state.Vault, PlaceKind.Vault, flights);
            PullFrom(state, state.Queue, PlaceKind.Queue, flights);
            return flights;
        }

        private static void PullFrom(BoardState state, List<int> units, PlaceKind kind, List<Flight> flights)
        {
            var kept = new List<int>();
            for (int i = 0; i < units.Count; i++)
            {
                int color = units[i];
                int slot = FindSlot(state, color);
                if (slot < 0)
                {
                    kept.Add(color);
                    continue;
                }

                Place from = kind == PlaceKind.Vault ? Place.InVault(i) : Place.InQueue(i);
                flights.Add(new Flight(color, from, Fill(state, slot)));
            }

            units.Clear();
            units.AddRange(kept);
        }
    }
}
