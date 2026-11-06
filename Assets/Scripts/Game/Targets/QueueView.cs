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
        [SerializeField] private ThemeSkin _skin; // added: units show the fruit icon of their colour when the skin has one

        private readonly List<SpriteRenderer> _chairs = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _units = new List<SpriteRenderer>();
        private Func<int, Color> _colorOf;
        private int _chairCount;
        private float _spacing;
        private float _chairSize;

        public void Init(Func<int, Color> colorOf)
        {
            _colorOf = colorOf;
        }

        public void Build(int chairCount, Vector2 size)
        {
            Squares.Prepare(transform, _chairs, _units);
            _chairCount = chairCount;
            _spacing = Mathf.Min(1f, size.x / chairCount);
            _chairSize = Mathf.Min(_spacing * 0.85f, size.y);
            Squares.HideFrom(_chairs, chairCount);
        }

        public void Show(BoardState state)
        {
            for (int i = 0; i < _chairCount; i++)
            {
                bool open = i < state.QOpen;
                SpriteRenderer chair = Squares.At(_chairs, i, transform, _sprite, 0);
                Sprite art = open ? _openSprite : _lockedSprite;
                var position = new Vector3(X(i), 0f, 0f);
                if (art != null)
                {
                    chair.sprite = art;
                    Squares.SetFit(chair, position, _chairSize, Color.white);
                }
                else
                {
                    chair.sprite = _sprite;
                    Squares.Set(chair, position, _chairSize, _chairSize, open ? _openColor : _lockedColor);
                }
            }
            
            float unit = _chairSize * 0.7f;
            int count = state.Queue.Count;
            for (int i = 0; i < count; i++)
            {
                int row = i / _chairCount;
                int column = i % _chairCount;
                var position = new Vector3(X(column), row * _spacing, 0f);
                SpriteRenderer square = Squares.At(_units, i, transform, _sprite, 2);
                Sprite icon = _skin != null ? _skin.Fruit(state.Queue[i]).Icon : null; // changed from here
                if (icon != null)
                {
                    square.sprite = icon;
                    Squares.SetFit(square, position, unit, Color.white);
                }
                else
                {
                    square.sprite = _sprite;
                    Squares.Set(square, position, unit, unit, _colorOf(state.Queue[i]));
                }
            }

            Squares.HideFrom(_units, count);
        }
        
        private float X(int index)
        {
            return (index - (_chairCount - 1) / 2f) * _spacing;
        }
    }
}
