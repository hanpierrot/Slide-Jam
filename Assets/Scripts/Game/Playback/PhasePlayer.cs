using System;
using System.Collections;
using System.Collections.Generic;
using SlideJam.Engine;
using UnityEngine;

namespace SlideJam.Game
{
    public class PhasePlayer : MonoBehaviour
    {
        private const int FlightSortingOrder = 30; 

        private BoardView _boardView;
        private SlotsView _slotsView;
        private QueueView _queueView;
        private StripView _stripView;
        private NextSpawnView _nextSpawnView;
        private GameConfig _config;
        private Action<BoardState> _showProgress;
        private SquarePool _flights;

        public void Init(BoardView boardView, SlotsView slotsView, QueueView queueView, StripView stripView,
            NextSpawnView nextSpawnView, GameConfig config, Action<BoardState> showProgress)
        {
            _boardView = boardView;
            _slotsView = slotsView;
            _queueView = queueView;
            _stripView = stripView;
            _nextSpawnView = nextSpawnView;
            _config = config;
            _showProgress = showProgress;

            _flights = SquarePool.Obtain(_flights, transform, "Flights", FlightSortingOrder);
            _flights.Warm(config.FlightPoolSize);
            _flights.HideFrom(0);
        }

        public void CancelFlights()
        {
            _flights?.HideFrom(0);
        }

        public IEnumerator PlayMove(BoardState before, MoveResult result, Axis axis, int index,
            float startOffset, int notches)
        {
            yield return SlideLine(before, axis, index, startOffset, notches, _config.SnapSeconds);

            BoardState previous = before;
            foreach (Phase phase in result.Phases)
            {
                switch (phase.Kind)
                {
                    case PhaseKind.Shift:
                        _boardView.ShowState(phase.Snap);
                        break;
                    case PhaseKind.Clear:
                        yield return PlayClear(phase, previous);
                        break;
                    case PhaseKind.Complete:
                        yield return PlayComplete(phase);
                        break;
                    case PhaseKind.Pull:
                        yield return PlayPull(phase, previous);
                        break;
                    case PhaseKind.Fall:
                        yield return PlayFall(phase);
                        break;
                }

                previous = phase.Snap;
            }
        }
        
        public IEnumerator SnapBack(BoardState state, Axis axis, int index, float startOffset)
        {
            return SlideLine(state, axis, index, startOffset, 0f, _config.SnapBackSeconds);
        }

        private IEnumerator SlideLine(BoardState state, Axis axis, int index, float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                _boardView.ShowLineOffset(state, axis, index, Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }

            _boardView.ShowLineOffset(state, axis, index, to);
        }

        private IEnumerator PlayClear(Phase clear, BoardState previous)
        {
            var matched = new List<BlockView>();
            foreach (Cell cell in clear.MatchedCells)
            {
                if (_boardView.TryGetView(previous.Grid[cell.Row, cell.Col].Id, out BlockView matchedView))
                {
                    matched.Add(matchedView);
                }
            }

            for (float t = 0f; t < _config.ClearSeconds; t += Time.deltaTime)
            {
                float k = t / _config.ClearSeconds;
                float factor = k < 0.5f
                    ? Mathf.Lerp(1f, _config.PulseScale, k * 2f)
                    : Mathf.Lerp(_config.PulseScale, 0f, (k - 0.5f) * 2f);
                foreach (BlockView view in matched) view.SetScaleFactor(factor);
                yield return null;
            }

            _boardView.ShowState(clear.Snap);
            yield return PlayFlights(clear.Flights, previous, clear.Snap);
            _slotsView.Show(clear.Snap);
            _queueView.Show(clear.Snap);
        }
        
        private IEnumerator PlayComplete(Phase complete)
        {
            int slot = complete.Slot;
            _slotsView.BeginPop(slot);
            yield return Animate(_config.SlotPopSeconds,
                k => _slotsView.SetPop(1f + (_config.SlotPopScale - 1f) * Mathf.Sin(k * Mathf.PI)));
            _slotsView.EndPop();

            _slotsView.Show(complete.Snap);
            _queueView.Show(complete.Snap);
            _stripView.Show(complete.Snap);
            _showProgress?.Invoke(complete.Snap);

            _slotsView.BeginPop(slot);
            yield return Animate(_config.SlotEnterSeconds, k => _slotsView.SetPop(Mathf.SmoothStep(0f, 1f, k)));
            _slotsView.EndPop();
        }
        
        private IEnumerator PlayPull(Phase pull, BoardState previous)
        {
            yield return PlayFlights(pull.Flights, previous, pull.Snap);
            _slotsView.Show(pull.Snap);
            _queueView.Show(pull.Snap);
        }
        
        private IEnumerator PlayFlights(IReadOnlyList<Flight> flights, BoardState before, BoardState after)
        {
            int count = flights.Count;
            if (count == 0) yield break;

            BoardState display = before.Clone();
            var starts = new Vector3[count];
            var ends = new Vector3[count];
            var icons = new SpriteRenderer[count];
            var launched = new bool[count];
            var landed = new bool[count];

            for (int i = 0; i < count; i++)
            {
                Flight flight = flights[i];
                starts[i] = WorldOf(flight.From, before);
                ends[i] = flight.To.Kind == PlaceKind.Vanish ? starts[i] : WorldOf(flight.To, after);

                Sprite icon = _boardView.Skin != null ? _boardView.Skin.Fruit(flight.Color).Icon : null;
                if (icon != null) icons[i] = _flights.Fit(i, icon, transform.InverseTransformPoint(starts[i]), icon.bounds.size.x, Color.white);
            }

            float total = (count - 1) * _config.FlightStaggerSeconds + _config.FlightSeconds;
            for (float t = 0f; t < total; t += Time.deltaTime)
            {
                for (int i = 0; i < count; i++)
                {
                    float local = t - i * _config.FlightStaggerSeconds;
                    if (local < 0f || landed[i]) continue;

                    if (!launched[i])
                    {
                        launched[i] = true;
                        if (flights[i].From.Kind == PlaceKind.Queue) _queueView.HideUnit(flights[i].From.A);
                    }

                    float k = _config.FlightSeconds > 0f ? Mathf.Clamp01(local / _config.FlightSeconds) : 1f;
                    if (icons[i] != null)
                    {
                        float endScale = flights[i].To.Kind == PlaceKind.Vanish ? 0f : _config.FlightEndScale;
                        icons[i].transform.localPosition = transform.InverseTransformPoint(Vector3.Lerp(starts[i], ends[i], Mathf.SmoothStep(0f, 1f, k)));
                        icons[i].transform.localScale = Vector3.one * Mathf.Lerp(1f, endScale, k);
                    }

                    if (k < 1f) continue;

                    landed[i] = true;
                    Land(display, flights[i], icons[i]);
                }

                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                if (!landed[i]) Land(display, flights[i], icons[i]);
            }

            _flights.HideFrom(0);
        }
        
        private void Land(BoardState display, Flight flight, SpriteRenderer icon)
        {
            if (icon != null) icon.gameObject.SetActive(false);

            switch (flight.To.Kind)
            {
                case PlaceKind.Slot:
                    display.Slots[flight.To.A] = display.Slots[flight.To.A].WithCount(flight.To.B + 1);
                    _slotsView.Show(display);
                    break;
                case PlaceKind.Queue:
                    display.Queue.Add(flight.Color);
                    _queueView.Show(display);
                    break;
            }
        }
        
        private Vector3 WorldOf(Place place, BoardState state) 
        {
            switch (place.Kind)
            {
                case PlaceKind.Grid: return _boardView.CellToWorld(place.A, place.B);
                case PlaceKind.Slot: return _slotsView.PipWorld(place.A, place.B, state.Slots[place.A].Cap);
                case PlaceKind.Queue: return _queueView.UnitWorld(place.A);
                default: return _queueView.ChairWorld(0);
            }
        }

        private static IEnumerator Animate(float seconds, Action<float> step)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                step(t / seconds);
                yield return null;
            }

            step(1f);
        }
        
        public IEnumerator PlayRevive(BoardState before, MoveResult result)
        {
            Phase revive = result.Phases[0];
            var movers = new List<BlockView>();
            var fromPos = new List<Vector3>();
            var toPos = new List<Vector3>();
            foreach (Flight flight in revive.Flights)
            {
                Block block = before.Grid[flight.From.A, flight.From.B];
                if (!_boardView.TryGetView(block.Id, out BlockView view)) continue;

                movers.Add(view);
                fromPos.Add(_boardView.CellToLocal(flight.From.A, flight.From.B));
                toPos.Add(_boardView.CellToLocal(flight.To.A, flight.To.B));
            }

            for (float t = 0f; t < _config.ReviveSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / _config.ReviveSeconds);
                for (int i = 0; i < movers.Count; i++) movers[i].transform.localPosition = Vector3.Lerp(fromPos[i], toPos[i], k);
                yield return null;
            }

            _boardView.ShowState(revive.Snap);
            _queueView.Show(revive.Snap);
        }
        
        private IEnumerator PlayFall(Phase fall)
        {
            if (_nextSpawnView != null) _nextSpawnView.Show(fall.Snap);
            var originOf = new Dictionary<int, Cell>();
            foreach (FallStart start in fall.Starts) originOf[start.Id] = start.From;

            Block[,] grid = fall.Snap.Grid;
            var movers = new List<BlockView>();
            var fromPos = new List<Vector3>();
            var toPos = new List<Vector3>();
            int longest = 0;
            for (int r = 0; r < grid.GetLength(0); r++)
            {
                for (int c = 0; c < grid.GetLength(1); c++)
                {
                    Block block = grid[r, c];
                    if (block.IsEmpty || !originOf.TryGetValue(block.Id, out Cell origin)) continue;

                    Vector3 from = _boardView.CellToLocal(origin.Row, origin.Col);
                    movers.Add(_boardView.ShowBlock(block.Id, block.Color, from));
                    fromPos.Add(from);
                    toPos.Add(_boardView.CellToLocal(r, c));
                    longest = Mathf.Max(longest, r - origin.Row);
                }
            }

            float seconds = _config.FallBaseSeconds + _config.FallPerCellSeconds * longest;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                for (int i = 0; i < movers.Count; i++)
                {
                    movers[i].transform.localPosition = Vector3.Lerp(fromPos[i], toPos[i], k);
                }

                yield return null;
            }

            _boardView.ShowState(fall.Snap);
        }
    }
}
