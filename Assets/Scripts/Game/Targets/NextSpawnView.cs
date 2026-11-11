using SlideJam.Engine;
using UnityEngine;

namespace SlideJam.Game
{
    public class NextSpawnView : MonoBehaviour
    {
        [SerializeField] private Sprite _hanger;
        [SerializeField] private ThemeSkin _skin;

        private float _nearSize; // changed: the sizes come from StageLayoutData in Build
        private float _farSize;
        private float _farAlpha;
        private float _bottomPad;
        private float _dotGap;
        private float _hangerWidth;
        [SerializeField, HideInInspector] private string _builtKey; // added: the layout stamp of the last Edit Mode build (LayoutStamp)
        private SquarePool _hangers;
        private SquarePool _icons;
        private int _cols;
        private int[] _shown;
        private bool _warned; // added

        public void Build(int cols, StageLayoutData layout) // changed: layout added
        {
            _nearSize = layout.NearSize; // added from here
            _farSize = layout.FarSize;
            _farAlpha = layout.FarAlpha;
            _bottomPad = layout.BottomPad;
            _dotGap = layout.DotGap;
            _hangerWidth = layout.HangerWidth;
            _cols = cols;
            _shown = new int[cols * 2];

            _hangers = SquarePool.Obtain(_hangers, transform, "Hangers", 0);
            _icons = SquarePool.Obtain(_icons, transform, "Icons", 2);
            _hangers.Warm(cols);
            _icons.Warm(cols * 2);
            _hangers.HideFrom(0);
            _icons.HideFrom(0);

            string key = LayoutStamp.Of(cols, _nearSize, _farSize, _farAlpha, _bottomPad, _dotGap, _hangerWidth); // added from here
            bool keep = LayoutStamp.CanKeep(this, _builtKey, key);
            LayoutStamp.Record(this, ref _builtKey, key);
            _hangers.KeepGeometry = keep; // the dots and the hangers stand still; only their sprites change
            _icons.KeepGeometry = keep;
        }

        public void Show(BoardState state)
        {
            if (_skin == null)
            {
                if (!_warned) Debug.LogWarning("NextSpawnView has no ThemeSkin, so the next-spawn dots are not drawn. Run Slide Jam > Setup Scene.", this); // changed: it used to return silently
                _warned = true;
                return;
            }

            LevelData level = state.Level;
            float nearY = _bottomPad + _nearSize / 2f;
            float farY = _bottomPad + _nearSize + _dotGap + _farSize / 2f;
            float hangerY = HangerCenterY(farY + _farSize / 2f); // added: the stalk stands on the top of the upper fruit

            for (int c = 0; c < _cols; c++)
            {
                float x = c - (_cols - 1) / 2f;
                _hangers.Fit(c, _hanger, new Vector3(x, hangerY, 0f), _hangerWidth, Color.white); // changed: hangerY is computed
                ShowDot(c, new Vector3(x, nearY, 0f), _nearSize, ColumnStream.At(level.Seed, c, state.Ptr[c], level.Colors), 1f);
                ShowDot(_cols + c, new Vector3(x, farY, 0f), _farSize, ColumnStream.At(level.Seed, c, state.Ptr[c] + 1, level.Colors), _farAlpha); // changed: alpha
            }
        }

        public bool Matches(BoardState state)
        {
            if (_skin == null) return true;

            LevelData level = state.Level;
            if (_shown == null || _shown.Length != level.Cols * 2) return false;

            for (int c = 0; c < level.Cols; c++)
            {
                if (_shown[c] != ColumnStream.At(level.Seed, c, state.Ptr[c], level.Colors)) return false;
                if (_shown[level.Cols + c] != ColumnStream.At(level.Seed, c, state.Ptr[c] + 1, level.Colors)) return false;
            }

            return true;
        }

        private void ShowDot(int index, Vector3 position, float size, int color, float alpha) // changed: alpha added
        {
            _shown[index] = color;
            _icons.Fit(index, _skin.Fruit(color).Icon, position, size, new Color(1f, 1f, 1f, alpha));
        }

        // added: the centre of the stalk so that its bottom rests just inside the top of the upper fruit
        private float HangerCenterY(float fruitTop)
        {
            if (_hanger == null) return fruitTop;

            float height = _hanger.bounds.size.y * (_hangerWidth / _hanger.bounds.size.x);
            return fruitTop - 0.03f + height / 2f;
        }
    }
}