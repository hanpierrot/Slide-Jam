using System;
using System.Collections;
using System.Collections.Generic;
using SlideJam.Engine;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SlideJam.Game
{
    public class DragInput : MonoBehaviour
    {
        private const float ReferenceWidth = 360f;

        [SerializeField] private GameController _controller;
        [SerializeField] private BoardView _boardView;
        [SerializeField] private Camera _camera;
        [SerializeField] private float _lockDistance = 8f;
        
        private enum DragState
        {
            Idle,
            Pressed,
            Dragging,
        }
        
        private DragState _state;
        private Vector2 _pressLocal;
        private int _row;
        private int _col;
        private Axis _axis;
        private int _index;
        private float _offset;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0)) OnPress();
            else if (Input.GetMouseButton(0)) OnHold();

            if (Input.GetMouseButtonUp(0)) OnRelease();
        }
        
        private void OnPress()
        {
            if (_controller.IsBusy || PointerOverUi()) return;

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
            _boardView.ShowLineOffset(_controller.State, _axis, _index, _offset);
        }

        private void OnRelease()
        {
            if (_state == DragState.Dragging)
            {
                _controller.TryMove(_axis, _index, Mathf.RoundToInt(_offset), _offset);
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
            float world = _lockDistance / ReferenceWidth * worldWidth;
            return world / _boardView.transform.lossyScale.x;
        }
    }
}
