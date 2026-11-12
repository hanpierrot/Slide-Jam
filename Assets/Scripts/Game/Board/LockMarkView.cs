using System.Collections;
using System.Collections.Generic;
using SlideJam.Engine;
using UnityEngine;

namespace SlideJam.Game
{
    // The red bars at the edge of the grid that mark the rows and the columns that cannot be slid (DESIGN 5.6):
    // a locked row has an upright bar at each end, a locked column has a flat bar at each end
    public class LockMarkView : MonoBehaviour
    {
        private const int SortingOrder = 20;

        [SerializeField] private ThemeSkin _skin;
        [SerializeField] private float _barLength = 0.7f; // the long side of a bar, in cells
        [SerializeField] private float _edgeOffset = 0.3f; // how far outside the grid the centre of a bar stands

        private SquarePool _marks;

        public void Show(BoardState state)
        {
            if (_skin == null) return;

            _marks = SquarePool.Obtain(_marks, transform, "Marks", SortingOrder);
            int rows = state.Level.Rows;
            int cols = state.Level.Cols;
            _marks.Warm(2 * (rows + cols));

            Sprite upright = _skin.LockMarkVertical;
            Sprite flat = _skin.LockMarkHorizontal;
            float uprightWidth = WidthForLength(upright, _barLength); // the thickness that gives the bar its length
            float left = -cols / 2f - _edgeOffset;
            float right = cols / 2f + _edgeOffset;
            float top = rows / 2f + _edgeOffset;
            float bottom = -rows / 2f - _edgeOffset;

            int shown = 0;
            for (int r = 0; r < rows; r++)
            {
                if (!Board.LineLocked(state, Axis.Row, r)) continue;

                float y = (rows - 1) / 2f - r;
                _marks.Fit(shown++, upright, new Vector3(left, y, 0f), uprightWidth, Color.white);
                _marks.Fit(shown++, upright, new Vector3(right, y, 0f), uprightWidth, Color.white);
            }

            for (int c = 0; c < cols; c++)
            {
                if (!Board.LineLocked(state, Axis.Col, c)) continue;

                float x = c - (cols - 1) / 2f;
                _marks.Fit(shown++, flat, new Vector3(x, top, 0f), _barLength, Color.white);
                _marks.Fit(shown++, flat, new Vector3(x, bottom, 0f), _barLength, Color.white);
            }

            _marks.HideFrom(shown);
        }

        // The width (the short side) of an upright bar whose height is the given length; Fit sizes a sprite by its width
        private static float WidthForLength(Sprite sprite, float length)
        {
            if (sprite == null) return length;

            return length * sprite.bounds.size.x / sprite.bounds.size.y;
        }
    }
}
