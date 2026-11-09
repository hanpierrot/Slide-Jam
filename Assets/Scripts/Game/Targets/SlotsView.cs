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
        [SerializeField] private ThemeSkin _skin;
        [SerializeField] private float _slotGap = 0.2f;
        [SerializeField] private float _basketOverflow = 0.3f;
        [SerializeField] private float _basketOffsetY = -0.2f;
        [SerializeField] private Color _emptyColor = new Color(0.22f, 0.22f, 0.22f);
        [SerializeField] private Color _pipOffColor = new Color(0f, 0f, 0f, 0.4f);
        private readonly List<Transform> _popItems = new List<Transform>();
        private readonly List<Vector3> _popPositions = new List<Vector3>();
        private readonly List<Vector3> _popScales = new List<Vector3>();
        private Vector3 _popOrigin;

        private SquarePool _shadows;
        private SquarePool _backs;
        private SquarePool _pipSlots;
        private SquarePool _fruits;
        private SquarePool _fronts;
        private SquarePool _locks;
        private Func<int, Color> _colorOf;
        private int _slotCount;
        private int _maxCap;
        private float _slotWidth;
        private float _height;
        private float _spacing;
        private float _pipSize;
        private float _basketWidth;
        
        private bool Skinned => _skin != null && _skin.BasketFront != null;
        
        public void Init(Func<int, Color> colorOf)
        {
            _colorOf = colorOf;
        }

        public void Build(int slotCount, int maxCap, Vector2 size)
        {
            _slotCount = slotCount;
            _maxCap = maxCap;
            _height = size.y;
            _slotWidth = (size.x - _slotGap * (slotCount - 1)) / slotCount;
            _spacing = _slotWidth / maxCap;
            _pipSize = Mathf.Min(_spacing * 0.7f, size.y * 0.3f);
            _basketWidth = 0f;
            
            _backs = SquarePool.Obtain(_backs, transform, "Backs", 1);
            _pipSlots = SquarePool.Obtain(_pipSlots, transform, "PipSlots", 2);
            _backs.Warm(slotCount);
            _pipSlots.Warm(slotCount * maxCap);

            if (Skinned)
            {
                Vector2 canvas = _skin.BasketFront.bounds.size;
                _basketWidth = Mathf.Min(_slotWidth, (_height + _basketOverflow) * canvas.x / canvas.y);

                _shadows = SquarePool.Obtain(_shadows, transform, "Shadows", 0);
                _fruits = SquarePool.Obtain(_fruits, transform, "Fruits", 3);
                _fronts = SquarePool.Obtain(_fronts, transform, "Fronts", 4);
                _locks = SquarePool.Obtain(_locks, transform, "Locks", 5);
                _shadows.Warm(slotCount);
                _fruits.Warm(slotCount * maxCap);
                _fronts.Warm(slotCount);
                _locks.Warm(slotCount);
            }

            HideAll();
        }

        public void Show(BoardState state)
        {
            if (Skinned) ShowBaskets(state);
            else ShowFlat(state);
        }
        
        public Vector3 SlotLocal(int slot)
        {
            float x = (slot - (_slotCount - 1) / 2f) * (_slotWidth + _slotGap);
            return new Vector3(x, Skinned ? _basketOffsetY : 0f, 0f);
        }

        public Vector3 PipLocal(int slot, int pip, int cap)
        {
            Vector3 center = SlotLocal(slot);
            if (Skinned)
            {
                ThemeSkin.BasketLayout layout = _skin.Basket;
                float pitch = _basketWidth * layout.InteriorWidth / cap;
                return new Vector3(center.x + (pip - (cap - 1) / 2f) * pitch, center.y + _basketWidth * layout.PipY, 0f);
            }

            return new Vector3(center.x + (pip - (cap - 1) / 2f) * _spacing, -_height / 2f + _pipSize, 0f);
        }

        public Vector3 SlotWorld(int slot)
        {
            return transform.TransformPoint(SlotLocal(slot));
        }

        public Vector3 PipWorld(int slot, int pip, int cap)
        {
            return transform.TransformPoint(PipLocal(slot, pip, cap));
        }
        
        public void BeginPop(int slot)
        {
            _popItems.Clear();
            _popPositions.Clear();
            _popScales.Clear();
            _popOrigin = SlotLocal(slot);

            CollectPop(_shadows, slot);
            CollectPop(_backs, slot);
            CollectPop(_fronts, slot);
            CollectPop(_locks, slot);
            for (int k = 0; k < _maxCap; k++)
            {
                int index = slot * _maxCap + k;
                CollectPop(_pipSlots, index);
                CollectPop(_fruits, index);
            }
        }
        
        public void SetPop(float factor)
        {
            for (int i = 0; i < _popItems.Count; i++)
            {
                _popItems[i].localPosition = _popOrigin + (_popPositions[i] - _popOrigin) * factor;
                _popItems[i].localScale = _popScales[i] * factor;
            }
        }

        public void EndPop()
        {
            SetPop(1f);
            _popItems.Clear();
            _popPositions.Clear();
            _popScales.Clear();
        }
        
        public bool Matches(BoardState state)
        {
            if (!Skinned) return true;

            for (int i = 0; i < _slotCount; i++)
            {
                Slot slot = i < state.Slots.Length ? state.Slots[i] : Slot.Empty;
                SpriteRenderer shadow = _shadows.Peek(i);
                bool shown = shadow != null && shadow.gameObject.activeSelf;
                if (shown == slot.IsEmpty) return false;

                int filled = 0;
                for (int k = 0; k < _maxCap; k++)
                {
                    SpriteRenderer fruit = _fruits.Peek(i * _maxCap + k);
                    if (fruit != null && fruit.gameObject.activeSelf) filled++;
                }

                int expected = slot.IsEmpty || slot.Locked ? 0 : slot.Count;
                if (filled != expected) return false;
            }

            return true;
        }
        
        private void CollectPop(SquarePool pool, int index)
        {
            SpriteRenderer square = pool?.Peek(index);
            if (square == null || !square.gameObject.activeSelf) return;

            _popItems.Add(square.transform);
            _popPositions.Add(square.transform.localPosition);
            _popScales.Add(square.transform.localScale);
        }
        
        private void ShowFlat(BoardState state)
        {
            for (int i = 0; i < _slotCount; i++)
            {
                Slot slot = i < state.Slots.Length ? state.Slots[i] : Slot.Empty;
                Color baseColor = slot.IsEmpty ? _emptyColor : _colorOf(slot.Color);
                Color back = slot.IsEmpty ? _emptyColor : Dim(baseColor);
                _backs.Stretch(i, _sprite, SlotLocal(i), _slotWidth, _height, back);

                for (int k = 0; k < _maxCap; k++)
                {
                    int index = i * _maxCap + k;
                    if (slot.IsEmpty || k >= slot.Cap)
                    {
                        _pipSlots.Hide(index);
                        continue;
                    }

                    _pipSlots.Stretch(index, _sprite, PipLocal(i, k, slot.Cap), _pipSize, _pipSize,
                        k < slot.Count ? baseColor : _pipOffColor);
                }
            }
        }

        private void ShowBaskets(BoardState state)
        {
            for (int i = 0; i < _slotCount; i++)
            {
                Slot slot = i < state.Slots.Length ? state.Slots[i] : Slot.Empty;
                Vector3 origin = SlotLocal(i);

                if (slot.IsEmpty)
                {
                    HideSlot(i);
                    continue;
                }

                ThemeSkin.FruitArt art = _skin.Fruit(slot.Color);
                _shadows.Fit(i, _skin.BasketShadow, origin, _basketWidth, Color.white);

                if (slot.Locked)
                {
                    _locks.Fit(i, art.BasketLocked, origin, _basketWidth, Color.white);
                    _backs.Hide(i);
                    _fronts.Hide(i);
                    HidePips(i);
                    continue;
                }

                _locks.Hide(i);
                _backs.Fit(i, art.BasketBack, origin, _basketWidth, Color.white);
                _fronts.Fit(i, _skin.BasketFront, origin, _basketWidth, Color.white);

                ThemeSkin.BasketLayout layout = _skin.Basket; // changed
                float pitch = _basketWidth * layout.InteriorWidth / slot.Cap;
                float pipWidth = Mathf.Min(_basketWidth * layout.PipWidth, pitch * layout.PipFill);
                for (int k = 0; k < _maxCap; k++)
                {
                    int index = i * _maxCap + k;
                    if (k >= slot.Cap)
                    {
                        _pipSlots.Hide(index);
                        _fruits.Hide(index);
                        continue;
                    }

                    Vector3 position = PipLocal(i, k, slot.Cap);
                    if (k < slot.Count)
                    {
                        _pipSlots.Hide(index);
                        _fruits.Fit(index, art.Icon, position, pipWidth * layout.FruitInPip, Color.white); // changed
                    }
                    else
                    {
                        _fruits.Hide(index);
                        _pipSlots.Fit(index, art.PipEmpty, position, pipWidth, Color.white);
                    }
                }
            }
        }

        private void HideSlot(int slotIndex)
        {
            _shadows.Hide(slotIndex);
            _backs.Hide(slotIndex);
            _fronts.Hide(slotIndex);
            _locks.Hide(slotIndex);
            HidePips(slotIndex);
        }

        private void HidePips(int slotIndex)
        {
            for (int k = 0; k < _maxCap; k++)
            {
                int index = slotIndex * _maxCap + k;
                _pipSlots.Hide(index);
                _fruits?.Hide(index);
            }
        }

        private void HideAll()
        {
            _shadows?.HideFrom(0);
            _backs?.HideFrom(0);
            _pipSlots?.HideFrom(0);
            _fruits?.HideFrom(0);
            _fronts?.HideFrom(0);
            _locks?.HideFrom(0);
        }

        private static Color Dim(Color color)
        {
            return new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 1f);
        }
    }
}
