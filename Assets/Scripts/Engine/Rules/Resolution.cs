using System.Collections.Generic;

namespace SlideJam.Engine
{
    public static class Resolution
    {
        public static List<Flight> RouteUnits(BoardState state, IReadOnlyList<Cell> cells, int round, out int vanished)
        {
            var flights = new List<Flight>(cells.Count);
            vanished = 0;

            foreach (Cell cell in cells)
            {
                int color = state.Grid[cell.Row, cell.Col].Color;
                state.Grid[cell.Row, cell.Col] = Block.Empty;

                Place to;
                int slot = FindSlot(state, color);
                if (slot >= 0)
                {
                    to = Fill(state, slot);
                }
                else if (round == 1)
                {
                    state.Queue.Add(color);
                    to = Place.InQueue(state.Queue.Count - 1);
                }
                else
                {
                    vanished++;
                    to = Place.ToVanish;
                }

                flights.Add(new Flight(color, Place.InGrid(cell), to));
            }

            return flights;
        }
        
        public static void CompleteAndPull(BoardState state, List<Phase> phases)
        {
            while (true)
            {
                int slot = FirstFull(state);
                if (slot < 0) return;

                Complete(state, slot);
                phases.Add(Phase.Complete(state.Clone(), slot));

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
        
        private static void Complete(BoardState state, int slot)
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

            if (state.Level.QGrow && state.QOpen < state.Level.QueueMax) state.QOpen++;
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
