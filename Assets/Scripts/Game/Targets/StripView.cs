using System;
using System.Collections;
using System.Collections.Generic;
using SlideJam.Engine;
using UnityEngine;

namespace SlideJam.Game
{
    public class StripView : MonoBehaviour
    {
        [SerializeField] private Sprite _sprite;
        [SerializeField] private ThemeSkin _skin; // added: optional, null keeps the flat coloured squares
        [SerializeField, Min(1)] private int _visible = 6;

        private readonly List<SpriteRenderer> _items = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _chips = new List<SpriteRenderer>(); // added
        private readonly List<SpriteRenderer> _icons = new List<SpriteRenderer>(); // added
        private Func<int, Color> _colorOf;
        private float _spacing;
        private float _itemSize;

        public void Init(Func<int, Color> colorOf)
        {
            _colorOf = colorOf;
        }

        public void Build(Vector2 size)
        {
            if (_skin != null) Squares.Prepare(transform, _chips, _icons); // changed
            else Squares.Prepare(transform, _items);
            _spacing = Mathf.Min(0.7f, size.x / _visible);
            _itemSize = Mathf.Min(_spacing * 0.8f, size.y);
            Squares.HideFrom(_items, _visible);
            Squares.HideFrom(_chips, _visible); // added
            Squares.HideFrom(_icons, _visible); // added
        }

        public void Show(BoardState state)
        {
            int shown = Mathf.Clamp(state.Seq.Length - state.SeqI, 0, _visible);
            if (_skin != null)
            {
                ShowChips(state, shown); // added
                return;
            }

            for (int k = 0; k < shown; k++)
            {
                Target target = state.Seq[state.SeqI + k];
                float x = (k - (_visible - 1) / 2f) * _spacing;
                Squares.Set(Squares.At(_items, k, transform, _sprite, 0), new Vector3(x, 0f, 0f), _itemSize, _itemSize,
                    _colorOf(target.Color));
            }

            Squares.HideFrom(_items, shown);
        }

        // added: a shelf chip per upcoming target with the fruit on top of it
        private void ShowChips(BoardState state, int shown)
        {
            float chipWidth = _spacing * 0.92f;
            for (int k = 0; k < shown; k++)
            {
                Target target = state.Seq[state.SeqI + k];
                ThemeSkin.FruitArt art = _skin.Fruit(target.Color);
                float x = (k - (_visible - 1) / 2f) * _spacing;
                var position = new Vector3(x, 0f, 0f);

                SpriteRenderer chip = Squares.At(_chips, k, transform, art.Chip, 0);
                chip.sprite = art.Chip;
                if (art.Chip != null) Squares.SetFit(chip, position, chipWidth, Color.white);
                chip.gameObject.SetActive(art.Chip != null);

                SpriteRenderer icon = Squares.At(_icons, k, transform, art.Icon, 1);
                icon.sprite = art.Icon;
                if (art.Icon != null) Squares.SetFit(icon, position, chipWidth * 0.5f, Color.white);
                icon.gameObject.SetActive(art.Icon != null);
            }

            Squares.HideFrom(_chips, shown);
            Squares.HideFrom(_icons, shown);
        }
    }
}
