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
        [SerializeField] private Transform _overflowAnchor;
        [SerializeField] private SpriteRenderer _overflowPlank;

        private SquarePool _chairs;
        private SquarePool _units;
        private Func<int, Color> _colorOf;
        private int _chairCount;
        private float _spacing;
        private float _chairSize;
        private int _unitBuffer;
        private float _overflowScale = 1f;
        private float _overflowSpacing;
        private float _fadeSeconds;
        private bool _plankVisible;
        private Coroutine _fade;
        
        public void Init(Func<int, Color> colorOf)
        {
            _colorOf = colorOf;
        }

        public void Build(int chairCount, Vector2 size, GameConfig config)
        {
            _chairCount = chairCount;
            _spacing = Mathf.Min(1f, size.x / chairCount);
            _chairSize = Mathf.Min(_spacing * 0.85f, size.y);
            _unitBuffer = Mathf.Max(1, config.QueueUnitBuffer);
            _overflowScale = config.OverflowUnitScale;
            _fadeSeconds = config.OverflowFadeSeconds;
            _overflowSpacing = Mathf.Min(_spacing * _overflowScale, chairCount * _spacing / _unitBuffer);

            _chairs = SquarePool.Obtain(_chairs, transform, "Chairs", 0);
            _units = SquarePool.Obtain(_units, transform, "Units", 2);
            _chairs.Warm(chairCount);
            _units.Warm(chairCount + _unitBuffer);
            _chairs.HideFrom(0);
            _units.HideFrom(0);
            ResetPlank();
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
                float unit = _chairSize * 0.7f * (IsOverflow(i) ? _overflowScale : 1f);
                Sprite icon = _skin != null ? _skin.Fruit(state.Queue[i]).Icon : null;
                if (icon != null) _units.Fit(i, icon, UnitLocal(i), unit, Color.white);
                else _units.Stretch(i, _sprite, UnitLocal(i), unit, unit, _colorOf(state.Queue[i]));
            }

            _units.HideFrom(count);
            ShowPlank(count > _chairCount);
        }

        public Vector3 ChairLocal(int index)
        {
            return new Vector3(X(index), 0f, 0f);
        }

        public Vector3 UnitLocal(int index)
        {
            if (index < _chairCount) return new Vector3(X(index), 0f, 0f);

            int slot = index - _chairCount;
            if (_overflowAnchor == null) return new Vector3(X(slot % _chairCount), _spacing, 0f);
            
            Vector3 origin = transform.InverseTransformPoint(_overflowAnchor.position);
            float x = (slot - (_unitBuffer - 1) / 2f) * _overflowSpacing;
            return origin + new Vector3(x, 0f, 0f);
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
        
        private bool IsOverflow(int index)
        {
            return index >= _chairCount && _overflowAnchor != null;
        }

        private void ResetPlank()
        {
            if (_fade != null) StopCoroutine(_fade);
            _fade = null;
            _plankVisible = false;
            if (_overflowPlank != null) _overflowPlank.gameObject.SetActive(false);
        }

        private void ShowPlank(bool visible)
        {
            if (_overflowPlank == null || visible == _plankVisible) return;

            _plankVisible = visible;
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadePlank(visible));
        }

        private IEnumerator FadePlank(bool visible)
        {
            Color color = _overflowPlank.color;
            float from = visible ? 0f : color.a;
            float to = visible ? 1f : 0f;
            _overflowPlank.gameObject.SetActive(true);

            for (float t = 0f; t < _fadeSeconds; t += Time.deltaTime)
            {
                color.a = Mathf.Lerp(from, to, t / _fadeSeconds);
                _overflowPlank.color = color;
                yield return null;
            }

            color.a = to;
            _overflowPlank.color = color;
            if (!visible) _overflowPlank.gameObject.SetActive(false);
            _fade = null;
        }
        
        private float X(int index)
        {
            return (index - (_chairCount - 1) / 2f) * _spacing;
        }
    }
}
