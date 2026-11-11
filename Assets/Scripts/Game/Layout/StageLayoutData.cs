using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlideJam.Game
{
    // The layout of the Stage for one board size: where the six anchors stand (saved from the scene by Slide Jam > Save Stage Layout)
    // and the sizes the views use to draw inside their anchors. Play mode never moves an anchor; only the menus apply this data.
    [CreateAssetMenu(fileName = "StageLayoutData", menuName = "Slide Jam/Stage Layout Data")]
    public class StageLayoutData : ScriptableObject
    {
        // One object below the anchors (a Pool_* square, the backdrop art, the vault box, a label): what Setup Scene needs to put it back
        [Serializable]
        public struct ItemLayout
        {
            public string Path; // relative to the Stage, for example Queue/Pool_Chairs/Square 3
            public Vector3 Position;
            public Vector3 Scale;
            public float RotationZ;
            public Vector2 Size; // RectTransform.sizeDelta; zero when the object is not a RectTransform
            public float FontSize; // TextMeshPro font size; zero when the object has no text
        }

        [Header("Anchor positions (local to the Stage; written by Save Stage Layout)")]
        [SerializeField] private bool _hasPositions;
        [SerializeField] private Vector3 _slotsPosition;
        [SerializeField] private Vector3 _stripPosition;
        [SerializeField] private Vector3 _queuePosition;
        [SerializeField] private Vector3 _branchPosition;
        [SerializeField] private Vector3 _dotsPosition;
        [SerializeField] private Vector3 _gridPosition;

        [Header("Anchor scales (local to the Stage; written by Save Stage Layout)")]
        [SerializeField] private bool _hasScales;
        [SerializeField] private Vector3 _slotsScale = Vector3.one;
        [SerializeField] private Vector3 _stripScale = Vector3.one;
        [SerializeField] private Vector3 _queueScale = Vector3.one;
        [SerializeField] private Vector3 _branchScale = Vector3.one;
        [SerializeField] private Vector3 _dotsScale = Vector3.one;
        [SerializeField] private Vector3 _gridScale = Vector3.one;

        [Header("Items below the anchors (written by Save Stage Layout)")]
        [SerializeField] private List<ItemLayout> _items = new List<ItemLayout>();

        [Header("Bands")]
        [SerializeField, Min(0f)] private float _slotsHeight = 1.6f;
        [SerializeField, Min(0f)] private float _stripHeight = 0.6f;
        [SerializeField, Min(0f)] private float _queueHeight = 0.9f;

        [Header("Slots")]
        [SerializeField] private float _slotsExtraWidth = 0.4f;
        [SerializeField] private float _slotGap = 0.05f;
        [SerializeField] private float _basketOverflow = 0.3f;
        [SerializeField] private float _basketOffsetY = -0.2f;

        [Header("Queue")]
        [SerializeField] private float _boxWidth = 1.5f;
        [SerializeField] private float _boxGap = 0.12f;

        [Header("Next-spawn dots")]
        [SerializeField] private float _nearSize = 0.4f;
        [SerializeField] private float _farSize = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _farAlpha = 0.6f;
        [SerializeField] private float _bottomPad = 0.1f;
        [SerializeField] private float _dotGap;
        [SerializeField] private float _hangerWidth = 0.08f;

        public bool HasPositions => _hasPositions;
        public Vector3 SlotsPosition => _slotsPosition;
        public Vector3 StripPosition => _stripPosition;
        public Vector3 QueuePosition => _queuePosition;
        public Vector3 BranchPosition => _branchPosition;
        public Vector3 DotsPosition => _dotsPosition;
        public Vector3 GridPosition => _gridPosition;

        public bool HasScales => _hasScales;
        public Vector3 SlotsScale => _slotsScale;
        public Vector3 StripScale => _stripScale;
        public Vector3 QueueScale => _queueScale;
        public Vector3 BranchScale => _branchScale;
        public Vector3 DotsScale => _dotsScale;
        public Vector3 GridScale => _gridScale;

        public IReadOnlyList<ItemLayout> Items => _items;

        public float SlotsHeight => _slotsHeight;
        public float StripHeight => _stripHeight;
        public float QueueHeight => _queueHeight;

        public float SlotsExtraWidth => _slotsExtraWidth;
        public float SlotGap => _slotGap;
        public float BasketOverflow => _basketOverflow;
        public float BasketOffsetY => _basketOffsetY;

        public float BoxWidth => _boxWidth;
        public float BoxGap => _boxGap;

        public float NearSize => _nearSize;
        public float FarSize => _farSize;
        public float FarAlpha => _farAlpha;
        public float BottomPad => _bottomPad;
        public float DotGap => _dotGap;
        public float HangerWidth => _hangerWidth;

        // Used by the editor tool (Save Stage Layout) and by tests
        public void SetPositions(Vector3 slots, Vector3 strip, Vector3 queue, Vector3 branch, Vector3 dots, Vector3 grid)
        {
            _slotsPosition = slots;
            _stripPosition = strip;
            _queuePosition = queue;
            _branchPosition = branch;
            _dotsPosition = dots;
            _gridPosition = grid;
            _hasPositions = true;
        }

        // The objects below the anchors, written by the editor tool
        public void SetItems(List<ItemLayout> items)
        {
            _items = items;
        }

        // Same, for the local scales of the six anchors. Assets saved before this existed have no scales and keep the scene's scales.
        public void SetScales(Vector3 slots, Vector3 strip, Vector3 queue, Vector3 branch, Vector3 dots, Vector3 grid)
        {
            _slotsScale = slots;
            _stripScale = strip;
            _queueScale = queue;
            _branchScale = branch;
            _dotsScale = dots;
            _gridScale = grid;
            _hasScales = true;
        }
    }
}
