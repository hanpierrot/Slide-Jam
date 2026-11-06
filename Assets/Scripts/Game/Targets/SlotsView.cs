using System;
using System.Collections;
using System.Collections.Generic;
using SlideJam.Engine;
using UnityEngine;

namespace SlideJam.Game
{
    public class SlotsView : MonoBehaviour
    {
        [SerializeField] private Sprite _sprite;
        [SerializeField] private ThemeSkin _skin; // added: optional, null keeps the flat coloured slots
        [SerializeField] private float _slotGap = 0.2f;
        [SerializeField] private float _basketOverflow = 0.3f; // added: how much taller than the slot band a basket may be
        [SerializeField] private float _basketOffsetY = -0.2f; // added: moves the baskets down inside the band
        [SerializeField] private Color _emptyColor = new Color(0.22f, 0.22f, 0.22f);
        [SerializeField] private Color _pipOffColor = new Color(0f, 0f, 0f, 0.4f);

        private readonly List<SpriteRenderer> _backs = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _pips = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _shadows = new List<SpriteRenderer>(); // added from here: the skinned basket layers
        private readonly List<SpriteRenderer> _baskets = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _pipSlots = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _fruits = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _fronts = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _locks = new List<SpriteRenderer>();
        private Func<int, Color> _colorOf;
        private int _slotCount;
        private int _maxCap;
        private float _slotWidth;
        private float _height;
        private float _spacing;
        private float _pipSize;
        
        public void Init(Func<int, Color> colorOf)
        {
            _colorOf = colorOf;
        }

        public void Build(int slotCount, int maxCap, Vector2 size)
        {
            if (_skin != null) Squares.Prepare(transform, _shadows, _baskets, _pipSlots, _fruits, _fronts, _locks); // changed
            else Squares.Prepare(transform, _backs, _pips);
            _slotCount = slotCount;
            _maxCap = maxCap;
            _height = size.y;
            _slotWidth = (size.x - _slotGap * (slotCount - 1)) / slotCount;
            _spacing = _slotWidth / maxCap;
            _pipSize = Mathf.Min(_spacing * 0.7f, size.y * 0.3f);

            Squares.HideFrom(_backs, slotCount);
            Squares.HideFrom(_pips, slotCount * maxCap);
            Squares.HideFrom(_shadows, slotCount); // added from here
            Squares.HideFrom(_baskets, slotCount);
            Squares.HideFrom(_fronts, slotCount);
            Squares.HideFrom(_locks, slotCount);
            Squares.HideFrom(_pipSlots, slotCount * maxCap);
            Squares.HideFrom(_fruits, slotCount * maxCap);
        }

        public void Show(BoardState state)
        {
            if (_skin != null)
            {
                ShowBaskets(state); // added
                return;
            }

            for (int i = 0; i < _slotCount; i++)
            {
                float x = (i - (_slotCount - 1) / 2f) * (_slotWidth + _slotGap);
                Slot slot = i < state.Slots.Length ? state.Slots[i] : Slot.Empty;
                Color baseColor = slot.IsEmpty ? _emptyColor : _colorOf(slot.Color);
                Color back = slot.IsEmpty ? _emptyColor : Dim(baseColor);
                Squares.Set(Squares.At(_backs, i, transform, _sprite, 0), new Vector3(x, 0f, 0f), _slotWidth, _height, back);

                float pipY = -_height / 2f + _pipSize;
                for (int k = 0; k < _maxCap; k++)
                {
                    SpriteRenderer pip = Squares.At(_pips, i * _maxCap + k, transform, _sprite, 1);
                    if (slot.IsEmpty || k >= slot.Cap)
                    {
                        pip.gameObject.SetActive(false);
                        continue;
                    }

                    float pipX = x + (k - (slot.Cap - 1) / 2f) * _spacing;
                    Squares.Set(pip, new Vector3(pipX, pipY, 0f), _pipSize, _pipSize, k < slot.Count ? baseColor : _pipOffColor);
                }
            }
        }

        // added: layers per slot, bottom to top: shadow, back (by fruit), empty pips, fruit pips, front frame; a locked slot shows shadow + locked art only
        private void ShowBaskets(BoardState state)
        {
            Vector2 canvas = _skin.BasketFront.bounds.size;
            float width = Mathf.Min(_slotWidth, (_height + _basketOverflow) * canvas.x / canvas.y); // changed

            for (int i = 0; i < _slotCount; i++)
            {
                float x = (i - (_slotCount - 1) / 2f) * (_slotWidth + _slotGap);
                Slot slot = i < state.Slots.Length ? state.Slots[i] : Slot.Empty;
                var origin = new Vector3(x, _basketOffsetY, 0f); // changed

                SpriteRenderer shadow = Squares.At(_shadows, i, transform, _skin.BasketShadow, 0);
                SpriteRenderer basket = Squares.At(_baskets, i, transform, _skin.BasketFront, 1);
                SpriteRenderer front = Squares.At(_fronts, i, transform, _skin.BasketFront, 4);
                SpriteRenderer locked = Squares.At(_locks, i, transform, _skin.BasketFront, 5);

                if (slot.IsEmpty)
                {
                    shadow.gameObject.SetActive(false);
                    basket.gameObject.SetActive(false);
                    front.gameObject.SetActive(false);
                    locked.gameObject.SetActive(false);
                    HidePips(i);
                    continue;
                }

                ThemeSkin.FruitArt art = _skin.Fruit(slot.Color);
                shadow.sprite = _skin.BasketShadow;
                Squares.SetFit(shadow, origin, width, Color.white);

                if (slot.Locked)
                {
                    locked.sprite = art.BasketLocked;
                    Squares.SetFit(locked, origin, width, Color.white);
                    basket.gameObject.SetActive(false);
                    front.gameObject.SetActive(false);
                    HidePips(i);
                    continue;
                }

                locked.gameObject.SetActive(false);
                basket.sprite = art.BasketBack;
                Squares.SetFit(basket, origin, width, Color.white);
                front.sprite = _skin.BasketFront;
                Squares.SetFit(front, origin, width, Color.white);

                float interior = width * 0.6f;
                float pitch = interior / slot.Cap;
                float pipWidth = Mathf.Min(width * 0.213f, pitch * 0.95f);
                float pipY = width * 0.053f;
                for (int k = 0; k < _maxCap; k++)
                {
                    int index = i * _maxCap + k;
                    SpriteRenderer empty = Squares.At(_pipSlots, index, transform, art.PipEmpty, 2);
                    SpriteRenderer fruit = Squares.At(_fruits, index, transform, art.Icon, 3);
                    if (k >= slot.Cap)
                    {
                        empty.gameObject.SetActive(false);
                        fruit.gameObject.SetActive(false);
                        continue;
                    }

                    var position = new Vector3(x + (k - (slot.Cap - 1) / 2f) * pitch, pipY + _basketOffsetY, 0f); // changed
                    bool filled = k < slot.Count;
                    empty.sprite = art.PipEmpty;
                    if (art.PipEmpty != null) Squares.SetFit(empty, position, pipWidth, Color.white);
                    empty.gameObject.SetActive(!filled && art.PipEmpty != null);

                    fruit.sprite = art.Icon;
                    if (art.Icon != null) Squares.SetFit(fruit, position, pipWidth * 0.9f, Color.white);
                    fruit.gameObject.SetActive(filled && art.Icon != null);
                }
            }
        }

        private void HidePips(int slotIndex)
        {
            for (int k = 0; k < _maxCap; k++)
            {
                int index = slotIndex * _maxCap + k;
                if (index < _pipSlots.Count) _pipSlots[index].gameObject.SetActive(false);
                if (index < _fruits.Count) _fruits[index].gameObject.SetActive(false);
            }
        }

        private static Color Dim(Color color)
        {
            return new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 1f);
        }
    }
}
