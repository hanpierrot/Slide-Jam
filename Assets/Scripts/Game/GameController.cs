using System;
using System.Collections;
using System.Collections.Generic;
using SlideJam.Engine;
using UnityEngine;

namespace SlideJam.Game
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private CameraFit _cameraFit;
        [SerializeField] private PhasePlayer _phasePlayer;
        
        // Temporary level data; moves into a LevelConfig asset in Phase 2
        [SerializeField] private int _rows = 6;
        [SerializeField] private int _cols = 6;
        [SerializeField] private int _colors = 5;
        [SerializeField] private int _seed = 20261031;
        [SerializeField] private bool _logMoves = true;
        
        private Coroutine _run;
        
        public BoardState State { get; private set; }
        public bool IsBusy { get; private set; }
        public event Action StateChanged;

        private void Start()
        {
            Restart();
        }
        
        public void Restart()
        {
            if (_run != null) StopCoroutine(_run);
            _run = null;
            IsBusy = false;

            var level = new LevelData(_rows, _cols, _colors, (uint)_seed);
            State = BoardState.New(level);
            _boardView.Build(level);
            _cameraFit.Fit(_cols, _rows);
            _boardView.ShowState(State);
            StateChanged?.Invoke();
        }
        
        public bool TryMove(Axis axis, int index, int notches, float startOffset)
        {
            if (IsBusy) return false;

            BoardState before = State;
            MoveResult result = MoveResolver.Move(before, axis, index, notches);
            if (_logMoves)
            {
                Debug.Log(result.Ok
                    ? $"{axis} {index} x{notches}: moves={result.State.Moves}, phases={result.Phases.Count}"
                    : $"{axis} {index} x{notches}: refused");
            }

            if (!result.Ok)
            {
                _run = StartCoroutine(Run(_phasePlayer.SnapBack(before, axis, index, startOffset)));
                return false;
            }

            State = result.State;
            StateChanged?.Invoke();
            _run = StartCoroutine(Run(_phasePlayer.PlayMove(before, result, axis, index, startOffset, notches)));
            return true;
        }
        
        private IEnumerator Run(IEnumerator routine)
        {
            IsBusy = true;
            yield return routine;

            if (!_boardView.Matches(State)) Debug.LogError("The board on screen differs from the state after the move.");

            _boardView.ShowState(State);
            IsBusy = false;
            _run = null;
        }
    }
}
