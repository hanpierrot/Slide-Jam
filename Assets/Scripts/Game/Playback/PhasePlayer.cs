using System.Collections;
using System.Collections.Generic;
using SlideJam.Engine;
using UnityEngine;

namespace SlideJam.Game
{
    public class PhasePlayer : MonoBehaviour
    {
        private BoardView _boardView;
        
        private GameConfig _config; // changed: the timings live in GameConfig (I10)

        public void Init(BoardView boardView, GameConfig config) // changed: takes the config
        {
            _boardView = boardView;
            _config = config;
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
        }
        
        private IEnumerator PlayFall(Phase fall)
        {
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
