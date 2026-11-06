using System.Collections;
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
        
        [Header("Configs")]
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private LevelConfig _levelConfig;
        [SerializeField] private bool _logMoves = true;
        
        private const float SlotsExtraWidth = 0.4f; // added

        private Coroutine _run;
        
        public BoardState State { get; private set; }
        public bool IsBusy { get; private set; }

        private void Awake()
        {
            _phasePlayer.Init(_boardView);
            _dragInput.Init(_boardView, _cameraFit.Cam);
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
            if (_levelConfig == null) return;
            if (_boardView != null) _boardView.SetPreviewSize(_levelConfig.Rows, _levelConfig.Cols);
            if (_stageLayout != null && _cameraFit != null)
            {
                Rect bounds = _stageLayout.Apply(_levelConfig.Rows, _levelConfig.Cols);
                _cameraFit.SetPreviewBounds(bounds);
            }
        }
        
        [ContextMenu("Preview Layout")]
        private void PreviewLayout()
        {
            if (Application.isPlaying) return;

            if (_gameConfig == null || _levelConfig == null || _stageLayout == null || _cameraFit == null
                || _boardView == null || _slotsView == null || _queueView == null || _stripView == null)
            {
                Debug.LogWarning("Preview Layout needs every reference on GameController to be assigned.");
                return;
            }

            LevelData level = _levelConfig.ToLevelData(_gameConfig);
            BoardState state = BoardState.New(level);

            _boardView.SetPreviewSize(level.Rows, level.Cols);
            Rect bounds = _stageLayout.Apply(level.Rows, level.Cols);
            _cameraFit.SetPreviewBounds(bounds);

            _slotsView.Init(_boardView.ColorOf);
            _queueView.Init(_boardView.ColorOf);
            _stripView.Init(_boardView.ColorOf);
            _slotsView.Build(level.ActiveSlots, _gameConfig.CapMax, new Vector2(level.Cols + SlotsExtraWidth, _stageLayout.SlotsHeight)); // changed: baskets may be a little wider than the grid
            _queueView.Build(level.QueueMax, new Vector2(level.Cols, _stageLayout.QueueHeight));
            _stripView.Build(new Vector2(level.Cols, _stageLayout.StripHeight));
            _slotsView.Show(state);
            _queueView.Show(state);
            _stripView.Show(state);

            UnityEditor.SceneView.RepaintAll();
        }
#endif

        public void Restart()
        {
            if (_run != null) StopCoroutine(_run);
            _run = null;
            _endOverlay.Hide();

            if (_gameConfig == null || _levelConfig == null)
            {
                Debug.LogError("GameController needs a GameConfig and a LevelConfig.");
                return;
            }

            LevelData level = _levelConfig.ToLevelData(_gameConfig);
            State = BoardState.New(level);
            SetBusy(false);
            Rect bounds = _stageLayout.Apply(level.Rows, level.Cols);
            _boardView.Build(level);
            _slotsView.Build(level.ActiveSlots, _gameConfig.CapMax, new Vector2(level.Cols + SlotsExtraWidth, _stageLayout.SlotsHeight)); // changed: baskets may be a little wider than the grid
            _queueView.Build(level.QueueMax, new Vector2(level.Cols, _stageLayout.QueueHeight));
            _stripView.Build(new Vector2(level.Cols, _stageLayout.StripHeight));
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
            _dragInput.enabled = !busy && (State == null || !State.Ended.HasValue);
        }

        private void ShowAll()
        {
            _boardView.ShowState(State);
            _slotsView.Show(State);
            _queueView.Show(State);
            _stripView.Show(State);
            _hudView.ShowTargetsLeft(State.Seq.Length - State.Done);
        }
        
        private IEnumerator Run(IEnumerator routine)
        {
            SetBusy(true);
            yield return routine;

            if (!State.Ended.HasValue && !_boardView.Matches(State)) Debug.LogError("The board on screen differs from the state after the move.");

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
