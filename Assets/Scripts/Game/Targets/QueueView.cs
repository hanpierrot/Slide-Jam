using System;
using System.Collections;
using System.Collections.Generic;
using SlideJam.Engine;
using UnityEngine;

namespace SlideJam.Game
{
    public class QueueView : MonoBehaviour
    {
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _openColor = new Color(0.32f, 0.32f, 0.32f);
        [SerializeField] private Color _lockedColor = new Color(0.14f, 0.14f, 0.14f);
        [SerializeField] private Sprite _openSprite;
        [SerializeField] private Sprite _lockedSprite;
        [SerializeField] private ThemeSkin _skin;

        private SquarePool _chairs;
        private SquarePool _units;
        private Func<int, Color> _colorOf;
        private int _chairCount;
        private float _spacing;
        private float _chairSize;
        
        public void Init(Func<int, Color> colorOf)
        {
            _colorOf = colorOf;
        }

        public void Build(int chairCount, Vector2 size, int unitBuffer) // changed: the buffer comes from GameConfig
        {
            _chairCount = chairCount;
            _spacing = Mathf.Min(1f, size.x / chairCount);
            _chairSize = Mathf.Min(_spacing * 0.85f, size.y);

            _chairs = SquarePool.Obtain(_chairs, transform, "Chairs", 0);
            _units = SquarePool.Obtain(_units, transform, "Units", 2);
            _chairs.Warm(chairCount);
            _units.Warm(chairCount + unitBuffer); // changed
            _chairs.HideFrom(0);
            _units.HideFrom(0);
        }

        public void Show(BoardState state)
        {
            for (int i = 0; i < _chairCount; i++)
            {
                bool open = i < state.QOpen;
                Sprite art = open ? _openSprite : _lockedSprite;
                if (art != null) _chairs.Fit(i, art, ChairLocal(i), _chairSize, Color.white);
                else _chairs.Stretch(i, _sprite, ChairLocal(i), _chairSize, _chairSize, open ? _openColor : _lockedColor);
            }

            float unit = _chairSize * 0.7f;
            int count = state.Queue.Count;
            for (int i = 0; i < count; i++)
            {
                Sprite icon = _skin != null ? _skin.Fruit(state.Queue[i]).Icon : null;
                if (icon != null) _units.Fit(i, icon, UnitLocal(i), unit, Color.white);
                else _units.Stretch(i, _sprite, UnitLocal(i), unit, unit, _colorOf(state.Queue[i]));
            }

            _units.HideFrom(count);
        }
        
        public Vector3 ChairLocal(int index)
        {
            return new Vector3(X(index), 0f, 0f);
        }

        public Vector3 UnitLocal(int index)
        {
            int row = index / _chairCount;
            int column = index % _chairCount;
            return new Vector3(X(column), row * _spacing, 0f);
        }

        public Vector3 ChairWorld(int index)
        {
            return transform.TransformPoint(ChairLocal(index));
        }

        public Vector3 UnitWorld(int index)
        {
            return transform.TransformPoint(UnitLocal(index));
        }
        
        private float X(int index)
        {
            return (index - (_chairCount - 1) / 2f) * _spacing;
        }
    }
}
