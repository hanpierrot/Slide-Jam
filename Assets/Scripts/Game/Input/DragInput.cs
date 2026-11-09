using System;
using SlideJam.Engine;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SlideJam.Game
{
    public class DragInput : MonoBehaviour
    {
        private enum DragState
        {
            Idle,
            Pressed,
            Dragging,
        }
        
        private BoardView _boardView;
        private Camera _camera;
        private GameConfig _config; // added
        private DragState _state;
        private Vector2 _pressLocal;
        private int _row;
        private int _col;
        private Axis _axis;
        private int _index;
        private float _offset;
        
        public event Action<LineDrag> Dragging;
        public event Action<LineDrag> Released; 

        public void Init(BoardView boardView, Camera camera, GameConfig config) // changed
        {
            _boardView = boardView;
            _camera = camera;
            _config = config; // added
        }
        
        private void OnDisable()
        {
            _state = DragState.Idle;
        }

        private void Update()
        {
            if (_boardView == null) return;

            if (Input.GetMouseButtonDown(0)) OnPress();
            else if (Input.GetMouseButton(0)) OnHold();

            if (Input.GetMouseButtonUp(0)) OnRelease();
        }
        
        private void OnPress()
        {
            if (PointerOverUi()) return;

            Vector3 world = PointerWorld();
            if (!_boardView.TryGetCell(world, out _row, out _col)) return;

            _pressLocal = _boardView.ToLocal(world);
            _state = DragState.Pressed;
        }

        private void OnHold()
        {
            if (_state == DragState.Idle) return;

            Vector2 delta = (Vector2)_boardView.ToLocal(PointerWorld()) - _pressLocal;
            if (_state == DragState.Pressed)
            {
                if (delta.magnitude < LockDistanceLocal()) return;

                _axis = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? Axis.Row : Axis.Col;
                _index = _axis == Axis.Row ? _row : _col;
                _state = DragState.Dragging;
            }

            _offset = _axis == Axis.Row ? delta.x : -delta.y;
            Dragging?.Invoke(new LineDrag(_axis, _index, _offset));
        }

        private void OnRelease()
        {
            if (_state == DragState.Dragging)
            {
                Released?.Invoke(new LineDrag(_axis, _index, _offset));
            }

            _state = DragState.Idle;
        }
        
        private static bool PointerOverUi()
        {
            if (EventSystem.current == null) return false;

            int pointerId = Input.touchCount > 0 ? Input.GetTouch(0).fingerId : -1;
            return EventSystem.current.IsPointerOverGameObject(pointerId);
        }

        private Vector3 PointerWorld()
        {
            Vector3 screen = Input.mousePosition;
            screen.z = -_camera.transform.position.z;
            return _camera.ScreenToWorldPoint(screen);
        }

        private float LockDistanceLocal()
        {
            float worldWidth = 2f * _camera.orthographicSize * _camera.aspect;
            float world = _config.LockDistance / _config.ReferenceWidth * worldWidth; // changed
            return world / _boardView.transform.lossyScale.x;
        }
    }
}
