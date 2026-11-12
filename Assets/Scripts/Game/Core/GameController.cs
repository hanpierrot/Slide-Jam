using System.Collections;
using System.Collections.Generic;
using SlideJam.Engine;
using UnityEngine;

namespace SlideJam.Game
{
    public class GameController : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private BoardView _boardView;
        [SerializeField] private CameraFit _cameraFit;
        [SerializeField] private PhasePlayer _phasePlayer;
        [SerializeField] private DragInput _dragInput;
        [SerializeField] private HudView _hudView;
        [SerializeField] private StageLayout _stageLayout;
        [SerializeField] private SlotsView _slotsView;
        [SerializeField] private QueueView _queueView;
        [SerializeField] private StripView _stripView;
        [SerializeField] private EndOverlayView _endOverlay;
        [SerializeField] private PreviewView _previewView;
        [SerializeField] private NextSpawnView _nextSpawnView;
        
        [Header("Configs")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private LevelConfig _levelConfig;
        [SerializeField] private bool _logMoves = true;
        
        private MovePreview _preview;
        private bool _hasPreview;
        private Axis _previewAxis;
        private int _previewIndex;
        private int _previewNotches;
        private bool _lockedFlashed;
        
        private GameConfig Config => _gameManager != null ? _gameManager.Config : null;
        private StageLayoutData Layout => _stageLayout != null ? _stageLayout.Data : null;

        private Coroutine _run;
        
        public BoardState State { get; private set; }
        public bool IsBusy { get; private set; }

        private void Awake()
        {
            if (Config == null)
            {
                Debug.LogError("GameController needs a GameManager that holds a GameConfig.", this);
                return;
            }
            
            _phasePlayer.Init(_boardView, _slotsView, _queueView, _stripView, _nextSpawnView, Config, ShowProgress);
            if (_previewView != null) _previewView.Init(_boardView, _queueView, Config);
            _dragInput.Init(_boardView, _cameraFit.Cam, Config);
            _slotsView.Init(_boardView.ColorOf);
            _queueView.Init(_boardView.ColorOf);
            _stripView.Init(_boardView.ColorOf);
            _dragInput.Dragging += OnDragging;
            _dragInput.Released += OnReleased;
            _endOverlay.ReviveClicked += Revive;
            _endOverlay.RestartClicked += Restart;
        }

        private void Start()
        {
            Restart();
        }

        private void OnDestroy()
        {
            if (_endOverlay != null)
            {
                _endOverlay.ReviveClicked -= Revive;
                _endOverlay.RestartClicked -= Restart;
            }
            
            if (_dragInput == null) return;

            _dragInput.Dragging -= OnDragging;
            _dragInput.Released -= OnReleased;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_levelConfig == null || Application.isPlaying) return;
            if (_boardView != null) _boardView.SetPreviewSize(_levelConfig.Rows, _levelConfig.Cols);
            if (_stageLayout != null && _cameraFit != null)
            {
                Rect bounds = _stageLayout.Measure(_levelConfig.Rows, _levelConfig.Cols);
                _cameraFit.SetPreviewBounds(bounds);
            }
        }
        
        public void PreviewLayout()
        {
            if (Application.isPlaying) return;

            if (Config == null || _levelConfig == null || _stageLayout == null || _cameraFit == null
                || _boardView == null || _slotsView == null || _queueView == null || _stripView == null || Layout == null)
            {
                Debug.LogWarning("Preview Layout needs every reference on GameController to be assigned, and a StageLayoutData on the StageLayout.");
                return;
            }

            LevelData level = _levelConfig.ToLevelData(Config);
            BoardState state = BoardState.New(level);

            _boardView.SetPreviewSize(level.Rows, level.Cols);
            Rect bounds = _stageLayout.Apply(level.Rows, level.Cols);
            _cameraFit.SetPreviewBounds(bounds);

            _slotsView.Init(_boardView.ColorOf);
            _queueView.Init(_boardView.ColorOf);
            _stripView.Init(_boardView.ColorOf);
            BuildViews(level, Layout);
            _slotsView.Show(state);
            _queueView.Show(state);
            _stripView.Show(state);
            if (_nextSpawnView != null) _nextSpawnView.Show(state);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            UnityEditor.SceneView.RepaintAll();
        }
#endif

        public void Restart()
        {
            if (_run != null) StopCoroutine(_run);
            _run = null;
            ClearPreview();
            _lockedFlashed = false;
            _phasePlayer.CancelFlights();
            _endOverlay.Hide();

            if (Config == null || _levelConfig == null)
            {
                Debug.LogError("GameController needs a GameManager with a GameConfig, and a LevelConfig.");
                return;
            }

            if (Layout == null)
            {
                Debug.LogError("The StageLayout has no StageLayoutData. Run Slide Jam > Setup Scene.");
                return;
            }

            LevelData level = _levelConfig.ToLevelData(Config);
            State = BoardState.New(level);
            SetBusy(false);
            Rect bounds = _stageLayout.Measure(level.Rows, level.Cols);
            _boardView.Build(level);
            BuildViews(level, Layout);
            _cameraFit.Fit(bounds);
            _hudView.ShowLevel(_levelConfig.Number);
            ShowAll();
        }
        
        public void Revive()
        {
            if (IsBusy || State == null || State.Ended != EndReason.QueueOverflow) return;

            BoardState before = State;
            MoveResult result = MoveResolver.Revive(before);
            if (!result.Ok) return;

            State = result.State;
            _endOverlay.Hide();
            _run = StartCoroutine(Run(_phasePlayer.PlayRevive(before, result)));
        }
        
        public bool TryMove(Axis axis, int index, int notches, float startOffset, bool previewKnown = false, MovePreview expected = null)
        {
            if (IsBusy) return false;

            BoardState before = State;
            int step = Board.StepOf(before, axis, index);
            float startCells = startOffset * step;
            MoveResult result = MoveResolver.Move(before, axis, index, notches);
            if (_logMoves)
            {
                Debug.Log(result.Ok
                    ? $"{axis} {index} x{notches}: moves={result.State.Moves}, phases={result.Phases.Count}"
                    : $"{axis} {index} x{notches}: refused");
            }

            if (!result.Ok)
            {
                _run = StartCoroutine(Run(_phasePlayer.SnapBack(before, axis, index, startCells)));
                return false;
            }
            
            if (previewKnown && !PreviewMatches(expected, before, result)) Debug.LogError("The drag preview differs from the move that was played (I5).");

            State = result.State;
            _run = StartCoroutine(Run(_phasePlayer.PlayMove(before, result, axis, index, startCells, notches * step)));
            return true;
        }
        
        private void OnDragging(LineDrag drag)
        {
            if (Board.LineLocked(State, drag.Axis, drag.Index))
            {
                if (!_lockedFlashed && _previewView != null) _previewView.FlashLine(drag.Axis, drag.Index, State.Level.Rows, State.Level.Cols);
                _lockedFlashed = true;
                return;
            }
            
            int step = Board.StepOf(State, drag.Axis, drag.Index);
            _boardView.ShowLineOffset(State, drag.Axis, drag.Index, drag.Offset * step);
            UpdatePreview(drag);
        }

        private void OnReleased(LineDrag drag)
        {
            _lockedFlashed = false;
            if (Board.LineLocked(State, drag.Axis, drag.Index))
            {
                ClearPreview();
                return;
            }
            
            bool known = IsPreviewFor(drag);
            MovePreview expected = known ? _preview : null;
            ClearPreview();
            TryMove(drag.Axis, drag.Index, drag.Notches, drag.Offset, known, expected);
        }

        private void SetBusy(bool busy) 
        {
            if (busy) ClearPreview();
            IsBusy = busy;
            _dragInput.enabled = !busy && (State == null || !State.Ended.HasValue);
        }

        private void ShowAll()
        {
            _boardView.ShowState(State);
            _slotsView.Show(State);
            _queueView.Show(State);
            _stripView.Show(State);
            if (_nextSpawnView != null) _nextSpawnView.Show(State);
            ShowProgress(State);
        }
        
        private void BuildViews(LevelData level, StageLayoutData layout)
        {
            _slotsView.Build(level.ActiveSlots, Config.CapMax, new Vector2(level.Cols + layout.SlotsExtraWidth, layout.SlotsHeight), layout);
            _queueView.Build(level.QueueMax, new Vector2(level.Cols, layout.QueueHeight), Config, layout);
            _stripView.Build(new Vector2(level.Cols, layout.StripHeight), Config.StripVisible);
            if (_nextSpawnView != null) _nextSpawnView.Build(level.Cols, layout);
        }

        private void ShowProgress(BoardState state) 
        {
            _hudView.ShowTargetsLeft(state.Seq.Length - state.Done);
        }
        
        private void UpdatePreview(LineDrag drag)
        {
            if (_previewView == null || IsBusy || State == null) return;
            if (IsPreviewFor(drag)) return;

            _hasPreview = true;
            _previewAxis = drag.Axis;
            _previewIndex = drag.Index;
            _previewNotches = drag.Notches;
            _preview = drag.Notches == 0 ? null : MoveResolver.Preview(State, drag.Axis, drag.Index, drag.Notches);
            _previewView.Show(_preview, State);
        }

        private bool IsPreviewFor(LineDrag drag)
        {
            return _hasPreview && _previewAxis == drag.Axis && _previewIndex == drag.Index && _previewNotches == drag.Notches;
        }

        private void ClearPreview()
        {
            _hasPreview = false;
            _preview = null;
            if (_previewView != null) _previewView.Clear();
        }

        private static bool PreviewMatches(MovePreview expected, BoardState before, MoveResult result)
        {
            Phase first = null;
            foreach (Phase phase in result.Phases)
            {
                if (phase.Kind != PhaseKind.Clear) continue;

                first = phase;
                break;
            }

            if (first == null) return expected == null;
            if (expected == null || expected.Cells.Count != first.MatchedCells.Count) return false;

            int cols = before.Level.Cols;
            var cells = new HashSet<int>();
            foreach (Cell cell in expected.Cells) cells.Add(cell.Row * cols + cell.Col);
            foreach (Cell cell in first.MatchedCells)
            {
                if (!cells.Contains(cell.Row * cols + cell.Col)) return false;
            }

            return expected.ToQueue == result.State.Queue.Count - before.Queue.Count
                   && expected.Lose == (result.State.Ended == EndReason.QueueOverflow);
        }
        
        private IEnumerator Run(IEnumerator routine)
        {
            SetBusy(true);
            yield return routine;

            if (!State.Ended.HasValue && !_boardView.Matches(State)) Debug.LogError("The board on screen differs from the state after the move.");
            if (!_slotsView.Matches(State) || !_queueView.Matches(State)) Debug.LogError("The slots or the queue on screen differ from the state after the move.");
            if (_nextSpawnView != null && !_nextSpawnView.Matches(State)) Debug.LogError("The next-spawn dots on screen differ from the state after the move.");

            ShowAll();
            SetBusy(false);
            _run = null;
            if (State.Ended.HasValue) _endOverlay.Show(State.Ended.Value, State.Moves, CanRevive());
        }

        private bool CanRevive()
        {
            return true;
        }
    }
}
