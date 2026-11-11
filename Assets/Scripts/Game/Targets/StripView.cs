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
        [SerializeField] private ThemeSkin _skin;
        private int _visible; // changed: set by Build from GameConfig

        private SquarePool _chips;
        private SquarePool _icons;
        private Func<int, Color> _colorOf;
        private float _spacing;
        private float _itemSize;
        [SerializeField, HideInInspector] private string _builtKey; // added: the layout stamp of the last Edit Mode build (LayoutStamp)

        private bool Skinned => _skin != null;

        public void Init(Func<int, Color> colorOf)
        {
            _colorOf = colorOf;
        }

        public void Build(Vector2 size, int visible) // changed
        {
            _visible = Mathf.Max(1, visible); // added
            _spacing = Mathf.Min(0.7f, size.x / _visible);
            _itemSize = Mathf.Min(_spacing * 0.8f, size.y);

            string key = LayoutStamp.Of(size.x, size.y, _visible, Skinned ? 1f : 0f); // added from here
            bool keep = LayoutStamp.CanKeep(this, _builtKey, key);
            LayoutStamp.Record(this, ref _builtKey, key);

            _chips = SquarePool.Obtain(_chips, transform, "Chips", 0);
            _chips.Warm(_visible);
            _chips.HideFrom(0);
            _chips.KeepGeometry = keep; // added
            if (Skinned)
            {
                _icons = SquarePool.Obtain(_icons, transform, "Icons", 1);
                _icons.Warm(_visible);
                _icons.KeepGeometry = keep; // added
            }

            _icons?.HideFrom(0);
        }

        public void Show(BoardState state)
        {
            int shown = Mathf.Clamp(state.Seq.Length - state.SeqI, 0, _visible);
            for (int k = 0; k < shown; k++)
            {
                Target target = state.Seq[state.SeqI + k];
                Vector3 position = ChipLocal(k);
                if (Skinned)
                {
                    ThemeSkin.FruitArt art = _skin.Fruit(target.Color);
                    float chipWidth = _spacing * 0.92f;
                    _chips.Fit(k, art.Chip, position, chipWidth, Color.white);
                    _icons.Fit(k, art.Icon, position, chipWidth * 0.5f, Color.white);
                }
                else
                {
                    _chips.Stretch(k, _sprite, position, _itemSize, _itemSize, _colorOf(target.Color));
                }
            }

            _chips.HideFrom(shown);
            _icons?.HideFrom(shown);
        }

        public Vector3 ChipLocal(int index)
        {
            return new Vector3((index - (_visible - 1) / 2f) * _spacing, 0f, 0f);
        }

        public Vector3 ChipWorld(int index)
        {
            return transform.TransformPoint(ChipLocal(index));
        }
    }
}
