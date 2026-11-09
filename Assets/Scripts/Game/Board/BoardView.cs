using System;
using System.Collections;
using System.Collections.Generic;
using SlideJam.Engine;
using UnityEngine;
using UnityEngine.Pool;

namespace SlideJam.Game
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private BlockView _blockPrefab;
        [SerializeField] private Transform _blocksRoot;
        [SerializeField] private Transform _background;
        [SerializeField] private Transform _mask;
        [SerializeField] private int _preMadeBlocks = 49;
        [SerializeField, HideInInspector] private int _previewRows;
        [SerializeField, HideInInspector] private int _previewCols;
        
        [SerializeField] private ThemeSkin _skin;

        private readonly Dictionary<int, BlockView> _views = new Dictionary<int, BlockView>();

        private BlockPool _pool;
        private int _rows;
        private int _cols;
        
        public ThemeSkin Skin => _skin;
        
        private void Awake()
        {
            _pool = new BlockPool(_blockPrefab, _blocksRoot);
            foreach (Transform child in _blocksRoot)
            {
                if (child.TryGetComponent(out BlockView view)) _pool.Adopt(view);
            }
        }

        public void Build(LevelData level)
        {
            if (_skin == null) throw new InvalidOperationException("BoardView needs a ThemeSkin (it holds the colour palette)."); // changed
            if (_skin.PaletteLength < level.Colors)
            {
                throw new InvalidOperationException("The palette has fewer colours than the level.");
            }

            _rows = level.Rows;
            _cols = level.Cols;

            var size = new Vector3(_cols, _rows, 1f);
            _background.localScale = size;
            _mask.localScale = size;
            ReleaseAll();
            
            if (_pool.Total < _rows * _cols)
            {
                Debug.LogWarning($"The block pool has {_pool.Total} blocks but the level needs {_rows * _cols}; " +
                                 "the rest are instantiated at run time. Raise Pre Made Blocks and run Pre-create Blocks.");
            }
        }
        
        public Vector3 CellToLocal(int row, int col)
        {
            return new Vector3(col - (_cols - 1) / 2f, (_rows - 1) / 2f - row, 0f);
        }

        public Color ColorOf(int color)
        {
            return _skin.ColorOf(color); // changed
        }

        public void ShowState(BoardState state)
        {
            var present = new HashSet<int>();
            foreach (Block block in state.Grid)
            {
                if (!block.IsEmpty) present.Add(block.Id);
            }

            ReleaseMissing(present);

            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    Block block = state.Grid[r, c];
                    if (block.IsEmpty) continue;

                    BlockView view = GetOrCreate(block.Id);
                    view.Bind(block.Id, ColorOf(block.Color), IconOf(block.Color)); // changed
                    view.transform.localPosition = CellToLocal(r, c);
                }
            }
        }
        
        public Vector3 ToLocal(Vector3 world)
        {
            return transform.InverseTransformPoint(world);
        }

        public bool TryGetCell(Vector3 world, out int row, out int col)
        {
            Vector3 local = ToLocal(world);
            col = Mathf.FloorToInt(local.x + _cols / 2f);
            row = Mathf.FloorToInt(_rows / 2f - local.y);
            return row >= 0 && row < _rows && col >= 0 && col < _cols;
        }
        
        public bool TryGetView(int id, out BlockView view)
        {
            return _views.TryGetValue(id, out view);
        }

        public BlockView ShowBlock(int id, int color, Vector3 localPosition)
        {
            BlockView view = GetOrCreate(id);
            view.Bind(id, ColorOf(color), IconOf(color)); // changed
            view.transform.localPosition = localPosition;
            return view;
        }
        
        public bool Matches(BoardState state)
        {
            int expected = 0;
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    Block block = state.Grid[r, c];
                    if (block.IsEmpty) continue;

                    expected++;
                    if (!_views.TryGetValue(block.Id, out BlockView view)) return false;
                    if ((view.transform.localPosition - CellToLocal(r, c)).sqrMagnitude > 0.0001f) return false;
                }
            }

            return expected == _views.Count;
        }
        
        public void ShowLineOffset(BoardState state, Axis axis, int index, float offset)
        {
            int length = axis == Axis.Row ? _cols : _rows;
            for (int i = 0; i < length; i++)
            {
                Block block = axis == Axis.Row ? state.Grid[index, i] : state.Grid[i, index];
                if (block.IsEmpty || !_views.TryGetValue(block.Id, out BlockView view)) continue;

                float along = Wrap(i + offset, length);
                view.transform.localPosition = axis == Axis.Row
                    ? new Vector3(along - (_cols - 1) / 2f, (_rows - 1) / 2f - index, 0f)
                    : new Vector3(index - (_cols - 1) / 2f, (_rows - 1) / 2f - along, 0f);
            }
        }

        private static float Wrap(float value, int length)
        {
            float shifted = value + 0.5f;
            return shifted - length * Mathf.Floor(shifted / length) - 0.5f;
        }

        private BlockView GetOrCreate(int id)
        {
            if (_views.TryGetValue(id, out BlockView existing)) return existing;

            BlockView view = _pool.Get();
            _views[id] = view;
            return view;
        }
        
        private void ReleaseMissing(HashSet<int> present)
        {
            var gone = new List<int>();
            foreach (KeyValuePair<int, BlockView> pair in _views)
            {
                if (!present.Contains(pair.Key)) gone.Add(pair.Key);
            }

            foreach (int id in gone) Release(id);
        }

        private void ReleaseAll()
        {
            ReleaseMissing(new HashSet<int>());
        }

        private void Release(int id)
        {
            _pool.Release(_views[id]);
            _views.Remove(id);
        }
        
        private Sprite IconOf(int color)
        {
            return _skin != null ? _skin.Fruit(color).Icon : null;
        }
        
#if UNITY_EDITOR
        private bool _layoutQueued;
        
        [ContextMenu("Pre-create Blocks")]
        private void PreCreateBlocks()
        {
            QueueEditorLayout();
        }
        
        public void SetPreviewSize(int rows, int cols)
        {
            if (_previewRows == rows && _previewCols == cols) return;

            _previewRows = rows;
            _previewCols = cols;
            UnityEditor.EditorUtility.SetDirty(this);
            QueueEditorLayout();
        }
        
        private void OnValidate()
        {
            _preMadeBlocks = Mathf.Max(0, _preMadeBlocks);
            QueueEditorLayout();
        }

        private void QueueEditorLayout()
        {
            if (_layoutQueued) return;

            _layoutQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                _layoutQueued = false;
                if (this != null) ApplyEditorLayout();
            };
        }

        private void ApplyEditorLayout()
        {
            if (Application.isPlaying || _blockPrefab == null || _blocksRoot == null) return;
            if (_previewRows <= 0 || _previewCols <= 0) return;

            _rows = _previewRows;
            _cols = _previewCols;
            var size = new Vector3(_cols, _rows, 1f);
            bool changed = SetScale(_background, size);
            changed |= SetScale(_mask, size);

            int missing = _preMadeBlocks - _blocksRoot.childCount;
            for (int i = 0; i < missing; i++)
            {
                var created = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(_blockPrefab.gameObject, _blocksRoot);
                created.SetActive(false);
                UnityEditor.Undo.RegisterCreatedObjectUndo(created, "Pre-create Blocks");
                changed = true;
            }

            int cells = _rows * _cols;
            for (int i = 0; i < _blocksRoot.childCount; i++)
            {
                Transform block = _blocksRoot.GetChild(i);
                bool shown = i < cells;
                if (block.gameObject.activeSelf != shown)
                {
                    block.gameObject.SetActive(shown);
                    changed = true;
                }

                if (!shown) continue;

                Vector3 position = CellToLocal(i / _cols, i % _cols);
                if (block.localPosition != position)
                {
                    block.localPosition = position;
                    changed = true;
                }
            }

            if (cells > _blocksRoot.childCount)
            {
                Debug.LogWarning($"Pre Made Blocks gives {_blocksRoot.childCount} blocks but the grid needs {cells}; raise Pre Made Blocks.");
            }

            if (changed) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        private static bool SetScale(Transform target, Vector3 scale)
        {
            if (target == null || target.localScale == scale) return false;

            target.localScale = scale;
            return true;
        }
#endif
    }
}
