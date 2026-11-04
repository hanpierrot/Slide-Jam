using System;
using System.Collections;
using System.Collections.Generic;
using SlideJam.Engine;
using UnityEngine;

namespace SlideJam.Game
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private Sprite _blockSprite;
        [SerializeField] private float _blockScale = 0.92f;
        [SerializeField] private Color _boardColor = new Color(0.16f, 0.20f, 0.18f);

        [SerializeField] private Color[] _palette =
        {
            new Color(0.90f, 0.25f, 0.30f),
            new Color(0.25f, 0.45f, 0.90f),
            new Color(0.45f, 0.75f, 0.30f),
            new Color(0.98f, 0.60f, 0.15f),
            new Color(0.60f, 0.35f, 0.75f),
        };

        private readonly Dictionary<int, BlockView> _views = new Dictionary<int, BlockView>();
        private readonly Stack<BlockView> _pool = new Stack<BlockView>();

        private Transform _blocksRoot;
        private Transform _background;
        private Transform _mask;
        private int _rows;
        private int _cols;
        
        public float BlockScale => _blockScale;

        public void Build(LevelData level)
        {
            if (_palette.Length < level.Colors)
            {
                throw new InvalidOperationException("The palette has fewer colours than the level.");
            }

            _rows = level.Rows;
            _cols = level.Cols;
            if (_blocksRoot == null) CreateRoots();

            var size = new Vector3(_cols, _rows, 1f);
            _background.localScale = size;
            _mask.localScale = size;
            ReleaseAll();
        }
        
        public Vector3 CellToLocal(int row, int col)
        {
            return new Vector3(col - (_cols - 1) / 2f, (_rows - 1) / 2f - row, 0f);
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
                    view.Bind(block.Id, _blockSprite, _palette[block.Color]);
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
            view.Bind(id, _blockSprite, _palette[color]);
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
        
        private void CreateRoots()
        {
            _background = CreateChild("Background", transform).transform;
            SpriteRenderer back = _background.gameObject.AddComponent<SpriteRenderer>();
            back.sprite = _blockSprite;
            back.color = _boardColor;
            back.sortingOrder = -1;

            _mask = CreateChild("Mask", transform).transform;
            _mask.gameObject.AddComponent<SpriteMask>().sprite = _blockSprite;

            _blocksRoot = CreateChild("Blocks", transform).transform;
        }

        private static GameObject CreateChild(string childName, Transform parent)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(parent, false);
            return child;
        }

        private BlockView GetOrCreate(int id)
        {
            if (_views.TryGetValue(id, out BlockView existing)) return existing;

            BlockView view;
            if (_pool.Count > 0)
            {
                view = _pool.Pop();
                view.gameObject.SetActive(true);
            }
            else
            {
                view = CreateChild("Block", _blocksRoot).AddComponent<BlockView>();
            }

            view.transform.localScale = Vector3.one * _blockScale;
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
            BlockView view = _views[id];
            view.gameObject.SetActive(false);
            _pool.Push(view);
            _views.Remove(id);
        }
    }
}
