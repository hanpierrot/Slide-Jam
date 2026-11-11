using System;
using System.Collections;
using System.Collections.Generic;
using SlideJam.Engine;
using UnityEngine;

namespace SlideJam.Game
{
    public class QueueView : MonoBehaviour
    {
        private const int BoxRows = 2;
        private const int BoxColumns = 5;

        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _openColor = new Color(0.32f, 0.32f, 0.32f);
        [SerializeField] private Color _lockedColor = new Color(0.14f, 0.14f, 0.14f);
        [SerializeField] private Sprite _openSprite;
        [SerializeField] private Sprite _lockedSprite;
        [SerializeField] private ThemeSkin _skin;
        [SerializeField] private SpriteRenderer _overflowBox;

        private SquarePool _chairs;
        private SquarePool _units;
        private Func<int, Color> _colorOf;
        private int _chairCount;
        private float _spacing;
        private float _chairSize;
        private float _left;
        private Vector3 _boxCenter;
        private float _boxHeight;
        private float _boxColumnSpacing;
        private float _boxUnit;
        private float _boxWidth; // changed: from StageLayoutData in Build
        private float _boxGap;
        [SerializeField, HideInInspector] private string _builtKey; // added: the layout stamp of the last Edit Mode build (LayoutStamp)

        public int ChairCount => _chairCount;
        public float ChairSize => _chairSize;
        public Vector3 BoxWorld => transform.TransformPoint(_boxCenter);

        public void Init(Func<int, Color> colorOf)
        {
            _colorOf = colorOf;
        }

        public void Build(int chairCount, Vector2 size, GameConfig config, StageLayoutData layout) // changed: layout added
        {
            _boxWidth = layout.BoxWidth; // added from here
            _boxGap = layout.BoxGap;
            _chairCount = chairCount;

            bool hasBox = _overflowBox != null && _overflowBox.sprite != null;
            float boxWidth = hasBox ? _boxWidth : 0f;
            float gap = hasBox ? _boxGap : 0f;
            _spacing = Mathf.Min(1f, (size.x - boxWidth - gap) / chairCount);
            _chairSize = Mathf.Min(_spacing * 0.85f, size.y);
            _left = -(chairCount * _spacing + gap + boxWidth) / 2f;

            string key = LayoutStamp.Of(chairCount, size.x, size.y, boxWidth, gap, hasBox ? 1f : 0f); // added from here
            bool keep = LayoutStamp.CanKeep(this, _builtKey, key);
            LayoutStamp.Record(this, ref _builtKey, key);
            PlaceBox(hasBox, boxWidth, gap, keep); // changed: keep added

            _chairs = SquarePool.Obtain(_chairs, transform, "Chairs", 0);
            _units = SquarePool.Obtain(_units, transform, "Units", 2);
            _chairs.Warm(chairCount);
            _units.Warm(chairCount + Mathf.Max(1, config.QueueUnitBuffer));
            _chairs.KeepGeometry = keep; // added: the chairs stand still; the units are always placed
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

            int count = state.Queue.Count;
            for (int i = 0; i < count; i++)
            {
                float unit = i < _chairCount ? _chairSize * 0.7f : _boxUnit;
                Sprite icon = _skin != null ? _skin.Fruit(state.Queue[i]).Icon : null;
                if (icon != null) _units.Fit(i, icon, UnitLocal(i), unit, Color.white);
                else _units.Stretch(i, _sprite, UnitLocal(i), unit, unit, _colorOf(state.Queue[i]));
            }

            _units.HideFrom(count);
        }

        public Vector3 ChairLocal(int index)
        {
            return new Vector3(_left + (index + 0.5f) * _spacing, 0f, 0f);
        }

        public Vector3 UnitLocal(int index)
        {
            if (index < _chairCount) return ChairLocal(index);

            int slot = Mathf.Min(index - _chairCount, BoxRows * BoxColumns - 1);
            int row = slot / BoxColumns;
            int column = slot % BoxColumns;
            float rowHeight = _boxHeight / BoxRows;
            float x = (column - (BoxColumns - 1) / 2f) * _boxColumnSpacing;
            float y = (BoxRows - 1) / 2f * rowHeight - row * rowHeight;
            return _boxCenter + new Vector3(x, y, 0f);
        }

        public Vector3 ChairWorld(int index)
        {
            return transform.TransformPoint(ChairLocal(index));
        }

        public Vector3 UnitWorld(int index)
        {
            return transform.TransformPoint(UnitLocal(index));
        }

        public void HideUnit(int index)
        {
            _units.Hide(index);
        }

        public bool Matches(BoardState state)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                bool shown = _units.Peek(i).gameObject.activeSelf;
                if (shown != i < state.Queue.Count) return false;
            }

            return _units.Count >= state.Queue.Count;
        }

        private void PlaceBox(bool hasBox, float boxWidth, float gap, bool keep) // changed: keep added
        {
            if (hasBox)
            {
                float scale = boxWidth / _overflowBox.sprite.bounds.size.x;
                _boxHeight = _overflowBox.sprite.bounds.size.y * scale;
                _boxCenter = new Vector3(_left + _chairCount * _spacing + gap + boxWidth / 2f, 0f, 0f);
                _boxColumnSpacing = boxWidth * 0.17f;
                if (!keep) // changed: the numbers above are always computed (the units use them); the box itself is moved only when the layout changed
                {
                    _overflowBox.transform.localPosition = _boxCenter;
                    _overflowBox.transform.localScale = new Vector3(scale, scale, 1f);
                }

                _overflowBox.gameObject.SetActive(true);
            }
            else
            {
                _boxHeight = _spacing;
                _boxCenter = new Vector3(0f, _spacing, 0f);
                _boxColumnSpacing = _chairCount * _spacing / BoxColumns;
            }

            _boxUnit = Mathf.Min(_boxColumnSpacing * 0.95f, _boxHeight / BoxRows * 0.8f);
        }
    }
}