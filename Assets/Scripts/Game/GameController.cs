using System.Collections;
using SlideJam.Engine;
using UnityEngine;

namespace SlideJam.Game
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private CameraFit _cameraFit;
        [SerializeField] private PhasePlayer _phasePlayer;
        [SerializeField] private DragInput _dragInput;
        [SerializeField] private HudView _hudView;
        
        // Temporary level data; moves into a LevelConfig asset in Phase 2
        [SerializeField] private int _rows = 6;
        [SerializeField] private int _cols = 6;
        [SerializeField] private int _colors = 5;
        [SerializeField] private int _seed = 20261031;
        [SerializeField] private bool _logMoves = true;
        
        private Coroutine _run;
        
        public BoardState State { get; private set; }
        public bool IsBusy { get; private set; }

        private void Awake()
        {
            _phasePlayer.Init(_boardView);
            _dragInput.Init(_boardView, _cameraFit.Cam);
            _dragInput.Dragging += OnDragging;
            _dragInput.Released += OnReleased;
        }

        private void Start()
        {
            Restart();
        }

        private void OnDestroy()
        {
            if (_dragInput == null) return;

            _dragInput.Dragging -= OnDragging;
            _dragInput.Released -= OnReleased;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_boardView != null) _boardView.SetPreviewSize(_rows, _cols);
            if (_cameraFit != null) _cameraFit.SetPreviewSize(_cols, _rows);
        }
#endif

        public void Restart()
        {
            if (_run != null) StopCoroutine(_run);
            _run = null;
            SetBusy(false);

            var level = new LevelData(_rows, _cols, _colors, (uint)_seed);
            State = BoardState.New(level);
            _boardView.Build(level);
            _cameraFit.Fit(_cols, _rows);
            _boardView.ShowState(State);
            _hudView.ShowMoves(State.Moves);
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
            _hudView.ShowMoves(State.Moves);
            _run = StartCoroutine(Run(_phasePlayer.PlayMove(before, result, axis, index, startOffset, notches)));
            return true;
        }
        
        private void OnDragging(LineDrag drag)
        {
            _boardView.ShowLineOffset(State, drag.Axis, drag.Index, drag.Offset);
        }

        private void OnReleased(LineDrag drag)
        {
            TryMove(drag.Axis, drag.Index, drag.Notches, drag.Offset);
        }

        private void SetBusy(bool busy) 
        {
            IsBusy = busy;
            _dragInput.enabled = !busy;
        }
        
        private IEnumerator Run(IEnumerator routine)
        {
            SetBusy(true);
            yield return routine;

            if (!_boardView.Matches(State)) Debug.LogError("The board on screen differs from the state after the move.");

            _boardView.ShowState(State);
            SetBusy(false);
            _run = null;
        }
    }
}
